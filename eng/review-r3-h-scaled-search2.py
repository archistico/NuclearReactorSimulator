"""Reproduce frozen H2 evidence without calling the plant or requiring old artifacts."""
from __future__ import annotations

import gzip
import json
import runpy
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
FROZEN = ROOT / "eng/frozen-evidence/r3-h-scaled-search2"
OUTPUT = ROOT / "artifacts/r3-h-scaled-search2-review"
MANIFEST_SHA256 = "ABDF99B76E08FBEC63FB2EEECCBF264492ACD82E31F617DCCE7119E4301A03BA"
CONTRACT_SHA256 = "FDB37F413B590A3C479E7D1175F1B910A41B0BECBF93F38F7F417A6865EAC102"


def terminal_predictions(module, batches, current):
    probes = {r["Id"]: r for r in batches[-2]["records"]}
    trials = {r["Id"]: r for r in batches[-1]["records"]}
    jacobian = [[0.] * 24 for _ in range(75)]
    for j, x in enumerate(current["Coordinates"]):
        signs = [1, -1] if abs(x) + 1e-6 <= .02 else [-1 if x > 0 else 1]
        for i in range(75):
            plus = probes[f"i1/probe/{j}/{signs[0]}"]["Residuals"][i]
            minus = probes[f"i1/probe/{j}/-1"]["Residuals"][i] if len(signs) == 2 else current["Residuals"][i]
            jacobian[i][j] = (plus - minus) / (2e-6 if len(signs) == 2 else signs[0] * 1e-6)
    comparisons = []
    for damping in module["POLICY"]["damping"]:
        delta, _ = module["direction"](jacobian, current["Residuals"], current["Coordinates"], damping)
        predicted = []
        for halving in range(21):
            residuals = module["MATH"]["predict"](jacobian, current["Residuals"], delta, 2. ** -halving)
            predicted.append(dict(halving=halving, maximum=max(map(abs, residuals)), plant_observed=halving <= 3))
        actual = [dict(fraction=fraction, maximum=module["score"](trials[f"i1/trial/{damping:g}/{fraction:g}"]))
                  for fraction in module["POLICY"]["fractions"]]
        module["require"](all(p["maximum"] < module["score"](current) for p in predicted), "Terminal predicted descent not reproduced")
        comparisons.append(dict(damping=damping, linear_predictions_only=predicted, actual_frozen_trials=actual))
    return comparisons


def review():
    module = runpy.run_path(str(ROOT / "eng/run-r3-h-scaled-search2.py"))
    require, digest = module["require"], module["digest"]
    require(digest((FROZEN / "manifest.json").read_bytes()) == MANIFEST_SHA256, "Frozen manifest drift")
    require(digest(module["CONTRACT"].read_bytes()) == CONTRACT_SHA256, "Frozen execution contract drift")
    module["authenticate"]()
    payloads = {}
    manifest = json.loads((FROZEN / "manifest.json").read_text(encoding="utf-8"))
    require(manifest["contract_sha256"] == CONTRACT_SHA256, "Manifest contract identity drift")
    for entry in manifest["files"]:
        data = (FROZEN / entry["stored"]).read_bytes()
        require(digest(data) == entry["stored_sha256"], "Stored evidence drift: " + entry["stored"])
        if entry["stored"].endswith(".gz"):
            data = gzip.decompress(data)
        require(len(data) == entry["bytes"] and digest(data) == entry["sha256"], "Logical evidence drift: " + entry["file"])
        require(entry["file"] not in payloads, "Duplicate frozen payload")
        payloads[entry["file"]] = data
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for name in ("03-batches.jsonl", "04-result.json"):
        (OUTPUT / name).write_bytes(payloads[name])
    # Redirect only the replay's owned output; the frozen numerical policy is unchanged.
    module["adjudicate"].__globals__["OUT"] = OUTPUT
    module["adjudicate"]()
    actual = json.loads((OUTPUT / "05-adjudication.json").read_text(encoding="utf-8"))
    require(actual == json.loads(payloads["05-adjudication.json"]), "Returned adjudication mismatch")
    batches = [json.loads(line) for line in payloads["03-batches.jsonl"].decode().splitlines()]
    final = json.loads(payloads["04-result.json"])["final"]
    best = min((r for b in batches for r in b["records"]), key=module["score"])
    localization = json.loads(payloads["07-localization.json"])
    require(localization["best_observed_probe"]["id"] == best["Id"]
            and localization["best_observed_probe"]["maximum"] == module["score"](best)
            and localization["best_observed_probe"]["coordinates"] == best["Coordinates"], "Best probe localization mismatch")
    require(localization["terminal_phase_crossings"] == sum(r["Phases"] != final["Phases"] for r in batches[-1]["records"])
            == localization["terminal_trials"] == 24, "Terminal phase localization mismatch")
    require(localization["hotwell_at_accepted"] == next(n for n in final["Raw"] if n["Id"] == "hotwell")
            and localization["terminal_best_hotwell"] == next(n for n in min(batches[-1]["records"], key=module["score"])["Raw"] if n["Id"] == "hotwell"),
            "Hotwell pressure/phase localization mismatch")
    receipt = dict(status="PASS-FROZEN-H2-REVIEW", plant_calls=0, frozen_manifest_sha256=MANIFEST_SHA256,
                   contract_sha256=CONTRACT_SHA256, result=actual, localization=localization,
                   terminal_prediction_comparison=terminal_predictions(module, batches, final))
    (OUTPUT / "06-review-receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print("PASS: complete frozen trace replayed; 0 plant calls; R3 remains RED.")


if __name__ == "__main__":
    review()
