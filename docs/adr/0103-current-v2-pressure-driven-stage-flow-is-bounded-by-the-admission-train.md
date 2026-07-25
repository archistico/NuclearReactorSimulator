# ADR 0103 — Current-v2 pressure-driven stage flow is bounded by the admission train

## Status

Candidate — M10.9.4.1-D.3.2

## Context

The D.3.1 breaker-open audit produced a decisive contradiction: the governing control valve was fully closed, but the current-v2 pressure-driven turbine stage still reported approximately 10.6 kg/s effective flow and 4.24 MW shaft power. The passive rotor loss introduced by D.3.1 therefore could not decelerate the rotor into the governor-controllable band.

The cause was structural. `TurbineStageMassFlowResolver` used only turbine-inlet-to-exhaust pressure difference whenever a stage owned an `ExpansionResistance`. That path bypassed the stop/control/admission valve-flow capacities used by the legacy resolver.

## Decision

For stages with an explicit pressure-driven expansion resistance, resolve two independent capacities from the same committed state:

1. downstream stage capacity from inlet-to-exhaust pressure difference and expansion resistance;
2. upstream admission-train capacity as the minimum positive flow through stop, control and admission valves.

The commanded stage flow is the minimum of those capacities. Any fully closed upstream valve therefore enforces zero stage flow. Definitions without `ExpansionResistance` retain the historical upstream-minimum law unchanged.

The synchronization governor profile is also frozen according to the actual source contract: P=0.5, I=0.02 s^-1, D=0 uses `ProportionalIntegral`, not `ProportionalIntegralDerivative`.

## Consequences

- A closed control, stop or admission valve can no longer coexist with positive current-v2 stage mass flow or shaft power.
- D.3.1 passive mechanical loss can act after steam isolation and provide a real breaker-open deceleration path.
- The earlier D.2 equal-head resistance map remains historical evidence of nominal component sizing, but it must not be interpreted as proof that the former implementation enforced a true series-flow constraint.
- No valve resistance, stage resistance, governor gain, actuator travel rate, droop, protection threshold, generator nameplate or timestep is retuned.
- Legacy/null-expansion-resistance behavior remains unchanged.

## UI preservation

The supplied continuation archive still contained the M10.9.3-specific PLANT renderer while subsystem workspaces used the later engineering-schematic visual grammar. D.3.2 aligns PLANT cards, typography, grid, process paths, arrows and live-value legend with the subsystem schematics while retaining interactive selection and subsystem drill-down.
