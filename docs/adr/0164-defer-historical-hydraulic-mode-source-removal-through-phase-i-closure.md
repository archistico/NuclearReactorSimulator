# ADR 0164 — Defer physical deletion of historical hydraulic numerical modes through M10.9.4.1 closure

## Status

Proposed by I.4; becomes accepted only after I.4 validation.

## Context

Phase-I audit consolidation removed H.5/H.21 from current CI and I.1 established that exact-version compatibility does not require their numerical modes. I.3 then validated exact-v3 corrected-commit as the authoritative production reference.

The historical modes still appear in four source files and four test files each. Removing them inside the Phase-I closure path would combine a compatibility/evidence review with a non-trivial API/runtime-source refactor.

## Decision

Do not expose `DeterministicHybridSemiImplicit` or `FourNodeBranchContinuityShadowIntegrated` as production choices.

Keep their source seams temporarily through I.5 while:

- production remains exact-v3 corrected-commit;
- exact-v2 explicit remains rollback/reference;
- current CI does not execute H.5/H.21 historical gates;
- compact frozen evidence remains authoritative for ordinary regression.

Perform physical deletion only in a separately scoped maintenance change after historical executable tests are archived or replaced.

## Consequences

- M10.9.4.1 closure does not depend on a risky legacy-source refactor.
- Historical executable provenance remains available.
- The codebase still carries two non-production modes temporarily; this is explicit technical debt rather than hidden production surface.
