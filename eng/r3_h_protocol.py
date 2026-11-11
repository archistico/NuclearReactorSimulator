"""Shared evidence and batch transport for explicitly frozen, test-only H protocols."""
from __future__ import annotations

import gzip
import json
import math
import os
import re
import runpy
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
H3 = runpy.run_path(str(ROOT / "eng/run-r3-h-trust-search3.py"))
require, digest, load = H3["require"], H3["digest"], H3["load"]
GENERIC = "tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/M10R3HProtocolBatchTests.cs"
ASSEMBLY, PROJECT = H3["H2"]["ASSEMBLY"], H3["H2"]["PROJECT"]


def write(folder, name, value):
    folder.mkdir(parents=True, exist_ok=True)
    (folder / name).write_text(json.dumps(value, indent=2, allow_nan=False) + "\n", encoding="utf-8")


def tree(directory, exclusions=()):
    files = sorted((p for p in (ROOT / directory).rglob("*") if p.is_file()
                    and not {"bin", "obj"}.intersection(p.relative_to(ROOT).parts)
                    and p.relative_to(ROOT).as_posix() not in exclusions), key=lambda p: p.relative_to(ROOT).as_posix())
    rows = "".join(f"{p.relative_to(ROOT).as_posix()}|{digest(p.read_bytes())}\n" for p in files)
    return dict(file_count=len(files), tree_sha256=digest(rows.encode()))


def frozen(folder, receipt_path=None):
    manifest = folder / "manifest.json"
    if receipt_path:
        require(load(receipt_path)["manifest_sha256"] == digest(manifest.read_bytes()), "Frozen manifest identity drift")
    payloads = {}
    for entry in load(manifest)["files"]:
        path = (folder / entry["stored"]).resolve()
        require(path.is_relative_to(folder.resolve()), "Frozen path escape")
        stored = path.read_bytes()
        require(digest(stored) == entry["stored_sha256"], "Frozen stored digest drift")
        data = gzip.decompress(stored) if entry["stored"].endswith(".gz") else stored
        require(digest(data) == entry["sha256"] and len(data) == entry["bytes"], "Frozen logical digest drift")
        require(entry["file"] not in payloads, "Duplicate frozen payload")
        payloads[entry["file"]] = data
    return payloads


def authenticate(protocol, policy):
    require(re.fullmatch(r"r3-h-[a-z0-9]+(?:-[a-z0-9]+)*", protocol) is not None, "Invalid protocol selector")
    path = ROOT / "eng" / (protocol + "-contract.json")
    c = load(path)
    require(c["schema"] == protocol + "-v1" and c["policy"] == policy, "Frozen protocol policy drift")
    require(c["output"] == "artifacts/" + protocol and c["test_additions"] in ([], [GENERIC]), "Unreviewed output or test surface")
    for key in ("src", "tests"):
        require(tree(key, c["test_additions"]) == c["incoming_baseline"][key], "Incoming tree drift: " + key)
    for entry in c["implementation_files"] + c["frozen_inputs"]:
        require(digest((ROOT / entry["path"]).read_bytes()) == entry["sha256"], "Input drift: " + entry["path"])
    return c


def preexecution(protocol):
    out = ROOT / "artifacts" / protocol
    contract = ROOT / "eng" / (protocol + "-contract.json")
    H3["H2"]["test_report"](out / "preexecution.xml", 3, 1)
    require(load(out / "01-static.json")["contract_sha256"] == digest(contract.read_bytes()), "Stale static review")
    write(out, "02-preexecution.json", dict(status="PASS-PREEXECUTION", contract_sha256=digest(contract.read_bytes()), assembly_sha256=digest(ASSEMBLY.read_bytes())))


class Batches:
    def __init__(self, protocol, recipes, batches):
        self.protocol = protocol
        self.out = ROOT / "artifacts" / protocol
        self.contract = ROOT / "eng" / (protocol + "-contract.json")
        self.review = load(self.out / "02-preexecution.json")
        require(self.review["status"] == "PASS-PREEXECUTION" and self.review["contract_sha256"] == digest(self.contract.read_bytes())
                and self.review["assembly_sha256"] == digest(ASSEMBLY.read_bytes()), "Stale execution identity")
        self.trace = self.out / "03-batches.jsonl"
        require(not self.trace.exists(), "Existing execution retained; never overwrite")
        self.maximum_recipes, self.maximum_batches = recipes, batches
        self.recipes, self.batches = 0, 0

    def evaluate(self, requests):
        results = []
        for offset in range(0, len(requests), 48):
            chunk = requests[offset:offset + 48]
            require(self.batches < self.maximum_batches and self.recipes + len(chunk) <= self.maximum_recipes, "Physical budget exceeded")
            write(self.out, "batch-request.json", chunk)
            write(self.out, "batch-token.json", dict(status="PASS-BATCH-REVIEW", contract_sha256=self.review["contract_sha256"],
                  assembly_sha256=self.review["assembly_sha256"], request_sha256=digest((self.out / "batch-request.json").read_bytes())))
            command = ["dotnet", "test", "--project", PROJECT, "--configuration", "Release", "--no-build", "--results-directory", str(self.out),
                       "--minimum-expected-tests", "1", "--", "--explicit", "only", "--filter-method",
                       "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10R3HProtocolBatchTests.ExecuteReviewedProtocolBatch",
                       "--parallel", "none", "--report-xunit", "--report-xunit-filename", "batch-execution.xml"]
            with (self.out / "execution.log").open("a", encoding="utf-8") as log:
                subprocess.run(command, cwd=ROOT, env=dict(os.environ, NRS_R3_H_PROTOCOL=self.protocol), stdout=log, stderr=subprocess.STDOUT, check=True)
            H3["H2"]["test_report"](self.out / "batch-execution.xml", 1, 0)
            records = [json.loads(line) for line in (self.out / "batch-results.jsonl").read_text(encoding="utf-8-sig").splitlines()]
            require(len(records) == len(chunk), "Batch record count mismatch")
            for request, record in zip(chunk, records):
                require(request["Id"] == record["Id"] and request["Coordinates"] == record["Coordinates"], "Batch request identity drift")
            self.batches += 1
            self.recipes += len(chunk)
            with self.trace.open("a", encoding="utf-8") as stream:
                stream.write(json.dumps(dict(batch=self.batches, requests=chunk, records=records), allow_nan=False) + "\n")
            print(f"batch={self.batches} recipes={self.recipes} best={min(map(H3['H2']['score'], records)):.12g}", flush=True)
            results.extend(records)
        return results


def audit_record(record):
    H3["H2"]["verify_physics"](record, load(ROOT / "eng/r3-equilibrium-reconstruction-planning1-contract.json"))
    if record["Residuals"] is not None:
        energy = record["SecondaryEnergy"]
        require(energy["Checkpoint"] == "1" and len(energy["Trains"]) == len(energy["Stages"]) == len(energy["Boundaries"]) == 1, "Incomplete secondary energy observation")
        def finite(value):
            if isinstance(value, dict):
                return all(finite(v) for v in value.values())
            if isinstance(value, list):
                return all(finite(v) for v in value)
            return not isinstance(value, (float, int)) or math.isfinite(value)
        require(finite(energy), "Nonfinite secondary energy observation")


def freeze(protocol, names):
    source = ROOT / "artifacts" / protocol
    target = ROOT / "eng/frozen-evidence" / protocol
    target.mkdir(parents=True, exist_ok=True)
    entries = []
    for name in names:
        data = (source / name).read_bytes()
        stored = name + ".gz" if name.endswith(".jsonl") or name == "ordinary-suite.log" else name
        payload = gzip.compress(data, mtime=0) if stored.endswith(".gz") else data
        require(not (target / stored).exists(), "Frozen file already exists")
        (target / stored).write_bytes(payload)
        entries.append(dict(file=name, stored=stored, bytes=len(data), sha256=digest(data), stored_sha256=digest(payload)))
    write(target, "manifest.json", dict(schema=protocol + "-frozen-v1", files=entries))
    write(ROOT / "eng", protocol + "-returned.json", dict(schema=protocol + "-returned-v1",
          contract_sha256=digest((ROOT / "eng" / (protocol + "-contract.json")).read_bytes()), manifest_sha256=digest((target / "manifest.json").read_bytes())))
