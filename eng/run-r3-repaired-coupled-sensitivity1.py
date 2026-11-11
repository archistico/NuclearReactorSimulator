"""Repeated, bounded coupled probes on repaired C4; no search or production change."""
from __future__ import annotations

import json
import math
import subprocess
import sys

import r3_h_protocol as P

ID = "r3-repaired-coupled-sensitivity1"
PARENT = "r3-stage-transport-ownership-repair1"
OWNER = "r3-repaired-raw-owner-diagnostic1"
ROOT, OUT = P.ROOT, P.ROOT / "artifacts" / ID
CONTRACT = ROOT / "eng" / (ID + "-contract.json")
PROJECT = "eng/diagnostics/R3CoupledSensitivity/R3CoupledSensitivity.csproj"
BINARY = ROOT / "eng/diagnostics/R3CoupledSensitivity/bin/Release/net10.0/R3CoupledSensitivity.dll"
POLICY = dict(recipes=82, repeats=2, maximum_runtime_steps=248, amplitudes=[1e-5, 1e-4],
              primary_amplitude=1e-4, reference_amplitude=1e-5, relative_linearity_tolerance=.1,
              energy_ledger_tolerance_w=.01, mass_ledger_tolerance_kg_s=1e-6,
              physical_weights=dict(mass_kg_s=1., energy_w=1e6, torque_nm=1., thermal_w=1e6),
              require_same_phases=True, require_same_hydraulic_regime=True, require_same_cooling_limits=True,
              require_in_envelope=True, proposals=0, searches=0, production_changes=0,
              root_criterion_unchanged=True, r3_passed=False, m10_closed=False)
INVENTORIES = [("pressure:mass", 14), ("pressure:energy", 15), ("outlet:mass", 12),
               ("outlet:energy", 13), ("exhaust:mass", 4), ("exhaust:energy", 5)]
OPERATING = [("neutron-population", 3), ("fuel-temperature-celsius", 4), ("structure-temperature-celsius", 5)]


def frozen(protocol):
    return P.frozen(ROOT / "eng/frozen-evidence" / protocol, ROOT / "eng" / (protocol + "-returned.json"))


def directions():
    owner = json.loads(frozen(OWNER)["03-measurements.jsonl"].splitlines()[0])
    transfers = {p["FromDomainId"]: p for p in owner["HeatTransfers"]}
    groups = owner["ChannelOwners"]
    initial = [305.62514906467646, 289.13956081139787]
    coefficients = [sum(g[key] for g in groups) / transfers[body]["ConductanceWK"] / temperature
                    for key, body, temperature in zip(("FuelHeatW", "StructureHeatW"), ("fuel", "structure"), initial)]
    result = []
    for name, index in INVENTORIES:
        coordinates = [0.] * 24
        coordinates[index] = 1.
        result.append(dict(name=name, coordinates=coordinates, operating=[0.] * 6, independent=True))
    for name, index in OPERATING:
        operating = [0.] * 6
        operating[index] = 1.
        result.append(dict(name=name, coordinates=[0.] * 24, operating=operating, independent=True))
    result.append(dict(name="coupled-nuclear-thermal", coordinates=[0.] * 24,
                       operating=[0., 0., 0., 1., *coefficients], independent=False))
    return result


def requests():
    points = [dict(Id="base", Coordinates=[0.] * 24, OperatingPointFractions=[0.] * 6)]
    for direction in directions():
        for amplitude in POLICY["amplitudes"]:
            for sign, tag in ((1, "plus"), (-1, "minus")):
                points.append(dict(Id=f'{direction["name"]}/{tag}/{amplitude:g}',
                                   Coordinates=[sign * amplitude * x for x in direction["coordinates"]],
                                   OperatingPointFractions=[sign * amplitude * x for x in direction["operating"]]))
    return [{**point, "Id": point["Id"] + f"/repeat/{repeat}"} for point in points for repeat in (1, 2)]


def layout():
    plan = P.load(ROOT / "eng/r3-equilibrium-reconstruction-planning1-contract.json")
    scales = [float(row[key]) for row in plan["residual_scales"] for key in ("mass_scale_kg_s", "energy_scale_w")]
    scales.append(float(plan["rotor_torque_scale_nm"]))
    names = [(checkpoint, node, kind) for checkpoint in ("seed-step1", "post-seed", "1")
             for node, kind in [(row["node"], kind) for row in plan["residual_scales"] for kind in ("mass", "energy")] + [("rotor", "torque")]]
    return plan, scales * 3, names


def norm(vector):
    return math.sqrt(math.fsum(x*x for x in vector))


def relative(actual, expected):
    denominator = norm(expected)
    P.require(denominator > 0, "Undefined relative comparison")
    return norm([a-b for a, b in zip(actual, expected)]) / denominator


def fixtures():
    plus, minus, h = [4., 8.], [0., 0.], 2.
    P.require([(a-b)/(2*h) for a, b in zip(plus, minus)] == [1., 2.], "Central-difference fixture")
    P.require(relative([2., 4.], [2., 4.]) == 0 and abs(relative([2.2, 4.4], [2., 4.])-.1) < 1e-14, "Linearity fixture")
    P.require(cosine([1., 0.], [0., 1.]) == 0 and cosine([1., 0.], [2., 0.]) == 1, "Cosine fixture")
    P.require(len(requests()) == POLICY["recipes"] and len({r["Id"] for r in requests()}) == 82, "Request budget fixture")
    return "PASS-COUPLED-SENSITIVITY-FIXTURES"


def authenticate():
    contract = P.load(CONTRACT)
    P.require(contract["schema"] == ID + "-v1" and contract["policy"] == POLICY and contract["directions"] == directions(), "Policy or direction drift")
    for key in ("src", "tests"):
        P.require(P.tree(key) == contract["unchanged_baseline"][key], "Runtime/test tree drift: " + key)
    for entry in contract["implementation_files"] + contract["frozen_inputs"]:
        P.require(P.digest((ROOT / entry["path"]).read_bytes()) == entry["sha256"], "Input drift: " + entry["path"])
    return contract


def preexecution():
    authenticate()
    P.require(not (OUT / "03-measurements.jsonl").exists(), "Acquisition already exists")
    P.write(OUT, "01-requests.json", requests())
    assemblies = [dict(file=p.name, sha256=P.digest(p.read_bytes())) for p in sorted(BINARY.parent.glob("*.dll"))]
    P.require(BINARY.exists() and len(assemblies) >= 5, "Build isolated console first")
    P.write(OUT, "02-preexecution.json", dict(status="PASS-PREEXECUTION", contract_sha256=P.digest(CONTRACT.read_bytes()),
            requests_sha256=P.digest((OUT / "01-requests.json").read_bytes()), assemblies=assemblies, fixtures=fixtures(), runtime_steps=0))
    subprocess.run(["dotnet", str(BINARY), str(ROOT), "--validate-only"], cwd=ROOT, check=True)
    return "PASS-PREEXECUTION"


def regime(record):
    return [(o["Extra"]["HydraulicNumerics"]["Mode"], o["Extra"]["HydraulicNumerics"]["UsedSemiImplicitCorrection"],
             o["Extra"]["HydraulicNumerics"]["FourNodeBranchContinuity"]["TriggerObserved"],
             o["Extra"]["HydraulicNumerics"]["FourNodeBranchContinuity"]["CorrectedCandidateCommitted"],
             o["Extra"]["Condenser"]["ActiveCondensationLimits"], o["Extra"]["Condenser"]["CoolingActiveLimits"])
            for o in record["Observations"]]


def thermal_vector(record):
    return [(now["EnergyJ"]-before["EnergyJ"])/.01 for o in record["Observations"]
            for before, now in zip(o["Extra"]["ThermalBefore"], o["Extra"]["ThermalAfter"])]


def weighted_vector(record, scales, names, include_thermal=True):
    weights = POLICY["physical_weights"]
    values = [v*s / weights[{"mass": "mass_kg_s", "energy": "energy_w", "torque": "torque_nm"}[name[2]]]
              for v, s, name in zip(record["Residuals"], scales, names)]
    return values + [x/weights["thermal_w"] for x in thermal_vector(record)] if include_thermal else values


def compatible(record, baseline):
    return record["Residuals"] is not None and record["Reason"] == "VALID" and all(o["InEnvelope"] for o in record["Observations"]) \
        and record["Phases"] == baseline["Phases"] and regime(record) == regime(baseline)


def close(actual, expected, tolerance, label):
    P.require(math.isfinite(actual) and math.isfinite(expected) and abs(actual-expected) <= tolerance, label)
    return actual-expected


def verify_extra(record):
    if record["Residuals"] is None:
        return []
    errors = []
    previous = {n["Id"]: n for n in record["Raw"]}
    for observation in record["Observations"]:
        e = observation["Extra"]
        now = {n["Id"]: n for n in observation["Nodes"]}
        P.require([b["Id"] for b in e["ThermalBefore"]] == ["fuel", "structure"] == [b["Id"] for b in e["ThermalAfter"]], "Thermal owner drift")
        P.require(len(e["ChannelHeat"]) == 1 and {h["FromDomainId"] for h in e["HeatTransfers"]} == {"fuel", "structure"}
                  and all(h["ToDomainId"] == "outlet" for h in e["HeatTransfers"]), "Heat paths need expanded audit")
        channels = e["ChannelHeat"][0]
        for before_body, after_body in zip(e["ThermalBefore"], e["ThermalAfter"]):
            port = next(h for h in e["HeatTransfers"] if h["FromDomainId"] == before_body["Id"])
            close(port["FromK"], before_body["TemperatureK"], 0, "Heat-transfer source state drift")
            close(port["ToK"], previous["outlet"]["Temperature"], 0, "Heat-transfer fluid state drift")
            heat = port["ConductanceWK"] * (port["FromK"]-port["ToK"])
            rate = (after_body["EnergyJ"]-before_body["EnergyJ"])/.01
            errors.append(close(rate, channels["FuelW" if before_body["Id"] == "fuel" else "StructureW"]-heat,
                                POLICY["energy_ledger_tolerance_w"], "Thermal-body ledger mismatch"))
        s, c = e["Stage"], e["Condenser"]
        P.require(s["ExhaustNodeId"] == c["SteamSpaceNodeId"] == "exhaust", "Exhaust owner drift")
        mass_rate = (now["exhaust"]["Mass"]-previous["exhaust"]["Mass"])/.01
        energy_rate = (now["exhaust"]["Energy"]-previous["exhaust"]["Energy"])/.01
        close(mass_rate, s["VaporMassKgS"]+e["BypassMassKgS"]-c["MassKgS"], POLICY["mass_ledger_tolerance_kg_s"], "Exhaust mass ledger mismatch")
        errors.append(close(energy_rate, s["ExhaustEnergyW"]+e["BypassEnergyW"]-c["SteamEnergyW"], POLICY["energy_ledger_tolerance_w"], "Exhaust energy ledger mismatch"))
        errors.append(close(c["HeatRejectionW"], c["MassKgS"]*(c["SteamAdvectedJKg"]-c["CondensateAdvectedJKg"]), POLICY["energy_ledger_tolerance_w"], "Condensation heat ledger"))
        previous = now
    return errors


def cosine(a, b):
    return math.fsum(x*y for x, y in zip(a, b)) / (norm(a)*norm(b))


def analyze(records):
    expected = requests()
    P.require(len(records) == len(expected) == POLICY["recipes"], "Recipe budget drift")
    plan, scales, names = layout()
    points, errors = {}, []
    for i in range(0, len(expected), 2):
        pair = records[i:i+2]
        for request, record in zip(expected[i:i+2], pair):
            P.require(all(record[key] == request[key] for key in request), "Request/response drift")
            P.H3["H2"]["verify_physics"](record, plan)
            errors += verify_extra(record)
        P.require({k:v for k,v in pair[0].items() if k != "Id"} == {k:v for k,v in pair[1].items() if k != "Id"}, "Nonidentical repeated recipe")
        points[pair[0]["Id"].rsplit("/repeat/", 1)[0]] = pair[0]
    base = points["base"]
    parent = json.loads(frozen(PARENT)["03-measurements.jsonl"].splitlines()[0])
    for key in ("Raw", "Residuals", "Phases"):
        P.require(base[key] == parent[key], "Repaired parent identity drift: " + key)
    P.require([o["StateSha256"] for o in base["Observations"]] == [o["StateSha256"] for o in parent["Observations"]], "Complete baseline state drift")
    summaries, columns = [], {}
    for direction in directions():
        samples, usable = [], {}
        for h in POLICY["amplitudes"]:
            plus = points[f'{direction["name"]}/plus/{h:g}']
            minus = points[f'{direction["name"]}/minus/{h:g}']
            ok = compatible(plus, base) and compatible(minus, base)
            entry = dict(amplitude=h, central_usable=ok, plus_reason=plus["Reason"], minus_reason=minus["Reason"],
                         plus_in_envelope=all(o["InEnvelope"] for o in plus.get("Observations", [])) if plus["Residuals"] else False,
                         minus_in_envelope=all(o["InEnvelope"] for o in minus.get("Observations", [])) if minus["Residuals"] else False,
                         phases_match=plus.get("Phases") == base["Phases"] == minus.get("Phases"),
                         regime_match=regime(plus) == regime(base) == regime(minus) if plus["Residuals"] and minus["Residuals"] else False)
            if ok:
                column = [(a-b)/(2*h) for a,b in zip(weighted_vector(plus, scales, names), weighted_vector(minus, scales, names))]
                usable[h] = column
                original = [(a-b)/(2*h) for a,b in zip(plus["Residuals"], minus["Residuals"])]
                energy = [(column[i]*POLICY["physical_weights"]["energy_w"]*h, names[i]) for i in range(75) if names[i][2] == "energy"]
                entry.update(energy_response_at_plus_amplitude=[dict(checkpoint=n[0], node=n[1], delta_w=v) for v,n in energy],
                             thermal_response_at_plus_amplitude_w=[x*POLICY["physical_weights"]["thermal_w"]*h for x in column[75:]],
                             mass_response_at_plus_amplitude_kg_s=[dict(checkpoint=n[0], node=n[1], delta_kg_s=column[i]*h)
                                 for i,n in enumerate(names) if n[2] == "mass"],
                             torque_response_at_plus_amplitude_nm=[column[i]*h for i,n in enumerate(names) if n[2] == "torque"],
                             original_75_scaled_derivative=original)
            samples.append(entry)
        primary, reference = POLICY["primary_amplitude"], POLICY["reference_amplitude"]
        deviation = relative(usable[primary], usable[reference]) if primary in usable and reference in usable else None
        validated = deviation is not None and deviation <= POLICY["relative_linearity_tolerance"]
        if validated:
            columns[direction["name"]] = usable[primary]
        summaries.append(dict(direction=direction["name"], independent=direction["independent"], samples=samples,
                              primary_to_reference_relative_deviation=deviation, validated=validated))
    nonfluid = [name for name, _ in OPERATING]
    pairs = []
    for i, a in enumerate(nonfluid):
        for b in nonfluid[i+1:]:
            if a in columns and b in columns:
                pairs.append(dict(a=a, b=b, fluid_and_torque_cosine=cosine(columns[a][:75], columns[b][:75]),
                                  with_thermal_bodies_cosine=cosine(columns[a], columns[b])))
    coupled = None
    if all(name in columns for name in [*nonfluid, "coupled-nuclear-thermal"]):
        d = directions()[-1]["operating"]
        prediction = [math.fsum(columns[name][i]*d[j] for name,j in OPERATING) for i in range(81)]
        deviation = relative(columns["coupled-nuclear-thermal"], prediction)
        coupled = dict(relative_superposition_error=deviation, within_tolerance=deviation <= POLICY["relative_linearity_tolerance"],
                       response_to_plus_1e_minus4_thermal_w=[x*1e6*1e-4 for x in columns["coupled-nuclear-thermal"][75:]],
                       interpretation="Measured combined probe; no equilibrium proposal or search")
    return dict(status="PASS-REPAIRED-COUPLED-SENSITIVITY-EVIDENCE", fixtures=fixtures(), recipes=len(records), maximum_runtime_steps=248,
                repeated_recipes_identical=True, baseline_matches_repaired_parent=True,
                valid_recipes=sum(r["Reason"] == "VALID" for r in records),
                in_envelope_recipes=sum(r["Residuals"] is not None and all(o["InEnvelope"] for o in r["Observations"]) for r in records),
                maximum_energy_ledger_error_w=max(map(abs, errors)),
                directions=summaries, validated_independent_directions=[d["name"] for d in directions() if d["independent"] and d["name"] in columns],
                nonfluid_cosines=pairs, coupled_superposition=coupled, family_selected=False,
                root_observed=False, r3_passed=False, m10_closed=False, seed_installed=False,
                limits=["Nine independent directions, not a complete 24-inventory basis", "No prediction outside the measured amplitudes",
                        "Thermal diagnostic rows supplement but do not replace the original 75 residuals or root criterion"])


def execute():
    authenticate()
    P.require(not (OUT / "03-measurements.jsonl").exists(), "Previous acquisition retained")
    with (OUT / "execution.log").open("x", encoding="utf-8") as log:
        subprocess.run(["dotnet", str(BINARY), str(ROOT)], cwd=ROOT, stdout=log, stderr=subprocess.STDOUT, check=True)
    return adjudicate()


def adjudicate():
    authenticate()
    rows = [json.loads(line) for line in (OUT / "03-measurements.jsonl").read_text(encoding="utf-8").splitlines()]
    result = analyze(rows)
    P.write(OUT, "04-analysis.json", result)
    return result["status"]


def freeze():
    adjudicate()
    P.freeze(ID, ["00-build.log", "01-requests.json", "02-preexecution.json", "03-measurements.jsonl", "04-analysis.json", "execution.log"])
    return "PASS-FROZEN-COUPLED-SENSITIVITY"


def review():
    authenticate()
    receipt = ROOT / "eng" / (ID + "-returned.json")
    P.require(P.load(receipt)["contract_sha256"] == P.digest(CONTRACT.read_bytes()), "Contract receipt drift")
    data = frozen(ID)
    result = analyze([json.loads(line) for line in data["03-measurements.jsonl"].splitlines()])
    P.require(result == json.loads(data["04-analysis.json"]), "Offline adjudication drift")
    return result["status"] + " (offline; zero runtime steps)"


if __name__ == "__main__":
    P.require(len(sys.argv) == 2 and sys.argv[1] in ("fixtures", "preexecution", "execute", "adjudicate", "freeze", "review"), "Choose a protocol stage")
    print(globals()[sys.argv[1]]())
