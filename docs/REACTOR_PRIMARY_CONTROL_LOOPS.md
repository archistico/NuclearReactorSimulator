# Reactor & Primary-System Control Loops

M5.3 wires the generic M5.2 measured-signal/controller/actuator primitives to concrete reactor and primary-system owners without bypassing the validated M2/M3/M4 physics boundaries.

## Closed control path

```text
FullPlantSnapshot true state
        ↓ M5.1 instruments only
MeasuredSignalFrame
        ↓
M5.2 P/PI/PID controllers
        ↓
ActuatorCommandFrame
        ├── rod/group motion command
        └── main-circulation-pump speed/run command
        ↓
M5.3 plant-specific adapters
        ├── existing M2 ControlRodSystemSolver
        └── canonical PlantState PumpState replacement
        ↓
existing M2 point kinetics + fission-power scaling
        ↓
existing M4.7 FullPlantSolver
```

Controllers still consume measured signals only. M5.3 is the first layer allowed to bind typed actuator commands to specific reactor/primary physical owners.

## Reactor-power / rod regulation

A `ReactorPowerRodRegulation` loop must:

- consume an instrument channel whose semantic source is `plant/reactor/thermal-power`;
- target a canonical M2 control rod or control-rod group;
- use the existing `ControlRodSystemSolver` for motion and travel-rate limits;
- use the existing `ControlRodReactivitySolver` and `PointKineticsSolver`; and
- derive fission thermal power through the existing M2 fission-power calibration.

M5.3 does **not** implement a synthetic relation such as `PID output -> MW`.

### Committed-state ordering

The reactivity used by point kinetics during a timestep comes from the **committed** rod positions plus an explicit non-rod reactivity input. Controller commands generated in that timestep advance rod positions for the next committed state.

```text
committed rods
    ↓ rod reactivity
+ explicit non-rod reactivity
    ↓
point kinetics over dt
    ↓
candidate neutron population
    ↓
fission-power calibration
    ↓
effective M3 total-fission-power input

measured power -> controller -> rod command -> candidate rods
                                      ↓
                           affects next committed step
```

This preserves the existing rule that physical solvers read one committed state per step and avoids an algebraic controller/reactor loop inside a single timestep.

## Non-rod reactivity seam

`ReactorPrimaryControlInputs.NonRodReactivity` is explicit. It is the aggregation seam for already validated contributions such as temperature feedback, void feedback, xenon and other non-rod terms when those are composed at a higher level.

M5.3 does not hide those contributions inside the controller implementation and does not silently assume they are zero.

## Main-circulation support

M5.3 adds stable semantic instrumentation sources for each canonical main-circulation loop:

- `main-circulation-loop/{loopId}/total-pump-flow` in kg/s;
- `main-circulation-loop/{loopId}/header-pressure-rise` in Pa.

A main-circulation control loop may regulate either total pump flow or header pressure rise. Its actuator must target a pump already owned by the canonical `MainCirculationSystemDefinition`.

The command adapter replaces only that pump's operational `PumpState` before the existing full-plant step. Pump hydraulic balances are still solved exactly once by `PlantNetworkOrchestrator`.

## State ownership

M5.3 introduces no duplicate fluid or thermal inventory.

- `FullPlantState` remains the M4 thermofluid/mechanical/electrical state envelope.
- `ControlAndActuatorState` remains algorithm/command memory.
- `ControlRodSystemState` remains the existing M2 authoritative rod state.
- `PointKineticsState` remains the existing M2 authoritative global-neutronics state.
- main-circulation pump physical state remains in canonical `PlantState`.

`ReactorPrimaryControlState` is only the M5.3 composition envelope over those existing control/neutronic owners.

## Effective fission-power input

`ReactorPrimaryControlledFullPlantSolver` rewrites only the `IntegratedPrimaryCircuitInputs.TotalFissionThermalPower` seam with the value produced by validated point kinetics and fission-power scaling. All other nested M4 inputs are preserved.

The M3 spatial/core-zone/channel deposition logic remains authoritative downstream; M5.3 does not apply the M2 heat-deposition list a second time.

## Diagnostics

`ReactorPrimaryControlSnapshot` exposes:

- generic controller/actuator diagnostics from M5.2;
- committed and candidate control-rod states;
- committed and candidate rod-reactivity breakdowns;
- explicit non-rod reactivity;
- total reactivity used by point kinetics;
- point-kinetics snapshot;
- fission-power snapshot;
- per-loop setpoint, measured value, controller output, saturation and target identity.

## Downstream composition

M5.3 remains authoritative only for reactor-power/rod and primary-circulation control. M5.4 composes downstream turbine speed/load, steam-pressure, drum/feedwater and hotwell/condensate loops over separate canonical valve/pump targets. Trips, SCRAM, permissives and interlocks are now supplied by the M5.5 protection layer through explicit override arbitration.
