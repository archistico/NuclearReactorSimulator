# Aggregated Core-Zone Model

M3.3 introduces a configurable spatial decomposition of the reactor core without replacing the validated global point-kinetics model.

## Boundary

The core model separates three concerns:

1. **Global neutronics** remains owned by `PointKineticsSolver`.
2. **Spatial power distribution** is represented by normalized per-zone power fractions.
3. **Local physical inventories** remain owned by `PlantState` fluid nodes and thermal bodies.

A core zone therefore references existing plant domains rather than duplicating temperature, pressure, energy or mass.

## Configuration

`AggregatedCoreDefinition` contains canonical `CoreZoneDefinition` entries. Each zone defines:

- a globally meaningful zone id;
- a logical `CoreZoneCoordinate`;
- a nominal power fraction;
- one fuel thermal-body id;
- one structure thermal-body id;
- one coolant fluid-node id.

Coordinates are zero-based logical placement metadata only. The engine does **not** assume a 3x3 grid, a rectangular shape, contiguous coordinates or a fixed zone count.

Nominal fractions must sum to 1.0. All referenced plant domains are validated eagerly against the canonical `PlantDefinition`.

## Dynamic state

`AggregatedCoreState` stores only the current normalized power fraction for each zone. The state must contain exactly one entry for every configured zone and the fractions must sum to 1.0.

This allows future spatial redistribution algorithms to evolve power shape without moving physical inventories out of `PlantState`.

## Power distribution

`AggregatedCorePowerSolver` receives:

- one global fission thermal power;
- one committed `AggregatedCoreState`;
- the matching committed `PlantState`.

It deterministically allocates global power across zones. The canonical final zone receives the floating-point residual so the sum of zone powers equals the supplied global power exactly under the solver's arithmetic order.

M3.3 itself does not deposit this power into fuel or coolant. M3.4 consumes the validated zone powers through equivalent fuel-channel groups and emits staged fuel/structure/coolant source terms for M3.2 integration.

## Local diagnostics

Each `CoreZoneSnapshot` exposes:

- current zone power fraction;
- zone fission thermal power;
- fuel temperature;
- structure temperature;
- coolant temperature and pressure;
- coolant phase and vapor quality;
- volumetric void fraction when the fluid phase is specified.

These values are projections from the committed plant state, not new conserved state.

## Deliberate exclusions

M3.3 does not implement:

- spatial neutron diffusion;
- inter-zone neutron coupling;
- channel-group hydraulics;
- local xenon inventories;
- automatic power-shape evolution;
- per-zone heat deposition.

Those capabilities are introduced only when their owning milestones provide the required physics and state semantics.
