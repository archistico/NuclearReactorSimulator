"""Replay H3 and compare exact one-dimensional minimax models, with no plant calls."""
from __future__ import annotations

import gzip
import json
import math
import runpy
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
H3 = runpy.run_path(str(ROOT / "eng/run-r3-h-trust-search3.py"))
require, digest, load = H3["require"], H3["digest"], H3["load"]
OUT = ROOT / "artifacts/r3-h-minimax-direction-review1"
CONTRACT = ROOT / "eng/r3-h-minimax-direction-review1-contract.json"
POLICY = dict(iterations=[8, 47], radii=[1e-6, 1e-4], maximum_bisections=80,
              fractional_bound=.02, fraction_safety=.99, plant_calls=0,
              nonlinear_search=False, seed_installation=False, r3_passed=False)


def minimize_line(residuals, changes):
    require(len(residuals) == len(changes) > 0 and all(math.isfinite(x) for x in residuals + changes), "Invalid line model")
    def observe(alpha):
        values = [r + alpha * d for r, d in zip(residuals, changes)]
        maximum = max(map(abs, values))
        slopes = [(d if v >= 0 else -d) for v, d in zip(values, changes) if abs(v) == maximum]
        # At zero every absolute-value branch has both one-sided slopes.
        if maximum == 0:
            slopes = [v for d in changes for v in (-abs(d), abs(d))]
        return maximum, min(slopes), max(slopes)
    low, high = 0., 1.
    best = min((observe(a)[0], a) for a in (0., 1.))
    for _ in range(80):
        mid = (low + high) / 2
        if mid == low or mid == high:
            break
        score, left, right = observe(mid)
        best = min(best, (score, mid))
        if left <= 0 <= right:
            if left < 0:
                break
            high = mid  # Find the first minimizer when the optimum contains a plateau.
            continue
        if right < 0:
            low = mid
        else:
            high = mid
    return best[1], best[0]


def fixtures():
    results = []
    for name, residual, change, alpha, score in [
        ("opposite-to-least-squares", [10., 9., 9.], [-1., 1., 1.], .5, 9.5),
        ("known-root", [2., -4.], [-2., 4.], 1., 0.),
        ("minimum-at-start", [2., 1.], [1., 1.], 0., 2.),
        ("minimum-at-end", [10.], [-1.], 1., 9.),
        ("zero-direction", [3., -2.], [0., 0.], 0., 3.),
        ("first-minimizer-on-plateau", [2., 3.], [0., -4.], .25, 2.),
        ("late-plateau", [2., 3.5], [0., -2.], .75, 2.),
    ]:
        actual_alpha, actual_score = minimize_line(residual, change)
        require(abs(actual_alpha - alpha) < 1e-12 and abs(actual_score - score) < 1e-12, "Minimax line fixture: " + name)
        results.append(dict(name=name, passed=True))
    try:
        minimize_line([1.], [math.nan])
    except ValueError:
        results.append(dict(name="nonfinite-rejected", passed=True))
    else:
        raise ValueError("Nonfinite line accepted")
    return results


def review():
    c = load(CONTRACT)
    require(c["schema"] == "r3-h-minimax-direction-review1-v1" and c["policy"] == POLICY, "Offline review policy drift")
    for entry in c["frozen_inputs"]:
        require(digest((ROOT / entry["path"]).read_bytes()) == entry["sha256"], "Offline input drift: " + entry["path"])
    H3["review"]()
    frozen = H3["FROZEN"]
    trace = gzip.decompress((frozen / "03-batches.jsonl.gz").read_bytes()).decode()
    records = [r for line in trace.splitlines() for r in json.loads(line)["records"]]
    by_id = {r["Id"]: r for r in records}
    result = load(frozen / "04-result.json")
    comparisons = []
    terminal = load(frozen / "07-terminal-review.json")
    for iteration in POLICY["iterations"]:
        detail = result["iterations"][iteration]
        require(detail["iteration"] == iteration, "Iteration identity drift")
        accepted = [a for a in result["accepted"] if a["iteration"] < iteration]
        current = by_id[accepted[-1]["id"]] if accepted else by_id["start"]
        columns = []
        for j in range(24):
            samples = [(int(r["Id"].split("/")[-1]), r) for r in records if r["Id"].startswith(f"i{iteration}/probe/{j}/")]
            column, _ = H3["compatible_column"](current, samples, detail["derivative_step"])
            columns.append(column)
        require(all(column is not None for column in columns), "Unavailable local derivative; no fictitious model allowed")
        jacobian = [list(row) for row in zip(*columns)]
        if iteration == terminal["last_iteration"]:
            require(terminal["current_maximum"] == H3["H2"]["score"](current), "Terminal objective mismatch")
            for recorded in terminal["models"]:
                delta, diagnostic = H3["local_direction"](jacobian, current["Residuals"], current["Coordinates"], recorded["damping"], detail["radius"])
                predicted = H3["H2"]["MATH"]["predict"](jacobian, current["Residuals"], delta, 1.)
                require(recorded["full_predicted_maximum"] == max(map(abs, predicted))
                        and recorded["full_predicted_squares"] == sum(v * v for v in predicted), "Terminal least-squares comparison mismatch")
                require(recorded["full_predicted_maximum"] > terminal["current_maximum"]
                        and recorded["full_predicted_squares"] < terminal["current_squares"], "Objective conflict not reproduced")
        for radius in POLICY["radii"]:
            directions = []
            for j, x in enumerate(current["Coordinates"]):
                for sign in (1, -1):
                    length = min(radius, .02 - x if sign > 0 else x + .02)
                    if length > 0:
                        delta = [0.] * 24
                        delta[j] = sign * .99 * length
                        directions.append((f"axis/{j}/{sign}", delta))
            for damping in H3["POLICY"]["damping"]:
                try:
                    delta, _ = H3["local_direction"](jacobian, current["Residuals"], current["Coordinates"], damping, radius)
                except ValueError:
                    continue
                directions.append((f"least-squares/{damping:g}", delta))
            candidates = []
            for identifier, delta in directions:
                changes = [sum(a * d for a, d in zip(row, delta)) for row in jacobian]
                alpha, score = minimize_line(current["Residuals"], changes)
                point = [x + alpha * d for x, d in zip(current["Coordinates"], delta)]
                require(all(abs(x) <= .02 and abs(x - previous) <= radius * (1 + 1e-12) for x, previous in zip(point, current["Coordinates"])), "Offline candidate escaped bounds")
                predicted = [r + alpha * d for r, d in zip(current["Residuals"], changes)]
                require(max(map(abs, predicted)) == score, "Line prediction mismatch")
                candidates.append(dict(id=identifier, alpha=alpha, coordinates=point,
                                       linear_predicted_residuals_only=predicted, predicted_maximum_only=score, plant_evaluated=False))
            best = min(range(len(candidates)), key=lambda i: candidates[i]["predicted_maximum_only"])
            comparisons.append(dict(iteration=iteration, radius=radius, derivative_step=detail["derivative_step"],
                                    current_maximum=H3["H2"]["score"](current), candidates=candidates,
                                    best_linear_direction_only=candidates[best]["id"], best_linear_maximum_only=candidates[best]["predicted_maximum_only"]))
    OUT.mkdir(parents=True, exist_ok=True)
    evidence = dict(status="PASS-OFFLINE-MINIMAX-DIRECTION-REVIEW", contract_sha256=digest(CONTRACT.read_bytes()),
                    fixtures=fixtures(), comparisons=comparisons, plant_calls=0, physical_candidate_selected=False, r3_passed=False)
    (OUT / "01-comparison.json").write_text(json.dumps(evidence, indent=2) + "\n", encoding="utf-8")
    summary = dict(status=evidence["status"], fixtures_passed=len(evidence["fixtures"]), plant_calls=0,
                   comparisons=[{k: row[k] for k in ("iteration", "radius", "derivative_step", "current_maximum", "best_linear_direction_only", "best_linear_maximum_only")} for row in comparisons],
                   physical_candidate_selected=False, r3_passed=False)
    (OUT / "02-summary.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
    returned = ROOT / "eng/r3-h-minimax-direction-review1-returned.json"
    if returned.exists():
        receipt = load(returned)
        folder = ROOT / "eng/frozen-evidence/r3-h-minimax-direction-review1"
        require(receipt["contract_sha256"] == digest(CONTRACT.read_bytes())
                and receipt["manifest_sha256"] == digest((folder / "manifest.json").read_bytes()), "Returned minimax identity drift")
        for entry in load(folder / "manifest.json")["files"]:
            stored = (folder / entry["stored"]).read_bytes()
            require(digest(stored) == entry["stored_sha256"], "Minimax stored payload drift")
            data = gzip.decompress(stored) if entry["stored"].endswith(".gz") else stored
            require(digest(data) == entry["sha256"] and len(data) == entry["bytes"], "Minimax logical payload drift")
            require(data == (OUT / entry["file"]).read_bytes(), "Returned minimax calculation mismatch")
    print(json.dumps(summary, indent=2))


if __name__ == "__main__":
    review()
