# ADR 0125 — Fixed-step refinement evidence precedes any numerical-method change

## Status

Accepted and validated through M10.9.4.1-H.1 evidence; H.2 consumes the result.

## Context

The current simulator uses a deterministic 10 ms fixed step. Long-horizon conservation and operational gates are green, but operator-visible raw primary flows have shown strong step-scale alternation and F.3 introduced committed-state condenser sequencing that may be timestep-sensitive. Phase G has now removed the major open-control-volume energy-convention ambiguity.

Changing directly to adaptive substepping or semi-implicit pressure/flow coupling without a refinement study would make it impossible to distinguish a justified numerical correction from parameter masking.

## Decision

1. Production current-v2 retains its 10 ms deterministic fixed step during H.1.
2. An internal Application-test evidence seam may construct the same versioned desktop point at explicitly requested divisor timesteps.
3. H.1 freezes the refinement points at 10, 5 and 2.5 ms.
4. The versioned desktop seed keeps a constant 20 ms preconditioning duration at every refinement level.
5. The audit records final-state convergence, raw hydraulic step changes, dominant per-step fractional inventory/pressure changes and wall-clock execution cost.
6. Wall-clock cost is evidence only. It never participates in simulation decisions.
7. No physical coefficient, controller tuning, protection threshold or presentation filter may be changed to make the refinement sweep look convergent.
8. H.2 must explicitly choose: retain 10 ms explicit integration, bounded deterministic substeps, or a designed semi-implicit pressure/flow treatment.
9. Hidden nonlinear repair and wall-clock adaptation remain prohibited.

## Consequences

- Numerical-method selection becomes evidence-driven and reproducible.
- Production/replay identity is unchanged by H.1.
- A poor convergence result is allowed and is considered useful audit evidence.
- Future extreme-operation/incident work has a measurable numerical-robustness prerequisite instead of relying on interlocks that merely protect the solver.
