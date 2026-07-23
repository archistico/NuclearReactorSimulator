# ADR 0077 — Whole-plant mimic is Application-owned presentation topology

**Status:** Accepted for M10.9.3 implementation candidate.

## Context

The operator needs a whole-plant schematic that shows major equipment, explicit inputs/outputs and process/energy connections. Building those relationships directly in Avalonia XAML would create a second, UI-owned interpretation of plant topology and make later state/flow semantics fragile.

## Decision

M10.9.3 introduces immutable `ControlRoomPlantMimic*` presentation contracts in Application.

Application composes the high-level whole-plant mimic from the already validated `ControlRoomSnapshot` boundary. It owns presentation-level element identity, directed connections, medium/energy classification, normalized layout and drill-down destination.

Avalonia:

- renders the supplied contracts;
- handles presentation selection;
- navigates to existing workspaces;
- does not infer process topology, flow direction, pressure, temperature, phase or plant state.

The mimic is an aggregate presentation map, not authoritative physics/network state and not a replacement for subsystem definitions.

## Consequences

- no second plant model is created in the UI;
- replay/fingerprint inputs remain unchanged;
- whole-plant layout can evolve without changing simulation ownership;
- detailed subsystem diagrams can reuse the same boundary principle in M10.9.4;
- navigation has no plant-state side effects.
