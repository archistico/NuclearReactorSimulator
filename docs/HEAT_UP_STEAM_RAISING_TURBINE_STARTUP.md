# Heat-Up, Steam Raising & Turbine Startup

M7.4 extends the validated M7.1–M7.3 operations framework with the first turbine-startup training flow.

## Exact initial condition

The built-in scenario pins:

```text
InitialConditionId: low-power-steam-raising
Version:            1
```

`HeatUpTurbineStartupInitialConditionFactory` reuses the canonical M7.2 construction path. It does not clone or privately own reactor, thermofluid, turbine, electrical, control, protection or alarm physics.

The v1 recipe starts with a warm low-power critical condition suitable for startup training:

- deterministic low-power point-kinetics population calibrated to approximately 5 MWth;
- regulating rod at the zero-worth midpoint;
- main circulation running;
- warm primary/steam inventory initialized at 120 °C through the validated water/steam closure;
- turbine rotor stopped;
- generator breaker open;
- startup steam lineup available with stop/admission valves open and governing control valve closed.

The lineup is explicit versioned initial-condition data. It is not a new UI-owned stop-valve model.

## Operator command boundary

M7.4 permits:

- rod INSERT / HOLD / WITHDRAW;
- reactor SCRAM and protection reset;
- MCP start/stop;
- turbine speed raise/lower;
- turbine/generator trip safety actions;
- generator breaker OPEN;
- alarm ACK/reset actions.

M7.4 deliberately rejects:

- generator breaker CLOSE;
- generator load RAISE/LOWER.

Those actions remain M7.5 ownership.

## Turbine roll

`TurbineSpeedRaise` changes only the canonical M5.4 turbine-speed controller setpoint through `IntegratedAutomaticOperationRuntimeEngine`. The existing controller/actuator path commands the governing control valve; M7.4 does not set valve position or rotor speed directly.

The versioned startup lineup keeps the governing valve closed at load time. A speed-raise command therefore produces an observable control-valve opening through the validated seam before turbine acceleration develops from the canonical steam-path physics.

## Observational guidance

`HeatUpTurbineStartupChecklistEvaluator` reads only `ControlRoomSnapshot` and checks:

- measured-signal health;
- protection clear;
- main circulation running;
- controlled reactor heating-power envelope;
- steam-drum pressure and inventory;
- startup steam lineup;
- turbine stopped/rolling/warm-up/near-synchronous speed bands;
- generator breaker isolation;
- unloaded/deferred electrical connection without inventing unavailable measurements.

The checks never alter physical state or force an acceptance result.

## M7.5 handoff

M7.4 completes at the training boundary where the turbine is near synchronous speed while:

- protection remains clear;
- generator breaker remains open;
- electrical loading remains deferred;
- synchronization and breaker closure have not begun.

M7.5 owns synchronization, breaker closure and initial load increase.
