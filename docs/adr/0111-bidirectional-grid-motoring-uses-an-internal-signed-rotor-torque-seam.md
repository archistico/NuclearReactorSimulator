# ADR 0111 — Bidirectional grid motoring uses an internal signed rotor-torque seam

## Status

Accepted and validated in M10.9.4.1-E.2 Hotfix 1 on 2026-07-26.

## Context

Bidirectional generator/grid coupling must apply negative electromagnetic torque when the connected rotor is below synchronous speed. Under the turbine-rotor sign convention, positive external load torque resists rotation and negative external torque assists rotation.

The historical public `TurbineRotorInput` constructor intentionally rejects negative manually commanded load torque. Reusing that public seam for motoring would either break compatibility or reject the valid grid-owned torque before the rotor balance.

## Decision

Keep the public/manual `TurbineRotorInput` contract non-negative. Add an internal factory dedicated to signed generator electromagnetic torque and call it only from `GeneratorGridSolver` when the generator/grid integration layer rewrites rotor input.

The rotor balance remains unchanged:

- positive external torque opposes turbine torque;
- negative external torque adds motoring torque;
- the existing zero-speed anti-reverse limiter constrains excessive positive resisting torque only.

Computed direction aliases remain excluded from JSON serialization so replay/checkpoint payload contracts do not change merely because convenience diagnostics were added.

## Consequences

- Current-v2 bidirectional motoring reaches the canonical rotor solver without opening arbitrary negative manual torque.
- Public/manual legacy callers retain their historical validation.
- Generation-only and null-coupling behavior remain compatible.
- Signed mechanical/electrical exchange and positive losses are owned by the generator/grid solver.
- E.3.1 recorded and validated signed trajectories; E.3.2 derives supervised delayed protection from the reviewed reports.
