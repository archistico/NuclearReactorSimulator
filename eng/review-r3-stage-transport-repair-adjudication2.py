"""Review the returned repair measurements using a storage-aware mass ledger."""
import argparse
import json
import math
import runpy

import r3_h_protocol as P

ID="r3-stage-transport-repair-adjudication2"
PARENT="r3-stage-transport-ownership-repair1"
OUT=P.ROOT/"artifacts"/ID
POLICY=dict(plant_calls=0,property_relative_tolerance=1e-9,mass_ledger_tolerance_kg_s=1e-6,
            energy_ledger_tolerance_w=.01,maximum_inlet_ratio=.1,frozen_inlet_accumulation_w=1006560.2752685547,
            original_measurement_not_reexecuted=True,seed_installation=False,r3_passed=False)


def payloads(identifier):
    receipt=P.ROOT/"eng"/(identifier+"-returned.json")
    P.require(P.load(receipt)["contract_sha256"]==P.digest((P.ROOT/"eng"/(identifier+"-contract.json")).read_bytes()),"Returned identity drift")
    return P.frozen(P.ROOT/"eng/frozen-evidence"/identifier,receipt)


def authenticate():
    contract=P.load(P.ROOT/"eng"/(ID+"-contract.json"))
    P.require(contract["schema"]==ID+"-v1" and contract["policy"]==POLICY,"Adjudication policy drift")
    for directory in ("src","tests"):
        P.require(P.tree(directory)==contract["implemented_baseline"][directory],"Repair tree drift: "+directory)
    for entry in contract["implementation_files"]+contract["frozen_inputs"]:
        P.require(P.digest((P.ROOT/entry["path"]).read_bytes())==entry["sha256"],"Adjudication input drift: "+entry["path"])


def require_mass_ledger(storage,inflow,outflow):
    error=storage-(inflow-outflow)
    P.require(math.isfinite(error) and abs(error)<=POLICY["mass_ledger_tolerance_kg_s"],"Mass storage/transfer inconsistency")
    return error


def fixtures():
    require_mass_ledger(0.,2.,2.)
    require_mass_ledger(.25,2.,1.75)
    try:
        require_mass_ledger(0.,2.,1.75)
    except ValueError:
        pass
    else:
        raise ValueError("Missing mass storage accepted")
    return [dict(name=name,passed=True) for name in ("stationary-closed-ledger","nonstationary-closed-ledger","unaccounted-mass-rejected")]


def analyze():
    data=payloads(PARENT)
    original=json.loads(data["05-adjudication.json"])
    records=[json.loads(line) for line in data["03-measurements.jsonl"].decode("utf-8-sig").splitlines()]
    P.require(len(records)==2 and {**records[0],"Id":"repeat"}=={**records[1],"Id":"repeat"},"Incomplete/nonidentical returned measurements")
    v1=runpy.run_path(str(P.ROOT/"eng/run-r3-stage-transport-ownership-repair1.py"))
    try:
        v1["audit"](records)
    except ValueError as ex:
        P.require(str(ex)==original["failed_criterion"]=="Stage mass ledger mismatch","Original failure changed")
    else:
        raise ValueError("Original nonconforming criterion unexpectedly passed")
    P.require(original["original_protocol_not_passed"] and original["status"]=="NONCONFORMING-ORIGINAL-MASS-STATIONARITY-CRITERION","Original result rewritten")
    plan=P.load(P.ROOT/"eng/r3-equilibrium-reconstruction-planning1-contract.json")
    for record in records:
        P.require(record["Coordinates"]==[0.]*24 and record["Reason"]=="VALID","Unhealthy/changed raw recipe")
        P.H3["H2"]["verify_physics"](record,plan)
    record=records[0]
    stage,cond=record["Stage"],record["Condenser"]
    relative=POLICY["property_relative_tolerance"]
    for actual,expected in ((stage["VaporAdvectedJKg"],stage["ExpectedVaporJKg"]),
                            (stage["DrainAdvectedJKg"],stage["ExpectedDrainJKg"]),
                            (cond["CondensateAdvectedJKg"],cond["ExpectedCondensateJKg"])):
        P.require(v1["close"](actual,expected,relative*max(1.,abs(expected))),"Transport property mismatch")
    rows=[]
    previous=record["Raw"]
    for observation in record["Observations"]:
        rates=[dict(node=node["Id"],mass_rate_kg_s=(node["Mass"]-before["Mass"])/.01,
                    energy_rate_w=(node["Energy"]-before["Energy"])/.01) for node,before in zip(observation["Nodes"],previous)]
        inlet=next(row for row in rates if row["node"]=="turbine-inlet")
        P.require(abs(inlet["energy_rate_w"])<=POLICY["maximum_inlet_ratio"]*POLICY["frozen_inlet_accumulation_w"],"Inlet energy repair criterion failed")
        rows.append(dict(checkpoint=observation["Checkpoint"],storage=rates,inlet=inlet,in_envelope=observation["InEnvelope"]))
        previous=observation["Nodes"]
    running=rows[-1]["inlet"]
    mass_error=require_mass_ledger(running["mass_rate_kg_s"],stage["AdmissionMassKgS"],stage["VaporMassKgS"]+stage["DrainMassKgS"])
    tolerance=POLICY["energy_ledger_tolerance_w"]
    stage_error=stage["InletEnergyW"]-stage["ExhaustEnergyW"]-stage["DrainEnergyW"]-stage["ShaftPowerW"]
    P.require(abs(stage_error)<=tolerance and abs(stage["OwnershipResidualW"])<=tolerance,"Stage energy imbalance")
    inlet_error=running["energy_rate_w"]-(stage["AdmissionEnergyW"]-stage["InletEnergyW"])
    P.require(abs(inlet_error)<=tolerance,"Inlet energy storage/transfer inconsistency")
    drop=cond["SteamAdvectedJKg"]-cond["CondensateAdvectedJKg"]
    P.require(drop>0,"Invalid condensation gradient")
    thermal=cond["CoolingCapacityW"]/drop
    actual=min(cond["MaximumLimitKgS"],cond["InventoryLimitKgS"],thermal)
    P.require(abs(cond["ThermalLimitKgS"]-thermal)<=1e-10 and abs(cond["MassKgS"]-actual)<=1e-10,"Condensation limit law mismatch")
    cond_error=cond["MassKgS"]*drop-cond["HeatRejectionW"]
    P.require(abs(cond_error)<=tolerance,"Condenser energy ledger mismatch")
    return dict(status="PASS-STAGE-TRANSPORT-OWNERSHIP-REPAIR-ADJUDICATION2",plant_calls=0,returned_recipes=2,
                fixtures=fixtures(),original_protocol_result=original,original_result_rewritten=False,
                criterion_correction="Compare mass storage with inflow minus outflow; do not require stationarity of the raw point",
                checkpoint_results=rows,stage=stage,condenser=cond,mass_ledger_error_kg_s=mass_error,
                inlet_energy_ledger_error_w=inlet_error,stage_energy_ledger_error_w=stage_error,condenser_energy_ledger_error_w=cond_error,
                running_inlet_ratio=abs(running["energy_rate_w"])/POLICY["frozen_inlet_accumulation_w"],
                maximum_scaled=max(map(abs,record["Residuals"])),all_in_envelope=all(row["in_envelope"] for row in rows),
                root_observed=max(map(abs,record["Residuals"]))<=1,
                next_activity="DIAGNOSE-REPAIRED-RAW-POINT-BEFORE-SELECTING-NEW-SEARCH-FAMILY",
                r3_passed=False,m10_closed=False,seed_installed=False,production_activation=False)


def execute():
    authenticate()
    result=analyze()
    P.write(OUT,"01-analysis.json",result)
    print(json.dumps(dict(status=result["status"],running_inlet_ratio=result["running_inlet_ratio"],
                          maximum_scaled=result["maximum_scaled"],all_in_envelope=result["all_in_envelope"],
                          mass_ledger_error_kg_s=result["mass_ledger_error_kg_s"]),indent=2))


def review():
    authenticate()
    result=analyze()
    P.require(result==json.loads(payloads(ID)["01-analysis.json"]),"Returned adjudication drift")
    print(json.dumps(dict(status=result["status"],running_inlet_ratio=result["running_inlet_ratio"],plant_calls=0,r3_passed=False,m10_closed=False),indent=2))


if __name__=="__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage",choices=["execute","review"])
    {"execute":execute,"review":review}[parser.parse_args().stage]()
