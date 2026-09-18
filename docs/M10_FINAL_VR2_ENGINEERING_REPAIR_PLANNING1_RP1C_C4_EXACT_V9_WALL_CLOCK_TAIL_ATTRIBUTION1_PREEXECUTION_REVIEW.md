# RP1C C4 Exact-v9 Wall-Clock Tail Attribution 1 — Pre-Execution Review

## Status

`STATIC-PREEXECUTION-REVIEW-PASS`

## Scope review

- C4 candidate remains immutable.
- Production `src/` is not modified.
- Frozen exact-v9 corpus remains immutable.
- RP1C selection is not implemented or authorized.
- The gate is exact-v9-only; seam is not remeasured.

## Protocol review

- 4 runtime modes × 5 fresh processes = 20 fresh processes.
- 360 rows × 64 measured passes = 23,040 calls/process.
- Total = 460,800 measured calls.
- Warm-up remains 16 passes and rotation stride remains 37.
- `100 us` remains diagnostic only.
- Strict max remains `409.30666666666673 us`.
- Evidence tree = 20 × 4 + 6 = 86 files.

## Runtime-environment review

The main runner checks the caller environment before executing the static audit or ordinary suite. If any planned `DOTNET_*` compilation variable is inherited, it stops with `CALLER-RUNTIME-CONFIGURATION-NOT-CLEAN`. The runner therefore cannot manufacture a nominally clean ambient control by silently deleting caller configuration.

Each focused process checks the exact five environment values expected for its runtime mode and writes them into `03-runtime-context.txt`.

## Harness review

- timing recorder is value-type and preallocated
- full collection occurs after warm-up/priming and before measurement
- no strings are created in the measured candidate-call loop
- candidate allocation is measured call-by-call
- whole-region allocation and candidate-allocation sum are recorded separately
- harness allocation must remain exactly zero
- GC generation deltas are recorded
- no performance result is asserted as xUnit PASS/FAIL

## Windows PowerShell 5.1 review

- no `String.Contains(string, StringComparison)` usage in PowerShell
- no `$variable:` interpolation hazard in the adjudicator
- invariant numeric parsing is used
- aggregate output is ASCII
- caller environment checks are performed in CMD without relying on newer PowerShell APIs

## Evidence/adjudication review

The adjudicator reconstructs 460,800 timing rows, verifies each mode/run identity and runtime configuration, recalculates per-run median/p95/max and tail counts, builds pass-window and row/path summaries, and emits no automatic causal conclusion.

## Final pre-execution conclusion

`STATIC-PREEXECUTION-REVIEW-PASS`

Execution is authorized only as an evidence-only attribution experiment. Returned-evidence adjudication remains mandatory before any causal interpretation or RP1C selection planning.

## REV1 conclusive review hardening

A second independent review was performed before execution. It found no certain compile RED in the focused C# test, but identified three weaknesses that could have reduced evidence integrity or forced an avoidable rerun:

1. the adjudicator checked the timing CSV row count but did not prove the full exact-v9 matrix identity/order (`64 × 360`) against the frozen corpus;
2. the focused test loaded the strict ceiling from `06-performance-baseline.csv`, but that frozen baseline was not pinned by the Attribution 1 validator and the per-call flag duplicated the ceiling as a literal;
3. the original runner executed all five processes of each runtime mode as a contiguous block, creating avoidable time/order confounding for a rare wall-clock tail.

REV1 therefore freezes the following hardening without changing C4, exact-v9, thresholds, process count or measured-call count:

- full exact-v9 row identity and deterministic rotation order are independently reconstructed by the adjudicator;
- process summaries, runtime allocation accounting and GC deltas are cross-checked against the raw per-call evidence;
- the frozen RP1A performance baseline is SHA-pinned and the focused test uses the loaded strict ceiling as its single source of truth;
- runtime context additionally records process id, processor count, post-region managed-thread id, server/workstation GC state and GC latency mode;
- the 20 fresh processes use a deterministic counterbalanced blocked order rather than four long mode blocks;
- aggregate run evidence records execution sequence index, and tail row/path evidence records run indices and run/pass pairs.

`STATIC-PREEXECUTION-REVIEW-PASS` remains valid only for this REV1 hardened candidate.

## REV1 Hotfix 1 — validator documentation-marker alignment

The first local REV1 execution stopped during `[1/4]` static audit before build or focused evidence because the validator required the literal phrase `full exact-v9 matrix integrity`, while this review records the hardened invariant as `full exact-v9 row identity` / matrix identity and order.

This was a documentation-marker mismatch only. Hotfix 1 changes the validator marker to `full exact-v9 row identity`, which is the wording actually frozen by this review. No C4 candidate, test logic, runtime-mode matrix, corpus, threshold, runner, adjudicator, source code or authority boundary changes.

A full static marker/hash replay after the correction passes all 68 active checks used by the Attribution 1 REV1 validator. The failed attempt therefore carries no performance or causal evidence.

