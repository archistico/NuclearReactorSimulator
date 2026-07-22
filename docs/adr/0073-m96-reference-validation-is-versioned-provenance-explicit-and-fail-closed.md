# ADR 0073 — M9.6 reference validation is versioned, provenance-explicit and fail-closed

## Status

Accepted. M9.6 local compilation and the complete automated suite later passed; manual GUI evidence is carried to the M9.7 phase gate.

## Context

After M9.5 established explicit historical provenance/fidelity metadata, the simulator needs quantitative regression/reference validation without creating a second physics owner or presenting internally curated numbers as external historical truth.

The project also needs stronger UI regression evidence before M10 significantly expands the operator interface.

## Decision

1. Reference validation is observational and versioned.
2. Every case declares a stable case ID, model version, provenance text, exact logical-step targets and explicit tolerance budgets.
3. Missing/unavailable evidence fails closed as `Missing`; it is never converted to zero or success.
4. Presentation-level reference extraction reads `ControlRoomSnapshot` only. Private Simulation state is not reached through from Application.
5. Bundled M9.6 v1 cases are explicitly internal validated regression baselines, not external historical measurements.
6. External quantitative datasets require new versioned cases with explicit provenance and justified tolerances.
7. Sensitivity analysis compares results from explicit canonical baseline/perturbed runs; the validation layer never mutates model parameters or optimizes them automatically.
8. Model version is part of every case/report boundary so a changed model cannot silently inherit an old validation identity.
9. App/UI validation is strengthened through deterministic ViewModel/XAML contract tests; visual layout/readability remains a documented manual validation gate until a separately justified visual-regression system exists.

## Consequences

- Calibration/reference evidence is reproducible and attributable.
- Regression baselines cannot silently masquerade as historical measurements.
- Unavailable presentation data cannot accidentally pass validation.
- M9.7 can integrate fidelity evidence across M9.1–M9.6 using explicit versioned reports.
- M10 starts from a better-tested GUI boundary without moving physics or protection ownership into App tests/UI code.

## Non-goals

This ADR does not establish licensing-grade validation, automatic model fitting, source authority ranking, screenshot pixel-diff testing or historical reconstruction accuracy.
