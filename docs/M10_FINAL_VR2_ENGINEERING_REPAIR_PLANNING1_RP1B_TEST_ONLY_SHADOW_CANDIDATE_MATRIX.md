# M10 Final — VR2 Engineering Repair Planning 1 — RP1B Test-Only Shadow Candidate Matrix

**Status:** VALIDATED EVIDENCE MATRIX — SUPERSEDED FOR ACTIVE EXECUTION BY RP1B REFINEMENT 1 C2/D2  
**Prerequisite:** RP1A REV1 returned evidence reviewed and promoted to `VALIDATED`  
**Authority:** test/reference/shadow comparison only. No production source change, repair activation, tolerance change, exact-v9 reinterpretation, VR3, P3-R1 or second replacement-long execution is authorized.


## 0. Attempt 1 preflight failure and Hotfix 1

The first local RP1B attempt did not complete the static preflight. The validator failed before the ordinary build and before the focused B1/C1/D1 matrix because the production-source identity scan used `Get-Content -Raw` followed by `.Contains(...)` without guarding a null result. No candidate evidence was produced and no engineering interpretation is attached to that attempt.

Hotfix 1 changes only validator text-reading/provenance. All validator text reads now use fail-closed `System.IO.File.ReadAllText(..., UTF8)` semantics: a readable text file yields a non-null string, while a read failure throws an explicit path-qualified error. Candidate implementations, frozen RP1A corpus, focused RP1B test, runner, thresholds and production `src/` remain unchanged.

The failed attempt is frozen at `eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Attempt1_PreflightRedSummary.txt`.

## 0.1 Attempt 2 build failure and Hotfix 2

Hotfix 1 passed the static preflight, but the Release build stopped before focused execution on one xUnit analyzer finding: `Assert.Single(vr2Rows.Where(predicate))` violates xUnit2031 under warnings-as-errors. No B1/C1/D1 focused evidence was produced.

Hotfix 2 changes only that assertion to the semantically equivalent xUnit predicate overload `Assert.Single(vr2Rows, predicate)`. The candidate implementations, immutable RP1A corpus, runner, RP1B contract, thresholds, performance ceilings and production `src/` remain unchanged. A repository-local scan of the RP1B test/reference files found no second LINQ-filtered `Assert.Single` pattern.

The failed build attempt is frozen at `eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Attempt2_BuildRedSummary.txt`.


## 0.2 Returned first-generation matrix and Refinement 1 authorization

Hotfix 2 completed locally and returned all nine RP1B artifacts. The matrix is frozen as `PASS-EVIDENCE-MATRIX-COMPLETE`; no B1/C1/D1 identity is selection-eligible. The complete returned artifact set is preserved under:

```text
eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Artifacts/
```

The reviewed summary is frozen at `eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_UserReturnedSummary.txt`.

The first-generation engineering findings are intentionally non-ranking but decisive for refinement scope:

- B1 keeps 100% exact-v9 phase agreement and meets the frozen performance ceiling, but misses the `<=10%` planning target and remains seam-incomplete;
- C1 resolves all 39 inverse-applicable VR2 rows and meets the performance ceiling, but leaves 12 exact-v9 rows unresolved, all owned by `feedwater-inventory`, and is seam-incomplete;
- D1 meets the planning fidelity target with 100% exact-v9 phase agreement, but violates the frozen performance ceiling and remains seam-incomplete because long-tail reference fallbacks dominate its worst cases.

The returned review therefore authorizes only versioned **RP1B Refinement 1** with `C2-EXTENDED-TABULATED-SURROGATE` and `D2-SEAM-COMPLETE-IF97-COMPARATOR`. B1/C1/D1 remain immutable evidence. RP1C remains unauthorized until returned Refinement 1 review.

## 1. Purpose

RP1B compares the three repair families advanced by validated Planning 1 against exactly the same corpus frozen by RP1A. It is deliberately a comparison gate, not a selection gate.

The three first-version candidates are immutable identities once this RP1B run produces final evidence:

```text
B1-PIECEWISE-REDUCED
C1-TABULATED-SURROGATE
D1-BOUNDED-IF97-SUBSET
```

If a result later motivates a mathematical change, the original result remains frozen and a new candidate version such as B2/C2/D2 must be created. RP1B never silently retunes B1/C1/D1 after seeing their final matrix.

## 2. Returned RP1A authority

The complete returned RP1A artifact set is frozen under:

```text
eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts/
```

The reviewed returned summary is frozen at:

```text
eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1A_UserReturnedSummary.txt
```

RP1A is now `VALIDATED`. The frozen evidence shape is:

- 40 VR2 reference rows: 39 inverse-applicable `(v,u)` rows plus the preserved `VR2-SAT-360C-PONLY` boundary-only row;
- 360 exact-v9 node rows;
- 288 hydraulic-context rows;
- 1,280 seam probes across 320 boundaries;
- 348 Region-4 and 12 Region-1 exact-v9 reference rows;
- deterministic seam repeat;
- 4 current-production seam unresolved rows and 638 resolved phase mismatches retained as baseline evidence.

The machine-local candidate ceilings were fixed before any B/C/D implementation or timing inspection:

```text
median Resolve <= 94.8 us
p95 Resolve    <= 158.80666666666667 us
max Resolve    <= 409.30666666666673 us
median allocation <= 2816 B
```

These are candidate-cost ceilings inherited from RP1A. They are not thermodynamic tolerances.

## 3. Candidate B1 — piecewise reduced closure

B1 is a test-only reduced closure. Its design intentionally avoids turning the candidate into a direct full IF97 inverse implementation.

It uses:

- a bounded Region-4 saturation table generated from the independent IF97 helper;
- reference-consistent mixture ownership from `(v,u)` over that table;
- a reduced compressed-liquid branch using saturation liquid properties and a temperature-dependent effective secant bulk modulus derived at initialization;
- a reduced superheated branch using an ideal-gas pressure relation plus a bounded vapor internal-energy continuation from saturation.

The purpose is to determine whether a relatively small educational closure can repair phase ownership and materially reduce pressure error while remaining cheap enough for the 10 ms simulation architecture.

B1 does not modify the existing production closure and is not a production prototype.

## 4. Candidate C1 — bounded IF97-derived table surrogate

C1 is a test-only structured interpolation surrogate generated deterministically from the independent IF97 helper.

It contains:

- a 1 K saturation table for Region-4 ownership and mixture inversion;
- Region-1 temperature rows at 5 K spacing with bounded pressure nodes up to 100 MPa;
- Region-2 temperature rows at 10 K spacing with bounded pressure nodes up to 20 MPa;
- deterministic interpolation at fixed specific volume followed by interpolation across temperature rows.

The table is generated before candidate evaluation from the already frozen RP1A domain design. No final RP1B result is used to move table nodes.

The study separately records initialization cost and steady-state `TryResolve(v,u)` cost so one-time table construction is not confused with fixed-step runtime cost. Region-2 table generation is additionally bounded by the official IF97 B23 Region-2/3 pressure boundary between 623.15 K and 863.15 K; the candidate never labels a B23-side Region-3 point as Region 2 merely because it lies below the generic 20 MPa study cap. The complexity artifact reports the 32-iteration Region-4 table-bisection ceiling rather than claiming that the surrogate is iteration-free.

## 5. Candidate D1 — bounded IF97 subset comparator

D1 is the fidelity/cost comparator. It uses C1 only as a deterministic initial seed, then refines states with direct IF97 Region 1/2/4 equations.

The direct path uses:

- Region-4 mixture refinement along the saturation curve;
- bounded two-variable Region-1 and Region-2 refinement in `(T,p)`;
- existing qualified independent inverse helpers as fail-closed fallback where available.

D1 is not the preferred production design by construction. Its role is to establish the fidelity/cost end of the trade space. Region-2 refinement uses the same B23 domain guard as C1, and the complexity artifact records a conservative 1200-iteration worst-path scan/refinement/fallback ceiling so the comparator cannot look artificially cheap. A favorable accuracy result cannot by itself authorize copying the test helper into production.

## 6. Immutable comparison corpus

Every candidate is evaluated against the same RP1A artifacts:

```text
02-vr2-reference-point-corpus.csv     40 rows
03-exact-v9-node-corpus.csv          360 rows
04-hydraulic-context.csv             288 rows
05-seam-probe-map.csv              1,280 rows
06-performance-baseline.csv
```

RP1B does not regenerate, filter, rebalance or discard points based on candidate behavior.

## 7. Evidence dimensions

### 7.1 VR2 inverse error map

For the 39 inverse-capable VR2 rows RP1B records the inverse result. The 40th frozen row, `VR2-SAT-360C-PONLY`, is preserved in the output as boundary-only/not-applicable and is not falsely scored as an inverse-closure failure. For inverse-applicable rows RP1B records:

- resolved/unresolved;
- candidate branch/phase;
- candidate temperature and pressure;
- phase agreement;
- pressure relative error;
- temperature relative error on an absolute-Kelvin basis.

The existing 25% VR2 blocking ceiling remains unchanged. The 10% figure remains only the stricter Planning 1 selection target and is not a replacement tolerance.

### 7.2 exact-v9 node corpus

For all 360 frozen exact-v9 rows RP1B records the same inverse comparison. Planning 1 requires 100% phase agreement as the target for a selectable repair design.

### 7.3 seam ownership and continuity

All 1,280 RP1A seam probes are evaluated without moving the boundary coordinates.

RP1B records:

- unresolved rows;
- phase mismatches;
- candidate-reference pressure and temperature differences;
- maximum R1-side versus R4-liquid-side pressure/temperature jumps;
- maximum R4-vapor-side versus R2-side pressure/temperature jumps.

These continuity values are comparison evidence. RP1B does not invent a new continuity tolerance after seeing candidate results.

### 7.4 hydraulic replay

Candidate pressures from the 360 node rows are applied offline to the exact same 288 Attempt-5 hydraulic rows.

The replay preserves:

- frozen resistance;
- frozen active pump boost;
- canonical flow only as observation;
- the exact-v9 feedwater discharge check-valve directionality;
- no reinjection into runtime state.

Before any candidate is evaluated, RP1B reconstructs all 288 frozen IF97 pressure-only counterfactual flows from the frozen reference pressures, resistance, pump boost and check-valve rule; the maximum reproduction error must remain `<=1e-9 kg/s`. For every candidate row RP1B then records candidate driving pressure and flow, absolute shift from canonical flow, and absolute distance from the previously frozen IF97 pressure-only counterfactual flow.

### 7.5 deterministic repeat

Each candidate is reconstructed independently and reevaluated over VR2 + exact-v9 + seam rows. Resolution state, branch identity, phase, pressure, temperature and quality must repeat bit-for-bit for the RP1B evidence gate to complete.

### 7.6 performance

Performance measurement uses:

```text
warm-up passes: 2
measured passes: 8
measured domain: 360 exact-v9 node states
additional seam worst-case pass: 1 x 1,280 probes
```

RP1B separately records candidate initialization time/allocation, steady-state median/p95/max resolve cost, seam maximum resolve cost and median steady-state allocation.

A candidate may exceed the frozen ceiling and RP1B may still complete successfully: that exceedance is a candidate result for RP1C, not corruption of the RP1B evidence harness.

## 8. Evidence-completion versus candidate qualification

This distinction is binding.

RP1B itself passes when:

- all three frozen candidate identities execute;
- each preserves all 40 VR2 rows (39 inverse-applicable + 1 boundary-only) and produces the required 360 / 1,280 / 288 inverse/path result rows;
- deterministic repeat passes;
- timing evidence is finite;
- all nine RP1B artifacts are written.

RP1B does **not** fail merely because a candidate has unresolved states, phase mismatches, >25% error, misses the 10% planning target, or exceeds the performance ceiling. Those are candidate findings.

The candidate summary separately records `rp1c_selection_eligible`. The stricter Planning 1 target is exactly the already-authored one: hot/compressed-liquid M10-core pressure error `<=10%` together with 100% phase agreement on the frozen reference-classifiable exact-v9 node corpus. It remains selection evidence for RP1C and is not a replacement VR2 tolerance. That field is evidence only. RP1B does not choose a winner.

## 9. Candidate qualification fields

For transparent RP1C input, each candidate summary records at least:

- `all_frozen_points_resolved`;
- `no_core_wrong_phase`;
- `vr2_blocking_ceiling_met`;
- `planning_target_met`;
- `deterministic_repeat`;
- `performance_ceiling_met`;
- `rp1c_selection_eligible`.

A candidate is marked selection-eligible only if the mandatory resolution, core phase, existing VR2 ceiling, Planning 1 target, deterministic and performance conditions are simultaneously met. RP1C remains responsible for the engineering choice and may still select no candidate.

## 10. Outputs

The focused gate writes:

```text
01-contract-and-provenance.txt
02-candidate-vr2-error-map.csv
03-candidate-exact-v9-node-map.csv
04-candidate-seam-map.csv
05-candidate-hydraulic-replay.csv
06-candidate-performance.csv
07-candidate-complexity.csv
08-candidate-summary.csv
09-rp1b-summary.txt
```

## 11. Explicit non-authorizations

RP1B does not authorize:

```text
RP1C selection before returned RP1B review
production source change
thermodynamic repair activation
thermodynamic tolerance change
exact-v9 change
existing closure reinterpretation
new exact-version activation
VR3
P3-R1
second replacement-long
```

## 12. Execution

From PowerShell:

```powershell
.\scripts\run-m10-final-vr2-engineering-repair-planning1-rp1b.cmd
```

Return the complete folder:

```text
.\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b
```

before implementing RP1C or changing production thermodynamics.
