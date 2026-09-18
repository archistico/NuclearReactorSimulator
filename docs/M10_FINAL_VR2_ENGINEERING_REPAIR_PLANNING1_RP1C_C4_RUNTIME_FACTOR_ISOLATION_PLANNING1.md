# M10 Final — VR2 Engineering Repair Planning 1 — RP1C C4 Runtime Factor Isolation Planning 1

## Status

**CANDIDATE — planning only.**

Attribution 1 REV1 returned evidence is adjudicated PASS as comparative evidence. It establishes strong runtime-configuration sensitivity, rejects `TIERING-OFF` as a repair direction, and does not establish Dynamic PGO causality, ambient-default equivalence, or RP1C selection readiness.

This planning gate freezes the next evidence-only experiment. It does **not** implement that experiment, change C4, change the exact-v9 corpus, change the `409.30666666666673 us` ceiling, change runtime configuration in production, select C4, or authorize RP1C/VR3/P3-R1.

## Why another isolation gate is required

Attribution 1 compared useful runtime bundles, but its largest contrast was not single-factor: `TIERING-OFF` and `TIERING-ON-PGO-OFF` differed in both `DOTNET_TieredCompilation` and `DOTNET_TC_QuickJit`. The returned evidence therefore proves runtime sensitivity without isolating which component of that bundle owns the difference.

The target path, `C2-MIXTURE-PREFIX`, also contains a loop. Attribution 1 preserved `DOTNET_TC_QuickJitForLoops=0`, so a null PGO OFF/ON difference cannot exclude Dynamic PGO for this path. PGO therefore remains outside this first isolation gate.

## Frozen prerequisite evidence

The planning gate relies only on the returned Attribution 1 evidence already frozen in the repository:

- 4 runtime modes × 5 fresh processes = 20 processes;
- 460,800 exact-v9 measured calls;
- `AMBIENT-UNSET-CONTROL`: 16 calls above `100 us`, 2 strict exceedances, max `724.2 us`;
- `TIERING-OFF`: 91,283 calls above `100 us`, 64 strict exceedances, max `15497.2 us`;
- `TIERING-ON-PGO-OFF`: 18 calls above `100 us`, 0 strict exceedances, max `273.9 us`;
- `TIERING-ON-PGO-ON`: 28 calls above `100 us`, 0 strict exceedances, max `330.4 us`;
- zero candidate allocation, zero harness allocation, zero unresolved calls and zero Gen0/Gen1/Gen2 collections in every measured region.

`100 us` remains a diagnostic floor only. It is not a new qualification threshold.

## Future gate

The separately versioned future gate is:

`RP1C-C4-EXACT-V9-RUNTIME-FACTOR-ISOLATION1`

It is evidence-only and must use the immutable C4 candidate and frozen exact-v9 corpus.

### Frozen mode chain

All four modes keep `DOTNET_TieredPGO=0` and `DOTNET_ReadyToRun=1`. Each adjacent pair changes exactly one runtime factor.

| Mode | TieredCompilation | TieredPGO | QuickJit | QuickJitForLoops | ReadyToRun | Single-factor purpose |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| `TIERING-OFF-QJ-OFF-QJFL-OFF` | 0 | 0 | 0 | 0 | 1 | lower anchor |
| `TIERING-ON-QJ-OFF-QJFL-OFF` | 1 | 0 | 0 | 0 | 1 | isolate TieredCompilation versus mode A |
| `TIERING-ON-QJ-ON-QJFL-OFF` | 1 | 0 | 1 | 0 | 1 | isolate QuickJit versus mode B |
| `TIERING-ON-QJ-ON-QJFL-ON` | 1 | 0 | 1 | 1 | 1 | isolate QuickJitForLoops versus mode C |

Required contrasts:

1. `A -> B`: `DOTNET_TieredCompilation` only.
2. `B -> C`: `DOTNET_TC_QuickJit` only.
3. `C -> D`: `DOTNET_TC_QuickJitForLoops` only.

No contrast in this gate changes Dynamic PGO.

## Measurement protocol

To remain directly comparable with Attribution 1:

- 5 fresh processes per mode;
- 4 modes;
- 20 fresh processes total;
- 360 exact-v9 rows;
- 16 warm-up passes;
- 64 measured passes;
- stride `37` row rotation;
- 23,040 measured calls per process;
- 460,800 measured calls total;
- immutable strict ceiling `409.30666666666673 us`;
- diagnostic tail floor `100 us` only;
- allocation-neutral harness;
- candidate allocation, GC deltas, measured pass, row/probe/node identity and C4 resolution path recorded.

The 20 processes must use a counterbalanced blocked-by-run schedule so that mode identity is not confounded with elapsed experiment order.

## Evidence interpretation

The future runner/adjudicator may establish only comparative evidence. It must not automatically promote any causal verdict or runtime-production decision.

Returned-evidence adjudication must determine separately whether:

- enabling tiered compilation while QuickJit remains off materially changes central tendency or strict-tail behavior;
- enabling QuickJit while QJFL remains off materially changes behavior;
- enabling QuickJitForLoops materially changes behavior on the loop-bearing target path;
- any observed separation repeats across fresh processes rather than being carried by one isolated maximum.

A negative or inconclusive factor result is valid engineering evidence, not a harness failure.

## PGO remains a later conditional gate

Dynamic PGO is deliberately not isolated here. If, after returned Runtime Factor Isolation 1 adjudication, PGO remains a material hypothesis, create a **separate planning gate** that holds:

- `DOTNET_TieredCompilation=1`;
- `DOTNET_TC_QuickJit=1`;
- `DOTNET_TC_QuickJitForLoops=1`;
- `DOTNET_ReadyToRun=1`;

and changes only `DOTNET_TieredPGO=0` versus `1`.

Do not fold that comparator into Runtime Factor Isolation 1 after the fact.

## Caller environment

The future runner must fail closed before changing child-process settings if any of these variables is already set in the caller:

- `DOTNET_TieredCompilation`
- `DOTNET_TieredPGO`
- `DOTNET_TC_QuickJit`
- `DOTNET_TC_QuickJitForLoops`
- `DOTNET_ReadyToRun`

Failure classification: `CALLER-RUNTIME-CONFIGURATION-NOT-CLEAN`.

## Planned evidence tree

The future experiment preserves the Attribution 1 shape:

- 20 process directories;
- 4 files per process;
- 6 aggregate files;
- **86 files total**.

The process files are:

1. `01-process-contract.txt`
2. `02-exact-v9-call-timing.csv`
3. `03-runtime-context.txt`
4. `04-process-summary.txt`

The aggregate files are:

1. `01-contract-and-provenance.txt`
2. `02-runtime-factor-run-summary.csv`
3. `03-single-factor-contrast-summary.csv`
4. `04-tail-row-path-summary.csv`
5. `05-runtime-factor-evidence-summary.txt`
6. `06-runtime-factor-isolation1-summary.txt`

## Authority boundary

This planning gate authorizes nothing beyond planning itself.

Current authority remains:

- Runtime Factor Isolation 1 implementation: **not yet authorized**;
- RP1C selection: **not authorized**;
- production/runtime configuration change: **not authorized**;
- C4 mutation: **not authorized**;
- threshold change: **not authorized**;
- exact-v9 change: **not authorized**;
- VR3: **not authorized**;
- P3-R1: **not authorized**;
- second replacement-long: **not authorized**.

After a local planning PASS, return the complete four-file planning artifact directory for adjudication before implementing Runtime Factor Isolation 1.
