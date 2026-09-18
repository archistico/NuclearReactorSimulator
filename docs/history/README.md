# Historical documentation

This directory contains records that remain useful for provenance but are no longer the current project description.

Use `../PROJECT.md` for the current validated checkpoint and restart instructions, `../ROADMAP.md` for future work, and `../ARCHITECTURE.md` for stable ownership.

## Structure

- `m10.9.4.1/` — detailed A–I numerical-hardening milestone notes, validation checklists, hotfix notes and static reviews.
- `m10-validation/` — consolidated completed M10.8–M10.9 validation/checklist material that no longer participates in executable contracts.
- `m10-final/` — consolidated completed M10 Final diagnostics and VR2/RP1 gate history.
- `project/` — superseded snapshots and earlier cross-milestone plans retained for comparison.
- `ARCHITECTURE_MILESTONE_LEDGER.md` — milestone-led architecture chronology superseded by ownership-oriented `../ARCHITECTURE.md`.

## Historical-document rule

Historical files are **not** the place to determine the current production policy, active candidate or next gate. They may intentionally contain links to former paths.

Do not delete historical evidence merely because it is no longer current. Compaction is allowed only when:

1. the source gate is completed/superseded;
2. no executable validator/script/test references the source path;
3. the consolidation dossier records every original filename and normalized-LF SHA-256;
4. the source content is retained in the dossier or in an equivalent frozen-evidence package.
