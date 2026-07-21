# Plant Composition & Topology

## Purpose

M3.1 introduces the first plant-level composition boundary without adding new physical equations.

The already validated M1 primitives can now be assembled into one immutable, canonical topology:

- fluid nodes;
- passive pipes;
- valves;
- pumps;
- thermal bodies;
- heat-transfer links;
- external heat sources.

M2 reactor-physics submodels remain validated and independent; their spatial/core-zone composition is introduced in M3.3 rather than being forced into the M3.1 topology prematurely.

## `PlantDefinition`

`PlantDefinition` owns immutable canonical registries for all topology definitions in M3.1.

All registries are sorted with ordinal ID ordering, copied from caller collections and validated before the plant can exist.

The topology enforces:

1. at least one fluid or thermal inventory domain exists;
2. IDs are unique within each registry;
3. topology IDs are globally unique across component kinds;
4. wrapped hydraulic-path IDs inside valves and pumps are also globally unique;
5. every hydraulic endpoint references an existing fluid node;
6. every heat-transfer endpoint references an existing thermal domain;
7. every heat-source target references an existing thermal domain.

A thermal domain in M3.1 can be either:

- a `ThermalBodyDefinition`; or
- a `FluidNodeDefinition` receiving/removing energy through its existing energy-balance boundary.

## `PlantState`

`PlantState` owns the complete immutable state of stateful M3.1 topology components:

- `FluidNodeState`;
- `ValveState`;
- `PumpState`;
- `ThermalBodyState`;
- `HeatSourceState`.

Passive definitions such as pipes and heat-transfer links do not duplicate state.

A `PlantState` is valid only when it contains exactly one state for every stateful definition and no orphan state. Fluid-node and thermal-body states must also use definitions equal to the canonical definitions owned by the plant.

This exactness is intentional: M3.2 must never discover missing state, ambiguous IDs or ad-hoc component definitions while solving a timestep.

## Plant snapshot boundary

`PlantSnapshot` is the first plant-level immutable projection for application/UI/recorder boundaries.

M3.1 snapshots contain committed component state only. They deliberately do not yet contain:

- flow-solver diagnostics;
- balance accumulation diagnostics;
- mass/energy audit totals;
- core-zone diagnostics.

Those are added by later M3 milestones as the corresponding physical/orchestration concepts become real.

## Non-goals

M3.1 does not:

- execute pipes, valves, pumps or heat links;
- mutate or integrate inventories;
- define solver ordering;
- implement a network iteration algorithm;
- add core zones or fuel-channel groups;
- add RBMK-specific constants;
- add steam drums or separation.

Those boundaries remain explicit milestones rather than being hidden inside a premature `Plant.Update()` method.
