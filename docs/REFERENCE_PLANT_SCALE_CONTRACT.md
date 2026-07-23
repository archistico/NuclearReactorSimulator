# Reference Plant Scale Contract

## Status

**OPEN ENGINEERING DECISION — M10.9.4.1; A.2 DOES NOT RESOLVE SCALE**

This document does not itself change constants. A.2 changes condenser capacity headroom only and deliberately does not decide generator/rotor/reference-plant scale.

## Problem

The current validated 5 MWe operating point combines values associated with different apparent plant scales:

- requested electrical output near 5 MWe;
- generator maximum electrical power 1,000 MW;
- rotor moment of inertia 1,000 kg·m²;
- condenser initial surface-transfer point 24.5 MW at 40 °C / 20 °C;
- current A.2 candidate installed cooling-boundary ceiling 40 MW;
- current A.2 candidate condenser maximum mass flow 20 kg/s;
- turbine flow and work sized for a low-megawatt educational secondary cycle.

These values influence governor droop authority, synchronizing correction limits, electromagnetic torque, protection thresholds, rotor acceleration and the interpretation of performance metrics. Changing one value in isolation would modify several validated mechanisms at once.

## Decision to make

Choose and document one coherent interpretation:

### Option A — full-scale unit at very low load

The reference generator remains a nominal 1 GW-class machine operating at approximately 0.5% load. The rotor inertia, turbine/condenser capacity, droop policy and low-load operating envelope must then be scaled consistently with that interpretation.

### Option B — reduced-scale educational unit

The reference plant becomes an intentionally scaled approximately 5–10 MWe trainer. Generator nameplate power, rotor inertia, turbine/condenser capacities, protection ranges and performance terminology must be consistently rescaled while retaining dimensionally correct laws.

A hybrid interpretation is not acceptable unless every non-geometric scaling rule is explicit.

## Evidence required before decision

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

## Gate

No nameplate, inertia, droop or synchronizing-limit correction may be promoted until this contract is resolved and an isolated migration plan is accepted.
