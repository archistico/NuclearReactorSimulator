"""Bounded nonlinear search driven by finite-direction minimax predictions."""
import argparse
import json
import math
import runpy

import r3_h_protocol as P

LINE = runpy.run_path(str(P.ROOT / "eng/review-r3-h-trust-search3.py"))
ID = "r3-h-minimax-search4"
OUT = P.ROOT / "artifacts" / ID
POLICY = dict(variables=24, residuals=75, fractional_bound=.02, radius_initial=1e-4, radius_minimum=1e-8,
              radius_maximum=1e-3, derivative_maximum=1e-6, maximum_proposals=8, maximum_halvings=8,
              damping=[.001, .01, .1, 1, 10, 100], maximum_iterations=64, maximum_recipes=4096,
              maximum_batches=193, maximum_physical_steps=12674, stagnation_relative=1e-9, stagnation_updates=3,
              keep_radius_on_accepted_probe=True, production_change=False, bound_expansion=False,
              residual_reweighting=False, seed_installation=False, r3_passed=False)


def start_record():
    folder = P.ROOT / "eng/frozen-evidence/r3-h-minimax-trial2"
    payloads = P.frozen(folder, P.ROOT / "eng/r3-h-minimax-trial2-returned.json")
    summary = json.loads(payloads["05-adjudication.json"])
    P.require(len(summary["promising"]) == 4, "Trial 2 prerequisite not favorable")
    records = [r for line in payloads["03-batches.jsonl"].decode().splitlines() for r in json.loads(line)["records"]]
    return min(records, key=P.H3["H2"]["score"])


def proposals(current, columns, radius):
    directions = []
    for j, (x, column) in enumerate(zip(current["Coordinates"], columns)):
        if column is None:
            continue
        for sign in (1, -1):
            length = min(radius, .02 - x if sign > 0 else .02 + x)
            if length > 0:
                delta = [0.] * len(columns)
                delta[j] = sign * .99 * length
                directions.append((f"axis/{j}/{sign}", delta, [d * delta[j] for d in column]))
    if all(c is not None for c in columns):
        jacobian = [list(row) for row in zip(*columns)]
        for damping in POLICY["damping"]:
            try:
                delta, _ = P.H3["local_direction"](jacobian, current["Residuals"], current["Coordinates"], damping, radius)
            except ValueError:
                continue
            changes = [sum(a * d for a, d in zip(row, delta)) for row in jacobian]
            directions.append((f"least-squares/{damping:g}", delta, changes))
    candidates = []
    for ordinal, (identifier, delta, changes) in enumerate(directions):
        alpha, score = LINE["minimize_line"](current["Residuals"], changes)
        if score < P.H3["H2"]["score"](current):
            point = [x + alpha * d for x, d in zip(current["Coordinates"], delta)]
            candidates.append(dict(id=identifier, coordinates=point, changes=[alpha * d for d in changes], predicted=score, ordinal=ordinal))
    ranked, seen = [], set()
    for candidate in sorted(candidates, key=lambda c: (c["predicted"], c["ordinal"])):
        key = tuple(candidate["coordinates"])
        if key not in seen:
            seen.add(key)
            ranked.append(candidate)
    return ranked[:8]


def next_radius(radius, accepted, is_probe, ratio, norm, phase_change, policy):
    if not accepted:
        value = radius * .25
    elif is_probe:
        value = radius
    elif ratio is None or ratio < .1:
        value = radius * .25
    elif ratio > .75 and norm >= .8 * radius:
        value = min(policy["radius_maximum"], radius * 2)
    else:
        value = radius
    if accepted and phase_change:
        value = min(value, .5 * radius)
    return max(policy["radius_minimum"], value)


def search(evaluate, initial=None, limits=None):
    policy = POLICY if limits is None else limits
    initial = start_record() if initial is None else initial
    current = evaluate([dict(Id="start", Coordinates=initial["Coordinates"])])[0]
    P.require(current["Reason"] == "VALID" and current["Residuals"] == initial["Residuals"] and current["Phases"] == initial["Phases"], "Starting state mismatch")
    radius, evaluations, stagnant = policy["radius_initial"], 1, 0
    iterations, accepted = [], []
    stop = "ITERATION-LIMIT"
    for iteration in range(policy["maximum_iterations"]):
        before = P.H3["H2"]["score"](current)
        if before <= 1:
            stop = "ROOT-OBSERVED-UNCONFIRMED"
            break
        h = min(1e-6, .1 * radius)
        requests, indices = [], []
        for j, x in enumerate(current["Coordinates"]):
            signs = [1, -1] if abs(x) + h <= .02 else [-1 if x > 0 else 1]
            column = []
            for sign in signs:
                point = current["Coordinates"][:]
                point[j] += sign * h
                column.append((sign, len(requests)))
                requests.append(dict(Id=f"i{iteration}/probe/{j}/{sign}", Coordinates=point))
            indices.append(column)
        if evaluations + len(requests) > policy["maximum_recipes"]:
            stop = "RECIPE-BUDGET"
            break
        probes = evaluate(requests)
        evaluations += len(probes)
        columns, stencils = [], []
        for column in indices:
            derivative, stencil = P.H3["compatible_column"](current, [(sign, probes[index]) for sign, index in column], h)
            columns.append(derivative)
            stencils.append(stencil)
        choices = proposals(current, columns, radius)
        requests, predictions = [], {}
        for rank, choice in enumerate(choices):
            for halving in range(9):
                fraction = 2. ** -halving
                point = [x + fraction * (y - x) for x, y in zip(current["Coordinates"], choice["coordinates"])]
                P.require(all(abs(x) <= .02 and abs(x - previous) <= radius * (1 + 1e-12) for x, previous in zip(point, current["Coordinates"])), "Proposal escaped trust box")
                identifier = f"i{iteration}/trial/{rank}/{halving}"
                predicted = [r + fraction * d for r, d in zip(current["Residuals"], choice["changes"])]
                requests.append(dict(Id=identifier, Coordinates=point))
                predictions[identifier] = max(map(abs, predicted))
        requests = requests[:policy["maximum_recipes"] - evaluations]
        trials = evaluate(requests) if requests else []
        evaluations += len(trials)
        best = min(probes + trials, key=P.H3["H2"]["score"])
        after = P.H3["H2"]["score"](best)
        improved = after < before
        ratio, phase_change, norm = None, False, 0.
        if improved:
            relative = (before - after) / before
            stagnant = stagnant + 1 if relative <= 1e-9 else 0
            if best["Id"] in predictions and before > predictions[best["Id"]]:
                ratio = (before - after) / (before - predictions[best["Id"]])
            phase_change = best["Phases"] != current["Phases"]
            norm = max(abs(a - b) for a, b in zip(best["Coordinates"], current["Coordinates"]))
            current = best
            accepted.append(dict(iteration=iteration, id=best["Id"], maximum=after, relative_reduction=relative, coordinates=best["Coordinates"]))
        updated = next_radius(radius, improved, best["Id"] not in predictions, ratio, norm, phase_change, policy)
        iterations.append(dict(iteration=iteration, radius=radius, next_radius=updated, derivative_step=h, stencils=stencils,
                               proposals=[{k: c[k] for k in ("id", "coordinates", "predicted", "ordinal")} for c in choices],
                               best_id=best["Id"], best_maximum=after if math.isfinite(after) else None, accepted=improved,
                               reduction_ratio=ratio, accepted_phase_change=phase_change))
        print(f"iteration={iteration} maximum={P.H3['H2']['score'](current):.12g} accepted={improved} radius={updated:.4g}", flush=True)
        if not improved and radius <= policy["radius_minimum"]:
            stop = "MINIMUM-RADIUS-NO-DESCENT"
            break
        radius = updated
        if stagnant >= policy["stagnation_updates"]:
            stop = "STAGNATION"
            break
        if evaluations >= policy["maximum_recipes"]:
            stop = "RECIPE-BUDGET"
            break
    if P.H3["H2"]["score"](current) <= 1:
        stop = "ROOT-OBSERVED-UNCONFIRMED"
    return dict(status="PASS-EVIDENCE-COMPLETE", stop=stop, evaluations=evaluations, accepted=accepted,
                iterations=iterations, final=current, root_observed=P.H3["H2"]["score"](current) <= 1,
                r3_passed=False, m10_closed=False, seed_installed=False)


def fixtures():
    results = LINE["fixtures"]()
    P.require(next_radius(1e-4, True, True, None, 1e-6, False, POLICY) == 1e-4, "Accepted probe shrank radius")
    results.append(dict(name="accepted-probe-keeps-radius", passed=True))
    def objective(requests):
        return [dict(**r, Reason="VALID", Residuals=[1e6 * (r["Coordinates"][0] + r["Coordinates"][1] - .005),
                                                  1e6 * (r["Coordinates"][0] - r["Coordinates"][1] - .003)] + [0.] * 73,
                     Phases=["fixture"] * 48) for r in requests]
    initial = objective([dict(Id="fixture", Coordinates=[0.] * 24)])[0]
    result = search(objective, initial)
    P.require(result["root_observed"] and max(abs(a - b) for a, b in zip(result["final"]["Coordinates"][:2], [.004, .001])) <= 1e-6, "Coupled minimax search fixture failed")
    results.append(dict(name="known-coupled-root", passed=True, recipes=result["evaluations"]))
    result = search(objective, initial, dict(POLICY, maximum_recipes=50))
    P.require(result["stop"] == "RECIPE-BUDGET" and result["evaluations"] == 50, "Minimax search budget fixture")
    results.append(dict(name="budget-includes-truncated-trials", passed=True))
    return results


def static():
    P.authenticate(ID, POLICY)
    P.write(OUT, "01-static.json", dict(status="PASS-STATIC", contract_sha256=P.digest((P.ROOT / "eng" / (ID + "-contract.json")).read_bytes()), fixtures=fixtures()))


def execute():
    P.authenticate(ID, POLICY)
    driver = P.Batches(ID, 4096, 193)
    result = search(driver.evaluate)
    result.update(batches=driver.batches, maximum_physical_steps=3 * driver.recipes + 2 * driver.batches)
    P.write(OUT, "04-result.json", result)


def audit(batches, expected):
    cursor = 0
    def replay(requests):
        nonlocal cursor
        records = []
        for offset in range(0, len(requests), 48):
            P.require(cursor < len(batches), "Incomplete minimax physical trace")
            batch = batches[cursor]
            cursor += 1
            chunk = requests[offset:offset + 48]
            P.require(batch["batch"] == cursor and batch["requests"] == chunk and len(batch["records"]) == len(chunk), "Minimax replay request drift")
            for request, record in zip(chunk, batch["records"]):
                P.require(request["Id"] == record["Id"] and request["Coordinates"] == record["Coordinates"], "Minimax physical trace identity drift")
                P.audit_record(record)
            records.extend(batch["records"])
        return records
    result = search(replay)
    result.update(batches=cursor, maximum_physical_steps=3 * result["evaluations"] + 2 * cursor)
    P.require(cursor == len(batches) and result == expected, "Minimax final replay mismatch")
    final = result["final"]
    return dict(status="PASS-INDEPENDENT-MINIMAX-REPLAY", stop=result["stop"], recipes=result["evaluations"], batches=cursor,
                maximum_physical_steps=result["maximum_physical_steps"], accepted_updates=len(result["accepted"]),
                initial_maximum=P.H3["H2"]["score"](start_record()), final_maximum=P.H3["H2"]["score"](final),
                all_in_envelope=all(o["InEnvelope"] for o in final["Observations"]), root_observed=result["root_observed"],
                r3_passed=False, m10_closed=False, seed_installed=False)


def adjudicate():
    P.authenticate(ID, POLICY)
    batches = [json.loads(line) for line in (OUT / "03-batches.jsonl").read_text().splitlines()]
    result = audit(batches, P.load(OUT / "04-result.json"))
    P.write(OUT, "05-adjudication.json", result)
    print(json.dumps(result, indent=2))


def review():
    P.authenticate(ID, POLICY)
    receipt = P.load(P.ROOT / "eng" / (ID + "-returned.json"))
    P.require(receipt["contract_sha256"] == P.digest((P.ROOT / "eng" / (ID + "-contract.json")).read_bytes()), "Returned minimax search contract drift")
    payloads = P.frozen(P.ROOT / "eng/frozen-evidence" / ID, P.ROOT / "eng" / (ID + "-returned.json"))
    result = audit([json.loads(line) for line in payloads["03-batches.jsonl"].decode().splitlines()], json.loads(payloads["04-result.json"]))
    P.require(result == json.loads(payloads["05-adjudication.json"]), "Returned minimax search adjudication drift")
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage", choices=["fixtures", "static", "preexecution", "execute", "adjudicate", "review"])
    args = parser.parse_args()
    {"fixtures": fixtures, "static": static, "preexecution": lambda: (P.authenticate(ID, POLICY), P.preexecution(ID)), "execute": execute, "adjudicate": adjudicate, "review": review}[args.stage]()
