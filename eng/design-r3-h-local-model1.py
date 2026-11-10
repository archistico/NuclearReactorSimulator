"""Bounded scaled linear-model comparison using frozen H data, never plant calls."""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import runpy
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
LIB = runpy.run_path(str(ROOT / "eng/review-r3-h-numerics1.py"))
require = LIB["require"]
digest = LIB["digest"]


def direction(jacobian, residual, damping):
    scales = [math.sqrt(sum(row[j] * row[j] for row in jacobian)) for j in range(len(jacobian[0]))]
    active = [j for j, scale in enumerate(scales) if scale > 0]
    delta = [0.0] * len(scales)
    if active:
        reduced = [[row[j] / scales[j] for j in active] for row in jacobian]
        z = LIB["solve"](LIB["normal"](reduced, residual, damping))
        for j, value in zip(active, z):
            delta[j] = value / scales[j]
    require(all(math.isfinite(value) for value in delta), "Nonfinite local-model direction")
    return delta, scales, active


def fraction_to_box(coordinates, delta):
    fractions = [(0.02 - x) / d if d > 0 else (-0.02 - x) / d
                 for x, d in zip(coordinates, delta) if d != 0]
    maximum = min(fractions) if fractions else 1.0
    if not math.isfinite(maximum) or maximum <= 0:
        return maximum, None, None
    alpha = min(1.0, 0.99 * maximum)
    point = [x + alpha * d for x, d in zip(coordinates, delta)]
    require(all(math.isfinite(x) and abs(x) <= 0.02 for x in point), "Capped point escaped original box")
    return maximum, alpha, point


def predict(jacobian, residual, delta, alpha):
    return [r + alpha * sum(value * step for value, step in zip(row, delta))
            for row, r in zip(jacobian, residual)]


def fixtures():
    results = []
    for name, matrix, target in [
        ("coupled-known-linear-root", [[3., 1.], [1., 2.]], [.005, .003]),
        ("zero-column-keeps-zero-delta", [[1., 0.], [2., 0.]], [.005, 0.]),
        ("badly-scaled-known-linear-root", [[1e12, 1.], [1e12, -1.]], [1e-12, .005]),
    ]:
        residual = [-sum(a * b for a, b in zip(row, target)) for row in matrix]
        delta, _, _ = direction(matrix, residual, 0.0)
        error = max(abs(a - b) for a, b in zip(delta, target))
        require(error <= 1e-12, "Coordinate-transform fixture failed: " + name)
        results.append({"fixture": name, "passed": True, "maximum_coordinate_error": error})
    x, delta = [-.019999, .01], [-.01, .005]
    limit, alpha, point = fraction_to_box(x, delta)
    require(point is not None and 0 < alpha < limit < 1 and point[0] > -.02, "Near-bound global-fraction fixture failed")
    require(abs((point[0] - x[0]) / delta[0] - (point[1] - x[1]) / delta[1]) < 1e-12, "Independent clipping detected")
    results.append({"fixture": "outward-near-bound-global-fraction", "passed": True, "alpha": alpha})
    require(fraction_to_box([-.02], [-1.])[1] is None, "Boundary-outward step should have no interior fraction")
    results.append({"fixture": "exact-bound-outward-direction-rejected", "passed": True})
    require(fraction_to_box([0., 0.], [0., 0.])[2] == [0., 0.], "Zero direction changed coordinates")
    results.append({"fixture": "zero-direction-does-not-move", "passed": True})
    return results


def design(contract_path, output):
    require(output.is_relative_to(ROOT / "artifacts"), "Design outputs must be under repository artifacts")
    output.mkdir(parents=True, exist_ok=True)
    names = ["01-provenance.json", "02-linear-fixtures.json", "03-column-scales.csv",
             "04-model-comparison.json", "05-design-summary.json", "06-manifest.csv"]
    for name in names:
        (output / name).unlink(missing_ok=True)
    contract = LIB["load"](contract_path)
    require(contract["schema"] == "r3-h-bounded-scaled-local-model-design1-v1", "Design schema drift")
    parent = LIB["load"](ROOT / "eng/r3-h-numerical-stop-review1-contract.json")
    require(contract["design"] == parent["next_design"], "Frozen offline design drift")
    for key in ("plant_evaluations", "nonlinear_search", "source_changes", "test_changes", "bounds_expansion",
                "residual_reweighting", "seed_installation", "controller_change", "r3_passed", "requalification3", "r4"):
        require(contract["authority"][key] is False, "Forbidden design authority: " + key)
    require(contract["authority"]["next_activity"] == "REVIEW-LOCAL-MODEL-EVIDENCE-BEFORE-NONLINEAR-PLANNING", "Next design authority drift")
    baseline = {directory: LIB["tree"](directory) for directory in ("src", "tests")}
    require(baseline == contract["baseline"] == parent["baseline"], "Design baseline drift")
    for entry in contract["frozen_inputs"]:
        require(digest((ROOT / entry["path"]).read_bytes()) == entry["sha256"], "Frozen design input drift: " + entry["path"])
    for name, markers in contract["document_markers"].items():
        lines = [line.strip() for line in (ROOT / name).read_text(encoding="utf-8-sig").splitlines()]
        for marker in markers:
            require(lines.count(marker) == 1, "Design marker drift: " + marker)
    # Re-authenticate/replay the prerequisite into a distinct owned directory, with zero plant calls.
    replay_output = output / "prerequisite-replay"
    LIB["review"]((ROOT / "eng/r3-h-numerical-stop-review1-contract.json").resolve(), replay_output)
    frozen = ROOT / "eng/frozen-evidence/r3-h-feasibility1"
    import gzip
    trace = [json.loads(line) for line in gzip.decompress((frozen / "04-search-trace.jsonl.gz").read_bytes()).decode().splitlines()]
    current = next(event for event in trace if event["Id"] == 401)
    probes = {event["Purpose"]: event for event in trace if event["Iteration"] == 8 and event["Purpose"].startswith("difference")}
    jacobian = [[(probes[f"difference+/{j}"]["Residuals"][i] - probes[f"difference-/{j}"]["Residuals"][i]) / 2e-6
                 for j in range(24)] for i in range(75)]
    residual = current["Residuals"]
    initial = max(abs(r) for r in residual)
    # The prerequisite proved that terminal damping leaves this matrix unchanged.
    legacy_delta = LIB["solve"](LIB["normal"](jacobian, residual, 0.0))
    recorded_full = next(event for event in trace if event["Id"] == 450)
    require(max(abs(x + d - actual) for x, d, actual in zip(current["Coordinates"], legacy_delta,
                recorded_full["Coordinates"])) <= 1e-12, "Legacy comparator replay drift")
    legacy_limit, legacy_alpha, legacy_point = fraction_to_box(current["Coordinates"], legacy_delta)
    legacy_predicted = predict(jacobian, residual, legacy_delta, legacy_alpha) if legacy_alpha is not None else None
    legacy_comparator = {"recorded_full_trial": 450, "delta": legacy_delta,
                         "recorded_full_coordinates": recorded_full["Coordinates"],
                         "historical_full_reason": recorded_full["Reason"],
                         "global_fraction_to_box": legacy_limit, "global_alpha": legacy_alpha,
                         "capped_coordinates": legacy_point,
                         "linear_predicted_maximum_only": max(abs(r) for r in legacy_predicted) if legacy_predicted is not None else None,
                         "plant_evaluated": False}
    results = []
    norms = None
    for damping in contract["design"]["damping_values"]:
        try:
            delta, norms, active = direction(jacobian, residual, damping)
        except ValueError as exc:
            results.append({"damping": damping, "status": "LINEAR-SOLVE-REJECTED", "reason": str(exc), "plant_evaluated": False})
            continue
        maximum, alpha, point = fraction_to_box(current["Coordinates"], delta)
        uncapped = [x + d for x, d in zip(current["Coordinates"], delta)]
        predicted = predict(jacobian, residual, delta, alpha) if alpha is not None else None
        score = max(abs(r) for r in predicted) if predicted is not None else None
        reduced = [[row[j] / norms[j] for j in active] for row in jacobian]
        damped = LIB["normal"](reduced, residual, damping)
        undamped = LIB["normal"](reduced, residual, 0.0)
        changes = sum(damped[i][i] != undamped[i][i] for i in range(len(active)))
        results.append({"damping": damping, "status": "PASS-LOCAL-LINEAR-COMPARISON", "delta": delta,
                        "uncapped_coordinates": uncapped, "uncapped_inside_box": all(abs(x) <= .02 for x in uncapped),
                        "fraction_to_box": maximum, "global_alpha": alpha, "capped_coordinates": point,
                        "initial_maximum": initial, "linear_predicted_residuals_only": predicted,
                        "linear_predicted_maximum_only": score, "strict_model_descent": score is not None and score < initial,
                        "effective_scaled_diagonal_changes": changes, "plant_evaluated": False})
    fixture_results = fixtures()
    require(norms is not None, "No scaled direction could be computed")
    phase_tags = LIB["load"](replay_output / "02-terminal-review.json")["phase_change_details"]
    passed = [row for row in results if row["status"] == "PASS-LOCAL-LINEAR-COMPARISON"]
    admissible = [row for row in passed if row["capped_coordinates"] is not None]
    descending = [row for row in admissible if row["strict_model_descent"]]
    LIB["write_json"](output, names[0], {"status": "PASS-DESIGN-PROVENANCE", "baseline": baseline,
                                        "contract_sha256": digest(contract_path.read_bytes()), "source_event": 401,
                                        "plant_evaluations": 0, "ordinary_suite_rerun": False})
    LIB["write_json"](output, names[1], fixture_results)
    LIB["write_csv"](output, names[2], [{"coordinate": i, "column_norm": value, "zero_column_delta_fixed": value == 0} for i, value in enumerate(norms)])
    LIB["write_json"](output, names[3], {"candidates": results, "derivative_phase_change_tags": phase_tags,
                                        "model_smoothness_proven": False, "historical_current_point_maximum": initial,
                                        "legacy_direction_comparator": legacy_comparator})
    LIB["write_json"](output, names[4], {"status": "PASS-OFFLINE-DESIGN-EVIDENCE", "linear_fixtures_passed": len(fixture_results),
                                        "damping_comparisons": len(results), "solved": len(passed), "box_admissible": len(admissible),
                                        "strict_model_descent": len(descending), "selected_physical_candidate": None,
                                        "nonlinear_improvement_proven": False, "authority": contract["authority"]})
    LIB["write_csv"](output, names[5], [{"file": name, "sha256": digest((output / name).read_bytes())} for name in names[:-1]])
    print("H bounded scaled local-model design: PASS. Linear arithmetic only; no nonlinear improvement or R3 PASS claimed.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--contract", type=Path, default=ROOT / "eng/r3-h-local-model-design1-contract.json")
    parser.add_argument("--output", type=Path, default=ROOT / "artifacts/r3-h-local-model1")
    arguments = parser.parse_args()
    design(arguments.contract.resolve(), arguments.output.resolve())
