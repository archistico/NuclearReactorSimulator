# M10 Final — Physical Reference Model Assessment Plan 1

**Status: ACTIVE UNDER VALIDATED PLAN AMENDMENT 3. VR0 is VALIDATED; VR1 is now the active execution candidate.**

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


## VR1 active execution binding

VR0 returned local PASS on 2026-09-14 and is frozen as VALIDATED provenance. VR1 now executes the first quantitative comparison using the contract in [`M10_FINAL_VR1_POINT_KINETICS_INDEPENDENT_BENCHMARK.md`](M10_FINAL_VR1_POINT_KINETICS_INDEPENDENT_BENCHMARK.md). A VR1 PASS may authorize only VR2; it does not calibrate exact-v9 plant parameters or authorize P3-R1.
