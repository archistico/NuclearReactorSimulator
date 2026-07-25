# ADR 0104 — Loaded desktop admission bias is realigned after train isolation

## Status

Superseded by ADR 0105 after local Hotfix 1 validation disproved the bias-only hypothesis.

## Supersession note

Local validation at 30% produced only `11.792118 kg/s`, essentially unchanged from the 28% result, and the gross-output gate still failed. The fully open stop valve was the actual initial bottleneck. This ADR is retained as rejected evidence; ADR 0105 owns the active decision.

## Context

D.3.2 corrected the current-v2 pressure-driven turbine stage so it can no longer exceed the capacity of the upstream stop, control and admission valves. This removed the former hidden bypass but also changed the loaded desktop operating point.

The ordinary suite measured `11.784841 kg/s` initial effective stage flow against the established `12.5–30 kg/s` generation-ready contract. After ten simulated seconds, turbine shaft power was `4.352905 MW`, below the existing `>4.5 MW` mechanical-support contract. Lowering those acceptance floors would silently redefine a generation-ready 5 MWe desktop seed as healthy despite reduced real electrical support.

## Decision

Keep all D.3.2 solver and isolation laws unchanged. Move only `DesktopSustainedGenerationInitialConditionFactory` initial control-valve bias from `28%` to `30%`. Keep `GridSynchronizationSustainedInitialConditionFactory` at `28%`.

The control valve has a linear characteristic. At a locally unchanged pressure pattern, 30/28 gives a 7.14% capacity increase, projecting the observed flow to about `12.63 kg/s` and shaft power to about `4.66 MW`. These projections select the smallest useful candidate step; local coupled validation remains authoritative.

The desktop controller initial manual output moves with the physical valve state, preserving the existing bumpless automatic initialization design.

## Consequences

- generation-ready flow, shaft-power and gross-output floors are retained;
- synchronization and loaded desktop profiles now intentionally own distinct 28% and 30% biases;
- the D.2 analytical authority map includes both operating points;
- no controller gain, anti-windup, actuator travel, droop, valve/stage resistance, turbine work, rotor loss, generator/grid, protection, timestep, replay or UI law is changed;
- the 30% value remains a current-v2 seed calibration candidate until the ordinary, explicit authority/governor, long-running and operational-envelope gates pass locally.
