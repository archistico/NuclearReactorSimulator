# ADR 0141 — Extend bounded branch continuity to the H.15-localized `header` only

## Status

Accepted and validated in M10.9.4.1-H.16.

## Context

H.13 solved the original H.9 failures at `steam` and `stop-out` using a shadow-only bounded previous-phase hysteresis policy with 2% pressure-drift and 5 K temperature-drift release limits. H.14 broadened that policy to 2,000 intervals and found one remaining failure at interval 723. H.15 localized that failure to `header` and confirmed the same overlapping-root / coarse-saturated-detection / fixed-priority shadowing mechanism.

## Decision

H.16 extends only the target node set from `steam|stop-out` to `steam|stop-out|header`.

The following remain unchanged:

- production `Resolve()` order;
- bounded hysteresis limits;
- H.9 Jacobian corrector and tolerances;
- P060/F040;
- physical coefficients;
- 10 ms production timestep;
- production explicit routing.

The H.14 two-node policy is retained as an internal control in the same focused audit.

## Consequences

A 15/15 result demonstrates that the H.15-localized third node is sufficient for the current 2,000-interval evidence set. It does not authorize production activation. A longer-horizon/cross-profile shadow gate is still required before activation design.
