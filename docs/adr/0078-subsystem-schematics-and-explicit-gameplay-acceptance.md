# ADR 0078 — Subsystem schematics remain presentation topology; long gameplay acceptance is explicit

## Status

Accepted for M10.9.4 implementation candidate.

## Context

The whole-plant mimic improved macro-level situation awareness, but detailed workspaces remained card-heavy. A manual run also exposed an observability gap: amber SHAFT plus later 0 MWe did not tell the operator whether the issue was process support, synchronization/breaker/load sequencing, protection, or integrated long-run behavior.

## Decision

1. Detailed reactor, primary, turbine, generator/grid and instrumentation/protection diagrams are immutable Application-owned presentation topology derived from existing control-room snapshots.
2. Avalonia only renders those contracts.
3. Process-medium colors are distinct from severity colors; amber mechanical shaft is not a warning state.
4. Generator power-path diagnostics expose existing breaker/sync/requested-load/shaft/output/protection evidence without predicting physics.
5. Long operator-journey/endurance acceptance tests use xUnit v3 explicit tests so normal development tests remain fast.
6. A long-test failure must be fixed in the smallest canonical owner; the test must not be weakened merely to preserve a candidate milestone.

## Consequences

- operators can understand detailed connectivity and signal priority directly in the HMI;
- 0 MWe states become diagnosable without introducing UI-side physics;
- the normal suite remains fast;
- integrated balance regressions gain a deliberate, separately runnable acceptance gate.
