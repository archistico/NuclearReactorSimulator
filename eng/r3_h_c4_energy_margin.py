"""Hotwell liquid-boundary energy margin for the authored C4 prefix neighbourhood."""
import r3_h_protocol as P
import r3_h_c4_phase_margin as C4


def node_margin(node):
    volume = C4.HOTWELL_VOLUME_M3 / node["Mass"]
    energy = node["Energy"] / node["Mass"]
    crossings = []
    table = C4.prefix()
    for left, right in zip(table, table[1:]):
        if left[2] != right[2] and min(left[2], right[2]) <= volume <= max(left[2], right[2]):
            fraction = (volume - left[2]) / (right[2] - left[2])
            crossings.append([a + fraction * (b - a) for a, b in zip(left, right)])
    P.require(crossings and 300 <= node["Temperature"] <= 340, "Hotwell boundary outside reviewed neighbourhood")
    boundary = min(crossings, key=lambda row: abs(row[0] - node["Temperature"]))
    P.require(abs(boundary[0] - node["Temperature"]) < 1., "Ambiguous liquid boundary temperature")
    tolerance = max(1e-5, abs(energy) * 1e-10)
    signed = boundary[3] - energy
    return dict(boundary_temperature_k=boundary[0], boundary_energy_j_kg=boundary[3],
                signed_energy_gap_j_kg=signed, existing_energy_root_tolerance_j_kg=tolerance,
                energy_margin_j_kg=signed + tolerance, normalized_margin_mj_kg=(signed + tolerance) / 1e6)


def column(base, samples, h, node_selector):
    def values(record):
        return [node_margin(n)["normalized_margin_mj_kg"] for n in node_selector(record)]
    same = [(s, r) for s, r in samples if r["Reason"] == "VALID" and r["Phases"] == base["Phases"]]
    if not same:
        return None
    if len(same) == 2:
        plus = values(next(r for s, r in same if s == 1))
        minus = values(next(r for s, r in same if s == -1))
        return [(a - b) / (2 * h) for a, b in zip(plus, minus)]
    sign, r = same[0]
    return [(a - b) / (sign * h) for a, b in zip(values(r), values(base))]


def fixtures():
    # Build a point exactly at a real, authenticated prefix liquid boundary.
    row = C4.saturation(320.15)
    mass = C4.HOTWELL_VOLUME_M3 / row[2]
    node = dict(Id="hotwell", Mass=mass, Energy=mass * row[3], Temperature=320.15)
    at = node_margin(node)
    cold = node_margin({**node, "Energy": mass * (row[3] - .01)})
    hot = node_margin({**node, "Energy": mass * (row[3] + .01)})
    P.require(at["energy_margin_j_kg"] > 0 and abs(at["signed_energy_gap_j_kg"]) < 1e-7, "Exact liquid energy boundary not reconstructed")
    P.require(cold["energy_margin_j_kg"] > 0 > hot["energy_margin_j_kg"], "Energy boundary sidedness failed")
    P.require(abs(at["normalized_margin_mj_kg"] * 1e6 - at["energy_margin_j_kg"]) < 1e-18, "Energy margin units changed")
    return [dict(name=name, passed=True) for name in ("exact-energy-boundary", "opposite-sides-of-existing-energy-root-band", "normalization-does-not-change-boundary")]
