# ADR 0005 — Runtime command scheduling and snapshot boundaries

- Status: Accepted
- Date: 2026-07-20

## Context

Operator actions can arrive asynchronously from the presentation layer while the physical model must evolve only on deterministic fixed-timestep boundaries. The UI must never receive mutable engine state.

## Decision

The generic simulation runtime owns a monotonic FIFO command queue. Pending commands are drained at the beginning of the next fixed physical step and are presented to the plant kernel together with that immutable step context.

The runtime publishes an immutable snapshot envelope containing runtime metadata and a model-specific state snapshot. The concrete kernel is responsible for creating a detached, immutable model snapshot.

Application-level operator intents will be mapped to simulation commands outside Avalonia Views. Avalonia never mutates engine state directly.

## Consequences

- command order is explicit and recordable;
- commands cannot execute halfway through a physical step;
- identical initial state, command order and external-duration sequence are replayable;
- UI refresh cadence does not define simulation cadence;
- future save/replay infrastructure has a stable command and snapshot boundary.
