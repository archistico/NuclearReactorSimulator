# Documentation architecture and indexing

## Purpose

The project documentation is large enough that discoverability and authority rules must be explicit. This document defines how current state, stable architecture, future planning, ADR provenance, technical references, acceptance artifacts and historical ledgers are separated.

## Authority model

| Need | Authoritative location |
| --- | --- |
| Current validated baseline, active candidate, current validation commands | `PROJECT.md` |
| Stable layer/subsystem ownership | `ARCHITECTURE.md` |
| Future work only | `ROADMAP.md` + milestone plans |
| Current fidelity/model limitations | `KNOWN_MODEL_LIMITATIONS.md` |
| Architectural decisions/provenance | `adr/README.md` + individual ADRs |
| Complete top-level technical-doc discovery | `TOP_LEVEL_DOCUMENT_INDEX.md` |
| Historical milestone chronology | `history/` |
| User-facing operating guidance | `usermanual/` |

A document must not duplicate the current validation checkpoint merely for convenience. If current state is needed, link to `PROJECT.md`.

## Stable architecture versus chronology

`ARCHITECTURE.md` is organized by **what owns a concern**, not by when a milestone introduced it. Milestone-led architecture prose is preserved under `history/ARCHITECTURE_MILESTONE_LEDGER.md`.

New architecture work should update the relevant subsystem section and its ADR, rather than appending another `## Mx.y ... boundary` section to the stable architecture file.

## Top-level technical documents

The top-level `docs/*.md` collection is intentionally broad. `README.md` provides a small curated entry point; `TOP_LEVEL_DOCUMENT_INDEX.md` is the exhaustive index.

Acceptance checklists may remain top-level while their milestone is active/recent, but they are not long-lived architecture references and should not be mixed into the curated technical-reference list.

## ADR collection

`adr/README.md` provides:

- a normalized status vocabulary;
- an area classification;
- a complete numbered ledger;
- a short list of current governing pointers by area.

Individual ADR bodies preserve narrative provenance. The index normalizes discovery without pretending that ADR history is the current build-status source.

## Historical references

Frozen documents under `history/` may intentionally reference files that no longer exist in the live documentation set. Those references are provenance, not live navigation promises. Live docs outside `history/` must use correct relative links or explicit repository-root paths.

## Release hardening

M11.5 owns automation that verifies at least:

- every live relative Markdown link resolves;
- every top-level `docs/*.md` file appears in `TOP_LEVEL_DOCUMENT_INDEX.md`;
- ADR numbering is unique and contiguous for the retained sequence;
- every ADR has a `## Status` heading;
- every ADR appears in `adr/README.md`;
- `ROADMAP.md` contains no concrete current-status checkpoint;
- `ARCHITECTURE.md` contains no active candidate/validation checkpoint;
- selected source XML comments that describe ownership do not contradict stable architecture documents.
