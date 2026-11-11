"""Offline phase-path diagnosis and fixed repeated verification of branch-aware proposals."""
import argparse
import json
import math

import r3_h_protocol as P
import r3_h_minimax_regularized as FREE
import r3_h_phase_minimax as BRANCH
import r3_h_phase_trajectory as T

ID = "r3-h-phase-trajectory-trial6"
OUT = P.ROOT / "artifacts" / ID
POLICY = dict(variables=24, residuals=75, fractional_bound=.02, radius_values=[1e-6, 1e-5, 1e-4],
              mixture_margin_fraction=.1, historical_halving=9, repeats=2, recipes=10, batches=1,
              maximum_physical_steps=32, production_change=False, seed_installation=False, r3_passed=False)


def inputs():
    mixture = T.model("r3-h-secondary-minimax-search6")
    liquid = T.model("r3-h-liquid-branch-search7")
    base = mixture["base"]
    q0 = [n["Quality"] for n in T.nodes(base)]
    p0 = [n["Pressure"] for n in T.nodes(base)]
    free = FREE.minimax_step(mixture["residual_jacobian"], base["Residuals"], base["Coordinates"], 1e-4)
    dq = [sum(a * d for a, d in zip(row, free["delta"])) for row in mixture["quality_jacobian"]]
    critical = [q / -d if d < 0 else None for q, d in zip(q0, dq)]
    P.require(min(c for c in critical if c is not None) < 1 / 256, "Historical branch-limit diagnosis not reproduced")
    historical = []
    for k, trial in enumerate(mixture["trials"]):
        predicted_q = T.predict(q0, mixture["quality_jacobian"], free["delta"], 2. ** -k)
        actual_nodes = T.nodes(trial)
        historical.append(dict(halving=k, predicted_quality_extrapolation_only=predicted_q,
                               actual_quality=[n["Quality"] for n in actual_nodes], actual_phases=[n["Phase"] for n in actual_nodes]))
    P.require(all(q > 0 for q in historical[8]["predicted_quality_extrapolation_only"][:2])
              and all(q < 0 for q in historical[8]["predicted_quality_extrapolation_only"][2:])
              and historical[8]["actual_phases"] == ["SaturatedMixture", "SaturatedMixture", "SubcooledLiquid", "SubcooledLiquid"], "Checkpoint branch diagnosis mismatch")
    points = [dict(id="base", coordinates=base["Coordinates"], predicted=base["Residuals"], predicted_quality=q0, predicted_pressure=p0,
                   phases=base["Phases"], baseline=True)]
    def point(identifier, delta, certificates):
        points.append(dict(id=identifier, coordinates=[x + d for x, d in zip(base["Coordinates"], delta)],
                           predicted=T.predict(base["Residuals"], mixture["residual_jacobian"], delta),
                           predicted_quality=T.predict(q0, mixture["quality_jacobian"], delta),
                           predicted_pressure=T.predict(p0, mixture["pressure_jacobian"], delta),
                           phases=base["Phases"], baseline=False, numerical_certificates=certificates))
    point("historical/halving/9", [d / 512 for d in free["delta"]], free["certificates"])
    for radius in POLICY["radius_values"]:
        constrained = BRANCH.minimax_step(mixture["residual_jacobian"], base["Residuals"], base["Coordinates"], radius, q0, mixture["quality_jacobian"])
        point(f"branch/radius/{radius:g}", constrained["delta"], constrained["certificates"])
    diagnosis = dict(initial_quality=q0, free_quality_change=dq, critical_fractions=critical, historical_trials=historical,
                     mixture_pressure_jacobian=mixture["pressure_jacobian"], liquid_pressure_jacobian=liquid["pressure_jacobian"],
                     liquid_quality_is_undefined=liquid["quality_jacobian"] is None, plant_calls=0)
    return points, diagnosis


def requests(points):
    return [dict(Id=p["id"] + f"/repeat/{repeat}", Coordinates=p["coordinates"]) for p in points for repeat in (1, 2)]


def static():
    P.authenticate(ID, POLICY)
    points, diagnosis = inputs()
    P.write(OUT, "01-static.json", dict(status="PASS-STATIC", contract_sha256=P.digest((P.ROOT / "eng" / (ID + "-contract.json")).read_bytes()),
                                       fixtures=BRANCH.fixtures(), points=points, diagnosis=diagnosis))


def execute():
    P.authenticate(ID, POLICY)
    points, _ = inputs()
    P.Batches(ID, 10, 1).evaluate(requests(points))


def audit(batches):
    points, diagnosis = inputs()
    P.require(len(batches) == 1 and batches[0]["batch"] == 1 and batches[0]["requests"] == requests(points), "Phase trial request drift")
    records = batches[0]["records"]
    P.require(len(records) == 10, "Phase trial count drift")
    for request, record in zip(requests(points), records):
        P.require(request["Id"] == record["Id"] and request["Coordinates"] == record["Coordinates"], "Phase trial record identity drift")
        P.audit_record(record)
    initial = max(map(abs, points[0]["predicted"]))
    comparisons = []
    for i, point in enumerate(points):
        a, b = records[2 * i:2 * i + 2]
        P.require({**a, "Id": "repeat"} == {**b, "Id": "repeat"}, "Phase trial repeats differ")
        if point["baseline"]:
            P.require(a["Reason"] == "VALID" and a["Residuals"] == point["predicted"] and a["Phases"] == point["phases"], "Phase trial base identity drift")
        score = P.H3["H2"]["score"](a)
        predicted = max(map(abs, point["predicted"]))
        ratio = (initial - score) / (initial - predicted) if not point["baseline"] and math.isfinite(score) else None
        path = T.nodes(a) if a["Residuals"] is not None else []
        compatible = len(path) == 4 and all(n["Phase"] == "SaturatedMixture" for n in path)
        comparisons.append(dict(id=point["id"], reason=a["Reason"], predicted_maximum=predicted, actual_maximum=score if math.isfinite(score) else None,
                                reduction_ratio=ratio, phase_compatible=compatible, actual_hotwell=path,
                                predicted_quality=point["predicted_quality"], predicted_pressure=point["predicted_pressure"],
                                all_in_envelope=all(o["InEnvelope"] for o in a.get("Observations", [])) if a.get("Observations") else False,
                                promising=not point["baseline"] and a["Reason"] == "VALID" and score < initial and ratio is not None and ratio >= .1 and compatible))
    return dict(status="PASS-REPEATED-PHASE-TRAJECTORY-TRIAL", recipes=10, maximum_physical_steps=32,
                critical_fractions=diagnosis["critical_fractions"], comparisons=comparisons,
                promising=[c["id"] for c in comparisons if c["promising"]], r3_passed=False, m10_closed=False, seed_installed=False)


def adjudicate():
    P.authenticate(ID, POLICY)
    result = audit([json.loads(line) for line in (OUT / "03-batches.jsonl").read_text().splitlines()])
    P.write(OUT, "05-adjudication.json", result)
    print(json.dumps({**result, "comparisons": [{k: v for k, v in c.items() if k != "actual_hotwell"} for c in result["comparisons"]]}, indent=2))


def review():
    P.authenticate(ID, POLICY)
    receipt = P.load(P.ROOT / "eng" / (ID + "-returned.json"))
    P.require(receipt["contract_sha256"] == P.digest((P.ROOT / "eng" / (ID + "-contract.json")).read_bytes()), "Phase returned contract drift")
    payloads = P.frozen(P.ROOT / "eng/frozen-evidence" / ID, P.ROOT / "eng" / (ID + "-returned.json"))
    result = audit([json.loads(line) for line in payloads["03-batches.jsonl"].decode().splitlines()])
    P.require(result == json.loads(payloads["05-adjudication.json"]), "Phase returned adjudication drift")
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage", choices=["static", "preexecution", "execute", "adjudicate", "review"])
    args = parser.parse_args()
    {"static": static, "preexecution": lambda: (P.authenticate(ID, POLICY), P.preexecution(ID)), "execute": execute, "adjudicate": adjudicate, "review": review}[args.stage]()
