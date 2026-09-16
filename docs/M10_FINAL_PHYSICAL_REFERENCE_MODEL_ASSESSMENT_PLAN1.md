# M10 Final — Physical Reference Model Assessment Plan 1

**Status: ACTIVE UNDER VALIDATED PLAN AMENDMENT 3. VR0 and VR1 are VALIDATED; VR2 is `MODEL-DISCREPANCY-BLOCKING`; Materiality Diagnostic 1 adjudication is closed as `HYDRAULIC-MATERIALITY-CONFIRMED`; VR2 Engineering Repair Planning 1 and RP1A REV1 are VALIDATED; the first-generation RP1B B1/C1/D1 matrix is frozen complete and non-selecting; RP1B Refinement 1 Hotfix 1 C2/D2 is the active execution candidate; Attempt 1 was static-preflight RED only and produced no candidate evidence.**

This plan closes a known credibility gap before P3-R1: several core subsystems are strongly internally verified, but their quantitative relationship to independent external references is not yet documented with an error budget.

This is **model assessment**, not a blanket claim of physical validation.

## Scope

Four bounded work packages:

| ID | Owner | External reference target | M10 relevance |
| --- | --- | --- | --- |
| VR1 | point kinetics | independent step-reactivity solution | high methodological relevance; current P1B thermal source already nearly static |
| VR2 | simplified water/steam | IAPWS-IF97 | high direct relevance to exact-v9 thermodynamic/steam ownership |
| VR3 | I-135/Xe-135 | independent published-parameter shutdown trajectory | conditional M10 relevance; high future M14 relevance |
| VR4 | decay heat | ANS-5.1 reference curve | conditional M10 relevance; high M12.5 relevance |

VR0 freezes source provenance/tolerances. VR5 performs impact-aware synthesis.

## Claim vocabulary

Use these terms consistently:

- `TEST PASS` — the software test completed within its test contract;
- `VERIFIED` — implementation/equation/invariant behavior is verified;
- `MODEL-ASSESSED` — quantitative comparison with an independent external reference exists;
- `QUALIFIED` — behavior is accepted within an explicitly declared scope/range;
- `PHYSICALLY VALIDATED` — reserved for evidence that justifies this stronger term; not created by this plan by default.

A milestone may be `VALIDATED` as an engineering gate while individual physical models remain only `VERIFIED` or `MODEL-ASSESSED`. Documentation must make that distinction explicit.

## Reference hierarchy

Preferred order:

1. official standard/specification or issuing-body technical document;
2. peer-reviewed/reference textbook with reproducible equations/parameters;
3. authoritative national-lab/regulator/standards implementation;
4. independently implemented high-precision numerical solution based on 1–3.

Do not use a blog, forum, or production implementation as the sole numerical authority.

## Tolerance policy

Tolerances must be frozen before reading final production comparison results. They must separate:

- external-reference precision;
- expected numerical discretization error;
- reduced-model approximation error;
- claim policy.

A tolerance is not allowed to hide reduced-model error by relabeling it as numerical error.

For reduced-order models, the first objective is an honest **error map**, not forcing every model to licensing-grade accuracy.

## Evidence package for every VR gate

Each gate must produce:

- source/provenance manifest;
- frozen parameter/input table;
- expected reference table;
- production result table;
- absolute/relative error table;
- deterministic repeat result;
- classification;
- impact statement on current M10 claims/gates;
- known limitations and non-transferable conclusions.

## No silent tuning

The first assessment run is observational. Do not change coefficients between seeing the result and recording the artifact. If a repair is later authorized, preserve the original assessment as provenance and assess the repaired exact version separately.

## Relationship to P3-R1

VR5 is the authority gate. P3-R1 remains blocked while this plan is active. The expected success route is `VR5=PROCEED-P3R1-EXACTV9`. A material reference discrepancy in an owner active during P1B may instead require a model repair decision before owner localization.


## VR0 frozen contract binding

Plan Amendment 3 is locally VALIDATED. VR0 now freezes the executable reference contracts before VR1 begins. The authoritative detailed values are in [`M10_FINAL_VR0_REFERENCE_PROVENANCE_CONTRACT.md`](M10_FINAL_VR0_REFERENCE_PROVENANCE_CONTRACT.md) and the source/runtime provenance is in [`research/M10_FINAL_VR0_EXTERNAL_REFERENCE_REGISTER.md`](research/M10_FINAL_VR0_EXTERNAL_REFERENCE_REGISTER.md) plus [`research/M10_FINAL_VR0_RUNTIME_APPLICABILITY_AUDIT.md`](research/M10_FINAL_VR0_RUNTIME_APPLICABILITY_AUDIT.md).

Key VR0 clarifications are now frozen: VR1 assesses the generic point-kinetics solver against Hébert's six-group equations while exact-v9's one-group parameterization remains reduced; VR2 is directly M10-material and uses IAPWS R7-97(2012); VR3 assesses the generic I/Xe solver but the M9.3 configuration remains educational and inactive in exact-v9 sustained generation; VR4 records that no canonical production decay-heat definition is active in exact-v9, so the absence of an ANS-calibrated group set is a future M12.5/reference-configuration gap rather than an automatic M10 blocker.


## VR1 validated execution binding

VR0 returned local PASS on 2026-09-14 and is frozen as VALIDATED provenance. VR1 subsequently executed the first quantitative comparison and is now VALIDATED from returned artifact review under [`M10_FINAL_VR1_POINT_KINETICS_INDEPENDENT_BENCHMARK.md`](M10_FINAL_VR1_POINT_KINETICS_INDEPENDENT_BENCHMARK.md). Its PASS authorized VR2 only; it did not calibrate exact-v9 plant parameters or authorize P3-R1.

## VR2 returned RED — materiality re-entry

Returned VR2 artifact review classifies the water/steam gate as `MODEL-DISCREPANCY-BLOCKING`. The IF97 reference self-check remains qualified (`2.8096211618213283E-09 <= 1E-08`), all frozen inverse states resolve, no M10-core phase mismatch occurs, and the blocker is localized to `INVERSE-COMPRESSED-LIQUID/resolved-pressure` at the 200 C / 2 MPa, 250 C / 7 MPa and 280 C / 10 MPa M10-core points.

VR3 was therefore not authorized. The completed follow-up gate [`M10_FINAL_VR2_REPLANNING_MATERIALITY_DIAGNOSTIC1.md`](M10_FINAL_VR2_REPLANNING_MATERIALITY_DIAGNOSTIC1.md) replayed the validated P1B exact-v9 path and evaluated the offline IF97 pressure-only counterfactual. Its returned evidence has now been adjudicated `HYDRAULIC-MATERIALITY-CONFIRMED`; current authority has moved to Engineering Repair Planning 1.

## VR2 materiality Attempt 5 — returned-evidence adjudication history

The full exact-v9/P1B materiality observation has now executed and produced the complete seven-file evidence set. Its generated summary classifies the IF97 pressure-only counterfactual as `HYDRAULIC-MATERIALITY-CONFIRMED`, but the test process returned RED afterward because the diagnostic demanded `1e-9 kg/s` equality between an H.22 committed fixed-point iterate and its instantaneous hydraulic map. Exact-v9 H.22 explicitly permits an absolute fixed-point flow residual up to `1e-2 kg/s`, and the returned maximum difference `0.009952798974779853 kg/s` remains inside that numerical contract. The separate fail-closed adjudication has now returned PASS and confirms `HYDRAULIC-MATERIALITY-CONFIRMED` after conservatively accounting for the full production residual ceiling; the original materiality thresholds remain unchanged. VR3, P3-R1 and production repair remain blocked while Engineering Repair Planning 1 is active.

## VR2 Engineering Repair Planning 1 binding

Returned-evidence adjudication is complete and confirms `HYDRAULIC-MATERIALITY-CONFIRMED`. VR3 therefore remains blocked while validated Planning 1 defines the repair domain and candidate-selection route. Returned RP1A REV1 evidence is now VALIDATED. The only authorized next execution is RP1B [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_TEST_ONLY_SHADOW_CANDIDATE_MATRIX.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_TEST_ONLY_SHADOW_CANDIDATE_MATRIX.md).

The planning gate treats the defect as `(v,u) -> phase,T,p,quality` ownership, because the actual exact-v9 path contains 72/72 sampled `pressure` inventories and 55/72 sampled `suction` inventories that production calls `SubcooledLiquid` while independent IF97 resolves Region-4 mixture. A standalone bulk-modulus/specific-heat retune is therefore not sufficient.

Planning 1 requires RP1A corpus/seam freeze, RP1B test-only shadow comparison and RP1C zero-or-one candidate selection before production code can be changed. Exact-v9 and `CorrelationConsistentInverseDomain` semantics remain immutable. Any future repair must use a new opt-in closure mode and later a new exact-version identity after requalification.
