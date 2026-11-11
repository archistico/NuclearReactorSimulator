"""Offline phase-path diagnosis and fixed repeated verification of branch-aware proposals."""
import argparse
import json
import math

import r3_h_protocol as P
import r3_h_phase_minimax as BRANCH
import r3_h_phase_trajectory as T
import r3_h_c4_phase_margin as C4
import r3_h_margin_trajectory as M

ID = "r3-h-c4-margin-trial7"
OUT = P.ROOT / "artifacts" / ID
POLICY = dict(variables=24, residuals=75, fractional_bound=.02, radius_values=[1e-6, 1e-5, 1e-4],
              mixture_margin_fraction=.1, unclipped_c4_margin=True, repeats=2, recipes=8, batches=1,
              maximum_physical_steps=26, production_change=False, seed_installation=False, r3_passed=False)


def inputs():
    mixture=M.model()
    base=mixture["base"]
    q0=[n["Quality"] for n in T.nodes(base)]
    p0=[n["Pressure"] for n in T.nodes(base)]
    margin=mixture["margins"]
    P.require(q0[-1]==0 and all(m>0 for m in margin),"H8 zero-quality boundary regression not reproduced")
    points=[dict(id="base",coordinates=base["Coordinates"],predicted=base["Residuals"],predicted_margin=margin,
                 predicted_pressure=p0,phases=base["Phases"],baseline=True)]
    for radius in POLICY["radius_values"]:
        solution=BRANCH.minimax_step(mixture["residual_jacobian"],base["Residuals"],base["Coordinates"],radius,margin,mixture["margin_jacobian"])
        delta=solution["delta"]
        points.append(dict(id=f"margin/radius/{radius:g}",coordinates=[x+d for x,d in zip(base["Coordinates"],delta)],
                           predicted=T.predict(base["Residuals"],mixture["residual_jacobian"],delta),
                           predicted_margin=T.predict(margin,mixture["margin_jacobian"],delta),
                           predicted_pressure=T.predict(p0,mixture["pressure_jacobian"],delta),phases=base["Phases"],baseline=False,
                           numerical_certificates=solution["certificates"]))
    return points,dict(reported_quality=q0,unclipped=[C4.node_margin(n) for n in T.nodes(base)],plant_calls=0)


def requests(points):
    return [dict(Id=p["id"] + f"/repeat/{repeat}", Coordinates=p["coordinates"]) for p in points for repeat in (1, 2)]


def static():
    P.authenticate(ID, POLICY)
    points, diagnosis = inputs()
    P.write(OUT, "01-static.json", dict(status="PASS-STATIC", contract_sha256=P.digest((P.ROOT / "eng" / (ID + "-contract.json")).read_bytes()),
                                       fixtures=BRANCH.fixtures()+C4.fixtures(), points=points, diagnosis=diagnosis))


def execute():
    P.authenticate(ID, POLICY)
    points, _ = inputs()
    P.Batches(ID, 8, 1).evaluate(requests(points))


def audit(batches):
    points, diagnosis = inputs()
    P.require(len(batches) == 1 and batches[0]["batch"] == 1 and batches[0]["requests"] == requests(points), "Phase trial request drift")
    records = batches[0]["records"]
    P.require(len(records) == 8, "Phase trial count drift")
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
                                predicted_margin=point["predicted_margin"], predicted_pressure=point["predicted_pressure"],
                                all_in_envelope=all(o["InEnvelope"] for o in a.get("Observations", [])) if a.get("Observations") else False,
                                promising=not point["baseline"] and a["Reason"] == "VALID" and score < initial and ratio is not None and ratio >= .1 and compatible))
    return dict(status="PASS-REPEATED-C4-MARGIN-TRIAL", recipes=8, maximum_physical_steps=26,
                unclipped_boundary=diagnosis, comparisons=comparisons,
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
