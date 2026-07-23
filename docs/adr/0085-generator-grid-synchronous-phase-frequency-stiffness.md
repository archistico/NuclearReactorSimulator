# ADR 0085 — Paralleled generators use explicit phase/frequency grid stiffness

**Status:** Accepted for M10.9.4 Hotfix 18 implementation candidate.

## Context

The historical M4.5 infinite-bus model converted requested electrical power directly into a constant opposing shaft torque evaluated at rated rotor speed. Once the breaker was closed, generator electrical phase continued to integrate from rotor speed but phase/frequency slip did not feed back into electromagnetic torque. A paralleled rotor could therefore drift far from synchronous speed while remaining electrically connected, with exported MWe falling roughly in proportion to rotor speed.

## Decision

`SynchronousGeneratorDefinition` may opt into `SynchronousGridCouplingDefinition`. For a closed breaker, current-v2 electromagnetic loading is the dispatched mechanical-power equivalent plus a bounded reduced-order infinite-bus correction:

```text
P_phase = P_sync,max * sin(delta)
P_frequency = P_damp@1Hz * (f_generator - f_grid)
P_load = clamp(P_dispatch + P_phase + P_frequency, 0, P_generator,max / efficiency)
T_e = P_load / omega_rated
```

Positive generator phase lead and positive frequency slip increase electromagnetic loading. Negative slip unloads the rotor so the infinite-bus boundary provides restoring frequency stiffness rather than allowing unconstrained kinetic-energy discharge. The actual rotor remains integrated only by the canonical M4.2 turbine rotor solver.

Current sustained-generation and synchronization v2 definitions use:

- `P_sync,max = 10 MW`;
- `P_damp@1Hz = 10 MW`.

At the validated Hotfix 17 design point (`delta = 0`, `f = 50 Hz`) both corrections are zero, so the initial dispatched operating point is unchanged.

A null coupling retains the historical dispatch-torque-only law as an isolated legacy seam.

## Consequences

- Rotor/grid phase and frequency now affect shaft electromagnetic loading after synchronization.
- Grid coupling is deterministic and uses committed electrical/rotor state only.
- Load request remains the operator dispatch setpoint; phase/frequency terms are restoring corrections, not a second load-control authority.
- No pump, protection, actuator-rate or adaptive-substep changes are included in this decision.
- Loss-of-synchronism protection remains a later protection-layer item; Hotfix 18 first establishes the missing physical stiffness and validates it independently.
