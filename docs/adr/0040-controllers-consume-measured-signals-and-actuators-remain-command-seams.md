# ADR 0040 — Controllers consume measured signals and actuators remain command seams

## Status

Accepted for M5.2; the milestone was subsequently locally validated.

## Decision

Reusable controllers consume only `MeasuredSignalFrame` channel ids. They do not accept or traverse `FullPlantSnapshot`. Controller memory is separate deterministic algorithm state.

Actuator primitives translate controller outputs into typed commands, but do not duplicate or integrate physical valve, pump or control-rod state. Physical ownership remains in the existing plant and reactor domains. Plant-specific adapters are introduced only when concrete loops are built in M5.3/M5.4.

Manual/automatic transition, saturation and anti-windup behavior are explicit and testable; invalid measurements cannot silently fall back to true state.

## Consequences

- sensor faults and quality degradation can influence control naturally through measured signals;
- controller replay remains deterministic because integration uses simulation timestep only;
- no second physical actuator model is introduced by generic control primitives;
- future protection and fallback policies can be layered without changing controller ownership.
