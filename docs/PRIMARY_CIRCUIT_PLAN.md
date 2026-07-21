# M3 Primary Circuit Integration Plan

## Purpose

M3 converts the validated M1 physical primitives and M2 reactor-physics models into the first coherent plant-level system: a configurable **RBMK-like primary circuit**.

M3 is intentionally not a full RBMK replica and not a full-scope operator-training simulator. The objective is an educational, deterministic, conservative and incrementally refinable primary-circuit model.

## Guiding decision

Do not implement the primary circuit as one monolithic reactor update method.

The plant must be composed from explicit definitions/states/solvers and must preserve the validated seams already present in the codebase:

```text
PlantDefinition / PlantState
        │
        ├── core zones
        ├── channel groups
        ├── fluid nodes
        ├── pipes / valves / pumps
        ├── thermal bodies / heat links
        ├── steam drums / separators
        └── boundary interfaces
                │
                ▼
      deterministic staged plant kernel
                │
                ▼
          immutable PlantSnapshot
```

## Fixed-step orchestration rule

For every conserved network domain, one fixed step follows the same staging rule:

```text
1. READ committed state
        ↓
2. SOLVE component transfers from that common state
        ↓
3. ACCUMULATE all signed balances per inventory
        ↓
4. INTEGRATE each inventory exactly once
        ↓
5. CLOSE thermodynamic/derived state
        ↓
6. VALIDATE plant invariants
        ↓
7. COMMIT candidate state + snapshot
```

Forbidden pattern:

```text
solve component A → mutate node X
solve component B → read already-mutated node X
```

That pattern makes component enumeration order part of the physics and is therefore not allowed.

See ADR 0023.

## Initial model resolution

### Core neutronics

M3 keeps the validated **global point-kinetics** model from M2.3.

Spatial detail is introduced only as a coarse distribution/feedback layer:

- global neutron population drives total fission power;
- configurable zone weights distribute power into coarse zones;
- zones evolve their own fuel/structure/coolant thermal-hydraulic state;
- zone temperature, void and xenon states generate named reactivity contributions;
- all contributions return to the existing `ReactivityModel`.

A reference nine-zone/3×3 layout is a reasonable first configuration, but the generic engine must accept other zone counts/topologies.

### Fuel channels

Do not model every physical channel individually in M3.

Use configurable **fuel-channel groups** that represent many similar channels. Each group can own or reference:

- inlet/outlet coolant control volumes;
- hydraulic resistance;
- fuel/structure thermal bodies;
- heat-transfer links;
- power/deposition weighting;
- diagnostic multipliers such as represented channel count.

This keeps the model operable by one player and computationally manageable while preserving a path to higher resolution later.

## Detailed milestone sequence

### M3.1 — Plant Composition & Topology Baseline — VALIDATED

Validated with composition and validation only:

- `PlantDefinition`;
- `PlantState`;
- deterministic registries/indexes;
- canonical IDs;
- topology validation;
- plant snapshot skeleton;
- no new equations.

Acceptance emphasis: duplicate/missing references fail fast, enumeration order is canonical, and a plant can be created/snapshotted headlessly.

### M3.2 — Deterministic Multi-Component Network Orchestration — VALIDATED

Introduce the plant-level gather/accumulate/integrate pipeline.

Acceptance emphasis:

- multiple pipes/valves/pumps can contribute to shared nodes;
- every connection reads the same committed state;
- each fluid node is integrated once;
- reversing component registration order produces the same result;
- total mass/energy audits are available.

Validated through `PlantNetworkOrchestrator`, `PlantNetworkStepResult` and `PlantNetworkAudit`, with explicit tests for committed-state parallel solving, exactly-once fluid closure, shuffled-registry equivalence and runtime pulse segmentation.

### M3.3 — Aggregated Core-Zone Model — VALIDATED

Introduce configurable zones and global-to-zone power distribution.

Each zone initially owns coarse state sufficient for:

- fuel/structure temperature;
- coolant state/void;
- local heat deposition;
- local feedback contributions;
- diagnostics.

Do not introduce spatial neutron diffusion in this milestone.

### M3.4 — Fuel-Channel Group Model — VALIDATED

Compose per-group hydraulic and thermal behavior:

```text
inlet coolant
    ↓
channel-group resistance + heat pickup
    ↓
outlet two-phase coolant
```

Fission/decay heat is partitioned by canonical zone/group shares and emitted as staged source terms into the existing thermal/fluid boundaries. Each group references an existing passive hydraulic pipe; the M3.2 orchestrator remains the only integration boundary.

### M3.5 — Main Circulation System — VALIDATED VIA M3.5.1

Add plant-level circulation topology:

- headers/manifolds;
- main circulation pumps;
- return/downcomer paths;
- parallel channel-group branches.

The solver remains pressure-driven. Pumps add active pressure; they do not impose branch flow directly.

### M3.6 — Steam Drums, Separation & Recirculation — VALIDATED

Add a lumped separator/drum model over canonical plant fluid nodes.

Delivered behaviors:

- channel returns terminate at a dedicated loop return collector / drum inventory;
- the drum maintains canonical water/steam inventory through the existing fluid-node model;
- pressure, temperature, phase, quality, void and normalized liquid level are observable;
- positive committed return flow is separated deterministically by phase/quality;
- separated steam is transferred to a dedicated steam-outlet node;
- separated liquid is recirculated to the MCP suction header;
- separation emits conservative internal source terms and never integrates state directly.

M3.6 is locally validated. Feedwater and exported-steam external boundaries were implemented and locally validated in M3.7.

### M3.7 — Feedwater & Steam Boundary Interfaces — VALIDATED

Before M4 exists, provide explicit replaceable boundaries:

```text
feedwater source → primary circuit → steam export sink
```

Delivered validated behaviors:

- exactly one feedwater boundary and one steam-export boundary per steam drum;
- feedwater targets canonical drum inventory nodes;
- steam export removes inventory from canonical steam-outlet nodes;
- per-step non-negative mass-flow inputs with explicit feedwater specific internal energy;
- committed-state specific energy for exported steam;
- signed external mass-flow and power accounting in `PlantNetworkSourceTerms`;
- explicit balance mass-rate residuals in `PlantNetworkAudit`;
- source-term composition without bypassing M3.2 single integration.

These are not permanent shortcuts. Their interfaces are designed to be replaced by the M4 secondary-cycle components.

### M3.8 — Integrated Primary-Circuit Baseline — VALIDATED

Compose the entire M3 plant and establish a reference operating condition.

Required verification:

- deterministic long run;
- no component-order dependence;
- bounded mass/energy drift;
- stable or explainably evolving pressure/temperature/flow/void states;
- global and per-subsystem diagnostics;
- plant snapshot ready for later instrumentation/UI.

Implemented candidate verification includes a deterministic zero-net-source equilibrium long run for raw numerical-drift detection, explicit staged-source order-independence checks, global mass/energy audit propagation, and immutable subsystem/plant snapshots. Powered or transient operating points are allowed to evolve physically and are measured rather than corrected toward an artificial steady state.

## Plant-step coupling order

The exact full multiphysics ordering may evolve, but M3 should preserve this conceptual staging:

```text
committed plant state
    ↓
operator commands / actuator state for this step
    ↓
reactivity feedback from committed physical state
    ↓
control-rod mechanics + reactivity composition
    ↓
point kinetics
    ↓
fission + decay heat source calculation
    ↓
hydraulic / thermal transfer solves from common committed network state
    ↓
balance accumulation
    ↓
fluid / thermal inventory integration
    ↓
thermodynamic closure
    ↓
I/Xe and other stateful slow dynamics according to their documented committed-input semantics
    ↓
invariants / commit / snapshot
```

Where a subsystem already has documented one-step committed-input semantics, M3 must not silently create hidden same-step nonlinear iteration.

## Diagnostics required before M4

The M3.8 integrated primary-circuit snapshot owns the M3 thermal-hydraulic and thermal-power diagnostics below.
Validated M2 neutron-population, reactivity and iodine/xenon diagnostics remain upstream reactor-physics snapshots and are not recomputed or fabricated by the primary-circuit solver. In particular, local per-zone xenon remains intentionally deferred by the validated M3.3 spatial model; a later full-simulator snapshot may compose those upstream diagnostics without introducing fake local inventories.

At minimum the complete simulator observability surface before operator UI should make available:

- total reactor fission power;
- total decay heat;
- neutron population/reactivity breakdown;
- per-zone power, temperatures, void and xenon;
- per-channel-group mass flow and outlet phase;
- circulation pump speeds/flows/power;
- header/drum pressures;
- drum inventory/level/steam export;
- total feedwater and steam mass flow;
- total plant water/steam mass inventory;
- global energy inventory and source/sink audit.

## Explicit non-goals for M3

M3 does not include:

- turbine expansion physics;
- condenser/vacuum dynamics;
- generator/grid synchronization;
- PI/PID automatic control;
- protection/interlock logic;
- production control-room UI;
- individual full-core channel simulation;
- spatial neutron diffusion/transport;
- licensing-grade two-phase thermal hydraulics.

These remain separate roadmap phases so they can be validated independently.

## M3.3 implementation note

The validated aggregated core-zone baseline establishes configurable canonical zone ids/coordinates, normalized current power shares and deterministic global-to-local fission-power projection. Global point kinetics remains unchanged. M3.4 is now validated and attaches equivalent fuel-channel groups to these zone identities, consuming zone power as staged local heat deposition into canonical fuel/structure/coolant domains.


## M3.4 implementation note — validated

The validated fuel-channel group layer composes M3.3 zones with canonical M3.1/M3.2 plant components rather than creating duplicate channel inventories. Group source terms can carry both fission and decay heat, while hydraulic diagnostics are observed from the committed passive path. `PlantNetworkAudit.SupplementalExternalPower` makes this nuclear input explicit at the plant-network boundary. M3.5.1 validates common suction/pressure headers, return paths and main circulation pumps around these parallel channel-group branches. M3.6 extends each loop with a dedicated return collector/steam-drum inventory and conservative staged steam/liquid separation without introducing a second network integrator. M3.7 adds replaceable feedwater/steam external interfaces with signed mass and energy declarations at that same orchestration boundary and is locally validated. M3.8 composes the complete chain into one committed-state top-level solver, integrated plant snapshot and deterministic headless long-run verification boundary and is locally validated, closing the M3 gate. M4.1 now consumes the validated steam-export seams as canonical main-steam topology connection points.
