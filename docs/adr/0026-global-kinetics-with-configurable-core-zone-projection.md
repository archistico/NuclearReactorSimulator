# ADR 0026: Keep point kinetics global while core zones provide configurable spatial projection

- Status: Accepted
- Milestone: M3.3

## Context

M2 established a validated global point-reactor kinetics model. M3 now needs spatial structure for an RBMK-like core, but introducing an unvalidated multi-node neutron solver at the same time as primary-circuit composition would couple several major fidelity changes.

## Decision

M3.3 keeps `PointKineticsSolver` global and introduces `AggregatedCoreDefinition` / `AggregatedCoreState` as a separate spatial projection boundary.

Global fission thermal power is partitioned by normalized zone power fractions. Zones reference canonical plant thermal and fluid domains for local diagnostics; they do not own duplicate mass, energy, pressure or temperature inventories.

Logical zone coordinates are configurable and do not imply a fixed 3x3 grid.

## Consequences

- Existing M2 kinetics remains unchanged and validated.
- Core spatial topology can evolve independently of neutron-kinetics fidelity.
- M3.4 can attach fuel-channel groups to zones without redefining global power.
- Future spatial kinetics can replace or evolve the power-share model behind the same zone identity boundary.
- M3.3 power shape is prescribed state, not an independently solved neutron field.
