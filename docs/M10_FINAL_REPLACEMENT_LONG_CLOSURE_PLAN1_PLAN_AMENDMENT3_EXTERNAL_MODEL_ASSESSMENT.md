# M10 Final Replacement-Long Closure Plan 1 — Plan Amendment 3

## External Physical-Reference Model Assessment Hold Before P3-R1

**Status: VALIDATED / DOCUMENTATION ONLY.**

## 1. Trigger

P2R2 is validated and authorizes `P3-R-OWNER-LOCALIZATION`, but not production repair. After P2R2, an independent review identified a documented V&V weakness: several fundamental owners are internally verified without a quantified external-reference error map. The existing M10 V&V matrix already distinguishes verification from external model assessment, so this amendment does not reverse previous engineering evidence. It inserts a bounded reference-assessment program before P3-R1 so that owner localization and any later repair decision are made with stronger quantitative context.

## 2. Decision

P3-R remains the selected branch. **P3-R1 execution is temporarily held**, not cancelled.

The next authorized sequence becomes:

`VR0 → VR1 → VR2 → VR3 → VR4 → VR5 → P3-R1`.

No production source change is authorized by this amendment.

## 3. Frozen invariants

Until VR5 completes:

- exact-v9 is immutable;
- production workload is unchanged;
- authority policy is unchanged;
- generator-load semantics are unchanged;
- protection semantics are unchanged;
- mission binding is unchanged;
- P3-W remains unauthorized;
- production repair remains unauthorized;
- second replacement-long baseline remains unauthorized;
- P1/P1A/P1B/P2R2 artifacts remain frozen evidence.

## 4. Why this is not scope creep

VR1–VR4 are small, isolated reference comparisons. They do not add plant features, new runtime state or new operator behavior. Their purpose is to quantify already-declared model limitations and protect P3-R from repairing the wrong owner.

## 5. Exit

Plan Amendment 3 is satisfied only after VR5 emits one of:

- `PROCEED-P3R1-EXACTV9`;
- `BLOCK-P3R1-MODEL-REPAIR`;
- `PLAN-STOP-REFERENCE-GAP`.

Only the first directly unblocks P3-R1.

Detailed execution rules are in `M10_FINAL_NEXT_STEPS_DETAILED_EXECUTION_PLAN.md` and `M10_FINAL_PHYSICAL_REFERENCE_MODEL_ASSESSMENT_PLAN1.md`.


## 6. Validation status and VR0 handoff

Plan Amendment 3 is VALIDATED from the user-reported local audit PASS on 2026-09-14. The next authorized gate is `VR0-Reference-Provenance-Contract-Freeze`. VR0 is documentation/reference-contract only and does not execute VR1-VR4 or P3-R1. Its frozen detailed contract is in `M10_FINAL_VR0_REFERENCE_PROVENANCE_CONTRACT.md`.
