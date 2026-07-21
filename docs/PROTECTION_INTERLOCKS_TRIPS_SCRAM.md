# Protection, Interlocks, Trips & SCRAM

M5.5 introduces the first dedicated plant-protection layer above the validated M5.1 measured-signal boundary and below the authoritative M4.7 physical full-plant step.

## Architectural boundary

```text
FullPlantSnapshot true state
        ↓
M5.1 Instrumentation
        ↓
MeasuredSignalFrame
        ├── M5.3/M5.4 normal controllers
        └── M5.5 protection
                 ├── latching trip functions
                 ├── non-latching interlocks
                 └── reset permissives
        ↓
explicit command arbitration
        ↓
canonical physical owners
        ↓
ONE FullPlantSolver.Step(...)
```

Protection never traverses `FullPlantSnapshot` directly. It consumes the same canonical `MeasuredSignalFrame` as normal controllers so instrument lag, invalidity, range behavior and deterministic sensor faults remain physically relevant to protection decisions.

## Latching trip functions

`ProtectionFunctionDefinition` binds one measured channel to:

- a high or low trip comparison;
- a trip threshold;
- a separate reset threshold for hysteresis;
- one or more actions: reactor SCRAM, turbine trip and generator trip;
- an explicit fail-closed policy for invalid/unavailable measurements.

A trip function latches after activation. Returning the measured variable to a safe range does not clear the latch by itself.

## Reset semantics

A reset request is accepted only when:

1. every active trip function is no longer at its trip condition;
2. every function is beyond its configured safe reset threshold;
3. every configured `ProtectionPermissiveDefinition` is satisfied with a valid measurement.

Rejected reset attempts remain observable and do not alter protection state.

## Non-latching interlocks

`ProtectionInterlockDefinition` provides deterministic command inhibits without latching trip state. M5.5 supports:

- block control-rod withdrawal;
- block turbine control/admission-valve opening;
- block generator-breaker close commands.

An interlock clears automatically when its measured condition clears. It does not masquerade as a latched trip.

## Protection arbitration

Protection has higher command authority than normal M5.3/M5.4 process control.

### Reactor SCRAM

SCRAM overrides normal rod commands with `Insert` commands for every canonical M2 control rod. Current-step kinetics still uses committed rod position, preserving the committed-state rule. Rod motion begins during the step and affects subsequent committed-state reactivity through the already validated M2 rod-worth and point-kinetics chain.

### Turbine trip

Turbine trip:

- forces every canonical M4.1 stop valve closed before turbine-flow projection;
- asserts the existing M4.2 `TurbineRotorInput.TripCommand` seam;
- therefore forces effective turbine admission to zero without creating a second steam/hydraulic model.

### Generator trip

Generator trip suppresses breaker-close commands and asserts the existing M4.5 breaker-open command seam for every canonical generator.

## State ownership

`ProtectionSystemState` contains only logical latches and explicit manual-trip memory. It does not own:

- rod position;
- valve position;
- pump state;
- turbine rotor state;
- breaker physical/electrical state;
- alarms or annunciator acknowledgement.

Those remain with the previously validated physical domains. M5.6 now owns alarm/annunciator presentation semantics through a separate observational state boundary; M5.5 remains the sole physical protection owner.

## Determinism

M5.5 contains no wall-clock or random behavior. Trip/interlock evaluation depends only on:

- committed protection latch state;
- one immutable measured-signal frame;
- explicit manual trip/reset inputs.

Scenario scheduling and fault timing remain later responsibilities.
