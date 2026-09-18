# RP1C C4 Exact-v9 Tail Attribution 1 — REV1 Conclusive Review

## Verdict

`GO-AFTER-REV1-HARDENING`

The original Attribution 1 candidate showed no certain compile-time defect in static review, but was not approved for execution because its integrity adjudicator could accept a 23,040-row CSV without proving that it contained every frozen exact-v9 row exactly once per measured pass. The runner also grouped all five processes of each runtime mode contiguously, which was an avoidable time/order confound for a rare wall-clock-tail experiment.

## Blocking findings corrected

### 1. Full matrix integrity

Original behavior: count/mode/run/pass-range checks only.

REV1 behavior: for each process and each of the 23,040 raw rows, the adjudicator independently derives the expected measured pass and the expected row under stride 37, then verifies frozen `probe_id`, `logical_step` and `node_id` against the 360-row corpus.

This closes a false-PASS path in which a duplicated row could replace a missing row without changing the CSV line count.

### 2. Strict-ceiling provenance

Original behavior: the process summary used `06-performance-baseline.csv`, while the per-call strict flag duplicated `409.30666666666673` as a C# literal; the performance-baseline file itself was not pinned by the Attribution 1 validator.

REV1 behavior: the frozen performance baseline is independently SHA-pinned and the loaded ceiling is passed to the per-call writer. The adjudicator confirms the same ceiling against the pinned baseline.

### 3. Runtime-mode execution order

Original behavior: `A×5 -> B×5 -> C×5 -> D×5`.

REV1 behavior: deterministic counterbalanced blocks:

`A1 B1 C1 D1 | B2 C2 D2 A2 | C3 D3 A3 B3 | D4 A4 B4 C4 | A5 C5 B5 D5`

The first four blocks rotate each mode through every execution position once. This does not eliminate OS scheduling noise, but materially reduces systematic time/order confounding.

## Additional hardening

- run summary includes `sequence_index`;
- row/path tail summary includes `run_indices` and `run_pass_pairs`;
- runtime context records process id, processor count, managed-thread id after the measured region, server-GC state and GC latency mode;
- runtime allocation accounting is checked as `whole = candidate + harness`;
- process median/p95/max/allocation/GC fields are recomputed from raw evidence and compared with each process summary;
- runner, adjudicator, focused test, C4, exact-v9 corpus, performance baseline and returned planning artifacts are independently normalized-LF SHA-pinned;
- Windows PowerShell 5.1 hazards previously encountered remain explicitly prohibited.

## Remaining limitations that are not REDs

The environment variables prove the requested child-process configuration, not a low-level JIT event trace. The ambient mode intentionally means “all five planned variables unset”; it does not claim that the runtime's effective defaults are independently introspected. Attribution remains comparative evidence only, and no result is automatically promoted to a causal or RP1C-selection conclusion.

The returned Planning 1 matrix freezes `DOTNET_TC_QuickJitForLoops=0` for the explicit tiered modes. The observed tail path is `C2-MIXTURE-PREFIX`, whose C4 implementation contains an indexed loop. .NET tiering design documentation notes that disabling Quick JIT for loops can cause loop-bearing methods to enter fully optimized code without the ordinary Tier0 instrumentation path used by Dynamic PGO. REV1 deliberately does **not** change this frozen mode matrix. Consequence: the experiment remains useful as comparative runtime/tiering evidence, but a null `TIERING-ON-PGO-OFF` versus `TIERING-ON-PGO-ON` result must not be interpreted as evidence that Dynamic PGO is irrelevant to the target path. A dedicated QJFL=1 comparator would require a separately adjudicated planning amendment.

## Authority

Unchanged:

- RP1C selection: not authorized;
- production repair: not authorized;
- thresholds: unchanged;
- exact-v9: unchanged;
- C4 source: immutable.
