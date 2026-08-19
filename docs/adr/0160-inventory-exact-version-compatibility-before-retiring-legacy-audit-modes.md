# ADR 0160 — Inventory exact-version compatibility before retiring legacy audit modes

## Status

Accepted / I.1 Hotfix 1 user-validated on 2026-08-19.

## Context

Phase H closed as `OPT-IN ONLY`: exact v2 explicit remains authoritative and exact v3 corrected remains qualified opt-in. The repository also contains older exact-version scenario identities and historical numerical modes used by the H.5/H.21 audit lineage.

Deleting code merely because it is old would risk two distinct failures:

1. breaking exact-version scenario/save/replay loading;
2. destroying executable provenance still referenced by historical audit tests.

## Decision

Phase I begins with an executable compatibility and retirement inventory.

Exact-version identities are retained whenever they are registered or referenced by versioned scenario/session/replay contracts. A lower version number is not sufficient evidence of dead code.

Historical numerical modes that are no longer production-selectable may be marked as retirement candidates, but they are not removed until audit consolidation proves that executable historical seams are no longer required.

The H.30 production decision remains unchanged:

```text
v2 ExplicitCommittedState                         authoritative default / rollback / reference
v3 FourNodeBranchContinuityCorrectedCommitOptIn  qualified opt-in
```

## Consequences

- compatibility becomes explicit and testable;
- retirement is evidence-driven rather than age-driven;
- no exact-version archive identity is reinterpreted;
- H.5 hybrid and H.21 shadow modes may be removed later only after audit consolidation;
- I.1 itself is metadata/test/documentation only under production source.
