# Operational Challenge Replay, Checkpoint & Determinism Closure

## Scope

M10.9.6.5 closes the Operational Challenge & Energy-Demand Framework without adding a new challenge, user interface, plant command authority, physical model, protection rule or exact-version plant identity.

The challenge framework remains derived Application evidence. Canonical physical/session replay ownership stays with the existing M9.1/M10.7 `ScenarioRecording`, `ScenarioSessionArchive`, `ScenarioCheckpoint` and `ScenarioFullReplayRunner` contracts.

## Derivable challenge state

M10.9.6 does not persist an opaque challenge-state dump. `OperationalChallengeRecordingProjector` reconstructs one exact challenge pack from:

- the exact pack/scenario/initial-condition identities;
- contiguous immutable recorder frames;
- accepted operator actions in deterministic sequence;
- the validated M10.9.6.1 condition evaluator/lifecycle contract;
- the validated M10.9.6.2 external-demand profile;
- the validated M10.9.6.3 exact scoring policy.

The internal replay adapter feeds recorded action acceptance and deterministic frames through the same `ScenarioChallengeTracker` used by live challenge observation. A recording identity mismatch fails closed.

## Terminal lifecycle replay-step alignment

The live M10.9.6.1 tracker intentionally stops re-evaluating a challenge after it reaches `Completed`, `Failed` or `Cancelled`. Its terminal snapshot therefore preserves the true terminal logical step even if the canonical recorder later captures additional plant frames.

M10.9.6.5 must not weaken the strict M10.9.6.2 rule that external-demand evidence and plant snapshot come from the same logical step. For replay projection only, a terminal lifecycle snapshot may therefore be represented *as of* a later recorder frame by advancing its current `LogicalStep` while preserving the exact terminal state, activation step, `TerminalLogicalStep`, target/deadline boundaries, observations and transitions. Non-terminal mismatches, backward alignment or a terminal snapshot whose frozen logical step does not equal its `TerminalLogicalStep` fail closed.

The same derived final-step view is used for score projection and the M10.9.6.5 validation fingerprint. This is derived replay evidence, not a change to the authoritative live lifecycle transition semantics.

## Checkpoint continuation

A checkpoint is still an M9.1/M10.7 replay-backed prefix. Challenge state at a checkpoint is reconstructed from that verified prefix; after `ScenarioFullReplayRunner.SeekAndVerify`, `ScenarioRecorder` may resume from the verified recording prefix. Projecting the extended recording must produce the same final challenge fingerprint as uninterrupted execution of the same logical trace.

No challenge-specific checkpoint schema is introduced.

## Demand and requested load

Every reconstructed frame keeps three independent values:

```text
EXTERNAL GRID DEMAND
!= GENERATOR REQUESTED LOAD
!= ACTUAL ELECTRICAL OUTPUT
```

Demand/error remains observational. Replay projection has no dispatcher or control-authority seam and cannot write requested generator load.

## Score evidence projection

M10.9.6.5 binds the previously documented pack evidence sources to deterministic score evidence:

- safety/protection discipline uses challenge-authored critical safety failure observations;
- procedure uses authored required/completion observations and the authored controlled-shutdown emergency-action failure;
- stability uses authored completion/stability observations;
- demand tracking uses paired external-demand/actual-output samples with deterministic mean absolute error normalization;
- logical-time efficiency uses terminal logical step relative to the authored target window and remains unavailable before terminal completion/failure.

Missing required evidence remains unavailable and therefore cannot silently pass under the M10.9.6.3 scoring contract. Generic protection trips are not globally score failures; only challenge-authored evidence is classified. M10.9.6.5 does not add demand-schedule action windows, timing penalties or any other new scoring criterion: the external-demand schedule remains observational evidence unless a future exact challenge/scoring-policy version explicitly authors a new rule.

## Deterministic fingerprint

`OperationalChallengeReplayFingerprint` (`m10965-challenge-replay-sha256-v1`) hashes only deterministic semantic evidence: lifecycle state/transition IDs and logical steps (not human-readable reason/evidence prose), recorder-frame fingerprints, demand/request/output evidence and numeric score decomposition. It is a validation fingerprint, not a plant/save identity and does not replace `ControlRoomSnapshotFingerprint`.

## Presentation boundary

M10.9.6.5 adds no challenge UI. Objective/demand/progress/score presentation belongs to M10.9.7 Mission & Performance Workstation. Therefore the M10.9.6.5 manual gate reviews replay/closure artifacts and semantic separation; minimum-window and visual score/demand checks remain M10.9.7 work.
