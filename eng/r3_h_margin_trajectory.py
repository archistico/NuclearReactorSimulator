"""Sensitivities of the unclipped C4 reachability margin, including reported q=0."""
import json

import r3_h_protocol as P
import r3_h_phase_trajectory as T
from r3_h_c4_phase_margin import node_margin


def values(record):
    return [node_margin(n)["reachability_margin"] for n in T.nodes(record)]


def column(base, samples, h):
    same = [(sign, r) for sign, r in samples if r["Reason"] == "VALID" and r["Phases"] == base["Phases"]]
    if not same:
        return None
    if len(same) == 2:
        plus = values(next(r for sign, r in same if sign == 1))
        minus = values(next(r for sign, r in same if sign == -1))
        return [(a - b) / (2 * h) for a, b in zip(plus, minus)]
    sign, r = same[0]
    return [(a - b) / (sign * h) for a, b in zip(values(r), values(base))]


def model():
    protocol = "r3-h-phase-constrained-search8"
    folder = P.ROOT / "eng/frozen-evidence" / protocol
    payloads = P.frozen(folder, P.ROOT / "eng" / (protocol + "-returned.json"))
    result = json.loads(payloads["04-result.json"])
    iteration = result["iterations"][-1]["iteration"]
    inherited = T.model(protocol, iteration)
    records = {r["Id"]: r for line in payloads["03-batches.jsonl"].decode().splitlines() for r in json.loads(line)["records"]}
    columns = []
    for j in range(24):
        samples = [(int(r["Id"].split("/")[-1]), r) for key, r in records.items() if key.startswith(f"i{iteration}/probe/{j}/")]
        columns.append(column(inherited["base"], samples, inherited["derivative_step"]))
    P.require(all(c is not None for c in columns), "Unclipped margin derivative unavailable")
    inherited["margin_jacobian"] = [list(row) for row in zip(*columns)]
    inherited["margins"] = values(inherited["base"])
    return inherited
