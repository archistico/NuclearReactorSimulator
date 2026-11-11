"""Offline bounded linear review of fluid inventories and frozen non-fluid columns; never calls the plant."""
import argparse
import json
import math
import runpy

import r3_h_protocol as P
import r3_op_protocol as OP

ID = "r3-op-bounded-combination-review1"
OUT = P.ROOT / "artifacts" / ID
REMAINING_ID = "r3-op-remaining-quantities-sensitivity1"
REMAINING = runpy.run_path(str(P.ROOT / "eng" / f"run-{REMAINING_ID}.py"))
REVIEW = REMAINING["REVIEW"]
CORE = ("neutron-population-relative", "fuel-temperature-celsius", "structure-temperature-celsius")
STEAM_PATH = ("header", "stop-out", "control-out", "turbine-inlet")
POLICY = dict(plant_calls=0, column_amplitude=1e-4, inventory_bound=.02, probe_bound=.01, operating_bound=.02,
              core_group=list(CORE), material_energy_fraction=.5, kkt_relative_tolerance=1e-8, maximum_active_set_iterations=500,
              physical_weights=dict(mass_kg_s=1., energy_w=2.5e6, torque_nm=100.),
              classification_models=["inventories/bounded", "grouped/probe-bounds", "ungrouped/probe-bounds"],
              reported_only_models=["grouped/operating-bounds"],
              production_change=False, bound_expansion=False, family_selected=False, seed_installation=False, r3_passed=False)


def bounded_least_squares(a, b, bound):
    """Active-set least squares with |x_j| <= bound_j, starting from the feasible origin; returns solution and certificate."""
    rows, columns = len(a), len(a[0])
    x, free = [0.] * columns, set(range(columns))
    gradient = lambda: [math.fsum(a[i][j] * (math.fsum(a[i][k] * x[k] for k in range(columns)) - b[i]) for i in range(rows)) for j in range(columns)]
    scale = max(abs(math.fsum(a[i][j] * b[i] for i in range(rows))) for j in range(columns))
    tolerance = POLICY["kkt_relative_tolerance"] * scale
    for iteration in range(POLICY["maximum_active_set_iterations"]):
        while free:
            order = sorted(free)
            rhs = [b[i] - math.fsum(a[i][k] * x[k] for k in range(columns) if k not in free) for i in range(rows)]
            z = dict(zip(order, REVIEW["least_squares"]([[a[i][j] for j in order] for i in range(rows)], rhs)))
            outside = [j for j in order if abs(z[j]) > bound[j]]
            if not outside:
                for j in order:
                    x[j] = z[j]
                break
            alpha, first = min(((math.copysign(bound[j], z[j]) - x[j]) / (z[j] - x[j]), j) for j in outside)
            for j in order:
                x[j] += alpha * (z[j] - x[j])
            for j in outside:
                if j == first or abs(x[j]) >= bound[j] * (1 - 1e-12):
                    x[j] = math.copysign(bound[j], z[j])
                    free.discard(j)
        g = gradient()
        # A bound is released only when moving inward lowers the objective beyond the tolerance.
        violations = [(abs(g[j]), j) for j in range(columns) if j not in free and math.copysign(1., x[j]) * g[j] > tolerance]
        if not violations:
            free_violation = max((abs(g[j]) for j in free), default=0.)
            return x, dict(converged=True, iterations=iteration + 1, active_bounds=columns - len(free),
                           free_gradient_relative=free_violation / scale, kkt_satisfied=free_violation <= tolerance)
        free.add(max(violations)[1])
    return x, dict(converged=False, iterations=POLICY["maximum_active_set_iterations"], active_bounds=columns - len(free),
                   free_gradient_relative=None, kkt_satisfied=False)


def fixtures():
    cases = []
    def check(case, a, b, bound, expected):
        x, certificate = bounded_least_squares(a, b, bound)
        P.require(certificate["kkt_satisfied"] and max(abs(u - v) for u, v in zip(x, expected)) < 1e-9, "Bounded least-squares fixture failed: " + case)
        cases.append(dict(case=case, solution=x, active_bounds=certificate["active_bounds"]))
    check("interior", [[2, 0], [0, 4], [0, 0]], [2, 8, 0], [10, 10], [1, 2])
    check("separable-clipped", [[2, 0], [0, 4], [0, 0]], [2, 8, 0], [1.5, 1.5], [1, 1.5])
    check("coupled-both-at-bound", [[1, 0], [1, 1]], [3, 3], [1, 1], [1, 1])
    check("clip-then-refit", [[1, 1], [0, 1], [0, 0]], [2, -1, 0], [10, .5], [2.5, -.5])
    check("release-after-clip", [[0, -2, -2], [-2, -2, -2], [0, 0, -1], [1, 0, 0], [0, 1, 0], [0, 0, 1]], [-2, 5, 6, 0, 0, 0], [1, 1, 1], [-1, 2 / 3, -1])
    check("collinear-clipped", [[1, 1], [1, 1 + 1e-9], [0, 0]], [4, 4, 0], [1, 1], [1, 1])
    return REVIEW["fixtures"]() + cases


def columns():
    """Inventory Jacobian, frozen baseline and the non-fluid primary columns of both reviewed diagnostics."""
    base, inventory = REMAINING["reference"]()
    valve_base, valve = REMAINING["frozen_valve"]()
    P.require(valve_base["Residuals"] == base["Residuals"], "Valve baseline drift")
    data = OP.frozen(REMAINING_ID)
    adjudication = json.loads(data["05-adjudication.json"])
    P.require(adjudication["status"] == "PASS-EVIDENCE-COMPLETE" and adjudication["classification"] == "NONFLUID-COMBINATION-MATERIAL-IN-LINEAR-MODEL",
              "Frozen remaining-quantity adjudication drift")
    records = {r["Id"]: r for line in data["03-batches.jsonl"].decode().splitlines() for r in json.loads(line)["records"]}
    P.require(records["base/repeat/1"]["Residuals"] == base["Residuals"], "Remaining-quantity baseline drift")
    h, result = POLICY["column_amplitude"], {OP.QUANTITIES[0]: valve}
    for entry in adjudication["quantities"]:
        quantity = entry["quantity"]
        P.require(entry["primary_column_status"] == "usable", "Unusable frozen primary column: " + quantity)
        samples = [(sign, records[f"{quantity}/{tag}/{h:g}/repeat/1"]) for sign, tag in ((1, "plus"), (-1, "minus"))]
        column, stencil = P.H3["compatible_column"](records["base/repeat/1"], samples, h)
        P.require(column is not None and stencil == "central", "Frozen primary column unavailable: " + quantity)
        result[quantity] = column
    return base, inventory, result


def analyze():
    _, nodes, scales, names = REVIEW["layout"]()
    base, inventory, nonfluid = columns()
    residuals = base["Residuals"]
    row_weights = REMAINING["weights"](scales, names)
    labels = [f"{n}:{q}" for n in nodes for q in ("mass", "energy")]
    weighted = lambda column: [c * w for c, w in zip(column, row_weights)]
    cosine = lambda u, v: math.fsum(x * y for x, y in zip(u, v)) / math.sqrt(math.fsum(x * x for x in u) * math.fsum(y * y for y in v))
    outlet = names.index(("seed-step1", "outlet", "energy"))
    P.require(all(nonfluid[q][outlet] > 0 for q in CORE), "Core columns do not share the outlet-energy sign")
    core = [math.fsum(nonfluid[q][i] for q in CORE) for i in range(75)]
    collinearity = [dict(pair=[p, q], weighted_cosine=cosine(weighted(nonfluid[p]), weighted(nonfluid[q]))) for k, p in enumerate(CORE) for q in CORE[k + 1:]]

    def model(tag, extra, extra_bound):
        cols = inventory + [c for _, c in extra]
        bound = [POLICY["inventory_bound"]] * 24 + [extra_bound] * len(extra)
        a = [[column[i] * row_weights[i] for column in cols] for i in range(75)]
        norms = [math.sqrt(math.fsum(row[j] ** 2 for row in a)) for j in range(len(cols))]
        P.require(all(n > 0 for n in norms), "Null derivative column")
        scaled, certificate = bounded_least_squares([[v / n for v, n in zip(row, norms)] for row in a],
                                                    [-r * w for r, w in zip(residuals, row_weights)], [d * n for d, n in zip(bound, norms)])
        delta = [x / n for x, n in zip(scaled, norms)]
        predicted = [r + math.fsum(column[i] * d for column, d in zip(cols, delta)) for i, r in enumerate(residuals)]
        physical = [p * s for p, s in zip(predicted, scales)]
        return dict(model=tag, certificate=certificate, bounds=dict(inventories=POLICY["inventory_bound"], nonfluid=extra_bound if extra else None),
                    step=[dict(coordinate=l, fraction=d, at_bound=abs(abs(d) - b) <= 1e-12 * b) for l, d, b in zip(labels + [q for q, _ in extra], delta, bound)],
                    predicted=REVIEW["metric"](predicted, scales, names),
                    steam_path_energy_rates_w=[dict(checkpoint=n[0], node=n[1], value=physical[i]) for i, n in enumerate(names)
                                               if n[1] in STEAM_PATH and n[2] == "energy"],
                    prediction_is_not_an_observation=True)

    grouped = [(OP.QUANTITIES[0], nonfluid[OP.QUANTITIES[0]]), (OP.QUANTITIES[1], nonfluid[OP.QUANTITIES[1]]),
               (OP.QUANTITIES[2], nonfluid[OP.QUANTITIES[2]]), ("core-heat-common-fraction", core)]
    ungrouped = [(q, nonfluid[q]) for q in OP.QUANTITIES]
    models = [model("inventories/bounded", [], None), model("grouped/probe-bounds", grouped, POLICY["probe_bound"]),
              model("ungrouped/probe-bounds", ungrouped, POLICY["probe_bound"]), model("grouped/operating-bounds", grouped, POLICY["operating_bound"])]
    energy = {m["model"]: abs(m["predicted"]["maximum_energy_rate_w"]["value"]) for m in models}
    ratios = {tag: energy[tag] / energy["inventories/bounded"] for tag in energy if tag != "inventories/bounded"}
    material = {tag: ratio <= POLICY["material_energy_fraction"] for tag, ratio in ratios.items()}
    if not all(m["certificate"]["kkt_satisfied"] for m in models if m["model"] in POLICY["classification_models"]):
        classification = "INCONCLUSIVE-SOLVER-CERTIFICATE"
    elif material["grouped/probe-bounds"] != material["ungrouped/probe-bounds"]:
        classification = "INCONCLUSIVE-GROUPING-SENSITIVE"
    elif material["grouped/probe-bounds"]:
        classification = "BOUNDED-NONFLUID-COMBINATION-MATERIAL-IN-LINEAR-MODEL"
    else:
        classification = "NO-BOUNDED-NONFLUID-COMBINATION-MATERIAL-IN-LINEAR-MODEL"
    return dict(status="PASS-BOUNDED-COMBINATION-REVIEW", classification=classification,
                contract_sha256=P.digest(OP.contract_path(ID).read_bytes()), plant_calls=0, fixtures=fixtures(),
                baseline_metric=REVIEW["metric"](residuals, scales, names), core_collinearity=collinearity,
                models=models, energy_ratio_to_bounded_inventories=ratios, material=material,
                next_activity=("PLAN-FAMILY-WITH-BOUNDED-NONFLUID-QUANTITIES" if classification.startswith("BOUNDED-")
                               else "REVIEW-STAGE-ENERGY-LAW-BEFORE-ANY-NEW-SEARCH" if classification.startswith("NO-") else "RESOLVE-INCONCLUSIVE-REVIEW"),
                linear_models_are_not_observations=True, bound_expansion_authorized=False, family_selected=False,
                r3_passed=False, m10_closed=False, seed_installed=False)


def execute():
    OP.authenticate(ID, POLICY)
    result = analyze()
    P.write(OUT, "01-analysis.json", result)
    print(json.dumps(dict(status=result["status"], classification=result["classification"], ratios=result["energy_ratio_to_bounded_inventories"],
                          next_activity=result["next_activity"], plant_calls=0), indent=2))


def review():
    OP.authenticate(ID, POLICY)
    result = analyze()
    P.require(result == json.loads(OP.frozen(ID)["01-analysis.json"]), "Frozen bounded-combination analysis drift")
    print(json.dumps(dict(status=result["status"], classification=result["classification"], plant_calls=0), indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage", choices=["static", "execute", "review"])
    {"static": lambda: (OP.authenticate(ID, POLICY), print(json.dumps(dict(status="PASS-STATIC", fixtures=len(fixtures())), indent=2))),
     "execute": execute, "review": review}[parser.parse_args().stage]()
