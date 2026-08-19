# M10.9.4.1-I.3 Hotfix 5 — Corrected 300 s Healthy Reference Requalification

I.2 remains the authoritative validated baseline. I.3 remains unvalidated. The preceding Hotfix 4 Classifier Fix 1 diagnostic is validated evidence: exact v2 produced 338 generation-drop steps and 338 targeted stop/control/admission reverse-flow steps with one-for-one coincidence, while exact v3 produced zero drops and zero targeted reverse-flow steps over the same first 100 s.

Hotfix 5 extends exact v3 `integrated-operations-desktop-stable@3` / `FourNodeBranchContinuityCorrectedCommitOptIn` to the full 300 s healthy reference horizon. Every 10 ms step is checked for trip/breaker/generation health and targeted reverse flow. One-second samples provide conservation/inventory trajectory evidence and final-60-second slopes. A separate 256-step repeat verifies deterministic trace equivalence.

A green gate requires zero generation-health violations, zero targeted reverse flow, zero rollback/fallback/unsafe/untargeted disagreement, closed conservation bounds, finite seven-slope inventory observations and deterministic repeat. It does not freeze I.3 tolerance budgets and does not change H.30 policy. It only unblocks a separate production-policy re-review.
