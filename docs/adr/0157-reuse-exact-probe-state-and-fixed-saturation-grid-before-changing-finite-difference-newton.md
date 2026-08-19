# ADR 0157 — Reuse exact probe state and fixed saturation grid before changing finite-difference Newton

## Status

Accepted for M10.9.4.1-H.28.1-D candidate evaluation.

## Context

H.28.1-A showed that Jacobian/probe work dominates H.9 trigger time. H.28.1-C removed ~97.6% of probe allocations without reducing wall time, proving CPU work rather than GC pressure is dominant. H.28.1-B removed the duplicated non-trigger predictor, leaving the unchanged 32-probe Jacobian as the principal bottleneck.

## Decision

Before considering a new nonlinear algorithm, remove only exact duplicate CPU work inside the existing finite-difference probes:

1. reuse already-integrated fluid-node states when the probe hydraulic balance is exactly equal to the reference balance;
2. reintegrate every changed node through the existing thermodynamic path;
3. precompute only the immutable 513-point saturation-property grid used by the fixed coarse saturated scan;
4. leave dynamic boundary scans, bisection, probe count, Jacobian construction and Newton safeguards unchanged.

## Consequences

A green result is an implementation optimization and must preserve the existing deterministic fingerprint. A negative result is also useful: after duplicate predictor work, allocation churn and exact probe-state duplication are removed, persistent ~1.5 s Jacobian cost is strong evidence that the finite-difference probe workload itself is the limiting factor. H.28 must still be rerun before H.29.
