# ADR 0060 — Fault injection is explicit deterministic scenario state

**Status:** Accepted / validated with M8.1

## Context

M5.1 already exposes explicit deterministic sensor-fault inputs, while M7 establishes exact-version initial conditions, scenario persistence, deterministic replay and observational training evaluation. M8 needs abnormal and transient scenarios without introducing hidden randomness, wall-clock scheduling, scenario-side physics or a second plant owner.

## Decision

1. Faults are explicit immutable entries in the versioned scenario definition, identified by stable fault ID, fault-type ID and target ID plus deterministic parameters.
2. A fault has one single-pass lifecycle: `Pending → Active → Cleared`. Reactivation requires a distinct declared fault entry rather than hidden scheduler memory.
3. Activation and optional deactivation are evaluated only at committed logical-step boundaries.
4. Exact logical-step triggers are deterministic schedule data. Plant-condition triggers reference a named evaluator registered explicitly by the application composition.
5. Plant-condition evaluators consume only committed `ControlRoomSnapshot` presentation data. They may not traverse authoritative `FullPlantSnapshot`/`PlantState` or mutate runtime state.
6. Fault effects are delegated through fail-closed runtime-bound applicators keyed by fault-type ID. M8.1 owns orchestration/lifecycle only; concrete subsystem fault semantics belong to later M8 milestones and must reuse canonical subsystem seams.
7. Fault lifecycle state is projected into the immutable control-room snapshot with activation/clear logical-step stamps and a monotonic deterministic transition sequence.
8. Scenario persistence advances to schema v2. Migrating v0/v1 preserves the exact initial-condition reference and creates no implicit faults.
9. Deterministic replay reloads the same scenario definition and reconstructs fault scheduling from scenario data; no wall-clock or independent probabilistic fault trace is introduced.

## Consequences

Fault scenarios become replayable and inspectable before concrete equipment-failure families are added. Missing effect handlers or condition evaluators fail closed at session load. M8.2+ can add typed applicators without moving pump, valve, instrumentation, control, protection or electrical ownership into the scenario layer.
