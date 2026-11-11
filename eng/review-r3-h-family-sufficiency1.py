"""Offline review of H residual scales, physical ledgers and linear family sufficiency; never calls the plant."""
import argparse
import csv
import gzip
import io
import json
import math

import r3_h_protocol as P

ID = "r3-h-family-sufficiency-review1"
OUT = P.ROOT / "artifacts" / ID
PLANNING = "eng/r3-equilibrium-reconstruction-planning1-contract.json"
FEASIBILITY = "eng/frozen-evidence/r3-h-feasibility1/04-search-trace.jsonl.gz"
DIAGNOSTIC = "eng/frozen-evidence/r3-dyn-eq-d1/"
SEARCHES = ("r3-h-minimax-search4", "r3-h-coupled-minimax-search5", "r3-h-secondary-minimax-search6",
            "r3-h-liquid-branch-search7", "r3-h-phase-constrained-search8", "r3-h-energy-guarded-search9")
CHECKPOINTS = ("seed-step1", "post-seed", "1")
POLICY = dict(plant_calls=0, reviewed_searches=list(SEARCHES), fractional_bound=.02, derivative_step=1e-6,
              physical_weights=dict(mass_kg_s=1., energy_w=2.5e6, torque_nm=100.),
              attribution_relative_tolerance=.02, production_change=False, bound_expansion=False,
              residual_reweighting=False, seed_installation=False, r3_passed=False)


def least_squares(matrix, rhs):
    """Householder QR least squares in binary64, independent of platform numerical libraries."""
    rows, columns = len(matrix), len(matrix[0])
    a = [list(map(float, row)) + [float(b)] for row, b in zip(matrix, rhs)]
    for k in range(columns):
        norm = math.sqrt(math.fsum(a[i][k] ** 2 for i in range(k, rows)))
        P.require(norm > 0, "Rank-deficient linear model")
        alpha = -norm if a[k][k] > 0 else norm
        v = [0.] * k + [a[k][k] - alpha] + [a[i][k] for i in range(k + 1, rows)]
        vv = math.fsum(x * x for x in v)
        for j in range(k, columns + 1):
            factor = 2 * math.fsum(v[i] * a[i][j] for i in range(k, rows)) / vv
            for i in range(k, rows):
                a[i][j] -= factor * v[i]
    solution = [0.] * columns
    for k in reversed(range(columns)):
        solution[k] = (a[k][columns] - math.fsum(a[k][j] * solution[j] for j in range(k + 1, columns))) / a[k][k]
    return solution


def fixtures():
    exact = least_squares([[2, 0], [0, 4], [0, 0]], [2, 8, 0])
    P.require(max(abs(x - y) for x, y in zip(exact, [1, 2])) < 1e-14, "Exact least-squares fixture failed")
    fitted = least_squares([[1, 0], [0, 1], [1, 1]], [1, 1, 0])
    P.require(max(abs(x - 1 / 3) for x in fitted) < 1e-14, "Inconsistent least-squares fixture failed")
    try:
        least_squares([[1, 1], [1, 1], [1, 1]], [1, 2, 3])
    except ValueError:
        return [dict(case="exact", solution=exact), dict(case="inconsistent", solution=fitted), dict(case="rank-deficient", rejected=True)]
    raise ValueError("Rank-deficient fixture was accepted")


def layout():
    contract = P.load(P.ROOT / PLANNING)
    nodes = contract["search"]["variable_order"]
    P.require([s["node"] for s in contract["residual_scales"]] == nodes, "Scale order drift")
    scales, names = [], []
    for checkpoint in CHECKPOINTS:
        for s in contract["residual_scales"]:
            scales += [float(s["mass_scale_kg_s"]), float(s["energy_scale_w"])]
            names += [(checkpoint, s["node"], "mass"), (checkpoint, s["node"], "energy")]
        scales.append(float(contract["rotor_torque_scale_nm"]))
        names.append((checkpoint, "rotor", "torque"))
    P.require(len(scales) == contract["search"]["residual_count"] == 75, "Residual layout drift")
    return contract, nodes, scales, names


def metric(residuals, scales, names):
    physical = [r * s for r, s in zip(residuals, scales)]
    def worst(kind):
        i = max((i for i, n in enumerate(names) if n[2] == kind), key=lambda i: abs(physical[i]))
        return dict(value=physical[i], checkpoint=names[i][0], node=names[i][1])
    step1 = [p for p, n in zip(physical, names) if n[0] == "seed-step1" and n[2] == "energy"]
    return dict(maximum_scaled=max(map(abs, residuals)), maximum_mass_rate_kg_s=worst("mass"), maximum_energy_rate_w=worst("energy"),
                maximum_rotor_torque_nm=worst("torque"), seed_step1_net_energy_rate_w=math.fsum(step1),
                seed_step1_gross_energy_rate_w=math.fsum(map(abs, step1)))


def table(name):
    return list(csv.DictReader(io.StringIO((P.ROOT / DIAGNOSTIC / name).read_text(encoding="utf-8-sig"))))


def attribution(scales, names, baseline):
    nodes = {(r["mode"], r["node"]): float(r["energy_j"]) / float(r["mass_kg"]) for r in table("01-node-residuals.csv") if r["checkpoint"] == "raw"}
    shift = lambda node: nodes["candidate-mode2", node] - nodes["mode1", node]
    row = lambda name: next(r for r in table(name) if r["mode"] == "candidate-mode2" and r["checkpoint"] == "seed-step1")
    stage, pump = float(row("03-controller-rotor.csv")["stage_transfer_kg_s"]), float(row("02-hydraulics-drum.csv")["pump_kg_s"])
    observed = lambda node: baseline[names.index(("seed-step1", node, "energy"))] * scales[names.index(("seed-step1", node, "energy"))]
    entries = [dict(node="turbine-inlet", flow_kg_s=stage, specific_energy_shift_j_kg=shift("control-out"),
                    predicted_w=stage * shift("control-out"), observed_w=observed("turbine-inlet")),
               dict(node="outlet", flow_kg_s=pump, specific_energy_shift_j_kg=shift("outlet") - shift("pressure"),
                    predicted_w=-pump * (shift("outlet") - shift("pressure")), observed_w=observed("outlet"))]
    for e in entries:
        e["relative_difference"] = abs(e["predicted_w"] - e["observed_w"]) / abs(e["observed_w"])
        P.require(e["relative_difference"] <= POLICY["attribution_relative_tolerance"], "Specific-energy attribution not reproduced: " + e["node"])
    return dict(entries=entries, interpretation="Same pressure/temperature/quality targets carry different specific energy under C4; "
                "storage rates equal transported-energy shifts while core heat and shaft work are unchanged",
                arithmetic_agreement_is_not_a_causal_proof=True)


def linear(scales, names, nodes):
    with gzip.open(P.ROOT / FEASIBILITY, "rt", encoding="utf-8-sig") as stream:
        rows = [json.loads(line) for line in stream if line.strip()]
    base = rows[0]
    P.require(base["Purpose"] == "initial" and base["Coordinates"] == [0.] * 24 and base["Reason"] == "VALID", "Baseline record drift")
    h = POLICY["derivative_step"]
    stencils = {"phase-compatible": [], "central-across-phase": []}
    kinds = []
    for j in range(24):
        samples = []
        for sign, tag in ((1, "difference+/"), (-1, "difference-/")):
            r = next(r for r in rows if r["Iteration"] == 1 and r["Purpose"] == tag + str(j))
            P.require(r["Reason"] == "VALID" and r["Coordinates"] == [sign * h if k == j else 0. for k in range(24)], "Derivative probe identity drift")
            samples.append((sign, r))
        column, kind = P.H3["compatible_column"](base, samples, h)
        P.require(column is not None, "Baseline derivative unavailable")
        stencils["phase-compatible"].append(column)
        stencils["central-across-phase"].append([(a - b) / (2 * h) for a, b in zip(samples[0][1]["Residuals"], samples[1][1]["Residuals"])])
        kinds.append(kind)
    r0 = base["Residuals"]
    labels = [f"{n}:{q}" for n in nodes for q in ("mass", "energy")]
    w = POLICY["physical_weights"]
    natural = [dict(mass=w["mass_kg_s"], energy=w["energy_w"], torque=w["torque_nm"])[n[2]] for n in names]

    def solve(stencil, tag, weights, selected):
        jacobian = [list(row) for row in zip(*stencils[stencil])]
        # weights convert a scaled residual into the comparison unit; columns are equilibrated only for conditioning
        a = [[jacobian[i][j] * weights[i] for j in range(24)] for i in selected]
        b = [-r0[i] * weights[i] for i in selected]
        norms = [math.sqrt(math.fsum(row[j] ** 2 for row in a)) for j in range(24)]
        delta = [x / n for x, n in zip(least_squares([[v / n for v, n in zip(row, norms)] for row in a], b), norms)]
        predicted = [r + math.fsum(jr * d for jr, d in zip(row, delta)) for r, row in zip(r0, jacobian)]
        return dict(model=tag, stencil=stencil, equations=len(selected), step=[dict(coordinate=l, fraction=d) for l, d in zip(labels, delta)],
                    maximum_abs_fraction=max(map(abs, delta)), coordinates_outside_box=sum(abs(d) > POLICY["fractional_bound"] for d in delta),
                    predicted=metric(predicted, scales, names), prediction_is_not_an_observation=True)

    everything = range(75)
    models = [solve(stencil, tag, weights, selected) for stencil in stencils for tag, weights, selected in (
        ("all-checkpoints/contract-scales", [1.] * 75, everything),
        ("all-checkpoints/physical-weights", [s / n for s, n in zip(scales, natural)], everything),
        ("seed-step1-only/contract-scales", [1.] * 75, range(25)))]
    one_sided = [dict(coordinate=l, stencil=k) for l, k in zip(labels, kinds) if k != "central"]
    return base, models, one_sided


def analyze():
    contract, nodes, scales, names = layout()
    base, models, one_sided = linear(scales, names, nodes)
    inventories = {r["node"]: r for r in table("01-node-residuals.csv") if r["mode"] == "mode1" and r["checkpoint"] == "raw"}
    scale_rows = [dict(node=s["node"], mass_scale_kg_s=float(s["mass_scale_kg_s"]), energy_scale_w=float(s["energy_scale_w"]),
                       energy_scale_per_inventory_per_s=float(s["energy_scale_w"]) / float(inventories[s["node"]]["energy_j"]),
                       mass_scale_per_inventory_per_s=float(s["mass_scale_kg_s"]) / float(inventories[s["node"]]["mass_kg"])) for s in contract["residual_scales"]]
    energies = [r["energy_scale_w"] for r in scale_rows]
    envelope_w = 1e4  # half-width of the frozen 4.99-5.01 MWe envelope
    progress = [dict(point="frozen-mode2-raw-vector", **metric(base["Residuals"], scales, names))]
    for protocol in SEARCHES:
        receipt = P.ROOT / "eng" / (protocol + "-returned.json")
        P.require(P.load(receipt)["contract_sha256"] == P.digest((P.ROOT / "eng" / (protocol + "-contract.json")).read_bytes()), "Input contract identity drift")
        final = json.loads(P.frozen(P.ROOT / "eng/frozen-evidence" / protocol, receipt)["04-result.json"])["final"]
        P.audit_record(final)
        running = final["Observations"][-1]
        progress.append(dict(point=protocol, **metric(final["Residuals"], scales, names), running_in_envelope=running["InEnvelope"],
                             running_pump_kg_s=running["PumpKgS"], running_governor_percent=running["Governor"], running_electrical_mwe=running["ElectricalMwe"],
                             coordinates_at_bound=[f"{nodes[i // 2]}:{'mass' if i % 2 == 0 else 'energy'}" for i, x in enumerate(final["Coordinates"])
                                                   if POLICY["fractional_bound"] - abs(x) < 1e-6]))
    first, last = progress[0], progress[-1]
    P.require(last["maximum_scaled"] < first["maximum_scaled"] and last["seed_step1_gross_energy_rate_w"] > first["seed_step1_gross_energy_rate_w"]
              and abs(last["maximum_rotor_torque_nm"]["value"]) > abs(first["maximum_rotor_torque_nm"]["value"]), "Scaled/physical divergence not reproduced")
    for model in models:
        if model["equations"] == 75:
            P.require(model["coordinates_outside_box"] > 0 and model["predicted"]["maximum_scaled"] > 1e5, "Linear insufficiency finding not reproduced")
    return dict(status="PASS-FAMILY-SUFFICIENCY-REVIEW", contract_sha256=P.digest((P.ROOT / "eng" / (ID + "-contract.json")).read_bytes()),
                plant_calls=0, fixtures=fixtures(),
                scales=dict(rows=scale_rows, rotor_torque_scale_nm=scales[24], energy_scale_spread=max(energies) / min(energies),
                            electrical_envelope_half_width_w=envelope_w, envelope_to_smallest_energy_scale=envelope_w / min(energies),
                            origin="Maximum canonical mode-1 storage rate or 64 ULP floor; numerical-noise level of Exact-V9"),
                attribution=attribution(scales, names, base["Residuals"]), physical_progress=progress, linear_models=models,
                baseline_one_sided_derivatives=one_sided,
                findings=dict(scaled_maximum_decreased_while_physical_imbalance_increased=True,
                              unconstrained_linear_root_absent_for_fluid_only_family=True,
                              unconstrained_linear_step_leaves_box=True, linearization_point="frozen-mode2-raw-vector",
                              h_infeasibility_proven=False, nonlinear_root_outside_linear_range_not_excluded=True,
                              independent_controller_defect_proven=False),
                next_activity="PLAN-NON-FLUID-OPERATING-POINT-SENSITIVITY-DIAGNOSTIC-BEFORE-ANY-NEW-H-SEARCH",
                bound_expansion_authorized=False, family_selected=False, r3_passed=False, m10_closed=False, seed_installed=False)


def execute():
    P.authenticate(ID, POLICY)
    result = analyze()
    P.write(OUT, "01-analysis.json", result)
    print(json.dumps(dict(status=result["status"], findings=result["findings"], next_activity=result["next_activity"], plant_calls=0), indent=2))


def review():
    P.authenticate(ID, POLICY)
    receipt = P.ROOT / "eng" / (ID + "-returned.json")
    P.require(P.load(receipt)["contract_sha256"] == P.digest((P.ROOT / "eng" / (ID + "-contract.json")).read_bytes()), "Contract identity drift")
    result = analyze()
    P.require(result == json.loads(P.frozen(P.ROOT / "eng/frozen-evidence" / ID, receipt)["01-analysis.json"]), "Frozen family-sufficiency analysis drift")
    print(json.dumps(dict(status=result["status"], plant_calls=0), indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage", choices=["execute", "review"])
    {"execute": execute, "review": review}[parser.parse_args().stage]()
