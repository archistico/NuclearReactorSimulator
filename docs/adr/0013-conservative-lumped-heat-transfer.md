# ADR 0013 — Conservative lumped heat transfer with explicit thermal inventories

## Status

Accepted for M1.6 baseline candidate.

## Context

The simulator now has conserved fluid mass/internal-energy inventories and a deterministic hydraulic network. Before implementing water/steam phase closure, the engine needs a general way to represent wall thermal inertia, heat exchange between domains and external thermal-power input.

A premature detailed heat-transfer or boiling model would couple M1.6 to assumptions that belong to later reactor and phase-model milestones.

## Decision

1. Represent solid/wall thermal inertia using immutable lumped `ThermalBodyState` objects.
2. Store conserved thermal energy as state and derive temperature from constant `HeatCapacity`.
3. Represent passive internal thermal coupling using positive `ThermalConductance` and the signed relation `Qdot = G × (Tfrom - Tto)`.
4. Produce exactly equal-and-opposite energy-rate balances for internal heat-transfer links.
5. Couple heat into fluid nodes only through their existing `FluidNodeBalance.NetEnergyRate` boundary.
6. Represent external heat sources explicitly so their energy input remains visible at the system boundary.
7. Keep all calculations stateless/deterministic before fixed-step integration.
8. Do not introduce water/steam equations of state, phase-change correlations or detailed spatial conduction in M1.6.

## Consequences

- internal heat transfer conserves total energy by construction;
- wall temperature cannot drift independently from wall energy;
- fluids can participate in heat exchange before M1.7 without embedding a fake thermodynamic closure;
- later temperature-dependent material properties can replace the constant-capacity model behind compatible domain boundaries;
- explicit integration requires physically sensible timestep/conductance/capacity combinations, which later solver-hardening milestones may validate more deeply.
