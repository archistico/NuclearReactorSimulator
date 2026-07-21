# Fuel-Channel Group Model

M3.4 attaches equivalent fuel-channel groups to the configurable aggregated core zones introduced in M3.3.

## Boundary

A fuel-channel group is a semantic composition over canonical plant components. It does not own duplicate fluid or thermal inventories.

Each group references:

- one parent core-zone id;
- one passive hydraulic `PipeDefinition` from inlet coolant node to outlet coolant node;
- the zone's canonical fuel thermal body;
- the zone's canonical structure thermal body;
- the zone's canonical outlet coolant fluid node;
- a represented physical-channel count;
- a fraction of its parent zone power;
- explicit fuel / structure / coolant heat-deposition fractions.

All group ids and hydraulic paths are canonical and deterministic. Group power fractions must sum to 1.0 independently inside every parent zone.

## Power routing

Global point kinetics remains unchanged. M3.3 first projects global fission power to zones. M3.4 then partitions each zone power across its configured channel groups.

The solver can also accept global decay heat. For M3.4 the same current zone/group power shape is used to spatially route that decay heat, while fission and decay remain separate diagnostics.

For each group:

`zone fission power -> group fission power`

`global decay heat -> zone share -> group decay heat`

`group nuclear heat -> fuel + structure + coolant source terms`

The final group in every canonical zone receives the floating-point residual so both fission and decay allocations close deterministically.

## Hydraulic diagnostics

The group does not implement another hydraulic solver. It references an existing passive plant pipe and observes its signed mass flow from the same committed `PlantState` using the validated `PipeFlowSolver`.

The actual integration of that pipe remains exclusively inside `PlantNetworkOrchestrator`.

## Staged source terms

`FuelChannelGroupSolver` emits `PlantNetworkSourceTerms` rather than mutating inventories.

The M3.2 orchestrator now accepts these supplemental source terms, validates their target ids, accumulates them before integration, and includes their declared external power in `PlantNetworkAudit`.

This keeps the invariant:

1. read committed state;
2. solve all components/source models;
3. accumulate all balances;
4. integrate every inventory once;
5. audit and commit transactionally.

## Deliberate limitations

M3.4 does not yet introduce:

- individual physical channels;
- channel boiling correlations beyond the existing lumped water/steam closure;
- pressure headers, suction headers or main circulation pumps as an integrated circuit;
- steam drums or phase separation;
- spatial neutron diffusion;
- independently solved per-group xenon or neutron population.

Those belong to M3.5+ or later fidelity work.
