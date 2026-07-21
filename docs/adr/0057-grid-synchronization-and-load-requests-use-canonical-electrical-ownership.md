# ADR 0057 — Grid synchronization and load requests use canonical electrical ownership

**Status:** Accepted / M7.5 validated

## Context

M7.5 must expose breaker closure and generator loading without creating scenario/UI ownership of synchronization physics, electromagnetic torque or electrical output.

## Decision

1. The M7.5 initial condition seeds a pre-synchronization handoff with breaker open; it does not pre-close or bypass the breaker.
2. M4.5 `SynchronizationConditionsSatisfied` remains the sole close-check result.
3. `GeneratorBreakerClose` remains a one-step command consumed by the existing generator/grid solver.
4. `GeneratorLoadRaise/Lower` changes only bounded M4.5 `RequestedElectricalPower` persistent input in Application command translation.
5. No Application/UI code writes electromagnetic torque, rotor speed or electrical output directly.
6. M5.4 speed governing continues to command canonical steam admission in response to rotor speed; reactor thermal power is changed only through M5.3/M2 rod-reactivity-kinetics ownership.
7. M7.5 guidance/checks are observational and cannot force synchronization or load acceptance.

## Consequences

The operator must coordinate thermal input and electrical load explicitly, while all physical consequences remain in validated M2–M5 solvers. M7.6 can build manoeuvring procedures on this stable ownership boundary.
