# ADR 0035 — Condensate/feedwater return reuses canonical pumps and zeroes the legacy feedwater source

## Status

Accepted for M4.4 baseline candidate.

## Context

M4.3 establishes a conserved hotwell inventory, while M3.7 still contains the temporary external feedwater source used before the secondary cycle exists. M4.4 must return condensate to the steam drums without creating a second hydraulic solver, duplicate inventories or double feedwater addition.

## Decision

1. M4.4 condensate and feedwater pumps are existing canonical `PumpDefinition` components in `PlantDefinition`.
2. Their balances are integrated only by the existing `PlantNetworkOrchestrator`.
3. M4.4 semantic definitions validate the exact hotwell → condensate pump → feedwater inventory → feedwater pump → M3 feedwater-target topology.
4. Every M3 feedwater boundary is covered by exactly one M4.4 train.
5. While M4.4 owns the return path, all M3 `FeedwaterBoundaryInput` mass flows must be exactly zero.
6. Feedwater thermal conditioning is an explicit bounded energy source term on a canonical fluid inventory with declared positive external power.
7. M4.4 pump solves outside the orchestrator are diagnostic projections only and never independently evolve conserved state.

## Consequences

- secondary-cycle mass can circulate internally from hotwell back to steam drums;
- mass remains conserved through one canonical network integration;
- pump hydraulic work remains covered by the existing global audit;
- conditioning energy remains explicit and auditable;
- the M3 feedwater seam remains backward-compatible while its temporary external source is disabled;
- later detailed heater/deaerator models can replace the lumped conditioning seam without changing topology ownership.
