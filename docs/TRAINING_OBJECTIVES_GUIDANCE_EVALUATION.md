# Training Objectives, Procedure Guidance & Evaluation

M7.7 adds a deterministic educational layer over the already validated M7 operating scenarios. It is deliberately an **Application-only observational layer**: it observes immutable control-room snapshots and records operator commands that have already passed scenario gating. It cannot advance time, mutate plant state, change controllers, clear protection or force an objective to pass.

## Deterministic observation boundary

`ControlRoomRuntimeCoordinator.DeterministicStepCompleted` is raised once for every fixed simulation step, including steps executed inside accelerated batches that are not published to the UI. Training checkpoint history therefore depends on deterministic simulation progression rather than rendering cadence or wall-clock refresh.

`SnapshotChanged` remains the presentation publication event. M7.7 does not use presentation stride as a time base.

## Accepted operator-action journal

`ScenarioOperatorActionJournal` records only non-host operator actions after `ScenarioCommandDispatcher` has accepted and forwarded them successfully. Each record contains:

- monotonic logical sequence;
- committed logical step at acceptance;
- immutable typed `ControlRoomCommand`.

`Run`, `Pause` and `SingleStep` remain runtime-host controls and are not training actions. Commands rejected by scenario permissions are not recorded.

## Checkpoints and objective scoring

A `ScenarioTrainingPlan` declares:

- historical checkpoints sourced from existing observational checklist semantics;
- deterministic criteria over checkpoint achievement and accepted-action history;
- scored mappings to the scenario's declared objectives;
- optional scoring penalties for declared procedural deviations.

Checkpoint state is monotonic observational memory: the first logical step at which a condition is satisfied is retained. Checkpoints may declare prerequisite checkpoints so later procedural conditions cannot receive credit before the required earlier operating phase has actually been observed. The evaluator never writes back to simulation state.

The M7.7 capstone uses a 100-point scale across four objectives:

1. verify stable low-load handoff — 15 points;
2. deliberate power manoeuvring — 30 points;
3. observe temperature/void feedback and preserve the xenon boundary — 20 points;
4. controlled normal shutdown with post-shutdown circulation — 35 points.

SCRAM, turbine trip and generator trip remain valid safety/protection actions. Their use during this normal-operation exercise is recorded only as a **training deviation penalty**; M7.7 never blocks or redefines their lower-layer physical/protection behavior.

## Optional guidance modes

`TrainingGuidanceMode` is presentation semantics only:

- `Hidden` — step-by-step guidance suppressed;
- `ChecklistOnly` — checkpoints visible without procedural instructions;
- `Guided` — full declared procedure guidance and checkpoints visible.

Switching modes cannot change objective criteria, checkpoint history, accepted actions, score or simulation results.

## Capstone composition

`IntegratedOperationsTrainingProgram` reuses the validated exact initial condition `stable-low-load-parallel-operation` v1 and the validated M7.6 command permissions/procedure guidance. M7.7 therefore introduces no new physical seed and no duplicate reactor/turbine/electrical ownership.

## Boundary to M8

M8.1 owns deterministic fault injection and fault state. M7.7 does not schedule faults, inject failures, add hidden randomness or encode incident outcomes. Future abnormal-scenario evaluation may reuse the M7.7 training framework once M8 supplies explicit deterministic fault inputs.
