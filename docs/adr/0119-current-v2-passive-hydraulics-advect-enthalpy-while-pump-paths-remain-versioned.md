# ADR 0119 — Current-v2 passive hydraulics advect enthalpy while pump paths remain versioned

**Status:** Accepted with validated M10.9.4.1-G.2 Hotfix 2

## Context

G.1 validated the target open-control-volume relation and measured a representative steam flow-work gap of 192.048450950 kJ/kg and 2.484103126 MW. Migrating every component group simultaneously would make it difficult to prove where pump and turbine work are counted.

## Decision

Add a definition-owned `FluidEnergyTransportMode` to `PipeDefinition` with historical `SpecificInternalEnergy` as the default and `SpecificEnthalpy` as an explicit opt-in.

The two current-v2 sustained profiles opt in their passive pipes and valve hydraulic paths. Their pump paths remain historical in G.2. `PipeFlowResult` publishes internal-energy, flow-work, enthalpy and selected applied rates; endpoint balances use only the selected applied rate.

Pump hydraulic fluid work remains a separate source term and shaft demand remains a separate mechanical demand. Neither is folded into enthalpy advection.

## Consequences

- Legacy and current-v1 definitions retain byte-for-intent historical behavior.
- Current-v2 passive hydraulic trajectories change physically and must pass all cumulative gates.
- Main-steam diagnostics can distinguish internal-energy evidence from the actual applied enthalpy rate.
- G.3 owns pump paths and the remaining internal/external boundary, separation, condenser, bypass and relief migrations; the G.2 evidence remains the historical pre-G.3 checkpoint.
- G.4 owns turbine expansion and exact shaft-work single counting.
