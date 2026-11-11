"""Repeated probes of the five remaining non-fluid operating-point quantities at the frozen mode-2 raw vector; diagnostic only."""
import argparse
import gzip
import json
import math
import runpy

import r3_h_protocol as P
import r3_op_protocol as OP

ID = "r3-op-remaining-quantities-sensitivity1"
OUT = P.ROOT / "artifacts" / ID
REVIEW = runpy.run_path(str(P.ROOT / "eng/review-r3-h-family-sufficiency1.py"))
FEASIBILITY = REVIEW["FEASIBILITY"]
VALVE_ID = "r3-op-control-valve-sensitivity1"
VALVE = OP.QUANTITIES[0]
POLICY = dict(quantities=list(OP.QUANTITIES[1:]), amplitudes=[1e-6, 1e-5, 1e-4, 1e-3, 1e-2], primary_amplitude=1e-4,
              linearity_reference_amplitude=1e-5, linearity_tolerance=.1, material_energy_fraction=.5,
              fractional_bound=.02, inventory_derivative_step=1e-6,
              physical_weights=dict(mass_kg_s=1., energy_w=2.5e6, torque_nm=100.),
              frozen_columns=[dict(protocol=VALVE_ID, quantity=VALVE, amplitude=1e-4)],
              repeats=2, maximum_recipes=102, maximum_batches=3, maximum_physical_steps=306,
              production_change=False, family_selected=False, seed_installation=False, r3_passed=False)


def points():
    result = [dict(id="base", quantity=None, fraction=0.)]
    for quantity in POLICY["quantities"]:
        for amplitude in POLICY["amplitudes"]:
            for sign in (1, -1):
                result.append(dict(id=f"{quantity}/{'plus' if sign > 0 else 'minus'}/{amplitude:g}", quantity=quantity, fraction=sign * amplitude))
    return result


def requests():
    return [OP.request(f'{p["id"]}/repeat/{repeat}', [0.] * 24, [p["fraction"] if q == p["quantity"] else 0. for q in OP.QUANTITIES])
            for p in points() for repeat in range(1, POLICY["repeats"] + 1)]


def reference():
    """Frozen zero-coordinate record and phase-compatible inventory Jacobian of H feasibility 1."""
    with gzip.open(P.ROOT / FEASIBILITY, "rt", encoding="utf-8-sig") as stream:
        rows = [json.loads(line) for line in stream if line.strip()]
    base, h = rows[0], POLICY["inventory_derivative_step"]
    P.require(base["Purpose"] == "initial" and base["Coordinates"] == [0.] * 24 and base["Reason"] == "VALID", "Reference record drift")
    columns = []
    for j in range(24):
        samples = [(sign, next(r for r in rows if r["Iteration"] == 1 and r["Purpose"] == tag + str(j))) for sign, tag in ((1, "difference+/"), (-1, "difference-/"))]
        column, _ = P.H3["compatible_column"](base, samples, h)
        P.require(column is not None, "Reference inventory derivative unavailable")
        columns.append(column)
    return base, columns


def frozen_valve():
    """Baseline record and primary column of the frozen, reviewed control-valve diagnostic."""
    data = OP.frozen(VALVE_ID)
    adjudication = json.loads(data["05-adjudication.json"])
    P.require(adjudication["status"] == "PASS-EVIDENCE-COMPLETE" and adjudication["classification"] == "CONTROL-VALVE-NOT-MATERIAL-IN-LINEAR-MODEL",
              "Frozen valve adjudication drift")
    records = {r["Id"]: r for line in data["03-batches.jsonl"].decode().splitlines() for r in json.loads(line)["records"]}
    base, amplitude = records["base/repeat/1"], POLICY["frozen_columns"][0]["amplitude"]
    samples = [(sign, records[f"valve/{tag}/{amplitude:g}/repeat/1"]) for sign, tag in ((1, "plus"), (-1, "minus"))]
    column, stencil = P.H3["compatible_column"](base, samples, amplitude)
    P.require(column is not None and stencil == "central" and adjudication["primary_column_deviation"] <= POLICY["linearity_tolerance"],
              "Frozen valve primary column unusable")
    return base, column


def weights(scales, names):
    w = POLICY["physical_weights"]
    return [s / dict(mass=w["mass_kg_s"], energy=w["energy_w"], torque=w["torque_nm"])[n[2]] for s, n in zip(scales, names)]


def solve(tag, columns, labels, residuals, row_weights, scales, names):
    a = [[column[i] * row_weights[i] for column in columns] for i in range(75)]
    norms = [math.sqrt(math.fsum(row[j] ** 2 for row in a)) for j in range(len(columns))]
    P.require(all(n > 0 for n in norms), "Null derivative column")
    scaled = REVIEW["least_squares"]([[v / n for v, n in zip(row, norms)] for row in a], [-r * w for r, w in zip(residuals, row_weights)])
    delta = [x / n for x, n in zip(scaled, norms)]
    predicted = [r + math.fsum(column[i] * d for column, d in zip(columns, delta)) for i, r in enumerate(residuals)]
    return dict(model=tag, step=[dict(coordinate=l, fraction=d) for l, d in zip(labels, delta)],
                coordinates_outside_bound=sum(abs(d) > POLICY["fractional_bound"] for d in delta),
                predicted=REVIEW["metric"](predicted, scales, names), prediction_is_not_an_observation=True)


def audit(batches):
    _, nodes, scales, names = REVIEW["layout"]()
    reference_base, inventory = reference()
    valve_base, valve_column = frozen_valve()
    expected = requests()
    size = 48
    P.require([b["batch"] for b in batches] == list(range(1, len(batches) + 1)) and len(batches) <= POLICY["maximum_batches"]
              and [b["requests"] for b in batches] == [expected[i:i + size] for i in range(0, len(expected), size)], "Probe request drift")
    records = [r for b in batches for r in b["records"]]
    P.require(len(records) == len(expected) <= POLICY["maximum_recipes"], "Probe count drift")
    by_point = {}
    for i, point in enumerate(points()):
        repeats = records[POLICY["repeats"] * i:POLICY["repeats"] * (i + 1)]
        for sent, record in zip(expected[POLICY["repeats"] * i:], repeats):
            P.require(OP.same_request(sent, record), "Probe identity drift")
            P.audit_record(record)
        P.require(all({**r, "Id": "repeat"} == {**repeats[0], "Id": "repeat"} for r in repeats), "Probe repeats differ")
        by_point[point["id"]] = repeats[0]
    base = by_point["base"]
    P.require(base["Reason"] == "VALID" and base["Residuals"] == reference_base["Residuals"] and base["Phases"] == reference_base["Phases"],
              "Frozen mode-2 baseline not reproduced by the operating-point executor")
    P.require({**base, "Id": "base"} == {**valve_base, "Id": "base"}, "Baseline differs from the frozen control-valve diagnostic")
    row_weights = weights(scales, names)
    residuals = base["Residuals"]
    labels = [f"{n}:{q}" for n in nodes for q in ("mass", "energy")]
    inventory_only = solve("inventories-only/physical-weights", inventory, labels, residuals, row_weights, scales, names)
    energy = lambda model: abs(model["predicted"]["maximum_energy_rate_w"]["value"])
    observations, quantities, usable = [], [], {}
    for quantity in POLICY["quantities"]:
        columns, sensitivities = {}, []
        for amplitude in POLICY["amplitudes"]:
            samples = [(sign, by_point[f"{quantity}/{tag}/{amplitude:g}"]) for sign, tag in ((1, "plus"), (-1, "minus"))]
            column, stencil = P.H3["compatible_column"](base, samples, amplitude)
            columns[amplitude] = column
            for sign, record in samples:
                valid = record["Reason"] == "VALID"
                observations.append(dict(id=f"{quantity}/{'plus' if sign > 0 else 'minus'}/{amplitude:g}", quantity=quantity, fraction=sign * amplitude,
                                         reason=record["Reason"], phase_compatible=valid and record["Phases"] == base["Phases"],
                                         phase_changes=[i for i, (a, b) in enumerate(zip(base["Phases"], record["Phases"])) if a != b] if valid else None,
                                         metric=REVIEW["metric"](record["Residuals"], scales, names) if valid else None,
                                         all_in_envelope=valid and all(o["InEnvelope"] for o in record["Observations"]),
                                         governor_percent=[o["Governor"] for o in record["Observations"]] if valid else None))
            sensitivities.append(dict(amplitude=amplitude, stencil=stencil, largest_physical_sensitivities_per_unit_fraction=None if column is None else [
                dict(checkpoint=names[i][0], node=names[i][1], quantity=names[i][2], value=column[i] * scales[i])
                for i in sorted(range(75), key=lambda i: -abs(column[i] * row_weights[i]))[:8]]))
        primary, check = columns[POLICY["primary_amplitude"]], columns[POLICY["linearity_reference_amplitude"]]
        deviation, status = None, "unavailable"
        if primary is not None and not any(primary):
            status = "null"
        elif primary is not None and check is not None:
            deviation = max(abs(a - b) * w for a, b, w in zip(primary, check, row_weights)) / max(abs(a) * w for a, w in zip(primary, row_weights))
            status = "usable" if deviation <= POLICY["linearity_tolerance"] else "nonlinear"
        entry = dict(quantity=quantity, sensitivities=sensitivities, primary_column_deviation=deviation, primary_column_status=status,
                     linear_models=[], joint_to_inventory_only_energy_ratio=None, material=False)
        if status == "usable":
            usable[quantity] = primary
            alone = solve(quantity + "-only/physical-weights", [primary], [quantity], residuals, row_weights, scales, names)
            joint = solve("inventories-and-" + quantity + "/physical-weights", inventory + [primary], labels + [quantity], residuals, row_weights, scales, names)
            entry["linear_models"] = [alone, joint]
            entry["joint_to_inventory_only_energy_ratio"] = energy(joint) / energy(inventory_only)
            entry["material"] = entry["joint_to_inventory_only_energy_ratio"] <= POLICY["material_energy_fraction"]
        quantities.append(entry)
    extra = [(VALVE, valve_column)] + list(usable.items())
    combined = solve("inventories-and-all-usable-nonfluid/physical-weights", inventory + [c for _, c in extra], labels + [q for q, _ in extra],
                     residuals, row_weights, scales, names)
    combined_ratio = energy(combined) / energy(inventory_only)
    material = [q["quantity"] for q in quantities if q["material"]]
    undetermined = [q["quantity"] for q in quantities if q["primary_column_status"] in ("unavailable", "nonlinear")]
    if material:
        classification = "NONFLUID-QUANTITY-MATERIAL-IN-LINEAR-MODEL"
    elif combined_ratio <= POLICY["material_energy_fraction"]:
        classification = "NONFLUID-COMBINATION-MATERIAL-IN-LINEAR-MODEL"
    elif undetermined:
        classification = "INCONCLUSIVE-UNUSABLE-PRIMARY-COLUMN"
    else:
        classification = "NO-NONFLUID-QUANTITY-MATERIAL-IN-LINEAR-MODEL"
    return dict(status="PASS-EVIDENCE-COMPLETE", classification=classification, material_quantities=material,
                undetermined_quantities=undetermined, null_quantities=[q["quantity"] for q in quantities if q["primary_column_status"] == "null"],
                recipes=len(records), batches=len(batches),
                maximum_physical_steps=3 * len(records), baseline_reproduced=True, baseline_matches_frozen_valve_diagnostic=True,
                baseline_metric=REVIEW["metric"](residuals, scales, names), inventory_only_model=inventory_only, quantities=quantities,
                observations=observations, combined_model=combined, combined_columns=[q for q, _ in extra],
                combined_to_inventory_only_energy_ratio=combined_ratio, linear_models_are_not_observations=True,
                family_selected=False, r3_passed=False, m10_closed=False, seed_installed=False)


def fixtures():
    rs = requests()
    P.require(len(rs) == POLICY["maximum_recipes"] and 3 * len(rs) == POLICY["maximum_physical_steps"], "Budget fixture failed")
    P.require(-(-len(rs) // 48) == POLICY["maximum_batches"], "Batch budget fixture failed")
    P.require(len({r["Id"] for r in rs}) == len(rs), "Duplicate request fixture failed")
    P.require(all(sum(x != 0 for x in r["OperatingPoint"]) <= 1 and not any(r["Coordinates"]) and r["OperatingPoint"][0] == 0 for r in rs),
              "Probe isolation fixture failed")
    P.require(all(rs[i]["OperatingPoint"] == rs[i + 1]["OperatingPoint"] and i // 48 == (i + 1) // 48 for i in range(0, len(rs), POLICY["repeats"])),
              "Repeat placement fixture failed")
    try:
        OP.request("outside", [0.] * 24, [0, .03, 0, 0, 0, 0])
    except ValueError:
        return REVIEW["fixtures"]() + [dict(case="outside-bound-request", rejected=True)]
    raise ValueError("Out-of-bound request was accepted")


def static():
    OP.authenticate(ID, POLICY)
    reference()
    frozen_valve()
    P.write(OUT, "01-static.json", dict(status="PASS-STATIC", contract_sha256=P.digest(OP.contract_path(ID).read_bytes()),
                                        fixtures=fixtures(), points=points(), requests=len(requests())))


def execute():
    OP.authenticate(ID, POLICY)
    OP.Batches(ID, POLICY["maximum_recipes"], POLICY["maximum_batches"]).evaluate(requests())


def adjudicate():
    OP.authenticate(ID, POLICY)
    result = audit([json.loads(line) for line in (OUT / "03-batches.jsonl").read_text(encoding="utf-8").splitlines()])
    P.write(OUT, "05-adjudication.json", result)
    print(json.dumps({k: v for k, v in result.items() if k not in ("observations", "quantities", "inventory_only_model", "combined_model")}, indent=2))


def review():
    OP.authenticate(ID, POLICY)
    data = OP.frozen(ID)
    result = audit([json.loads(line) for line in data["03-batches.jsonl"].decode().splitlines()])
    P.require(result == json.loads(data["05-adjudication.json"]), "Frozen remaining-quantity adjudication drift")
    print(json.dumps(dict(status=result["status"], classification=result["classification"]), indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage", choices=["static", "preexecution", "execute", "adjudicate", "review"])
    {"static": static, "preexecution": lambda: (OP.authenticate(ID, POLICY), OP.preexecution(ID)), "execute": execute,
     "adjudicate": adjudicate, "review": review}[parser.parse_args().stage]()
