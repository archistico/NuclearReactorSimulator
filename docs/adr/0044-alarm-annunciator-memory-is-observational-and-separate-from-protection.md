# ADR 0044 — Alarm/annunciator memory is observational and separate from protection

## Status

Accepted; M5.6 locally validated.

## Decision

Alarm and annunciator state observes canonical M5.1 measured signals and M5.5 protection snapshots. It may latch, acknowledge, reset presentation memory, assign first-out ownership and emit deterministic logical events, but it cannot issue or clear physical protection actions.

M5.5 remains the sole owner of SCRAM, turbine trip, generator trip, interlocks and protection reset. Alarm acknowledgement therefore has no physical side effect.

First-out/event ordering uses monotonic logical sequence numbers rather than wall-clock time.

## Consequences

- operator acknowledgement cannot accidentally clear a trip;
- M6 can render alarms and event timelines from immutable snapshots;
- replay remains deterministic;
- protection and annunciation can evolve independently while remaining correlated through snapshots.
