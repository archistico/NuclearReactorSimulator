# Condensate & Feedwater Train

M4.4 closes the modeled secondary-water mass path from the validated M4.3 condenser hotwell back to the M3 steam-drum feedwater targets.

## Canonical path

```text
Turbine exhaust
    ↓
Condenser steam space
    ↓ condensation
Hotwell
    ↓
Canonical condensate pump
    ↓
Feedwater inventory / conditioning node
    ↓
Canonical feedwater pump
    ↓
Steam-drum feedwater target
```

The new M4.4 semantic layer does not create a parallel hydraulic graph. Both pumps are existing canonical `PumpDefinition` components owned by `PlantDefinition`, and their fluid transport is still solved by the existing `PlantNetworkOrchestrator` from the same committed `PlantState` as every other hydraulic component.

## Definitions

`CondensateFeedwaterSystemDefinition` owns one or more `CondensateFeedwaterTrainDefinition` entries.

Each train binds:

- one M4.3 condenser/hotwell;
- exactly one legacy M3 `FeedwaterBoundaryDefinition` seam;
- one canonical condensate pump from hotwell to a canonical feedwater-inventory node;
- one canonical feedwater pump from that inventory node to the M3 feedwater target;
- a configured maximum thermal-conditioning power.

Every M3 feedwater boundary must be covered by exactly one M4.4 train. Pump endpoint direction is validated eagerly so the semantic topology cannot silently disagree with the canonical hydraulic graph.

## Replacement of the M3 feedwater source

The original M3.7 feedwater boundary remains in the definition graph as the stable semantic seam used by later phases, but while M4.4 is active every corresponding `FeedwaterBoundaryInput.MassFlowRate` must be exactly zero.

This prevents double accounting:

```text
INVALID:
external M3 feedwater source
+ internal M4.4 hotwell return

VALID M4.4:
hotwell → canonical pumps → drum target
```

M4.4 therefore closes the modeled secondary-water mass path without introducing external makeup mass during normal train operation.

## Pump ownership and diagnostics

M4.4 does not solve pump balances a second time for state evolution.

`CondensateFeedwaterSystemSolver` evaluates `PumpFlowSolver` against the committed node/pump states only to produce deterministic diagnostics such as:

- effective pump speed;
- mass flow;
- active pressure boost;
- internal pressure loss;
- hydraulic power exchange;
- shaft-power demand.

The actual pump balances used to evolve inventories remain those generated once by `PlantNetworkOrchestrator`.

## Feedwater inventory and thermal conditioning

Each train has a canonical feedwater-inventory node between the condensate and feedwater pumps. This node is an ordinary `FluidNodeState`, so its:

- mass;
- internal energy;
- temperature;
- pressure;
- phase

remain conserved/closed through the normal fluid-node integration path.

M4.4 provides a manually commanded lumped thermal-conditioning input bounded by `MaximumThermalConditioningPower`. This represents educational feedwater heating/deaeration duty without yet modeling detailed extraction-steam heater trains.

Conditioning heat is added as an explicit `PlantNetworkSourceTerms` energy contribution to the feedwater-inventory node and is declared as positive external power. It is therefore visible in `PlantNetworkAudit`; no energy is created through hidden state mutation.

## Single-integration composition

The full staged path is now:

```text
M3 integrated primary circuit
+ M4.1 main steam
+ M4.2 turbine expansion
+ M4.3 condenser
+ M4.4 feedwater thermal-conditioning terms
        ↓
PlantNetworkSourceTerms.Combine(...)
        ↓
ONE PlantNetworkOrchestrator.Step(...)
```

Canonical pump transport is already part of that same orchestrator pass.

`CondenserSystemSolver` therefore gains a backward-compatible supplemental-source-term overload so M4.4 can compose conditioning heat before the inherited single integration boundary.

## Snapshot surface

`CondensateFeedwaterSystemSnapshot` exposes:

- inherited M4.3 condenser/turbine/primary diagnostics;
- per-train hotwell and feedwater-inventory initial/final masses;
- feedwater-inventory initial/final temperature and specific internal energy;
- final phase;
- condensate-pump diagnostics;
- feedwater-pump diagnostics;
- commanded thermal-conditioning power;
- aggregate conditioning power and pump shaft-power demand;
- inherited global thermofluid audit.

## Deliberate limits

M4.4 does not yet model:

- detailed low/high-pressure heater cascades;
- extraction-steam drains and drain coolers;
- deaerator gas-release chemistry;
- makeup-water chemistry or inventory control;
- automatic hotwell/feedwater level controllers;
- generator electromagnetic load;
- grid synchronization;
- closed integrated secondary-cycle operating-point validation.

Generator/grid coupling begins in M4.5. Full coupled secondary-cycle heat-balance verification belongs to M4.6.


## M4.5 composition seam

M4.5 adds a backward-compatible overload on `CondensateFeedwaterSystemSolver` that accepts supplemental `PlantNetworkSourceTerms`. The original M4.4 overload remains unchanged. This allows later secondary/electrical phases to wrap the complete validated M4.4 stack while all thermofluid source terms still reach the same single `PlantNetworkOrchestrator` integration. Generator electromagnetic loading itself remains mechanical and is injected through the nested M4.2 rotor-load seam, not as a fake thermofluid term.
