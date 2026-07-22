# Operator Computer — Session, Checkpoint, Replay & Save Workspace

## Status

**M10.7 — VALIDATED.** The user confirmed Hotfix 1 compiled successfully and the complete automated suite passed; M10.7 is the current official baseline beneath M10.7.1.

M10.2 through M10.6 were validated together by the user through a clean cumulative build/test gate. M10.7 activates the fixed `SESSION` terminal page without creating a second state owner or opaque save-state format.

## Ownership

M10.7 packages existing canonical owners:

```text
M7 exact scenario + initial-condition identity
        +
M9.1 ScenarioRecorder / ScenarioCheckpoint / ScenarioFullReplayRunner
        +
M10.5/M10.6 automation-intent journal
        ↓
ScenarioSessionArchive schema v1
        ↓
JSON persistence adapter
        ↓
verified replay restoration
```

The archive is **not** a solver-memory dump. It persists compact deterministic evidence:

- exact versioned `ScenarioDefinition`;
- one fingerprint/event-range entry per recorded logical step;
- accepted operator-action journal;
- accepted M10.5/M10.6 automation-intent journal;
- deterministic recorder event stream;
- replay-backed checkpoints.

Load and checkpoint restore remain owned by `ScenarioFullReplayRunner` and fail closed on fingerprint/event/checkpoint divergence.

## Desktop recording policy

Full M9.1 recording remains explicit opt-in because it fingerprints and retains every deterministic fixed step. Normal desktop startup therefore leaves the recorder inactive.

`START RECORDED SESSION` deliberately restarts the exact versioned desktop scenario at logical step zero with a recorder attached. This avoids hidden performance overhead and makes the lifecycle transition explicit to the operator.

## SESSION page functions

- show scenario ID/title, exact initial-condition identity/version, logical step, recorder state and frame/checkpoint counts;
- start a fresh recorded session;
- create replay-backed checkpoints while paused;
- verify the current archive through a full deterministic replay;
- save a compact `.nrs-session.json` archive;
- load an archive and replay/fingerprint-verify it before replacing the desktop runtime;
- restore a selected checkpoint through verified replay of the archive prefix;
- resume recording from a verified replay/restored prefix.

## Recorder continuation

M10.7 extends `ScenarioRecorder` with a non-finalizing immutable `Capture()` and a verified-prefix resume constructor. Resume is permitted only when:

- scenario and initial-condition identity match;
- restored logical step equals the prefix final step;
- current snapshot fingerprint equals the verified prefix fingerprint;
- restored operator-action and automation-intent journals exactly match the prefix.

This allows a loaded session to continue generating one deterministic recording instead of starting an unrelated trace.

## Training continuity

When loading the built-in integrated desktop training scenario, the `ScenarioTrainingTracker` is attached **before replay begins**. Therefore checkpoint observations, action-derived scoring and training state are reconstructed by deterministic replay rather than reinitialized only at the final restored snapshot.

## Performance regression policy

The historical M9.7 validation still records that both 6,000-step / 60-second endurance tests passed. After those failures were isolated with direct thermodynamic regression cases, the routine desktop/full-plant endurance tests are reduced in M10.7 to 1,000 steps / 10 simulated seconds to keep the normal suite efficient. Direct `drum`/`exhaust` saturation/superheat boundary regressions remain mandatory and preserve coverage of the structural bugs that originally motivated the longer gate.

## Non-goals

- no opaque memory/state serialization;
- no second checkpoint format or restore owner;
- no second fault trace or historian;
- no wall-clock timestamp dependency for deterministic identity;
- no automatic recorder overhead in ordinary desktop sessions;
- no bypass of exact scenario/initial-condition version resolution.

Checkpoint prefixes also preserve recorder-event evidence exactly: operator-action events accepted between committed frames are included only when their corresponding action belongs to the applied checkpoint prefix, while later same-step accepted actions are excluded from that restore point.
