# ADR 0126 — H.1 evidence selects deterministic semi-implicit pressure/flow coupling

## Status

Accepted / H.2 validated; runtime activation deferred to H.4 after H.3 prototype evidence.

## Context

The validated H.1 current-v2 audit compared 10, 5 and 2.5 ms fixed explicit steps from the same deterministic 20 ms seed-preconditioning state. Runtime cost approximately doubled at each refinement, raw primary hydraulic step changes remained large, and maximum final-state relative difference changed from 0.005401937 for 10→5 ms to 0.006028534 for 5→2.5 ms. Refinement therefore did not improve monotonically. Conservation gates remained green.

## Decision

1. Keep 10 ms explicit committed-state integration as the production baseline until a replacement is validated.
2. Do not select bounded explicit substepping as the preferred numerical cure.
3. Select an explicitly owned deterministic semi-implicit pressure/flow coupling for prototype and evidence.
4. H.2 changes no runtime physics or integration path.
5. H.3 owns an isolated prototype/audit.
6. H.4 owns any current-v2 activation after evidence.
7. The future coupled treatment must preserve single integration of conserved inventories, Phase G energy ownership, component reverse-flow semantics, conservation, replay/checkpoint determinism and legacy behavior.
8. Wall-clock adaptation, hidden damping, display filtering as a solver cure, physical-coefficient retuning and solver-protection interlocks are prohibited.

## Consequences

The Phase H sequence expands from a single decision directly into hardening to an evidence-gated implementation path: H.1 evidence → H.2 method decision → H.3 prototype/audit → H.4 activation gate → Phase I. This is deliberate because the selected method changes the numerical coupling contract and must not be activated on architectural judgment alone.
