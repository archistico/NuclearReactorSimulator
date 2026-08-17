# ADR 0136 — Localize thermodynamic boundaries before active-set formulation

**Status:** Accepted for M10.9.4.1-H.11 candidate.

## Context

Validated H.10 found no local hydraulic switching/non-smoothness around the two persistent H.9 failures. Instead it found exactly two thermodynamic phase/envelope switching nodes in the stalled H.9 candidate states and zero corresponding switches at the committed explicit endpoints.

That evidence is not sufficient to justify immediately implementing a semi-smooth Newton or active-set solver. The responsible node identity, perturbation axis and boundary type must first be made explicit.

## Decision

Add a separate shadow-only thermodynamic boundary-localization diagnostic.

For H.10-flagged nodes only, probe conserved mass and internal energy on both sides of the candidate state and record phase/envelope response, vapor quality and saturation-relative evidence. Classify the crossing axis and boundary class. Emit a nominal-phase active-set label only as a diagnostic hypothesis.

Also report node-local mapped-minus-applied hydraulic balance residuals so the localized boundary can be related to the stalled fixed-point mismatch without changing H.9 internals.

## Consequences

H.11 does not enforce an active set, alter the thermodynamic model, widen/clamp its envelope, introduce a nonlinear solver or route shadow state into production.

A later active-set/semi-smooth experiment is justified only if H.11 deterministically localizes the validated H.10 switching evidence to concrete nodes and boundary classes.
