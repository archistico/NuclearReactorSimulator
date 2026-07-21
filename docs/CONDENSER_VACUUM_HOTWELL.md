# Condenser, Vacuum & Hotwell

## Purpose

M4.3 adds the first explicit condenser model downstream of the validated M4.2 turbine exhaust seam.

The model is deliberately educational and lumped. It provides the physical ownership boundaries required by later condensate/feedwater and control milestones without introducing a second fluid inventory integrator or pretending to be a detailed surface-condenser design code.

## Canonical topology

Each M4.2 turbine stage group must discharge to exactly one M4.3 condenser:

```text
M4.2 turbine stage group
        |
        v
canonical exhaust / steam-space fluid node
        |
        | condensation + heat rejection
        v
canonical hotwell fluid node
        |
        v
M4.4 condensate/feedwater train

external cooling-water/environment boundary
        ^
        |
        +--- rejected condenser heat
```

`CondenserSystemDefinition` composes:

- the validated `TurbineExpansionSystemDefinition`;
- one `CondenserDefinition` per turbine stage group;
- one `CondenserCoolingBoundaryDefinition` per condenser.

The condenser steam-space node must be exactly the stage group's canonical M4.2 exhaust node. The hotwell is an existing canonical `FluidNodeDefinition`, so condensate mass and energy remain part of `PlantState` and are integrated by the normal plant-network orchestration boundary.

## Condensation model

For each condenser step, the solver reads only the committed plant state.

The maximum condensable mass flow is limited by all of the following:

1. the condenser definition's maximum condensation mass flow;
2. vapor mass available in the committed steam-space inventory;
3. the heat-rejection capacity supplied by the cooling boundary;
4. a strictly positive residual steam-space inventory required by the canonical fluid-node invariant.

The current coarse vapor-availability projection is:

- superheated vapor: condensable fraction = 1;
- saturated mixture: condensable fraction = committed vapor quality;
- subcooled liquid: condensable fraction = 0.

No hidden controller changes the cooling capacity or condensation rate.

## Mass and energy accounting

Condensation is staged as an internal transfer:

```text
steam space  --mass-->  hotwell
```

For the actual condensation mass flow `m_dot`:

- the steam-space node loses `m_dot` and `u_steam * m_dot`;
- the hotwell gains `m_dot` and `u_hotwell * m_dot`;
- the difference `(u_steam - u_hotwell) * m_dot` is declared as signed external heat rejection.

Therefore:

- total plant mass exchange caused by the condenser is zero;
- rejected heat leaves the modeled plant boundary explicitly as negative `ExternalPower`;
- M3 + M4.1 + M4.2 + M4.3 thermofluid balances still reach exactly one `PlantNetworkOrchestrator` integration.

No conservation residual is corrected or hidden.

## Vacuum and condenser pressure

M4.3 does not maintain a separate synthetic "vacuum state".

The condenser steam-space pressure is the pressure resolved from the canonical exhaust-node conserved inventory after the single plant-network integration. As condensation removes steam-space mass/energy, the thermodynamic closure resolves the new pressure and phase.

Snapshots expose:

- initial and final absolute steam-space pressure;
- initial and final vacuum below standard atmosphere;
- temperature, phase and vapor quality;
- condensable vapor fraction and available condensable mass;
- inventory-, thermal- and definition-limited condensation rates.

This keeps pressure/vacuum dynamics tied to conserved inventory instead of an independent empirical pressure integrator.

## Hotwell inventory

The hotwell is a normal canonical fluid node.

Snapshots expose its initial/final:

- mass;
- temperature;
- phase.

M4.3 only establishes and fills the condensate inventory. Pumps, heaters/deaeration and replacement of the simplified M3 feedwater source belong to M4.4.

## Cooling-water/environment boundary

`CondenserCoolingBoundaryDefinition` is a replaceable seam.

For M4.3 the per-step input is only an available heat-rejection power. The condenser uses no more than that capacity and exposes used/unused power in the snapshot.

A later higher-fidelity cooling-water or environmental model can replace this boundary input without changing condenser ownership of:

- turbine exhaust;
- condensation;
- hotwell inventory;
- plant energy accounting.

## Determinism and ownership

M4.3 preserves the project rules:

1. all condenser calculations read one committed state per step;
2. condenser logic produces source terms and diagnostics, not mid-step state mutation;
3. all fluid/thermal inventories are integrated exactly once by `PlantNetworkOrchestrator`;
4. M4.2 remains the sole integrator of turbine rotor mechanical state;
5. cooling heat rejection is explicit signed external power;
6. no wall-clock dependency, hidden controller or automatic trip logic is introduced.

## Deliberate simplifications

M4.3 does not yet model:

- detailed tube-bundle heat-transfer correlations;
- circulating-water pump hydraulics;
- air ingress, ejectors or vacuum pumps;
- non-condensable gas partial pressure;
- condensate subcooling maps;
- hotwell level geometry/control;
- condensate/feedwater pumps or heaters;
- turbine wet-steam blade-stage detail.

Those may be introduced only through later roadmap milestones without violating the canonical seams established here.

## M4.4 downstream condensate return

After M4.3 validation, M4.4 takes ownership of the hotwell outlet path. Each condensate/feedwater train references a canonical M4.3 condenser hotwell and routes that inventory through existing plant pumps to a canonical feedwater inventory and then to an M3 steam-drum feedwater target.

Condenser condensation remains exactly as defined by M4.3. M4.4 adds no second condenser or hotwell inventory; it consumes the same canonical state through the normal plant-network hydraulic graph.
