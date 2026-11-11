# M10 Final — VR2 Engineering Repair Planning 1 — RP1C C4 Runtime Factor Isolation 1

## Status

**CANDIDATE — executable evidence-only gate. Local execution not performed in this package.**

Returned Runtime Factor Isolation Planning 1 is frozen as `PASS-AS-AUTHORED`. This candidate implements only the authorized gate `RP1C-C4-EXACT-V9-RUNTIME-FACTOR-ISOLATION1` against immutable C4 and the frozen exact-v9 corpus.

It does not select C4, change production runtime settings, change production code, change the exact-v9 corpus, change the `409.30666666666673 us` ceiling, promote a causal runtime claim, authorize VR3/P3-R1, or authorize a second replacement-long baseline.

## Question

Attribution 1 established strong runtime-configuration sensitivity, but its strongest comparison changed more than one runtime factor. This gate asks a narrower evidence question: when Dynamic PGO and ReadyToRun are fixed, how does exact-v9 wall-clock behavior change across three adjacent single-factor contrasts?

The gate records the answer. Returned-evidence adjudication, not the runner, decides whether any observed separation is sufficiently repeatable and material to support later planning.

## Frozen single-factor chain

All child processes explicitly set `DOTNET_TieredPGO=0` and `DOTNET_ReadyToRun=1`.

| ID | TieredCompilation | TieredPGO | QuickJit | QuickJitForLoops | ReadyToRun |
| --- | ---: | ---: | ---: | ---: | ---: |
| `TIERING-OFF-QJ-OFF-QJFL-OFF` | 0 | 0 | 0 | 0 | 1 |
| `TIERING-ON-QJ-OFF-QJFL-OFF` | 1 | 0 | 0 | 0 | 1 |
| `TIERING-ON-QJ-ON-QJFL-OFF` | 1 | 0 | 1 | 0 | 1 |
| `TIERING-ON-QJ-ON-QJFL-ON` | 1 | 0 | 1 | 1 | 1 |

The only authorized contrasts are:

1. A ↔ B — `DOTNET_TieredCompilation` only.
2. B ↔ C — `DOTNET_TC_QuickJit` only.
3. C ↔ D — `DOTNET_TC_QuickJitForLoops` only.

Dynamic PGO is not a comparator in this gate. A later PGO OFF/ON experiment requires separate planning if it remains material after returned evidence is reviewed.

## Measurement protocol

Each mode runs in five fresh `dotnet test` processes. Every process executes the same frozen exact-v9 workload:

- 360 rows;
- 16 warm-up passes;
- 64 measured passes;
- rotation stride `37`;
- 23,040 measured calls per process;
- 20 processes total;
- 460,800 measured calls total;
- strict max remains `409.30666666666673 us`;
- `100 us` remains a diagnostic tail floor only;
- allocation-neutral measured region;
- raw elapsed time, allocated bytes, resolved state, C4 resolution path, strict flag and diagnostic-tail flag for every call.

The adjudicator verifies the complete deterministic `64 × 360` timing matrix for every process, including pass index, row order, probe ID, logical step and node ID against the frozen RP1A corpus.

## Counterbalanced execution order

The runner freezes this blocked schedule:

- run 1: A B C D;
- run 2: B C D A;
- run 3: C D A B;
- run 4: D A B C;
- run 5: A C B D.

The first four blocks rotate every mode through every sequence position. The fifth block supplies the fifth fresh process per mode without changing the process count or runtime matrix.

## Fail-closed caller environment

Before ordinary or focused execution, the runner fails with `CALLER-RUNTIME-CONFIGURATION-NOT-CLEAN` if the caller already defines any of:

- `DOTNET_TieredCompilation`;
- `DOTNET_TieredPGO`;
- `DOTNET_TC_QuickJit`;
- `DOTNET_TC_QuickJitForLoops`;
- `DOTNET_ReadyToRun`.

The runner does not silently clear parent runtime configuration.

## Evidence tree

A complete execution contains exactly 86 files: 20 process directories × 4 files plus 6 aggregate files.

Each process writes:

1. `01-process-contract.txt`
2. `02-exact-v9-call-timing.csv`
3. `03-runtime-context.txt`
4. `04-process-summary.txt`

The adjudicator writes:

1. `01-contract-and-provenance.txt`
2. `02-runtime-factor-run-summary.csv`
3. `03-single-factor-contrast-summary.csv`
4. `04-tail-row-path-summary.csv`
5. `05-runtime-factor-evidence-summary.txt`
6. `06-runtime-factor-isolation1-summary.txt`

`02-runtime-factor-run-summary.csv` preserves all 20 process summaries and execution sequence indices. `03-single-factor-contrast-summary.csv` reports the three adjacent contrasts using process counts, call counts, diagnostic-tail counts, strict-exceedance counts, maxima, median-of-process-medians and median-of-process-p95 values. Those deltas are observational evidence only.

## Engineering result versus infrastructure RED

A mode may be slower, faster, indistinguishable, negative, or inconclusive without causing the focused xUnit test to fail. Performance observations are written to evidence and remain for returned adjudication.

The gate is RED only for infrastructure/evidence-integrity defects such as dirty caller runtime variables, build/test failure, missing process files, malformed evidence, wrong runtime environment, matrix identity/order drift, corpus drift, baseline drift, allocation-neutrality violation, or aggregate integrity failure.

Therefore `engineering-negative-or-inconclusive-is-infrastructure-red=False` is explicit in the aggregate evidence.

## Authority boundary

After local completion, all current authority remains false:

- `RP1C-SELECTION-AUTHORIZED=False`
- `PRODUCTION-RUNTIME-CHANGE-AUTHORIZED=False`
- `PRODUCTION-REPAIR-AUTHORIZED=False`
- `THRESHOLD-CHANGE-AUTHORIZED=False`
- `EXACT-V9-CHANGE-AUTHORIZED=False`
- `VR3-AUTHORIZED=False`
- `P3-R1-AUTHORIZED=False`
- `SECOND-REPLACEMENT-LONG-AUTHORIZED=False`

The next action after a clean local completion is to return the complete 86-file artifact directory for adjudication.

## Hotfix 1 — Windows PowerShell 5.1 source-decoding correction

The first local execution stopped during static step `[1/4]` because Windows PowerShell 5.1 parsed a UTF-8-without-BOM multiplication sign embedded in the validator source as `Ã—`. No build, focused process, performance measurement or runtime-factor evidence had started.

Hotfix 1 keeps executable gate sources 7-bit ASCII and verifies that invariant in the static validator. The exact UTF-8 documentation marker `64 × 360` remains checked by constructing the multiplication sign at runtime with `[char]0x00D7`. No physics, corpus, threshold, test protocol, runtime matrix, runner, adjudicator or authority boundary changes.
