# ADR 0028 — Main circulation composes canonical network components

- Status: Accepted
- Milestone: M3.5

## Context

M3.4 introduced equivalent fuel-channel groups mapped to canonical plant pipes and thermal/fluid domains. The next step requires grouping headers, main circulation pumps, channel branches and return paths into meaningful reactor circulation loops.

Creating a separate RBMK-specific hydraulic solver would duplicate M1/M3.2 physics, introduce competing state ownership and risk order-dependent behavior.

## Decision

The main circulation system is a semantic composition over existing canonical plant components.

- Pumps remain `PumpDefinition` / `PumpState` solved by `PumpFlowSolver`.
- Channel hydraulic paths remain passive `PipeDefinition` objects owned by M3.1 topology and referenced by M3.4 groups.
- Return paths remain passive canonical pipes.
- Header states remain canonical `FluidNodeState` inventories.
- `MainCirculationSystemSolver` is diagnostic only and reads one committed `PlantState`.
- `PlantNetworkOrchestrator` remains the sole integration boundary.

Every fuel-channel group must belong to exactly one circulation loop in an M3.5 system definition.

## Consequences

- No duplicated pump/pipe equations or inventories.
- Loop diagnostics can evolve independently from integration mechanics.
- Component enumeration order cannot become physical semantics.
- M3.6 can insert steam drums/separation at the circulation seam without replacing the validated network solver.
- Future pump coastdown, check-valve and electrical behavior can extend operational state/control layers without rewriting the hydraulic topology boundary.
