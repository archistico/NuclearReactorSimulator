"""Repeated control-valve operating-point probes at the frozen mode-2 raw vector; diagnostic only."""
import argparse
import gzip
import json
import math
import runpy

import r3_h_protocol as P
import r3_op_protocol as OP

ID = "r3-op-control-valve-sensitivity1"
OUT = P.ROOT / "artifacts" / ID
REVIEW = runpy.run_path(str(P.ROOT / "eng/review-r3-h-family-sufficiency1.py"))
FEASIBILITY = REVIEW["FEASIBILITY"]
VALVE = OP.QUANTITIES.index("control-valve-percent-open")
POLICY = dict(quantity="control-valve-percent-open", amplitudes=[1e-6, 1e-5, 1e-4, 1e-3, 1e-2], primary_amplitude=1e-4,
              linearity_reference_amplitude=1e-5, linearity_tolerance=.1, material_energy_fraction=.5,
              fractional_bound=.02, inventory_derivative_step=1e-6,
              physical_weights=dict(mass_kg_s=1., energy_w=2.5e6, torque_nm=100.),
              repeats=2, maximum_recipes=22, maximum_batches=1, maximum_physical_steps=66,
              production_change=False, family_selected=False, seed_installation=False, r3_passed=False)


def points():
    result = [dict(id="base", fraction=0.)]
    for amplitude in POLICY["amplitudes"]:
        for sign in (1, -1):
            result.append(dict(id=f"valve/{'plus' if sign > 0 else 'minus'}/{amplitude:g}", fraction=sign * amplitude))
    return result


def requests():
    return [OP.request(f'{p["id"]}/repeat/{repeat}', [0.] * 24, [p["fraction"] if k == VALVE else 0. for k in range(len(OP.QUANTITIES))])
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
    expected = requests()
    P.require(len(batches) == 1 and batches[0]["batch"] == 1 and batches[0]["requests"] == expected, "Valve probe request drift")
    records = batches[0]["records"]
    P.require(len(records) == len(expected) <= POLICY["maximum_recipes"], "Valve probe count drift")
    by_point = {}
    for i, point in enumerate(points()):
        repeats = records[POLICY["repeats"] * i:POLICY["repeats"] * (i + 1)]
        for sent, record in zip(expected[POLICY["repeats"] * i:], repeats):
            P.require(OP.same_request(sent, record), "Valve probe identity drift")
            P.audit_record(record)
        P.require(all({**r, "Id": "repeat"} == {**repeats[0], "Id": "repeat"} for r in repeats), "Valve probe repeats differ")
        by_point[point["id"]] = repeats[0]
    base = by_point["base"]
    P.require(base["Reason"] == "VALID" and base["Residuals"] == reference_base["Residuals"] and base["Phases"] == reference_base["Phases"],
              "Frozen mode-2 baseline not reproduced by the operating-point executor")
    row_weights = weights(scales, names)
    observations, sensitivities, columns = [], [], {}
    for amplitude in POLICY["amplitudes"]:
        samples = [(sign, by_point[f"valve/{tag}/{amplitude:g}"]) for sign, tag in ((1, "plus"), (-1, "minus"))]
        column, stencil = P.H3["compatible_column"](base, samples, amplitude)
        columns[amplitude] = column
        for sign, record in samples:
            valid = record["Reason"] == "VALID"
            observations.append(dict(id=f"valve/{'plus' if sign > 0 else 'minus'}/{amplitude:g}", fraction=sign * amplitude, reason=record["Reason"],
                                     phase_compatible=valid and record["Phases"] == base["Phases"],
                                     phase_changes=[i for i, (a, b) in enumerate(zip(base["Phases"], record["Phases"])) if a != b] if valid else None,
                                     metric=REVIEW["metric"](record["Residuals"], scales, names) if valid else None,
                                     all_in_envelope=valid and all(o["InEnvelope"] for o in record["Observations"]),
                                     governor_percent=[o["Governor"] for o in record["Observations"]] if valid else None))
        sensitivities.append(dict(amplitude=amplitude, stencil=stencil, largest_physical_sensitivities_per_unit_fraction=None if column is None else [
            dict(checkpoint=names[i][0], node=names[i][1], quantity=names[i][2], value=column[i] * scales[i])
            for i in sorted(range(75), key=lambda i: -abs(column[i] * row_weights[i]))[:8]]))
    primary, check = columns[POLICY["primary_amplitude"]], columns[POLICY["linearity_reference_amplitude"]]
    deviation = None
    if primary is not None and check is not None:
        deviation = max(abs(a - b) * w for a, b, w in zip(primary, check, row_weights)) / max(abs(a) * w for a, w in zip(primary, row_weights))
    labels = [f"{n}:{q}" for n in nodes for q in ("mass", "energy")]
    residuals = base["Residuals"]
    models = [solve("inventories-only/" + tag, inventory, labels, residuals, w, scales, names)
              for tag, w in (("contract-scales", [1.] * 75), ("physical-weights", row_weights))]
    usable = primary is not None and deviation is not None and deviation <= POLICY["linearity_tolerance"]
    if usable:
        for tag, w in (("contract-scales", [1.] * 75), ("physical-weights", row_weights)):
            models.append(solve("control-valve-only/" + tag, [primary], [POLICY["quantity"]], residuals, w, scales, names))
            models.append(solve("inventories-and-control-valve/" + tag, inventory + [primary], labels + [POLICY["quantity"]], residuals, w, scales, names))
    energy = lambda tag: abs(next(m for m in models if m["model"] == tag)["predicted"]["maximum_energy_rate_w"]["value"])
    ratio = energy("inventories-and-control-valve/physical-weights") / energy("inventories-only/physical-weights") if usable else None
    classification = ("INCONCLUSIVE-NONLINEAR-OR-PHASE-INCOMPATIBLE-PRIMARY-COLUMN" if not usable
                      else "CONTROL-VALVE-MATERIAL-IN-LINEAR-MODEL" if ratio <= POLICY["material_energy_fraction"]
                      else "CONTROL-VALVE-NOT-MATERIAL-IN-LINEAR-MODEL")
    return dict(status="PASS-EVIDENCE-COMPLETE", classification=classification, recipes=len(records), batches=1,
                maximum_physical_steps=3 * len(records), baseline_reproduced=True, baseline_metric=REVIEW["metric"](residuals, scales, names),
                observations=observations, sensitivities=sensitivities, primary_amplitude=POLICY["primary_amplitude"], primary_column_deviation=deviation,
                linear_models=models, joint_to_inventory_only_energy_ratio=ratio,
                valve_and_speed_controller_output_move_together=True, linear_models_are_not_observations=True,
                family_selected=False, r3_passed=False, m10_closed=False, seed_installed=False)


def fixtures():
    P.require(len(requests()) == POLICY["maximum_recipes"] and 3 * len(requests()) == POLICY["maximum_physical_steps"], "Budget fixture failed")
    P.require(len({r["Id"] for r in requests()}) == len(requests()), "Duplicate request fixture failed")
    P.require(all(sum(x != 0 for x in r["OperatingPoint"]) <= 1 and not any(r["Coordinates"]) for r in requests()), "Probe isolation fixture failed")
    try:
        OP.request("outside", [0.] * 24, [.03, 0, 0, 0, 0, 0])
    except ValueError:
        return REVIEW["fixtures"]() + [dict(case="outside-bound-request", rejected=True)]
    raise ValueError("Out-of-bound request was accepted")


def static():
    OP.authenticate(ID, POLICY)
    reference()
    P.write(OUT, "01-static.json", dict(status="PASS-STATIC", contract_sha256=P.digest(OP.contract_path(ID).read_bytes()),
                                        fixtures=fixtures(), points=points(), requests=len(requests())))


def execute():
    OP.authenticate(ID, POLICY)
    OP.Batches(ID, POLICY["maximum_recipes"], POLICY["maximum_batches"]).evaluate(requests())


def adjudicate():
    OP.authenticate(ID, POLICY)
    result = audit([json.loads(line) for line in (OUT / "03-batches.jsonl").read_text(encoding="utf-8").splitlines()])
    P.write(OUT, "05-adjudication.json", result)
    print(json.dumps({k: v for k, v in result.items() if k not in ("observations", "sensitivities", "linear_models")}, indent=2))


def review():
    OP.authenticate(ID, POLICY)
    data = OP.frozen(ID)
    result = audit([json.loads(line) for line in data["03-batches.jsonl"].decode().splitlines()])
    P.require(result == json.loads(data["05-adjudication.json"]), "Frozen valve-sensitivity adjudication drift")
    print(json.dumps(dict(status=result["status"], classification=result["classification"]), indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage", choices=["static", "preexecution", "execute", "adjudicate", "review"])
    {"static": static, "preexecution": lambda: (OP.authenticate(ID, POLICY), OP.preexecution(ID)), "execute": execute,
     "adjudicate": adjudicate, "review": review}[parser.parse_args().stage]()
