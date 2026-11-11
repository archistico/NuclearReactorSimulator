# M10 Final VR2 RP1C C4 Runtime Factor Isolation 1 — Returned-Evidence Adjudication

## Status

**PASS — returned evidence complete; single-factor runtime sensitivity is adjudicated.**

The returned `RP1C-C4-EXACT-V9-RUNTIME-FACTOR-ISOLATION1` evidence tree contains exactly 86 required files: 20 fresh process directories with four files each plus six aggregate files. The immutable C4 candidate, frozen exact-v9 corpus, `409.30666666666673 us` strict maximum and diagnostic-only `100 us` floor remain unchanged.

This adjudication accepts the experiment as complete evidence. It does not select C4 and does not authorize any production/runtime change.

## Reconstructed integrity

Independent reconstruction from the returned raw CSV evidence confirms:

- 4 runtime modes;
- 5 fresh processes per mode;
- 20 unique process IDs;
- 64 measured passes per process;
- 360 exact-v9 rows per measured pass;
- 23,040 measured calls per process;
- 460,800 measured calls total;
- the same ordered 360-row probe/logical-step/node/resolution-path identity in every measured pass and every process;
- 0 unresolved calls;
- 0 per-call allocation;
- 0 whole-region candidate/harness allocation;
- 0 Gen0/Gen1/Gen2 collections during measured regions;
- `.NET 10.0.7`, Windows `10.0.22631`, X64 in all 20 runtime-context records;
- all five planned `DOTNET_*` values match the frozen mode matrix in every process.

The evidence-integrity classification is therefore accepted as `PASS-RUNTIME-FACTOR-ISOLATION-EVIDENCE-INTEGRITY`.

## Reconstructed mode results

| Mode | Calls | Aggregate median (us) | Aggregate p95 (us) | Max (us) | >100 us | >409.3067 us | Processes with strict exceedance |
|---|---:|---:|---:|---:|---:|---:|---:|
| `TIERING-OFF-QJ-OFF-QJFL-OFF` | 115,200 | 109.0 | 117.6 | 729.5 | 92,203 | 20 | 5/5 |
| `TIERING-ON-QJ-OFF-QJFL-OFF` | 115,200 | 4.6 | 5.2 | 457.8 | 36 | 1 | 1/5 |
| `TIERING-ON-QJ-ON-QJFL-OFF` | 115,200 | 4.4 | 5.0 | 181.3 | 21 | 0 | 0/5 |
| `TIERING-ON-QJ-ON-QJFL-ON` | 115,200 | 4.6 | 5.1 | 232.0 | 21 | 0 | 0/5 |

The diagnostic `100 us` count is not an acceptance metric. It is shown only to characterize tail shape.

## Single-factor adjudication

### A ↔ B — `DOTNET_TieredCompilation`

This contrast is decisive for the gross slowdown.

Across all five counterbalanced run blocks, enabling TieredCompilation while keeping PGO, QuickJit and QuickJitForLoops OFF reduces:

- process median by approximately `103.9–108.1 us` in every block;
- process p95 by approximately `109.4–113.0 us` in every block;
- aggregate calls above `100 us` from `92,203` to `36`;
- strict exceedances from `20` to `1`;
- aggregate median from `109.0 us` to `4.6 us`.

Because A and B differ only in `DOTNET_TieredCompilation`, this establishes a **material single-factor causal effect for the gross exact-v9 wall-clock slowdown under the frozen experiment**.

It does **not** establish TieredCompilation as the complete owner of the rare strict tail: one B call still reaches `457.8 us`.

### B ↔ C — `DOTNET_TC_QuickJit`

QuickJit produces only a small change in central tendency:

- median-of-process-medians `4.6 -> 4.5 us`;
- median-of-process-p95 `5.1 -> 5.0 us`;
- calls above `100 us` `36 -> 21`;
- strict exceedances `1 -> 0`.

The per-run median change is small (`0.1–0.3 us`) and the p95 direction is mixed. The only strict-exceedance difference is one isolated B event at run 4 / pass 43 / row 172 (`exact-v9-5-to-6mwe`, logical step `140784`, `outlet`, `C2-MIXTURE-PREFIX`, `457.8 us`).

Therefore QuickJit is **not adjudicated as a material causal owner of the rare strict tail**. The evidence is compatible with a modest central-performance effect, but the tail improvement is not repeated strongly enough to promote a rare-tail causal claim.

### C ↔ D — `DOTNET_TC_QuickJitForLoops`

QuickJitForLoops shows no material improvement:

- calls above `100 us`: `21 -> 21`;
- strict exceedances: `0 -> 0`;
- median-of-process-medians: `4.5 -> 4.7 us`;
- median-of-process-p95: `5.0 -> 5.1 us`;
- max: `181.3 -> 232.0 us`.

The per-run differences are small and mixed. `DOTNET_TC_QuickJitForLoops` is therefore **not a material tail-repair factor in this gate**.

## Tail-shape interpretation

The tiering-off mode is qualitatively different from all tiering-on modes: 323 exact row identities exceed `100 us`, many on every pass in every process. That is a broad runtime-mode slowdown, not a single deterministic C4 row.

With tiering enabled, the tail becomes sparse:

- B: 36 calls above `100 us` across 33 row identities;
- C: 21 calls above `100 us` across 21 row identities;
- D: 21 calls above `100 us` across 20 row identities.

The sparse enabled-tiering events are distributed across rows, passes and nodes rather than repeatedly owning one thermodynamic coordinate. This continues to disfavor a deterministic C4 physics slow path as the explanation for the historical rare wall-clock miss.

## Dynamic PGO materiality decision

Dynamic PGO remains a **material unresolved runtime hypothesis**, but for a narrower reason than before.

Historical Attribution 1 compared PGO OFF/ON with `DOTNET_TC_QuickJitForLoops=0`; both modes had zero strict exceedances. Runtime Factor Isolation 1 has now shown that enabling QuickJitForLoops itself is not materially beneficial, but it has still measured only PGO OFF when QJFL is ON.

A separate QJFL=1 PGO comparator is useful before Runtime Configuration Impact Assessment because it determines whether any future explicit runtime qualification would need to constrain Dynamic PGO at all. Avoiding an unnecessary process-wide PGO setting is materially relevant to the production/runtime decision.

The next gate is therefore **planning-only** for:

`RP1C-C4-EXACT-V9-DYNAMIC-PGO-COMPARATOR1`

with TieredCompilation ON, QuickJit ON, QuickJitForLoops ON and ReadyToRun ON in both modes; only Dynamic PGO may change OFF ↔ ON.

## Roadmap decision

The project remains on **Branch A — reproducible runtime-mode sensitivity**.

Completed within Branch A:

- Attribution 1: runtime sensitivity established.
- Runtime Factor Isolation Planning 1: returned/adjudicated.
- Runtime Factor Isolation 1: returned and adjudicated here.
- TieredCompilation: material single-factor owner of the gross slowdown.
- QuickJit: no promoted rare-tail causal claim.
- QuickJitForLoops: no material tail benefit.

Next:

1. Dynamic PGO Comparator Planning 1 only.
2. After returned planning adjudication, execute the separately versioned PGO comparator.
3. Only after that returned evidence is adjudicated may Branch A2 Runtime Configuration Impact Assessment be planned.
4. Ambient/effective-default equivalence remains unresolved and must be addressed before treating any explicit runtime setting as a production repair.
5. Full-Domain Performance Confirmation 1 remains historical negative evidence and is not reinterpreted.

## Authority boundary

This adjudication authorizes only the next **planning** gate. It does not authorize the PGO experiment itself.

Current authority remains:

- `RP1C-SELECTION-AUTHORIZED=False`
- `PRODUCTION-RUNTIME-CHANGE-AUTHORIZED=False`
- `PRODUCTION-REPAIR-AUTHORIZED=False`
- `THRESHOLD-CHANGE-AUTHORIZED=False`
- `EXACT-V9-CHANGE-AUTHORIZED=False`
- `VR3-AUTHORIZED=False`
- `P3-R1-AUTHORIZED=False`
- `SECOND-REPLACEMENT-LONG-AUTHORIZED=False`
