# ADR-0195 — Separate C3 algorithmic performance from managed-runtime wall-clock tail before repair selection

## Status

Accepted — returned RP1B Performance Measurement Replanning 1 audit `PASS-AS-AUTHORED`; Refinement 5 implements the frozen cross-process rule without changing C3.

## Context

C3 is physically complete on the frozen RP1A corpus and, after Refinement 3, its exact-v9 and targeted-state timing remained below the frozen RP1A maximum ceiling. The remaining strict blocker is isolated seam wall-clock tail.

Refinement 4 localized two new exceedances but did not reproduce a single candidate boundary as the owner: the screen exceedance occurred on boundary 3 with Gen0 activity, while the targeted exceedance occurred on boundary 191 without GC activity. Both boundaries retained ordinary median/p95 timings.

A code change to C3 from this evidence would risk optimizing for runtime scheduling noise rather than deterministic candidate work. Conversely, the historical strict maximum observations cannot simply be deleted or reinterpreted away.

## Decision

Before C4 or RP1C:

- keep C3 byte-for-byte immutable;
- preserve the existing strict single-call maximum ceiling and all historical exceedances;
- do not authorize C4 unless a same-boundary ceiling exceedance is confirmed in at least two independent focused-test process runs under the frozen Refinement 5 protocol;
- if no same-boundary cross-process slow path is confirmed, route to a separate performance-contract adjudication instead of silently promoting C3;
- keep RP1C, production repair and exact-v9 changes unauthorized until that evidence is reviewed.

## Consequences

The project distinguishes a candidate-specific deterministic slow path from managed-runtime wall-clock tail without weakening existing evidence. Refinement 5 becomes a reproducibility diagnostic, not a repair or selection gate.
