"""Re-evaluate frozen non-fluid columns with a matching energy minimax objective."""
import argparse
import json
import math
import runpy

import r3_h_protocol as P
import r3_h_minimax_lp as LP

ID="r3-op-energy-objective-review2"
OUT=P.ROOT/"artifacts"/ID
POLICY=dict(plant_calls=0,inventory_bound=.02,nonfluid_bound=.01,energy_rows=36,material_ratio=.5,
            objective="max-absolute-energy-rate",production_change=False,root_qualification=False)
R=runpy.run_path(str(P.ROOT/"eng/review-r3-op-bounded-combination1.py"))


def authenticate():
    contract=P.load(P.ROOT/"eng"/(ID+"-contract.json"))
    P.require(contract["schema"]==ID+"-v1" and contract["policy"]==POLICY,"Objective-review policy drift")
    # This is an offline review of historical observations. Bind its numerical
    # inputs and implementation, rather than the unrelated current runtime tree.
    for entry in contract["implementation_files"]+contract["frozen_inputs"]:
        P.require(P.digest((P.ROOT/entry["path"]).read_bytes())==entry["sha256"],"Objective-review input drift: "+entry["path"])


def analyze():
    base,inventory,nonfluid=R["columns"]()
    _,nodes,scales,names=R["REVIEW"]["layout"]()
    indices=[i for i,name in enumerate(names) if name[2]=="energy"]
    P.require(len(indices)==POLICY["energy_rows"],"Energy row count drift")
    labels=[f"{node}:{q}" for node in nodes for q in ("mass","energy")]
    models=[]
    for tag,columns,bounds,order in (
        ("inventories",inventory,[.02]*24,labels),
        ("all-nonfluid",inventory+list(nonfluid.values()),[.02]*24+[.01]*6,labels+list(nonfluid))):
        matrix=[[column[i]*scales[i]*bound/1e6 for column,bound in zip(columns,bounds)] for i in indices]
        values=[base["Residuals"][i]*scales[i]/1e6 for i in indices]
        shifted=[value-math.fsum(row) for value,row in zip(values,matrix)]
        n=len(columns)
        constraints=[row+[-1.] for row in matrix]+[[-v for v in row]+[-1.] for row in matrix]
        constraints += [[float(j==k) for j in range(n)]+[0.] for k in range(n)]
        rhs=[-v for v in shifted]+shifted+[2.]*n
        result=LP.linear_program(constraints,rhs,[0.]*n+[-1.])
        P.require(result["status"]=="OPTIMAL-CERTIFIED","Uncertified energy minimax")
        delta=[bound*(v-1.) for bound,v in zip(bounds,result["primal"])]
        P.require(all(abs(d)<=bound+1e-12 for d,bound in zip(delta,bounds)),"Minimax step outside reviewed bounds")
        predicted=[value+math.fsum(column[i]*d for column,d in zip(columns,delta)) for i,value in enumerate(base["Residuals"])]
        metric=R["REVIEW"]["metric"](predicted,scales,names)
        maximum=abs(metric["maximum_energy_rate_w"]["value"])
        P.require(abs(maximum-result["primal"][-1]*1e6)<=.01,"Minimax reconstruction mismatch")
        models.append(dict(model=tag,maximum_energy_rate_w=maximum,metric=metric,
                           certificate={k:result[k] for k in ("status","primal_violation","dual_violation","duality_gap")},
                           step=[dict(coordinate=name,fraction=d,bound=b) for name,d,b in zip(order,delta,bounds)],
                           linear_prediction_only=True,qualified_seed=False))
    ratio=models[1]["maximum_energy_rate_w"]/models[0]["maximum_energy_rate_w"]
    return dict(status="PASS-CONSISTENT-ENERGY-OBJECTIVE-REVIEW",plant_calls=0,fixtures=LP.fixtures(),models=models,
                energy_maximum_ratio=ratio,halving_observed_in_linear_model=ratio<=POLICY["material_ratio"],
                interpretation="The earlier L2 fits do not bound the energy maximum. These energy-only minima retain mass/torque residuals and are not equilibrium candidates.",
                nonlinear_infeasibility_proven=False,old_frozen_classification_rewritten=False,
                family_selected=False,r3_passed=False,m10_closed=False,seed_installed=False)


def execute():
    authenticate()
    result=analyze()
    P.write(OUT,"01-analysis.json",result)
    print(json.dumps(dict(status=result["status"],energy_maximum_ratio=result["energy_maximum_ratio"],
                          maxima_w=[m["maximum_energy_rate_w"] for m in result["models"]]),indent=2))


def review():
    authenticate()
    receipt=P.ROOT/"eng"/(ID+"-returned.json")
    P.require(P.load(receipt)["contract_sha256"]==P.digest((P.ROOT/"eng"/(ID+"-contract.json")).read_bytes()),"Returned identity drift")
    data=P.frozen(P.ROOT/"eng/frozen-evidence"/ID,receipt)
    result=analyze()
    P.require(result==json.loads(data["01-analysis.json"]),"Frozen energy-objective review drift")
    print(json.dumps(dict(status=result["status"],energy_maximum_ratio=result["energy_maximum_ratio"],plant_calls=0),indent=2))


if __name__=="__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage",choices=["execute","review"])
    {"execute":execute,"review":review}[parser.parse_args().stage]()
