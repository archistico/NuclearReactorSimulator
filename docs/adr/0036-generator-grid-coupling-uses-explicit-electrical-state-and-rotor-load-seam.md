# ADR 0036 — Generator/grid coupling uses explicit electrical state and the existing rotor-load seam

## Status

Accepted for M4.5 baseline candidate.

## Context

M4.2 established a separate turbine rotor state and a manual external-load torque seam. M4.5 must add generator/grid physics without introducing a second rotor integrator, wall-clock-dependent phase, hidden electrical energy sinks or double mechanical loading.

## Decision

- Add a separate deterministic `GeneratorGridState` for grid phase, generator electrical phase and breaker state.
- Derive generator frequency from M4.2 rotor speed and configured pole-pair count.
- Advance electrical/grid phase only from committed state and fixed timestep.
- Require exactly one synchronous generator per M4.2 rotor.
- Require legacy M4.2 manual external-load torque to be zero while M4.5 owns electromagnetic loading.
- Translate accepted generator electrical loading into the existing M4.2 `TurbineRotorInput.ExternalLoadTorque` seam before the single rotor integration.
- Accept manual breaker closure only inside explicit frequency/phase/voltage synchronization windows.
- Reconcile mechanical input, electrical export and conversion losses in an explicit electrical audit.
- Keep automatic synchronization, excitation/governor control and protection logic deferred to M5.

## Consequences

The turbine mechanical integrator remains authoritative, replay stays deterministic, and later electrical fidelity can evolve behind stable state/topology seams. M4.5 can train manual synchronization without contaminating plant fluid/thermal state or prematurely implementing automatic controls.
