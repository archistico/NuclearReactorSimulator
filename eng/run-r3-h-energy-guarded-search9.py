"""Bounded H9 search with energy margins and measured prediction-error checks."""
import argparse
import json
import math

import r3_h_protocol as P
import r3_h_phase_trajectory as T
import r3_h_phase_minimax as LP
import r3_h_c4_energy_margin as E
import r3_h_multiscale_derivatives as D

ID = "r3-h-energy-guarded-search9"
OUT = P.ROOT / "artifacts" / ID
POLICY = dict(variables=24,residuals=75,fractional_bound=.02,radius_initial=1e-5,radius_minimum=1e-7,radius_maximum=1e-5,
              derivative_widths=[1e-7,1e-8],maximum_halvings=4,maximum_iterations=32,maximum_recipes=3233,
              maximum_batches=97,maximum_physical_steps=9893,energy_margin_fraction=.1,
              minimum_reduction_to_Trial9_error=10.,maximum_prediction_error_fraction=.25,minimum_reduction_ratio=.1,
              production_change=False,bound_expansion=False,residual_reweighting=False,seed_installation=False,r3_passed=False)


def frozen(protocol):
    receipt=P.ROOT/"eng"/(protocol+"-returned.json")
    P.require(P.load(receipt)["contract_sha256"]==P.digest((P.ROOT/"eng"/(protocol+"-contract.json")).read_bytes()),"Prerequisite identity drift")
    return P.frozen(P.ROOT/"eng/frozen-evidence"/protocol,receipt)


def prerequisites():
    data=frozen("r3-h-energy-guarded-trial10")
    adjudication=json.loads(data["05-adjudication.json"])
    P.require(adjudication["promising"],"No favorable energy-guarded prerequisite")
    records=[r for line in data["03-batches.jsonl"].decode().splitlines() for r in json.loads(line)["records"]]
    candidates=[r for r in records if r["Id"].rsplit("/repeat/",1)[0] in adjudication["promising"]]
    return min(candidates,key=score),POLICY["minimum_reduction_to_Trial9_error"]*adjudication["diagnosis"]["observed_directional_error"]


def score(record):
    return P.H3["H2"]["score"](record)


def margins(record):
    return [E.node_margin(n)["normalized_margin_mj_kg"] for n in T.nodes(record)]


def eligible(record,phases,margin_values):
    return record["Reason"]=="VALID" and record["Phases"]==phases and all(m>0 for m in margin_values(record))


def search(evaluate,initial=None,minimum_reduction=None,limits=None,margin_values=margins):
    policy=POLICY if limits is None else limits
    if initial is None:
        initial,minimum_reduction=prerequisites()
    P.require(minimum_reduction is not None and minimum_reduction>=0,"Missing diagnostic decision threshold")
    current=evaluate([dict(Id="start",Coordinates=initial["Coordinates"])])[0]
    P.require({**current,"Id":"base"}=={**initial,"Id":"base"},"Starting state drift")
    phases=current["Phases"]
    radius=policy["radius_initial"]
    evaluations=1
    accepted,iterations=[],[]
    stop="ITERATION-LIMIT"
    for iteration in range(policy["maximum_iterations"]):
        if score(current)<=1:
            stop="ROOT-OBSERVED-UNCONFIRMED"
            break
        before=score(current)
        columns=[None]*24
        energy_columns=[None]*24
        stencils=[None]*24
        probes=[]
        budget_stop=False
        for level,h in enumerate(policy["derivative_widths"]):
            requests=[]
            groups={}
            for j in range(24):
                if columns[j] is not None:
                    continue
                groups[j]=[]
                for sign in (1,-1):
                    if abs(current["Coordinates"][j]+sign*h)<=.02:
                        coordinates=current["Coordinates"][:]
                        coordinates[j]+=sign*h
                        groups[j].append((sign,len(requests)))
                        requests.append(dict(Id=f"i{iteration}/level/{level}/probe/{j}/{sign}",Coordinates=coordinates))
            if evaluations+len(requests)>policy["maximum_recipes"]:
                budget_stop=True
                break
            if not requests:
                continue
            observed=evaluate(requests)
            evaluations+=len(observed)
            probes+=observed
            for j,indices in groups.items():
                same=[(s,observed[k]) for s,k in indices if observed[k]["Reason"]=="VALID" and observed[k]["Phases"]==phases]
                columns[j]=D.column(current["Residuals"],[(s,r["Residuals"]) for s,r in same],h)
                energy_columns[j]=D.column(margin_values(current),[(s,margin_values(r)) for s,r in same],h)
                stencils[j]=dict(width=h,signs=[s for s,_ in same])
        if budget_stop:
            stop="RECIPE-BUDGET"
            break
        diagnostic={}
        trial_requests,predictions=[],{}
        if any(c is None for c in columns):
            diagnostic=dict(status="UNAVAILABLE-PHASE-COMPATIBLE-COLUMN",columns=[j for j,c in enumerate(columns) if c is None])
        else:
            jacobian=[list(row) for row in zip(*columns)]
            energy_jacobian=[list(row) for row in zip(*energy_columns)]
            try:
                solution=LP.minimax_step(jacobian,current["Residuals"],current["Coordinates"],radius,margin_values(current),energy_jacobian)
            except ValueError as ex:
                known=("LP-CERTIFICATE-FAILED","LP-PIVOT-BUDGET","Phase step escaped box","Phase certificate failed in original quality units")
                if str(ex) not in known and not str(ex).startswith(("Phase primary LP:","Phase secondary LP:")):
                    raise
                diagnostic=dict(status="NUMERICAL-SUBPROBLEM-REJECTED",reason=str(ex))
            else:
                diagnostic=dict(status="PASS-NUMERICAL-CERTIFICATES",certificates=solution["certificates"],
                                full_predicted_energy_margins=solution["predicted_quality"],skipped_below_error_screen=0)
                for halving in range(policy["maximum_halvings"]+1):
                    fraction=2.**-halving
                    coordinates=[x+fraction*d for x,d in zip(current["Coordinates"],solution["delta"])]
                    delta=[x-b for x,b in zip(coordinates,current["Coordinates"])]
                    predicted=T.predict(current["Residuals"],jacobian,delta)
                    reduction=before-max(map(abs,predicted))
                    if reduction<=0 or reduction<minimum_reduction:
                        diagnostic["skipped_below_error_screen"]+=1
                        continue
                    identifier=f"i{iteration}/trial/{halving}"
                    trial_requests.append(dict(Id=identifier,Coordinates=coordinates))
                    predictions[identifier]=dict(residuals=predicted,reduction=reduction,
                                                  energy_margins=T.predict(margin_values(current),energy_jacobian,delta))
        trial_requests=trial_requests[:max(0,policy["maximum_recipes"]-evaluations)]
        trials=evaluate(trial_requests) if trial_requests else []
        evaluations+=len(trials)
        candidates=[r for r in probes if eligible(r,phases,margin_values)]
        trial_checks=[]
        for record in trials:
            prediction=predictions[record["Id"]]
            valid=record["Reason"]=="VALID"
            error=max(abs(a-b) for a,b in zip(record["Residuals"],prediction["residuals"])) if valid else None
            ratio=(before-score(record))/prediction["reduction"] if valid else None
            allowed=eligible(record,phases,margin_values) and error<=policy["maximum_prediction_error_fraction"]*prediction["reduction"] and ratio>=policy["minimum_reduction_ratio"]
            trial_checks.append(dict(id=record["Id"],eligible=allowed,maximum_prediction_error=error,reduction_ratio=ratio,
                                     predicted=prediction,actual_energy_margins=margin_values(record) if valid else None))
            if allowed:
                candidates.append(record)
        best=min([current]+candidates,key=score)
        improved=score(best)<before
        chosen_check=next((c for c in trial_checks if c["id"]==best["Id"]),None)
        if improved:
            current=best
            accepted.append(dict(iteration=iteration,id=best["Id"],maximum=score(best),coordinates=best["Coordinates"],is_model_step=chosen_check is not None))
        iterations.append(dict(iteration=iteration,radius=radius,stencils=stencils,diagnostic=diagnostic,trial_checks=trial_checks,
                               accepted=improved,best_id=best["Id"],maximum=score(current),
                               phase_or_energy_rejections=sum(not eligible(r,phases,margin_values) for r in probes+trials)))
        print(f"iteration={iteration} maximum={score(current):.12g} accepted={improved} radius={radius:.4g}",flush=True)
        if not improved and radius<=policy["radius_minimum"]:
            stop="MINIMUM-RADIUS-NO-ELIGIBLE-DESCENT"
            break
        if not improved:
            radius=max(policy["radius_minimum"],radius*.5)
        if evaluations>=policy["maximum_recipes"]:
            stop="RECIPE-BUDGET"
            break
    if score(current)<=1:
        stop="ROOT-OBSERVED-UNCONFIRMED"
    return dict(status="PASS-EVIDENCE-COMPLETE",stop=stop,evaluations=evaluations,minimum_predicted_reduction=minimum_reduction,
                accepted=accepted,iterations=iterations,final=current,root_observed=score(current)<=1,
                r3_passed=False,m10_closed=False,seed_installed=False)


def fixtures():
    results=D.fixtures()+LP.fixtures()
    def objective(requests):
        node=dict(Id="hotwell",Phase="SaturatedMixture")
        return [dict(**r,Reason="VALID",Residuals=[1e6*(r["Coordinates"][0]+r["Coordinates"][1]-.005),
                                                  1e6*(r["Coordinates"][0]-r["Coordinates"][1]-.003)]+[0.]*73,
                     Phases=["fixture"]*48,Raw=[node],Observations=[dict(Nodes=[node]) for _ in range(3)]) for r in requests]
    initial=objective([dict(Id="fixture",Coordinates=[0.]*24)])[0]
    result=search(objective,initial,0.,dict(POLICY,radius_initial=.01,radius_maximum=.01),lambda r:[.1]*4)
    P.require(result["root_observed"] and result["evaluations"]<=POLICY["maximum_recipes"],"Known coupled root not recovered")
    results.append(dict(name="known-coupled-root-with-error-check",passed=True))
    result=search(objective,initial,0.,dict(POLICY,maximum_recipes=1),lambda r:[.1]*4)
    P.require(result["stop"]=="RECIPE-BUDGET" and result["evaluations"]==1,"Budget did not stop before probes")
    results.append(dict(name="budget-stops-before-unfunded-probes",passed=True))
    return results


def static():
    P.authenticate(ID,POLICY)
    P.write(OUT,"01-static.json",dict(status="PASS-STATIC",contract_sha256=P.digest((P.ROOT/"eng"/(ID+"-contract.json")).read_bytes()),fixtures=fixtures()))


def execute():
    P.authenticate(ID,POLICY)
    driver=P.Batches(ID,POLICY["maximum_recipes"],POLICY["maximum_batches"])
    result=search(driver.evaluate)
    result.update(batches=driver.batches,maximum_physical_steps=3*driver.recipes+2*driver.batches)
    P.write(OUT,"04-result.json",result)


def audit(batches,expected):
    cursor=0
    def replay(requests):
        nonlocal cursor
        records=[]
        for offset in range(0,len(requests),48):
            P.require(cursor<len(batches),"Incomplete H9 trace")
            batch=batches[cursor]
            cursor+=1
            chunk=requests[offset:offset+48]
            P.require(batch["batch"]==cursor and batch["requests"]==chunk and len(batch["records"])==len(chunk),"H9 request drift")
            for request,record in zip(chunk,batch["records"]):
                P.require(request["Id"]==record["Id"] and request["Coordinates"]==record["Coordinates"],"H9 physical identity drift")
                P.audit_record(record)
            records+=batch["records"]
        return records
    result=search(replay)
    result.update(batches=cursor,maximum_physical_steps=3*result["evaluations"]+2*cursor)
    P.require(cursor==len(batches) and result==expected,"H9 replay drift")
    P.require(result["evaluations"]<=POLICY["maximum_recipes"] and cursor<=POLICY["maximum_batches"]
              and result["maximum_physical_steps"]<=POLICY["maximum_physical_steps"],"H9 budget drift")
    return dict(status="PASS-INDEPENDENT-ENERGY-GUARDED-REPLAY",stop=result["stop"],recipes=result["evaluations"],batches=cursor,
                maximum_physical_steps=result["maximum_physical_steps"],accepted_updates=len(result["accepted"]),
                accepted_model_steps=sum(a["is_model_step"] for a in result["accepted"]),initial_maximum=score(prerequisites()[0]),
                final_maximum=score(result["final"]),all_in_envelope=all(o["InEnvelope"] for o in result["final"]["Observations"]),
                root_observed=result["root_observed"],r3_passed=False,m10_closed=False,seed_installed=False)


def adjudicate():
    P.authenticate(ID,POLICY)
    result=audit([json.loads(line) for line in (OUT/"03-batches.jsonl").read_text().splitlines()],P.load(OUT/"04-result.json"))
    P.write(OUT,"05-adjudication.json",result)
    print(json.dumps(result,indent=2))


def review():
    P.authenticate(ID,POLICY)
    data=frozen(ID)
    result=audit([json.loads(line) for line in data["03-batches.jsonl"].decode().splitlines()],json.loads(data["04-result.json"]))
    P.require(result==json.loads(data["05-adjudication.json"]),"Frozen H9 adjudication drift")
    print(json.dumps(result,indent=2))


if __name__ == "__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage",choices=["static","preexecution","execute","adjudicate","review"])
    {"static":static,"preexecution":lambda:(P.authenticate(ID,POLICY),P.preexecution(ID)),"execute":execute,
     "adjudicate":adjudicate,"review":review}[parser.parse_args().stage]()
