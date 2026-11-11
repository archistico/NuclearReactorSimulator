"""Fixed one-sided directional derivative profile at the returned H8 boundary."""
import argparse
import json
import math

import r3_h_protocol as P
import r3_h_phase_trajectory as T
import r3_h_c4_phase_margin as C4

ID = "r3-h-directional-derivative-trial8"
OUT = P.ROOT / "artifacts" / ID
POLICY = dict(variables=24, residuals=75, fractional_bound=.02, fractions=[1., .1, .01, .001, .0001], repeats=2,
              recipes=12, batches=1, maximum_physical_steps=38, production_change=False, seed_installation=False, r3_passed=False)


def inputs():
    data = P.frozen(P.ROOT / "eng/frozen-evidence/r3-h-c4-margin-trial7", P.ROOT / "eng/r3-h-c4-margin-trial7-returned.json")
    points = json.loads(data["01-static.json"])["points"]
    base = points[0]
    direction = next(p for p in points if p["id"] == "margin/radius/1e-06")
    delta = [v - x for v, x in zip(direction["coordinates"], base["coordinates"])]
    changes = [v - r for v, r in zip(direction["predicted"], base["predicted"])]
    result = [dict(id="base", coordinates=base["coordinates"], predicted=base["predicted"], phases=base["phases"], fraction=0.)]
    for fraction in POLICY["fractions"]:
        coordinates = [x + fraction * d for x, d in zip(base["coordinates"], delta)]
        P.require(all(abs(x) <= .02 for x in coordinates), "Directional profile escaped original box")
        result.append(dict(id=f"fraction/{fraction:g}", coordinates=coordinates, predicted=[r + fraction * d for r, d in zip(base["predicted"], changes)],
                           phases=base["phases"], fraction=fraction))
    return result


def requests(points):
    return [dict(Id=p["id"] + f"/repeat/{repeat}", Coordinates=p["coordinates"]) for p in points for repeat in (1, 2)]


def static():
    P.authenticate(ID, POLICY)
    P.write(OUT, "01-static.json", dict(status="PASS-STATIC", contract_sha256=P.digest((P.ROOT / "eng" / (ID + "-contract.json")).read_bytes()), points=inputs()))


def execute():
    P.authenticate(ID, POLICY)
    P.Batches(ID, 12, 1).evaluate(requests(inputs()))


def audit(batches):
    points = inputs()
    P.require(len(batches) == 1 and batches[0]["batch"] == 1 and batches[0]["requests"] == requests(points), "Directional request identity drift")
    records = batches[0]["records"]
    P.require(len(records) == 12, "Directional record count drift")
    for request, record in zip(requests(points), records):
        P.require(request["Id"] == record["Id"] and request["Coordinates"] == record["Coordinates"], "Directional coordinate drift")
        P.audit_record(record)
    base = points[0]
    initial = max(map(abs, base["predicted"]))
    comparisons = []
    for i, point in enumerate(points):
        a, b = records[2 * i:2 * i + 2]
        P.require({**a, "Id": "repeat"} == {**b, "Id": "repeat"}, "Directional repeats differ")
        if i == 0:
            P.require(a["Reason"] == "VALID" and a["Residuals"] == base["predicted"] and a["Phases"] == base["phases"], "Directional base identity drift")
        score = P.H3["H2"]["score"](a)
        error = max((abs(r - predicted) for r, predicted in zip(a.get("Residuals") or [], point["predicted"])), default=None)
        path = T.nodes(a) if a["Residuals"] is not None else []
        comparisons.append(dict(id=point["id"], fraction=point["fraction"], reason=a["Reason"], actual_maximum=score if math.isfinite(score) else None,
                                predicted_maximum=max(map(abs, point["predicted"])), maximum_error=error,
                                error_per_fraction=error / point["fraction"] if point["fraction"] and error is not None else None,
                                actual_reduction=initial - score if math.isfinite(score) else None,
                                phase_compatible=len(path) == 4 and all(n["Phase"] == "SaturatedMixture" for n in path),
                                margins=[C4.node_margin(n)["reachability_margin"] for n in path],
                                phase_changes=[j for j, (x, y) in enumerate(zip(a.get("Phases", []), base["phases"])) if x != y]))
    return dict(status="PASS-REPEATED-DIRECTIONAL-DERIVATIVE-TRIAL", recipes=12, maximum_physical_steps=38, comparisons=comparisons,
                r3_passed=False, m10_closed=False, seed_installed=False)


def adjudicate():
    P.authenticate(ID, POLICY)
    result = audit([json.loads(line) for line in (OUT / "03-batches.jsonl").read_text().splitlines()])
    P.write(OUT, "05-adjudication.json", result)
    print(json.dumps(result, indent=2))


def review():
    P.authenticate(ID, POLICY)
    receipt = P.load(P.ROOT / "eng" / (ID + "-returned.json"))
    P.require(receipt["contract_sha256"] == P.digest((P.ROOT / "eng" / (ID + "-contract.json")).read_bytes()), "Directional returned identity drift")
    data = P.frozen(P.ROOT / "eng/frozen-evidence" / ID, P.ROOT / "eng" / (ID + "-returned.json"))
    result = audit([json.loads(line) for line in data["03-batches.jsonl"].decode().splitlines()])
    P.require(result == json.loads(data["05-adjudication.json"]), "Directional returned adjudication drift")
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage", choices=["static", "preexecution", "execute", "adjudicate", "review"])
    args = parser.parse_args()
    {"static": static, "preexecution": lambda: (P.authenticate(ID, POLICY), P.preexecution(ID)), "execute": execute, "adjudicate": adjudicate, "review": review}[args.stage]()
