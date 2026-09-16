# M10 Final V&V Matrix

Status: **FROZEN-PRE-LONG**.

The machine-readable authority is `eng/m10-final-vv-matrix.json`. It contains 27 phenomenon/model rows and separates verification, model assessment, integral qualification and user/HMI acceptance. M10.9.8.5 manual acceptance is recorded as accepted; `LONG-SOAK-01` intentionally remains pending.

The final cumulative gate may not widen frozen I.3 budgets or reinterpret historical exact-version identities. Historical superseded long audits remain provenance unless explicitly selected by the current gate. Passing the cumulative gate does not close M10.

The curated cumulative gate passed on the validated Hotfix 1 baseline (`m10-final-cumulative-validation-passes=True`). The first exact-v4 long remains failed provenance. Replacement-Long Execution 1 also remains **RED** because RL-M1/RL-R1 shared the protected 5→10 MWe path; its other legs and replay/scalability evidence remain preserved. Replacement-Long Failure Diagnostics 1–6 are returned diagnostic evidence, not promotion evidence.

The remaining route is governed by [`M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md`](M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md). P0 Hotfix 2 is VALIDATED. P1 returned `INCONCLUSIVE` after the authorized 1,800 s continuation. P2 Decision Gate 1 is VALIDATED. P1A returned execution PASS / overall `INCONCLUSIVE`: 5.5 MWe `CONVERGED`, 6 MWe load reachability demonstrated but full stationarity not demonstrated at 3,600 s. P2R1 records PLAN-STOP-INCONCLUSIVE and authorizes neither P3-W nor P3-R. Plan Amendment 2 defined P1B slow-state/owner qualification; mandatory Todreas/Kazimi Deep Review Pass 2 returned local PASS / `PASS-AS-AUTHORED`, and P1B has now returned execution PASS / `COUPLED-MULTI-DOMAIN`. P2R2 is now VALIDATED and selects P3-R owner localization only. Before P3-R1 executes, Plan Amendment 3 inserts an external physical-reference assessment hold (VR0–VR5) for point kinetics, simplified water/steam, iodine/xenon and decay heat. `LONG-SOAK-01` remains pending through P1–P4 and may be promoted only after P5 Replacement-Long Execution 2 passes and P6 records exact provenance.

Passing P5B makes M10 closure **eligible**; M10 is not declared CLOSED until P6 promotes the long evidence into the final closure record and this matrix is updated from `FROZEN-PRE-LONG`.

## Post-P2R2 external physical-reference assessment hold

P2R1 / Plan Amendment 2 Hotfix 1, mandatory **Todreas/Kazimi Deep Review Pass 2**, P1B and P2R2 have returned PASS. P1B creates no V&V promotion: `LONG-SOAK-01` remains pending. P2R2 selects P3-R owner localization because whole-plant stationarity is unresolved. **Plan Amendment 3 creates an external physical-reference assessment hold before P3-R1.** VR1–VR4 do not automatically promote any machine-readable V&V row; promotion to `Model validation / assessment` may be proposed only after VR5 reviews quantified independent-reference evidence. P3-W, production repair and the second replacement-long remain unauthorized.


P1B execution is tracked as a closure-plan evidence gate rather than a new row in the frozen machine-readable M10 phenomenon matrix. It cannot promote `LONG-SOAK-01`; its returned evidence is frozen for P2R2. If P2R2 validates, only P3-R1 owner localization becomes authorized.


## External physical-reference assessment hold

The frozen 27-row machine-readable matrix is intentionally not rewritten by planning alone. Plan Amendment 3 adds a pre-P3-R1 assessment program without changing existing row classifications until evidence exists:

- `VR1` — `PHY-NK-01` independent point-kinetics benchmark;
- `VR2` — `TH-WSP-01` IAPWS-IF97 error map;
- `VR3` — `PHY-XE-01` independent I-135/Xe-135 shutdown trajectory;
- `VR4` — `PHY-DH-01` ANS-5.1 decay-heat assessment;
- `VR5` — consolidated impact/claim decision.

A successful script execution alone does not upgrade a row. The row may be proposed for `Model validation / assessment` only if the independent reference, frozen inputs/tolerances and quantified discrepancy support that claim. The full contract is in [`M10_FINAL_PHYSICAL_REFERENCE_MODEL_ASSESSMENT_PLAN1.md`](M10_FINAL_PHYSICAL_REFERENCE_MODEL_ASSESSMENT_PLAN1.md).


## External physical-reference hold status

Plan Amendment 3 is VALIDATED. The **VR0 reference/provenance contract and VR1 Point-Kinetics Independent Benchmark are VALIDATED**. VR2 returned `MODEL-DISCREPANCY-BLOCKING`, and its later returned-evidence adjudication established `HYDRAULIC-MATERIALITY-CONFIRMED`; the project is therefore held in the VR2 engineering-repair planning/requalification route before VR3. The current executable checkpoint is maintained only in `PROJECT.md`; this frozen matrix is not rewritten by each RP1B refinement and no machine-readable row is promoted by planning evidence alone.
