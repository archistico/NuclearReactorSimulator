"""Test-only reconstruction of the turbine-stage admission energy law at the frozen mode-2 baseline, evaluated on the committed step state; changes no production code."""
import argparse
import gzip
import json
import os
import subprocess

import r3_h_protocol as P
import runpy

ID = "r3-stage-energy-law-reconstruction2"
OUT = P.ROOT / "artifacts" / ID
CONTRACT = P.ROOT / "eng" / (ID + "-contract.json")
GAMEPLAY = "tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/"
EXECUTOR = GAMEPLAY + "M10R3StageEnergyLawReconstructionTests.cs"
CLASS = "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10R3StageEnergyLawReconstructionTests"
REVIEW = runpy.run_path(str(P.ROOT / "eng/review-r3-h-family-sufficiency1.py"))
POLICY = dict(plant_calls=1, checkpoint="1", node="turbine-inlet", explained_fraction=.1, partial_fraction=.5,
              identity_relative_tolerance=1e-9, production_change=False, family_selected=False,
              seed_installation=False, r3_passed=False)


def authenticate():
    c = P.load(CONTRACT)
    P.require(c["schema"] == ID + "-v1" and c["policy"] == POLICY and c["output"] == "artifacts/" + ID, "Frozen protocol policy drift")
    P.require(c["test_additions"] == [] and c["test_modifications"] == [EXECUTOR], "Unreviewed test surface")
    P.require(P.tree("tests", (EXECUTOR,)) == c["unchanged_tests_baseline"], "Unchanged test tree drift")
    for key in ("src", "tests"):
        P.require(P.tree(key) == c["incoming_baseline"][key], "Incoming tree drift: " + key)
    for entry in c["implementation_files"] + c["frozen_inputs"]:
        P.require(P.digest((P.ROOT / entry["path"]).read_bytes()) == entry["sha256"], "Input drift: " + entry["path"])
    return c


def reference():
    with gzip.open(P.ROOT / REVIEW["FEASIBILITY"], "rt", encoding="utf-8-sig") as stream:
        base = json.loads(next(line for line in stream if line.strip()))
    P.require(base["Purpose"] == "initial" and base["Coordinates"] == [0.] * 24 and base["Reason"] == "VALID", "Reference record drift")
    return base


def close(a, b):
    return abs(a - b) <= POLICY["identity_relative_tolerance"] * max(abs(a), abs(b), 1.)


def audit(record):
    _, nodes, scales, names = REVIEW["layout"]()
    P.require(record["Protocol"] == ID and record["Checkpoint"] == POLICY["checkpoint"], "Reconstruction identity drift")
    P.require(record["Residuals"] == reference()["Residuals"], "Frozen mode-2 baseline not reproduced")
    stage, admission, condenser = record["Stage"], record["Admission"], record["Condenser"]
    P.require(stage["InletNodeId"] == admission["TurbineInletNodeId"] == POLICY["node"] and stage["InletPhase"] == "SaturatedMixture", "Stage inlet drift")
    index = names.index((POLICY["checkpoint"], POLICY["node"], "energy"))
    observed = record["Residuals"][index] * scales[index]
    P.require(close(admission["MassKgS"], stage["VaporMassKgS"] + stage["DrainMassKgS"]), "Turbine-inlet mass ledger not closed")
    P.require(close(observed, admission["EnergyW"] - stage["InletEnergyW"]), "Turbine-inlet energy ledger not reproduced")
    providers, x = stage["Providers"], stage["InletQuality"]
    hist, act = providers["Historical"], providers["ActiveClosure"]
    uses = lambda p: close(stage["VaporAdvectedJKg"], p["VaporSelectedJKg"]) and close(stage["DrainAdvectedJKg"], p["LiquidSelectedJKg"])
    uses_historical, uses_active = uses(hist), uses(act)
    outflow = lambda p: stage["VaporMassKgS"] * p["VaporSelectedJKg"] + stage["DrainMassKgS"] * p["LiquidSelectedJKg"]
    counterfactual = admission["EnergyW"] - outflow(act)
    ratio = abs(counterfactual) / abs(observed)
    mixture = lambda p: x * p["VaporSelectedJKg"] + (1 - x) * p["LiquidSelectedJKg"]
    admission_specific = admission["EnergyW"] / admission["MassKgS"]
    if not uses_historical or uses_active:
        classification = "NO-STAGE-ADMISSION-CLOSURE-INCOHERENCE"
    elif ratio <= POLICY["explained_fraction"]:
        classification = "STAGE-ADMISSION-CLOSURE-INCOHERENCE-DEMONSTRATED"
    elif ratio <= POLICY["partial_fraction"]:
        classification = "STAGE-ADMISSION-CLOSURE-INCOHERENCE-PARTIAL"
    else:
        classification = "STAGE-ADMISSION-HISTORICAL-SATURATION-NOT-EXPLANATORY"
    cond = condenser["Providers"]
    return dict(status="PASS-RECONSTRUCTION-COMPLETE", classification=classification, baseline_reproduced=True, state_sha256=record["StateSha256"],
                stage=dict(energy_transport_mode=stage["EnergyTransportMode"], inlet_quality=x, inlet_pressure_pa=stage["InletPressurePa"],
                           vapor_mass_kg_s=stage["VaporMassKgS"], drain_mass_kg_s=stage["DrainMassKgS"],
                           node_advected_j_kg=stage["InletAdvectedJKg"], admission_advected_j_kg=admission_specific,
                           recorded_vapor_j_kg=stage["VaporAdvectedJKg"], recorded_drain_j_kg=stage["DrainAdvectedJKg"],
                           historical_saturation=hist, active_closure_transport=act,
                           uses_historical_saturation=uses_historical, uses_active_closure_transport=uses_active,
                           node_minus_historical_mixture_j_kg=stage["InletAdvectedJKg"] - mixture(hist),
                           node_minus_active_mixture_j_kg=stage["InletAdvectedJKg"] - mixture(act),
                           admission_minus_recorded_outflow_j_kg=admission_specific - stage["InletEnergyW"] / admission["MassKgS"]),
                turbine_inlet=dict(observed_accumulation_w=observed, recorded_stage_outflow_w=stage["InletEnergyW"],
                                   active_closure_outflow_w=outflow(act), counterfactual_accumulation_w=counterfactual,
                                   counterfactual_to_observed=ratio, counterfactual_is_not_an_observation=True),
                condenser=dict(energy_transport_mode=condenser["EnergyTransportMode"], condensation_kg_s=condenser["CondensationKgS"],
                               recorded_condensate_j_kg=condenser["CondensateAdvectedJKg"],
                               historical_liquid_j_kg=cond["Historical"]["LiquidSelectedJKg"], active_liquid_j_kg=cond["ActiveClosure"]["LiquidSelectedJKg"],
                               uses_historical_saturation=close(condenser["CondensateAdvectedJKg"], cond["Historical"]["LiquidSelectedJKg"]),
                               active_minus_recorded_transfer_w=condenser["CondensationKgS"] * (cond["ActiveClosure"]["LiquidSelectedJKg"] - condenser["CondensateAdvectedJKg"]),
                               reported_only=True),
                next_activity=("PLAN-CLOSURE-CONSISTENT-STAGE-ADMISSION-TRANSPORT-CHANGE" if classification.endswith("DEMONSTRATED")
                               else "REVIEW-RECONSTRUCTION-BEFORE-ANY-PHYSICS-CHANGE"),
                production_change=False, family_selected=False, r3_passed=False, m10_closed=False, seed_installed=False)


def dotnet(arguments, log):
    command = ["dotnet", "test", "--project", P.PROJECT, "--configuration", "Release", "--no-build", "--results-directory", str(OUT)] + arguments
    with (OUT / log).open("w", encoding="utf-8") as stream:
        subprocess.run(command, cwd=P.ROOT, env=dict(os.environ, NRS_R3_STAGE_PROTOCOL=ID), stdout=stream, stderr=subprocess.STDOUT, check=True)


def static():
    authenticate()
    reference()
    P.write(OUT, "01-static.json", dict(status="PASS-STATIC", contract_sha256=P.digest(CONTRACT.read_bytes())))


def preexecution():
    authenticate()
    P.require(P.load(OUT / "01-static.json")["contract_sha256"] == P.digest(CONTRACT.read_bytes()), "Stale static review")
    dotnet(["--minimum-expected-tests", "2", "--", "--filter-class", CLASS, "--parallel", "none",
            "--report-xunit", "--report-xunit-filename", "preexecution.xml"], "preexecution.log")
    P.H3["H2"]["test_report"](OUT / "preexecution.xml", 2, 1)
    P.write(OUT, "02-preexecution.json", dict(status="PASS-PREEXECUTION", contract_sha256=P.digest(CONTRACT.read_bytes()),
                                              assembly_sha256=P.digest(P.ASSEMBLY.read_bytes())))


def execute():
    authenticate()
    review = P.load(OUT / "02-preexecution.json")
    P.require(review["contract_sha256"] == P.digest(CONTRACT.read_bytes()) and review["assembly_sha256"] == P.digest(P.ASSEMBLY.read_bytes()), "Stale execution identity")
    P.require(not (OUT / "03-reconstruction.json").exists(), "Existing execution retained; never overwrite")
    dotnet(["--minimum-expected-tests", "1", "--", "--explicit", "only", "--filter-method", CLASS + ".ExecuteReviewedReconstruction",
            "--parallel", "none", "--report-xunit", "--report-xunit-filename", "execution.xml"], "execution.log")
    P.H3["H2"]["test_report"](OUT / "execution.xml", 1, 0)


def adjudicate():
    authenticate()
    result = audit(json.loads((OUT / "03-reconstruction.json").read_text(encoding="utf-8-sig")))
    P.write(OUT, "05-adjudication.json", result)
    print(json.dumps(dict(classification=result["classification"], turbine_inlet=result["turbine_inlet"],
                          uses_historical=result["stage"]["uses_historical_saturation"], next_activity=result["next_activity"]), indent=2))


def review():
    authenticate()
    receipt = P.ROOT / "eng" / (ID + "-returned.json")
    P.require(P.load(receipt)["contract_sha256"] == P.digest(CONTRACT.read_bytes()), "Contract identity drift")
    data = P.frozen(P.ROOT / "eng/frozen-evidence" / ID, receipt)
    result = audit(json.loads(data["03-reconstruction.json"].decode("utf-8-sig")))
    P.require(result == json.loads(data["05-adjudication.json"]), "Frozen reconstruction adjudication drift")
    print(json.dumps(dict(status=result["status"], classification=result["classification"]), indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage", choices=["static", "preexecution", "execute", "adjudicate", "review"])
    {"static": static, "preexecution": preexecution, "execute": execute, "adjudicate": adjudicate, "review": review}[parser.parse_args().stage]()
