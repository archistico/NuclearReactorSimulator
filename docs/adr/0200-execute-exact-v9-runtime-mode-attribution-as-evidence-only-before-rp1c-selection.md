# ADR-0200: Execute exact-v9 runtime-mode attribution as evidence-only before RP1C selection

## Status

Accepted.

## Context

C4 Full-Domain Performance Confirmation 1 returned complete evidence but remained `C4-FULL-DOMAIN-PERFORMANCE-NOT-CONFIRMED` because two rare exact-v9 calls exceeded the unchanged single-call ceiling. The returned evidence did not establish a thermodynamic slow path, GC cause, tiered-compilation cause, Dynamic PGO cause, or scheduling cause.

Planning 1 froze a four-mode runtime experiment over immutable C4 and the frozen exact-v9 corpus.

## Decision

Implement and execute the four-mode Attribution 1 gate strictly as comparative evidence.

The gate:

- uses 20 fresh processes
- records the effective `DOTNET_*` compilation environment per process
- preserves the 16 warm-up / 64 measured-pass exact-v9 protocol
- uses `100 us` only as a diagnostic tail floor
- preserves `409.30666666666673 us` as the engineering maximum
- fails closed when the caller already defines any planned runtime compilation variable
- does not mutate C4 or production thermodynamics
- does not automatically infer runtime causality
- does not automatically select C4

A separate returned-evidence adjudication is required before any causal conclusion or RP1C selection planning.

## Consequences

A mode-dependent change in tail frequency becomes evidence to interpret, not an automatic proof of cause. A lack of mode dependence likewise does not prove a thermodynamic cause; it leaves runtime/scheduling attribution unresolved.

Production repair, threshold changes, exact-v9 changes, VR3, P3-R1 and a second replacement-long baseline remain unauthorized.
