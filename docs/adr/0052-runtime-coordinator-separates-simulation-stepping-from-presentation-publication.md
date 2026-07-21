# ADR 0052 — Runtime coordinator separates simulation stepping from presentation publication

## Status

Accepted; M6.7 locally validated on 2026-07-21.

## Decision

The M6 live runtime boundary is split into a deterministic `IControlRoomRuntimeEngine` and an Application-level `ControlRoomRuntimeCoordinator`.

The runtime engine owns M5.7 stepping and operator-command translation. The coordinator owns run/pause/single-step semantics, bounded accelerated batches and snapshot publication cadence.

Presentation publication may be sparser than simulation stepping. Every logical simulation step still executes exactly once with the fixed simulation timestep.

## Consequences

- UI refresh cadence cannot influence physical results.
- Accelerated execution can reduce presentation traffic without skipping physics.
- Avalonia remains free of Simulation references.
- M7.1 can supply versioned initialized sessions without changing M6 views/view models.
