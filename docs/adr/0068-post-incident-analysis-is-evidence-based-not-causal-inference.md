# ADR 0068 — Post-incident analysis is evidence-based, not causal inference

- **Status:** Accepted / M9.2 VALIDATED
- **Date:** 2026-07-22

## Context

M9.1 provides deterministic recordings, event streams, replay verification and checkpoints. M9.2 needs useful debrief analysis without creating a second simulation model or overstating what temporal event ordering proves.

## Decision

Post-incident analysis consumes immutable M9.1 recording artifacts only.

It may derive:

- deterministic pre/post logical-step windows;
- event ordering and relative-step latency;
- observed state summaries and peak counts;
- references to preceding replay-backed checkpoints.

It must not infer that event A physically caused event B solely because A preceded B. The report preserves recorded evidence and temporal relationships; causal interpretation remains explicit and external unless the authoritative model exposes that relationship directly.

## Consequences

- analysis is deterministic and UI-cadence independent;
- no private solver-state serialization is introduced;
- replay remains the authoritative verification/reconstruction path;
- reports remain suitable for training debrief without creating false forensic precision.

## Rejected alternatives

- automatic root-cause labels from event adjacency: unsupported causal inference;
- separate incident-state restore dumps: duplicates M9.1 checkpoint/replay ownership;
- wall-clock response timing: breaks deterministic logical-time semantics.
