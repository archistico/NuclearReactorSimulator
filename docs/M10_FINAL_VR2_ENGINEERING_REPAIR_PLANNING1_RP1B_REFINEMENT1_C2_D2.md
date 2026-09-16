# M10 Final — VR2 Engineering Repair Planning 1 — RP1B Refinement 1 C2/D2

**Status:** HOTFIX 1 EXECUTION CANDIDATE — Attempt 1 was static-preflight RED only; no C2/D2 evidence exists from that attempt  
**Prerequisite:** RP1A REV1 validated; returned RP1B B1/C1/D1 matrix complete and reviewed  
**Authority:** test/reference/shadow refinement only. No production source change, thermodynamic repair, tolerance change, exact-v9 change, RP1C selection, VR3, P3-R1 or second replacement-long execution is authorized.

## 0. Attempt 1 preflight finding and Hotfix 1

The first local Refinement 1 attempt did not start the ordinary build or the focused C2/D2 matrix. Windows PowerShell `ConvertFrom-Json` supplied numeric values that were then compared by the validator with exact `-ne` floating literals. The first false RED occurred on the frozen p95 ceiling `158.80666666666667 us`. This was a validator contract defect, not a corpus, C2, D2, threshold or performance result.

Hotfix 1 retains the exact same JSON values and engineering limits, but validates floating values through finite double conversion plus bounded absolute error. It also restores the CSV schema/key/output/authority checks already proven in the validated first-generation RP1B gate. C2/D2 source and the focused test remain unchanged.

## 1. Why Refinement 1 exists

The returned first-generation RP1B matrix is valid evidence but yields no selectable candidate.

B1 demonstrates that a reduced closure can preserve exact-v9 phase ownership and stay inexpensive, but it misses the already-authored `<=10%` planning target and leaves substantial seam incompleteness. C1 demonstrates that a table surrogate can be extremely accurate and inexpensive on the states it resolves, but it leaves 12 exact-v9 rows unresolved; all 12 are the same logical owner, `feedwater-inventory`, around 47.52 °C in Region 1. D1 demonstrates that an IF97-consistent result is reachable over the exact-v9 path, but its worst-case scan/fallback paths exceed the frozen performance ceiling and it still leaves many near-boundary seam probes unresolved.

The correct response is not to retune B1/C1/D1 in place. Their identities and returned results are frozen. Refinement 1 therefore creates two new identities:

```text
C2-EXTENDED-TABULATED-SURROGATE
D2-SEAM-COMPLETE-IF97-COMPARATOR
```

RP1C remains blocked until these new identities have returned evidence on exactly the same RP1A corpus.

## 2. Frozen evidence and corpus

The complete first-generation RP1B artifact set is frozen under:

```text
eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Artifacts/
```

The reviewed first-generation summary is frozen at:

```text
eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_UserReturnedSummary.txt
```

Refinement 1 does not regenerate or move any RP1A coordinate. It reuses:

```text
40 VR2 rows
  39 inverse-applicable
   1 boundary-only pressure row
360 exact-v9 node rows
288 hydraulic rows
1,280 seam probes
320 seam boundaries
```

The four seam sides and their RP1A offsets remain unchanged. A C2/D2 difficulty on those coordinates is evidence; it is not permission to move the probe.

## 3. Frozen thresholds and performance ceilings

Refinement 1 preserves the existing comparison policy:

```text
VR2 blocking ceiling                 25%
Planning selection target            10%
Exact-v9 phase-agreement target      100%
Resolve median ceiling               94.8 us
Resolve p95 ceiling                  158.80666666666667 us
Resolve max ceiling                  409.30666666666673 us
Resolve median allocation ceiling    2816 B
```

The 10% value remains a planning/selection target and does not replace the existing VR2 25% blocking ceiling.

Candidate qualification remains evidence rather than the execution PASS criterion. The gate must still complete if C2 or D2 is inaccurate, incomplete or too slow, provided the matrix itself is complete, deterministic and finite.

## 4. C2 — extended tabulated surrogate

C2 is a new version of family C, not a rewrite of C1.

The first-generation C1 failure pattern is narrow enough to justify bounded refinement: all 12 exact-v9 unresolved rows belong to `feedwater-inventory`, while all 39 inverse-applicable VR2 rows resolve and the candidate remains inside the RP1A performance ceiling.

C2 therefore changes the test-only table design before execution as follows:

- Region-4 saturation nodes are generated at 0.5 K spacing rather than the C1 1 K spacing;
- Region-4 inversion explicitly brackets transitions between unreachable and reachable mixture geometry so a root immediately inside q≈0 or q≈1 is not skipped merely because the first sampled node lies past it;
- Region-1 rows use 1 K temperature spacing rather than C1 5 K spacing;
- Region-1 pressure support adds low-pressure nodes and deterministic pressure offsets immediately above saturation, specifically to cover cold feedwater and liquid-side seam states without direct IF97 at resolve time;
- Region-2 rows use 2 K temperature spacing rather than C1 10 K spacing;
- Region-2 pressure support adds deterministic fractions immediately below the Region-2 upper pressure boundary, including the official B23-limited boundary above 623.15 K;
- bounded near-boundary liquid/vapor fallbacks use only initialization-time reference-derived local slopes; they do not call IF97 during `TryResolve`.

C2 remains a surrogate. `UsesDirectIf97AtResolveTime` must remain `false`.

The denser initialization table is allowed to cost more to build. RP1B compares initialization cost separately from steady-state `TryResolve` cost; only the already-frozen steady-state ceilings are used for later selection evidence.

## 5. D2 — seam-complete IF97 comparator

D2 is a new version of family D, not a rewrite of D1.

D1 already proved the fidelity end of the trade space on resolved states, but its worst paths were dominated by broad reference scans/fallbacks and it remained seam-incomplete. D2 therefore changes the comparator topology rather than relaxing any threshold:

1. C2 supplies only a deterministic phase-aware initial seed.
2. If the seed is mixture, D2 refines temperature along the Region-4 saturation curve with direct IF97 evaluation.
3. If the seed is Region 1 or Region 2, D2 applies a bounded two-variable `(T,p)` Newton refinement under the same IF97 domain guards.
4. Qualified independent Region-4/Region-1 reference inverse helpers remain fail-closed fallback only; if Region 2 is still unresolved, D2 performs a bounded multi-seed Region-2 Newton fallback over the official Region-2 domain.

D2 still uses direct IF97 at resolve time and remains a fidelity/cost comparator. A favorable D2 performance result does not by itself make direct IF97 the preferred production design.

The intended engineering question is whether better phase-aware seeding can remove D1's long-tail behavior and near-boundary gaps. The result is measured, not assumed.

## 6. Comparison dimensions

C2 and D2 are evaluated with the same machinery already validated by RP1B:

### VR2 error map

All 40 frozen rows are preserved. The 39 `(v,u)` rows are scored for resolution, phase, pressure and temperature. `VR2-SAT-360C-PONLY` remains boundary-only/not inverse-scored.

### Exact-v9 node map

All 360 rows are evaluated. The returned C1 `feedwater-inventory` gap is not special-cased or removed: C2 either resolves those rows under its new fixed design or records the failure.

### Seam map

All 1,280 probes are evaluated without moving the boundary coordinates. Resolution, phase agreement and liquid/vapor cross-seam jumps are recorded.

### Hydraulic replay

Candidate node pressures are replayed over all 288 frozen path rows using the already-qualified quadratic law and check-valve rule. The frozen IF97 pressure-only replay law is self-checked before candidate evaluation with the existing `<=1e-9 kg/s` harness tolerance.

### Determinism

Each candidate is constructed independently twice and must reproduce resolution, branch identity, phase, pressure, temperature and quality bit-for-bit across VR2 + exact-v9 + seam inputs for the evidence gate to complete.

### Performance and complexity

The same two warm-up passes and eight measured exact-v9 passes are used. Initialization, median/p95/max resolve cost, seam maximum and median allocation are recorded against the RP1A ceilings. Complexity records initialization reference-point count, conservative iteration ceiling and whether direct IF97 is called during resolve.

## 7. RP1B Refinement 1 PASS semantics

The gate passes only when:

- returned first-generation RP1B evidence is present and internally consistent;
- the RP1A corpus remains unchanged;
- exactly C2 and D2 are evaluated;
- every candidate produces 40 VR2 rows, 360 node rows, 1,280 seam rows and 288 hydraulic rows;
- measured call counts match the frozen performance contract;
- deterministic repeat passes for both candidates;
- all timing evidence is finite;
- all nine expected artifacts are written.

The gate does **not** fail merely because C2 or D2 is not selection-eligible. That remains engineering evidence for the returned review.

## 8. Selection remains deferred

Refinement 1 does not choose a candidate. In particular:

- an eligible C2 does not authorize production repair;
- a fast D2 does not authorize copying direct IF97 into production;
- a non-eligible C2 and D2 do not authorize changing the frozen thresholds;
- no result authorizes changing exact-v9 in place;
- no result authorizes VR3 before the VR2 repair/re-entry route is explicitly closed.

Returned review may authorize RP1C only after the complete C2/D2 artifact set is examined.

## 9. Execution

From PowerShell:

```powershell
.\scripts\run-m10-final-vr2-engineering-repair-planning1-rp1b-refinement1.cmd
```

Return the complete folder:

```text
artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-refinement1
```

before RP1C implementation or any production thermodynamic change.
