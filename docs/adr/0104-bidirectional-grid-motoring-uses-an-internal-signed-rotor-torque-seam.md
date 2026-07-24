# ADR 0104 — Bidirectional grid motoring uses an internal signed rotor-torque seam

## Status

Accepted for M10.9.4.1-E.2 Hotfix 1 candidate. Local validation pending.

## Context

E.2 introduced versioned bidirectional generator/grid coupling. In motoring, the electrical solver correctly computes negative electromagnetic torque: under the turbine rotor sign convention, positive external load torque resists rotation and negative external load torque assists rotation.

The historical M4.2 public `TurbineRotorInput` constructor intentionally rejected negative externally commanded load torque. Passing E.2 motoring torque through that same public seam therefore threw before the rotor balance could be evaluated. Ordinary generation paths remained unaffected, which is why the defect appeared only in the dedicated motoring regression and explicit operational-envelope trajectories that crossed into negative electrical exchange.

## Decision

Keep the public/manual `TurbineRotorInput` contract non-negative for backward compatibility. Add an internal factory dedicated to signed generator electromagnetic torque and use it only from `GeneratorGridSolver` when it rewrites the rotor input owned by the generator/grid integration layer.

The rotor balance itself remains unchanged:

- positive external torque opposes turbine torque;
- negative external torque adds motoring torque to the rotor;
- the existing zero-speed anti-reverse limiter continues to constrain excessive positive resisting torque only.

## Consequences

- Current-v2 bidirectional motoring can reach the turbine rotor solver.
- Public/manual legacy callers still cannot inject arbitrary negative load torque.
- Generation-only coupling remains unchanged.
- Signed mechanical/electrical power and positive conversion-loss accounting introduced by E.2 remain unchanged.
- E.3 protections remain deferred until E.2 plus this hotfix are locally green.
