# ADR 0133 — Use safeguarded Anderson acceleration before Jacobian-informed hydraulic root solving

## Status

Accepted and validated through M10.9.4.1-H.8.

## Context

H.6 showed that bounded Picard relaxation/iteration retuning rescued only 6/7 frozen difficult intervals. H.7 replaced relaxed-motion convergence with the true unrelaxed fixed-point residual and deterministic monotone backtracking, but still converged only 5/7 and exhausted its line search on two events. This indicates that the remaining limitation is the fixed-point search direction, not merely relaxation size or a false convergence criterion.

## Decision

Add a separate shadow-only `AndersonHydraulicCorrectorSolver` before introducing an explicit Jacobian-based Newton method.

The solver uses bounded-memory regularized affine residual minimization over recent unrelaxed hydraulic-map evaluations. Coefficients are safeguarded by finiteness, affine-sum and L1-norm checks. Every accelerated direction is subjected to the unchanged H.7 fixed-point merit and deterministic backtracking. If acceleration cannot provide an admissible decreasing direction, the solver falls back to the H.7 residual direction.

Production remains explicit. The historical Picard solver, H.7 solver and `PlantNetworkOrchestrator` routing are unchanged.

## Consequences

This isolates whether a multisecant direction can recover the frozen events without the implementation and evaluation cost of a full numerical Jacobian. If H.8 still fails to qualify, the evidence supports moving to a Jacobian-informed Newton/quasi-Newton formulation rather than further Picard/relaxation tuning.


## Validation note

H.8 was user-validated and returned 5/7 convergence with two line-search exhaustions and `accelerated-corrector-qualification-passes=False`; this consequence activates the ADR's planned transition to Jacobian-informed H.9 development.
