"""Refine the three unavailable C4 directions, preserving the preceding evidence."""
from __future__ import annotations

import json
import math
import runpy
import subprocess
import sys

import r3_h_protocol as P

ROOT = P.ROOT
S = runpy.run_path(str(ROOT / "eng/run-r3-repaired-coupled-sensitivity1.py"))
ID = "r3-repaired-missing-directions2"
OUT = ROOT / "artifacts" / ID
CONTRACT = ROOT / "eng" / (ID + "-contract.json")
PROJECT = "eng/diagnostics/R3ProbeRunner/R3ProbeRunner.csproj"
BINARY = ROOT / "eng/diagnostics/R3ProbeRunner/bin/Release/net10.0/R3ProbeRunner.dll"
POLICY = dict(recipes=70, repeats=2, maximum_runtime_steps=212,
              pressure_amplitudes=[1e-7, 3e-7, 1e-6, 2e-6],
              exhaust_energy_amplitudes=[1e-6, 3e-6, 1e-5, 3e-5, 1e-4, 3e-4, 1e-3],
              compensated_amplitudes=[1e-5, 1e-4], relative_linearity_tolerance=.1,
              physical_weights=S["POLICY"]["physical_weights"],
              energy_ledger_tolerance_w=.01, mass_ledger_tolerance_kg_s=1e-6, torque_ledger_tolerance_nm=1e-6,
              selection="largest adjacent admissible pair within ten percent; retain every pair result",
              require_same_mechanical_regime=True, root_criterion_unchanged=True,
              proposals=0, searches=0, production_changes=0, r3_passed=False, m10_closed=False)


def parent_records():
    return {r["Id"]: r for r in map(json.loads, S["frozen"](S["ID"])["03-measurements.jsonl"].splitlines())}


def directions():
    old = parent_records()
    def pressure_change(name):
        return old[f"pressure:{name}/plus/1e-05/repeat/1"]["Raw"][7]["Pressure"] - old[f"pressure:{name}/minus/1e-05/repeat/1"]["Raw"][7]["Pressure"]
    coefficient = -pressure_change("mass") / pressure_change("energy")
    return [dict(name="pressure:mass", indices=[14], coefficients=[1.], amplitudes=POLICY["pressure_amplitudes"]),
            dict(name="pressure:energy", indices=[15], coefficients=[1.], amplitudes=POLICY["pressure_amplitudes"]),
            dict(name="exhaust:energy", indices=[5], coefficients=[1.], amplitudes=POLICY["exhaust_energy_amplitudes"]),
            dict(name="pressure:compensated", indices=[14, 15], coefficients=[1., coefficient], amplitudes=POLICY["compensated_amplitudes"])]


def requests():
    points = [dict(Id="base", Coordinates=[0.]*24, OperatingPointFractions=[0.]*6)]
    for d in directions():
        for h in d["amplitudes"]:
            for sign, tag in ((1, "plus"), (-1, "minus")):
                coordinates = [0.]*24
                for index, coefficient in zip(d["indices"], d["coefficients"]):
                    coordinates[index] = sign*h*coefficient
                points.append(dict(Id=f'{d["name"]}/{tag}/{h:g}', Coordinates=coordinates, OperatingPointFractions=[0.]*6))
    return [{**point, "Id": point["Id"]+f"/repeat/{repeat}"} for point in points for repeat in (1, 2)]


def mechanical_regime(record):
    fields = ("TripBlocked", "ThermodynamicWorkLimited", "TorqueReferenceAboveRated", "ExternalLoadTorqueLimitedAtZeroSpeed", "PassiveMechanicalLossTorqueLimitedAtZeroSpeed")
    return [[*[o["Extra"]["Mechanical"][key] for key in fields],
             o["Extra"]["Mechanical"]["Governor"]["IsSaturated"], o["Extra"]["Mechanical"]["Governor"]["AntiWindupActive"]]
            for o in record["Observations"]]


def fixtures():
    P.require(len(requests()) == POLICY["recipes"] and len({r["Id"] for r in requests()}) == 70, "Budget fixture")
    P.require(abs((3.-1.-.5)-1.5) < 1e-15, "Torque ledger fixture")
    P.require(select_pair([dict(usable=True, relative_deviation=.05, amplitude=1.), dict(usable=False, relative_deviation=.01, amplitude=2.)])["amplitude"] == 1., "Selection rejects unavailable pair")
    P.require(select_pair([dict(usable=True, relative_deviation=.2, amplitude=1.)]) is None, "Selection rejects nonlinear pair")
    return "PASS-MISSING-DIRECTIONS-FIXTURES"


def select_pair(pairs):
    valid = [p for p in pairs if p["usable"] and p["relative_deviation"] <= POLICY["relative_linearity_tolerance"]]
    return max(valid, key=lambda p:p["amplitude"]) if valid else None


def authenticate():
    c = P.load(CONTRACT)
    P.require(c["schema"] == ID+"-v1" and c["policy"] == POLICY and c["directions"] == directions(), "Policy drift")
    for key in ("src", "tests"):
        P.require(P.tree(key) == c["unchanged_baseline"][key], "Runtime/test tree drift")
    for entry in c["implementation_files"]+c["frozen_inputs"]:
        P.require(P.digest((ROOT/entry["path"]).read_bytes()) == entry["sha256"], "Input drift: "+entry["path"])


def preexecution():
    authenticate()
    P.require(not (OUT/"03-measurements.jsonl").exists(), "Existing acquisition retained")
    P.write(OUT, "01-requests.json", requests())
    P.require(BINARY.exists(), "Build the reusable probe runner first")
    assemblies = [dict(file=p.name, sha256=P.digest(p.read_bytes())) for p in sorted(BINARY.parent.glob("*.dll"))]
    P.write(OUT, "02-preexecution.json", dict(status="PASS-PREEXECUTION", contract_sha256=P.digest(CONTRACT.read_bytes()),
            requests_sha256=P.digest((OUT/"01-requests.json").read_bytes()), assemblies=assemblies, fixtures=fixtures(), runtime_steps=0))
    subprocess.run(["dotnet", str(BINARY), str(ROOT), ID, "--validate-only"], cwd=ROOT, check=True)
    return "PASS-PREEXECUTION"


def audit_mechanical(record):
    errors = []
    if record["Residuals"] is None:
        return errors
    for o in record["Observations"]:
        m = o["Extra"]["Mechanical"]
        check = lambda actual, expected, label: errors.append(S["close"](actual, expected, POLICY["torque_ledger_tolerance_nm"], label))
        check(m["NetTorqueNm"], m["TurbineTorqueNm"]-m["ExternalLoadTorqueNm"]-m["LossTorqueNm"], "Rotor torque ledger")
        check(o["RotorTorque"], m["NetTorqueNm"], "Original torque residual ownership")
        check(m["ExternalLoadTorqueNm"], m["GeneratorTorqueNm"], "Generator load ownership")
        check(m["NetTorqueNm"], m["InertiaKgM2"]*(m["FinalOmega"]-m["InitialOmega"])/.01, "Angular momentum ledger")
        expected = o["Extra"]["Stage"]["VaporMassKgS"]*m["StageIdealWorkJKg"]*m["StageEfficiency"]/max(m["RatedOmega"], m["InitialOmega"])
        check(m["TurbineTorqueNm"], expected, "Stage work-to-torque law")
    return errors


def analyze(records):
    P.require(len(records) == POLICY["recipes"], "Recipe budget drift")
    plan, scales, names = S["layout"]()
    points, energy_errors, torque_errors = {}, [], []
    expected = requests()
    for i in range(0, len(records), 2):
        pair = records[i:i+2]
        for r, request in zip(pair, expected[i:i+2]):
            P.require(all(r[key] == request[key] for key in request), "Request/response drift")
            P.H3["H2"]["verify_physics"](r, plan)
            energy_errors += S["verify_extra"](r)
            torque_errors += audit_mechanical(r)
        P.require({k:v for k,v in pair[0].items() if k != "Id"} == {k:v for k,v in pair[1].items() if k != "Id"}, "Repeatability drift")
        points[pair[0]["Id"].rsplit("/repeat/", 1)[0]] = pair[0]
    base, old = points["base"], parent_records()
    overlapping = []
    for key, record in points.items():
        prior = old.get(key+"/repeat/1")
        if prior and record["Residuals"] is not None:
            for field in ("Raw", "Residuals", "Phases"):
                P.require(record[field] == prior[field], "Parent point identity drift")
            P.require([o["StateSha256"] for o in record["Observations"]] == [o["StateSha256"] for o in prior["Observations"]], "Parent full-state identity drift")
            overlapping.append(key)
    summaries, torque = [], []
    for direction in directions():
        columns, samples = {}, []
        for h in direction["amplitudes"]:
            plus, minus = [points[f'{direction["name"]}/{tag}/{h:g}'] for tag in ("plus", "minus")]
            available = S["compatible"](plus, base) and S["compatible"](minus, base)
            mech_match = (mechanical_regime(plus) == mechanical_regime(base) == mechanical_regime(minus)) if plus["Residuals"] and minus["Residuals"] else False
            usable = available and mech_match
            item = dict(amplitude=h, central_usable=usable, original_guards_passed=available, mechanical_regime_match=mech_match,
                        plus_reason=plus["Reason"], minus_reason=minus["Reason"],
                        plus_in_envelope=all(o["InEnvelope"] for o in plus.get("Observations", [])) if plus["Residuals"] else False,
                        minus_in_envelope=all(o["InEnvelope"] for o in minus.get("Observations", [])) if minus["Residuals"] else False)
            if usable:
                columns[h] = [(a-b)/(2*h) for a,b in zip(S["weighted_vector"](plus, scales, names), S["weighted_vector"](minus, scales, names))]
                item["original_75_scaled_derivative"] = [(a-b)/(2*h) for a,b in zip(plus["Residuals"], minus["Residuals"])]
            if direction["name"] == "exhaust:energy" and plus["Residuals"] and minus["Residuals"]:
                checkpoint_terms = []
                for a,b in zip(plus["Observations"], minus["Observations"]):
                    ma, mb = a["Extra"]["Mechanical"], b["Extra"]["Mechanical"]
                    checkpoint_terms.append(dict(checkpoint=a["Checkpoint"],
                        derivatives_per_fraction={key:(ma[key]-mb[key])/(2*h) for key in ("TurbineTorqueNm", "ExternalLoadTorqueNm", "LossTorqueNm", "NetTorqueNm", "StageIdealWorkJKg", "InitialOmega")},
                        plus_governor=ma["Governor"], minus_governor=mb["Governor"]))
                torque.append(dict(amplitude=h, checkpoint_terms=checkpoint_terms))
            samples.append(item)
        pairs = []
        for lower, upper in zip(direction["amplitudes"], direction["amplitudes"][1:]):
            available = lower in columns and upper in columns
            pairs.append(dict(reference_amplitude=lower, amplitude=upper, usable=available,
                              relative_deviation=S["relative"](columns[upper], columns[lower]) if available else None))
        selected = select_pair(pairs)
        summaries.append(dict(direction=direction["name"], samples=samples, adjacent_pairs=pairs, selected=selected,
                              selected_weighted_81_derivative=columns[selected["amplitude"]] if selected else None))
    return dict(status="PASS-REPAIRED-MISSING-DIRECTIONS-EVIDENCE", fixtures=fixtures(), recipes=len(records), maximum_runtime_steps=212,
                exact_repeats=True, parent_points_reproduced=overlapping,
                valid_recipes=sum(r["Reason"] == "VALID" for r in records),
                in_envelope_recipes=sum(r["Residuals"] is not None and all(o["InEnvelope"] for o in r["Observations"]) for r in records),
                maximum_energy_ledger_error_w=max(map(abs, energy_errors)), maximum_torque_ledger_error_nm=max(map(abs, torque_errors)),
                directions=summaries, exhaust_torque_decomposition=torque,
                validated_coordinate_directions=[d["direction"] for d in summaries[:3] if d["selected"]],
                complete_cycle_basis=False, family_selected=False, root_observed=False, r3_passed=False, m10_closed=False,
                limits=["Adjacent-scale agreement is local; no extrapolation or physical infeasibility proof",
                        "Mechanical branch matching supplements the previous guards; original weights and root criterion remain unchanged"])


def execute():
    authenticate()
    P.require(not (OUT/"03-measurements.jsonl").exists(), "Existing acquisition retained")
    with (OUT/"execution.log").open("x", encoding="utf-8") as stream:
        subprocess.run(["dotnet", str(BINARY), str(ROOT), ID], cwd=ROOT, stdout=stream, stderr=subprocess.STDOUT, check=True)
    return adjudicate()


def adjudicate():
    authenticate()
    result = analyze([json.loads(line) for line in (OUT/"03-measurements.jsonl").read_text(encoding="utf-8").splitlines()])
    P.write(OUT, "04-analysis.json", result)
    return result["status"]


def freeze():
    adjudicate()
    P.freeze(ID, ["00-build.log", "01-requests.json", "02-preexecution.json", "03-measurements.jsonl", "04-analysis.json", "execution.log"])
    return "PASS-FROZEN-MISSING-DIRECTIONS"


def review():
    authenticate()
    receipt = ROOT/"eng"/(ID+"-returned.json")
    P.require(P.load(receipt)["contract_sha256"] == P.digest(CONTRACT.read_bytes()), "Contract receipt drift")
    data = S["frozen"](ID)
    result = analyze([json.loads(line) for line in data["03-measurements.jsonl"].splitlines()])
    P.require(result == json.loads(data["04-analysis.json"]), "Replay drift")
    return result["status"]+" (offline; zero runtime steps)"


if __name__ == "__main__":
    P.require(len(sys.argv) == 2 and sys.argv[1] in ("fixtures", "preexecution", "execute", "adjudicate", "freeze", "review"), "Choose a protocol stage")
    print(globals()[sys.argv[1]]())
