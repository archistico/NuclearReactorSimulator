# Documentation

The documentation follows one rule: **current state is written once**. Historical gate detail is retained for provenance, but completed checklists, hotfix notes and diagnostic chains should not compete with the current project view.

## Start here

1. **`PROJECT.md`** — authoritative current checkpoint, active candidate, validation commands and continuation rule.
2. **`ROADMAP.md`** — future work and branch/stop rules only.
3. **`ARCHITECTURE.md`** — stable layer/subsystem ownership.
4. **`KNOWN_MODEL_LIMITATIONS.md`** — current unresolved model limitations.
5. **`TOP_LEVEL_DOCUMENT_INDEX.md`** — exhaustive index of live top-level documents.
6. **`adr/README.md`** — architectural decision ledger.
7. **`history/README.md`** — historical and consolidated provenance.

If an older document conflicts with `PROJECT.md`, use `PROJECT.md` for the current state.

## Authority model

| Need | Authoritative location |
| --- | --- |
| Current baseline / active candidate / commands | `PROJECT.md` |
| Stable architecture and ownership | `ARCHITECTURE.md` |
| Future execution only | `ROADMAP.md` + milestone plans |
| Model limitations | `KNOWN_MODEL_LIMITATIONS.md` |
| Architectural decisions | `adr/README.md` + ADRs |
| Live-document discovery | `TOP_LEVEL_DOCUMENT_INDEX.md` |
| Historical chronology / completed gates | `history/` |
| User operating guidance | `usermanual/` |

Do not create parallel `CURRENT_STATUS`, `CHAT_HANDOFF` or `RESTART` documents. New-chat restart instructions belong in `PROJECT.md`.

## Current M10 / VR2 working set

The current execution branch is intentionally small. Use `PROJECT.md` and `ROADMAP.md` for status; the active executable-contract documents remain at their existing paths because validators depend on them.

- `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1.md`
- `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_RUNTIME_FACTOR_ISOLATION_PLANNING1.md`
- `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_RUNTIME_FACTOR_ISOLATION_PLANNING1_PREEXECUTION_REVIEW.md`
- `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_EXACT_V9_WALL_CLOCK_TAIL_ATTRIBUTION1.md`
- `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_EXACT_V9_WALL_CLOCK_TAIL_ATTRIBUTION1_PREEXECUTION_REVIEW.md`
- `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_EXACT_V9_WALL_CLOCK_TAIL_ATTRIBUTION1_REV1_CONCLUSIVE_REVIEW.md`
- `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION2.md`
- `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION2_PREEXECUTION_REVIEW.md`
- `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION2_RETURNED_EVIDENCE_ADJUDICATION.md`
- `M10_FINAL_VR2_R1_IMPLEMENTATION_PLANNING1.md`
- `M10_FINAL_VR2_R1_IMPLEMENTATION_PLANNING1_PREEXECUTION_REVIEW.md`
- `M10_FINAL_VR2_R1_IMPLEMENTATION_PLANNING1_RETURNED_EVIDENCE_ADJUDICATION.md`
- `M10_FINAL_VR2_R2_FOCUSED_THERMODYNAMIC_REFERENCE_TOPOLOGY_QUALIFICATION_PLANNING1.md`
- `M10_FINAL_VR2_R2_FOCUSED_THERMODYNAMIC_REFERENCE_TOPOLOGY_QUALIFICATION_PLANNING1_RETURNED_EVIDENCE_ADJUDICATION.md`
- `M10_FINAL_VR2_R3_SHORT_EXACT_V9_EQUIVALENT_SHADOW_COMPOSITION_REQUALIFICATION_PLANNING1.md`
- `M10_FINAL_VR2_R3_SHORT_EXACT_V9_EQUIVALENT_SHADOW_COMPOSITION_REQUALIFICATION_PLANNING1_RETURNED_EVIDENCE_ADJUDICATION.md`
- `M10_FINAL_VR2_R3_SHORT_EXACT_V9_EQUIVALENT_SHADOW_COMPOSITION_REQUALIFICATION1.md`
- `M10_FINAL_VR2_R3_SHORT_EXACT_V9_EQUIVALENT_SHADOW_COMPOSITION_REQUALIFICATION1_PREQUALIFICATION_REVIEW.md`

Completed RP1B/RP1C intermediate notes that no longer participate in executable contracts are consolidated under `history/m10-final/`.

## Stable technical documentation

Domain and subsystem documentation remains intentionally modular: reactor physics, thermal hydraulics, plant topology, control/protection, operator-computer/HMI, replay/persistence and training/scenario concerns have different ownership and should not be collapsed into milestone narratives. Use `TOP_LEVEL_DOCUMENT_INDEX.md` to discover them.

## Milestones, research and history

- `milestones/` — milestone plans and acceptance summaries.
- `research/` — source reviews and traceability material.
- `history/` — superseded project snapshots and consolidated completed-gate dossiers.
- `history/DOCUMENTATION_CLEANUP_AUDIT_2026-09-17.md` — inventory, consolidation rationale and deferred cleanup risks.
- `history/m10-validation/` — compacted legacy M10 validation/checklist material.
- `history/m10-final/` — completed long/replacement-long and VR2/RP1 histories.

Historical files are provenance, not current navigation authority.

## Executable-contract documents retained at top level

Some older-looking files remain live because PowerShell validators/contracts reference their exact names or content. Do not move or merge them until the corresponding validator migration is explicitly planned. Important retained examples include:

- `M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md`
- `M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P2_DECISION.md`
- `M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P2R_DECISION_PLAN_AMENDMENT2.md`
- `M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P2R2_DECISION_REENTRY2.md`
- `PRE_M11_PLANT_DYNAMICS_THERMAL_HYDRAULICS_REACTOR_PHYSICS_REVIEW.md`
- `PRE_M11_DEEP_ENGINEERING_SECTION_REVIEW_2.md`
- `PRE_M11_TODREAS_KAZIMI_THERMAL_HYDRAULIC_DEEP_REVIEW_PASS1.md`
- `PRE_M11_TODREAS_KAZIMI_THERMAL_HYDRAULIC_DEEP_REVIEW_PASS2.md`

These names are also retained here for backward-compatible validator markers.

## Maintenance rules

- `PROJECT.md` must not become a chronology ledger.
- `ROADMAP.md` must not duplicate the current validation checkpoint.
- `ARCHITECTURE.md` is organized by ownership, not by milestone chronology.
- Completed gate/hotfix/pre-execution documents should move to `history/` or a historical dossier once no executable contract references them.
- A dossier must retain an original-file manifest and normalized-LF SHA-256 values before source files are removed.
- Live relative Markdown links must resolve.
- Every live top-level `docs/*.md` file must appear in `TOP_LEVEL_DOCUMENT_INDEX.md`.
- ADR numbering/status/indexing remains governed by `adr/README.md`.

## Pre-M11 review anchors

The following retained references remain part of existing validator contracts and review provenance:

- `PRE_M11_PLANT_DYNAMICS_THERMAL_HYDRAULICS_REACTOR_PHYSICS_REVIEW.md`
- `PRE_M11_DEEP_ENGINEERING_SECTION_REVIEW_2.md`
- `PRE_M11_TODREAS_KAZIMI_THERMAL_HYDRAULIC_DEEP_REVIEW_PASS1.md`
- `PRE_M11_TODREAS_KAZIMI_THERMAL_HYDRAULIC_DEEP_REVIEW_PASS2.md`

For full source/section traceability, use the matching files in `research/` and the exhaustive top-level index.

### Historical RP1C evidence state (2026-09-18)
Runtime Factor Isolation 1, Dynamic PGO Comparator 1 and Runtime Configuration Impact Assessment 1 are returned/adjudicated. TieredCompilation is materially causal for the gross slowdown; Dynamic PGO ON improves the central exact-v9 distribution; A2 then shows both ambient and explicit profiles preserve ordinary/replay/non-VR2 behavior on one host. `AMBIENT-UNSET` records zero strict exact-v9 misses and is sufficient for the next qualification step without introducing a production runtime override. Internal .NET default identity is not claimed.

## Historical executable M10 gate — FDPC2 (superseded)

FDPC2 Planning 1 returned `PASS-AS-AUTHORED`. At that historical checkpoint the only live Branch A gate was evidence-only `RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION2`: immutable C4, `AMBIENT-UNSET`, same A2 host/power scheme, five fresh full-domain processes and unchanged thresholds. FDPC1 remains negative historical evidence. RP1C selection and every production/runtime change remain blocked until returned FDPC2 adjudication.


### Historical M10 VR2 checkpoint — FDPC2 returned / RP1C Selection Planning 1 (superseded)

FDPC2 returned green on the frozen A2 host. `C4 + AMBIENT-UNSET` became the sole selection-ready candidate/runtime pair; D3 remained blocked by its frozen seam maximum. At that historical checkpoint the only live gate was planning-only `RP1C-ENGINEERING-REPAIR-SELECTION-PLANNING1`; this is superseded by returned Selection 1 / R1 Implementation Planning 1.

### Historical M10 VR2 checkpoint — Selection Planning 1 returned / Selection 1 (superseded)

Selection Planning 1 returned `PASS-AS-AUTHORED`. At that historical checkpoint the only live gate was decision-only `RP1C-ENGINEERING-REPAIR-SELECTION1`; this is superseded by returned Selection 1 / R1 Implementation Planning 1. It preserves `SELECT-C4 | SELECT-NONE`, performs no measurement, and contains an authored `SELECT-C4` decision supported by the frozen readiness evidence. Production repair remains unauthorized until returned selection adjudication and later R1 planning/implementation gates.

### Historical M10 VR2 checkpoint -- Selection returned / R1 Implementation Planning 1 (superseded)

RP1C Selection 1 returned `SELECT-C4`; this planning checkpoint is superseded by returned `PASS-AS-AUTHORED` R1 Implementation Planning 1.

### Historical M10 VR2 checkpoint -- R1 Selected C4 Opt-In Closure Implementation 1 (closed)

R1 Implementation Planning 1 returned `PASS-AS-AUTHORED`; the implementation gate later returned complete evidence and is adjudicated PASS. Mode 2 is qualified as opt-in implementation evidence only.

