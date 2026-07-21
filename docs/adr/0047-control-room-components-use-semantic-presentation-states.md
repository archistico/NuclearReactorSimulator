# ADR 0047 — Control-room components use semantic presentation states

## Status

Accepted and validated with M6.2.

## Context

M6.1 established a presentation-only Avalonia shell. M6.2 needs reusable instruments and operator controls that can be composed across reactor, primary, turbine, electrical and alarm workspaces without duplicating visual-state logic or leaking simulation ownership into UI code.

## Decision

1. Reusable control-room components use the shared Application-layer `ControlRoomVisualState` contract: `Normal`, `Warning`, `Trip`, `Unavailable`.
2. Avalonia components render a supplied semantic state; they do not calculate trip/warning thresholds from physical truth.
3. Display components remain read-only.
4. Operator controls use standard keyboard/pointer semantics and route operational actions through Application command seams.
5. `Unavailable` interactive controls are disabled rather than accepting commands that cannot be represented honestly.
6. The component catalog is defined in Application without Avalonia dependencies; concrete rendering stays in App.

## Consequences

- M6.3–M6.6 can reuse one consistent visual and interaction vocabulary.
- UI styling can evolve without changing simulation/controller/protection ownership.
- Presentation tests can verify semantic catalogs independently from Avalonia rendering.
- Workspace code should not create ad-hoc warning/trip colors or control semantics when a reusable primitive exists.
