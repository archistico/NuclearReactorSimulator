# ADR 0105 — Loaded desktop stop-valve pressure grade is rebalanced after local bottleneck evidence

## Status

Proposed / M10.9.4.1-D.3.2 Hotfix 2 candidate.

## Context

D.3.2 correctly made the current-v2 pressure-driven turbine stage respect the complete stop/control/admission train. The first ordinary-suite run then measured `11.784841 kg/s` effective stage flow and `4.352905 MW` shaft power, below the unchanged generation-ready floors.

D.3.2 Hotfix 1 attempted the smallest apparent correction by moving the loaded desktop control-valve bias from `28%` to `30%`. Local validation disproved that hypothesis: the effective stage flow changed only to `11.792118 kg/s`, and the ten-second gross-output gate still failed.

The reason is visible in the committed seed pressure grade. With the simplified saturation closure and the current `1,000 Pa·s²/kg²` valve base resistance:

- header at `278.5 °C` resolves to about `6.2725 MPa`;
- stop outlet at `277.0 °C` resolves to about `6.1311 MPa`;
- the fully open stop valve therefore receives only about `141.5 kPa` head and has about `11.89 kg/s` capacity;
- the control valve at `28%`, across the much larger stop-out to control-out pressure drop, already has about `13.10 kg/s` capacity.

The fully open stop valve, not the control valve, is therefore the active initial bottleneck. Increasing only the control-valve opening cannot materially raise the train flow.

## Decision

Keep the D.3.2 admission-isolation law and all hydraulic resistances unchanged.

Restore the loaded desktop control-valve bias to `28%`, matching the validated authority point. Rebalance only the loaded desktop initial steam-path pressure grade by moving the stop-outlet seed temperature from `277.0 °C` to `276.7 °C`.

Under the same deterministic saturation model this gives approximately:

- stop-valve head: `169.45 kPa`;
- fully open stop-valve capacity: `13.017 kg/s`;
- 28% control-valve capacity: `13.015 kg/s`.

The two adjacent valve capacities are therefore intentionally matched near the generation-ready operating point instead of leaving an artificial fully-open stop-valve choke. Local coupled validation remains authoritative for effective vapor-admitted flow, shaft power and gross electrical output.

## Consequences

- the existing `12.5–30 kg/s`, `>4.5 MW` shaft-power and `>4 MWe` gross-output floors remain unchanged;
- desktop and synchronization profiles both retain a `28%` initial control-valve bias, while their PI/PID controller definitions remain distinct;
- no valve/stage resistance, characteristic, turbine-work law, rotor loss, governor gain, anti-windup, actuator travel, droop, generator/grid, protection, timestep, replay or PLANT renderer changes;
- application regressions now freeze the stop-valve pressure head, the stop/control capacity balance and the commanded/effective generation-ready flow;
- ADR 0104 remains as rejected historical evidence and is superseded by this decision.
