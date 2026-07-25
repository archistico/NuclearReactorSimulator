# Reference Plant Scale Contract

## Status

**DECISION ACCEPTED IN E.1 — 10 MWe CURRENT-V2 TARGET; E.2 + HOTFIX 1 IMPLEMENTED CANDIDATE**

This contract records the accepted scale decision. E.2 applies it only to current-v2 sustained profiles; historical/default definitions retain legacy scale and generation-only coupling. The current candidate is focused-audit green but remains unpromoted until the complete long-running and operational-envelope gates pass.

## Current candidate contract

- generator nameplate: 10 MWe;
- normal sustained point: 5 MWe (50%);
- rated speed / rotor inertia: 3,000 rpm / 1,000 kg·m²;
- inertia constant: approximately 4.934802 s;
- full-load governor reference rise: 1.5 rpm, preserving 0.75 rpm at 5 MWe;
- current-v2 grid mode: bidirectional; legacy/default: generation-only;
- current-v2 electrical presentation: -10..+10 MWe;
- operator requested-load range: 0..10 MWe with the existing 5 MWe command increment.

## Pre-migration problem record

The intended current-v2 5 MWe operating point combines values associated with different apparent plant scales:

- requested electrical output near 5 MWe;
- generator maximum electrical power 1,000 MW;
- rotor moment of inertia 1,000 kg·m²;
- condenser initial surface-transfer point 24.5 MW at 40 °C / 20 °C;
- current A.2 candidate installed cooling-boundary ceiling 40 MW;
- current A.2 candidate condenser maximum mass flow 20 kg/s;
- turbine flow and work sized for a low-megawatt educational secondary cycle.

These values influence governor droop authority, synchronizing correction limits, electromagnetic torque, protection thresholds, rotor acceleration and the interpretation of performance metrics. Changing one value in isolation would modify several validated mechanisms at once.

## A.3 measured evidence

The current-v2 definitions imply:

- 49.348 MJ of stored rotor energy at 3,000 rpm;
- inertia constant `H = 0.049348 s` against the configured 1,000 MW nameplate;
- inertia constant `H = 4.934802 s` against a 10 MW reduced-scale reference;
- 5 MW request equal to 0.5% of the configured nameplate;
- 0.75 rpm governor-reference rise at the current 5 MW request;
- approximately 30.396 rpm/s rotor acceleration per 1 MW uncompensated torque imbalance at rated speed;
- 10 MW synchronizing correction equal to 1% of configured nameplate but 200% of current dispatch.

These values are frozen by `ReferencePlantScaleAuditTests` and fully tabulated in `REFERENCE_PLANT_SCALE_EVIDENCE.md`.

## Provisional direction

Static evidence favors **Option B — reduced-scale educational unit**, because the present rotor yields a conventional multi-second inertia constant near a 10 MW reference and the turbine/condenser path is already low-megawatt in scale. This is not authorization to change `MaximumElectricalPower` alone. Nameplate, inertia, droop, grid coupling, protections, UI ranges and versioned baselines must migrate together.

## Decision record

E.1 selected Option B. The alternatives below are retained as the decision history:

### Option A — full-scale unit at very low load

The reference generator remains a nominal 1 GW-class machine operating at approximately 0.5% load. The rotor inertia, turbine/condenser capacity, droop policy and low-load operating envelope must then be scaled consistently with that interpretation.

### Option B — reduced-scale educational unit

The reference plant becomes an intentionally scaled approximately 5–10 MWe trainer. Generator nameplate power, rotor inertia, turbine/condenser capacities, protection ranges and performance terminology must be consistently rescaled while retaining dimensionally correct laws.

A hybrid interpretation is not acceptable unless every non-geometric scaling rule is explicit.

## Evidence required for promotion

- effective inertia constant at the current operating point;
- rotor acceleration/deceleration under known torque imbalance;
- droop reference displacement across the supported load range;
- synchronizing correction power relative to machine rating;
- turbine mass-flow and shaft-power capability map;
- condenser heat-rejection and mass-flow margin;
- intended educational load range and future demand-tracking range;
- replay/reference-baseline migration impact.

## Affected owners

A scale decision may require coordinated changes to:

- `SynchronousGeneratorDefinition`;
- turbine rotor inertia and rated speed contracts;
- generator/grid coupling and motoring limits;
- governor droop normalization;
- protection thresholds and supervision;
- condenser/turbine design capacities;
- UI nameplate/range metadata;
- reference-validation trajectories and versioned seeds.

## Remaining gate

The implemented current-v2 migration may not be promoted until:

- A.2 validation evidence is available;
- turbine capability and controlled rotor-response evidence are recorded;
- both explicit 60-second journeys and the 300-second operational-envelope trajectory pass;
- signed generation/motoring and replay/checkpoint behavior are recorded;
- HMI and protection-supervision consequences are manually reviewed.

Changing only `MaximumElectricalPower` remains explicitly prohibited.
