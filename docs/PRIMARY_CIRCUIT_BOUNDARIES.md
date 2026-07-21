# Primary-Circuit Feedwater & Steam Boundary Interfaces

M3.7 introduces explicit temporary external boundaries around the M3 primary circuit so the model can exchange mass and energy before the M4 secondary cycle exists.

These boundaries are deliberately replaceable. They are not hidden reservoirs, procedural steady-state corrections, or permanent shortcuts around the plant model.

## Topology

Each `SteamDrumDefinition` owns exactly one:

- `FeedwaterBoundaryDefinition`, targeting the drum's canonical inventory node;
- `SteamExportBoundaryDefinition`, sourcing the drum's canonical steam-outlet node.

`PrimaryCircuitBoundarySystemDefinition` validates these mappings eagerly and requires exactly one feedwater source and one steam-export sink per drum.

No boundary definition creates a new fluid inventory. All mass and energy enter or leave through existing canonical `PlantDefinition` fluid nodes.

## Per-step inputs

`PrimaryCircuitBoundaryInputs` contains a complete canonical input set for one deterministic solve.

Feedwater input provides:

- non-negative controllable mass flow;
- explicit incoming specific internal energy.

Steam-export input provides:

- non-negative controllable export mass flow.

The exported steam specific internal energy is read from the committed canonical steam-outlet node. This preserves committed-state semantics and prevents the boundary from inventing an unrelated discharge enthalpy/energy state.

## Signed external accounting

`PlantNetworkSourceTerms` now declares both:

- signed `ExternalMassFlowRate`;
- signed `ExternalPower`.

Sign convention:

```text
positive = enters modeled plant boundary
negative = leaves modeled plant boundary
```

Therefore:

```text
feedwater:
  + mass flow to drum inventory
  + energy rate = feedwater specific internal energy × feedwater mass flow

steam export:
  - mass flow from steam-outlet node
  - energy rate = committed outlet specific internal energy × export mass flow
```

Internal M3.6 drum separation still declares zero external mass flow and zero external power because it only redistributes inventory inside the modeled boundary.

## Plant-network audit

M3.7 extends the M3.2 audit contract so external mass is as explicit as external power.

The audit now exposes:

- `ExpectedExternalMassFlowRate`;
- `SupplementalExternalMassFlowRate`;
- `BalanceMassRateResidualKilogramsPerSecond`;
- existing mass closure residual over the integration interval;
- signed supplemental external power and energy closure residuals.

This distinction matters because simply integrating whatever node balances were supplied cannot prove that an internal source-term set is conservative. The accumulated node mass rate must equal the declared external mass exchange.

## Composition

`PlantNetworkSourceTerms.Combine(...)` allows independently solved staged contributions to be accumulated before the single M3.2 integration boundary.

M3.8 composes these boundaries conceptually as:

```text
fuel/channel nuclear source terms
        +
steam-drum internal separation terms
        +
feedwater/steam external boundary terms
        ↓
PlantNetworkOrchestrator
        ↓
exactly one integration per conserved inventory
```

The boundary solver never mutates `PlantState` and never integrates inventories.

## Deliberate simplifications

M3.7 does not yet implement:

- feedwater pumps, heaters or deaeration;
- turbine admission or steam-line pressure-drop dynamics;
- condenser/hotwell return;
- automatic drum-level control;
- valve actuator dynamics for feedwater or steam admission;
- safety/relief valves;
- a closed secondary cycle.

M4 replaces these simplified external boundaries with explicit turbine-island and feedwater-train components while preserving the same canonical mass/energy accounting boundary.

## M4.1 transition of the steam-export seam

M4.1 does not delete `SteamExportBoundaryDefinition`; it reuses it as the canonical identity of each steam-drum outlet seam.

When `MainSteamNetworkInputs` is active, every corresponding M3 `SteamExportBoundaryInput` must be exactly zero. The physical downstream path is then the canonical M4.1 main-steam pipe/header and admission-valve topology, ending at the temporary turbine-admission boundary.

Feedwater remains an active M3.7 external source through M4.1–M4.3. M4.4 replaces that simplified mass source with the canonical condensate/feedwater return train while retaining the M3 boundary as a semantic target seam.

## M4.4 replacement of the feedwater source

M4.4 preserves each `FeedwaterBoundaryDefinition` as a canonical semantic target seam but disables its temporary external mass source during closed secondary-cycle operation.

`CondensateFeedwaterSystemInputs` requires every legacy `FeedwaterBoundaryInput.MassFlowRate` to be exactly zero. Actual feedwater mass then arrives through canonical plant pumps from the M4.3 hotwell path, so the M3 steam-drum target is reused without double mass or energy accounting.

The old boundary definition is therefore not deleted; ownership of mass supply is replaced through a backward-compatible seam.
