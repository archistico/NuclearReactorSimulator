# ADR 0051 — Operational history uses logical step and alarm sequence

## Status

Accepted and validated with M6.6.

## Context

Trends and event timelines can easily create a second notion of time based on UI refresh or wall clock. Alarm presentation can also accidentally duplicate protection semantics if the UI re-evaluates alarm/trip conditions itself.

## Decision

1. Trend samples are indexed only by the published `ControlRoomSnapshot.LogicalStep`.
2. Re-observing the same logical step replaces that trend point rather than appending a refresh-dependent sample.
3. Alarm-event ordering uses only the M5.6 monotonic event `Sequence`; M6.6 may attach the publishing logical step for operator context but must not invent a second ordering key.
4. Annunciator rows and first-out state are projected from M5.6 snapshots rather than recomputed in Avalonia.
5. ACK/RESET are typed Application command intents addressing annunciator memory only; M5.5 protection ownership remains separate.
6. Trend and event history are bounded presentation state and cannot influence simulation timestep, solver execution or physical results.

## Consequences

- replay produces deterministic trend/timeline history for the same snapshot stream;
- rendering cadence cannot create additional physical-time samples;
- missing values remain explicit gaps instead of true-state fallbacks;
- M6.7 can connect a runtime coordinator without changing M5.5/M5.6 ownership boundaries.
