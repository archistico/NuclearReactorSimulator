# ADR 0054 — Cold-shutdown recipe and pre-start guidance remain layered

- Status: Accepted / M7.2 validated
- Date: 2026-07-21

## Context

M7.1 established exact-version initial-condition reconstruction and scenario/session ownership, but intentionally supplied no concrete operational plant condition. M7.2 needs the first real training session without turning scenario files, Application presentation logic or Avalonia into new owners of thermofluid, neutronic, control, protection or electrical state.

Pre-start guidance also creates a second risk: a convenient "procedure" implementation could silently dispatch commands, patch state when checks fail, or advance simulation time to force progress. That would violate the deterministic ownership model established through M0–M6.

## Decision

1. The first operational initial condition is the immutable exact reference `cold-shutdown-pre-start` v1.
2. Its factory lives in Application and reconstructs a **fresh canonical runtime composition** using existing M1–M5 definitions/state owners, M5.7 integration and the validated simplified water/steam closure.
3. The initial-condition recipe may define the deterministic seed composition, but no scenario metadata, checklist, ViewModel or View may post-load patch authoritative physical state.
4. Pre-start readiness is evaluated only from the immutable `ControlRoomSnapshot` presentation boundary.
5. Readiness checks are observational only. They cannot dispatch commands, clear latches, retune controls, alter inventories or advance logical time.
6. Guided preparation is declarative. A step may name a suggested typed operator action, but only the operator/application command path may dispatch it.
7. Scenario permissions fail closed. M7.2 deliberately excludes control-rod withdrawal and generator-breaker closure, keeping first criticality and later synchronization in their planned milestones.
8. The desktop composition may load this exact session paused through the M7.1 registry/session factory; Avalonia still owns no physical initialization logic.

## Consequences

- The desktop gains a real initialized operational session without weakening App/Simulation separation.
- Initial-condition version changes remain explicit and replay-stable.
- Checklist/guidance behavior is independently testable from physics and cannot "fix" the plant to satisfy a procedure.
- M7.3 can build first-criticality operations on a precise pre-criticality handoff instead of redefining cold-shutdown state.
- General checkpoint serialization remains M9.1 scope; M7.2 does not serialize arbitrary live object graphs.

## Rejected alternatives

### Store a complete mutable plant-state blob in the scenario JSON

Rejected because it would couple scenario schema directly to every lower-layer state representation, encourage piecemeal ownership reconstruction and prematurely duplicate M9.1 checkpoint responsibilities.

### Let the ViewModel drive the procedure automatically

Rejected because UI refresh/timing would become behaviorally significant and guidance could bypass typed operator-command and deterministic-step boundaries.

### Auto-correct failed readiness checks

Rejected because acceptance/readiness criteria must remain observational; state correction would hide model behavior and create a second physics owner.
