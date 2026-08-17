# ADR 0139 — Broaden bounded thermodynamic hysteresis before activation

## Status

Accepted for M10.9.4.1-H.14 candidate.

## Context

Validated H.13 Hotfix 2 showed that targeted previous-phase continuity and targeted bounded previous-phase hysteresis both remove the two persistent H.9 nonlinear failures: each reaches 7/7 on the frozen P060/F040 set with deterministic conservation/ownership safeguards.

However, bounded hysteresis performed zero releases on that evidence set. Therefore H.13 proves the hold side of the policy under the difficult solver events, but does not establish that the explicit 2% pressure / 5 K temperature release condition can safely relinquish the previous phase.

## Decision

Before any production activation candidate:

- retain production `Resolve()` and explicit 10 ms integration unchanged;
- retain the H.13 bounded policy unchanged and targeted only at `steam` and `stop-out`;
- extend the committed shadow horizon from 500 to 2,000 intervals;
- re-evaluate all P060/F040 events found in that horizon with unchanged H.9 plus bounded hysteresis;
- observe target branch selection on every committed interval;
- add deterministic near-boundary hold and out-of-band release challenges in both phase directions;
- treat broader qualification and audit validity as separate results.

## Consequences

A positive H.14 result permits design of a later production-isolated activation candidate, but does not itself activate H.9 or thermodynamic hysteresis.

A negative result keeps production explicit and provides evidence about whether convergence, branch continuity or release behavior remains the limiting factor.
