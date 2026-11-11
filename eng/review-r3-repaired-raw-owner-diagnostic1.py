"""Bounded acquisition and offline owner diagnosis of the repaired raw C4 recipe."""
from __future__ import annotations

import json
import math
import subprocess
import sys
from pathlib import Path

import r3_h_protocol as P

ID = "r3-repaired-raw-owner-diagnostic1"
PARENT = "r3-stage-transport-ownership-repair1"
ROOT, OUT = P.ROOT, P.ROOT / "artifacts" / ID
CONTRACT = ROOT / "eng" / (ID + "-contract.json")
PROJECT = "eng/diagnostics/R3RepairedRawDiagnosis/R3RepairedRawDiagnosis.csproj"
BINARY = ROOT / "eng/diagnostics/R3RepairedRawDiagnosis/bin/Release/net10.0/R3RepairedRawDiagnosis.dll"
POLICY = dict(recipes=2, runtime_steps=6, dt_s=.01, energy_tolerance_w=.01, mass_tolerance_kg_s=1e-6,
              perturbations=0, searches=0, production_changes=0, r3_passed=False, m10_closed=False)


def close(actual, expected, tolerance, label):
    P.require(math.isfinite(actual) and math.isfinite(expected) and abs(actual-expected) <= tolerance, label)
    return actual-expected


def cooling_counterfactual(m_in, energy_in, h_steam, h_liquid):
    """Fixed-state algebra; these are not new operating points or physical calls."""
    P.require(m_in > 0 and h_steam > h_liquid >= 0, "Invalid counterfactual domain")
    energy_balanced_mass = energy_in / h_steam
    return dict(mass_balanced_condensation_kg_s=m_in,
                mass_balanced_cooling_w=m_in*(h_steam-h_liquid),
                energy_residual_when_mass_balanced_w=energy_in-m_in*h_steam,
                energy_balanced_condensation_kg_s=energy_balanced_mass,
                energy_balanced_cooling_w=energy_balanced_mass*(h_steam-h_liquid),
                mass_residual_when_energy_balanced_kg_s=m_in-energy_balanced_mass,
                required_steam_specific_energy_j_kg=energy_in/m_in)


def fixtures():
    # A nonzero mass inventory rate must not be mistaken for a ledger defect.
    close(.3, 2.3-2, 1e-12, "Mass-storage fixture")
    try:
        close(0, .3, 1e-6, "Expected mass-stationarity rejection")
    except ValueError:
        pass
    else:
        raise RuntimeError("Fixture accepted false stationarity")
    f = cooling_counterfactual(2, 30, 10, 1)
    P.require(f["energy_residual_when_mass_balanced_w"] == 10 and
              f["mass_residual_when_energy_balanced_kg_s"] == -1, "Coupled cooling fixture")
    return "PASS-ALGEBRA-FIXTURES"


def parent():
    data = P.frozen(ROOT / "eng/frozen-evidence" / PARENT, ROOT / "eng" / (PARENT + "-returned.json"))
    return [json.loads(line) for line in data["03-measurements.jsonl"].splitlines()]


def authenticate(current_tree=True):
    c = P.load(CONTRACT)
    P.require(c["schema"] == ID + "-v1" and c["policy"] == POLICY, "Policy identity drift")
    if current_tree:
        for key in ("src", "tests"):
            P.require(P.tree(key) == c["unchanged_baseline"][key], "Runtime/test tree drift: " + key)
        for entry in c["implementation_files"]:
            P.require(P.digest((ROOT / entry["path"]).read_bytes()) == entry["sha256"], "Implementation drift: " + entry["path"])
    for entry in c["frozen_inputs"]:
        P.require(P.digest((ROOT / entry["path"]).read_bytes()) == entry["sha256"], "Frozen input drift")
    return c


def preexecution():
    authenticate()
    P.require(not (OUT / "03-measurements.jsonl").exists(), "Prior measurement retained")
    P.require(BINARY.exists(), "Build the isolated diagnostic first")
    assemblies = [dict(file=p.name, sha256=P.digest(p.read_bytes()))
                  for p in sorted(BINARY.parent.glob("*.dll"))]
    P.require(len(assemblies) == 4, "Unexpected diagnostic dependency surface")
    P.write(OUT, "02-preexecution.json", dict(status="PASS-PREEXECUTION", fixtures=fixtures(),
            contract_sha256=P.digest(CONTRACT.read_bytes()), assemblies=assemblies, plant_calls=0))
    return "PASS-PREEXECUTION"


def q(owner, field, unit):
    return owner[field][unit]


def analyze(records):
    P.require(len(records) == POLICY["recipes"], "Recipe count drift")
    P.require({k: v for k, v in records[0].items() if k != "Id"} ==
              {k: v for k, v in records[1].items() if k != "Id"}, "Repeatability drift")
    old = parent()
    for record, reference in zip(records, old):
        P.require(record["Id"] == reference["Id"], "Recipe identity drift")
        for side, index, sha in (("Before", 1, "BeforeStateSha256"), ("After", 2, "AfterStateSha256")):
            observation = reference["Observations"][index]
            P.require(record[sha] == observation["StateSha256"], "Complete state identity differs from repaired parent")
            P.require(observation["Healthy"] and observation["InEnvelope"], "Parent state unhealthy/outside envelope")
            a, b = record[side], observation["Nodes"]
            P.require([n["Id"] for n in a] == [n["Id"] for n in b], "Node order drift")
            for fresh, frozen in zip(a, b):
                for key in ("Mass", "Energy", "Phase", "Quality"):
                    P.require(fresh[key] == frozen[key], "Frozen inventory identity drift")
                P.require(fresh["PressurePa"] == frozen["Pressure"] and fresh["TemperatureK"] == frozen["Temperature"], "State projection drift")
    r = records[0]
    before, after = ({n["Id"]: n for n in r[key]} for key in ("Before", "After"))
    rates = {node: dict(mass_rate_kg_s=(after[node]["Mass"]-b["Mass"])/POLICY["dt_s"],
                       energy_rate_w=(after[node]["Energy"]-b["Energy"])/POLICY["dt_s"])
             for node, b in before.items()}
    heat = {node: 0. for node in before}
    heat.update({body["Id"]: 0. for body in r["Thermal"]})
    heat_ports = []
    for port in r["HeatTransfers"]:
        power = port["ConductanceWK"]*(port["FromK"]-port["ToK"])
        heat[port["FromDomainId"]] -= power
        heat[port["ToDomainId"]] += power
        heat_ports.append(dict(id=port["Id"], source=port["FromDomainId"], destination=port["ToDomainId"], power_w=power))
    nuclear = {node: 0. for node in heat}
    for group in r["ChannelOwners"]:
        nuclear[group["FuelThermalBodyId"]] += group["FuelHeatW"]
        nuclear[group["StructureThermalBodyId"]] += group["StructureHeatW"]
        nuclear[group["OutletCoolantNodeId"]] += group["CoolantHeatW"]
    for source in r["HeatSources"]:
        nuclear[source["TargetDomainId"]] += source["AppliedW"]
    thermal = []
    for body in r["Thermal"]:
        storage = (body["AfterJ"]-body["BeforeJ"])/POLICY["dt_s"]
        error = close(storage, nuclear[body["Id"]]+heat[body["Id"]], POLICY["energy_tolerance_w"], "Thermal-body ledger mismatch")
        thermal.append(dict(id=body["Id"], storage_w=storage, source_w=nuclear[body["Id"]], transfer_w=heat[body["Id"]], ledger_error_w=error))

    s, c, cooling = r["Stage"], r["Condenser"], r["Cooling"]
    m_in, e_in = q(s, "EffectiveMassFlowRate", "KilogramsPerSecond"), q(s, "ExhaustEnergyFlowRate", "Watts")
    m_out, e_out = q(c, "ActualCondensationMassFlowRate", "KilogramsPerSecond"), q(c, "SteamEnergyRemovalRate", "Watts")
    P.require(all(q(b, "MassFlowRate", "KilogramsPerSecond") == 0 and q(b, "AdvectedEnergyTransferRate", "Watts") == 0 for b in r["Bypasses"]), "Active bypass needs expanded ledger")
    P.require(not any(p["FromNodeId"] == "exhaust" or p["ToNodeId"] == "exhaust" for kind in ("Pipes", "Valves", "Pumps") for p in r[kind]), "Unaccounted exhaust hydraulic path")
    close(heat["exhaust"]+nuclear["exhaust"], 0, 0, "Unaccounted exhaust heat source")
    mass_error = close(rates["exhaust"]["mass_rate_kg_s"], m_in-m_out, POLICY["mass_tolerance_kg_s"], "Exhaust mass ledger")
    energy_error = close(rates["exhaust"]["energy_rate_w"], e_in-e_out, POLICY["energy_tolerance_w"], "Exhaust energy ledger")
    hs, hl = q(c, "SteamAdvectedSpecificEnergy", "JoulesPerKilogram"), q(c, "CondensateAdvectedSpecificEnergy", "JoulesPerKilogram")
    close(e_out, m_out*hs, POLICY["energy_tolerance_w"], "Condenser steam transport")
    capacity = q(c, "EffectiveHeatRejectionCapacity", "Watts")
    close(capacity, min(r["CoolingInstalledW"], q(cooling, "AvailableHeatRejectionPower", "Watts"),
                        q(cooling, "SurfaceHeatTransferLimitedPower", "Watts")), POLICY["energy_tolerance_w"], "Cooling capacity composition")
    close(m_out, min(q(c, "MaximumCondensationMassFlowRate", "KilogramsPerSecond"),
                    q(c, "InventoryLimitedCondensationMassFlowRate", "KilogramsPerSecond"), capacity/(hs-hl)), 1e-10, "Condensation limit law")
    close(q(c, "HeatRejectionPower", "Watts"), m_out*(hs-hl), POLICY["energy_tolerance_w"], "Condenser heat rejection ledger")
    cf = cooling_counterfactual(m_in, e_in, hs, hl)

    P.require(all(g["OutletCoolantNodeId"] == "outlet" for g in r["ChannelOwners"]), "Unexpected coolant heat target")
    outlet_heat = heat["outlet"]+nuclear["outlet"]
    # This is inferred NET advective power from actual committed storage, not an independent port measurement.
    outlet_advection = rates["outlet"]["energy_rate_w"]-outlet_heat
    return dict(status="PASS-REPAIRED-RAW-OWNER-DIAGNOSTIC", fixtures=fixtures(), recipes=2, runtime_steps=6,
                full_state_identity_with_parent=True, exact_repeats=True, rates=rates, thermal_bodies=thermal, heat_ports=heat_ports,
                outlet=dict(storage=rates["outlet"], direct_coolant_source_w=nuclear["outlet"],
                            heat_transfer_w=heat["outlet"], total_heat_input_w=outlet_heat,
                            inferred_net_advective_power_w=outlet_advection,
                            interpretation="Actual storage minus reconstructed nonhydraulic heat; individual corrected component flows are not measured",
                            historical_channel_flow_kg_s=r["HistoricalChannelFlowKgS"], historical_return_flow_kg_s=r["HistoricalReturnFlowKgS"],
                            hydraulic_numerics=r["HydraulicNumerics"]),
                exhaust=dict(storage=rates["exhaust"], incoming_mass_kg_s=m_in, outgoing_mass_kg_s=m_out,
                             incoming_energy_w=e_in, outgoing_energy_w=e_out, mass_ledger_error_kg_s=mass_error, energy_ledger_error_w=energy_error,
                             incoming_specific_energy_j_kg=e_in/m_in, node_steam_specific_energy_j_kg=hs,
                             condensate_specific_energy_j_kg=hl, effective_cooling_capacity_w=capacity,
                             installed_cooling_w=r["CoolingInstalledW"], cooling_active_limits=r["CoolingActiveLimits"],
                             fixed_state_cooling_counterfactual=cf),
                root_found=False, r3_passed=False, m10_closed=False,
                limits=["One 10 ms Running step after the same two-step preparation; not a long confirmation",
                        "No individual corrected hydraulic ports; outlet net advection is inferred from conservation",
                        "Cooling counterfactual holds the state and stage fixed; not a feasibility proof over a search box"])


def run():
    authenticate()
    token = P.load(OUT / "02-preexecution.json")
    P.require(token["contract_sha256"] == P.digest(CONTRACT.read_bytes()), "Preexecution token drift")
    P.require(not (OUT / "03-measurements.jsonl").exists(), "Prior measurement retained")
    with (OUT / "execution.log").open("x", encoding="utf-8") as log:
        subprocess.run(["dotnet", str(BINARY), str(ROOT)], cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, check=True)
    rows = [json.loads(line) for line in (OUT / "03-measurements.jsonl").read_text(encoding="utf-8").splitlines()]
    result = analyze(rows)
    P.write(OUT, "04-analysis.json", result)
    authenticate()
    P.freeze(ID, ["02-preexecution.json", "03-measurements.jsonl", "04-analysis.json", "execution.log"])
    return result["status"]


def review():
    authenticate()
    receipt = ROOT / "eng" / (ID + "-returned.json")
    P.require(P.load(receipt)["contract_sha256"] == P.digest(CONTRACT.read_bytes()), "Contract receipt drift")
    data = P.frozen(ROOT / "eng/frozen-evidence" / ID, receipt)
    result = analyze([json.loads(line) for line in data["03-measurements.jsonl"].splitlines()])
    P.require(result == json.loads(data["04-analysis.json"]), "Offline analysis drift")
    return result["status"] + " (offline replay; zero runtime steps)"


if __name__ == "__main__":
    P.require(len(sys.argv) == 2 and sys.argv[1] in ("fixtures", "preexecution", "run", "review"), "Choose fixtures/preexecution/run/review")
    print(globals()[sys.argv[1]]())
