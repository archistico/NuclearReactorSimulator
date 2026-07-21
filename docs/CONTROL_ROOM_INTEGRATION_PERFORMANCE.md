# Control-Room Integration & Performance Baseline

M6.7 closes the control-room phase by wiring the validated M5.7 automatic-operation boundary to the M6 presentation and command seams without moving physics into Avalonia.

## Runtime ownership

`IntegratedAutomaticOperationRuntimeEngine` owns one initialized M5.7 runtime session: committed automatic-operation state, immutable persistent inputs, the last integrated snapshot and deterministic fixed `deltaTime`.

`ControlRoomRuntimeCoordinator` implements both `IControlRoomSnapshotSource` and `IControlRoomCommandDispatcher`. It owns only run/pause/single-step semantics and presentation publication cadence.

```text
Avalonia
  ↓ typed ControlRoomCommand
ControlRoomRuntimeCoordinator
  ↓
IntegratedAutomaticOperationRuntimeEngine
  ↓
IntegratedAutomaticOperationSolver (M5.7)
  ↓
ONE deterministic plant/control/protection/alarm step
  ↓
ControlRoomSnapshotProjector
  ↓
immutable ControlRoomSnapshot
```

## Command translation

M6.7 translates existing typed intents into validated runtime seams:

- SCRAM / turbine trip / generator trip / protection reset → one-step M5.5 inputs;
- alarm ACK/reset → one-step M5.6 inputs;
- breaker open/close → one-step M4.5 generator inputs;
- rod and MCP commands → canonical M5.3 controller/actuator bindings;
- turbine-speed and generator-load raise/lower → M5.4 controller setpoint changes.

Transient commands are cleared after exactly one deterministic step. Controller mode/setpoint changes are persistent immutable input updates.

## Accelerated execution

`AdvanceRunning(stepCount, publicationStride)` executes every requested logical step exactly once. `publicationStride` changes only how many presentation snapshots are emitted.

A cooperative `ControlRoomRuntimeExecutionBudget` bounds one batch to 256 simulation steps by default. Hosts can run repeated batches while yielding between them. The batch size and publication stride never change fixed simulation `deltaTime` or solver ordering.

M7.7 adds a separate Application-only `DeterministicStepCompleted` observation event on the coordinator. It is raised for every executed fixed step, including steps omitted by sparse presentation publication, so training/evaluation history cannot depend on UI refresh cadence. The event is observational and does not change `Current`, solver inputs or publication semantics.

## Initial conditions

M6.7 intentionally did not invent a default live plant state. Validated M7.1 owns exact-version initial-condition/scenario/session creation. M7.2 now supplies `cold-shutdown-pre-start` v1 and the desktop loads that exact runtime paused; pacing/publication semantics remain those of the M6.7 coordinator.

## Gate invariants

- no Avalonia reference to Simulation namespaces;
- no rendering-cadence dependency in physical results;
- one-shot commands are never replayed accidentally;
- presentation publication can be sparse while all physical steps still execute;
- unsupported/non-canonical command targets fail closed;
- no UI-side protection, synchronization or physics reimplementation.
