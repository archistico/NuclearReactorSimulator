# Turbine, Steam & Feedwater Control Loops

M5.4 binds the generic M5.1 measured-signal and M5.2 controller/actuator layers to the secondary-cycle physical owners already validated in M4.

## Control path

```text
FullPlantSnapshot truth
        ↓
M5.1 instrumentation
        ↓
MeasuredSignalFrame
        ↓
M5.2 P / PI / PID
        ↓
typed valve / pump commands
        ↓
M5.4 plant-specific adapters
        ↓
canonical ValveState / PumpState + automatic turbine-flow seam
        ↓
ONE M4.7 full-plant physical step
```

Controllers never traverse `FullPlantSnapshot` directly.

## Supported loop roles

`TurbineSecondaryControlLoopKind` provides five semantic roles:

- `TurbineSpeedAdmission`: measured rotor speed controls a normal control/admission valve;
- `TurbineLoadAdmission`: measured generator electrical output controls a normal control/admission valve;
- `SteamPressureAdmission`: measured source-drum pressure controls a normal control/admission valve;
- `SteamDrumLevelFeedwater`: measured drum level controls the canonical M4.4 feedwater pump;
- `HotwellInventoryCondensate`: measured condenser hotwell inventory controls the canonical M4.4 condensate pump.

Loop definitions validate the expected measured source and physical target. Stop valves are not valid normal governor targets; they are reserved for isolation/trip behavior and are now owned by the M5.5 protection layer.

## Turbine admission and M4.2 stage flow

M4.2 intentionally exposed stage-group mass flow as a manual command seam. M5.4 closes that seam without creating another hydraulic solver.

Before the one physical full-plant step, M5.4:

1. applies controller valve commands to canonical `ValveState`;
2. evaluates the already validated stateless `ValveFlowSolver` against the same committed fluid-node state;
3. takes the non-negative limiting flow through stop, control and admission valves in series;
4. rewrites the existing M4.2 stage-group mass-flow input with that projected admission-path flow.

The plant-network orchestrator remains the only hydraulic/inventory integrator. The projection only couples the already existing M4.1 valve path to the already existing M4.2 turbine-demand seam.

## Feedwater and condensate control

Drum-level and hotwell-inventory loops replace only canonical pump operating commands before the physical step:

```text
steam-drum level → controller → feedwater PumpState
hotwell mass     → controller → condensate PumpState
```

Pump flow, pressure boost, hydraulic power, fluid balances and inventory integration remain owned by the existing plant-network orchestration.

M5.4 adds the stable measured source:

```text
condenser/{condenserId}/hotwell-mass   [kg]
```

## M5.3 + M5.4 composition

`TurbineSecondaryControlledFullPlantSolver` composes reactor/primary and secondary controls over one measured frame:

```text
same MeasuredSignalFrame
        ├── M5.3 reactor/primary control
        └── M5.4 turbine/secondary control
                 ↓
commands applied to disjoint physical targets
                 ↓
M5.3 validated kinetics rewrites total fission power
M5.4 admission projection rewrites turbine stage flow
                 ↓
ONE M4.7 FullPlantSolver.Step(...)
```

The composition requires both control systems to share the same canonical instrumentation definition and rejects duplicate physical actuator ownership.

## Deliberately deferred

M5.4 does not implement:

- stop-valve or turbine trip logic;
- reactor SCRAM;
- generator protection;
- automatic permissives/interlocks;
- protection override arbitration;
- alarm/annunciator semantics.

Those protection actions belong to M5.5; alarm/annunciator presentation belongs to M5.6.
