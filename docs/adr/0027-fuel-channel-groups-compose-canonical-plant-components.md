# ADR 0027 — Fuel-channel groups compose canonical plant components

- Status: Accepted
- Milestone: M3.4

## Context

M3.3 introduced configurable core zones, while M3.2 already owns deterministic network orchestration and single integration of conserved inventories. Fuel-channel groups must become operational without duplicating those responsibilities.

## Decision

A `FuelChannelGroupDefinition` is a semantic composition that references existing canonical plant domains and one existing passive hydraulic pipe. It never owns duplicate fluid or thermal state.

`FuelChannelGroupSolver` partitions zone fission power and optional global decay heat, observes committed-state hydraulic diagnostics, and emits immutable `PlantNetworkSourceTerms` for fuel, structure and coolant heat deposition.

`PlantNetworkOrchestrator` remains the only integration boundary. Supplemental source terms are accumulated before integration and their external power is included explicitly in the global conservation audit.

## Consequences

- No second hydraulic or thermal integration path is introduced.
- Zone/group registration order cannot become physical order.
- Fission and decay heat remain separate diagnostics while sharing the same staged thermal-source boundary.
- M3.5 can connect these groups to headers and pumps without redefining channel-group energy routing.
