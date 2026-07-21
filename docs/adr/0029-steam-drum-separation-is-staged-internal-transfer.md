# ADR 0029 — Steam-drum separation is a staged internal transfer

## Status

Accepted for M3.6.

## Context

The primary circuit now needs steam drums between channel outlets and main-circulation-pump suction. A naive implementation could directly mutate drum, steam and suction inventories during separation, violating the M3.2 committed-state orchestration rule and making component order observable.

## Decision

Steam-drum separation is modeled as a committed-state solver that produces conservative `PlantNetworkSourceTerms`.

- Channel return paths terminate at a dedicated loop return collector, which is the drum inventory node for M3.6.
- Separation reads committed circulation return flow and committed drum thermodynamics.
- Steam and liquid transfers are accumulated as internal fluid-node balances.
- `PlantNetworkOrchestrator` remains the only inventory integration boundary.
- The separation source terms declare zero external power because they only redistribute mass and energy inside the modeled plant boundary.

The existing five-argument `MainCirculationLoopDefinition` constructor remains backward compatible by treating the suction header as the return collector. M3.6 configurations use the new explicit return-collector overload.

## Consequences

- solver ordering cannot create mid-step drum-state feedback;
- mass/energy closure remains auditable at plant level;
- M3.7 can add feedwater/steam boundary terms without rewriting separation;
- one fixed-step staging lag remains explicit and deterministic;
- detailed separator correlations can replace the ideal split behind the same source-term boundary later.
