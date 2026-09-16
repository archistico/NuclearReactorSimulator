# ADR-0198 — Require immutable C4 full-domain performance confirmation before RP1C selection

## Status

Accepted for RP1C evidence planning.

## Context

C4 is bit-equivalent to physically complete C3 and closes the R1 allocation/wall-clock tail. Its returned timing campaign, however, intentionally measured the R1 owner rather than the complete exact-v9 and four-side seam domain. C4 introduces new allocation-neutral mixture/liquid prefixes, so C3 full-domain timing cannot be silently inherited as C4 timing.

## Decision

Before RP1C selection, measure immutable C4 in five fresh processes over the frozen exact-v9 and seam corpora using the Refinement-3 warm-up/measured-pass protocol and unchanged ceilings. Keep negative performance results as evidence rather than harness failures. Preserve `SELECT-NONE` and require returned confirmation adjudication before any selection gate.

## Consequences

This adds one bounded performance gate but prevents a selection decision from relying on inferred wall-clock equivalence. It does not authorize production repair or candidate mutation.
