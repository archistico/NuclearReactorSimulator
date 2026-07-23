# ADR 0086 — Secondary-pump discharge check valves are opt-in hydraulic topology

## Status

Accepted for M10.9.4 Hotfix 19 candidate.

## Context

The canonical pump model composes an active speed-squared pressure source with a bidirectional passive hydraulic path. That is appropriate for generic pumps and for scenarios where reverse flow is intentionally represented, but it allowed the secondary condensate/feedwater train to reverse through a stopped or under-headed pump. In long-run diagnostics this appeared as negative condensate-pump flow from the pressurized feedwater inventory back toward the hotwell. A global `max(0, flow)` clamp would incorrectly remove intentional reverse-flow capability from every pump and hide topology semantics inside the solver.

## Decision

`PumpDefinition` owns an opt-in `HasDischargeCheckValve` property. The default is `false`. `PumpFlowSolver` continues to solve the unconstrained active-head/quadratic-resistance relation. If and only if the pump definition has a discharge check valve and the solved flow is negative relative to the pump reference direction, the valve closes and the committed path transfer is zero mass, zero advected/internal energy and zero hydraulic exchange for that fixed step. Positive flow is unchanged.

The current-v2 sustained-generation and synchronization definitions enable the property on `condensate-pump` and `feedwater-pump` only. Legacy/default definitions remain bidirectional, and the main circulation pump is not changed by this ADR.

## Consequences

- Secondary-train backflow is prevented by explicit topology rather than by a global solver clamp.
- Stopped pumps with an enabled check valve still permit passive forward flow when the upstream pressure opens the valve.
- Running pumps cannot receive regenerative credit through reverse flow when the discharge check valve is closed.
- Protection expansion, actuator travel rates and adaptive substepping remain separate follow-on work.
