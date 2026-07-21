# ADR 0056 — Turbine startup lineup remains versioned and governing control uses the existing seam

- Status: Accepted / validated with M7.4
- Date: 2026-07-21

## Context

M7.3 ends with reactor operation stabilized at low power while steam admission and the grid remain isolated. M7.4 must introduce heat-up, usable steam conditions and turbine startup without creating a new scenario/UI physical owner.

The validated M5.4 architecture deliberately gives normal turbine governing ownership only over control/admission command seams. Stop valves remain associated with trip/isolation semantics under M5.5. The current operator-intent surface therefore has no independent normal `open stop valve` command.

## Decision

1. M7.4 introduces exact initial condition `low-power-steam-raising` v1.
2. The new factory reuses the canonical M7.2 construction path and varies only explicit versioned initial-condition parameters: low-power critical kinetics, warm primary/steam temperature, main circulation established and a turbine-startup lineup.
3. The startup lineup records stop/admission availability as initial-condition state while keeping the governing control valve closed. It does not create a second stop-valve controller or a UI mutation path.
4. Turbine roll and acceleration use only `TurbineSpeedRaise` / `TurbineSpeedLower`, which already traverse the Application runtime adapter into the validated M5.4 speed-admission controller.
5. M7.4 guidance/checks observe immutable `ControlRoomSnapshot` data only. They never force temperature, pressure, inventory, valve position, rotor speed or power.
6. Generator-breaker close and generator-load raise/lower remain excluded by scenario permissions. Synchronization and loading are M7.5 ownership.
7. Missing measured generator electrical output remains unavailable; M7.4 does not fabricate it. Generator isolation is established from the published breaker state, and any available measured output is checked only when present.

## Consequences

- M7.4 can exercise the validated turbine governing path without violating M5.4/M5.5 ownership boundaries.
- Startup remains deterministic and replayable from an exact versioned initial condition.
- Grid connection cannot occur accidentally during M7.4 because breaker-close and load commands fail closed at the scenario boundary.
- A future explicit normal stop-valve operator-control seam would require a separately designed lower-layer command contract rather than an M7 scenario shortcut.
