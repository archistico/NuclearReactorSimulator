# ADR 0074 — Supervisory automation is M5-owned and authority intents are replayed separately

## Status

Accepted for the M10.5/M10.6 implementation candidate chain; validation pending local clean build/test of M10.2–M10.6.

## Context

The operator computer needs both training-assistance selection and real plant-control automation. Treating those as one mode would couple didactic presentation to physics. Implementing the supervisor in App/Application would create a second control owner. Encoding authority/objective changes as ordinary `ControlRoomCommandKind` values would also collapse physically distinct command, training and session/control-intent seams and would make M9.1 replay incomplete if the new intents were omitted.

## Decision

1. Training assistance and plant control authority are independent axes.
2. Real supervisory logic is implemented in Simulation under the canonical M5 control domain.
3. The supervisor may only coordinate existing local controller modes/setpoints and typed canonical seams; it may not assign physical outcomes directly.
4. Required feedback uses measured signals only. Missing/invalid required measurements degrade fail-closed with no true-state fallback.
5. Protection/interlocks remain superior and may suspend supervisory decisions. Supervisory logic never resets protection or acknowledges alarms automatically.
6. Manual takeover uses committed controller last outputs for deterministic bumpless handover.
7. Authority and supervisory-objective changes remain typed Application intents separate from `ControlRoomCommandKind`.
8. Accepted authority/objective intents are stored in a dedicated deterministic scenario journal and reapplied by M9.1 replay at the next fixed-step boundary. Existing `ControlRoomSnapshot` fingerprint schema v1 is not expanded solely for UI/control-authority metadata.

## Consequences

- historical M9.1 recordings without automation intents remain valid and have an empty automation-intent stream;
- sessions that use M10.5/M10.6 can be replayed without fabricating controller state from presentation snapshots;
- the operator computer can show requested/effective/degraded authority without owning the algorithms;
- future supervisory objectives must remain bounded, deterministic and expressed through canonical control seams;
- full human-automation/fault/replay validation remains the M10.9 gate.
