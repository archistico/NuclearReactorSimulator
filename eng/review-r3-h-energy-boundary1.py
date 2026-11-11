"""Replay a local hotwell energy-boundary diagnosis; never call or qualify the plant."""
import argparse
import json

import r3_h_protocol as P
import r3_h_phase_trajectory as T
import r3_h_c4_phase_margin as C4
import r3_h_c4_energy_margin as ENERGY

ID = "r3-h-energy-boundary-review1"
OUT = P.ROOT / "artifacts" / ID
PROTOCOLS = ("r3-h-phase-trajectory-trial6", "r3-h-c4-margin-trial7", "r3-h-directional-derivative-trial8")
POLICY = dict(plant_calls=0, reviewed_protocols=list(PROTOCOLS), hotwell_volume_m3=10.,
              reviewed_temperature_k=[300, 340], maximum_boundary_distance_k=1.,
              energy_root_absolute_tolerance_j_kg=1e-5, energy_root_relative_tolerance=1e-10,
              production_change=False, seed_installation=False, r3_passed=False)


def evidence(protocol):
    receipt = P.ROOT / "eng" / (protocol + "-returned.json")
    P.require(P.load(receipt)["contract_sha256"] == P.digest((P.ROOT / "eng" / (protocol + "-contract.json")).read_bytes()), "Input contract identity drift")
    return P.frozen(P.ROOT / "eng/frozen-evidence" / protocol, receipt)


def analyze():
    fixtures = ENERGY.fixtures() + C4.fixtures()
    samples, summaries = [], []
    for protocol in PROTOCOLS:
        data = evidence(protocol)
        records = [r for line in data["03-batches.jsonl"].decode().splitlines() for r in json.loads(line)["records"]]
        P.require(len(records) % 2 == 0, "Incomplete repeated input")
        P.require(len({r["Id"] for r in records}) == len(records), "Duplicate input identity")
        for a, b in zip(records[::2], records[1::2]):
            P.require({**a, "Id": "repeat"} == {**b, "Id": "repeat"}, "Input repeats differ")
        for record in records:
            P.audit_record(record)
            P.require(record["Reason"] == "VALID", "Unresolved input cannot support boundary reconstruction")
            for checkpoint, node in zip(("raw", "seed-step1", "post-seed", "1"), T.nodes(record)):
                energy, volume = ENERGY.node_margin(node), C4.node_margin(node)
                P.require(node["Phase"] in ("SaturatedMixture", "SubcooledLiquid"), "Unexpected phase outside reviewed neighbourhood")
                predicted = "SaturatedMixture" if energy["energy_margin_j_kg"] >= 0 else "SubcooledLiquid"
                P.require(predicted == node["Phase"], "Local energy-boundary explanation does not match an observed phase")
                samples.append(dict(protocol=protocol, id=record["Id"], checkpoint=checkpoint, phase=node["Phase"],
                                    temperature_k=node["Temperature"], reported_quality=node["Quality"],
                                    energy=energy, volume=volume, observed_phase_reproduced=True))
        summaries.append(dict(protocol=protocol, recipes=len(records), distinct_repeated_points=len(records)//2, checkpoints=len(records)*4))
    directional_data = evidence(PROTOCOLS[-1])
    directional = json.loads(directional_data["05-adjudication.json"])
    checkpoints = [s for s in samples if s["protocol"] == PROTOCOLS[-1] and s["checkpoint"] == "1" and s["id"].endswith("/repeat/1")]
    volume_counterexamples = [s for s in samples if s["phase"] == "SubcooledLiquid" and s["volume"]["reachability_margin"] > 0]
    P.require(volume_counterexamples, "Volume-only counterexample was not reproduced")
    small = next(c for c in directional["comparisons"] if c["fraction"] == .001)
    smallest = next(c for c in directional["comparisons"] if c["fraction"] == .0001)
    P.require(small["phase_compatible"] and small["actual_reduction"] > 0 and small["maximum_error"] > small["actual_reduction"], "Small-step model limitation drift")
    P.require(smallest["phase_compatible"] and smallest["actual_reduction"] < 0, "Smallest-step reversal not reproduced")
    return dict(status="PASS-LOCAL-ENERGY-BOUNDARY-REVIEW", contract_sha256=P.digest((P.ROOT / "eng" / (ID + "-contract.json")).read_bytes()),
                fixtures=fixtures, plant_calls=0, inputs=summaries, samples=samples,
                volume_only_counterexamples=volume_counterexamples, directional_running_checkpoints=checkpoints,
                derivative_limit=dict(small_step=small, smallest_step=smallest,
                                      stable_first_order_model_demonstrated=False),
                scope="Observed hotwell neighbourhood only; no global C4 classifier or solver equivalence proof",
                next_activity="MULTISCALE-RESIDUAL-AND-ENERGY-MARGIN-DERIVATIVE-REVIEW-BEFORE-NEW-SEARCH",
                optimizer_guard_qualified=False, r3_passed=False, m10_closed=False, seed_installed=False)


def execute():
    P.authenticate(ID, POLICY)
    result = analyze()
    P.write(OUT, "01-analysis.json", result)
    print(json.dumps({k: v for k, v in result.items() if k not in ("samples", "volume_only_counterexamples", "directional_running_checkpoints", "derivative_limit")}, indent=2))


def review():
    P.authenticate(ID, POLICY)
    data = evidence(ID)
    result = analyze()
    P.require(result == json.loads(data["01-analysis.json"]), "Frozen energy-boundary analysis drift")
    print(json.dumps(dict(status=result["status"], samples=len(result["samples"]),
                          volume_only_counterexamples=len(result["volume_only_counterexamples"]), plant_calls=0), indent=2))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage", choices=["execute", "review"])
    {"execute": execute, "review": review}[parser.parse_args().stage]()
