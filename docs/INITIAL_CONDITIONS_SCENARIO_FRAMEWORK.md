# Versioned Initial Conditions & Scenario Framework

## Scope

M7.1 establishes the deterministic ownership boundary for training scenarios and initialized runtime sessions. It deliberately does **not** add a cold-start procedure, fault scheduling, full-state save/checkpoint serialization or objective scoring.

The framework is built around four rules:

1. an initial condition is identified by an immutable `(InitialConditionId, Version)` pair;
2. loading requires an exact registered version — there is no silent fallback to "latest";
3. a scenario declares metadata, objectives and allowed operator actions without forcing physical outcomes;
4. replay is driven only by logical simulation steps and the existing deterministic M0 command-trace primitive.

## Initial-condition ownership

`IVersionedInitialConditionFactory` is the authoritative construction seam for one exact initial-condition version. A factory returns a **fresh** `IControlRoomRuntimeEngine` whose lower-layer physical/control/instrumentation/protection/alarm object graph remains owned by the validated M1–M5 composition.

The scenario layer therefore never reconstructs individual fluid inventories, rod states, controller memories or protection latches itself.

`VersionedInitialConditionRegistry` resolves exact versions and rejects duplicate registrations. Semantic changes to an initial condition require a new version rather than mutating an existing version in place.

M7.1 intentionally treats initial conditions as versioned deterministic construction recipes rather than general arbitrary checkpoint blobs. Rich full-state checkpoints/seek belong to M9.1.

## Scenario schema

A scenario contains:

- stable scenario ID;
- title and description;
- one exact versioned initial-condition reference;
- descriptive objective metadata;
- an explicit set of allowed operator command kinds.

Run, pause and single-step are runtime host controls and are not scenario operator permissions.

The Infrastructure JSON adapter writes canonical schema version `1`. It also retains a deterministic migration from legacy schema `0` to `1`. Migration may reshape metadata, but it must preserve the exact initial-condition ID/version and may never reinterpret a scenario against a newer initial condition.

Unknown future schema versions fail closed.

## Session load/start boundary

`ScenarioSessionFactory.Load(...)`:

1. resolves the exact initial-condition version;
2. creates a fresh runtime engine;
3. creates a `ControlRoomRuntimeCoordinator` in `Paused` state;
4. wraps command dispatch with `ScenarioCommandDispatcher`.

This is the canonical M7.1 initialized-session boundary. Scenario metadata never patches physical state after construction.

`ScenarioCommandDispatcher` forwards host runtime controls, but physical/operator commands must be declared in the scenario whitelist. Disallowed commands fail closed before reaching the runtime engine.

## Deterministic replay boundary

`ScenarioReplayRunner` reuses `SimulationCommandTrace<ControlRoomCommand>` from M0.

For each next logical step:

1. all operator commands scheduled for that step are dispatched in trace order;
2. exactly one paused `SingleStep` is executed;
3. no wall-clock timestamp, frame cadence or UI refresh interval participates in ordering.

A replay always starts by loading a fresh exact-version initial condition. Runtime host commands are forbidden inside replay traces because the replay runner itself owns stepping semantics.

## Deliberate omissions

M7.1 itself does not define operational recipe content. M7.2 now provides:

- the concrete exact-version `cold-shutdown-pre-start` v1 recipe through `ColdShutdownInitialConditionFactory`;
- procedure guidance and objective evaluation are owned by M7.7 as observational Application state over deterministic snapshots/actions;
- deterministic fault activation schedules — M8.1 owns fault scenario inputs;
- general full-state save/load checkpoints or seek — M9.1 owns recorder/checkpoint evolution.

M7.2 now uses this validated seam in production: the desktop composition registers `ColdShutdownInitialConditionFactory` and loads `cold-shutdown-pre-start` v1 paused through `ScenarioSessionFactory`. Avalonia still does not construct or patch the physical object graph. See `COLD_SHUTDOWN_PRESTART.md`.
