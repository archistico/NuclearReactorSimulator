# Documentation cleanup audit — 2026-09-17

## Scope

Detailed review of the repository documentation after the M10 Final / VR2 / RP1C roadmap expansion. The goal is to reduce navigation noise without weakening executable validation contracts or historical traceability.

## Inventory before cleanup

- Markdown files under `docs/`: **716**
- Live top-level `docs/*.md`: **231**
- ADR files: **201**
- Existing historical files: **175**
- Milestone files: **96**
- Top-level M10-prefixed files: **95**
- Top-level M10 Final VR2 files: **37**

The main source of navigation overload was not stable subsystem documentation; it was accumulated milestone/checklist/diagnostic/pre-execution/hotfix material left at the live top level after its gate had completed.

## Safety rule used

No document referenced by `eng/`, `scripts/`, `tests/`, `src/` or `.github/` was removed or moved. This is intentionally stricter than checking Markdown links alone because many historical-looking files are still part of executable validator contracts.

## Consolidation performed

Fifty completed and executable-unreferenced M10 documents were compacted into five historical dossiers:

1. `history/m10-validation/M10_LEGACY_VALIDATION_CHECKLISTS_DOSSIER.md`
2. `history/m10-final/M10_FINAL_LONG_DIAGNOSTICS_DOSSIER.md`
3. `history/m10-final/M10_FINAL_REPLACEMENT_LONG_DIAGNOSTICS_DOSSIER.md`
4. `history/m10-final/vr2/M10_FINAL_VR2_RP1B_REFINEMENTS_DOSSIER.md`
5. `history/m10-final/vr2/M10_FINAL_VR2_RP1C_CONFIRMATION_HISTORY_DOSSIER.md`

Each dossier contains the original filename list, normalized-LF SHA-256 and retained source snapshots.

`DOCUMENTATION_ARCHITECTURE_AND_INDEXING.md` was folded into the curated `README.md`, eliminating a separate governance file. The superseded `FORWARD_EXECUTION_PLAN_M10_9_7_TO_M15.md` was moved to `history/project/`; current forward execution remains in `ROADMAP.md`.

## Inventory after this cleanup

- Markdown files under `docs/`: **671**
- Live top-level `docs/*.md`: **179**
- Top-level reduction: **52 files**
- Total Markdown reduction: **45 files**


## Existing validator/documentation debt discovered

A literal-marker scan across the existing PowerShell validators found **16 `Require-Text` checks that were already stale in the input baseline**. The cleanup does not introduce these failures: the same 16 checks fail against the pre-cleanup package. They are concentrated in older VR0/VR1/VR2, replacement-long and Plan-Amendment validators whose historical status markers no longer match the current `PROJECT.md`/`ROADMAP.md` wording.

This is important because it explains why some old validator contracts cannot safely be used as a guide for document deletion. A later post-M10 validator-migration phase should either freeze those validators with their historical document snapshots or revise them to consume immutable evidence/contracts instead of mutable current-state prose.

## What remains intentionally separate

- Stable domain/subsystem documents: these represent distinct ownership boundaries and should remain modular.
- ADRs: retain one decision per file for provenance and numbering.
- Milestone documents: retain milestone-level acceptance summaries.
- Files referenced by executable validators/contracts: keep exact paths until a deliberate validator-migration phase.
- Current Attribution 1 implementation/review documents: keep live until the active gate is completed and returned evidence is adjudicated.

## Remaining cleanup opportunities — defer until safe

### After Attribution 1 closes

Move/compact the current Attribution 1 planning/implementation/review chain once no validator requires the exact paths.

### After M10 closes

Perform a validator-path migration that can retire more legacy top-level M10 Final contract documents. This should be a separate documentation/infrastructure refactor with its own static audit because many files that look historical are still required by old validation scripts.

### Optional later reorganization

The 100+ stable technical top-level documents can eventually be grouped into `physics/`, `plant/`, `control/`, `hmi/`, `scenarios/` and `assurance/`, but only together with an automated reference rewrite and validator migration. This is not justified during the active M10 closure branch.
