"""Fixed repeated energy-guarded proposals using Trial 9's largest complete stencil."""
import argparse
import json
import math

import r3_h_protocol as P
import r3_h_phase_trajectory as T
import r3_h_phase_minimax as LP
import r3_h_c4_energy_margin as E

ID = "r3-h-energy-guarded-trial10"
OUT = P.ROOT / "artifacts" / ID
POLICY = dict(variables=24, residuals=75, fractional_bound=.02, radius_values=[1e-6,1e-5,1e-4],
              model_selection="largest-complete-Trial9-width", energy_margin_fraction=.1,
              predicted_reduction_to_observed_error=10., maximum_prediction_error_fraction=.25,
              minimum_reduction_ratio=.1, repeats=2, maximum_recipes=8, maximum_batches=1,
              maximum_physical_steps=26, production_change=False, seed_installation=False, r3_passed=False)


def frozen(protocol):
    receipt=P.ROOT/"eng"/(protocol+"-returned.json")
    P.require(P.load(receipt)["contract_sha256"]==P.digest((P.ROOT/"eng"/(protocol+"-contract.json")).read_bytes()),"Prerequisite contract drift")
    return P.frozen(P.ROOT/"eng/frozen-evidence"/protocol,receipt)


def inputs():
    audit=json.loads(frozen("r3-h-multiscale-derivative-trial9")["05-adjudication.json"])
    model=max((p for p in audit["profiles"] if p["complete"]),key=lambda p:p["width"])
    base=json.loads(frozen("r3-h-phase-constrained-search8")["04-result.json"])["final"]
    jacobian=[list(row) for row in zip(*(c["residual_derivative"] for c in model["columns"]))]
    # The frozen LP normalizes each auxiliary inequality. MJ/kg is a unit
    # conversion for its positive margins; the 75 physical residuals are unchanged.
    energy_jacobian=[[v/1e6 for v in row] for row in zip(*(c["energy_derivative"] for c in model["columns"]))]
    margins=[E.node_margin(n)["normalized_margin_mj_kg"] for n in T.nodes(base)]
    error=max(c["maximum_residual_error"] for c in model["directional_comparisons"] if c["phase_compatible"])
    P.require(error>0 and all(m>0 for m in margins),"Invalid local margin/error prerequisite")
    baseline=max(map(abs,base["Residuals"]))
    points=[dict(id="base",coordinates=base["Coordinates"],predicted=base["Residuals"],
                 predicted_energy_margin_j_kg=[m*1e6 for m in margins],baseline=True)]
    diagnostics=[]
    for radius in POLICY["radius_values"]:
        try:
            solution=LP.minimax_step(jacobian,base["Residuals"],base["Coordinates"],radius,margins,energy_jacobian)
        except ValueError as ex:
            if str(ex) not in ("LP-CERTIFICATE-FAILED","LP-PIVOT-BUDGET"):
                raise
            diagnostics.append(dict(radius=radius,status="NUMERICAL-REJECTION",reason=str(ex),plant_calls=0))
            continue
        delta=solution["delta"]
        predicted=T.predict(base["Residuals"],jacobian,delta)
        reduction=baseline-max(map(abs,predicted))
        if reduction<POLICY["predicted_reduction_to_observed_error"]*error:
            diagnostics.append(dict(radius=radius,status="PREDICTED-REDUCTION-BELOW-DIAGNOSTIC-ERROR-SCREEN",predicted_reduction=reduction,plant_calls=0))
            continue
        points.append(dict(id=f"energy/radius/{radius:g}",coordinates=[x+d for x,d in zip(base["Coordinates"],delta)],
                           predicted=predicted,predicted_energy_margin_j_kg=[m*1e6 for m in solution["predicted_quality"]],
                           baseline=False,numerical_certificates=solution["certificates"]))
        diagnostics.append(dict(radius=radius,status="PASS-NUMERICAL-CERTIFICATES-AND-ERROR-SCREEN",predicted_reduction=reduction))
    return base,points,dict(width=model["width"],observed_directional_error=error,
                           error_is_not_a_bound_for_new_directions=True,subproblems=diagnostics)


def requests(points):
    return [dict(Id=p["id"]+f"/repeat/{repeat}",Coordinates=p["coordinates"]) for p in points for repeat in (1,2)]


def static():
    P.authenticate(ID,POLICY)
    _,points,diagnosis=inputs()
    P.require(len(requests(points))<=POLICY["maximum_recipes"],"Trial recipe budget exceeded")
    P.write(OUT,"01-static.json",dict(status="PASS-STATIC",contract_sha256=P.digest((P.ROOT/"eng"/(ID+"-contract.json")).read_bytes()),
                                     fixtures=LP.fixtures()+E.fixtures(),points=points,diagnosis=diagnosis))


def execute():
    P.authenticate(ID,POLICY)
    _,points,_=inputs()
    P.Batches(ID,POLICY["maximum_recipes"],POLICY["maximum_batches"]).evaluate(requests(points))


def audit(batches):
    base,points,diagnosis=inputs()
    expected=requests(points)
    P.require(len(batches)==1 and batches[0]["batch"]==1 and batches[0]["requests"]==expected,"Energy trial request drift")
    records=batches[0]["records"]
    P.require(len(records)==len(expected)<=POLICY["maximum_recipes"],"Energy trial count drift")
    for request,record in zip(expected,records):
        P.require(request["Id"]==record["Id"] and request["Coordinates"]==record["Coordinates"],"Energy trial identity drift")
        P.audit_record(record)
    baseline=max(map(abs,base["Residuals"]))
    comparisons=[]
    for i,point in enumerate(points):
        a,b=records[2*i:2*i+2]
        P.require({**a,"Id":"repeat"}=={**b,"Id":"repeat"},"Energy trial repeats differ")
        if point["baseline"]:
            P.require({**a,"Id":"base"}=={**base,"Id":"base"},"Energy trial baseline not reproduced")
        valid=a["Reason"]=="VALID"
        score=P.H3["H2"]["score"](a)
        compatible=valid and a["Phases"]==base["Phases"]
        predicted=max(map(abs,point["predicted"]))
        reduction=baseline-predicted
        ratio=(baseline-score)/reduction if not point["baseline"] and valid else None
        error=max(abs(x-y) for x,y in zip(a["Residuals"],point["predicted"])) if valid else None
        margins=[E.node_margin(n)["energy_margin_j_kg"] for n in T.nodes(a)] if valid else []
        promising=not point["baseline"] and compatible and all(m>0 for m in margins) and score<baseline and ratio>=POLICY["minimum_reduction_ratio"] and error<=POLICY["maximum_prediction_error_fraction"]*reduction
        comparisons.append(dict(id=point["id"],reason=a["Reason"],phase_compatible=compatible,
                                actual_maximum=score if math.isfinite(score) else None,predicted_maximum=predicted,
                                actual_reduction=baseline-score if math.isfinite(score) else None,reduction_ratio=ratio,
                                maximum_residual_error=error,maximum_residual_error_limit=POLICY["maximum_prediction_error_fraction"]*reduction,
                                actual_energy_margin_j_kg=margins,predicted_energy_margin_j_kg=point["predicted_energy_margin_j_kg"],
                                all_in_envelope=valid and all(o["InEnvelope"] for o in a["Observations"]),promising=promising))
    return dict(status="PASS-REPEATED-ENERGY-GUARDED-TRIAL",recipes=len(records),batches=1,maximum_physical_steps=3*len(records)+2,
                diagnosis=diagnosis,comparisons=comparisons,promising=[c["id"] for c in comparisons if c["promising"]],
                r3_passed=False,m10_closed=False,seed_installed=False)


def adjudicate():
    P.authenticate(ID,POLICY)
    result=audit([json.loads(line) for line in (OUT/"03-batches.jsonl").read_text().splitlines()])
    P.write(OUT,"05-adjudication.json",result)
    print(json.dumps(result,indent=2))


def review():
    P.authenticate(ID,POLICY)
    data=frozen(ID)
    result=audit([json.loads(line) for line in data["03-batches.jsonl"].decode().splitlines()])
    P.require(result==json.loads(data["05-adjudication.json"]),"Frozen energy trial adjudication drift")
    print(json.dumps(result,indent=2))


if __name__ == "__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage",choices=["static","preexecution","execute","adjudicate","review"])
    {"static":static,"preexecution":lambda:(P.authenticate(ID,POLICY),P.preexecution(ID)),"execute":execute,
     "adjudicate":adjudicate,"review":review}[parser.parse_args().stage]()
