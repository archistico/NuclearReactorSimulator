# ADR 0134 — Use conservative-coordinate finite-difference Newton before diagnosing hydraulic-map non-smoothness

## Status

Accepted for M10.9.4.1-H.9 candidate evaluation.

## Context

H.8 validated the safeguarded-Anderson implementation but did not improve frozen-event convergence: H.4 remained 5/7, H.6 remained the best fixed-relaxation result at 6/7, and both H.7 residual/backtracking and H.8 safeguarded Anderson converged 5/7 with two line-search exhaustions. H.8 nevertheless proved exact determinism, strict accepted-merit decrease, bounded coefficients, preserved conservation/ownership and a low deterministic work ratio. The remaining evidence therefore justifies changing from fixed-point/multisecant directions to a Jacobian-informed root direction.

A naive numerical Jacobian over arbitrary node balances could violate hydraulic mass closure or pump-energy ownership during finite-difference probes and line-search trials. Such violations would contaminate the numerical diagnosis with states outside the already validated physical ownership contract.

## Decision

Add a separate shadow-only `JacobianHydraulicCorrectorSolver`.

The solver parameterizes each hydraulic iterate in deterministic conservative coordinates:

- all non-anchor node hydraulic mass-balance rates are independent coordinates and the anchor rate closes total hydraulic mass exactly;
- all non-anchor node hydraulic energy rates are independent coordinates;
- pump hydraulic power is an independent coordinate and the anchor energy rate closes total hydraulic energy to that pump-fluid-work ownership exactly;
- pipe, valve and pump mass-flow rates remain explicit coordinates.

At every accepted iterate H.9 builds a scaled forward finite-difference Jacobian of the conservative fixed-point residual, with deterministic backward probing only when a forward probe is inadmissible. The normalized linear system is solved with deterministic scaled partial pivoting, bounded by a pivot-conditioning estimate and a small diagonal regularization. The normalized Newton step is capped before a deterministic H.7-style line search. A trial is accepted only when the unchanged unrelaxed pressure/flow merit strictly decreases. If Jacobian construction/solve is rejected or the Newton direction cannot reduce merit, H.9 falls back to the H.7 residual direction.

Production remains explicit. H.9 does not replace Picard, H.7, H.8, or `PlantNetworkOrchestrator` routing.

## Consequences

H.9 directly tests whether local derivative information resolves the two persistent frozen events while preserving the established conservation contract. A positive result authorizes only broader shadow qualification. A negative result is evidence to inspect switching surfaces, discontinuities or local non-existence/non-smoothness in the hydraulic map before adding still more solver sophistication.
