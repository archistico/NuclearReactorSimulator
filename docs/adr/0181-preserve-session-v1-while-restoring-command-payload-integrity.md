# ADR-0181 — Preserve session schema v1 while restoring command payload integrity

## Status

Proposed — implemented in M10.9.7.2 Hotfix 3 REV1 candidate; local validation pending. The original Hotfix 3 package is superseded/not validated after a test-only exact `JsonException` type assertion failure; REV1 leaves the persistence runtime unchanged.

## Context

The schema-v1 session archive used a private `CommandDocument` that persisted command kind, target id and target kind but omitted `ControlRoomCommand.NumericValue`. `TurbineControlValveManualDemandSet` requires that numeric percentage during replay, so an otherwise valid saved session could lose data and fail when restored.

The same review also found inconsistent adapter error normalization, undefined persisted enum values reaching later layers, and direct use of the Application `ControlRoomCommand` record inside the post-incident JSON document model.

## Decision

1. Keep `ScenarioSessionArchive.CurrentSchemaVersion = 1`.
2. Add `NumericValue` to the schema-v1 private command DTO and round-trip it in both operator actions and recorder events.
3. Reject a persisted manual-demand command without numeric value at the adapter boundary.
4. Reject undefined persisted command target/event enum values at the archive boundary.
5. Freeze the current schema-v1 numeric enum ordinals in executable tests.
6. Give post-incident persistence its own private command DTO with the same JSON payload members.
7. Normalize malformed/structurally invalid JSON to `InvalidDataException` in every JSON adapter while preserving `NotSupportedException` for future schema versions.
8. Defer a numeric-to-string enum migration and stream-based persistence API to separately versioned work.

## Rationale

Adding `numericValue` is backward-compatible for valid v1 archives and repairs the actual data-loss defect without forcing every existing save through a format migration. Changing enum representation at the same time would create a larger compatibility surface unrelated to the lost payload.

A pre-Hotfix-3 v1 archive that already contains a manual-demand command without its numeric value is irrecoverably incomplete; failing immediately and explicitly is safer than fabricating a value or letting replay fail later.

## Consequences

- new v1 archives preserve complete typed manual-demand commands;
- existing v1 archives without numeric-value commands remain readable;
- incomplete historical v1 manual-demand archives fail at load with `InvalidDataException`;
- numeric enum ordinals become an explicitly tested persistence contract until a future schema version replaces them;
- live M10.9.7.3 wiring remains blocked until this persistence closure validates.
