# GitHub Ordinary CI Stable Contract V2

Status: `IMPLEMENTED-NOT-HOSTED-VALIDATED`

<!-- NRS-MARKER:CI-STABLE-CONTRACT-V2 -->

## Why V2 exists

The original deterministic CI Hotfix 1 validator was correct as a candidate-scoped immutability gate, but it was left permanently wired into `eng/ci-ordinary.cmd`. It therefore compared the live repository against frozen `src/` and `tests/` snapshots from the Hotfix 1 milestone. That is not a valid permanent CI contract: later authorized production or test evolution must be allowed to enter the ordinary suite.

The raw tree hash was also worktree-EOL-sensitive. A Windows hosted checkout may materialize tracked text as CRLF while another working tree contains LF, even when Git content is semantically identical. Permanent ordinary CI must not classify that representation difference as production drift.

Measured preflight on the returned candidate confirms both failure modes:

- Hotfix 1 frozen `src` raw tree SHA-256: `3BCC1ABA5FA29A67ECD907E575351A82364A26E7D29B8E789722C8104ADBAE28`;
- the same 961 source files with only LF -> CRLF materialization produce `4F08BBBB5C44262FA3012705B6A1533B39BFFF4B6A4B5727590B7F3B1C4977C5` under the old raw-byte algorithm;
- current repository `tests/` contains 399 files while the historical Hotfix 1 contract freezes 396, because later authorized diagnostic work added test-only files.

Therefore updating the old snapshot hashes would only postpone the next false RED; the permanent contract itself must change.

## Permanent contract

V2 protects execution semantics instead of repository content snapshots. It requires the pinned SDK/test runner, warnings-as-errors, deterministic serialized test execution, the complete ordinary suite, no filters, no retries, no continue-on-error and the current frozen-evidence gate. Harness files are checked with normalized text hashes.

It intentionally does **not** freeze `src/` or `tests/` counts or tree hashes. Candidate-specific milestone gates may still freeze those trees when immutability is part of that milestone's authority.

## Provenance

`github-ordinary-ci-deterministic-hotfix1-contract.json` and its validator remain unchanged as historical Hotfix 1 provenance. They are no longer invoked from permanent ordinary CI.

## Authority

This change is CI-harness-only. It does not modify production physics, test semantics, thresholds, Diagnostic 3 REV1 evidence, R3 status or repair ownership.
