"""Verify the four-file transport repair and replay its repeated raw C4 measurements."""
import argparse
import json
import math
import subprocess

import r3_h_protocol as P

ID = "r3-stage-transport-ownership-repair1"
OUT = P.ROOT / "artifacts" / ID
CONTRACT = P.ROOT / "eng" / (ID + "-contract.json")
CLASS = "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10R3StageTransportOwnershipRepairTests"
PRODUCTION = ["src/NuclearReactorSimulator.Simulation/" + path for path in (
    "Physics/Fluids/IWaterSteamPhaseTransportPropertyProvider.cs",
    "Physics/Fluids/SimplifiedWaterSteamThermodynamicModel.cs",
    "Physics/TurbineIsland/Turbine/TurbineExpansionSolver.cs",
    "Physics/TurbineIsland/Condenser/CondenserSystemSolver.cs")]
POLICY = dict(repeats=2, maximum_recipes=2, maximum_physical_steps=8,
              maximum_inlet_ratio=.1, frozen_inlet_accumulation_w=1006560.2752685547,
              property_relative_tolerance=1e-9, ledger_energy_tolerance_w=.01, ledger_mass_tolerance_kg_s=1e-6,
              ordinary_passed=1409, ordinary_explicit_excluded=167,
              seed_installation=False, production_activation=False, r3_passed=False)


def authenticate():
    contract=P.load(CONTRACT)
    P.require(contract["schema"]==ID+"-v1" and contract["policy"]==POLICY and contract["production_files"]==PRODUCTION,"Repair scope/policy drift")
    P.require(contract["output"]=="artifacts/"+ID,"Repair output drift")
    for directory in ("src","tests"):
        P.require(P.tree(directory)==contract["implemented_baseline"][directory],"Implemented tree drift: "+directory)
        P.require(P.tree(directory,contract["allowed_changes"][directory])==contract["unchanged_trees"][directory],"Unchanged tree drift: "+directory)
    for entry in contract["implementation_files"]+contract["frozen_inputs"]:
        P.require(P.digest((P.ROOT/entry["path"]).read_bytes())==entry["sha256"],"Repair input drift: "+entry["path"])
    return contract


def close(a,b,tolerance):
    return math.isfinite(a) and math.isfinite(b) and abs(a-b)<=tolerance


def audit(records):
    P.require(len(records)==POLICY["repeats"],"Incomplete repeated measurement")
    P.require([r["Id"] for r in records]==["base/repeat/1","base/repeat/2"],"Measurement identity drift")
    P.require({**records[0],"Id":"repeat"}=={**records[1],"Id":"repeat"},"Repair repeats differ")
    plan=P.load(P.ROOT/"eng/r3-equilibrium-reconstruction-planning1-contract.json")
    for record in records:
        P.require(record["Coordinates"]==[0.]*24,"Measurement installed a different seed")
        P.H3["H2"]["verify_physics"](record,plan)
        P.require(record["Reason"]=="VALID","Repaired raw point is not healthy")
    record=records[0]
    stage,condenser=record["Stage"],record["Condenser"]
    for actual,expected in ((stage["VaporAdvectedJKg"],stage["ExpectedVaporJKg"]),
                            (stage["DrainAdvectedJKg"],stage["ExpectedDrainJKg"]),
                            (condenser["CondensateAdvectedJKg"],condenser["ExpectedCondensateJKg"])):
        P.require(close(actual,expected,POLICY["property_relative_tolerance"]*max(1.,abs(expected))),"Transport does not use active C4 properties")
    P.require(close(stage["AdmissionMassKgS"],stage["VaporMassKgS"]+stage["DrainMassKgS"],POLICY["ledger_mass_tolerance_kg_s"]),"Stage mass ledger mismatch")
    stage_error=stage["InletEnergyW"]-stage["ExhaustEnergyW"]-stage["DrainEnergyW"]-stage["ShaftPowerW"]
    P.require(abs(stage_error)<=POLICY["ledger_energy_tolerance_w"] and abs(stage["OwnershipResidualW"])<=POLICY["ledger_energy_tolerance_w"],"Stage energy ledger mismatch")
    drop=condenser["SteamAdvectedJKg"]-condenser["CondensateAdvectedJKg"]
    P.require(drop>0,"Nonpositive condenser transport gradient")
    expected_thermal=condenser["CoolingCapacityW"]/drop
    expected_flow=min(condenser["MaximumLimitKgS"],condenser["InventoryLimitKgS"],expected_thermal)
    P.require(close(condenser["ThermalLimitKgS"],expected_thermal,1e-10) and close(condenser["MassKgS"],expected_flow,1e-10),"Condensation law was not recomputed from the active energy gradient")
    P.require(close(condenser["MassKgS"]*drop,condenser["HeatRejectionW"],POLICY["ledger_energy_tolerance_w"]),"Condenser energy ledger mismatch")
    previous=record["Raw"]
    rates=[]
    for observation in record["Observations"]:
        storage=[]
        for node,before in zip(observation["Nodes"],previous):
            storage.append(dict(node=node["Id"],mass_rate_kg_s=(node["Mass"]-before["Mass"])/.01,energy_rate_w=(node["Energy"]-before["Energy"])/.01))
        inlet=next(n["energy_rate_w"] for n in storage if n["node"]=="turbine-inlet")
        P.require(abs(inlet)<=POLICY["maximum_inlet_ratio"]*POLICY["frozen_inlet_accumulation_w"],"Inlet accumulation exceeds frozen repair criterion")
        rates.append(dict(checkpoint=observation["Checkpoint"],inlet_accumulation_w=inlet,
                          inlet_ratio_to_previous=abs(inlet)/POLICY["frozen_inlet_accumulation_w"],storage=storage))
        previous=observation["Nodes"]
    running_inlet=rates[-1]["inlet_accumulation_w"]
    P.require(close(running_inlet,stage["AdmissionEnergyW"]-stage["InletEnergyW"],POLICY["ledger_energy_tolerance_w"]),"Inlet storage does not match recorded transfers")
    return dict(status="PASS-STAGE-TRANSPORT-OWNERSHIP-REPAIR",recipes=2,maximum_physical_steps=8,
                repeated_identically=True,checkpoints=rates,stage=stage,condenser=condenser,stage_energy_error_w=stage_error,
                maximum_scaled=max(map(abs,record["Residuals"])),all_in_envelope=all(o["InEnvelope"] for o in record["Observations"]),
                root_observed=max(map(abs,record["Residuals"]))<=1,
                next_activity="DIAGNOSE-REPAIRED-RAW-POINT-BEFORE-SELECTING-NEW-SEARCH-FAMILY",
                r3_passed=False,m10_closed=False,seed_installed=False,production_activation=False)


def dotnet(arguments,log):
    with (OUT/log).open("w",encoding="utf-8") as stream:
        subprocess.run(["dotnet","test","--project",P.PROJECT,"--configuration","Release","--no-build","--results-directory",str(OUT)]+arguments,
                       cwd=P.ROOT,stdout=stream,stderr=subprocess.STDOUT,check=True)


def static():
    contract=authenticate()
    P.write(OUT,"01-static.json",dict(status="PASS-STATIC",contract_sha256=P.digest(CONTRACT.read_bytes()),
                                      incoming_baseline=contract["incoming_baseline"],implemented_baseline=contract["implemented_baseline"],
                                      production_files=PRODUCTION))


def preexecution():
    authenticate()
    P.require(P.load(OUT/"01-static.json")["contract_sha256"]==P.digest(CONTRACT.read_bytes()),"Stale static review")
    verification=P.load(OUT/"verification.json")
    P.require(verification["build_exit_code"]==verification["ordinary_exit_code"]==verification["current_evidence_exit_code"]==0,"Required verification did not pass")
    P.require(verification["ordinary_passed"]==POLICY["ordinary_passed"] and verification["ordinary_excluded"]==POLICY["ordinary_explicit_excluded"] and verification["ordinary_failed"]==0,"Ordinary suite count drift")
    for name in ("build.log","ordinary-suite.log","current-evidence.log"):
        P.require(verification["sha256"][name]==P.digest((OUT/name).read_bytes()),"Stale verification log")
    dotnet(["--minimum-expected-tests","2","--","--filter-class",CLASS,"--parallel","none","--report-xunit","--report-xunit-filename","preexecution.xml"],"preexecution.log")
    P.H3["H2"]["test_report"](OUT/"preexecution.xml",2,1)
    P.write(OUT,"02-preexecution.json",dict(status="PASS-PREEXECUTION",contract_sha256=P.digest(CONTRACT.read_bytes()),assembly_sha256=P.digest(P.ASSEMBLY.read_bytes()),
                                            verification_sha256=P.digest((OUT/"verification.json").read_bytes())))


def execute():
    authenticate()
    token=P.load(OUT/"02-preexecution.json")
    P.require(token["contract_sha256"]==P.digest(CONTRACT.read_bytes()) and token["assembly_sha256"]==P.digest(P.ASSEMBLY.read_bytes())
              and token["verification_sha256"]==P.digest((OUT/"verification.json").read_bytes()),"Stale execution identity")
    P.require(not (OUT/"03-measurements.jsonl").exists(),"Existing measurement retained")
    dotnet(["--minimum-expected-tests","1","--","--explicit","only","--filter-method",CLASS+".ExecuteReviewedMeasurement",
            "--parallel","none","--report-xunit","--report-xunit-filename","execution.xml"],"execution.log")
    P.H3["H2"]["test_report"](OUT/"execution.xml",1,0)


def adjudicate():
    authenticate()
    result=audit([json.loads(line) for line in (OUT/"03-measurements.jsonl").read_text(encoding="utf-8-sig").splitlines()])
    P.write(OUT,"05-adjudication.json",result)
    print(json.dumps({k:v for k,v in result.items() if k!="checkpoints"},indent=2))
    print(json.dumps([dict(checkpoint=r["checkpoint"],inlet_accumulation_w=r["inlet_accumulation_w"]) for r in result["checkpoints"]]))


def review():
    authenticate()
    receipt=P.ROOT/"eng"/(ID+"-returned.json")
    P.require(P.load(receipt)["contract_sha256"]==P.digest(CONTRACT.read_bytes()),"Returned contract drift")
    data=P.frozen(P.ROOT/"eng/frozen-evidence"/ID,receipt)
    result=audit([json.loads(line) for line in data["03-measurements.jsonl"].decode("utf-8-sig").splitlines()])
    P.require(result==json.loads(data["05-adjudication.json"]),"Frozen repair adjudication drift")
    print(json.dumps(dict(status=result["status"],recipes=result["recipes"],maximum_scaled=result["maximum_scaled"],r3_passed=False,m10_closed=False),indent=2))


if __name__=="__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage",choices=["static","preexecution","execute","adjudicate","review"])
    {"static":static,"preexecution":preexecution,"execute":execute,"adjudicate":adjudicate,"review":review}[parser.parse_args().stage]()
