# M10 Final — VR2 Engineering Repair Planning 1 — RP1B Refinement 3 C3 Performance-Tail Attribution

**Status:** VALIDATED RETURNED EVIDENCE — exact-v9/targeted performance qualified; one isolated R1-side seam worst-case remains for Refinement 4 localization  
**Prerequisite:** RP1A validated; first-generation RP1B validated evidence; RP1B Refinement 1 C2/D2 validated evidence; RP1B Refinement 2 C3/D3 returned `PASS-EVIDENCE-MATRIX-COMPLETE` and reviewed.  
**Authority:** performance attribution only on immutable C3. No C4 implementation, production source change, thermodynamic repair, tolerance change, exact-v9 change, RP1C selection, VR3, P3-R1 or second replacement-long execution is authorized.

## 1. Why Refinement 3 exists

Refinement 2 closed the remaining physical/seam problem. C3 and D3 both returned:

```text
39/39 inverse-applicable VR2 rows resolved
360/360 exact-v9 rows resolved
100% exact-v9 phase agreement
1280/1280 seam probes resolved
0 seam phase mismatch
planning target <=10% met
deterministic repeat true
```

The remaining issue is performance interpretation.

C3 is the production-oriented surrogate family and returned:

```text
median Resolve   7.2 us
p95 Resolve      124.6 us
max Resolve      823.4 us
seam max         175.1 us
```

The frozen RP1A max ceiling is `409.30666666666673 us`, therefore C3 was not performance-qualified even though its seam maximum was already inside the ceiling.

D3 returned `resolve max=208.5 us` but `seam max=16504.9 us`. The Refinement-2 boolean `within_frozen_ceilings` did not include `seam_max`, so the recorded D3 `rp1c-selection-eligible=True` is retained as frozen evidence but is not treated as sufficient engineering authorization. Planning 1 requires worst-case boundary/inverse-search cost to be considered explicitly.

Refinement 3 therefore does not modify either candidate. It attributes C3's exact-v9 performance tail and applies a corrected full performance predicate that includes seam maximum.

## 2. Immutable candidate and corpus

C3 remains exactly:

```text
C3-VAPOR-SEAM-COMPLETE-SURROGATE
```

No C4 identity is introduced. `Rp1bRefinement2ShadowThermodynamicCandidates.cs` remains unchanged from the returned Refinement-2 candidate.

The timing corpus remains the RP1A freeze:

```text
360 exact-v9 node states
1280 seam probes
320 seam boundaries
4 seam sides x 320 probes
```

No state coordinate, seam offset, VR2 ceiling or planning target is changed.

## 3. Full-corpus timing protocol

To reduce ordering bias without changing the corpus, C3 is warmed for 16 full passes and then measured for 64 full passes. Each pass rotates the first state by a fixed stride of 37 rows; the same 360 states are still measured exactly once per pass.

The exact-v9 stage therefore produces:

```text
360 x 64 = 23040 measured Resolve calls
```

For each state the artifact records median, p95, max, the pass containing the max, number/fraction of calls above the frozen max ceiling, and a descriptive tail classification.

A full GC is requested before the measured full-corpus stage and generation collection counts are recorded as attribution context. GC evidence does not change the pass/fail ceiling.

## 4. Targeted tail-repeat protocol

Refinement 3 selects a bounded set consisting of:

- every state that exceeded the frozen max ceiling during screening; plus
- the highest-p95 states, ensuring at least the top 12 are represented.

For each selected state:

```text
32 warm-up calls
8 measured blocks
64 calls per block
512 measured calls per target state
```

This stage distinguishes:

```text
TARGETED-WITHIN-CEILING
TARGETED-ISOLATED-EXCEEDANCE
TARGETED-REPEATED-EXCEEDANCE
TARGETED-PERSISTENT-SLOW-PATH
```

These labels are descriptive attribution, but the targeted calls are still strict measured evidence: any targeted single-call value above the frozen maximum keeps performance qualification false. Targeted statistics never replace or relax the frozen single-call maximum requirement.

## 5. Seam-side timing protocol

The same immutable 1280 seam probes are warmed for 4 passes and measured for 16 passes. Evidence is aggregated independently for:

```text
R1-SIDE
R4-LIQUID-SIDE
R4-VAPOR-SIDE
R2-SIDE
ALL-SEAM-SIDES
```

Every measured call must remain resolved and phase-consistent with the frozen reference.

## 6. Corrected full performance predicate

Refinement 3 preserves every RP1A ceiling:

```text
median Resolve <= 94.8 us
p95 Resolve    <= 158.80666666666667 us
max exact-v9   <= 409.30666666666673 us
max seam       <= 409.30666666666673 us
median alloc   <= 2816 B
```

The key correction is explicit:

```text
full-performance-qualified =
    median <= median ceiling
    AND p95 <= p95 ceiling
    AND exact-v9 single-call max <= max ceiling
    AND seam single-call max <= same max ceiling
    AND targeted-repeat single-call max <= same max ceiling
    AND median allocation <= allocation ceiling
```

The prior D3 eligibility flag is not rewritten; it remains provenance of the Refinement-2 harness. Refinement 3 simply prevents future engineering selection from omitting the seam worst case.

## 7. Gate semantics

Refinement 3 is an evidence-completion gate. It may PASS even if C3 remains performance-unqualified.

Possible engineering attribution values include:

```text
STRICT-PERFORMANCE-CONTRACT-MET
SEAM-WORST-CASE-BLOCKING
DETERMINISTIC-SLOW-PATH-CONFIRMED
REPEATED-PERFORMANCE-TAIL-CONFIRMED
ISOLATED-PERFORMANCE-TAIL-NOT-QUALIFIED
PERFORMANCE-CONTRACT-NOT-MET
```

A returned `STRICT-PERFORMANCE-CONTRACT-MET` may support an RP1C authorization decision only after artifact review. Any other attribution keeps RP1C blocked; C4 is still not automatically authorized.

## 8. Execution

From PowerShell:

```powershell
.\scripts\run-m10-final-vr2-engineering-repair-planning1-rp1b-refinement3.cmd
```

Return the complete folder:

```text
artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-refinement3
```

before RP1C, C4 or any production thermodynamic change.

## 9. Attempt 1 / Hotfix 1

The first execution stopped in the static preflight before build because the production-source leak scan admitted a generated native library from `src/**/bin`. No C3 performance measurement ran and no Refinement-3 engineering attribution exists for that attempt.

Hotfix 1 changes only validator/provenance behavior: the leak scan now examines real `.cs` files outside `bin`/`obj`. C3 and the complete timing/qualification protocol in sections 3–7 remain byte-identical.


## 10. Returned result and successor

Returned Refinement 3 completed with `PASS-PERFORMANCE-ATTRIBUTION-EVIDENCE-COMPLETE`. Exact-v9 max was `150.5 us` and targeted-repeat max `162.7 us`, both below the frozen ceiling, while seam max was `3667.1 us`. The only seam exceedance was one R1-side call among 5,120 R1 measurements. The aggregate artifact did not retain its boundary identity, so RP1B Refinement 4 is authorized only for immutable-C3 R1-seam localization/reproducibility. RP1C and C4 remain unauthorized pending returned Refinement 4 review.
