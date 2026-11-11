"""Evidence and batch transport for frozen, test-only non-fluid operating-point protocols."""
import json
import os
import re
import subprocess

import r3_h_protocol as P

GAMEPLAY = "tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/"
EXECUTOR = GAMEPLAY + "M10R3OperatingPointProtocolBatchTests.cs"
HARNESS = GAMEPLAY + "R3HFeasibilityHarness.cs"
METHOD = "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10R3OperatingPointProtocolBatchTests.ExecuteReviewedProtocolBatch"
QUANTITIES = ("control-valve-percent-open", "condensate-pump-percent", "feedwater-pump-percent",
              "neutron-population-relative", "fuel-temperature-celsius", "structure-temperature-celsius")
FRACTIONAL_BOUND = .02


def contract_path(protocol):
    P.require(re.fullmatch(r"r3-op-[a-z0-9]+(?:-[a-z0-9]+)*", protocol) is not None, "Invalid protocol selector")
    return P.ROOT / "eng" / (protocol + "-contract.json")


def authenticate(protocol, policy):
    c = P.load(contract_path(protocol))
    P.require(c["schema"] == protocol + "-v1" and c["policy"] == policy, "Frozen protocol policy drift")
    P.require(c["output"] == "artifacts/" + protocol, "Unreviewed output surface")
    P.require(c["test_additions"] == [EXECUTOR] and c["test_modifications"] == [HARNESS], "Unreviewed test surface")
    P.require(P.tree("tests", (EXECUTOR, HARNESS)) == c["unchanged_tests_baseline"], "Unchanged test tree drift")
    for key in ("src", "tests"):
        P.require(P.tree(key) == c["incoming_baseline"][key], "Incoming tree drift: " + key)
    for entry in c["implementation_files"] + c["frozen_inputs"]:
        P.require(P.digest((P.ROOT / entry["path"]).read_bytes()) == entry["sha256"], "Input drift: " + entry["path"])
    return c


def request(identity, coordinates, operating_point):
    P.require(len(coordinates) == 24 and len(operating_point) == len(QUANTITIES), "Request shape drift")
    P.require(all(abs(x) <= FRACTIONAL_BOUND for x in list(coordinates) + list(operating_point)), "Request outside frozen bounds")
    return dict(Id=identity, Coordinates=list(coordinates), OperatingPoint=list(operating_point))


def preexecution(protocol):
    out = P.ROOT / "artifacts" / protocol
    contract = contract_path(protocol)
    P.H3["H2"]["test_report"](out / "preexecution.xml", 3, 1)
    P.require(P.load(out / "01-static.json")["contract_sha256"] == P.digest(contract.read_bytes()), "Stale static review")
    P.write(out, "02-preexecution.json", dict(status="PASS-PREEXECUTION", contract_sha256=P.digest(contract.read_bytes()),
                                              assembly_sha256=P.digest(P.ASSEMBLY.read_bytes())))


def same_request(sent, returned):
    return all(sent[key] == returned[key] for key in ("Id", "Coordinates", "OperatingPoint"))


class Batches:
    def __init__(self, protocol, recipes, batches):
        self.protocol = protocol
        self.out = P.ROOT / "artifacts" / protocol
        self.contract = contract_path(protocol)
        self.review = P.load(self.out / "02-preexecution.json")
        P.require(self.review["status"] == "PASS-PREEXECUTION" and self.review["contract_sha256"] == P.digest(self.contract.read_bytes())
                  and self.review["assembly_sha256"] == P.digest(P.ASSEMBLY.read_bytes()), "Stale execution identity")
        self.trace = self.out / "03-batches.jsonl"
        P.require(not self.trace.exists(), "Existing execution retained; never overwrite")
        self.maximum_recipes, self.maximum_batches = recipes, batches
        self.recipes, self.batches = 0, 0

    def evaluate(self, requests):
        results = []
        for offset in range(0, len(requests), 48):
            chunk = requests[offset:offset + 48]
            P.require(self.batches < self.maximum_batches and self.recipes + len(chunk) <= self.maximum_recipes, "Physical budget exceeded")
            P.write(self.out, "batch-request.json", chunk)
            P.write(self.out, "batch-token.json", dict(status="PASS-BATCH-REVIEW", contract_sha256=self.review["contract_sha256"],
                    assembly_sha256=self.review["assembly_sha256"], request_sha256=P.digest((self.out / "batch-request.json").read_bytes())))
            command = ["dotnet", "test", "--project", P.PROJECT, "--configuration", "Release", "--no-build", "--results-directory", str(self.out),
                       "--minimum-expected-tests", "1", "--", "--explicit", "only", "--filter-method", METHOD,
                       "--parallel", "none", "--report-xunit", "--report-xunit-filename", "batch-execution.xml"]
            with (self.out / "execution.log").open("a", encoding="utf-8") as log:
                subprocess.run(command, cwd=P.ROOT, env=dict(os.environ, NRS_R3_OP_PROTOCOL=self.protocol), stdout=log, stderr=subprocess.STDOUT, check=True)
            P.H3["H2"]["test_report"](self.out / "batch-execution.xml", 1, 0)
            records = [json.loads(line) for line in (self.out / "batch-results.jsonl").read_text(encoding="utf-8-sig").splitlines()]
            P.require(len(records) == len(chunk) and all(map(same_request, chunk, records)), "Batch request identity drift")
            self.batches += 1
            self.recipes += len(chunk)
            with self.trace.open("a", encoding="utf-8") as stream:
                stream.write(json.dumps(dict(batch=self.batches, requests=chunk, records=records), allow_nan=False) + "\n")
            print(f"batch={self.batches} recipes={self.recipes}", flush=True)
            results.extend(records)
        return results


def frozen(protocol):
    receipt = P.ROOT / "eng" / (protocol + "-returned.json")
    P.require(P.load(receipt)["contract_sha256"] == P.digest(contract_path(protocol).read_bytes()), "Contract identity drift")
    return P.frozen(P.ROOT / "eng/frozen-evidence" / protocol, receipt)
