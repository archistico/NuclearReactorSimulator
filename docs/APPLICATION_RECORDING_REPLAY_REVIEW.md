# Application Recording, Replay & Mission Timeline Review

## Purpose

This document records the disposition of the post-M10.9.7.3 static review of `NuclearReactorSimulator.Application`. It is a planning/architecture record, not evidence that unimplemented changes are already validated.

The review was performed after the M10.9.7.2 robustness/performance/persistence hardening chain and after the automated M10.9.7.3 Hotfix 1 REV2 gates were green. The current runtime candidate remains unchanged by this document.

## Already-closed findings

The review reconfirmed that earlier M10.9.7.2 fixes are in the right ownership layer:

- terminal challenge logical-step alignment is shared by replay, demand and Mission/Performance projection;
- requested generator load aggregation has one Application owner;
- future protection events are filtered from mission presentation;
- objective title/description come from `ScenarioObjectiveDefinition` rather than challenge aliases;
- recent mission evidence is bounded;
- `ControlRoomCommand.NumericValue` persistence and adapter error contracts are closed in Infrastructure.

## Challenge observation change notifications

`ScenarioChallengeTracker` deliberately preserves the pre-Hotfix-2 observable behavior: active condition observations normally change every deterministic logical step because `ChallengeConditionObservation` contains `LogicalStep` and evidence. Hotfix 2 replaced string-materialized observation fingerprints with a version counter to remove allocation/work; it did **not** redefine `LifecycleChanged` as "condition outcome changed only".

Therefore:

- per-step `LifecycleChanged` while an active observation is refreshed is currently a contract, not a regression;
- M10.9.7.3 does not repaint at 100 Hz solely because of this event because deterministic evidence accumulation, presentation cadence and explicit structural presentation change detection are separate;
- any future semantic split between `ObservationSampled`, `ConditionOutcomeChanged` and lifecycle-transition notifications requires measurement plus a versioned behavioral decision. It belongs to M11.3, not a silent M10 hotfix.

## Mission timeline retention

The M10.9.7.1 `RecentEvents` contract intentionally keeps only the newest 100 combined objective/protection/scoring presentation events. This is sufficient for the live at-a-glance panel, but it is **not** an acceptable owner for the M10.9.7.4 deterministic timeline because a high-volume protection stream can evict sparse mission lifecycle transitions.

M10.9.7.4 must therefore separate two bounded concepts:

### Lifecycle spine

Retain the mission-defining transitions needed to understand the mission narrative, including activation, terminal completion/failure and relevant reset/restart boundaries. This spine has its own explicit bound/retention rule and cannot be evicted merely by protection-event volume.

### Recent operational evidence

Retain bounded protection, scoring, operator-action and other recent evidence separately. This stream may use last-N retention appropriate to drill-down and display.

The timeline presentation merges both sources under one deterministic ordering contract. It does not create a second recorder and does not make presentation state authoritative.

## Control-room snapshot fingerprint v1

`ControlRoomSnapshotFingerprint` currently hashes a deterministic JSON representation of the normalized `ControlRoomSnapshot` under algorithm id:

`sha256-control-room-snapshot-v1`

The documented rule already says that a future contract change requires a new algorithm id, but the repository does not yet have a sufficiently populated golden/schema-anchor fixture that makes accidental drift fail immediately.

Before M10.9.7.4 expands archive/replay-equivalence coverage, add a **fingerprint-v1 golden anchor**:

- construct one deterministic canonical snapshot fixture that populates every fingerprint-relevant scalar and at least one representative element of every fingerprint-relevant collection;
- assert the exact `AlgorithmId`;
- assert the exact expected 64-hex fingerprint;
- ensure presentation-only data intentionally excluded from v1 remains excluded;
- treat a changed golden value as an explicit compatibility decision, not a test-update chore.

If the semantic fingerprint surface genuinely changes, introduce a new algorithm id. Do not silently redefine `sha256-control-room-snapshot-v1`.

## Fingerprint compatibility beyond v1

M11.2 owns multi-version compatibility. If a future `ControlRoomSnapshotFingerprint` v2 is introduced:

- supported historical v1 recordings/checkpoints remain identifiable by algorithm id;
- replay/seek chooses the algorithm declared by persisted evidence rather than assuming the current algorithm only;
- a v1→v2 migration may add metadata/derived evidence but may not rewrite historical v1 expected hashes as if they had always been v2;
- unsupported algorithm ids fail closed with a precise compatibility error.

## Fingerprint hot-path cost

Full-step recording is opt-in, but when enabled it computes a full snapshot fingerprint at deterministic-step cadence. M11.3 must measure before optimizing:

- normalized snapshot materialization;
- JSON serialization allocation/time;
- SHA-256 time;
- hexadecimal formatting/allocation, including whether `Convert.ToHexStringLower` can replace `ToHexString(...).ToLowerInvariant()` without changing the v1 string contract;
- total bytes/frame and recorder throughput at 100 Hz.

Any optimization must preserve exact v1 fingerprints for the golden fixture and representative recordings.

## Recorder collection API

`ScenarioRecorder.Frames`, `Events` and `Checkpoints` currently protect mutability by copying the backing list into a new array/read-only wrapper on property access. Current core call sites do not make this quadratic in practice, but the API is an allocation trap.

M11.3 should replace repeated full copies with a stable read-only view over the private list or an equivalent non-copying read-only surface, while preserving mutation ownership inside the recorder.

## Recording growth and retention

M9.1 schema/recording v1 intentionally represents one contiguous fingerprinted frame per logical step. Silent frame decimation or prefix truncation would redefine that contract and remove historical verification anchors.

Therefore M11 may measure and improve memory/persistence through:

- chunked/incremental persistence;
- compression;
- streaming save/load;
- bounded in-memory caching while preserving a complete persisted recording;
- a separately versioned future retention/decimation contract if ever required.

M11 must **not** silently reinterpret recording v1 as a circular buffer or decimated trace.

## Recorder failure policy

The recorder is observational with respect to plant state and command authority, but its synchronous event handlers consume work and can fail while producing evidence. Documentation must not claim that recording can never delay or fail a run.

Current fail-closed evidence behavior is retained through M10. M11 robustness work must explicitly decide whether a recorder evidence failure:

1. faults/stops the host as today; or
2. marks the recorder `Compromised/Faulted`, unsubscribes it, permits deterministic plant execution to continue, and makes capture/complete/save fail explicitly.

Catching and hiding recorder exceptions while continuing to claim a complete recording is forbidden.

## Navigation-decision metadata

`MissionPerformanceNavigationDecision` records the M10.9.7.2 architectural decision. Its constant properties are not themselves enforcement. Enforcement remains executable tests over workspace catalog, F1-F8, F9 absence, navigation selection-only behavior and plant-command authority.

No pre-M10.9.7 closure refactor is required. A future maintenance pass may reduce/internalize compiled decision metadata only after preserving the executable contract tests and ADR provenance.

## Ownership summary

| Finding | Owner |
| --- | --- |
| fingerprint-v1 golden/schema anchor | M10.9.7.4 prerequisite |
| lifecycle-spine vs recent-evidence retention | M10.9.7.4 timeline contract |
| `LifecycleChanged` notification semantics/cost | M11.3 measured Application hardening |
| snapshot JSON/SHA/hex fingerprint cost | M11.3 measured performance |
| recorder read-only collection copies | M11.3 measured Application hardening |
| recorder long-session memory growth | M11.3 performance/memory; M11.2 if format/version changes |
| recorder synchronous failure policy | M11.3 robustness decision |
| future fingerprint v2 compatibility | M11.2 compatibility/migration |
| navigation-decision compiled metadata cleanup | maintenance, non-blocking |

## Non-scope of this review disposition

No current M10.9.7.3 runtime, scoring, challenge condition, replay authority, plant command authority, physics or persistence schema is changed by this documentation pass.
