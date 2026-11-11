"""Bounded scaled H2 search with explicit batches and complete offline replay."""
from __future__ import annotations

import argparse
import copy
import gzip
import json
import math
import os
import runpy
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
LIB = runpy.run_path(str(ROOT / "eng/review-r3-h-numerics1.py"))
MATH = runpy.run_path(str(ROOT / "eng/design-r3-h-local-model1.py"))
require, digest, load = LIB["require"], LIB["digest"], LIB["load"]
OUT = ROOT / "artifacts/r3-h-scaled-search2"
CONTRACT = ROOT / "eng/r3-h-scaled-search2-contract.json"
NEW_TEST = "tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/M10R3HScaledSearch2Tests.cs"
PROJECT = "tests/NuclearReactorSimulator.Application.Tests/NuclearReactorSimulator.Application.Tests.csproj"
ASSEMBLY = ROOT / "tests/NuclearReactorSimulator.Application.Tests/bin/Release/net10.0/NuclearReactorSimulator.Application.Tests.dll"
POLICY = dict(variables=24, residuals=75, fractional_bound=.02, derivative_step=1e-6,
              damping=[.001,.01,.1,1,10,100], fractions=[1,.5,.25,.125], fraction_safety=.99,
              maximum_recipes=4096, maximum_iterations=64, maximum_batches=129,
              maximum_physical_steps=12546, active_set_iterations=256, kkt_relative_tolerance=1e-10,
              stagnation_relative=1e-10, stagnation_updates=3, root_threshold=1,
              start="lambda-0.01", seed_installation=False, r3_passed=False,
              production_change=False, residual_reweighting=False, bound_expansion=False)


def write(name, value):
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT/name).write_text(json.dumps(value, indent=2, allow_nan=False)+"\n", encoding="utf-8")


def bounded_quadratic(matrix, rhs, lower, upper):
    """Convex quadratic active set; bound hits and releases use ordinal ties."""
    n = len(rhs)
    z, active = [0.] * n, {}
    releases = 0
    tolerance = 1e-10 * max(1., max(abs(v) for v in rhs))
    for iteration in range(256):
        free = [j for j in range(n) if j not in active]
        target = z[:]
        if free:
            reduced = [[matrix[i][j] for j in free] + [rhs[i] - sum(matrix[i][j]*z[j] for j in active)] for i in free]
            solution = LIB["solve"](reduced)
            for j, value in zip(free, solution):
                target[j] = value
        hits = []
        for j in free:
            if target[j] < lower[j]:
                hits.append(((lower[j]-z[j])/(target[j]-z[j]), j, -1))
            elif target[j] > upper[j]:
                hits.append(((upper[j]-z[j])/(target[j]-z[j]), j, 1))
        if hits:
            fraction, hit, side = min(hits)
            require(0 <= fraction <= 1, "Infeasible active-set path")
            for j in free:
                z[j] += fraction * (target[j]-z[j])
            z[hit] = lower[hit] if side < 0 else upper[hit]
            active[hit] = side
            continue
        z = target
        gradient = [sum(row[j]*z[j] for j in range(n))-r for row,r in zip(matrix,rhs)]
        violations = [(side * gradient[j], -j, j) for j,side in active.items() if side * gradient[j] > tolerance]
        if not violations:
            require(all(lower[j]-1e-8 <= z[j] <= upper[j]+1e-8 for j in range(n)), "Quadratic escaped bounds")
            require(all(abs(gradient[j]) <= tolerance for j in free), "Unresolved free-coordinate stationarity")
            return z, dict(iterations=iteration+1, releases=releases, active=[dict(index=j, side=active[j]) for j in sorted(active)],
                           free_gradient_max=max((abs(gradient[j]) for j in free), default=0.), tolerance=tolerance)
        del active[max(violations)[2]]
        releases += 1
    raise ValueError("ACTIVE-SET-ITERATION-LIMIT")


def direction(jacobian, residual, x, damping):
    scales = [math.sqrt(sum(row[j]*row[j] for row in jacobian)) or 1. for j in range(len(x))]
    reduced = [[a/s for a,s in zip(row,scales)] for row in jacobian]
    augmented = LIB["normal"](reduced, residual, damping)
    matrix, rhs = [row[:-1] for row in augmented], [row[-1] for row in augmented]
    lower = [(-.02-v)*s for v,s in zip(x,scales)]
    upper = [(.02-v)*s for v,s in zip(x,scales)]
    z, diagnostic = bounded_quadratic(matrix,rhs,lower,upper)
    delta = [v/s for v,s in zip(z,scales)]
    _,alpha,point = MATH["fraction_to_box"](x,delta)
    require(point is not None, "No interior step from quadratic")
    return [alpha*d for d in delta], diagnostic


def fixtures():
    checks=[]
    for name,h,g,lo,hi,want in [
        ("interior", [[2.,1.],[1.,2.]], [3.,3.], [-2.,-2.],[2.,2.],[1.,1.]),
        ("coupled-upper", [[2.,1.],[1.,2.]], [4.,4.], [-1.,-1.],[1.,1.],[1.,1.]),
        ("coupled-one-active", [[2.,1.],[1.,2.]], [4.,0.], [-1.,-1.],[1.,1.],[1.,-.5]),
        ("coupled-lower", [[2.,1.],[1.,2.]], [-4.,-4.], [-1.,-2.],[1.,2.],[-1.,-1.5]),
        ("release-bound", [[2.,-3.],[-3.,5.]], [1.5,-6.], [-1.,-1.],[1.,1.],[-.75,-1.]),
    ]:
        actual,diag=bounded_quadratic(h,g,lo,hi)
        require(max(abs(a-b) for a,b in zip(actual,want)) < 1e-12, "Quadratic fixture: "+name)
        require(name != "release-bound" or diag["releases"] == 1, "Active-set release was not exercised")
        gradient=[sum(a*b for a,b in zip(row,actual))-v for row,v in zip(h,g)]
        require(all((v >= -1e-10 if abs(x-l)<1e-12 else v <= 1e-10 if abs(x-u)<1e-12 else abs(v)<1e-10)
                    for x,l,u,v in zip(actual,lo,hi,gradient)), "KKT fixture: "+name)
        checks.append(dict(name=name, passed=True, **diag))
    for name,j,target in [("badly-scaled", [[1e12,1.],[1e12,-1.]], [1e-12,.005]),
                           ("zero-column", [[1.,0.],[2.,0.]], [.005,0.])]:
        residual=[-sum(a*b for a,b in zip(row,target)) for row in j]
        step,_=direction(j,residual,[0.,0.],.001)
        require(max(abs(v) for v in MATH["predict"](j,residual,step,1.)) < max(abs(v) for v in residual)*.002, "Scaled fixture: "+name)
        checks.append(dict(name=name, passed=True))
    return checks


def validate_policy(c):
    require(c["schema"] == "r3-h-scaled-search2-v1" and c["policy"] == POLICY, "Frozen search policy drift")
    require(c["test_additions"] == [NEW_TEST] and c["output"] == "artifacts/r3-h-scaled-search2", "Search surface drift")


def authenticate():
    c=load(CONTRACT)
    validate_policy(c)
    for key in ("src","tests"):
        files=sorted((p for p in (ROOT/key).rglob("*") if p.is_file()
                      and not {"bin","obj"}.intersection(p.relative_to(ROOT).parts)
                      and p.relative_to(ROOT).as_posix()!=NEW_TEST), key=lambda p:p.relative_to(ROOT).as_posix())
        rows="".join(f"{p.relative_to(ROOT).as_posix()}|{digest(p.read_bytes())}\n" for p in files)
        require(dict(file_count=len(files), tree_sha256=digest(rows.encode())) == c["incoming_baseline"][key], "Incoming source/test drift: "+key)
    for entry in c["implementation_files"]+c["frozen_inputs"]:
        require(digest((ROOT/entry["path"]).read_bytes())==entry["sha256"], "Frozen input drift: "+entry["path"])
    frozen=ROOT/"eng/frozen-evidence/r3-h-local-trial1"
    for entry in load(frozen/"manifest.json")["files"]:
        data=(frozen/entry["stored"]).read_bytes()
        require(digest(data)==entry["stored_sha256"], "Prerequisite stored payload drift")
        if entry["stored"].endswith(".gz"):
            data=gzip.decompress(data)
        require(digest(data)==entry["sha256"] and len(data)==entry["bytes"], "Prerequisite logical payload drift")
    require(load(frozen/"05-adjudication.json")["promising_directions"]==[f"lambda-{v:g}" for v in POLICY["damping"]], "Prerequisite trial failed")
    return c


def start_record():
    data=gzip.decompress((ROOT/"eng/frozen-evidence/r3-h-local-trial1/04-trials.jsonl.gz").read_bytes()).decode("utf-8-sig")
    return next(r for r in map(json.loads,data.splitlines()) if r["Id"]==POLICY["start"] and r["Repeat"]==1)


def test_report(path, passed, not_run):
    root=ET.parse(path).getroot()
    a=root.find("assembly").attrib
    require(int(a["passed"])==passed and int(a["failed"])==0 and int(a["errors"])==0
            and int(a["skipped"])==0 and int(a.get("not-run",0))==not_run, "Failed or incomplete tests")


def static():
    c=authenticate()
    negative=[]
    for name,key,value in [("wider-box","fractional_bound",.03),("extra-budget","maximum_recipes",4097),
                           ("seed-installation","seed_installation",True),("residual-reweighting","residual_reweighting",True)]:
        changed=copy.deepcopy(c)
        changed["policy"][key]=value
        try:
            validate_policy(changed)
        except ValueError:
            negative.append(dict(case=name,rejected=True))
        else:
            raise ValueError("Negative policy fixture accepted: "+name)
    write("01-static.json", dict(status="PASS-STATIC", numerical_fixtures=fixtures(), negative_policy_cases=negative,
                                 contract_sha256=digest(CONTRACT.read_bytes()), incoming_baseline=c["incoming_baseline"]))


def preexecution():
    authenticate()
    fixtures()
    test_report(OUT/"preexecution.xml",2,1)
    require(load(OUT/"01-static.json")["contract_sha256"]==digest(CONTRACT.read_bytes()), "Stale static review")
    write("02-preexecution.json", dict(status="PASS-PREEXECUTION", contract_sha256=digest(CONTRACT.read_bytes()), assembly_sha256=digest(ASSEMBLY.read_bytes())))


def score(record):
    return max(abs(v) for v in record["Residuals"]) if record["Reason"]=="VALID" else math.inf


def search(evaluate):
    start=start_record()
    current=evaluate([dict(Id="start",Coordinates=start["Coordinates"])])[0]
    require(current["Residuals"]==start["Residuals"] and current["Phases"]==start["Phases"] and current["Reason"]=="VALID", "Starting point mismatch")
    evaluations=1
    accepted=[]
    models=[]
    stagnant=0
    reason="ITERATION-LIMIT"
    for iteration in range(64):
        if score(current)<=1:
            reason="ROOT-OBSERVED-UNCONFIRMED"
            break
        requests=[]
        stencils=[]
        for j,x in enumerate(current["Coordinates"]):
            signs=[1,-1] if abs(x)+1e-6<=.02 else [-1 if x>0 else 1]
            indices=[]
            for sign in signs:
                point=current["Coordinates"][:]
                point[j]+=sign*1e-6
                indices.append(len(requests))
                requests.append(dict(Id=f"i{iteration}/probe/{j}/{sign}",Coordinates=point))
            stencils.append((signs,indices))
        if evaluations+len(requests)>4096:
            reason="RECIPE-BUDGET"
            break
        probes=evaluate(requests)
        evaluations+=len(probes)
        if any(p["Reason"]!="VALID" for p in probes):
            reason="INVALID-DIFFERENCE-PROBE"
            break
        jacobian=[[0.]*24 for _ in range(75)]
        for j,(signs,indices) in enumerate(stencils):
            for i in range(75):
                jacobian[i][j]=((probes[indices[0]]["Residuals"][i]-probes[indices[1]]["Residuals"][i])/2e-6 if len(signs)==2
                                else (probes[indices[0]]["Residuals"][i]-current["Residuals"][i])/(signs[0]*1e-6))
        requests=[]
        iteration_models=[]
        for damping in POLICY["damping"]:
            try:
                delta,diagnostic=direction(jacobian,current["Residuals"],current["Coordinates"],damping)
            except ValueError as ex:
                iteration_models.append(dict(damping=damping,rejected=str(ex)))
                continue
            iteration_models.append(dict(damping=damping,**diagnostic))
            for fraction in POLICY["fractions"]:
                point=[x+fraction*d for x,d in zip(current["Coordinates"],delta)]
                require(all(math.isfinite(x) and abs(x)<=.02 for x in point), "Out-of-bounds search point")
                requests.append(dict(Id=f"i{iteration}/trial/{damping:g}/{fraction:g}",Coordinates=point))
        models.append(dict(iteration=iteration,models=iteration_models,
                           phase_changing_probes=[p["Id"] for p in probes if p["Phases"]!=current["Phases"]]))
        requests=requests[:4096-evaluations]
        if not requests:
            reason="NO-ADMISSIBLE-LOCAL-MODEL" if evaluations<4096 else "RECIPE-BUDGET"
            break
        trials=evaluate(requests)
        evaluations+=len(trials)
        best=min(trials,key=score)
        if score(best)>=score(current):
            reason="NO-STRICT-DESCENT"
            break
        relative=(score(current)-score(best))/score(current)
        stagnant=stagnant+1 if relative<=1e-10 else 0
        current=best
        accepted.append(dict(iteration=iteration,id=current["Id"],coordinates=current["Coordinates"],maximum=score(current),relative_reduction=relative))
        if stagnant>=3:
            reason="STAGNATION"
            break
    if score(current)<=1:
        reason="ROOT-OBSERVED-UNCONFIRMED"
    return dict(status="PASS-EVIDENCE-COMPLETE",stop=reason,evaluations=evaluations,accepted=accepted,
                final=current,models=models,root_observed=score(current)<=1,
                r3_passed=False,m10_closed=False,seed_installed=False)


def execute():
    authenticate()
    review=load(OUT/"02-preexecution.json")
    require(review["contract_sha256"]==digest(CONTRACT.read_bytes()) and review["assembly_sha256"]==digest(ASSEMBLY.read_bytes()), "Stale preexecution identity")
    trace=OUT/"03-batches.jsonl"
    require(not trace.exists(), "Existing execution retained; use replay, never overwrite")
    batches=0
    def evaluate(requests):
        nonlocal batches
        require(batches<129, "Batch budget exceeded")
        write("batch-request.json",requests)
        write("batch-token.json",dict(status="PASS-BATCH-REVIEW",contract_sha256=review["contract_sha256"],assembly_sha256=review["assembly_sha256"],request_sha256=digest((OUT/"batch-request.json").read_bytes())))
        env=dict(os.environ,NRS_R3_H_SCALED_SEARCH2="1")
        command=["dotnet","test","--project",PROJECT,"--configuration","Release","--no-build","--results-directory",str(OUT),"--minimum-expected-tests","1","--","--explicit","only","--filter-method","NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10R3HScaledSearch2Tests.ExecuteReviewedBatch","--parallel","none","--report-xunit","--report-xunit-filename","batch-execution.xml"]
        with (OUT/"execution.log").open("a",encoding="utf-8") as log:
            subprocess.run(command,cwd=ROOT,env=env,stdout=log,stderr=subprocess.STDOUT,check=True)
        test_report(OUT/"batch-execution.xml",1,0)
        records=[json.loads(line) for line in (OUT/"batch-results.jsonl").read_text(encoding="utf-8-sig").splitlines()]
        require(len(records)==len(requests), "Batch count mismatch")
        for request,record in zip(requests,records):
            require(request["Id"]==record["Id"] and request["Coordinates"]==record["Coordinates"], "Batch identity mismatch")
        batches+=1
        with trace.open("a",encoding="utf-8") as stream:
            stream.write(json.dumps(dict(batch=batches,requests=requests,records=records),allow_nan=False)+"\n")
        print(f"batch={batches} recipes={len(records)} best={min(map(score,records)):.9g}",flush=True)
        return records
    result=search(evaluate)
    result["batches"]=batches
    result["maximum_physical_steps"]=result["evaluations"]*3+batches*2
    write("04-result.json",result)


def verify_physics(record,plan):
    if record["Residuals"] is None:
        require(record["Reason"].startswith(("C4-UNRESOLVED/","THERMODYNAMIC-DOMAIN/","QUANTITY-DOMAIN/","NUMERICAL-REJECTION/")), "Unexpected physical rejection")
        return
    scales=[float(n[k]) for n in plan["residual_scales"] for k in ("mass_scale_kg_s","energy_scale_w")]
    previous=record["Raw"]
    residual=[]
    obs=record["Observations"]
    require([o["Checkpoint"] for o in obs]==["seed-step1","post-seed","1"], "Checkpoint drift")
    for o in obs:
        values=[]
        require(len(previous)==len(o["Nodes"])==12, "Incomplete node record")
        for j,(now,before) in enumerate(zip(o["Nodes"],previous)):
            require(now["Id"]==before["Id"]==plan["search"]["variable_order"][j], "Physical node order drift")
            values.extend([(now["Mass"]-before["Mass"])/.01/scales[2*j],(now["Energy"]-before["Energy"])/.01/scales[2*j+1]])
        values.append(o["RotorTorque"]/float(plan["rotor_torque_scale_nm"]))
        require(all(math.isfinite(v) for v in values) and values==o["ScaledResiduals"], "Physical residual mismatch")
        require(not o["Healthy"] or abs(o["MassClosure"])<=1e-6 and abs(o["EnergyClosure"])<=.01, "Contradictory health")
        residual.extend(values)
        previous=o["Nodes"]
    require(record["Residuals"]==residual and len(residual)==75, "Flattened residual mismatch")
    require(record["Phases"]==[n["Phase"] for n in record["Raw"]]+[n["Phase"] for o in obs for n in o["Nodes"]], "Physical phase mismatch")
    require(record["Reason"]==("VALID" if all(o["Healthy"] for o in obs) else "HEALTH-SENTINEL"), "Health classification drift")


def adjudicate():
    authenticate()
    batches=[json.loads(line) for line in (OUT/"03-batches.jsonl").read_text().splitlines()]
    cursor=0
    plan=load(ROOT/"eng/r3-equilibrium-reconstruction-planning1-contract.json")
    def replay(requests):
        nonlocal cursor
        require(cursor<len(batches), "Incomplete physical trace")
        batch=batches[cursor]
        cursor+=1
        require(batch["batch"]==cursor and batch["requests"]==requests and len(batch["records"])==len(requests), "Search replay mismatch")
        for request,record in zip(requests,batch["records"]):
            require(request["Id"]==record["Id"] and request["Coordinates"]==record["Coordinates"], "Physical trace identity mismatch")
            verify_physics(record,plan)
        return batch["records"]
    result=search(replay)
    result["batches"]=cursor
    result["maximum_physical_steps"]=result["evaluations"]*3+cursor*2
    require(cursor==len(batches) and result==load(OUT/"04-result.json"), "Final replay mismatch")
    final=result["final"]
    ranked=sorted(enumerate(final["Residuals"]),key=lambda p:-abs(p[1]))[:10]
    nodes=plan["search"]["variable_order"]
    dominant=[dict(checkpoint=["seed-step1","post-seed","1"][i//25],node=nodes[(i%25)//2] if i%25<24 else "rotor",
                   quantity="torque" if i%25==24 else "mass" if (i%25)%2==0 else "energy",scaled_residual=v) for i,v in ranked]
    write("05-adjudication.json",dict(status="PASS-INDEPENDENT-REPLAY",stop=result["stop"],evaluations=result["evaluations"],
        accepted_updates=len(result["accepted"]),batches=cursor,maximum_physical_steps=result["maximum_physical_steps"],
        initial_maximum=max(abs(v) for v in start_record()["Residuals"]),final_maximum=score(final),
        all_in_envelope=all(o["InEnvelope"] for o in final["Observations"]),dominant_residuals=dominant,
        near_bounds=[dict(index=i,node=nodes[i//2],quantity="mass" if i%2==0 else "energy",coordinate=x) for i,x in enumerate(final["Coordinates"]) if .02-abs(x)<1e-6],
        root_observed=result["root_observed"],r3_passed=False,m10_closed=False,seed_installed=False,
        next_activity="REVIEW-ROOT-BEFORE-INDEPENDENT-CONFIRMATION" if result["root_observed"] else "REVIEW-ACTIVE-BOUNDS-AND-DOMINANT-RESIDUALS"))
    print(json.dumps(load(OUT/"05-adjudication.json"),indent=2))


if __name__=="__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage",choices=["fixtures","static","preexecution","execute","adjudicate"])
    args=parser.parse_args()
    {"fixtures":lambda:print(json.dumps(fixtures(),indent=2)),"static":static,"preexecution":preexecution,"execute":execute,"adjudicate":adjudicate}[args.stage]()
