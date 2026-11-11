"""Fixed repeated nonlinear comparison of four frozen minimax proposals."""
import argparse
import json

import r3_h_protocol as P

ID = "r3-h-minimax-trial2"
OUT = P.ROOT / "artifacts" / ID
POLICY = dict(variables=24, residuals=75, fractional_bound=.02, iterations=[8, 47], radii=[1e-6, 1e-4],
              repeats=2, recipes=12, batches=1, maximum_physical_steps=38, minimum_reduction_ratio=.1,
              nonlinear_search=False, production_change=False, seed_installation=False, r3_passed=False)


def inputs():
    payloads = P.frozen(P.ROOT / "eng/frozen-evidence/r3-h-minimax-direction-review1", P.ROOT / "eng/r3-h-minimax-direction-review1-returned.json")
    comparisons = json.loads(payloads["01-comparison.json"])["comparisons"]
    h3 = P.frozen(P.H3["FROZEN"], P.ROOT / "eng/r3-h-trust-search3-returned.json")
    records = {r["Id"]: r for line in h3["03-batches.jsonl"].decode().splitlines() for r in json.loads(line)["records"]}
    result = json.loads(h3["04-result.json"])
    points = []
    for iteration in POLICY["iterations"]:
        prior = [a for a in result["accepted"] if a["iteration"] < iteration]
        base = records[prior[-1]["id"]]
        points.append(dict(id=f"i{iteration}/base", coordinates=base["Coordinates"], predicted=base["Residuals"], phases=base["Phases"], baseline=True, base_id=f"i{iteration}/base"))
        for radius in POLICY["radii"]:
            comparison = next(c for c in comparisons if c["iteration"] == iteration and c["radius"] == radius)
            best = min(comparison["candidates"], key=lambda c: c["predicted_maximum_only"])
            points.append(dict(id=f"i{iteration}/radius/{radius:g}", coordinates=best["coordinates"], predicted=best["linear_predicted_residuals_only"], phases=base["Phases"], baseline=False, base_id=f"i{iteration}/base"))
    return points


def static():
    P.authenticate(ID, POLICY)
    points = inputs()
    P.require(len(points) == 6 and len({p["id"] for p in points}) == 6, "Unexpected frozen point selection")
    P.write(OUT, "01-static.json", dict(status="PASS-STATIC", contract_sha256=P.digest((P.ROOT / "eng" / (ID + "-contract.json")).read_bytes()), points=points))


def requests():
    return [dict(Id=point["id"] + f"/repeat/{repeat}", Coordinates=point["coordinates"]) for point in inputs() for repeat in (1, 2)]


def execute():
    P.authenticate(ID, POLICY)
    driver = P.Batches(ID, 12, 1)
    driver.evaluate(requests())


def audit(batches):
    P.require(len(batches) == 1 and batches[0]["batch"] == 1 and batches[0]["requests"] == requests(), "Trial request or count drift")
    records = batches[0]["records"]
    P.require(len(records) == 12, "Trial record count drift")
    for request, record in zip(requests(), records):
        P.require(request["Id"] == record["Id"] and request["Coordinates"] == record["Coordinates"], "Trial identity mismatch")
        P.audit_record(record)
    comparisons = []
    bases = {p["id"]: p for p in inputs() if p["baseline"]}
    for index, point in enumerate(inputs()):
        a, b = records[2 * index:2 * index + 2]
        P.require({**a, "Id": "repeat"} == {**b, "Id": "repeat"}, "Trial repeats differ")
        if point["baseline"]:
            P.require(a["Reason"] == "VALID" and a["Residuals"] == point["predicted"] and a["Phases"] == point["phases"], "Frozen base identity drift")
        baseline = max(map(abs, bases[point["base_id"]]["predicted"]))
        score = P.H3["H2"]["score"](a)
        predicted = max(map(abs, point["predicted"]))
        ratio = (baseline - score) / (baseline - predicted) if not point["baseline"] and score < float("inf") else None
        comparisons.append(dict(id=point["id"], reason=a["Reason"], actual_maximum=score if score < float("inf") else None,
            predicted_maximum=predicted, reduction_ratio=ratio, phase_changes=[j for j, (x, y) in enumerate(zip(a.get("Phases", []), point["phases"])) if x != y],
            prediction_error=max((abs(x - y) for x, y in zip(a.get("Residuals") or [], point["predicted"])), default=None),
            all_in_envelope=all(o["InEnvelope"] for o in a.get("Observations", [])) if a.get("Observations") else False,
            promising=not point["baseline"] and a["Reason"] == "VALID" and score < baseline and ratio is not None and ratio >= .1))
    return dict(status="PASS-REPEATED-MINIMAX-TRIAL", recipes=12, maximum_physical_steps=38, comparisons=comparisons,
                promising=[c["id"] for c in comparisons if c["promising"]], r3_passed=False, m10_closed=False, seed_installed=False)


def adjudicate():
    P.authenticate(ID, POLICY)
    result = audit([json.loads(line) for line in (OUT / "03-batches.jsonl").read_text().splitlines()])
    P.write(OUT, "05-adjudication.json", result)
    print(json.dumps(result, indent=2))


def review():
    P.authenticate(ID, POLICY)
    receipt = P.load(P.ROOT / "eng" / (ID + "-returned.json"))
    P.require(receipt["contract_sha256"] == P.digest((P.ROOT / "eng" / (ID + "-contract.json")).read_bytes()), "Returned contract drift")
    payloads = P.frozen(P.ROOT / "eng/frozen-evidence" / ID, P.ROOT / "eng" / (ID + "-returned.json"))
    result = audit([json.loads(line) for line in payloads["03-batches.jsonl"].decode().splitlines()])
    P.require(result == json.loads(payloads["05-adjudication.json"]), "Returned trial adjudication drift")
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage", choices=["static", "preexecution", "execute", "adjudicate", "review"])
    args = parser.parse_args()
    {"static": static, "preexecution": lambda: (P.authenticate(ID, POLICY), P.preexecution(ID)), "execute": execute, "adjudicate": adjudicate, "review": review}[args.stage]()
