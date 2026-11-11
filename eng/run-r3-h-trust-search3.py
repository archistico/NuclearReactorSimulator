"""H3 bounded trust-box search, explicit plant batches and offline adjudication."""
from __future__ import annotations

import argparse
import copy
import gzip
import json
import math
import os
import runpy
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
H2 = runpy.run_path(str(ROOT / "eng/run-r3-h-scaled-search2.py"))
require, digest, load = H2["require"], H2["digest"], H2["load"]
OUT = ROOT / "artifacts/r3-h-trust-search3"
FROZEN = ROOT / "eng/frozen-evidence/r3-h-trust-search3"
CONTRACT = ROOT / "eng/r3-h-trust-search3-contract.json"
NEW_TEST = "tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/M10R3HTrustSearch3Tests.cs"
POLICY = dict(variables=24, residuals=75, fractional_bound=.02, derivative_maximum=1e-6,
              derivative_radius_fraction=.1, radius_initial=1e-4, radius_minimum=1e-10,
              radius_maximum=1e-3, damping=[.001, .01, .1, 1, 10, 100], maximum_halvings=20,
              fraction_safety=.99, maximum_recipes=8192, maximum_iterations=64,
              maximum_batches=257, maximum_physical_steps=25090, maximum_batch_recipes=48,
              accept_improving_probes=True, phase_compatible_stencils=True,
              stagnation_relative=1e-10, stagnation_updates=3, root_threshold=1,
              production_change=False, bound_expansion=False, residual_reweighting=False,
              seed_installation=False, r3_passed=False)


def write(name, value):
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / name).write_text(json.dumps(value, indent=2, allow_nan=False) + "\n", encoding="utf-8")


def validate_policy(c):
    require(c["schema"] == "r3-h-trust-search3-v1" and c["policy"] == POLICY, "H3 policy drift")
    require(c["test_additions"] == [NEW_TEST] and c["output"] == "artifacts/r3-h-trust-search3", "H3 surface drift")


def authenticate():
    c = load(CONTRACT)
    validate_policy(c)
    for directory in ("src", "tests"):
        files = sorted((p for p in (ROOT / directory).rglob("*") if p.is_file()
                        and not {"bin", "obj"}.intersection(p.relative_to(ROOT).parts)
                        and p.relative_to(ROOT).as_posix() != NEW_TEST), key=lambda p: p.relative_to(ROOT).as_posix())
        rows = "".join(f"{p.relative_to(ROOT).as_posix()}|{digest(p.read_bytes())}\n" for p in files)
        require(dict(file_count=len(files), tree_sha256=digest(rows.encode())) == c["incoming_baseline"][directory], "Incoming tree drift: " + directory)
    for entry in c["implementation_files"] + c["frozen_inputs"]:
        require(digest((ROOT / entry["path"]).read_bytes()) == entry["sha256"], "Input drift: " + entry["path"])
    parent = ROOT / "eng/frozen-evidence/r3-h-scaled-search2"
    for entry in load(parent / "manifest.json")["files"]:
        stored = (parent / entry["stored"]).read_bytes()
        require(digest(stored) == entry["stored_sha256"], "H2 stored payload drift")
        data = gzip.decompress(stored) if entry["stored"].endswith(".gz") else stored
        require(len(data) == entry["bytes"] and digest(data) == entry["sha256"], "H2 logical payload drift")
    return c


def start_record():
    data = gzip.decompress((ROOT / "eng/frozen-evidence/r3-h-scaled-search2/03-batches.jsonl.gz").read_bytes()).decode()
    records = [r for line in data.splitlines() for r in json.loads(line)["records"]]
    best = min(records, key=H2["score"])
    require(best["Id"] == "i1/probe/6/1", "Frozen start selection drift")
    return best


def local_direction(jacobian, residual, x, damping, radius):
    scales = [math.sqrt(sum(row[j] * row[j] for row in jacobian)) or 1. for j in range(len(x))]
    reduced = [[a / s for a, s in zip(row, scales)] for row in jacobian]
    matrix = H2["LIB"]["normal"](reduced, residual, damping)
    lower = [max(-.02 - v, -radius) * s for v, s in zip(x, scales)]
    upper = [min(.02 - v, radius) * s for v, s in zip(x, scales)]
    z, diagnostic = H2["bounded_quadratic"]([r[:-1] for r in matrix], [r[-1] for r in matrix], lower, upper)
    delta = [.99 * v / s for v, s in zip(z, scales)]
    require(all(abs(d) <= radius * (1 + 1e-12) and abs(v + d) <= .02 for v, d in zip(x, delta)), "Trust step escaped bounds")
    return delta, diagnostic


def compatible_column(current, samples, h):
    same = [(sign, sample) for sign, sample in samples if sample["Reason"] == "VALID" and sample["Phases"] == current["Phases"]]
    if len(same) == 2:
        plus = next(s for sign, s in same if sign == 1)
        minus = next(s for sign, s in same if sign == -1)
        return [(a - b) / (2 * h) for a, b in zip(plus["Residuals"], minus["Residuals"])], "central"
    if len(same) == 1:
        sign, sample = same[0]
        return [(a - b) / (sign * h) for a, b in zip(sample["Residuals"], current["Residuals"])], f"one-sided/{sign}"
    return None, "unavailable-phase-or-domain"


def search(evaluate, initial=None, limits=None):
    p = POLICY if limits is None else limits
    start = start_record() if initial is None else initial
    current = evaluate([dict(Id="start", Coordinates=start["Coordinates"])])[0]
    require(current["Reason"] == "VALID" and current["Residuals"] == start["Residuals"] and current["Phases"] == start["Phases"], "Starting state mismatch")
    radius, evaluations, stagnant = p["radius_initial"], 1, 0
    accepted, iterations = [], []
    stop = "ITERATION-LIMIT"
    for iteration in range(p["maximum_iterations"]):
        if H2["score"](current) <= 1:
            stop = "ROOT-OBSERVED-UNCONFIRMED"
            break
        h = min(1e-6, .1 * radius)
        requests, indices = [], []
        for j, value in enumerate(current["Coordinates"]):
            signs = [1, -1] if abs(value) + h <= .02 else [-1 if value > 0 else 1]
            column = []
            for sign in signs:
                point = current["Coordinates"][:]
                point[j] += sign * h
                column.append((sign, len(requests)))
                requests.append(dict(Id=f"i{iteration}/probe/{j}/{sign}", Coordinates=point))
            indices.append(column)
        if evaluations + len(requests) > p["maximum_recipes"]:
            stop = "RECIPE-BUDGET"
            break
        probes = evaluate(requests)
        evaluations += len(probes)
        columns, stencils = [], []
        for column in indices:
            derivative, stencil = compatible_column(current, [(sign, probes[index]) for sign, index in column], h)
            columns.append(derivative)
            stencils.append(stencil)
        trial_requests, predictions, model_diagnostics = [], {}, []
        if all(column is not None for column in columns):
            jacobian = [list(row) for row in zip(*columns)]
            for damping in p["damping"]:
                try:
                    delta, diagnostic = local_direction(jacobian, current["Residuals"], current["Coordinates"], damping, radius)
                except ValueError as ex:
                    model_diagnostics.append(dict(damping=damping, rejected=str(ex)))
                    continue
                model_diagnostics.append(dict(damping=damping, **diagnostic))
                for halving in range(p["maximum_halvings"] + 1):
                    fraction = 2. ** -halving
                    point = [v + fraction * d for v, d in zip(current["Coordinates"], delta)]
                    identifier = f"i{iteration}/trial/{damping:g}/{halving}"
                    predicted = H2["MATH"]["predict"](jacobian, current["Residuals"], delta, fraction)
                    predictions[identifier] = max(map(abs, predicted))
                    trial_requests.append(dict(Id=identifier, Coordinates=point))
        trial_requests = trial_requests[:p["maximum_recipes"] - evaluations]
        trials = evaluate(trial_requests) if trial_requests else []
        evaluations += len(trials)
        best = min(probes + trials, key=H2["score"])
        before, old_radius = H2["score"](current), radius
        item = dict(iteration=iteration, radius=radius, derivative_step=h, stencils=stencils,
                    phase_changing_probes=[r["Id"] for r in probes if r.get("Phases") != current["Phases"]], models=model_diagnostics,
                    best_id=best["Id"], best_maximum=H2["score"](best), accepted=H2["score"](best) < before)
        if item["accepted"]:
            actual_reduction = before - H2["score"](best)
            relative = actual_reduction / before
            stagnant = stagnant + 1 if relative <= 1e-10 else 0
            if best["Id"] not in predictions:
                radius *= .5
                item["reduction_ratio"] = None
            else:
                predicted_reduction = before - predictions[best["Id"]]
                ratio = actual_reduction / predicted_reduction if predicted_reduction > 0 else None
                item["reduction_ratio"] = ratio
                norm = max(abs(a - b) for a, b in zip(current["Coordinates"], best["Coordinates"]))
                if ratio is None or ratio < .1:
                    radius *= .25
                elif ratio > .75 and norm >= .8 * radius:
                    radius = min(p["radius_maximum"], 2 * radius)
            current = best
            accepted.append(dict(iteration=iteration, id=current["Id"], maximum=H2["score"](current), relative_reduction=relative, coordinates=current["Coordinates"]))
        else:
            radius *= .25
        item["next_radius"] = max(p["radius_minimum"], radius)
        iterations.append(item)
        if not item["accepted"] and old_radius <= p["radius_minimum"]:
            stop = "MINIMUM-RADIUS-NO-DESCENT"
            break
        radius = max(p["radius_minimum"], radius)
        if stagnant >= p["stagnation_updates"]:
            stop = "STAGNATION"
            break
        if evaluations >= p["maximum_recipes"]:
            stop = "RECIPE-BUDGET"
            break
    if H2["score"](current) <= 1:
        stop = "ROOT-OBSERVED-UNCONFIRMED"
    return dict(status="PASS-EVIDENCE-COMPLETE", stop=stop, evaluations=evaluations, accepted=accepted,
                iterations=iterations, final=current, root_observed=H2["score"](current) <= 1,
                r3_passed=False, m10_closed=False, seed_installed=False)


def fixtures():
    checks = H2["fixtures"]()
    delta, _ = local_direction([[1., 0.], [0., 1.]], [10., -10.], [.019999, 0.], .001, 1e-5)
    require(max(map(abs, delta)) <= 1e-5 and all(abs(x + d) <= .02 for x, d in zip([.019999, 0.], delta)), "Local bound fixture")
    checks.append(dict(name="local-step-with-original-box", passed=True))
    current = dict(Residuals=[1.], Phases=["liquid"])
    samples = [(1, dict(Reason="VALID", Residuals=[1000.], Phases=["vapor"])),
               (-1, dict(Reason="VALID", Residuals=[.5], Phases=["liquid"]))]
    column, kind = compatible_column(current, samples, .1)
    require(column == [5.] and kind == "one-sided/-1", "Phase-compatible one-sided fixture")
    require(compatible_column(current, samples[:1], .1)[0] is None, "Invented phase derivative")
    checks.append(dict(name="one-sided-and-unavailable-at-seam", passed=True))
    def objective(requests):
        records = []
        for r in requests:
            x = r["Coordinates"][0]
            residual = 1e8 * (x - 5e-5) if x <= 1e-4 else 5000. + 1e12 * (x - 1e-4)
            records.append(dict(**r, Reason="VALID", Residuals=[residual] + [0.] * 74, Phases=["liquid" if x <= 1e-4 else "compressed"] * 48))
        return records
    coordinates = [1.1e-4] + [0.] * 23
    initial = objective([dict(Id="fixture", Coordinates=coordinates)])[0]
    result = search(objective, initial)
    require(result["root_observed"], "Known kinked objective did not converge")
    checks.append(dict(name="known-root-across-slope-change", passed=True, evaluations=result["evaluations"]))
    limited = dict(POLICY, maximum_recipes=50)
    result = search(objective, initial, limited)
    require(result["evaluations"] <= 50 and result["stop"] == "RECIPE-BUDGET", "Budget fixture")
    checks.append(dict(name="budget-truncates-fixed-trial-prefix", passed=True, evaluations=result["evaluations"]))
    return checks


def static():
    c = authenticate()
    negative = []
    for key, value in [("maximum_recipes", 8193), ("fractional_bound", .03), ("seed_installation", True)]:
        changed = copy.deepcopy(c)
        changed["policy"][key] = value
        try:
            validate_policy(changed)
        except ValueError:
            negative.append(dict(case=key, rejected=True))
        else:
            raise ValueError("Negative policy fixture accepted: " + key)
    write("01-static.json", dict(status="PASS-STATIC", contract_sha256=digest(CONTRACT.read_bytes()), fixtures=fixtures(), negative_policy_cases=negative))


def preexecution():
    authenticate()
    H2["test_report"](OUT / "preexecution.xml", 2, 1)
    require(load(OUT / "01-static.json")["contract_sha256"] == digest(CONTRACT.read_bytes()), "Stale static review")
    write("02-preexecution.json", dict(status="PASS-PREEXECUTION", contract_sha256=digest(CONTRACT.read_bytes()), assembly_sha256=digest(H2["ASSEMBLY"].read_bytes())))


def execute():
    authenticate()
    review = load(OUT / "02-preexecution.json")
    require(review["status"] == "PASS-PREEXECUTION" and review["contract_sha256"] == digest(CONTRACT.read_bytes())
            and review["assembly_sha256"] == digest(H2["ASSEMBLY"].read_bytes()), "Stale execution identity")
    trace = OUT / "03-batches.jsonl"
    require(not trace.exists(), "Existing execution retained; never overwrite")
    batches, recipes = 0, 0
    def evaluate(requests):
        nonlocal batches, recipes
        results = []
        for offset in range(0, len(requests), 48):
            chunk = requests[offset:offset + 48]
            require(batches < 257 and recipes + len(chunk) <= 8192, "Physical budget exceeded")
            write("batch-request.json", chunk)
            write("batch-token.json", dict(status="PASS-BATCH-REVIEW", contract_sha256=review["contract_sha256"], assembly_sha256=review["assembly_sha256"], request_sha256=digest((OUT / "batch-request.json").read_bytes())))
            command = ["dotnet", "test", "--project", H2["PROJECT"], "--configuration", "Release", "--no-build", "--results-directory", str(OUT), "--minimum-expected-tests", "1", "--", "--explicit", "only", "--filter-method", "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10R3HTrustSearch3Tests.ExecuteReviewedBatch", "--parallel", "none", "--report-xunit", "--report-xunit-filename", "batch-execution.xml"]
            with (OUT / "execution.log").open("a", encoding="utf-8") as log:
                subprocess.run(command, cwd=ROOT, env=dict(os.environ, NRS_R3_H_TRUST_SEARCH3="1"), stdout=log, stderr=subprocess.STDOUT, check=True)
            H2["test_report"](OUT / "batch-execution.xml", 1, 0)
            records = [json.loads(line) for line in (OUT / "batch-results.jsonl").read_text(encoding="utf-8-sig").splitlines()]
            require(len(records) == len(chunk) and all(a["Id"] == b["Id"] and a["Coordinates"] == b["Coordinates"] for a, b in zip(chunk, records)), "Batch mismatch")
            batches += 1
            recipes += len(chunk)
            with trace.open("a", encoding="utf-8") as stream:
                stream.write(json.dumps(dict(batch=batches, requests=chunk, records=records), allow_nan=False) + "\n")
            print(f"batch={batches} recipes={recipes} first={chunk[0]['Id']} best={min(map(H2['score'], records)):.9g}", flush=True)
            results.extend(records)
        return results
    result = search(evaluate)
    result.update(batches=batches, maximum_physical_steps=3 * recipes + 2 * batches)
    write("04-result.json", result)


def audit(batches, expected):
    cursor = 0
    plan = load(ROOT / "eng/r3-equilibrium-reconstruction-planning1-contract.json")
    def replay(requests):
        nonlocal cursor
        records = []
        for offset in range(0, len(requests), 48):
            require(cursor < len(batches), "Incomplete physical trace")
            batch = batches[cursor]
            cursor += 1
            chunk = requests[offset:offset + 48]
            require(batch["batch"] == cursor and batch["requests"] == chunk and len(batch["records"]) == len(chunk), "H3 decision replay mismatch")
            for request, record in zip(chunk, batch["records"]):
                require(request["Id"] == record["Id"] and request["Coordinates"] == record["Coordinates"], "Physical trace identity mismatch")
                H2["verify_physics"](record, plan)
            records.extend(batch["records"])
        return records
    result = search(replay)
    result.update(batches=cursor, maximum_physical_steps=3 * result["evaluations"] + 2 * cursor)
    require(cursor == len(batches) and result == expected, "H3 final replay mismatch")
    final = result["final"]
    nodes = plan["search"]["variable_order"]
    ranked = sorted(enumerate(final["Residuals"]), key=lambda p: -abs(p[1]))[:10]
    return dict(status="PASS-INDEPENDENT-REPLAY", stop=result["stop"], recipes=result["evaluations"], batches=cursor,
                maximum_physical_steps=result["maximum_physical_steps"], accepted_updates=len(result["accepted"]),
                initial_maximum=H2["score"](start_record()), final_maximum=H2["score"](final),
                all_in_envelope=all(o["InEnvelope"] for o in final["Observations"]),
                dominant_residuals=[dict(checkpoint=["seed-step1", "post-seed", "1"][i // 25], node=nodes[(i % 25) // 2] if i % 25 < 24 else "rotor", quantity="torque" if i % 25 == 24 else "mass" if i % 25 % 2 == 0 else "energy", scaled_residual=v) for i, v in ranked],
                root_observed=result["root_observed"], r3_passed=False, m10_closed=False, seed_installed=False)


def adjudicate():
    authenticate()
    batches = [json.loads(line) for line in (OUT / "03-batches.jsonl").read_text().splitlines()]
    result = audit(batches, load(OUT / "04-result.json"))
    write("05-adjudication.json", result)
    print(json.dumps(result, indent=2))


def review():
    authenticate()
    receipt = load(ROOT / "eng/r3-h-trust-search3-returned.json")
    require(receipt["contract_sha256"] == digest(CONTRACT.read_bytes()) and receipt["manifest_sha256"] == digest((FROZEN / "manifest.json").read_bytes()), "Returned identity drift")
    payloads = {}
    for entry in load(FROZEN / "manifest.json")["files"]:
        stored = (FROZEN / entry["stored"]).read_bytes()
        require(digest(stored) == entry["stored_sha256"], "Frozen stored evidence drift")
        data = gzip.decompress(stored) if entry["stored"].endswith(".gz") else stored
        require(len(data) == entry["bytes"] and digest(data) == entry["sha256"], "Frozen logical evidence drift")
        payloads[entry["file"]] = data
    batches = [json.loads(line) for line in payloads["03-batches.jsonl"].decode().splitlines()]
    result = audit(batches, json.loads(payloads["04-result.json"]))
    require(result == json.loads(payloads["05-adjudication.json"]), "Returned adjudication mismatch")
    write("06-frozen-review.json", dict(status="PASS-FROZEN-H3-REVIEW", plant_calls=0, result=result))
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage", choices=["fixtures", "static", "preexecution", "execute", "adjudicate", "review"])
    args = parser.parse_args()
    {"fixtures": lambda: print(json.dumps(fixtures(), indent=2)), "static": static, "preexecution": preexecution, "execute": execute, "adjudicate": adjudicate, "review": review}[args.stage]()
