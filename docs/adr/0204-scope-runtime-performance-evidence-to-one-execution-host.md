# ADR-0204 - Scope runtime performance evidence to one execution host

## Status
Accepted for planning amendment only.

## Context
Branch A2 will compare runtime profiles using wall-clock exact-v9 evidence and an unchanged absolute strict maximum. The project is executed on at least two PCs with materially different performance. The original A2 Planning 1 froze runtime profiles and test families but did not freeze physical-host provenance.

Without an explicit host constraint, hardware differences could be mistaken for runtime-profile effects or software regressions.

## Decision
A complete `RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT1` execution must use one physical host for all profiles and all assessment families.

The runner must capture a privacy-preserving SHA-256 host fingerprint at gate start and end, propagate it into child-run contracts, and fail closed on mismatch. Machine name, user name and raw system UUID are not retained. The active Windows power scheme is recorded and must remain stable for the gate.

Absolute timing observations are host-scoped. Cross-host timing comparison or threshold extrapolation is not part of A2 and requires separately planned replication if later material.

## Consequences
- Work-PC and home-PC executions remain valid as separate complete gates, not as mixed evidence.
- Same-host profile contrasts remain interpretable.
- The frozen strict maximum is neither relaxed nor scaled for slower hardware.
- C4 and exact-v9 remain immutable.
- Planning 1 remains PASS as authored, but Amendment 1 is required before A2 implementation.
- No production runtime configuration is selected by this decision.
