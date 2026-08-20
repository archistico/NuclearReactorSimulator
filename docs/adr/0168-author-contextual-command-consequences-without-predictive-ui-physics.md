# ADR-0168 — Author contextual command consequences without predictive UI physics

## Status

Proposed in M10.9.5.1; becomes Accepted only after local build, complete ordinary tests and the focused consequence-catalog gate are explicitly green.

## Context

M10.4 already exposes typed contextual commands with AVAILABLE/BLOCKED/UNAVAILABLE state while canonical runtime/scenario validation remains authoritative. M10.9.5 must explain command meaning and downstream relevance without turning Application/Avalonia into a second plant model or implying that a requested action guarantees a future pressure, flow, power or reactivity value.

The runtime currently defines 27 `ControlRoomCommandKind` values. Some turbine valve/control-valve commands are canonical runtime commands even though the existing M10.4 COMMANDS console does not yet enumerate them. A consequence contract that only covers current UI rows would therefore be incomplete and fragile.

## Decision

- Maintain an explicit deterministic Application-only consequence catalog keyed by typed `ControlRoomCommandKind` plus supported canonical target kind.
- Separate direct intent, expected qualitative influence, already-published permissive references and monitor targets.
- Limit expected-influence vocabulary to authored qualitative relations: increases/decreases expected demand on, enables/disables path, affects, may affect and protection may override.
- Never produce predicted numeric future values, confidence percentages or generic SUCCESS/FAILURE from the consequence catalog.
- Reference only already-published `ControlRoomSnapshot` property paths and canonical whole-plant mimic element identifiers.
- Preserve MEASURED / MODEL / canonical-state provenance on monitor targets.
- Cover all 27 current command kinds, including runtime-supported turbine valve/control-valve commands not yet exposed by M10.4.
- Unknown/future command-target shapes fail closed as `NO AUTHORED CONSEQUENCE MAP`; no automatic graph traversal or inferred causality is permitted.
- Command dispatch remains `ControlRoomCommand` → `IControlRoomCommandDispatcher` → canonical runtime/scenario validation.

## Consequences

M10.9.5.2 can build bounded dependency chains on top of a stable semantic vocabulary without inventing topology. M10.9.5.3 can present the catalog in COMMANDS/inspector/schematic views while keeping command execution and permissive ownership unchanged. Missing authored relationships remain visible gaps rather than implicit predictions.
