# ADR 0050 — Turbine/electrical panels observe canonical owners and dispatch typed intents

## Status

Accepted and locally validated with M6.5.

## Context

The operator UI now needs turbine, condenser/feedwater, generator synchronization and breaker controls. These domains already have validated M4 physical owners and M5 control/protection ownership. Reimplementing synchronization, governor logic, condenser state or breaker physics in Avalonia would create competing authorities.

## Decision

1. `ControlRoomSnapshotProjector` is the only M6.5 boundary that may project M4/M5 immutable state into Application presentation records.
2. M5.1 channels are used for measured instruments whenever a canonical semantic source exists; non-instrumented values are labelled model diagnostics.
3. Avalonia binds only to `TurbineSecondaryPanelSnapshot` and `ElectricalPanelSnapshot` plus related presentation records.
4. Turbine/generator trip presentation observes M5.5 state; UI commands are typed intents and cannot directly alter protection state.
5. Breaker close/open commands target canonical breaker identity. UI close enablement may fail closed from the published synchronization permissive, but M4.5 performs the authoritative close check and state transition.
6. Turbine-speed raise/lower and generator-load raise/lower are typed operator intents only. The UI defines neither physical increments nor controller algorithms; later runtime coordination maps them to validated M5.4 setpoint seams.
7. Missing runtime/equipment data is presented as unavailable/empty rather than synthesized.

## Consequences

- Turbine and electrical workspaces can be operationally rich without duplicating plant physics.
- Measurement quality remains visible and distinct from educational diagnostics.
- Synchronization cannot be bypassed by presentation logic.
- Future M6.7 runtime integration can wire command intents without redesigning the panels.
