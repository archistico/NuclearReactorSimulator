# Recorder, Checkpoints & Full Replay

## Purpose

M9.1 adds a deterministic evidence/reconstruction layer above the validated M0 command-trace primitive and M7/M8 scenario runtime. It does not own plant physics.

## Recording model

`ScenarioRecorder` must be attached to a fresh loaded `ScenarioSession`. It immediately captures the initial `ControlRoomSnapshot`, then subscribes to `ControlRoomRuntimeCoordinator.DeterministicStepCompleted` so every fixed step is recorded even when accelerated execution publishes presentation snapshots only every N steps.

A completed `ScenarioRecording` contains:

- exact scenario id;
- exact `InitialConditionReference` id/version;
- one contiguous frame per logical step including the initial frame;
- retained immutable `ControlRoomSnapshot` presentation state for analysis;
- deterministic snapshot fingerprint per frame;
- accepted typed operator actions from the existing M7.7 journal;
- M10.5/M10.6 semantic plant-control-authority and supervisory-objective intents from a separate deterministic automation-intent journal;
- a monotonic event stream for operator actions, alarm events, fault lifecycle transitions and protection-trip transitions;
- zero or more versioned checkpoints.

The recorder observes only. It never queues commands, changes stepping, mutates state or influences fault/protection decisions.

## Operator action timing

`ScenarioOperatorActionJournal` records an accepted action at the currently committed logical step N. The queued physical/operator command affects the next deterministic step N+1. M9.1 therefore converts journal actions into replay trace entries at `LogicalStep + 1` while preserving accepted-action sequence order for multiple commands targeting the same step.

A recording cannot be completed while an accepted action has not yet reached its application step.

## Fingerprint v1

`ControlRoomSnapshotFingerprint.AlgorithmId` is:

`sha256-control-room-snapshot-v1`

The complete immutable control-room snapshot is serialized deterministically and hashed with SHA-256 after normalizing `ControlRoomRunState` to `Paused`.

Run/pause is a host execution concern; normalizing it prevents a session originally executed through `Run + AdvanceRunning` from diverging when replayed through fixed paused single steps. Logical step, measured state, panels, alarms, protection and fault lifecycle remain part of the fingerprint.

Changing the fingerprint contract in the future requires a new algorithm id rather than silently redefining v1.

## Versioned checkpoints

A `ScenarioCheckpoint` schema v1 contains:

- `CheckpointId`;
- `SchemaVersion`;
- exact `ScenarioId`;
- exact versioned initial-condition reference;
- `LogicalStep`;
- `LastAppliedOperatorActionSequence`;
- fingerprint algorithm id;
- expected snapshot fingerprint.

A checkpoint deliberately does **not** serialize private solver/runtime object graphs. Seeking reconstructs the exact session from its immutable versioned seed, replays the required accepted command prefix and verifies the checkpoint fingerprint.

This is slower than an opaque memory dump but preserves determinism, schema clarity and state ownership. A future optimized state-snapshot format would require its own explicitly versioned contract and restore invariants.

## Full replay

`ScenarioFullReplayRunner.ReplayAndVerify`:

1. validates scenario and initial-condition identity;
2. loads a fresh paused session through `ScenarioSessionFactory`;
3. replays accepted operator actions and semantic M10.5/M10.6 automation intents at their exact next-step boundary;
4. executes one deterministic fixed step at a time;
5. compares each actual frame fingerprint with the recording immediately;
6. fails closed on the first mismatch with `ScenarioReplayDivergenceException`;
7. after final state verification, compares the complete recorder event stream.

M8 faults are **not** duplicated into another replay trace. Their schedule/lifecycle is reconstructed from the same `ScenarioDefinition`, exact initial state and committed snapshots, preserving M8.1 ownership.

## Seek

`SeekAndVerify` replays only through the selected checkpoint logical step using exactly the action prefix declared by the checkpoint, then validates its fingerprint before returning the reconstructed session.

A checkpoint with mismatched scenario/version, unsupported schema/fingerprint algorithm, inconsistent action prefix or incorrect fingerprint fails closed.


## M10.5/M10.6 replay extension

M10.5/M10.6 automation intents remain separate from `ControlRoomCommandKind`. `ScenarioAutomationIntentJournal` records accepted authority/objective requests as semantic replay inputs. Full replay and checkpoint seek reapply the applicable intents at `N + 1`, alongside the existing operator-action prefix, before stepping the deterministic runtime. The versioned `ControlRoomSnapshot` fingerprint contract remains unchanged; automation intent equality is verified separately by full replay.
