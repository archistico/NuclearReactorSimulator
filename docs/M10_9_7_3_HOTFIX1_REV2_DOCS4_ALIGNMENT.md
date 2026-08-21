# M10.9.7.3 Hotfix 1 REV2 Docs4 — Documentation Architecture / Indexing / Limitations Alignment

## Scope

Documentation-only alignment over the unchanged M10.9.7.3 Hotfix 1 REV2 runtime/test/script candidate. Automated build/ordinary/focused evidence already reported for REV2 remains applicable because Docs4 changes no source, test, script, build, CI or evidence contract.

## What Docs4 changes

- reorganizes `ARCHITECTURE.md` by stable layer/subsystem ownership and removes stale current-checkpoint narration;
- preserves the former milestone-led architecture additions in `history/ARCHITECTURE_MILESTONE_LEDGER.md`;
- restores `ROADMAP.md` to future-only content by removing completed-milestone status duplication;
- adds exhaustive `TOP_LEVEL_DOCUMENT_INDEX.md` while keeping `README.md` curated;
- adds `adr/README.md` with normalized navigation status/area and standardizes ADR status headings;
- adds `DOCUMENTATION_ARCHITECTURE_AND_INDEXING.md` and ADR-0186;
- expands `KNOWN_MODEL_LIMITATIONS.md` with the stateless relief/bypass no-blowdown/reseat limitation and moves exact regression precision back to machine evidence;
- explicitly documents that physical control-rod travel rate is already stateful/deterministic and is not an instantaneous-motion limitation;
- fixes the live `milestones/M10.9.4.md` root-CHANGELOG/history references;
- assigns automated documentation-index/status/link/source-comment drift checks to M11.5.

## Important review correction

The App/Domain review inference that control rods move instantaneously is not supported by the current physical model. `ControlRodDefinition.TravelRate` and `ControlRodMotionSolver` own deterministic rod travel. The null generic `ActuatorDefinition.ControlRod` travel-rate field means only that controller-side actuator ramping is not duplicated above the physical rod owner.

## Validation state

Docs4 does not promote M10.9.7.3 Hotfix 1 REV2. The only remaining promotion requirement remains the manual HMI checklist recorded in `PROJECT.md`.

## Static documentation audit

- Markdown files checked: 585;
- live top-level Markdown files indexed: 124 / 124;
- ADR files indexed: 186 / 186 (`0001`–`0186`, contiguous);
- ADR files with `## Status`: 186 / 186;
- relative Markdown links missing: 0;
- `ARCHITECTURE.md` milestone-led headings: 0;
- `ARCHITECTURE.md` current-checkpoint section: absent;
- concrete `VALIDATED / CLOSED` status blocks in `ROADMAP.md`: 0;
- `src/`, `tests/`, `scripts/`, `eng/`, `.github/`: byte-identical to Docs3.
