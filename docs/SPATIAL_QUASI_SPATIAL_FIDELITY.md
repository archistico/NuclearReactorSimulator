# Spatial / Quasi-Spatial Fidelity Refinement

## Purpose

M9.4 upgrades the existing aggregated-core boundary from a prescribed power-share projection into an optional deterministic quasi-spatial feedback and power-shape layer while keeping the validated global point-reactor kinetics model.

The key distinction is:

```text
spatial diagnostics / weighting / shape redistribution
!=
independent spatial neutron kinetics
```

## Existing baseline reused

M3.3 already provides:

- arbitrary canonical core-zone ids;
- arbitrary logical coordinates;
- normalized dynamic `AggregatedCoreState` power fractions;
- references from each zone to canonical fuel, structure and coolant domains;
- deterministic total-power partitioning.

M3.4 already consumes those zone powers through equivalent channel groups and remains responsible for staged heat deposition into canonical plant inventories.

M2.6/M2.7 already provide validated linear temperature and void reactivity formulas.

M9.4 composes these owners. It does not recreate them.

## M9.4 definition

`QuasiSpatialCoreFeedbackDefinition` references one canonical `AggregatedCoreDefinition` and configures:

- fuel-temperature feedback definition;
- coolant-temperature feedback definition;
- void feedback definition;
- power-shape sensitivity;
- power-shape relaxation time;
- optional explicit symmetric zone couplings.

The M5.3 reactor/primary definition may opt into exactly one such profile.

## Why coupling is explicit

Logical `CoreZoneCoordinate` was deliberately defined in M3.3 as metadata, with no assumption of:

- rectangular topology;
- contiguous coordinates;
- fixed dimensions;
- physical distance.

M9.4 therefore does not infer neighbours from row/column values. Coupling relationships are explicit versioned model configuration.

## Committed-state calculation

For each zone, the solver reads one committed state:

```text
fuel temperature
coolant temperature
coolant phase/thermodynamics → void fraction
committed zone power fraction
```

It evaluates the existing feedback equations and creates one local total feedback value.

The global quasi-spatial reactivity contribution is:

```text
sum(current zone power fraction × local zone feedback)
```

This produces exactly one scalar passed through the existing M5.3 non-rod-reactivity composition seam before the existing global `PointKineticsSolver`. When the profile is enabled, callers must not duplicate the same configured fuel/coolant/void terms inside the external `NonRodReactivity` scalar; that seam is reserved for distinct contributions.

## Power-shape path

The same local values are separately used to evolve only the spatial projection:

1. explicit neighbour coupling smooths the local shape-driving signal;
2. each coupled signal is compared with the current global weighted mean;
3. configured sensitivity produces a positive target weight relative to the committed zone share;
4. all target weights are normalized;
5. committed shares relax toward the target according to the configured relaxation time;
6. the resulting normalized `AggregatedCoreState` is the candidate next-step shape.

The current step still uses the committed power shape for physical heat allocation. This avoids hidden same-step nonlinear iteration.

## Conservation and ownership

M9.4 stores no:

- local mass;
- local thermal energy;
- local pressure;
- local neutron population.

The candidate core state contains only normalized power fractions.

Total fission power remains generated once by global kinetics/fission power and partitioned by the canonical M3.3/M3.4 chain. The final-zone residual logic still closes total allocated power exactly under the existing deterministic arithmetic order.

## Opt-in compatibility

The quasi-spatial definition is nullable.

Absent configuration means the legacy path is retained. In particular, validated M7/M8/M9.3 initial-condition versions are not silently modified merely because M9.4 code exists.

A later versioned scenario/plant profile may opt into a multi-zone definition and M9.4 configuration explicitly.

## Higher-resolution cores

M9.4 does not encode a zone count. Tests exercise arbitrary/non-grid layouts and a five-zone aggregation.

A higher-resolution full-plant configuration must still provide matching canonical M3 topology and fuel-channel groups. M9.4 never fabricates physical channel inventories just to increase visual/spatial resolution.

## Failure semantics

M9.4 fails closed when:

- the core/plant definitions are not the exact canonical references;
- required coolant phase information is unspecified;
- coupling definitions reference unknown zones;
- duplicate coupling pairs exist;
- incident coupling fractions exceed unity;
- normalization/relaxation produces a non-finite or invalid result.

No fallback to fabricated local values is allowed.

## Explicit non-goals

- neutron diffusion/transport;
- local kinetics;
- spatial xenon;
- historical RBMK calibration claims;
- automatic scenario outcome shaping;
- geometry inferred from UI coordinates.
