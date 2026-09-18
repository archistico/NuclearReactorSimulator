# M10 Final VR2 — R3 Reference-Consistent Seed Integration Planning 1

## Purpose

Plan the narrowest opt-in production seam for integrating the returned 12-node reference-consistent conserved-inventory candidate without mutating canonical exact-v9.

## Frozen predecessor evidence

Candidate Construction 1 established that all 12 frozen exact-v9 target states are representable by the existing C4 inverse, with all phases matching and sub-Pascal-to-low-Pascal pressure/head residuals.

The candidate mass/internal-energy vector is now frozen evidence.

## Selected production seam

Implementation 1 may modify exactly three production files:

1. `OperationalFluidNodeSeed.cs`
2. `ColdShutdownInitialConditionFactory.cs`
3. `DesktopSustainedGenerationInitialConditionFactory.cs`

### 1. Extend the internal authored-state seam

Add:

`OperationalFluidNodeSeed.ConservedInventory`

with explicit:
- `MassKilograms`;
- `InternalEnergyJoules`;
- target/previous pressure;
- target/previous temperature;
- target/previous phase;
- target/previous vapor quality where applicable.

The extra thermodynamic fields are a branch/identity hint for the authored seed seam; the authoritative thermodynamic state must still be produced by the active production closure resolver from the conserved inventory.

### 2. Resolve conserved inventory through the active closure

`ColdShutdownInitialConditionFactory.ResolveAuthoredFluidNode(...)` shall support the new subtype by:

- validating finite positive mass and finite internal energy;
- constructing the exact `FluidNodeInventory`;
- constructing the explicit previous/target `FluidThermodynamicState`;
- calling the existing `thermodynamicModel.Resolve(...)`;
- returning the resolved `FluidNodeState`.

No C4-specific branch logic may be added to the Application factory.

### 3. Add a new internal opt-in exact-v9-equivalent candidate factory

Add a new internal method to `DesktopSustainedGenerationInitialConditionFactory`, separate from and without modifying the body/identity of:

`CreatePostMoistureEquilibriumCandidateRuntimeEngine(TimeSpan runtimeStep)`

The new candidate factory shall:
- use the same non-fluid exact-v9 constants and controls;
- use closure mode `ReferenceConsistentTabulatedInverseDomain`;
- use the frozen 12-node conserved-inventory vector;
- retain the same deterministic 20 ms seed-preconditioning duration;
- remain internal and non-default.

Canonical exact-v9 remains mode 1 and byte-for-byte source-preserved.

## Implementation 1 fast qualification

The implementation gate must complete before another 120 s R3 run.

It shall prove:

- exact 12-node frozen candidate vector embedded in the new opt-in factory;
- raw candidate resolves with 12/12 phase matches;
- after the canonical 20 ms / 2-step preconditioning, no node is unresolved/non-finite;
- first 100 Running steps complete;
- first-100-step exact-v9 health envelope has zero violations;
- rollback count remains zero through the first 100 Running steps;
- canonical exact-v9 factory/source remains unchanged;
- mode 0/mode 1 historical tests and ordinary Release suite pass.

A fast Implementation 1 PASS does **not** close R3. It authorizes only a full R3 short requalification on the new candidate.

## Forbidden changes

Implementation 1 must not modify:
- `ReferenceConsistentTabulatedInverseResolver`;
- `NRSVR2C4.v1.bin`;
- water/steam thresholds/tolerances;
- H.13/H.28 hydraulic policy;
- canonical exact-v9 factory method body;
- default closure mode;
- R3 health envelope.

## Downstream sequence

`Seed Integration Implementation 1 -> R3 Short Requalification 3 -> R4 Planning 1`

R4 remains blocked until the full 120 s R3 gate returns/adjudicates PASS.
