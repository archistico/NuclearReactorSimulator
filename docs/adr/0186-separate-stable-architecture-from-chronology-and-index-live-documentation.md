# ADR-0186 — Separate stable architecture from milestone chronology and index live documentation

## Status

Accepted — documentation architecture decision, 2026-08-21.

## Context

The documentation set grew to hundreds of files. `ARCHITECTURE.md` accumulated milestone-by-milestone sections and a stale current-checkpoint narrative, while `ROADMAP.md` duplicated completed/current status despite declaring itself future-only. The top-level documentation README listed only a subset of live technical documents, and the ADR collection had no index or uniform status-heading convention.

These problems do not change runtime behavior, but they make ownership discovery dependent on knowing historical milestone numbers and create multiple places where current state can drift.

## Decision

1. `PROJECT.md` remains the only current validation/candidate status source.
2. `ARCHITECTURE.md` is organized by layer/subsystem ownership and contains no active checkpoint narrative.
3. Former milestone-led architecture sections move intact to [`history/ARCHITECTURE_MILESTONE_LEDGER.md`](../history/ARCHITECTURE_MILESTONE_LEDGER.md).
4. `ROADMAP.md` contains future work only; completed-milestone status is removed.
5. `README.md` remains curated and points to `TOP_LEVEL_DOCUMENT_INDEX.md` for exhaustive top-level discovery.
6. [`adr/README.md`](README.md) becomes the complete ADR index with normalized navigation status and area classification.
7. Individual ADRs expose a consistent `## Status` heading; historical status prose remains preserved.
8. `KNOWN_MODEL_LIMITATIONS.md` describes human-readable current limitations and links exact regression values to machine evidence rather than embedding excessive precision in prose.
9. M11.5 owns automated documentation-index/status/link drift checks.

## Consequences

- Architecture is discoverable by topic rather than chronology.
- Historical provenance remains available without masquerading as current ownership.
- Current state is less likely to diverge across README/ROADMAP/architecture files.
- ADR and top-level-document discovery become mechanical.
- Documentation maintenance gains an explicit release gate instead of relying on memory.
