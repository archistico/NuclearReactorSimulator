# ADR 0041 — Reactor/primary control reuses M2 neutronics and canonical pump state

## Status

Accepted. M5.3 was subsequently locally validated.

## Context

M5.2 provides generic measured-signal controllers and typed actuator commands but deliberately does not bind them to plant-specific physical owners. M5.3 must close reactor-power and primary-circulation control paths without creating a second rod model, a synthetic reactor-power integrator or a parallel pump/hydraulic solver.

## Decision

1. Reactor-power controllers consume only M5.1 measured reactor thermal power.
2. Rod commands are applied through the existing M2 `ControlRodSystemSolver`.
3. Point kinetics uses committed rod reactivity plus an explicit non-rod reactivity seam; commands generated in the current step affect rod reactivity on the next committed step.
4. Fission thermal power is derived through the existing M2 point-kinetics/fission-power path and replaces only the M3 total-fission-power input seam.
5. M3 remains authoritative for spatial/core-zone/channel heat deposition; M5.3 never applies a second heat-deposition path.
6. Main-circulation controllers may command only pumps already owned by the canonical M3 circulation topology.
7. Pump commands replace canonical `PumpState` operational values before the one existing M4.7 physical step; hydraulic state evolution remains owned by `PlantNetworkOrchestrator`.
8. Drum/feedwater/steam/turbine loops remain outside M5.3 and are owned by M5.4.

## Consequences

- no direct PID-output-to-MW shortcut exists;
- controller behavior remains affected by sensor lag/faults because it consumes `MeasuredSignalFrame` only;
- reactor neutronics and rod dynamics reuse validated M2 physics;
- one-step rod actuation delay is explicit and deterministic;
- no duplicate pump, fluid or thermal inventory is introduced;
- later feedback/xenon composition can enter through the explicit non-rod reactivity seam without changing controller primitives.
