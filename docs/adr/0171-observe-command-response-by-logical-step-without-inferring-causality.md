# ADR-0171 — Observe command response by logical step without inferring causality

## Status

Proposed in M10.9.5.4; becomes Accepted after local build, complete ordinary tests and the focused observed-response gate are explicitly green.

## Context

M10.9.5.1 defines authored qualitative consequence/monitor semantics, M10.9.5.2 defines bounded dependency chains, and M10.9.5.3 integrates those semantics into F4 COMMANDS. The remaining operator question is what actually changed after an attempted command. That evidence must not become a second physics model or claim causality merely because two events occurred close together.

## Decision

- Use only the existing M10.9.5.1 authored monitor set for observed response.
- Capture the monitor values at the dispatch logical-step boundary and compare them with later UI-safe snapshots.
- Use a fixed 500-logical-step observation window; wall-clock time is forbidden.
- Report baseline/latest values, numeric delta/direction or state transition when directly observable.
- Report accepted/rejected canonical feedback separately from monitor changes.
- Rejected commands show no fictional plant-effect deltas.
- Protection clear/active state may be reported as an observation, never as proof that the command caused a trip.
- Never derive generic command `SUCCESS/FAILURE` from numeric deltas unless an existing canonical procedure contract already owns that semantic.
- Keep observation samples derivable and `[JsonIgnore]` so replay/save fingerprints and authoritative plant state are unchanged.

## Consequences

M10.9.5.5 can close the consequence model by testing expected-vs-observed separation, replay determinism, inspection non-mutation and representative manual HMI behavior without introducing predictive UI physics.
