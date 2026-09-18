# M10 Final — VR2 Engineering Repair Planning 1 — RP1C C4 Dynamic PGO Comparator 1 — Returned-Evidence Adjudication

## Decision

Returned `RP1C-C4-EXACT-V9-DYNAMIC-PGO-COMPARATOR1` evidence is accepted as complete and internally consistent. The gate contains exactly 46 files, 10 fresh processes and 230,400 measured exact-v9 calls. All 10 process matrices contain 64 measured passes x 360 rows, zero unresolved calls, zero candidate/harness allocation and zero GC collections in the measured region.

**Adjudication: PASS — Branch A runtime sensitivity remains confirmed.**

The single-factor intervention proves a material Dynamic PGO effect on the central exact-v9 timing distribution under the frozen QJFL-on configuration. It does **not** prove Dynamic PGO ownership of the rare strict wall-clock tail.

## Frozen comparison

Both modes hold `DOTNET_TieredCompilation=1`, `DOTNET_TC_QuickJit=1`, `DOTNET_TC_QuickJitForLoops=1` and `DOTNET_ReadyToRun=1`. Only `DOTNET_TieredPGO` changes.

| Metric | PGO OFF | PGO ON | Adjudicated interpretation |
| --- | ---: | ---: | --- |
| processes | 5 | 5 | balanced |
| calls | 115,200 | 115,200 | balanced |
| median of process medians | 4.7 us | 2.1 us | repeated material PGO effect |
| median of process p95 | 5.4 us | 2.5 us | repeated material PGO effect |
| calls > 100 us | 13 | 8 | sparse tail; not sufficient alone for causal tail ownership |
| strict exceedances > 409.30666666666673 us | 1 | 0 | isolated OFF maximum; not sufficient alone for tail causality |
| maximum | 410.6 us | 251.7 us | descriptive only |

Across the five counterbalanced run blocks, PGO ON reduces the process median by 2.4–2.7 us in every block and the process p95 by 2.3–3.3 us in every block. Aggregating raw evidence, all 360/360 exact-v9 rows have a lower row median with PGO ON. The aggregate raw-call median changes 4.7 -> 2.1 us, p95 5.4 -> 2.5 us, p99 6.3 -> 4.0 us.

This repeated all-row/all-block direction is strong enough to promote **Dynamic PGO central-distribution causality** for this frozen experiment.

## Rare-tail limit

The tail evidence remains sparse. Per block, >100 us counts change `1->2`, `3->1`, `3->3`, `1->2`, `5->0`; the direction is therefore not monotonic. The only strict PGO-OFF exceedance is one 410.6 us event in one process. PGO ON has no strict exceedance, but that single-event contrast is not enough to establish that PGO removes the rare strict tail.

No stable strict row owner is reproduced in two independent processes. Diagnostic >100 us events remain distributed across rows/nodes on the same `C2-MIXTURE-PREFIX` resolution path. The gate therefore does not justify a new C4 mutation or a strict-threshold change.

## Branch decision

The compilation-factor investigation is now sufficiently bounded to advance to **Branch A2 Runtime Configuration Impact Assessment planning**. The assessment must not treat the five explicit runtime variables as a production prescription. It must compare an ambient/unset profile with the explicit reference profile and assess project-wide consequences before any production/runtime choice.

The reference explicit profile for A2 planning is:

- `DOTNET_TieredCompilation=1`;
- `DOTNET_TieredPGO=1`;
- `DOTNET_TC_QuickJit=1`;
- `DOTNET_TC_QuickJitForLoops=1`;
- `DOTNET_ReadyToRun=1`.

This profile is a **qualification reference**, not an authorized deployment configuration.

## Authority boundary

This adjudication authorizes only planning of `RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT-PLANNING1`.

It does not authorize Runtime Configuration Impact Assessment implementation, RP1C selection, a production runtime setting, production repair, C4 mutation, threshold change, exact-v9 change, Full-Domain Performance Confirmation 2, VR3, P3-R1 or a second replacement-long baseline.
