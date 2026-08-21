# ADR-0173 — External energy demand is versioned logical-step evidence, not generator control

## Status

Accepted — M10.9.6.2 Hotfix 1 validated 2026-08-20

## Context

Operational challenges need an external electrical-demand reference for training and later demand-tracking evaluation. The simulator already has a canonical generator requested electrical-power setpoint and measured actual electrical output. Conflating any of these three values would create hidden control authority and make scoring capable of changing the plant.

## Decision

1. External demand is optional, versioned challenge-owned Application state.
2. Profile time is logical-step-only and relative to challenge activation.
3. Initial profiles use ordered HOLD/LINEAR control points, supporting constant, step, bounded-ramp and piecewise sequences.
4. `EXTERNAL GRID DEMAND`, generator requested load and actual electrical output are separate semantics.
5. Demand/output error is observational evidence only.
6. A demand profile never writes generator requested load, torque, grid coupling or supervisory authority.
7. Future schedule visibility is explicitly owned by the profile definition.
8. Demand is unavailable outside an activated challenge that owns a profile.
9. Replay reconstructs demand from versioned definition plus logical-step evidence rather than mutable serialized demand state.

## Consequences

M10.9.6.3 may score demand tracking without becoming a physical owner. M10.9.6.4 may decide per challenge whether the next demand point is visible. M10.9.7 may present demand/request/output distinctly without inventing a fourth authority source.
