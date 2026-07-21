# Fluid Node Model

## Purpose

M1.2 introduces the first physical plant primitive: a deterministic lumped fluid control volume.

The model intentionally separates conserved inventory from thermodynamic closure so later milestones can increase fidelity without rewriting node ownership or conservation logic.

## State decomposition

```text
FluidNodeDefinition
  id
  fixed control volume
        |
        v
FluidNodeState
  + FluidNodeInventory
      mass
      internal energy
  + FluidThermodynamicState
      absolute pressure
      absolute temperature
        |
        +--> density                = mass / volume
        +--> specific internal energy = internal energy / mass
```

Density and specific internal energy are derived rather than stored, preventing duplicated state from drifting out of agreement.

## Conserved versus closure variables

M1.2 treats these as the conserved extensive inventory:

- mass;
- internal energy.

Pressure and temperature are intensive closure variables. They are resolved through `IFluidThermodynamicModel` after a candidate inventory has been integrated.

M1.2 deliberately provided no production water/steam equation of state. M1.7 now implements the first simplified production closure behind the same `IFluidThermodynamicModel` seam.

This preserved the permanent architecture while allowing thermodynamic fidelity to arrive later without rewriting node ownership or conservation.

## Balance integration

`FluidNodeBalance` contains signed net rates:

```text
NetMassFlowRate
NetEnergyRate
```

Positive values add inventory. Negative values remove inventory.

For a deterministic interval `dt`, `FluidNodeIntegrator` computes the candidate conserved state and only then requests thermodynamic closure:

```text
previous node state
      +
net signed rates over dt
      |
      v
candidate mass + internal energy
      |
      v
IFluidThermodynamicModel.Resolve(...)
      |
      v
new immutable FluidNodeState
```

The balance is intentionally already net. M1.2 does not yet decide how pipes, valves, pumps, heat transfer or advective energy calculate their individual contributions.

## Safety rules

A populated fluid node requires:

- a non-empty identifier;
- fixed control volume > 0;
- fluid mass > 0;
- absolute pressure > vacuum;
- absolute temperature > 0 K;
- finite strongly typed quantities inherited from M1.1.

If a balance would reduce mass to zero or below, `FluidNodeIntegrator` throws `FluidNodeDepletionException` before thermodynamic resolution.

A fully evacuated/vacuum boundary is not represented as a populated fluid node in M1.2. Explicit boundary-node concepts can be introduced when the network model requires them.

## Determinism and immutability

`FluidNodeState` and its components are immutable records. Integration creates a new candidate state and never mutates the committed input state.

The integrator reads no wall-clock time. It accepts an explicit deterministic `TimeSpan`, making it directly composable with the fixed-step runtime from M0.

## Explicitly deferred

M1.2 does not include:

- pipes or pressure-drop equations;
- flow-network solving;
- valves;
- pumps;
- heat transfer;
- enthalpy transport rules;
- viscosity/friction correlations;
- vacuum/boundary reservoirs.
