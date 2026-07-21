# Integrated Automatic Operation

M5.7 closes the M5 architecture gate by composing the validated M5.1–M5.6 layers into one deterministic multi-step automatic-operation boundary.

## State and ownership

`IntegratedAutomaticOperationState` is only a canonical envelope over existing owners:

```text
FullPlantState                 -> M4.7 physical truth
InstrumentationState           -> M5.1 sensor/filter memory
MeasuredSignalFrame            -> committed controller/protection observation
ReactorPrimaryControlState     -> M5.3 algorithm + M2 rod/kinetics state
TurbineSecondaryControlState   -> M5.4 algorithm/command memory
ProtectionSystemState          -> M5.5 protection latches
AlarmSystemState               -> M5.6 annunciator memory
```

It introduces no new physical inventory and no additional physical integrator.

## Logical-step ordering

Each M5.7 step has an explicit one-step observation/control order:

```text
committed MeasuredSignalFrame
        |
        +--> M5.3/M5.4 normal control
        +--> M5.5 protection/interlocks
        +--> M5.6 alarm observation of the resulting protection decision
        |
        v
protection arbitration
        |
        v
ONE M4.7 full-plant physical step
        |
        v
candidate immutable FullPlantSnapshot
        |
        v
M5.1 instrumentation/filter step
        |
        v
candidate MeasuredSignalFrame for the NEXT logical step
```

Controllers and protection therefore never read a candidate true state retroactively. The same committed measured frame drives all current-step M5 decisions, avoiding hidden algebraic loops.

## Explicit verification phases

`AutomaticOperationVerificationPlan` is a headless verification construct, not a scenario scripting engine. It contains explicit ordered phases. Each phase supplies one immutable `IntegratedAutomaticOperationInputs` bundle and a finite step count.

Changing a bundle between phases can represent:

- a reference automatic hold;
- an operator setpoint change;
- an explicit plant-boundary disturbance;
- an explicit sensor fault input;
- a manual protection command used in the deterministic trip/interlock matrix.

The runner never schedules hidden events and never forces a physical outcome.

## Tracking and gate criteria

A phase may define measured-signal tracking targets. Acceptance is evaluated only against the final measured frame of that phase. Missing/invalid measurements fail the tracking target rather than falling back to true state.

Global gate criteria observe:

- maximum absolute full-plant mass-closure residual;
- maximum absolute full energy-path closure residual;
- maximum invalid measured-signal count;
- maximum unacknowledged alarm count.

Each phase also declares the expected final latched protection actions and active interlocks. The verification matrix therefore checks normal-operation phases and intentional protection cases with the same deterministic runtime path.

Criteria are observational only. Failure never trims inventories, retunes controllers, clears alarms, resets protection or modifies physical state.

## M5 gate

M5.7 is complete when local build/test validation confirms:

1. deterministic replay of the integrated M5.1–M5.6 path;
2. committed-measurement ordering and next-step instrumentation publication;
3. stable/reference automatic-operation verification without hidden corrections;
4. explicit setpoint/disturbance phase support;
5. deterministic protection/interlock expectation matrix;
6. alarm/annunciator processing remains observational;
7. mass/energy audit closure remains visible through the full automatic-control stack.

After validation, the M5 gate is complete and M6 may build the operator control room on immutable measured/control/protection/alarm snapshots and application commands.
