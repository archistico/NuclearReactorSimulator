# Heat Transfer Model

M1.6 introduced deterministic lumped heat transfer while intentionally leaving water/steam closure to the separate M1.7 thermodynamic-model seam.

## Design goals

- conserve energy exactly across internal thermal links;
- keep stored energy, heat-transfer rate and temperature as distinct concepts;
- represent solid/wall thermal inertia explicitly;
- allow fluid nodes to receive/remove thermal power through their existing energy balance;
- keep external heat sources explicitly visible at the system-energy boundary;
- remain deterministic and compatible with the M0 fixed-step runtime.

## Thermal quantities

Two strongly typed SI quantities are added:

```text
HeatCapacity        J/K
ThermalConductance  W/K
```

`HeatCapacity` and `ThermalConductance` are non-negative value types. Definitions that require a functioning thermal body or link enforce values strictly greater than zero.

Selected dimensional relationships:

```text
HeatCapacity × TemperatureDifference -> Energy
ThermalConductance × TemperatureDifference -> Power
Power × time -> Energy                  (existing M1.1 relationship)
```

## Lumped thermal bodies

A `ThermalBodyDefinition` owns identity and constant lumped heat capacity.

A `ThermalBodyState` owns only conserved stored thermal energy. Temperature is derived:

```text
T = E / C
```

with the energy reference set at absolute zero for this simplified constant-capacity model.

This deliberately avoids storing independent energy and temperature values that could drift apart.

## Heat transfer links

`HeatTransferDefinition` connects two named thermal domains and owns one lumped thermal conductance.

`HeatTransferSolver` evaluates:

```text
Qdot = G × (Tfrom - Tto)
```

The endpoint order defines only the positive sign convention. If the temperature gradient reverses, heat flow reverses naturally.

Every internal link returns exactly opposite endpoint balances:

```text
FromDomainBalance = -Qdot
ToDomainBalance   = +Qdot
```

Therefore internal heat transfer cannot create or destroy total system energy.

## Fluid coupling

A heat-transfer result can be applied to an existing fluid node by adding its heat rate to `FluidNodeBalance.NetEnergyRate` while leaving mass flow at zero.

```text
wall energy loss
      ↓
HeatTransferSolver
      ↓
fluid internal-energy gain
```

M1.6 intentionally does not own the relationship between fluid internal energy and temperature. `IFluidThermodynamicModel` remains the thermodynamic closure boundary, and M1.7 now provides the first simplified water/steam implementation without changing the heat-transfer solver.

## External heat sources

`HeatSourceDefinition` and `HeatSourceState` model an explicit external thermal-power input.

An enabled source adds its rated thermal power to the target-domain balance. A disabled source adds zero.

Unlike an internal heat-transfer link, this energy is not balanced by an opposite endpoint because it crosses the simulated system boundary.

## Numerical scope

M1.6 uses explicit fixed-step integration and constant lumped properties. It does not yet model:

- temperature-dependent material heat capacity;
- area-specific convection coefficients;
- conduction PDEs or spatial wall meshes;
- radiation heat transfer;
- boiling/condensation heat-transfer correlations;
- critical heat flux;
- fuel/cladding gap models.

Those may be introduced only when required by later plant/reactor milestones.
