# M10 Final VR2 — R3 Requalification 2 Reference-Consistent Operating-Point Seed Replanning 1

## Purpose

Replan R3 after Diagnostic 2 proved that the historical authored exact-v9 seed is not forward/inverse consistent with closure mode 2.

## Frozen facts

- canonical exact-v9 remains the historical mode-1 production identity;
- C4 mode 2 and payload `NRSVR2C4.v1.bin` remain unchanged;
- the legacy forward saturation-property recipe is bitwise common to mode 1 and mode 2;
- those legacy-forward raw inventories resolve differently under C4 on 12/12 nodes;
- the resulting pressure/head displacement is materially large enough to explain the immediate operating-point shift;
- controllers, rollback policy and R3 physical thresholds are not the initiating cause.

## Operating-point target

The reference target for seed reconstruction is the **resolved mode-1 raw-seed thermodynamic state vector**, not the historical authored parameterization itself.

For each of the 12 fluid nodes the target is frozen as:

- pressure;
- temperature;
- phase;
- vapor quality where applicable.

This preserves the already-qualified exact-v9 operating-point coordinates while allowing a closure-specific conserved-inventory representation under C4.

Canonical exact-v9 is not modified.

## Selected next gate

`R3-REFERENCE-CONSISTENT-RAW-SEED-CANDIDATE-CONSTRUCTION1`

This is **test-only candidate construction**, not production implementation.

The gate shall use C# only and shall:

1. construct a C4/reference-consistent conserved inventory candidate for each of the 12 frozen target states;
2. resolve each candidate through the existing production mode-2 resolver;
3. record pressure, temperature, phase and quality residuals against the frozen target state vector;
4. reconstruct the eight hydraulic pressure heads and compare them with the frozen mode-1 target heads;
5. emit the candidate mass/internal-energy vector with deterministic provenance;
6. perform no runtime preconditioning and no dynamic simulation;
7. modify no production source.

The construction gate is evidence-generating. It must not silently introduce acceptance tolerances or productionize the candidate.

## Why explicit conserved-inventory candidate first

A new public forward C4 API would couple the Application factory to the internal reference payload before we know the required candidate vector is materially correct.

A test-only conserved-inventory candidate lets us first establish whether the already-qualified inverse domain can represent the exact-v9 operating point at all, and with what residuals.

Only returned candidate-construction evidence may authorize a later seed-integration planning gate.

## Explicit non-authority

This replanning does not authorize:

- modifying `ReferenceConsistentTabulatedInverseResolver`;
- modifying the C4 payload;
- modifying canonical exact-v9;
- adding a production conserved-inventory seed type;
- creating an exact-v10 identity;
- changing R3 health/ownership thresholds;
- R4 planning or execution.
