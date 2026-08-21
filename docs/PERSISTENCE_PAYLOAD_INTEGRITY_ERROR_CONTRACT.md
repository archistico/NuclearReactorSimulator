# Persistence Payload Integrity & Adapter Error Contract

## Status and scope

This document describes the persistence runtime implemented by M10.9.7.2 Hotfix 3 and carried unchanged by **Hotfix 3 REV1**. The original Hotfix 3 package was superseded after a test-only exact exception-type assertion; REV1 changes that assertion, not the persistence implementation. Validation/current-package state is intentionally authoritative only in `PROJECT.md`.

M10.9.7.2 Hotfix 3 hardens the four JSON persistence adapters before live Mission/Performance UI wiring. It is a persistence-boundary correction only: no replay authority, scenario semantics, scoring, physics, protection, plant command authority or workstation route is changed.

## Session archive schema v1 remains v1

`ScenarioSessionArchive.CurrentSchemaVersion` remains `1`. `ControlRoomCommand.NumericValue` is added to the existing private `CommandDocument` DTO and is persisted for both `operatorActions[].command` and `events[].operatorCommand`.

This is additive for valid schema-v1 archives:

- old v1 archives containing commands that never require `NumericValue` remain readable;
- new v1 archives preserve manual turbine-control-valve demand exactly;
- old v1 archives that contain `TurbineControlValveManualDemandSet` but were written without `numericValue` are incomplete and cannot be reconstructed. They now fail immediately with `InvalidDataException` at deserialization rather than reaching replay as a malformed command.

A schema-v2 conversion from numeric enums to string enums is deliberately **not** part of this hotfix.

## Frozen schema-v1 numeric enum ordinals

The session archive continues to persist its enum fields as numbers. Regression tests freeze the current ordinal sets so changing any of these becomes an explicit persistence-contract decision:

- `ControlRoomCommandKind`: `Run=0` through `TurbineControlValveManualDemandSet=26`;
- `ControlRoomCommandTargetKind`: `ControlRod=0` through `Valve=7`;
- `ScenarioRecordingEventKind`: `OperatorAction=0` through `ProtectionTransition=3`;
- `ScenarioAutomationIntentKind`: `PlantControlAuthority=0`, `SupervisoryObjective=1`;
- `PlantControlAuthorityMode`: `Manual=0`, `Assisted=1`, `SupervisoryAutomatic=2`;
- `SupervisoryOperatingObjectiveKind`: `HoldReactorPower=0`, `HoldTurbineSpeed=1`, `HoldOperatingPoint=2`.

A future string-enum representation requires an explicit schema version and migration policy.

## Fail-fast command/event validation

The session archive adapter rejects unsupported persisted values before constructing replay evidence:

- undefined `ControlRoomCommandKind`;
- undefined non-null `ControlRoomCommandTargetKind`;
- undefined `ScenarioRecordingEventKind`;
- `TurbineControlValveManualDemandSet` without `NumericValue`.

The full-replay regression serializes and deserializes a real 37.5% turbine control-valve manual-demand sequence and verifies the restored archive through `ScenarioFullReplayRunner`.

## Adapter error contract

All four JSON adapters now use the same boundary convention for deserialization:

- malformed/truncated JSON, wrong JSON shape and constructor-level structural argument failures → `InvalidDataException`;
- unsupported future schema versions → `NotSupportedException`.

The session archive already had this normalization from M10.9.7.1 Hotfix 2. Hotfix 3 applies the same convention to scenario definitions, checkpoints and post-incident analysis.

## Post-incident DTO ownership

`JsonPostIncidentAnalysisSerializer` no longer exposes the Application-layer `ControlRoomCommand` as a document property. Its private `CommandDocument` explicitly owns `Kind`, `TargetId`, `TargetKind` and `NumericValue`, preserving the existing JSON member shape while decoupling the persisted schema from future changes to the Application record.

## Explicitly deferred

The following are not part of Hotfix 3:

- session archive schema v2;
- numeric-enum → string-enum migration;
- stream-based persistence APIs / `Utf8JsonWriter`;
- removal of the scenario-definition adapter's double parse;
- unrelated dictionary/comparer cleanup.

Ownership of those deferred items is explicit:

- schema-v2 / string-enum migration → M11.2 save/scenario/session compatibility and migration hardening, if adopted;
- stream-based persistence → M11.3 performance/memory gate, only if measured allocation/LOH evidence justifies it;
- scenario-definition double parsing and comparer cleanup → non-blocking maintenance unless a gate demonstrates a defect.

Those changes require separately scoped compatibility/performance evidence.
