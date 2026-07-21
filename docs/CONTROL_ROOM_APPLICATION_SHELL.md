# Control-Room Application Shell

M6.1 establishes the production-facing Avalonia shell without moving simulation logic into the UI.

## Boundaries

```text
M5.7 immutable automatic-operation snapshot
        ↓
Application projection
        ↓
ControlRoomSnapshot
        ↓
IControlRoomSnapshotSource
        ↓
Avalonia ViewModels / Views

Operator interaction
        ↓
ControlRoomCommand
        ↓
IControlRoomCommandDispatcher
        ↓
future runtime coordinator
```

The shell never owns reactor, hydraulic, turbine, electrical, controller, protection or alarm physics.

## Workspaces

The M6.1 navigation catalog defines stable top-level workspaces:

- Plant Overview;
- Reactor & Core;
- Primary Circuit;
- Turbine & Secondary Cycle;
- Generator & Electrical;
- Alarms & Events.

M6.1 hosts placeholders only. Detailed instruments and controls begin in M6.2 and domain-specific panels follow in M6.3–M6.6.

## Snapshot-driven presentation

`ControlRoomSnapshot` is intentionally smaller than the M5.7 true-state graph. It contains presentation-level operating status only:

- logical step;
- run-state presentation;
- measured-signal health counts;
- alarm/acknowledgement counts;
- headline reactor/turbine/generator trip state.

`ControlRoomSnapshotProjector` derives this contract from validated M5.7 measured, protection and alarm boundaries. Avalonia does not receive `FullPlantSnapshot`, `PlantState`, rotor state or generator state.

## Command dispatch

Avalonia emits application commands through `IControlRoomCommandDispatcher`. The M6.1 shell exposes run, pause and single-step command seams but does not implement simulation stepping itself. A later runtime coordinator can replace the shell dispatcher without changing view code.

## Performance budget

`ControlRoomPerformanceBudget` defines presentation-only targets:

- maximum presentation refresh rate: 20 Hz;
- maximum visible workspace rows: 250;
- maximum simultaneous live trend series: 12.

These are UI workload budgets only. They never alter the deterministic simulation timestep or physical results.

## Layout

The desktop shell uses a scalable three-column workspace:

- left navigation rail;
- central workspace host;
- right operating/status rail;
- persistent top command bar and bottom architecture/performance status.

Minimum window dimensions preserve usability while larger desktops gain workspace area without changing simulation behavior.

## Explicit non-goals for M6.1

- no detailed instrument widgets;
- no reactor/core panel;
- no primary/secondary mnemonics;
- no production trend renderer;
- no production annunciator panel;
- no UI-side physics or controller calculations;
- no rendering-cadence coupling to simulation time.

## M6.2 extension

M6.1 is validated. M6.2 now populates the shell with a reusable component library while preserving the same snapshot/command boundary. Numeric indicators, meters, lamps, toggle switches, selectors and pushbuttons share the semantic `Normal` / `Warning` / `Trip` / `Unavailable` state contract. The shell gallery is a visual-validation surface only; plant-specific Reactor/Core composition begins in M6.3.

## M6.3 extension

M6.2 is validated. M6.3 fills the Reactor/Core workspace with Application-projected measured/diagnostic presentation records and typed operator command intents while preserving the M6.1 App/Simulation dependency separation.

## M6.7 extension

M6.7 supplies the live Application-level runtime boundary promised by M6.1. `ControlRoomRuntimeCoordinator` now implements both snapshot publication and operator-command dispatch over `IntegratedAutomaticOperationRuntimeEngine`, which delegates every physical step to M5.7. M7.2 now promotes the default desktop composition from `SHELL ONLY` to the exact paused `cold-shutdown-pre-start` v1 session. The physical object graph is created by the Application-layer initial-condition factory through the M7.1 registry/session boundary, never inside Avalonia.
