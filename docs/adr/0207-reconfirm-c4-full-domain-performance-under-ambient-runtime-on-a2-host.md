# ADR 0207 - Reconfirm C4 full-domain performance under ambient runtime on the A2 host

## Status

Accepted for planning only.

## Context

FDPC1 was valid negative evidence because two rare exact-v9 wall-clock calls exceeded the frozen single-call maximum. The subsequent runtime-sensitive Branch A work showed that tiering/PGO materially affect the timing distribution. A2 then compared ambient/unset with an explicit all-ON reference on one physical host across exact-v9, ordinary Release, replay/determinism and a validated non-VR2 hot-path owner.

A2 returned complete same-host evidence: both profiles preserve project behavior, ambient records no strict exact-v9 exceedance, and the explicit all-ON reference is not materially required for project qualification. The evidence does not prove internal runtime defaults are identical; it shows ambient is operationally sufficient on the A2 host.

## Decision

Plan a new `RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION2` under `AMBIENT-UNSET` on the same physical A2 host and stable power scheme. Preserve C4, exact-v9, seam corpus and all performance thresholds. Do not reinterpret FDPC1 and do not introduce an explicit production runtime policy.

The future gate remains evidence-only. A green returned FDPC2 may make the C4 + ambient pair selection-ready, but a separate RP1C selection gate must still choose `SELECT-C4` or `SELECT-NONE`.

## Consequences

- FDPC2 planning may proceed.
- FDPC2 implementation remains unauthorized until returned Planning 1 adjudication.
- Cross-host substitution is not allowed without a separate amendment.
- Production runtime change remains unauthorized.
- RP1C selection remains unauthorized.
