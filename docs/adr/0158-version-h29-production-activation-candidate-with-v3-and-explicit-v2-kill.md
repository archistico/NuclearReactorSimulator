# ADR 0158 — Version the H.29 production activation candidate as v3 and preserve v2 as the explicit kill/rollback reference

## Status

Accepted for H.29 candidate qualification.

## Context

The corrected four-node path has accumulated numerical, authority, replay, long-horizon, protection/transient, rollback, off-design and performance evidence through H.28 and the post-H.28 H.24 requalification. H.29 must now qualify a production-default candidate without silently changing historical save/replay semantics or pre-deciding H.30.

Reusing the existing v2 initial-condition identity for a different hydraulic numerical policy would make old versioned recordings ambiguous. Mutating policy inside an already-running session would also complicate deterministic replay authority.

## Decision

1. Keep `integrated-operations-desktop-stable` v2 immutable and explicit.
2. Introduce exact version v3 as the H.29 corrected production activation candidate.
3. Resolve deployment policy before runtime construction.
4. An explicit kill request always resolves to v2.
5. Keep the existing standard scenario pinned to v2 and add a separate H.29 candidate scenario pinned to v3.
6. Use H.20 same-step fail-closed fallback inside v3; do not add an independent mid-step policy switch.
7. Keep production numerical diagnostics internal and observational; do not add them to the operator snapshot merely to satisfy H.29.
8. Leave the authoritative default unchanged until H.30 makes the cumulative Phase H closure decision.

## Consequences

- Existing recordings/checkpoints keep exact historical meaning.
- Candidate deployment and rollback are explicit, deterministic and testable.
- v2 and v3 can coexist in the versioned registry without aliasing.
- H.29 can qualify production mechanics without claiming that corrected ownership is already the production default.
- H.30 can select `ACTIVATE`, `OPT-IN ONLY` or `REMAIN EXPLICIT` using the complete evidence chain.
