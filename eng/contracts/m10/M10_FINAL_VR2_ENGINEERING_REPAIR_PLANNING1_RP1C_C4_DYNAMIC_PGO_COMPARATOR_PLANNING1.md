# M10 Final — VR2 Engineering Repair Planning 1 — RP1C C4 Dynamic PGO Comparator Planning 1

## Status

**CANDIDATE — planning only.**

Runtime Factor Isolation 1 returned evidence is adjudicated PASS. The single-factor A↔B contrast establishes TieredCompilation as materially causal for the gross exact-v9 slowdown under the frozen experiment. QuickJit is not promoted as a rare-tail causal owner, and QuickJitForLoops shows no material tail benefit.

Dynamic PGO remains the final compilation factor that is materially useful to isolate before any Runtime Configuration Impact Assessment. This planning gate defines that comparator only. It does not implement it, select C4, change production runtime settings, mutate C4, alter exact-v9, change thresholds, or authorize VR3/P3-R1.

## Why PGO remains material

Attribution 1 compared Dynamic PGO OFF versus ON while `DOTNET_TC_QuickJitForLoops=0`. Both explicit tiering-on modes were strict-clean, but that comparison did not cover PGO behavior with QuickJitForLoops enabled on the loop-bearing target path.

Runtime Factor Isolation 1 has now established:

- TieredCompilation ON is materially required to avoid the gross slowdown;
- QuickJit adds at most a small central-performance improvement and does not have repeated rare-tail evidence;
- QuickJitForLoops does not materially improve the tail;
- mode D (`TieredCompilation=1`, `QuickJit=1`, `QuickJitForLoops=1`, `PGO=0`, `ReadyToRun=1`) is strict-clean in 5/5 processes.

Before proposing a process-wide runtime configuration, the project should know whether PGO may remain ON. The comparator therefore answers a bounded question: **with the other four compilation settings fixed, does changing only Dynamic PGO materially alter the exact-v9 distribution or strict tail?**

## Future gate

The separately versioned future gate is:

`RP1C-C4-EXACT-V9-DYNAMIC-PGO-COMPARATOR1`

It remains evidence-only.

### Frozen two-mode comparator

| Mode | TieredCompilation | TieredPGO | QuickJit | QuickJitForLoops | ReadyToRun |
| --- | ---: | ---: | ---: | ---: | ---: |
| `PGO-OFF-QJFL-ON` | 1 | 0 | 1 | 1 | 1 |
| `PGO-ON-QJFL-ON` | 1 | 1 | 1 | 1 | 1 |

The only changed factor is `DOTNET_TieredPGO`.

The future gate must not add an ambient/unset mode. Effective-default equivalence belongs to the later Runtime Configuration Impact Assessment and must not be mixed into this single-factor comparator.

## Frozen measurement protocol

For direct comparability with Attribution 1 and Runtime Factor Isolation 1:

- 2 runtime modes;
- 5 fresh processes per mode;
- 10 fresh processes total;
- 360 frozen exact-v9 rows;
- 16 warm-up passes;
- 64 measured passes;
- rotation stride `37`;
- 23,040 measured calls per process;
- 230,400 measured calls total;
- strict max unchanged at `409.30666666666673 us`;
- `100 us` remains diagnostic-only;
- allocation-neutral measured region;
- raw per-call elapsed time, allocation, resolved state, C4 resolution path, diagnostic-tail flag and strict flag;
- runtime context per process;
- complete `64 × 360` identity/order verification per process.

The two modes must be run as fresh child processes in counterbalanced run blocks. A planned five-block order is:

1. OFF, ON
2. ON, OFF
3. OFF, ON
4. ON, OFF
5. OFF, ON

No early stopping or extra sampling is allowed after results are visible.

## Planned evidence tree

The future comparator contains exactly 46 files:

- 10 process directories × 4 files = 40;
- 6 aggregate files.

Per process:

1. `01-process-contract.txt`
2. `02-exact-v9-call-timing.csv`
3. `03-runtime-context.txt`
4. `04-process-summary.txt`

Aggregate:

1. `01-contract-and-provenance.txt`
2. `02-pgo-run-summary.csv`
3. `03-pgo-contrast-summary.csv`
4. `04-tail-row-path-summary.csv`
5. `05-pgo-evidence-summary.txt`
6. `06-dynamic-pgo-comparator1-summary.txt`

The runner may report negative or inconclusive engineering evidence without failing the harness. Infrastructure, environment, matrix-integrity, corpus-integrity, allocation-neutrality and evidence-shape defects remain RED.

## Returned-evidence interpretation

The future runner must not auto-promote Dynamic PGO causality.

Returned adjudication must determine separately whether:

- PGO OFF/ON changes median/p95 materially;
- diagnostic-tail count changes are repeated across fresh processes;
- any strict-exceedance difference is repeated rather than carried by one isolated maximum;
- the same row/path repeatedly owns any separation;
- a PGO setting is actually necessary before Branch A2.

If both modes are effectively equivalent and strict-clean, the preferred next planning question is not another compilation micro-factor. It is Branch A2 Runtime Configuration Impact Assessment, including effective-default equivalence and broader regression scope.

If PGO introduces a repeatable adverse tail, Branch A2 must treat PGO state as part of the proposed runtime configuration and qualify that process-wide impact before any Full-Domain Performance Confirmation 2.

## Caller environment

The future runner must fail closed if the caller already defines any of:

- `DOTNET_TieredCompilation`
- `DOTNET_TieredPGO`
- `DOTNET_TC_QuickJit`
- `DOTNET_TC_QuickJitForLoops`
- `DOTNET_ReadyToRun`

The runner must not silently clear inherited values.

## Authority boundary

This package authorizes planning only.

Current authority remains:

- Dynamic PGO Comparator 1 implementation: **not yet authorized**;
- RP1C selection: **not authorized**;
- production/runtime configuration change: **not authorized**;
- production repair: **not authorized**;
- C4 mutation: **not authorized**;
- threshold change: **not authorized**;
- exact-v9 change: **not authorized**;
- VR3: **not authorized**;
- P3-R1: **not authorized**;
- second replacement-long: **not authorized**.

After a local planning PASS, return the complete four-file planning artifact directory before implementing Dynamic PGO Comparator 1.
