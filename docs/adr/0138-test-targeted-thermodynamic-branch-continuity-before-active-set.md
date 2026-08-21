# ADR 0138 — Test targeted thermodynamic branch continuity before active-set reformulation

## Status

Accepted for M10.9.4.1-H.13 candidate.

## Context

Validated H.12 proved that `steam` and `stop-out` expose overlapping saturated/superheated inverse roots and that coarse saturated detection can toggle under tiny conserved-inventory perturbations. Because production selection uses fixed branch priority and ignores `previousState`, a valid later saturated root can be shadowed by an earlier superheated root.

## Decision

Before introducing a thermodynamic active-set or semi-smooth formulation, test two shadow-only selectors under the unchanged H.9 hydraulic corrector:

1. previous-phase continuity;
2. bounded previous-phase hysteresis with explicit 2% pressure and 5 K temperature release limits.

Apply the experiment only to `steam` and `stop-out`. Keep production `Resolve()` and all production routing unchanged.

## Rationale

H.12 identified a branch-selection discontinuity rather than a lack of nonlinear hydraulic solver sophistication. A targeted continuity experiment directly tests causality with less implementation risk than changing the global thermodynamic formulation.

The bounded policy is deliberately preferred if both alternatives qualify because it has a deterministic escape condition and does not amount to permanent phase locking.

## Consequences

H.13 may produce a negative qualification result without failing as a diagnostic milestone. Production remains explicit at 10 ms and receives no hysteresis or active-set behavior. A positive result authorizes broader shadow qualification only, not production activation.
