# ADR 0048 — Reactor/Core panel separates measured instruments from model diagnostics

## Status

Accepted and validated with M6.3.

## Context

M6.1 established a presentation-only control-room shell and M6.2 established reusable semantic controls. The Reactor/Core workspace needs both operator-facing measured values and educational spatial/kinetics diagnostics that are not all represented as M5.1 instrument channels. Mixing those categories silently would weaken the architecture and could encourage UI logic to treat perfect diagnostics as instrumentation.

## Decision

1. Avalonia continues to bind only to Application-layer presentation records.
2. Measured instrument tiles are projected from `MeasuredSignalFrame` and preserve validity/quality semantics.
3. Non-instrumented reactor/core values may be exposed only as explicitly labelled model diagnostics projected at the Application boundary; they do not become hidden controller/protection inputs.
4. Physical warning/trip thresholds are never inferred in Avalonia.
5. Reactor SCRAM/interlock status is presentation of M5.5 protection state, not UI-owned protection logic.
6. Rod/SCRAM/reset operator actions leave Avalonia as typed `ControlRoomCommand` intents; no rod/kinetics/protection state is mutated by the view or view model.
7. Missing operational boundaries remain `Unavailable`. In particular, M2.8 xenon state is not fabricated while it is absent from the M5.7 automatic-operation envelope.

## Consequences

- The first domain panel can be educationally rich without reintroducing physics into Avalonia.
- Operators can distinguish measured channels from model diagnostics.
- Later M6 panels can reuse the same presentation discipline.
- Runtime integration remains an explicit later concern rather than being hidden behind UI event handlers.
