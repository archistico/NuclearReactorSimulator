# ADR 0102 — Reference plant scale target is a 10 MWe educational unit

## Status

Accepted target / M10.9.4.1-E.1. E.2 migration implemented as a candidate; focused audit green, long/manual promotion gates pending.

## Context

A.3 proved that the current hybrid configuration combines a 1,000 MW generator nameplate with a 1,000 kg·m² rotor, a 5 MWe sustained operating point and low-megawatt turbine/condenser capacities. The same rotor corresponds to `H = 0.049348 s` at 1,000 MW but `H = 4.934802 s` at 10 MWe. D.1–D.3 subsequently closed turbine phase semantics and measured admission/governor behavior without identifying a need for actuator tracking anti-windup.

## Decision

The reference current-v2 educational plant uses a nominal **10 MWe generator scale** in the E.2 candidate. The accepted scale basis retains:

- 3,000 rpm rated speed;
- 1,000 kg·m² rotor inertia;
- 5 MWe normal sustained reference point;
- 0–10 MWe requested-load envelope for the current training profile;
- 5 MWe operator load-command increment unless separately revised by an HMI/training decision.

E.1 records the target. E.2 applies the migration as one coordinated physical change covering nameplate, governor normalization, signed/bidirectional grid coupling, HMI ranges and trajectory evidence. It uses a 1.5 rpm full-load rise and explicitly retains the 10 MW coupling values pending long-running evidence.

## Consequences

- The 5 MWe reference point becomes 50% load.
- The retained rotor gives an inertia constant of approximately 4.935 s at the target nameplate.
- Changing only `MaximumElectricalPower` remains prohibited.
- Reverse-power, underfrequency and loss-of-synchronism protections remain deferred to E.3 until the implemented signed generator/grid states pass the complete promotion gate.
- Historical/v1 profiles are not rewritten by the current-v2 migration.
