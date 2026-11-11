"""Frozen six-direction nonlinear experiment: authenticate, review, adjudicate."""
from __future__ import annotations

import argparse
import copy
import csv
import gzip
import json
import math
import runpy
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
MATH = runpy.run_path(str(ROOT / "eng/design-r3-h-local-model1.py"))
LIB = MATH["LIB"]
require, digest, load = LIB["require"], LIB["digest"], LIB["load"]
OUT = ROOT / "artifacts/r3-h-local-trial1"
CONTRACT = ROOT / "eng/r3-h-local-trial1-contract.json"
ASSEMBLY = ROOT / "tests/NuclearReactorSimulator.Application.Tests/bin/Release/net10.0/NuclearReactorSimulator.Application.Tests.dll"
NEW_TEST = "tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/M10R3HLocalModelTrial1Tests.cs"
POLICY = dict(event=401, variables=24, residuals=75, fractional_bound=.02,
              damping=[.001, .01, .1, 1, 10, 100], repeats=2, recipes=14,
              setup_steps=2, maximum_steps=44, timestep_seconds=.01,
              minimum_reduction_ratio=.1, root_threshold=1,
              optimization=False, seed_installation=False, production_change=False,
              r3_passed=False, bound_expansion=False, residual_reweighting=False)


def write(name, value):
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / name).write_text(json.dumps(value, indent=2, allow_nan=False) + "\n", encoding="utf-8")


def incoming_tree(directory):
    files = sorted((p for p in (ROOT / directory).rglob("*") if p.is_file()
                   and not {"bin", "obj"}.intersection(p.relative_to(ROOT).parts)
                   and p.relative_to(ROOT).as_posix() != NEW_TEST), key=lambda p: p.relative_to(ROOT).as_posix())
    rows = "".join(f"{p.relative_to(ROOT).as_posix()}|{digest(p.read_bytes())}\n" for p in files)
    return dict(file_count=len(files), tree_sha256=digest(rows.encode()))


def validate_policy(contract):
    require(contract["schema"] == "r3-h-local-trial1-v1", "Schema drift")
    require(contract["policy"] == POLICY, "Frozen policy drift")
    require(contract["test_additions"] == [NEW_TEST], "Unreviewed test addition")
    require(contract["output"] == "artifacts/r3-h-local-trial1", "Output escape")


def authenticate():
    contract = load(CONTRACT)
    validate_policy(contract)
    for key in ("src", "tests"):
        require(incoming_tree(key) == contract["incoming_baseline"][key], "Incoming tree drift: " + key)
    for entry in contract["implementation_files"] + contract["frozen_inputs"]:
        require(digest((ROOT / entry["path"]).read_bytes()) == entry["sha256"], "Input drift: " + entry["path"])
    frozen = ROOT / "eng/frozen-evidence/r3-h-feasibility1"
    for entry in load(frozen / "13-payload-map.json")["files"]:
        data = (frozen / entry["stored_file"]).read_bytes()
        require(digest(data) == entry["stored_sha256"], "Stored payload drift")
        data = gzip.decompress(data) if entry["compression"] == "GZIP" else data
        require(len(data) == entry["logical_bytes"] and digest(data) == entry["logical_sha256"], "Logical payload drift")
    return contract


def inputs():
    trace = [json.loads(line) for line in gzip.decompress((ROOT / "eng/frozen-evidence/r3-h-feasibility1/04-search-trace.jsonl.gz").read_bytes()).decode().splitlines()]
    base = next(e for e in trace if e["Id"] == 401)
    probes = {e["Purpose"]: e for e in trace if e["Iteration"] == 8 and e["Purpose"].startswith("difference")}
    jacobian = [[(probes[f"difference+/{j}"]["Residuals"][i] - probes[f"difference-/{j}"]["Residuals"][i]) / 2e-6 for j in range(24)] for i in range(75)]
    comparison = load(ROOT / "eng/frozen-evidence/r3-h-local-model1/04-model-comparison.json")
    # Read the original schema explicitly rather than select a point by predicted score.
    rows = comparison["candidates"]
    require([r["damping"] for r in rows] == POLICY["damping"], "Frozen direction order drift")
    points = [dict(Id="baseline", Coordinates=base["Coordinates"], Predicted=base["Residuals"], Phases=base["Phases"])]
    for row in rows:
        delta, _, _ = MATH["direction"](jacobian, base["Residuals"], row["damping"])
        _, alpha, point = MATH["fraction_to_box"](base["Coordinates"], delta)
        predicted = MATH["predict"](jacobian, base["Residuals"], delta, alpha)
        require(point == row["capped_coordinates"] and predicted == row["linear_predicted_residuals_only"], "Local arithmetic replay drift")
        points.append(dict(Id=f'lambda-{row["damping"]:g}', Coordinates=point, Predicted=predicted, Phases=base["Phases"]))
    return dict(Points=points)


def static():
    contract = authenticate()
    derived = inputs()
    require(derived == load(ROOT / "eng/contracts/r3-h-local-trial1-inputs.json"), "Frozen trial points drift")
    write("01-inputs.json", derived)
    negative = []
    for name, mutate in [
        ("wider-box", lambda c: c["policy"].update(fractional_bound=.03)),
        ("seventh-direction", lambda c: c["policy"]["damping"].append(1000)),
        ("extra-recipe", lambda c: c["policy"].update(recipes=15)),
        ("seed-installation", lambda c: c["policy"].update(seed_installation=True)),
        ("output-escape", lambda c: c.update(output="../outside")),
        ("unreviewed-tests", lambda c: c["test_additions"].append("tests/extra.cs")),
    ]:
        changed = copy.deepcopy(contract)
        mutate(changed)
        try:
            validate_policy(changed)
        except ValueError:
            negative.append(dict(case=name, rejected=True))
        else:
            raise ValueError("Negative fixture unexpectedly accepted: " + name)
    write("02-static-review.json", dict(status="PASS-STATIC", incoming_baseline=contract["incoming_baseline"],
          contract_sha256=digest(CONTRACT.read_bytes()), negative_cases=negative,
          review="Six directions reproduced exactly; phase-changing probes prevent assuming local differentiability."))


def report(name, passed, not_run):
    path = OUT / name
    require(path.is_file(), "Missing test report: " + name)
    assemblies = ET.parse(path).getroot().findall("assembly")
    require(len(assemblies) == 1, "Unexpected test assembly count")
    a = assemblies[0].attrib
    require(int(a["passed"]) == passed and int(a["failed"]) == 0 and int(a["errors"]) == 0
            and int(a["skipped"]) == 0 and int(a.get("not-run", 0)) == not_run, "Unexpected test counts: " + str(a))
    tests = list(ET.parse(path).getroot().iter("test"))
    require(len(tests) == passed + not_run and sum(t.attrib.get("result") == "Pass" for t in tests) == passed
            and sum(t.attrib.get("result") == "NotRun" for t in tests) == not_run, "Nonpassing test result")


def preexecution():
    authenticate()
    require(load(OUT / "01-inputs.json") == inputs(), "Derived points drift")
    require(load(OUT / "02-static-review.json")["contract_sha256"] == digest(CONTRACT.read_bytes()), "Stale static review")
    report("preexecution.xml", 3, 1)
    write("03-preexecution-review.json", dict(status="PASS-PREEXECUTION", contract_sha256=digest(CONTRACT.read_bytes()),
          assembly_sha256=digest(ASSEMBLY.read_bytes()), inputs_sha256=digest((OUT / "01-inputs.json").read_bytes())))


def adjudicate():
    authenticate()
    expected = inputs()["Points"]
    token = load(OUT / "03-preexecution-review.json")
    require(token["assembly_sha256"] == digest(ASSEMBLY.read_bytes()) and token["contract_sha256"] == digest(CONTRACT.read_bytes())
            and token["inputs_sha256"] == digest((OUT / "01-inputs.json").read_bytes()), "Execution identity drift")
    report("execution.xml", 1, 0)
    records = [json.loads(line) for line in (OUT / "04-trials.jsonl").read_text(encoding="utf-8-sig").splitlines()]
    require(len(records) == 14, "Recipe count mismatch")
    plan = load(ROOT / "eng/r3-equilibrium-reconstruction-planning1-contract.json")
    scales = [float(n[key]) for n in plan["residual_scales"] for key in ("mass_scale_kg_s", "energy_scale_w")]
    torque_scale = float(plan["rotor_torque_scale_nm"])
    baseline_score = max(abs(v) for v in expected[0]["Predicted"])
    comparisons = []
    for i, point in enumerate(expected):
        a, b = records[2 * i:2 * i + 2]
        require(a["Repeat"] == 1 and b["Repeat"] == 2 and {**a, "Repeat": 0} == {**b, "Repeat": 0}, "Nondeterministic repeat")
        require(a["Id"] == point["Id"] and a["Coordinates"] == point["Coordinates"], "Unplanned coordinates")
        observations = a["Observations"]
        if observations is None:
            require(i > 0 and a["Reason"].startswith(("C4-UNRESOLVED/", "THERMODYNAMIC-DOMAIN/", "QUANTITY-DOMAIN/", "NUMERICAL-REJECTION/")), "Unexpected rejection")
            comparisons.append(dict(id=point["Id"], reason=a["Reason"], strict_real_descent=False))
            continue
        require([o["Checkpoint"] for o in observations] == ["seed-step1", "post-seed", "1"], "Checkpoint drift")
        previous = a["Raw"]
        recomputed = []
        for o in observations:
            require(len(o["Nodes"]) == 12 and len(o["StateSha256"]) == 64, "Incomplete physical observation")
            residual = []
            for j, (now, before) in enumerate(zip(o["Nodes"], previous)):
                require(now["Id"] == before["Id"] == plan["search"]["variable_order"][j], "Node order drift")
                residual.extend([(now["Mass"] - before["Mass"]) / .01 / scales[2*j], (now["Energy"] - before["Energy"]) / .01 / scales[2*j+1]])
            residual.append(o["RotorTorque"] / torque_scale)
            require(all(math.isfinite(v) for v in residual) and residual == o["ScaledResiduals"], "Residual reconstruction mismatch")
            require(abs(o["MassClosure"]) <= 1e-6 and abs(o["EnergyClosure"]) <= .01 or not o["Healthy"], "Contradictory health sentinel")
            recomputed.extend(residual)
            previous = o["Nodes"]
        phases = [n["Phase"] for n in a["Raw"]] + [n["Phase"] for o in observations for n in o["Nodes"]]
        require(recomputed == a["Residuals"] and phases == a["Phases"], "Residual/phase report mismatch")
        require(a["Reason"] == ("VALID" if all(o["Healthy"] for o in observations) else "HEALTH-SENTINEL"), "Validity classification drift")
        if i == 0:
            require(recomputed == point["Predicted"] and phases == point["Phases"] and a["Reason"] == "VALID", "Frozen baseline no longer reproduced")
        score = max(abs(v) for v in recomputed)
        predicted_score = max(abs(v) for v in point["Predicted"])
        reduction = baseline_score - score
        ratio = reduction / (baseline_score - predicted_score) if i else None
        comparisons.append(dict(id=point["Id"], reason=a["Reason"], actual_maximum=score,
            predicted_maximum=predicted_score, actual_reduction_percent=100 * reduction / baseline_score,
            actual_to_predicted_reduction_ratio=ratio, maximum_prediction_error=max(abs(v-p) for v,p in zip(recomputed, point["Predicted"])),
            phase_changes=[j for j,(x,y) in enumerate(zip(phases, point["Phases"])) if x != y],
            all_in_envelope=all(o["InEnvelope"] for o in observations),
            strict_real_descent=a["Reason"] == "VALID" and reduction > 0,
            local_model_reliable=ratio is not None and ratio >= .1 and a["Reason"] == "VALID"))
    promising = [c["id"] for c in comparisons[1:] if c["strict_real_descent"] and c.get("local_model_reliable")]
    write("05-adjudication.json", dict(status="PASS-EVIDENCE-COMPLETE", comparisons=comparisons,
        promising_directions=promising, recipes=14, constructor_setup_steps=2, maximum_physical_steps=44,
        r3_passed=False, m10_closed=False, seed_installed=False,
        next_activity="PLAN-SEPARATE-BOUNDED-SEARCH" if promising else "REVIEW-FAILED-LOCAL-MODEL-BEFORE-ANY-NEW-SEARCH"))
    files = sorted(p for p in OUT.iterdir() if p.is_file() and p.name != "06-manifest.csv")
    with (OUT / "06-manifest.csv").open("w", newline="", encoding="utf-8") as stream:
        writer = csv.writer(stream)
        writer.writerow(["file", "bytes", "sha256"])
        writer.writerows((p.name, p.stat().st_size, digest(p.read_bytes())) for p in files)
    print(json.dumps(dict(status="PASS-EVIDENCE-COMPLETE", promising=promising, comparisons=comparisons), indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage", choices=["static", "preexecution", "adjudicate"])
    args = parser.parse_args()
    {"static": static, "preexecution": preexecution, "adjudicate": adjudicate}[args.stage]()
