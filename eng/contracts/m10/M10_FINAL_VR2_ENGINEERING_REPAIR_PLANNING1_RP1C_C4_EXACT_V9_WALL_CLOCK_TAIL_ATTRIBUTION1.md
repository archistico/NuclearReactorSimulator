# M10 Final VR2 Engineering Repair Planning 1 — RP1C C4 Exact-v9 Wall-Clock Tail Attribution 1

## Status

**CANDIDATE — evidence-only attribution implementation authorized by returned Planning 1 adjudication.**

This gate does not modify C4, the exact-v9 corpus, production thermodynamics, thresholds, or exact-v9. It executes the immutable test-only C4 candidate under four explicitly controlled runtime compilation environments and records comparative wall-clock evidence.

## Purpose

Full-Domain Performance Confirmation 1 returned complete valid evidence but did not confirm C4 because two rare exact-v9 calls exceeded the unchanged `409.30666666666673 us` single-call ceiling. Five exact-v9 calls were above the diagnostic `100 us` floor, all on `C2-MIXTURE-PREFIX`, with pass signature `15|17|17|18|18`, zero allocation on those calls, zero measured-region harness allocation, and no measured-region GC collections.

Attribution 1 tests whether that rare tail is sensitive to runtime compilation configuration. It does **not** assume that tiering, Dynamic PGO, scheduling, or a thermodynamic path is causal.

## Runtime modes

Each mode runs in five fresh focused-test processes:

1. `AMBIENT-UNSET-CONTROL`
   - all five planned `DOTNET_*` compilation variables unset
2. `TIERING-OFF`
   - `DOTNET_TieredCompilation=0`
   - `DOTNET_TieredPGO=0`
   - `DOTNET_TC_QuickJit=0`
   - `DOTNET_TC_QuickJitForLoops=0`
   - `DOTNET_ReadyToRun=1`
3. `TIERING-ON-PGO-OFF`
   - `1 / 0 / 1 / 0 / 1`
4. `TIERING-ON-PGO-ON`
   - `1 / 1 / 1 / 0 / 1`

The runner fails closed before any measurement if the caller already defines any of these variables:

- `DOTNET_TieredCompilation`
- `DOTNET_TieredPGO`
- `DOTNET_TC_QuickJit`
- `DOTNET_TC_QuickJitForLoops`
- `DOTNET_ReadyToRun`

It does not silently erase inherited configuration.

## Measurement protocol

Per fresh process:

- 360 frozen exact-v9 rows
- 16 warm-up passes
- 64 measured passes
- rotation stride 37
- 23,040 measured calls

Across the gate:

- 20 fresh processes
- **460,800 measured calls**

The measured region records:

- elapsed microseconds
- per-call allocated bytes
- resolved/unresolved status
- C4 resolution path
- pass index
- exact-v9 row/probe/node identity
- `>100 us` diagnostic-tail flag
- `>409.30666666666673 us` strict-ceiling flag
- whole-region/candidate/harness allocation
- Gen0/Gen1/Gen2 collection deltas

The recorder is preallocated before the forced full collection. Harness allocation inside the measured region must remain zero.

## Evidence contract

Each of the 20 process directories contains four files:

1. `01-process-contract.txt`
2. `02-exact-v9-call-timing.csv`
3. `03-runtime-context.txt`
4. `04-process-summary.txt`

The adjudicator adds six aggregate files:

1. `01-contract-and-provenance.txt`
2. `02-runtime-mode-run-summary.csv`
3. `03-tail-pass-window-summary.csv`
4. `04-tail-row-path-summary.csv`
5. `05-attribution-evidence-summary.txt`
6. `06-attribution1-summary.txt`

Total required evidence: **86 files**.

## Interpretation boundary

`100 us` remains a diagnostic tail floor only. It does not replace or weaken the unchanged engineering ceiling.

The adjudicator deliberately emits:

`ATTRIBUTION-EVIDENCE-COMPLETE-NO-CAUSAL-PROMOTION`

when the evidence tree is complete. It summarizes differences between modes but does not automatically declare tiering, PGO, scheduling, or thermodynamic code causal.

A negative, positive, or inconclusive runtime-mode comparison is evidence rather than an xUnit failure. xUnit/runner RED is reserved for harness, configuration, compilation, or evidence-integrity failure.

## Authority

Authorized now:

- test-only Attribution 1 implementation and execution

Not authorized:

- causal promotion from the runner itself
- RP1C selection
- production repair
- threshold/tolerance changes
- exact-v9 changes
- VR3
- P3-R1
- second replacement-long baseline

Returned evidence must be adjudicated before any next decision.

## REV1 pre-execution hardening

The conclusive pre-execution review strengthens evidence integrity while preserving the Planning 1 experiment exactly at four modes, five fresh processes per mode and 460,800 measured calls.

Execution is now counterbalanced by run block. The first four blocks rotate the four modes through all four execution positions; the fifth block supplies the fifth process per mode. This reduces the chance that long-lived machine drift is confused with a runtime-mode effect.

The frozen RP1A performance baseline is now an independent normalized-text SHA-256 prerequisite. `WriteExactTiming` receives the strict maximum from that pinned baseline instead of duplicating the numeric ceiling in the focused test.

The adjudicator proves the complete `64 × 360` exact-v9 matrix for every process, including deterministic rotation order and frozen `probe_id`, `logical_step` and `node_id`. It also cross-checks process summaries and allocation/GC accounting against the raw timing CSV. Tail row/path aggregates preserve both run indices and run/pass pairs.

These are evidence-quality changes only. C4, the exact-v9 corpus, the `409.30666666666673 us` ceiling, the diagnostic `100 us` floor, and all authority boundaries remain unchanged.

## REV1 interpretation guardrail

The explicit tiered modes preserve the returned Planning 1 value `DOTNET_TC_QuickJitForLoops=0`. Because the target `C2-MIXTURE-PREFIX` implementation contains a loop, a null PGO-on/off difference is not treated as exclusionary evidence for Dynamic PGO. This gate records comparative runtime evidence only; changing QJFL would require a separate planning amendment.
