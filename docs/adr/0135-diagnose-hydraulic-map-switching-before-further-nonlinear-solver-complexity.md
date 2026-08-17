# ADR 0135 — Diagnose hydraulic-map switching before further nonlinear-solver complexity

## Status

Accepted for M10.9.4.1-H.10 candidate.

## Context

The frozen H.5-H.9 evidence set contains seven P060/F040-triggered intervals. Bounded Picard rescue reached 6/7, while true-residual backtracking, safeguarded Anderson and a finite-difference damped-Newton method each reached 5/7. H.9 produced well-conditioned Jacobians, bounded Newton steps, deterministic strict merit decrease and the same two persistent line-search failures seen by H.7/H.8.

A further move to Broyden, Newton-Krylov, trust-region or other higher-complexity nonlinear methods would be poorly justified until the local map structure is understood.

## Decision

Before increasing nonlinear-solver complexity, inspect the two persistent H.9 failures for local switching and non-smoothness.

H.10 therefore adds a shadow-only `HydraulicMapSmoothnessAnalyzer` with:

- two-scale pressure probes of existing pipe, valve and pump laws;
- explicit classification of forward/reverse/zero, closed-valve and check-valve-blocked branches;
- one-sided slope-asymmetry and derivative-scale-growth evidence;
- two-scale conserved mass/internal-energy probes through the existing thermodynamic closure;
- phase and supported-envelope transition evidence;
- comparison against the committed explicit endpoint of each failing interval;
- exact deterministic fingerprints.

The diagnostic does not solve for or commit a new state.

## Consequences

If H.10 localizes switching or non-smoothness, later work may formulate an active-set or semi-smooth treatment around the identified component rather than globally increasing solver complexity.

If H.10 does not find such evidence, the next investigation must test local fixed-point existence, residual floors and basin/branch structure before another solver family is introduced.

Production remains explicit at 10 ms throughout.
