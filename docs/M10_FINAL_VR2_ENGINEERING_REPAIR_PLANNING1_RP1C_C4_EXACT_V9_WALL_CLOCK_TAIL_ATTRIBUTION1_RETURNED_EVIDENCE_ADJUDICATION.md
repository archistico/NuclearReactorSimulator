# M10 Final VR2 RP1C C4 Exact-v9 Wall-Clock Tail Attribution 1 — Returned-Evidence Adjudication

## Status

**PASS — comparative evidence complete; controlled runtime sensitivity established; no automatic causal promotion or RP1C selection.**

The returned Attribution 1 REV1 package contains the complete 86-file evidence tree for four runtime modes, five fresh processes per mode, 23,040 measured exact-v9 calls per process, and 460,800 measured calls total. The frozen candidate, corpus, ceiling and counterbalanced schedule are unchanged.

## Reconstructed mode results

| Mode | Calls | Median (us) | Max (us) | >100 us | >409.3067 us | Processes with strict exceedance |
|---|---:|---:|---:|---:|---:|---:|
| AMBIENT-UNSET-CONTROL | 115,200 | 2.0 | 724.2 | 16 | 2 | 2/5 |
| TIERING-OFF | 115,200 | 110.2 | 15,497.2 | 91,283 | 64 | 5/5 |
| TIERING-ON-PGO-OFF | 115,200 | 4.6 | 273.9 | 18 | 0 | 0/5 |
| TIERING-ON-PGO-ON | 115,200 | 4.2 | 330.4 | 28 | 0 | 0/5 |

All modes recorded zero candidate allocation, zero harness allocation, zero unresolved calls and zero Gen0/Gen1/Gen2 collections in the measured regions.

## Path-level interpretation

The dominant target path is `C2-MIXTURE-PREFIX`.

- Ambient control: median about 2.0 us, max 724.2 us.
- Tiering off: median about 110.5 us, max 15,497.2 us.
- Tiering on / PGO off: median about 4.6 us, max 273.9 us.
- Tiering on / PGO on: median about 4.2 us, max 330.4 us.

The `C2-NEAR-BOUNDARY-LIQUID-PREFIX` path remains much smaller and does not explain the strict misses in the tiering-enabled modes.

## Causal adjudication

### Supported

1. **Runtime configuration materially affects exact-v9 wall-clock performance.** The counterbalanced experiment changes runtime configuration while holding candidate, corpus, schedule, call count, ceiling and harness constant. `TIERING-OFF` is dramatically and reproducibly slower in all five processes.
2. **Disabling the tiering/QuickJit configuration bundle is ruled out as a repair direction.** It worsens both central tendency and tail behavior by a very large margin.
3. **The rare strict tail is not explained by candidate allocation or GC.** All measured regions remain zero-allocation and no managed collections occur.
4. **A deterministic thermodynamic slow path is strongly disfavored.** The same candidate/corpus path has radically different latency under runtime-mode intervention.

### Not supported yet

1. **Dynamic PGO causality is not proven.** PGO OFF and PGO ON both have zero strict exceedances; neither consistently dominates the other on diagnostic tails.
2. **Tiered compilation alone is not isolated from QuickJit.** The `TIERING-OFF` and `TIERING-ON-PGO-OFF` bundles differ in both `DOTNET_TieredCompilation` and `DOTNET_TC_QuickJit`.
3. **Ambient-default equivalence is not proven.** `AMBIENT-UNSET-CONTROL` records the variables as unset, not the effective resolved runtime policy. It still shows two rare strict misses.
4. **Scheduling/off-CPU causality is not proven.** The evidence is consistent with a rare external wall-clock component, but Attribution 1 did not capture ETW/perf scheduling evidence.
5. **PGO-sensitive loop behavior is not fully tested.** The planning deliberately froze `DOTNET_TC_QuickJitForLoops=0`; this limits any negative conclusion about Dynamic PGO on the loop-containing target path.

## Authority decision

Attribution 1 returned evidence is **adjudicated PASS as comparative evidence**. It does not make C4 RP1C-selection-ready and does not authorize any production/runtime change.

The next authorized activity is **planning-only for a single-factor runtime isolation gate**. That planning should separate:

- tiering enabled with QuickJit OFF vs ON while PGO remains OFF;
- a QJFL=1 PGO OFF vs PGO ON comparator if Dynamic PGO sensitivity is still material;
- ambient-default vs explicit runtime configuration only after effective-default equivalence is explicitly defined.

No threshold change, C4 mutation, exact-v9 mutation, VR3, P3-R1 or second replacement-long is authorized.
