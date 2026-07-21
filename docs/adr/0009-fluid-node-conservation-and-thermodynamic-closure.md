# ADR 0009 — Separate fluid-node conservation from thermodynamic closure

## Status

Accepted for M1.2.

## Context

The first physical fluid primitive must conserve mass and energy, remain deterministic, and later support increasingly credible water/steam behavior.

Deriving pressure and temperature from mass, internal energy and volume requires a thermodynamic property model. Implementing a placeholder equation of state in M1.2 would risk coupling the permanent node architecture to deliberately temporary physics.

## Decision

A fluid node is split into:

- immutable geometry/identity (`FluidNodeDefinition`);
- conserved extensive inventory (`FluidNodeInventory`: mass and internal energy);
- intensive closure variables (`FluidThermodynamicState`: pressure and temperature).

Density and specific internal energy are derived, never duplicated as independent stored state.

`FluidNodeIntegrator` integrates signed net mass and energy rates over an explicit deterministic interval. It then delegates pressure/temperature resolution to `IFluidThermodynamicModel`.

M1.2 defines the closure interface but provides no production equation of state. The simplified water/steam model planned for M1.7 will implement the appropriate thermodynamic behavior.

A populated fluid node must retain strictly positive mass. Candidate depletion to zero or negative mass fails explicitly before closure.

## Consequences

Positive:

- conservation logic is independent from property-model fidelity;
- no fake equation of state becomes architectural debt;
- future water/steam models can be substituted behind a stable interface;
- derived density/specific energy cannot drift from conserved state;
- deterministic fixed-step composition remains straightforward.

Trade-offs:

- M1.2 cannot independently predict pressure or temperature without a supplied thermodynamic model;
- network components must wait for later milestones to define physically meaningful flow and transported-energy contributions;
- vacuum/boundary reservoirs require explicit future concepts rather than abusing an empty populated node.
