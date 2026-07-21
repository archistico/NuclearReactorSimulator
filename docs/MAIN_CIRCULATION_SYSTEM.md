# Main Circulation System

M3.5 composes the validated hydraulic primitives and M3.4 equivalent fuel-channel groups into semantic main-circulation loops.

## Scope

A loop is defined by canonical existing plant components:

```text
suction header
    │
    ├── main circulation pump(s)
    ▼
pressure header
    │
    ├── fuel-channel group A hydraulic path
    ├── fuel-channel group B hydraulic path
    └── ...
          │
          ▼
     group outlet nodes
          │
     passive return paths
          │
          └──────────────► suction header
```

M3.5 deliberately does **not** create new pump, pipe, fluid-node or fuel-channel inventories. `MainCirculationSystemDefinition` is a semantic composition above the canonical M3.1 `PlantDefinition` and M3.4 `FuelChannelGroupSetDefinition`.

Steam drums are not modeled in M3.5. The closed return path represents the pre-M3.6 circulation topology seam. M3.6 will introduce explicit separator/drum inventories and replace the simple return closure with drum/recirculation behavior without changing the pump/branch diagnostics boundary.

## Topology rules

Each `MainCirculationLoopDefinition` contains:

- one suction-header fluid node;
- one pressure-header fluid node;
- one or more canonical pump IDs;
- one or more channel branches.

Each branch maps:

- one canonical `FuelChannelGroupDefinition`;
- one canonical passive return `PipeDefinition` from the group outlet back to the loop suction header.

Validation is eager:

- every pump must run from the declared suction header to the declared pressure header;
- every fuel-channel group must take inlet coolant from the loop pressure header;
- every return pipe must run from that group's outlet node to the loop suction header;
- pump IDs, return-pipe IDs and fuel-channel-group assignments cannot be reused across loops;
- every M3.4 fuel-channel group must belong to exactly one circulation loop.

## Solver boundary

`MainCirculationSystemSolver` is diagnostic and stateless.

It reads only a committed `PlantState` and delegates to the already validated:

- `PumpFlowSolver` for MCP behavior;
- `PipeFlowSolver` for channel and return-path flow;
- `WaterSteamVoidFractionSolver` for local outlet void diagnostics.

It does not integrate or mutate inventories.

Physical evolution remains exclusively:

```text
PlantNetworkOrchestrator
    ↓
solve all canonical network components from common committed state
    ↓
accumulate balances
    ↓
integrate each inventory once
```

This prevents semantic plant composition from becoming a second network solver.

## Diagnostics

Per pump:

- running state and effective speed;
- active pressure boost;
- mass/volumetric flow;
- hydraulic power exchange;
- shaft power demand.

Per channel branch:

- channel-group and return-path flow;
- per-equivalent-channel flow;
- channel and return pressure differences;
- branch continuity residual;
- outlet phase, vapor quality and void fraction.

Per loop:

- suction/pressure-header pressure;
- header pressure rise;
- total pump flow;
- total channel-group flow;
- total return flow;
- pump-to-channel and channel-to-return continuity residuals;
- total hydraulic power exchange and shaft demand.

Continuity residuals are diagnostics. M3.5 never hides or numerically corrects them.

## Fidelity boundary

M3.5 intentionally defers:

- steam-drum/separator inventories;
- natural-circulation elevation head;
- pump rotational inertia/coastdown dynamics;
- check valves and pump discharge isolation logic;
- cavitation/NPSH;
- detailed two-phase channel pressure-drop correlations;
- electrical motors and bus supply.

These belong to later milestones. The semantic loop composition and committed-state diagnostic boundary are designed to survive those fidelity increases.


## M3.6 return-collector extension

M3.6 adds a backward-compatible explicit `ReturnCollectorNodeId` to `MainCirculationLoopDefinition`. The original constructor still maps return paths directly to the suction header. Steam-drum configurations use the explicit overload so channel return pipes terminate at the dedicated drum inventory node before staged liquid recirculation returns separated liquid to MCP suction.
