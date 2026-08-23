# M10 Final V&V Matrix

Status: **FROZEN-PRE-LONG**.

The machine-readable authority is `eng/m10-final-vv-matrix.json`. It contains 27 phenomenon/model rows and separates verification, model assessment, integral qualification and user/HMI acceptance. M10.9.8.5 manual acceptance is recorded as accepted; `LONG-SOAK-01` intentionally remains pending.

The final cumulative gate may not widen frozen I.3 budgets or reinterpret historical exact-version identities. Historical superseded long audits remain provenance unless explicitly selected by the current gate. Passing the cumulative gate does not close M10.

The curated cumulative gate passed on the validated Hotfix 1 baseline (`m10-final-cumulative-validation-passes=True`). The first exact-v4 long remains failed provenance. Replacement-Long Execution 1 also remains **RED** because RL-M1/RL-R1 shared the protected 5→10 MWe path; its other legs and replay/scalability evidence remain preserved. Replacement-Long Failure Diagnostics 1–6 are returned diagnostic evidence, not promotion evidence.

The remaining route is governed by [`M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md`](M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md). P0 Hotfix 2 is VALIDATED. P1 returned `INCONCLUSIVE` after the authorized 1,800 s continuation. P2 Decision Gate 1 is the active planning checkpoint and authorizes neither P3-W nor P3-R; Plan Amendment 1 permits only P1A asymptotic closure extension before P2R branch selection. `LONG-SOAK-01` remains pending through P1–P4 and may be promoted only after P5 Replacement-Long Execution 2 passes and P6 records exact provenance.

Passing P5B makes M10 closure **eligible**; M10 is not declared CLOSED until P6 promotes the long evidence into the final closure record and this matrix is updated from `FROZEN-PRE-LONG`.
