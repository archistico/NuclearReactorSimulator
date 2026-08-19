# New chat start

We are continuing the **Nuclear Reactor Simulator** C#/.NET 10/Avalonia project.

Authoritative fully validated baseline: **M10.9.4.1-I.2 — Audit Consolidation & CI Baseline Hardening**.

Validated later evidence:

- I.3 Hotfix 4 Classifier Fix 1: exact v2 has 338/338 generation drops coincident with targeted stop/control/admission reverse flow; exact v3 has 0 drops / 0 targeted reverse flow over 100 s at 10 ms resolution.
- I.3 Hotfix 5: exact v3 completed 300 s / 30,000 steps with 0 health violations, 0 targeted reverse flow, 3,757 corrected commits, 0 rollback/fallback/unsafe/untargeted disagreement and deterministic repeat.

Current candidate: **M10.9.4.1-H.30 Requalification 1 — Production Policy Re-review after I.3 Continuity Evidence**.

Candidate decision: `ACTIVATE` exact v3 corrected-commit as authoritative desktop default while preserving exact v2 explicit as fail-closed rollback/reference. H.28 remains `bounded-but-costly`; no numerical mathematics, coefficients or 10 ms timestep are changed.

I.3 tolerance budgets are still unfrozen. If H.30 RQ1 validates, return to I.3 using the authoritative v3 policy before final Phase-I closure.

Read `PROJECT_HANDOFF.md` and `PROJECT_STATUS.md` first. Detailed M10.9.4.1 history is archived under `history/m10.9.4.1/` and must not be mistaken for current policy.
