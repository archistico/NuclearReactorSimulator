"""Replay H9 and review the derivative availability at its active C4 boundary."""
import contextlib
import importlib.util
import io
import json

import r3_h_protocol as P
import r3_h_minimax_regularized as LP
import r3_h_c4_phase_margin as C4
import r3_h_c4_energy_margin as ENERGY

CURRENT = "r3-h-energy-guarded-search9"
PREVIOUS = "r3-h-phase-constrained-search8"
OUT = P.ROOT / "artifacts/r3-h-current-equilibrium-review"


def payloads(protocol):
    receipt = P.ROOT / "eng" / (protocol + "-returned.json")
    contract = P.ROOT / "eng" / (protocol + "-contract.json")
    P.require(P.load(receipt)["contract_sha256"] == P.digest(contract.read_bytes()), "Historical execution identity drift")
    return P.frozen(P.ROOT / "eng/frozen-evidence" / protocol, receipt)


def hotwell(record):
    groups = [("raw", record["Raw"])] + [(o["Checkpoint"], o["Nodes"]) for o in record["Observations"]]
    return [dict(checkpoint=tag, **next(n for n in nodes if n["Id"] == "hotwell")) for tag, nodes in groups]


def ledger(record):
    P.audit_record(record)
    before, now = record["Observations"][1:3]
    train = record["SecondaryEnergy"]["Trains"][0]
    stage = record["SecondaryEnergy"]["Stages"][0]
    boundary = record["SecondaryEnergy"]["Boundaries"][0]
    entries = []
    for node, inflow_mass, outflow_mass, inflow_energy, outflow_energy in [
        ("control-out", train["ControlMassKgS"], train["AdmissionMassKgS"], train["ControlEnergyW"], train["AdmissionEnergyW"]),
        ("turbine-inlet", train["AdmissionMassKgS"], stage["MassKgS"] + boundary["MassKgS"], train["AdmissionEnergyW"], stage["InletEnergyW"] + boundary["EnergyW"]),
    ]:
        previous = next(n for n in before["Nodes"] if n["Id"] == node)
        current = next(n for n in now["Nodes"] if n["Id"] == node)
        mass_rate = (current["Mass"] - previous["Mass"]) / .01
        energy_rate = (current["Energy"] - previous["Energy"]) / .01
        mass_error = mass_rate - (inflow_mass - outflow_mass)
        energy_error = energy_rate - (inflow_energy - outflow_energy)
        P.require(abs(mass_error) <= 1e-6 and abs(energy_error) <= .01, "Recorded node storage does not match recorded transfers")
        entries.append(dict(node=node, mass_rate_kg_s=mass_rate, energy_rate_w=energy_rate, inflow_energy_w=inflow_energy,
                            outflow_energy_w=outflow_energy, mass_ledger_error_kg_s=mass_error, energy_ledger_error_w=energy_error))
    stage_error = stage["InletEnergyW"] - stage["ExhaustEnergyW"] - stage["ShaftPowerW"] - stage["DrainEnergyW"] - stage["OwnershipResidualW"]
    P.require(abs(stage_error) <= .001, "Recorded turbine energy ownership mismatch")
    return dict(checkpoint="1", nodes=entries, stage_ownership_error_w=stage_error,
                equilibrium_qualified=False, ledger_tolerance_is_not_equilibrium_tolerance=True)


def phase_path_review():
    frozen = payloads("r3-h-secondary-minimax-search6")
    batches = [json.loads(line) for line in frozen["03-batches.jsonl"].decode().splitlines()]
    base = batches[0]["records"][0]
    probes = batches[1]["records"]
    columns = []
    for j in range(24):
        samples = [(int(r["Id"].split("/")[-1]), r) for r in probes if r["Id"].startswith(f"i0/probe/{j}/")]
        column, _ = P.H3["compatible_column"](base, samples, 1e-6)
        columns.append(column)
    P.require(all(c is not None for c in columns), "H6 initial Jacobian unavailable")
    jacobian = [list(row) for row in zip(*columns)]
    step = LP.minimax_step(jacobian, base["Residuals"], base["Coordinates"], 1e-4)
    trials = batches[2]["records"]
    P.require(len(trials) == 9, "H6 initial trial sequence incomplete")
    rows = []
    for k, record in enumerate(trials):
        predicted = P.H3["H2"]["MATH"]["predict"](jacobian, base["Residuals"], step["delta"], 2. ** -k)
        rows.append(dict(id=record["Id"], halving=k, predicted_maximum_only=max(map(abs, predicted)), reason=record["Reason"],
                         actual_maximum=max(map(abs, record["Residuals"])), hotwell=hotwell(record),
                         phase_changes=[i for i, (x, y) in enumerate(zip(base["Phases"], record["Phases"])) if x != y]))
    P.require(all(r["predicted_maximum_only"] < P.H3["H2"]["score"](base) < r["actual_maximum"] for r in rows), "H6 initial model/plant disagreement not reproduced")
    P.require(rows[-1]["phase_changes"] == [29, 41], "Smallest H6 step phase-path mismatch")
    return dict(base_maximum=P.H3["H2"]["score"](base), base_hotwell=hotwell(base), trials=rows,
                raw_phase_alone_is_insufficient=True, calls_to_plant=0)


def review():
    spec = importlib.util.spec_from_file_location("current_h9", P.ROOT / "eng" / ("run-" + CURRENT + ".py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    with contextlib.redirect_stdout(io.StringIO()):
        module.review()
        for name in (PREVIOUS, "r3-h-phase-trajectory-trial6", "r3-h-c4-margin-trial7", "r3-h-directional-derivative-trial8",
                     "r3-h-multiscale-derivative-trial9", "r3-h-energy-guarded-trial10"):
            trial_spec = importlib.util.spec_from_file_location(name.replace("-", "_"), P.ROOT / "eng" / ("run-" + name + ".py"))
            trial = importlib.util.module_from_spec(trial_spec)
            trial_spec.loader.exec_module(trial)
            trial.review()
        energy_spec = importlib.util.spec_from_file_location("energy_boundary", P.ROOT / "eng/review-r3-h-energy-boundary1.py")
        energy_module = importlib.util.module_from_spec(energy_spec)
        energy_spec.loader.exec_module(energy_module)
        energy_module.review()
    current = payloads(CURRENT)
    result = json.loads(current["04-result.json"])
    previous_result = json.loads(payloads(PREVIOUS)["04-result.json"])
    final = result["final"]
    summary = json.loads(current["05-adjudication.json"])
    observed = [r for line in current["03-batches.jsonl"].decode().splitlines() for r in json.loads(line)["records"] if r["Reason"] == "VALID"]
    best_observed = min(observed, key=P.H3["H2"]["score"])
    phase = phase_path_review()
    nodes = P.load(P.ROOT / "eng/r3-equilibrium-reconstruction-planning1-contract.json")["search"]["variable_order"]
    ranked = sorted(enumerate(final["Residuals"]), key=lambda item: -abs(item[1]))[:10]
    searches = []
    best = None
    best_protocol = None
    for protocol in ("r3-h-minimax-search4", "r3-h-coupled-minimax-search5", "r3-h-secondary-minimax-search6", "r3-h-liquid-branch-search7", PREVIOUS, CURRENT):
        data = payloads(protocol)
        execution = json.loads(data["04-result.json"])
        adjudication = json.loads(data["05-adjudication.json"])
        if best is None or P.H3["H2"]["score"](execution["final"]) < P.H3["H2"]["score"](best):
            best, best_protocol = execution["final"], protocol
        searches.append(dict(adjudication, protocol=protocol,
                             accepted_model_steps=sum("/trial/" in update["id"] for update in execution["accepted"]),
                             numerical_rejections=sum(i.get("lp_diagnostic", i.get("diagnostic", {})).get("status") == "NUMERICAL-SUBPROBLEM-REJECTED" for i in execution["iterations"])))
    phase_trial = payloads("r3-h-phase-trajectory-trial6")
    margin_trial = payloads("r3-h-c4-margin-trial7")
    phase_trial_result = json.loads(phase_trial["05-adjudication.json"])
    margin_trial_result = json.loads(margin_trial["05-adjudication.json"])
    directional_trial_result = json.loads(payloads("r3-h-directional-derivative-trial8")["05-adjudication.json"])
    energy_review = json.loads(payloads("r3-h-energy-boundary-review1")["01-analysis.json"])
    multiscale = json.loads(payloads("r3-h-multiscale-derivative-trial9")["05-adjudication.json"])
    energy_trial = json.loads(payloads("r3-h-energy-guarded-trial10")["05-adjudication.json"])
    P.require(len(phase_trial_result["promising"]) == 4 and margin_trial_result["promising"] == [], "Returned branch diagnostic outcomes drifted")
    boundary = [dict(checkpoint=tag, **C4.node_margin(node)) for tag, node in zip(["raw", "seed-step1", "post-seed", "1"],
                [next(n for n in group if n["Id"] == "hotwell") for group in [final["Raw"]] + [o["Nodes"] for o in final["Observations"]]])]
    running_hotwell = next(n for n in final["Observations"][-1]["Nodes"] if n["Id"] == "hotwell")
    P.require(running_hotwell["Quality"] == 0 and boundary[-1]["reachability_margin"] > 0, "Zero-quality active-boundary diagnosis not reproduced")
    margin_points = json.loads(margin_trial["01-static.json"])["points"]
    margin_records = [r for line in margin_trial["03-batches.jsonl"].decode().splitlines() for r in json.loads(line)["records"]]
    directional_errors = []
    for i, point in enumerate(margin_points[1:], 1):
        actual = margin_records[2 * i]
        ranked_errors = sorted(enumerate([r - predicted for r, predicted in zip(actual["Residuals"], point["predicted"])]), key=lambda item: -abs(item[1]))[:5]
        directional_errors.append(dict(id=point["id"], derivative_step=previous_result["iterations"][-1]["derivative_step"],
                                       largest_errors=[dict(checkpoint=["seed-step1", "post-seed", "1"][j // 25],
                                                           node=nodes[(j % 25) // 2] if j % 25 < 24 else "rotor",
                                                           quantity="torque" if j % 25 == 24 else "mass" if j % 25 % 2 == 0 else "energy", scaled_error=error)
                                                       for j, error in ranked_errors]))
    last_iteration = result["iterations"][-1]
    unavailable = last_iteration["diagnostic"].get("columns", [])
    P.require(unavailable == [0] and final["Coordinates"][0] == .02, "H9 active bound / missing derivative not reproduced")
    terminal_energy = [dict(checkpoint=node["checkpoint"], **ENERGY.node_margin(node)) for node in hotwell(final)]
    rejected_probes = [r for r in observed if r["Id"].startswith(f'i{last_iteration["iteration"]}/') and "/probe/0/" in r["Id"]]
    P.require(len(rejected_probes) == 2 and all(r["Reason"] == "VALID" and r["Phases"] != final["Phases"]
              and ENERGY.node_margin(hotwell(r)[-1])["energy_margin_j_kg"] < 0 for r in rejected_probes), "H9 inward probes no longer reproduce branch crossing")
    missing_derivative = dict(coordinate=0, node=nodes[0], quantity="mass", upper_box_bound=.02,
                             observed_running_energy_margin_j_kg=terminal_energy[-1]["energy_margin_j_kg"],
                             failed_probes=[dict(id=r["Id"], reason=r["Reason"],
                                                phase_changes=[i for i,(a,b) in enumerate(zip(final["Phases"],r["Phases"])) if a!=b],
                                                running_energy=ENERGY.node_margin(hotwell(r)[-1])) for r in rejected_probes],
                             numerical_subproblem_was_not_attempted=True)
    evidence = dict(status="PASS-CURRENT-EQUILIBRIUM-REVIEW", plant_calls=0, current=CURRENT, summary=summary, searches=searches,
                    best_accepted=dict(protocol=best_protocol, maximum=P.H3["H2"]["score"](best), all_in_envelope=all(o["InEnvelope"] for o in best["Observations"]), ledger=ledger(best)),
                    best_observed_any_branch=dict(id=best_observed["Id"], maximum=P.H3["H2"]["score"](best_observed), hotwell=hotwell(best_observed),
                                                  accepted=best_observed["Id"] in {a["id"] for a in result["accepted"]}, qualified=False),
                    phase_trial_summary=phase_trial_result, c4_margin_trial_summary=margin_trial_result, current_boundary=boundary,
                    directional_trial_summary=directional_trial_result,
                    energy_boundary_review=dict(status=energy_review["status"], samples=len(energy_review["samples"]),
                                                distinct_repeated_points=sum(i["distinct_repeated_points"] for i in energy_review["inputs"]),
                                                volume_only_counterexamples=energy_review["volume_only_counterexamples"],
                                                derivative_limit=energy_review["derivative_limit"],
                                                reviewed_h8_running=energy_review["directional_running_checkpoints"][0],
                                                sampled_review_alone_qualifies_optimizer_guard=False, scope=energy_review["scope"]),
                    multiscale_review=dict(status=multiscale["status"], recipes=multiscale["recipes"],
                                          sufficient_small_step_model_observed=multiscale["sufficient_small_step_model_observed"],
                                          profiles=[dict(width=p["width"], complete=p["complete"], unavailable_columns=p["unavailable_columns"],
                                                         resolves_small_steps=p["resolves_small_steps"], affine_error_bound=p["affine_error_bound"],
                                                         directional_comparisons=p["directional_comparisons"]) for p in multiscale["profiles"]]),
                    energy_guarded_trial_summary=energy_trial, current_energy_margins=terminal_energy,
                    current_missing_derivative=missing_derivative,
                    boundary_directional_errors=directional_errors,
                    next_activity="REVIEW-COMPENSATED-DERIVATIVE-BASIS-OR-INTERIOR-ENERGY-RESERVE-BEFORE-NEW-SEARCH",
                    current_hotwell=hotwell(final), current_ledger=ledger(final), phase_path=phase,
                    dominant_residuals=[dict(checkpoint=["seed-step1", "post-seed", "1"][i // 25], node=nodes[(i % 25) // 2] if i % 25 < 24 else "rotor",
                                             quantity="torque" if i % 25 == 24 else "mass" if i % 25 % 2 == 0 else "energy", scaled_residual=value) for i, value in ranked],
                    near_bounds=[dict(index=i, node=nodes[i // 2], quantity="mass" if i % 2 == 0 else "energy", coordinate=x) for i, x in enumerate(final["Coordinates"]) if .02 - abs(x) < 1e-6],
                    branch_comparison_is_not_infeasibility_proof=True, r3_passed=False, m10_closed=False)
    P.write(OUT, "01-current-review.json", evidence)
    print(json.dumps(dict(status=evidence["status"], current=CURRENT, result=summary, best_accepted=evidence["best_accepted"], ledger=evidence["current_ledger"], plant_calls=0), indent=2))


if __name__ == "__main__":
    review()
