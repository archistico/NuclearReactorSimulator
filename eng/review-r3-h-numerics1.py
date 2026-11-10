"""Read-only replay of the frozen H terminal linear model; no plant imports or runs."""
from __future__ import annotations

import argparse
import csv
import gzip
import hashlib
import io
import json
import math
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
PAYLOADS = ROOT / "eng/frozen-evidence/r3-h-feasibility1"


def require(condition, message):
    if not condition:
        raise ValueError(message)


def digest(data):
    return hashlib.sha256(data).hexdigest().upper()


def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def bits(values):
    return b"".join(struct.pack(">d", value) for value in values)


def tree(directory):
    files = sorted((p for p in (ROOT / directory).rglob("*") if p.is_file()
                    and not {"bin", "obj"}.intersection(p.relative_to(ROOT).parts)),
                   key=lambda p: p.relative_to(ROOT).as_posix())
    rows = "".join(f"{p.relative_to(ROOT).as_posix()}|{digest(p.read_bytes())}\n" for p in files)
    return {"file_count": len(files), "tree_sha256": digest(rows.encode())}


def normal(jacobian, residuals, damping):
    size = len(jacobian[0])
    matrix = [[0.0] * (size + 1) for _ in range(size)]
    for i in range(size):
        for j in range(size):
            total = damping if i == j else 0.0
            for row in jacobian:
                total += row[i] * row[j]
            matrix[i][j] = total
        for row, residual in zip(jacobian, residuals):
            matrix[i][size] -= row[i] * residual
    return matrix


def solve(original):
    matrix = [row[:] for row in original]
    size = len(matrix)
    order = list(range(size))
    magnitude = max(abs(v) for row in matrix for v in row[:size])
    require(magnitude > 0 and math.isfinite(magnitude), "Invalid normal matrix")
    for column in range(size):
        pivot = max(range(column, size), key=lambda row: (abs(matrix[row][column]), -order[row]))
        require(abs(matrix[pivot][column]) / magnitude > 1e-14, "Frozen pivot guard rejected replay")
        if pivot != column:
            for j in range(column, size + 1):
                matrix[column][j], matrix[pivot][j] = matrix[pivot][j], matrix[column][j]
            order[column], order[pivot] = order[pivot], order[column]
        for row in range(column + 1, size):
            ratio = matrix[row][column] / matrix[column][column]
            for j in range(column, size + 1):
                matrix[row][j] -= ratio * matrix[column][j]
    answer = [0.0] * size
    for row in range(size - 1, -1, -1):
        value = matrix[row][size]
        for column in range(row + 1, size):
            value -= matrix[row][column] * answer[column]
        answer[row] = value / matrix[row][row]
    require(all(math.isfinite(v) for v in answer), "Nonfinite replay direction")
    return answer


def write_json(output, name, value):
    (output / name).write_text(json.dumps(value, indent=2, allow_nan=False) + "\n", encoding="utf-8")


def write_csv(output, name, rows):
    with (output / name).open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)


def review(contract_path, output):
    require(output.is_relative_to(ROOT / "artifacts"), "Review outputs must be under repository artifacts")
    output.mkdir(parents=True, exist_ok=True)
    names = ["01-provenance.json", "02-terminal-review.json", "03-damping-replay.csv",
             "04-fractional-step-model.csv", "05-review-summary.json", "06-manifest.csv"]
    # Invalidate only this gate's explicit success outputs; no recursive cleanup.
    for name in names:
        (output / name).unlink(missing_ok=True)
    contract = load(contract_path)
    require(contract["schema"] == "r3-h-numerical-stop-review1-v1", "Review schema drift")
    for flag in ("plant_evaluations", "source_changes", "test_changes", "search_rerun", "bounds_expansion",
                 "controller_change", "threshold_change", "r3_passed", "requalification3", "r4"):
        require(contract["authority"][flag] is False, f"Forbidden authority: {flag}")
    require(contract["authority"]["next_activity"] == "H-BOUNDED-SCALED-LOCAL-MODEL-DESIGN1-OFFLINE", "Next activity drift")
    require(contract["replay_coordinate_absolute_guard"] == 1e-12, "Replay arithmetic guard drift")
    require(contract["next_design"] == {
        "id": "H-BOUNDED-SCALED-LOCAL-MODEL-DESIGN1-OFFLINE", "event": 401,
        "variables": 24, "residuals": 75, "fractional_bound": 0.02,
        "column_scaling": "EUCLIDEAN-NORM-NO-RESIDUAL-REWEIGHTING",
        "zero_column_delta": 0, "damping_values": [1e-3, 1e-2, 1e-1, 1, 10, 100],
        "pivot_guard": 1e-14, "fraction_to_box_safety": 0.99,
        "coordinate_clipping": False, "plant_evaluations": 0,
        "nonlinear_search_authorized": False}, "Offline next-design contract drift")
    baseline = {directory: tree(directory) for directory in ("src", "tests")}
    require(baseline == contract["baseline"], "Production/test baseline drift")
    for entry in contract["frozen_inputs"]:
        require(digest((ROOT / entry["path"]).read_bytes()) == entry["sha256"], "Frozen file drift: " + entry["path"])
    for name, markers in contract["document_markers"].items():
        lines = [line.strip() for line in (ROOT / name).read_text(encoding="utf-8-sig").splitlines()]
        for marker in markers:
            require(lines.count(marker) == 1, f"Marker cardinality drift: {marker}")
    payload_map = load(PAYLOADS / "13-payload-map.json")
    payloads = {}
    for entry in payload_map["files"]:
        path = (PAYLOADS / entry["stored_file"]).resolve()
        require(path.is_relative_to(PAYLOADS.resolve()), "Payload path escaped frozen directory")
        stored = path.read_bytes()
        require(digest(stored) == entry["stored_sha256"], "Stored payload digest drift")
        require(entry["compression"] in ("NONE", "GZIP"), "Unsupported frozen compression")
        data = gzip.decompress(stored) if entry["compression"] == "GZIP" else stored
        require(digest(data) == entry["logical_sha256"] and len(data) == entry["logical_bytes"], "Logical payload drift")
        require(entry["logical_file"] not in payloads, "Duplicate logical payload")
        payloads[entry["logical_file"]] = data
    for entry in csv.DictReader(io.StringIO(payloads["08-manifest.csv"].decode("utf-8-sig"))):
        require(digest(payloads[entry["file"]]) == entry["sha256"], "Historical manifest drift")

    trace = [json.loads(line) for line in payloads["04-search-trace.jsonl"].decode().splitlines()]
    require(len(trace) == 527 and [event["Id"] for event in trace] == list(range(1, 528)), "Trace identity drift")
    accepted = [event for event in trace if event["Accepted"]]
    require(len(accepted) == 8, "Accepted-update accounting drift")
    current = accepted[-1]
    require(current["Id"] == 401 and current["Iteration"] == 7, "Terminal starting point drift")
    terminal = [event for event in trace if event["Iteration"] == 8]
    probes = {event["Purpose"]: event for event in terminal if event["Purpose"].startswith("difference")}
    trials = [event for event in terminal if event["Purpose"].startswith("trial/")]
    require(len(probes) == 48 and len(trials) == 78, "Terminal workload drift")
    require(all(event["Reason"] == "VALID" for event in probes.values()), "Invalid terminal derivative probes")
    require(all(event["Reason"] == "OUTSIDE-FROZEN-BOUNDS" and event["Residuals"] is None and not event["Accepted"]
                for event in trials), "Terminal trial rejection class drift")
    plan = json.loads(payloads["execution-contract.json"])
    require(plan["search"]["fractional_bound"] == 0.02 and plan["search"]["central_difference_step"] == 1e-6,
            "Frozen numerical source contract drift")
    jacobian = [[(probes[f"difference+/{j}"]["Residuals"][i] - probes[f"difference-/{j}"]["Residuals"][i]) / 2e-6
                 for j in range(24)] for i in range(75)]
    zero_normal = normal(jacobian, current["Residuals"], 0.0)
    damping = 1e-3
    for event in accepted[1:]:
        require(event["Purpose"].startswith("trial/0/"), "Earlier damping history needs a different replay")
        damping = max(1e-12, damping / 10.0)
    full_trials = {int(event["Purpose"].split("/")[1]): event for event in trials if event["Purpose"].endswith("/0")}
    require(set(full_trials) == set(range(6)), "Missing damping attempt identity")
    actual_directions_identical = len({bits(event["Coordinates"]) for event in full_trials.values()}) == 1
    matrix_rows, matrix_fingerprints = [], []
    replay_direction = None
    for attempt in range(6):
        matrix = normal(jacobian, current["Residuals"], damping)
        matrix_fingerprints.append(digest(bits([v for row in matrix for v in row])))
        direction = solve(matrix)
        full = [x + delta for x, delta in zip(current["Coordinates"], direction)]
        error = max(abs(a - b) for a, b in zip(full, full_trials[attempt]["Coordinates"]))
        require(error <= 1e-12, "Replay direction disagrees with recorded direction")
        changed = sum(struct.pack(">d", matrix[i][i]) != struct.pack(">d", zero_normal[i][i]) for i in range(24))
        matrix_rows.append({"attempt": attempt, "damping": format(damping, ".17g"),
                            "effective_diagonal_changes_vs_zero_damping": changed,
                            "normal_matrix_sha256": matrix_fingerprints[-1], "max_full_coordinate_replay_error": error})
        replay_direction = direction
        damping = min(1e12, damping * 10.0)
    fractions = []
    first_admissible = None
    for halving in range(21):
        alpha = math.ldexp(1.0, -halving)
        coordinates = [x + alpha * d for x, d in zip(current["Coordinates"], replay_direction)]
        violating = [j for j, x in enumerate(coordinates) if not math.isfinite(x) or abs(x) > 0.02]
        predicted = []
        for row, residual in zip(jacobian, current["Residuals"]):
            change = 0.0
            for value, delta in zip(row, replay_direction):
                change += value * delta
            predicted.append(residual + alpha * change)
        fractions.append({"halving": halving, "alpha": alpha, "inside_frozen_box": not violating,
                          "violating_coordinates": "|".join(map(str, violating)),
                          "suction_energy_coordinate": coordinates[21],
                          "linear_model_predicted_maximum_only": max(abs(r) for r in predicted),
                          "plant_evaluated": False})
        if not violating and first_admissible is None:
            first_admissible = halving
    baseline_phases = current["Phases"]
    probe_phase_changes = sum(event["Phases"] != baseline_phases for event in probes.values())
    phase_changes = [{"probe": purpose, "changes": [
        {"checkpoint": ["raw", "seed-step1", "post-seed", "1"][i // 12],
         "node": plan["search"]["variable_order"][i % 12], "before": before, "after": after}
        for i, (before, after) in enumerate(zip(baseline_phases, event["Phases"])) if before != after]}
        for purpose, event in probes.items() if event["Phases"] != baseline_phases]
    column_norms = [math.sqrt(sum(row[j] * row[j] for row in jacobian)) for j in range(24)]
    write_json(output, names[0], {"status": "PASS-FROZEN-PROVENANCE", "baseline": baseline,
                                 "contract_sha256": digest(contract_path.read_bytes()),
                                 "trace_sha256": digest(payloads["04-search-trace.jsonl"]),
                                 "plant_evaluations": 0, "ordinary_suite_rerun": False,
                                 "inherited_regression": json.loads(payloads["11-regression-summary.json"])})
    write_json(output, names[1], {"status": "PASS-OFFLINE-TERMINAL-REPLAY", "starting_event": 401,
                                 "terminal_iteration": 8, "valid_derivative_probes": 48,
                                 "out_of_bounds_trials": 78, "plant_evaluated_terminal_trials": 0,
                                 "bound_coordinate": 21, "bound_node": "suction", "bound_quantity": "energy",
                                 "current_coordinate": current["Coordinates"][21],
                                 "remaining_lower_bound_distance": 0.02 + current["Coordinates"][21],
                                 "six_recorded_full_directions_bit_identical": actual_directions_identical,
                                 "six_replayed_matrices_bit_identical": len(set(matrix_fingerprints)) == 1,
                                 "effective_diagonal_change_counts": [row["effective_diagonal_changes_vs_zero_damping"] for row in matrix_rows],
                                 "phase_changing_derivative_probes": probe_phase_changes,
                                 "phase_change_details": phase_changes,
                                 "minimum_jacobian_column_norm": min(column_norms), "maximum_jacobian_column_norm": max(column_norms),
                                 "first_box_admissible_halving_on_frozen_direction": first_admissible,
                                 "nonlinear_improvement_at_that_halving_proven": False})
    write_csv(output, names[2], matrix_rows)
    write_csv(output, names[3], fractions)
    write_json(output, names[4], {"status": "PASS-REVIEW-EVIDENCE-COMPLETE",
                                 "classification": "TERMINAL-SEARCH-DIRECTION-BLOCKED-BY-FROZEN-BOUND",
                                 "damping_effect_lost_in_reconstructed_normal_matrix": all(row["effective_diagonal_changes_vs_zero_damping"] == 0 for row in matrix_rows),
                                 "physical_infeasibility_proven": False, "new_seed_constructed": False,
                                 "authority": contract["authority"]})
    write_csv(output, names[5], [{"file": name, "sha256": digest((output / name).read_bytes())} for name in names[:-1]])
    print("H numerical stop review: PASS. Offline replay only; next: bounded scaled local-model design. R3 remains RED.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--contract", type=Path, default=ROOT / "eng/r3-h-numerical-stop-review1-contract.json")
    parser.add_argument("--output", type=Path, default=ROOT / "artifacts/r3-h-numerics1")
    args = parser.parse_args()
    review(args.contract.resolve(), args.output.resolve())
