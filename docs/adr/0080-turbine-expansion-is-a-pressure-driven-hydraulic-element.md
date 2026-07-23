# ADR 0080 — Turbine expansion is a pressure-driven hydraulic element

## Status

Accepted for M10.9.4 Hotfix 13 implementation candidate.

## Context

The canonical plant network already integrates stop, control and admission valves as real hydraulic mass transfers through `header → stop-out → control-out → turbine-inlet`. M4 turbine expansion separately drains `turbine-inlet` and sources the exhaust node through `TurbineExpansionSolver` source terms.

The historical M5.4/M5.5 composition duplicated a derived stage-flow law `min(stopFlow, controlFlow, admissionFlow)`. With the three valve transfers already integrated, the combined intermediate inventory obeyed `d(m_stop-out + m_control-out + m_turbine-inlet)/dt = F_stop - F_stage >= 0`. The admission train was therefore structurally a monotonic accumulator. Tuning resistance, temperature, controller bias, condenser capacity or volume could only change the time to pressure equalization and zero terminal stage flow.

## Decision

`TurbineStageGroupDefinition` may publish an optional `ExpansionResistance`.

- Current physical definitions use a positive quadratic resistance and compute stage mass flow from the pressure difference between the admission-boundary source node (`turbine-inlet`) and the stage exhaust node.
- Flow is zero when `P_inlet <= P_exhaust`; turbine stages do not reverse-flow in this educational model.
- A deterministic inventory guard prevents one fixed step from draining more than half of committed inlet mass.
- Definitions with `ExpansionResistance == null` retain the historical upstream-valve-minimum law only for explicitly isolated legacy compatibility.
- One `TurbineStageMassFlowResolver` in the M4 turbine namespace is shared by M5.4 and M5.5 input composition; duplicated physical flow laws are forbidden.
- `TurbineExpansionSolver` remains the sole owner of turbine inlet→exhaust mass/energy source terms and shaft extraction.

Current generation-ready v2 seeds use `21,400 Pa·s²/kg²`, approximately matching ~13 kg/s at the intended low-load inlet/exhaust pressure difference. This number remains an operating-point parameter subject to validation; the structural requirement is the pressure-driven closure itself.

## Consequences

The turbine inlet acquires a real hydraulic downstream boundary. Admission-train pressure can settle where upstream valve inflow equals turbine expansion outflow, condenser backpressure influences stage flow, and the monotonic-accumulator invariant is removed.

An ordinary ~200-step regression checks bounded combined admission-train inventory and agreement between admission-valve and stage flow at the settled checkpoint. The explicit 60-second gameplay pack remains the integrated acceptance gate.
