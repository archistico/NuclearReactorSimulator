# ADR 0070 — Operator computer aggregates existing owners; supervisory automation remains M5-owned

## Status

Accepted for planned M10 architecture.

## Context

The simulator already contains guidance/training evaluation (M7), measured/presentation state and typed operator command seams (M5/M6), alarms/history (M5.6/M6.6), exact-version scenarios/sessions (M7), and recorder/checkpoint/replay/post-incident analysis (M9.1/M9.2).

A unified operator "computer" is desired to expose these capabilities through one keyboard-friendly terminal. The same phase is also intended to add real Manual / Assisted / Supervisory Automatic plant-control authority.

Without an explicit boundary, a terminal could accidentally become a second owner of guidance, alarms, commands, session state or physical automation, or could conflate training assistance with physical control authority.

## Decision

1. The operator computer is an **Application-layer aggregator plus App-layer presenter**, not a new physical owner.
2. Training assistance (`TrainingGuidanceMode`) and plant control authority are **independent axes**.
3. Real supervisory plant automation extends the canonical **M5 control domain** and coordinates existing controllers/setpoints/typed plant commands. App/UI never owns supervisory algorithms.
4. Supervisory automation never directly assigns derived physical outcomes. It acts only through existing canonical control/actuator seams.
5. Protection/interlocks remain superior to all normal/supervisory control. No automatic trip/SCRAM reset or alarm acknowledgement is implied.
6. Supervisory consumers use required measured signals only and must degrade/fail closed on invalid/unavailable required information; no silent true-state fallback.
7. Manual takeover requires deterministic bumpless handover using/reusing M5.2 semantics.
8. Terminal actions are separated into **plant commands**, **training/presentation intents**, and **session lifecycle intents**. They are not all forced into `ControlRoomCommandKind`.
9. The terminal uses fixed deterministic pages/menu navigation. No free-form natural-language command parser or external model dependency is introduced.
10. Session save/load added in M10 must remain a versioned packaging layer over exact scenario identity, M9.1 recording/action history and replay-backed checkpoints. Restoration authority remains replay/fingerprint verification, not opaque state dumping.

## Consequences

### Positive

- one coherent operator workflow without duplicating source-of-truth ownership;
- training assistance can vary independently from physical automation;
- supervisory automation remains testable headlessly and deterministic;
- M8 faults and invalid measurements can exercise automation degradation through canonical operational seams;
- M9 recorder/replay/post-incident capabilities can be surfaced without inventing a second historian/session model;
- keyboard-first terminal behavior remains deterministic and testable.

### Costs

- M10 requires explicit aggregation contracts/adapters rather than directly binding every existing ViewModel/type;
- supervisory automation is a substantive M5 extension, not a cosmetic UI feature;
- requested/effective/degraded authority and per-loop modes need careful state modeling and replay semantics;
- a true persistent "save session" requires versioned recording/session packaging in addition to existing checkpoint serialization.

## Rejected alternatives

### Put the autopilot in the operator-computer ViewModel

Rejected because it would move physical control ownership into App/UI, couple behavior to presentation lifetime/cadence and violate headless deterministic architecture.

### Reuse `TrainingGuidanceMode` as the plant automation level

Rejected because educational assistance and physical control authority have different semantics and must be combinable independently.

### Route every terminal action through `ControlRoomCommandKind`

Rejected because training/presentation and session lifecycle intents are not plant commands and should retain their proper owners.

### Add a free-form command/NLP terminal

Rejected because it introduces ambiguous parsing/nondeterministic/external dependencies without adding value to the deterministic operator-training model.

### Save opaque private solver state

Rejected because M9.1 established replay-backed versioned checkpoints as the restoration authority. M10 persistence must compose with that architecture rather than supersede it.
