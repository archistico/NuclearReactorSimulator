"""Read-only reconstruction of C4's signed saturation volume/reachability margin."""
import bisect
import functools
import math
import struct

import r3_h_protocol as P

PAYLOAD = P.ROOT / "src/NuclearReactorSimulator.Simulation/Physics/Fluids/ReferenceData/NRSVR2C4.v1.bin"
PAYLOAD_SHA256 = "EF49B1D097FC63F1F1254E425F46C6ACA837B82C58EFBA9C7774727C51C82267"
HOTWELL_VOLUME_M3 = 10.


@functools.lru_cache(maxsize=1)
def prefix():
    data = PAYLOAD.read_bytes()
    P.require(P.digest(data) == PAYLOAD_SHA256 and data[:8] == b"NRSVR2C4", "C4 payload identity drift")
    header = struct.unpack_from("<6i", data, 8)
    P.require(header == (1, 0x01020304, 17502, 701, 351, 401), "C4 payload layout drift")
    return [struct.unpack_from("<8d", data, 32 + 64 * (header[2] + i)) for i in range(header[3])]


def saturation(temperature):
    table = prefix()
    P.require(table[0][0] <= temperature <= table[-1][0], "Temperature outside C4 prefix")
    index = min(max(bisect.bisect_right([row[0] for row in table], temperature) - 1, 0), len(table) - 2)
    left, right = table[index:index + 2]
    fraction = (temperature - left[0]) / (right[0] - left[0])
    return [a + fraction * (b - a) for a, b in zip(left, right)]


def margin_from_volumes(specific_volume, liquid_volume, vapor_volume):
    span = vapor_volume - liquid_volume
    P.require(span > 0 and specific_volume > 0, "Invalid saturation volumes")
    tolerance = max(1e-14, abs(specific_volume) * 1e-10)
    signed = (specific_volume - liquid_volume) / span
    return dict(signed_quality=signed, volume_tolerance_m3_kg=tolerance,
                reachability_margin=signed + tolerance / span, reachable_by_volume=specific_volume >= liquid_volume - tolerance)


def node_margin(node):
    row = saturation(node["Temperature"])
    result = margin_from_volumes(HOTWELL_VOLUME_M3 / node["Mass"], row[2], row[4])
    if node["Phase"] == "SaturatedMixture":
        P.require(node["Quality"] is not None and abs(min(1., max(0., result["signed_quality"])) - node["Quality"]) <= 5e-17,
                  "Reported C4 quality not reproduced from prefix and geometry")
    return result


def fixtures():
    result = []
    v, liquid, vapor = .001, .001, 10.
    tolerance = max(1e-14, v * 1e-10)
    exact = margin_from_volumes(v, liquid, vapor)
    inside = margin_from_volumes(v - .5 * tolerance, liquid, vapor)
    outside = margin_from_volumes(v - 2 * tolerance, liquid, vapor)
    P.require(exact["signed_quality"] == 0 and exact["reachability_margin"] > 0, "Saturation boundary lost its existing C4 tolerance")
    P.require(inside["signed_quality"] < 0 < inside["reachability_margin"] and inside["reachable_by_volume"], "Clipped-zero interior fixture failed")
    P.require(outside["reachability_margin"] < 0 and not outside["reachable_by_volume"], "Out-of-branch volume accepted")
    result.extend(dict(name=name, passed=True) for name in ("exact-saturation-has-reachability-margin", "negative-signed-quality-can-clamp-to-zero", "beyond-original-tolerance-is-outside"))
    for row in (prefix()[0], prefix()[94], prefix()[-1]):
        actual = saturation(row[0])
        P.require(max(abs(a - b) for a, b in zip(actual, row)) < 1e-9, "Prefix node reconstruction failed")
    result.append(dict(name="authenticated-prefix-node-interpolation", passed=True))
    return result
