# ADR 0081 — Legacy replay compatibility does not constrain current-model correctness

## Status

Accepted for M10.9.4 Hotfix 13 implementation candidate.

## Context

During the M10.9.4 turbine/generator investigation, preserving exact historical initial-condition behavior became conflated with preserving historical physical defects. That encourages workaround accumulation and can force the active simulator to inherit pre-release modeling mistakes solely to reproduce old development artifacts.

## Decision

Current-model correctness has priority over exact behavioral compatibility with pre-release legacy simulations.

- Versioned legacy initial conditions/replays may remain resolvable through isolated legacy definitions when doing so is cheap and does not contaminate current physics.
- The current baseline must not keep a known-defective physical law merely because historical replay used it.
- When a physical correction cannot coexist cleanly with exact legacy behavior, legacy artifacts may be migrated, explicitly marked unsupported, or replayed only through an isolated compatibility path.
- Compatibility failures must remain explicit/fail-closed; no silent reinterpretation of old replay data is allowed.
- New gameplay, validation and release baselines always use the corrected current model.

## Consequences

`ExpansionResistance == null` may preserve the historical turbine stage-flow law for explicit legacy definitions, while current v2 definitions use the corrected pressure-driven law. Future cleanup may remove pre-release compatibility paths after migration/retention decisions without blocking physical improvements to the active simulator.
