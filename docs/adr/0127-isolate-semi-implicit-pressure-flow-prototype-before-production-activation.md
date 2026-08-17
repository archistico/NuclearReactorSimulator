# ADR 0127 — Isolate semi-implicit pressure/flow prototype before production activation

**Status:** Accepted / validated by M10.9.4.1-H.3 Hotfix 1

## Context

Validated H.1 evidence shows non-improving explicit timestep refinement at approximately doubled cost per halving. Validated H.2 therefore selects deterministic semi-implicit pressure/flow coupling as the preferred method direction, while requiring isolated evidence before any current-v2 activation.

A direct replacement inside `PlantNetworkOrchestrator` would make it difficult to distinguish numerical-method effects from changes in heat transfer, drum separation, turbine/condenser boundaries, controller state or other full-plant owners.

## Decision

H.3 introduces a separate `SemiImplicitHydraulicPrototypeSolver` and does not route production execution through it.

The current-v2 audit reconstructs non-hydraulic forcing from the validated explicit trajectory and freezes that forcing per logical interval. Explicit and semi-implicit isolated replays then differ only in how pipe/valve/pump pressure-flow feedback is resolved.

The prototype:

- reuses existing component laws unchanged;
- iterates deterministically within one external 10 ms logical step;
- uses bounded under-relaxed Picard iteration;
- rebuilds every provisional inventory from the original committed state, so provisional iterations are never cumulative commits;
- records convergence, chatter, pressure, ownership, deterministic-repeat and cost evidence;
- remains non-production until H.4.

## Consequences

This adds test-only computational cost and a second numerical implementation seam, but avoids introducing a second production physics owner. H.4 must either activate a proven method through one canonical production owner or leave the current runtime unchanged and redesign the prototype.

Prototype tolerances/relaxation are evidence parameters and are not automatically accepted as final production constants.
