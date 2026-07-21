# ADR 0031 — Integrated primary circuit preserves single committed-state integration

## Status

Accepted for M3.8.

## Context

M3.3 through M3.7 introduced semantic layers for core zones, equivalent fuel-channel groups, main circulation, steam drums and temporary external feedwater/steam boundaries. The final M3 gate requires these layers to run as one primary-circuit model without allowing component ordering to create hidden same-step feedback or duplicate integration of conserved inventories.

A tempting implementation would call each subsystem sequentially and feed each candidate state into the next. That would make physical results depend on orchestration order and would violate the committed-state semantics established by M3.2.

## Decision

M3.8 introduces a top-level `IntegratedPrimaryCircuitDefinition` and `IntegratedPrimaryCircuitSolver`.

For every fixed step:

1. all subsystem solvers read the same committed `PlantState`;
2. diagnostic projections are produced from that committed state;
3. staged source terms are combined deterministically;
4. `PlantNetworkOrchestrator` integrates each conserved inventory exactly once;
5. the candidate plant state and complete subsystem diagnostics are exposed through one immutable integrated snapshot/result.

Reference operating points contain only explicit initial state, inputs and timestep. Long-run verification measures drift and residuals; it never corrects them procedurally.

## Consequences

- component enumeration cannot become an implicit nonlinear iteration;
- M3.4 nuclear heat, M3.6 internal separation and M3.7 external boundaries share one accounting boundary;
- later instrumentation can consume one plant-level snapshot without owning physics;
- M4 can replace the temporary steam/feedwater boundaries without rewriting the primary-circuit integration contract;
- true tightly coupled nonlinear iteration, if ever introduced, must be an explicit future architectural decision rather than an accidental side effect of solver order.
