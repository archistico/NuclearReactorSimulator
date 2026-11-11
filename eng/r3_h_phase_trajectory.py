"""Reconstruct residual, pressure and quality sensitivities from authenticated plant probes."""
import json

import r3_h_protocol as P


def nodes(record):
    return [next(n for n in group if n["Id"] == "hotwell") for group in
            [record["Raw"]] + [o["Nodes"] for o in record["Observations"]]]


def model(protocol, iteration=0):
    folder = P.ROOT / "eng/frozen-evidence" / protocol
    receipt = P.ROOT / "eng" / (protocol + "-returned.json")
    payloads = P.frozen(folder, receipt)
    result = json.loads(payloads["04-result.json"])
    detail = result["iterations"][iteration]
    records = {r["Id"]: r for line in payloads["03-batches.jsonl"].decode().splitlines() for r in json.loads(line)["records"]}
    prior = [a for a in result["accepted"] if a["iteration"] < iteration]
    base = records[prior[-1]["id"]] if prior else records["start"]
    base_nodes = nodes(base)
    h = detail["derivative_step"]
    residual_columns, pressure_columns, quality_columns, stencils = [], [], [], []
    for j in range(24):
        samples = [(int(r["Id"].split("/")[-1]), r) for key, r in records.items() if key.startswith(f"i{iteration}/probe/{j}/")]
        residual, stencil = P.H3["compatible_column"](base, samples, h)
        P.require(residual is not None, "Unavailable phase-compatible residual column")
        selected = [(sign, r) for sign, r in samples if r["Reason"] == "VALID" and r["Phases"] == base["Phases"]]
        P.require(len(selected) in (1, 2), "Unexpected derivative stencil")
        def derivative(field):
            if len(selected) == 2:
                plus = nodes(next(r for sign, r in selected if sign == 1))
                minus = nodes(next(r for sign, r in selected if sign == -1))
                return [(a[field] - b[field]) / (2 * h) for a, b in zip(plus, minus)]
            sign, r = selected[0]
            return [(a[field] - b[field]) / (sign * h) for a, b in zip(nodes(r), base_nodes)]
        residual_columns.append(residual)
        pressure_columns.append(derivative("Pressure"))
        quality_columns.append(derivative("Quality") if all(n["Quality"] is not None for n in base_nodes) else None)
        stencils.append(stencil)
    return dict(protocol=protocol, iteration=iteration, base=base, derivative_step=h, stencils=stencils,
                residual_jacobian=[list(row) for row in zip(*residual_columns)],
                pressure_jacobian=[list(row) for row in zip(*pressure_columns)],
                quality_jacobian=[list(row) for row in zip(*quality_columns)] if all(c is not None for c in quality_columns) else None,
                trials=[r for key, r in records.items() if key.startswith(f"i{iteration}/trial/")])


def predict(values, jacobian, delta, alpha=1.):
    return [v + alpha * sum(a * d for a, d in zip(row, delta)) for v, row in zip(values, jacobian)]
