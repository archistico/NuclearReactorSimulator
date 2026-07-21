# Instrumentation & Control Faults

M8.3 adds concrete deterministic instrumentation/control effects on top of the validated M8.1 lifecycle scheduler. It reuses M5.1–M5.5 seams rather than creating a scenario-side signal, controller or protection model.

## Sensor fault types

| Fault type ID | Effect |
|---|---|
| `instrumentation.sensor-bias` | Adds finite `biasEngineeringUnits` through the existing M5.1 bias mode; signal remains valid but suspect. |
| `instrumentation.sensor-freeze` | Holds the last committed measured output; validity becomes invalid and quality suspect. |
| `instrumentation.sensor-failed-low` | Drives the channel to its configured measurement-range minimum with M5.1 bad/invalid semantics. |
| `instrumentation.sensor-failed-high` | Drives the channel to its configured measurement-range maximum with M5.1 bad/invalid semantics. |
| `instrumentation.sensor-unavailable` | Publishes no engineering value and marks quality unavailable. |

Only the selected `InstrumentationInputs` entry changes. `InstrumentationSolver` still owns filtering, scaling, validity, quality and freeze memory. Controllers, protection and alarms see the same faulted `MeasuredSignalFrame`; no consumer silently falls back to true state.

## Controller-output fault types

- `control.controller-output-freeze`
- `control.controller-output-fail-low`
- `control.controller-output-fail-high`

These effects temporarily replace the targeted canonical controller's per-step input with bounded manual output. Freeze captures the committed `ControllerChannelState.LastOutput` at activation. Fail-low/high use the controller's canonical `OutputRange` limits.

The physical consequence still follows:

```text
fault overlay -> ControllerInput -> ControllerSystemSolver -> ActuatorSystemSolver
             -> typed valve/pump/rod command -> existing physical owner
```

M8.3 never writes the physical target directly.

## Actuator-command fault types

- `control.actuator-command-freeze`
- `control.actuator-command-fail-low`
- `control.actuator-command-fail-high`

M5.2 actuator objects are command translators rather than independent physical-state owners. M8.3 therefore resolves the canonical actuator to its controller command path and applies the temporary override there. This is permitted only when that controller drives exactly one actuator. If a future topology shares one controller across multiple actuators, an actuator-specific fault fails closed until an explicit fan-out fault model is defined.

## Protection and interlock diagnostics

Protection/interlock logic is not faulted directly. Sensor failures enter M5.1 and become the committed measured frame consumed by M5.5 on the normal deterministic ordering boundary. For example, the built-in `instrumentation-protection-fail-safe-diagnostic` scenario makes the pressure channel unavailable; the existing `TripOnInvalidMeasurement` policy then produces the protection response on the following logical step.

This preserves the causal chain:

```text
scenario fault activation
        -> M5.1 sensor fault input
        -> candidate measured frame
        -> committed next-step measured frame
        -> M5.2/M5.3/M5.4 control + M5.5 protection/interlocks
        -> canonical physical commands
```

## Built-in scenario pack

`InstrumentationControlFaultScenarioPack.Demonstration` reuses `stable-low-load-parallel-operation` v1 and schedules non-overlapping examples of all M8.3 sensor/controller/actuator-command fault types.

`InstrumentationControlFaultScenarioPack.ProtectionDiagnostic` isolates invalid-measurement fail-safe behavior on the pressure protection channel.

Both are deterministic scenario definitions. They do not force expected outcomes or modify physics to satisfy training objectives.

## Limits

M8.3 is not an electronics reliability simulator. It does not add stochastic noise, random drift, relay hardware failure probabilities or arbitrary protection-logic corruption. Transient equipment/system scenarios remain M8.4+, while larger leak/LOCA and electrical-loss/SBO classes remain M8.5/M8.6.
