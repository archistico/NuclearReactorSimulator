# ADR 0007 — Logical command traces and deterministic replay

## Status

Accepted for M0.3 baseline candidate.

## Context

Future training scenarios, regression tests and incident reconstruction require repeatable operator actions. Wall-clock timestamps are unsuitable because UI cadence, machine load and scheduler timing must not influence physical results.

## Decision

A command trace is an immutable ordered sequence of:

```text
logical fixed-step index + command
```

Commands sharing the same step execute in trace order.

`SimulationReplayRunner` replays traces only against a paused runtime using `StepOnce()`. It rejects hidden pending commands and traces that target already executed steps.

The automated test harness builds on the same semantics and captures the initial snapshot plus every committed step snapshot.

M0.3 intentionally does not define serialized replay files. Serialization, compatibility/versioning and long-term recorder storage remain Infrastructure concerns for later milestones.

## Consequences

- replay timing is independent from wall-clock and UI timing;
- test scenarios are readable in terms of physical step boundaries;
- the same trace can be executed repeatedly to verify deterministic state equality;
- future recorders have a stable logical primitive to serialize without coupling persistence into Simulation.
