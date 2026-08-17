# ADR 0123 — Control room retains area workspaces and gains industrial controls, persistent mimic layout and Instructor mode

## Status

Accepted on 2026-08-16 as future product/UX architecture direction. Implementation is deferred; multi-monitor is explicitly excluded for now.

## Context

The existing control room is correctly divided by subsystem areas/tabs and already has a whole-plant mimic, advanced instrumentation, alarms, procedures/guidance and typed commands. The current visual treatment is not yet sufficiently industrial/retro, control-handle semantics can be made more realistic, and the mimic needs better navigation/layout ergonomics.

The separate `archistico/IndustrialControls` Avalonia project is a preferred future control-library source, subject to implementation-time compatibility review.

## Decision

1. Keep subsystem/area tabs; do not collapse the application into one giant control-room screen.
2. Integrate appropriate IndustrialControls only at the Avalonia presentation layer over existing Application snapshots/commands.
3. Add stronger retro-industrial visual identity without sacrificing legibility, provenance, quality, alarm or protection semantics.
4. Distinguish maintained operator-handle/selector position from effective equipment state where real control semantics require it. Automatic trip does not magically move a maintained selector back to a neutral state.
5. Preserve distinct semantics for maintained selectors/toggles, momentary pushbuttons, spring-return switches, breaker controls and reset actions.
6. Treat the whole-plant mimic as a first-class navigable surface with zoom, pan, fit/reset view, layout-edit mode, drag, lock and reset-to-default.
7. Persist user layout overrides by stable canonical equipment ID. Visual positions never change physical/electrical/hydraulic topology.
8. Add workspace presets that change presentation context only, never plant state.
9. Develop more plant-like mnemonic displays and real operating procedures over canonical commands.
10. Add a visually distinct Instructor/Fault mode for deterministic fault injection and future scenario-control authority.
11. Do not implement multi-monitor/multi-computer operation in the current approved backlog.

## Consequences

The control-room UI will become more immersive without becoming an owner of simulation logic. Layout persistence requires a small versioned presentation-preference schema. Operator-handle state may require replay-visible Application-level presentation/command state separate from equipment state.
