# ADR 0076 — Advanced gauges render published semantics and trends use logical steps

## Status

Accepted for M10.9.2 implementation candidate.

## Context

The validated M10.9.1 HMI contract distinguishes display scale, operating interpretation, target, setpoint and protection limits. A conventional progress bar cannot faithfully show those independent concepts and can misleadingly make “inside range” look equivalent to “safe”.

Trend indication also risks introducing wall-clock/UI-cadence behavior if derived from render timing.

## Decision

1. Advanced linear/circular controls consume immutable Application presentation semantics; Avalonia owns geometry only.
2. Operating bands are shown only when a canonical owner explicitly publishes them. The UI never synthesizes a green “normal” region from the absence of alarms.
3. Target windows, controller setpoints and protection limits remain separate visual layers.
4. Off-scale numeric values remain explicit; graphical clamping never changes their semantic status.
5. Measurement provenance/quality remain visible and missing measurement never falls back to model state.
6. Trend/rate uses published logical-step deltas only. Same-step or backwards-step discontinuities invalidate the trend.
7. Circular gauges are selective; values without a defensible canonical scale remain numeric rather than receiving invented ranges.
8. Presentation-only metadata added to `ControlRoomValueSnapshot` is excluded from fingerprint-v1 serialization/replay identity.

## Consequences

- HMI clarity improves without moving physics/protection ownership into App.
- New scenario target bands can be added later by their canonical owner without redesigning gauge controls.
- Replay/session reset does not create false trend arrows.
- Some values remain plain numeric indicators until a legitimate scale/limit contract exists.
