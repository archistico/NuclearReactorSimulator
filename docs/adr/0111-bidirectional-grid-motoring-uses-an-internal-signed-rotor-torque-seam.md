# ADR 0111 — Bidirectional grid motoring uses an internal signed rotor-torque seam

## Status

Proposed design / M10.9.4.1-E.2 follow-up. Not implemented in the validated D.4 source.

## Context

The planned E.2 design introduces versioned bidirectional generator/grid coupling. In the proposed motoring path, the electrical solver must compute negative electromagnetic torque: under the turbine rotor sign convention, positive external load torque resists rotation and negative external load torque assists rotation.

The historical M4.2 public `TurbineRotorInput` constructor intentionally rejected negative externally commanded load torque. Passing future E.2 motoring torque through that same public seam would fail before the rotor balance is evaluated. The internal seam is therefore a design requirement for the E.2 implementation, not validated current behavior.

## Decision

Keep the public/manual `TurbineRotorInput` contract non-negative for backward compatibility. Add an internal factory dedicated to signed generator electromagnetic torque and use it only from `GeneratorGridSolver` when it rewrites the rotor input owned by the generator/grid integration layer.

The rotor balance itself remains unchanged:

- positive external torque opposes turbine torque;
- negative external torque adds motoring torque to the rotor;
- the existing zero-speed anti-reverse limiter continues to constrain excessive positive resisting torque only.

## Consequences

- Current-v2 bidirectional motoring will be able to reach the turbine rotor solver once E.2 is implemented.
- Public/manual legacy callers still cannot inject arbitrary negative load torque.
- Generation-only coupling remains unchanged.
- Signed mechanical/electrical power and positive conversion-loss accounting remain required E.2 behavior.
- E.3 protections remain deferred until E.2 plus this internal seam are implemented and validated.
