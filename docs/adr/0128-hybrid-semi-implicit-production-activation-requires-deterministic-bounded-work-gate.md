# ADR 0128 — Hybrid semi-implicit production activation requires a deterministic bounded-work gate

## Status

Accepted / H.4 VALIDATED

## Context

H.3 proved that deterministic semi-implicit pressure/flow coupling can strongly reduce hydraulic chatter while retaining conservation and exact repeatability. Applying that corrector to every audited 10 ms interval, however, cost approximately 15.9 times the isolated explicit replay.

A full-time corrector is therefore not an acceptable production default. At the same time, rejecting the method entirely would discard strong numerical evidence.

## Decision

Before any production activation, evaluate a deterministic hybrid strategy:

1. always compute the existing explicit predictor;
2. derive stiffness indicators only from predicted pressure and hydraulic-flow changes;
3. invoke the H.3 semi-implicit corrector only when deterministic thresholds are crossed;
4. sweep bounded trigger and Picard-control configurations under the same frozen-forcing comparison;
5. rank candidates using deterministic work and numerical-quality criteria;
6. treat wall-clock cost as observational evidence only;
7. keep production explicit until a separate integration candidate is authorized.

The activation gate must preserve mass/internal-energy integration, pump hydraulic-energy ownership, reverse-flow semantics, deterministic repeat and fixed 10 ms external logical time.

## Consequences

- H.4 can conclude either `activation-criteria-met=True` or `False` without changing production physics.
- No wall-clock-dependent adaptive behavior is permitted.
- A passing H.4 gate is permission for a later integration candidate, not automatic activation.
- A failing gate requires numerical redesign/optimization rather than coefficient retuning, hidden filtering or weaker conservation tolerances.
- The future extreme-operation/accident roadmap continues to depend on this numerical hardening before intentionally driving the plant far outside its normal envelope.
