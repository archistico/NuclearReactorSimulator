# ADR 0065 — Electrical loss constrains the canonical grid connection and composes explicit powered-load losses

- **Status:** Accepted / M8.6 validated
- **Date:** 2026-07-22

## Context

M8.6 requires loss-of-external-supply and station-blackout-class educational scenarios. The validated plant contains an M4.5 infinite-grid/generator/breaker model plus canonical pumps and M5 control paths, but no station AC/DC distribution, diesel, battery or emergency-switchgear model.

Inventing implicit buses in the scenario layer would create a second electrical ownership model and make consequences impossible to audit.

## Decision

Represent loss of external supply as a typed scenario fault bound to the exact canonical `ElectricalGridDefinition.Id`.

While active, the fault constrains the existing `GeneratorGridInputs` so canonical generator breakers receive open commands and close commands cannot take effect. M4.5 remains the sole breaker/electrical/rotor owner.

Represent station-blackout-class powered-equipment consequences as explicit co-scheduled faults over already validated seams:

- M8.2 pump trip constraints;
- M8.3 actuator-command fail-low overlays where powered command-path loss is part of the declared exercise;
- M8.4 turbine/generator trip events where declared.

Do not infer affected equipment from an unmodeled bus topology.

M2.5 decay heat remains a validated physical model but is not yet part of the M5.7 integrated operational runtime state. M8.6 therefore documents that gap and does not fabricate constant decay heat in the scenario layer.

## Consequences

- electrical isolation remains physically owned by M4.5;
- scenario assumptions about unavailable powered equipment are explicit and replay-visible;
- fault clearance cannot teleport breaker/equipment state back to pre-fault conditions;
- future station electrical-distribution fidelity can be added behind an explicit physical owner without preserving hidden M8.6 assumptions as de facto physics;
- quantitative station-blackout decay-heat analysis remains out of scope until M2.5 history/state is integrated into the full-plant operational runtime.
