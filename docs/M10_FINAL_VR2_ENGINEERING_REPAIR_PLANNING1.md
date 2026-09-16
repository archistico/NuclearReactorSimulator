# M10 Final — VR2 Engineering Repair Planning 1 — Thermodynamic Inverse Closure & Phase-Boundary Repair

**Status:** VALIDATED / CLOSED — returned static audit `PASS-AS-AUTHORED`; RP1A REV1, first-generation RP1B, Refinements 1–5 are VALIDATED returned evidence; RP1B Performance Measurement Replanning 1 is VALIDATED; returned Refinement 5 justifies C4 planning only, while C4 implementation and RP1C remain unauthorized  
**Prerequisite:** VR2 Materiality Diagnostic 1 returned-evidence adjudication — completed with `HYDRAULIC-MATERIALITY-CONFIRMED`  
**Authority boundary:** planning and design-selection only. No production source change, thermodynamic repair, tolerance change, exact-v9 reinterpretation, VR3, P3-R1 or second replacement-long execution is authorized.

## 1. Purpose

VR2 first established a `MODEL-DISCREPANCY-BLOCKING` error in the existing simplified water/steam inverse closure. Materiality Diagnostic 1 then demonstrated that the discrepancy is not merely an absolute-pressure offset: after conservative adjudication against the authoritative H.22 fixed-point residual ceiling, the independent IF97 pressure-only counterfactual remains hydraulically material on the actual exact-v9/P1B path.

The returned evidence also shows that the repair problem is broader than the original compressed-liquid pressure formula. On the actual path, production classifies all 72 sampled `pressure` states as `SubcooledLiquid` while independent IF97 inversion of the same conserved `(v,u)` inventories resolves Region 4 `SaturatedMixture`; the same production-subcooled/reference-mixture reinterpretation occurs for 55 of 72 `suction` samples.

The returned planning audit validated this milestone as authored. RP1A REV1 subsequently returned a complete 7/7 artifact set and is now VALIDATED. First-generation RP1B and Refinements 1–4 are frozen validated evidence. Refinement 2 closed the physical/seam qualification for C3/D3. Refinement 3 then showed C3 exact-v9 and targeted-state timing below the frozen ceiling but retained one isolated R1 seam wall-clock spike. Refinement 4 localized two isolated R1-side exceedances to different boundaries — boundary 3 with Gen0 activity during the screen and boundary 191 without GC activity during targeted repeats — while both boundaries retained ordinary median/p95 timing. Engineering review at the Refinement 4 stage therefore did not justify C4 and did not erase the strict max evidence. RP1B Performance Measurement Replanning 1 returned `PASS-AS-AUTHORED`; RP1B Refinement 5 has now returned validated `C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED` evidence without changing C3. Returned review localizes the repeatable tail to deterministic per-call allocation / Gen0 correlation rather than a proven boundary-3 thermodynamic branch. This planning milestone defines how to investigate and select a repair for the complete inverse closure:

```text
(v, u) -> coarse phase -> temperature -> pressure -> optional quality
```

It does **not** implement that repair.

## 2. Frozen evidence basis

The planning decision is bound to the following already returned evidence:

- VR2 IF97 helper qualified against official Region 1/2/4 check values;
- VR2 classification `MODEL-DISCREPANCY-BLOCKING`;
- Materiality Diagnostic 1 Attempt 5 complete evidence set: 3/3 P1B checkpoints, 360/360 resolved node rows, 288/288 resolved hydraulic path rows, zero trip/nonconvergence/nonfinite/rollback sentinels and deterministic repeat;
- returned late-window materiality: 8 `CONFIRMED` windows and 8 `NOT-EXCLUDED` windows;
- adjudicated engineering classification `HYDRAULIC-MATERIALITY-CONFIRMED`;
- maximum committed-flow versus instantaneous-map difference `0.009952798974779853 kg/s`, inside the authoritative exact-v9 H.22 fixed-point residual ceiling of `0.01 kg/s`;
- phase-boundary reinterpretation counts: `pressure=72/72`, `suction=55/72` production-subcooled versus IF97 Region-4 mixture.

The frozen returned adjudication summary is stored at:

```text
eng/frozen-evidence/ordinary/M10FinalPhysicalReferenceVR2MaterialityDiagnostic1_ReturnedEvidenceAdjudicationSummary.txt
```

## 3. Current production mechanism that must not be silently retuned

The authoritative exact-v9 path uses:

```text
WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain
```

The current subcooled-liquid inverse branch derives temperature from a constant liquid heat capacity and pressure from a constant bulk modulus relative to the model's saturated-liquid density:

```text
T = Ttriple + u / 4200
p = psat(T) + 2.2 GPa * max(0, rho/rho_f(T) - 1)
```

The production resolver traverses saturated-mixture, subcooled-liquid and superheated branches in deterministic order. `CorrelationConsistentInverseDomain` already repairs earlier internal inverse-topology defects around saturation and superheated-vapor ownership, but it does not replace the constant-`cp` / constant-bulk-modulus compressed-liquid closure with an IF97-calibrated Region-1 inverse.

The new VR2 evidence must therefore not be reduced to a single coefficient-tuning exercise.

## 4. Historical architecture that remains binding

ADR 0165–0167 established and validated three rules that remain applicable:

1. stage thermodynamic repair before production activation;
2. requalify repaired thermodynamics before exact-version activation;
3. never reinterpret historical exact identities through changed physics.

Planning 1 extends those rules to exact-v9. In particular:

- `CorrelationConsistentInverseDomain` remains immutable historical/current exact-v4 through exact-v9 semantics;
- exact-v9 must not be silently reinterpreted;
- any authorized repair must be introduced behind a **new opt-in closure mode**;
- any eventual production activation must use a **new exact-version identity**, assigned only after repair qualification;
- the existing test-only IF97 helper remains an external/reference oracle and is not automatically promoted into production code.

ADR 0194 records this architecture boundary.

## 5. Repair families considered

Planning 1 considers four bounded families. This milestone does not yet authorize production implementation of any of them.

### Family A — local compressed-liquid coefficient retune

Examples include retuning only `LiquidSpecificHeatJoulesPerKilogramKelvin`, `LiquidBulkModulusPascals`, or replacing the fixed bulk modulus with a one-dimensional temperature function while retaining the existing phase ownership.

**Planning decision:** do not advance this family as a standalone repair candidate.

Reason: it may reduce Region-1 pressure error, but it does not address the returned exact-v9 evidence where the same conserved `(v,u)` states are owned as `SubcooledLiquid` by production and as Region-4 mixture by IF97. A scalar or one-dimensional retune therefore cannot by itself justify phase-boundary ownership.

It may still appear as an internal component of a broader candidate, but it cannot be the complete repair.

### Family B — piecewise reference-consistent reduced inverse closure

Introduce a new reduced educational closure mode whose phase-domain ownership and branch equations are explicitly designed around the IF97 Region 1 / Region 4 / Region 2 topology needed by the M10 domain, without implementing the entire IF97 standard.

Candidate characteristics:

- deterministic unique coarse-phase ownership from conserved `(v,u)`;
- Region-4-consistent boundary handling;
- bounded Region-1-like liquid mapping for `T` and `p`;
- existing reduced educational scope retained outside the qualified range;
- no inventory clamping or mutation;
- fail-closed outside the declared supported domain.

**Planning status:** ADVANCE TO TEST-ONLY SHADOW STUDY.

### Family C — bounded IF97-derived table/interpolation surrogate

Generate a provenance-controlled bounded table over the M10 thermodynamic domain from the independent IF97 reference and use deterministic interpolation for inverse phase/temperature/pressure resolution.

Candidate characteristics:

- explicit frozen domain and grid;
- interpolation error separately measured from reduced-model error;
- deterministic and potentially inexpensive at 10 ms runtime cadence;
- table provenance and generation method recorded so constants are not opaque tuning data;
- boundary interpolation must not create gaps, overlaps or phase chatter.

**Planning status:** ADVANCE TO TEST-ONLY SHADOW STUDY.

### Family D — bounded production IF97 subset

Port the minimum required IF97 Region 1/2/4 equations plus bounded inverse logic into production and use them directly for the supported M10 domain.

Candidate characteristics:

- strongest direct reference fidelity;
- larger code and verification surface;
- inverse search and runtime cost must be measured explicitly;
- educational model scope and fail-closed domain must remain explicit;
- the test-only reference implementation cannot simply be copied into production and declared qualified without independent production-path verification.

**Planning status:** ADVANCE AS A REFERENCE-FIDELITY / COST COMPARATOR, not as the default preferred implementation.

## 6. Candidate selection criteria

No candidate is selected from accuracy alone. RP1B must compare the advanced families against the same frozen corpus and report at least the following dimensions.

### 6.1 Reference correctness

Mandatory conditions before a candidate can be considered for production implementation:

- finite deterministic resolution for every in-envelope frozen point;
- no M10-core wrong coarse phase;
- no M10-core numerical comparison above the existing VR2 blocking ceiling of 25%;
- no new unresolved gap in the supported inverse domain;
- deterministic repeat.

Planning target, stricter than the existing blocking ceiling but **not a replacement VR2 tolerance**:

- compressed-liquid / hot-primary pressure should reach at least the existing `BOUNDED-EDUCATIONAL` band (`<=10%`) on the frozen M10-core points;
- phase agreement should be 100% on the frozen reference-classifiable exact-v9 node corpus.

If no candidate reaches the planning target without creating regressions, return to planning. Do not widen VR2 claim bands.

### 6.2 Phase-boundary ownership and continuity

The candidate must demonstrate:

- deterministic Region-1 / Region-4 / Region-2 coarse ownership over the bounded corpus;
- no unowned seam interval;
- no ambiguous multiple-owner interval unless an explicit deterministic selection rule is separately justified;
- continuity evidence for pressure and temperature approaching the owned boundary from both sides;
- bounded phase switching under small `(v,u)` perturbations;
- compatibility with the existing previous-phase continuity/hysteresis machinery, with evidence determining whether that machinery is still needed rather than assuming retirement.

### 6.3 Conserved-state semantics

Forbidden shortcuts:

- clamping mass, volume or internal energy to force a branch;
- synthesizing hidden latent energy;
- changing node inventory to obtain a preferred phase;
- masking unsupported states with a fallback pressure;
- node-id special cases such as `pressure` or `suction` repair branches.

The repair must remain a pure mapping from the committed conserved inventory plus explicit model definition to a thermodynamic state.

### 6.4 Hydraulic materiality closure

Before runtime activation, candidate pressure results must be applied **offline** to the frozen Attempt-5 path corpus using the same hydraulic-law counterfactual method.

The study must report whether the candidate removes or materially reduces:

- `CHANNEL` driving-pressure sign inversion;
- `FEEDWATER-PUMP` confirmed shifts;
- `MCP` and `RETURN` not-excluded shifts.

This shadow comparison is a ranking tool. It does not authorize a runtime commit.

### 6.5 Performance and determinism

The 10 ms fixed step is immutable.

RP1A must first measure the current exact-v9 thermodynamic-resolve cost and whole-step margin on the validation machine. A numerical candidate-performance ceiling must then be frozen **before** RP1B candidate timing results are inspected. Planning 1 intentionally does not invent a percentage without a measured baseline.

Every candidate must remain deterministic and allocation/cost evidence must distinguish:

- one-time initialization/table construction;
- steady-state `Resolve(...)` cost;
- boundary/inverse-search worst cases;
- whole exact-v9 step impact.

## 7. Frozen repair-planning sequence

### RP1A — Reference Domain Corpus & Seam Map Freeze

Observation/test-only. No production repair.

Build one immutable corpus from:

1. the complete frozen VR2 point matrix;
2. all 360 Attempt-5 exact-v9 node samples;
3. the 288 hydraulic path rows as downstream materiality context;
4. deterministic seam probes immediately on both sides of every Region-1/4 and Region-4/2 boundary encountered by the corpus;
5. current exact-v9 `Resolve(...)` cost and whole-step performance baseline.

RP1A outputs the supported domain envelope, phase-ownership map, seam-probe matrix and pre-result candidate performance ceiling.

### RP1B — Test-Only Shadow Candidate Matrix

Implement Families B, C and D only in test/reference/shadow code. Do not edit production selection or exact-version registration.

Run every family against the RP1A corpus and produce:

- numerical error map;
- phase-ownership map;
- seam continuity/perturbation evidence;
- unresolved/nonfinite census;
- deterministic repeat;
- offline hydraulic materiality replay over the frozen Attempt-5 inventories;
- steady-state and worst-case cost evidence against the RP1A performance ceiling;
- implementation complexity / maintenance assessment.

No candidate may be tuned after viewing its final RP1B result without creating a new versioned candidate and preserving the failed result.

### RP1C — Engineering Repair Selection Gate

RP1C may select **zero or one** repair design.

Possible results:

```text
NO-REPAIR-CANDIDATE-QUALIFIED
SELECT-PIECEWISE-REDUCED-CLOSURE
SELECT-TABULATED-REFERENCE-SURROGATE
SELECT-BOUNDED-IF97-SUBSET
```

RP1C still does not activate runtime physics. A selection authorizes only a separately versioned production repair candidate.

## 7.1 Performance-tail replanning after Refinement 4

Returned Refinement 4 evidence does not identify a stable C3 slow-path owner. The screen observed one exceedance at R1 boundary 3 with Gen0 activity; the targeted stage observed one exceedance at boundary 191 without GC activity, and boundary 3 did not reproduce as the targeted exceedance owner. C3 remains physically complete and byte-identical.

Therefore:

- C4 is not yet justified;
- RP1C remains unauthorized;
- the `409.30666666666673 us` strict single-call maximum is not relaxed or erased;
- the next authorized design step is RP1B Performance Measurement Replanning 1, which freezes a five-independent-process Refinement 5 protocol;
- C4 may be planned only if the same boundary exceeds the unchanged ceiling in at least two independent process runs;
- otherwise the project must perform a separate performance-contract adjudication before RP1C rather than silently changing the candidate or threshold.

ADR 0195 records this measurement/selection boundary.

## 8. Post-selection implementation route — not yet authorized

Only if RP1C explicitly selects one candidate may a later milestone perform production work.

The intended route is:

```text
R1  new opt-in closure mode implementation; no exact-v9 reinterpretation
R2  focused thermodynamic/reference/topology qualification
R3  short exact-v9-equivalent shadow/composition requalification
R4  P1B-equivalent long materiality recheck with repaired candidate
R5  VR2 re-entry against the repaired exact-version candidate
R6  versioned activation decision only if all previous gates are green
```

At R1 the existing `CorrelationConsistentInverseDomain` behavior remains unchanged. A new exact-version identity is introduced only after the repair candidate has passed the required qualification chain.

VR3 remains blocked until the repaired VR2 route returns a nonblocking result or a later explicit decision changes the Plan Amendment 3 sequence.

## 9. Explicit non-authorizations

Planning 1 does **not** authorize:

```text
production-src-change
thermodynamic-repair
thermodynamic-tolerance-change
exact-v9-change
existing-closure-mode-reinterpretation
new-exact-version-activation
VR3
P3-R1
second-replacement-long
```

The first executable successor is only:

```text
VR2 Engineering Repair Planning 1 / RP1A Reference Domain Corpus & Seam Map Freeze
```

The returned Planning 1 audit is reviewed `PASS-AS-AUTHORED`, and returned RP1A REV1 evidence is VALIDATED. First-generation RP1B, Refinement 1 C2/D2 and Refinement 2 C3/D3 are validated evidence. Refinement 2 produced physically complete C3/D3 results, but C3 requires performance-tail attribution and D3's recorded eligibility omitted seam worst-case timing. RP1B Refinement 3 subsequently returned complete timing evidence: exact-v9 and targeted C3 calls are below ceiling, while one R1-side seam call reached 3667.1 us. RP1B Refinement 4 is validated returned localization evidence. Performance Measurement Replanning 1 is VALIDATED / CLOSED and its five-process rule has now fired on returned Refinement 5 evidence. Refinement 5 is VALIDATED returned evidence with `C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED`; C4 planning is justified, while C4 implementation, RP1C and all production changes remain unauthorized.

## 10. Returned planning audit and next planning gate

The historical Planning 1 audit command was ` .\scripts\run-m10-final-vr2-engineering-repair-planning1-audit.cmd ` and its returned artifact is frozen `PASS-AS-AUTHORED`. RP1A REV1, first-generation RP1B, Refinements 1–4, Performance Measurement Replanning 1 and Refinement 5 are now validated predecessors.

Refinement 5 returned the frozen machine classification `C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED`. Engineering review accepts that result while recording that all five repeated boundary-3 exceedances coincide with the only Gen0 collection in each process and with a uniform 416-byte allocation on every measured C3 call. The next authorized action is therefore C4 **planning only**, focused on the demonstrated allocation/managed-runtime tail mechanism. C4 implementation, RP1C and production thermodynamic changes remain unauthorized.
