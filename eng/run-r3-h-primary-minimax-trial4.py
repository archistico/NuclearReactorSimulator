"""Frozen repeated nonlinear check of certified coupled minimax model steps."""
import argparse
import json

import r3_h_protocol as P
import r3_h_minimax_primary as LP
import r3_h_minimax_lp as HISTORICAL

ID = "r3-h-primary-minimax-trial4"
OUT = P.ROOT / "artifacts" / ID
POLICY = dict(iteration=0, radii=[1e-6, 1e-4, 4e-4], variables=24, residuals=75, fractional_bound=.02,
              repeats=2, recipes=8, batches=1, maximum_physical_steps=26, minimum_reduction_ratio=.1,
              lp_epsilon=1e-10, maximum_pivots=20000, certificate_relative_tolerance=1e-7,
              secondary_objective=False, production_change=False, seed_installation=False, r3_passed=False)


def inputs():
    payloads = P.frozen(P.ROOT / "eng/frozen-evidence/r3-h-coupled-minimax-search5", P.ROOT / "eng/r3-h-coupled-minimax-search5-returned.json")
    batches = [json.loads(line) for line in payloads["03-batches.jsonl"].decode().splitlines()]
    current = batches[0]["records"][0]
    probes = batches[1]["records"]
    columns = []
    for j in range(24):
        samples = [(int(r["Id"].split("/")[-1]),r) for r in probes if r["Id"].startswith(f"i0/probe/{j}/")]
        column,_ = P.H3["compatible_column"](current,samples,1e-6)
        columns.append(column)
    P.require(all(c is not None for c in columns),"Unavailable primary regression derivative")
    jacobian = [list(row) for row in zip(*columns)]
    regression = json.loads(payloads["07-first-model.json"])
    P.require(jacobian == regression["jacobian"] and current["Coordinates"] == regression["coordinates"] and current["Residuals"] == regression["residuals"],"H5 regression model mismatch")
    points = [dict(id="base",coordinates=current["Coordinates"],predicted=current["Residuals"],phases=current["Phases"],baseline=True)]
    for radius in POLICY["radii"]:
        solution = LP.minimax_step(jacobian,current["Residuals"],current["Coordinates"],radius)
        predicted = P.H3["H2"]["MATH"]["predict"](jacobian,current["Residuals"],solution["delta"],1.)
        points.append(dict(id=f"radius/{radius:g}",radius=radius,coordinates=[x+d for x,d in zip(current["Coordinates"],solution["delta"])],
                           predicted=predicted,phases=current["Phases"],baseline=False,numerical_certificates=solution["certificates"]))
    return points


def regression_fixture():
    payloads = P.frozen(P.ROOT / "eng/frozen-evidence/r3-h-coupled-minimax-search5",P.ROOT / "eng/r3-h-coupled-minimax-search5-returned.json")
    model = json.loads(payloads["07-first-model.json"])
    LP.minimax_step(model["jacobian"],model["residuals"],model["coordinates"],model["radius"])
    try:
        HISTORICAL.minimax_step(model["jacobian"],model["residuals"],model["coordinates"],model["radius"])
    except ValueError as ex:
        P.require(str(ex)=="LP-CERTIFICATE-FAILED","Unexpected historical numerical rejection")
    else:
        raise ValueError("Historical secondary rejection was not reproduced")
    return dict(name="h5-primary-passes-secondary-still-rejected",passed=True)


def requests(points):
    return [dict(Id=p["id"] + f"/repeat/{repeat}", Coordinates=p["coordinates"]) for p in points for repeat in (1, 2)]


def static():
    P.authenticate(ID, POLICY)
    P.write(OUT, "01-static.json", dict(status="PASS-STATIC", contract_sha256=P.digest((P.ROOT / "eng" / (ID + "-contract.json")).read_bytes()),
                                       fixtures=LP.fixtures() + [regression_fixture()], points=inputs()))


def execute():
    P.authenticate(ID, POLICY)
    P.Batches(ID, 8, 1).evaluate(requests(inputs()))


def audit(batches):
    points = inputs()
    P.require(len(batches) == 1 and batches[0]["batch"] == 1 and batches[0]["requests"] == requests(points), "Coupled trial request drift")
    records = batches[0]["records"]
    P.require(len(records) == 8, "Coupled trial budget drift")
    for request, record in zip(requests(points), records):
        P.require(request["Id"] == record["Id"] and request["Coordinates"] == record["Coordinates"], "Coupled trial identity drift")
        P.audit_record(record)
    initial = max(map(abs, points[0]["predicted"]))
    comparisons = []
    for i, point in enumerate(points):
        a, b = records[2 * i:2 * i + 2]
        P.require({**a, "Id": "repeat"} == {**b, "Id": "repeat"}, "Coupled trial repeats differ")
        if point["baseline"]:
            P.require(a["Reason"] == "VALID" and a["Residuals"] == point["predicted"] and a["Phases"] == point["phases"], "Coupled base identity drift")
        score = P.H3["H2"]["score"](a)
        predicted = max(map(abs, point["predicted"]))
        ratio = (initial - score) / (initial - predicted) if not point["baseline"] and score < float("inf") else None
        comparisons.append(dict(id=point["id"], radius=point.get("radius"), reason=a["Reason"], actual_maximum=score if score < float("inf") else None,
                                predicted_maximum=predicted, reduction_ratio=ratio,
                                phase_changes=[j for j, (x, y) in enumerate(zip(a.get("Phases", []), point["phases"])) if x != y],
                                all_in_envelope=all(o["InEnvelope"] for o in a.get("Observations", [])) if a.get("Observations") else False,
                                promising=not point["baseline"] and a["Reason"] == "VALID" and score < initial and ratio is not None and ratio >= .1))
    return dict(status="PASS-REPEATED-PRIMARY-MINIMAX-TRIAL", recipes=8, maximum_physical_steps=26, comparisons=comparisons,
                promising=[c["id"] for c in comparisons if c["promising"]], root_observed=False, r3_passed=False, m10_closed=False, seed_installed=False)


def adjudicate():
    P.authenticate(ID, POLICY)
    result = audit([json.loads(line) for line in (OUT / "03-batches.jsonl").read_text().splitlines()])
    P.write(OUT, "05-adjudication.json", result)
    print(json.dumps(result, indent=2))


def review():
    P.authenticate(ID, POLICY)
    receipt = P.load(P.ROOT / "eng" / (ID + "-returned.json"))
    P.require(receipt["contract_sha256"] == P.digest((P.ROOT / "eng" / (ID + "-contract.json")).read_bytes()), "Coupled returned identity drift")
    payloads = P.frozen(P.ROOT / "eng/frozen-evidence" / ID, P.ROOT / "eng" / (ID + "-returned.json"))
    result = audit([json.loads(line) for line in payloads["03-batches.jsonl"].decode().splitlines()])
    P.require(result == json.loads(payloads["05-adjudication.json"]), "Coupled returned adjudication drift")
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage", choices=["static", "preexecution", "execute", "adjudicate", "review"])
    args = parser.parse_args()
    {"static": static, "preexecution": lambda: (P.authenticate(ID, POLICY), P.preexecution(ID)), "execute": execute, "adjudicate": adjudicate, "review": review}[args.stage]()
