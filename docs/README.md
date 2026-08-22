# Documentation

The documentation has one rule: **current state is written once**. Historical milestone detail is preserved, but it is not repeated across status, handoff, roadmap and candidate files.

## Read these first

1. **`PROJECT.md`** — authoritative current checkpoint, production policy, active candidate, validation commands and continuation rule.
2. **`ARCHITECTURE.md`** — stable architecture organized by layers, subsystems and ownership boundaries.
3. **`KNOWN_MODEL_LIMITATIONS.md`** — unresolved limitations only.
4. **`ROADMAP.md`** — future work only.
5. **`TOP_LEVEL_DOCUMENT_INDEX.md`** — exhaustive index of live top-level technical/acceptance documents.
6. **`adr/README.md`** — complete ADR index with normalized navigation status and area.

If two historical documents disagree with `PROJECT.md`, `PROJECT.md` is the current source.

## Forward execution plans

- **`FORWARD_EXECUTION_PLAN_M10_9_7_TO_M15.md`** — detailed execution sequence, implementation slices, gates and deferred-item ownership from the active M10.9.7 Mission/Performance work through M15.
- **`M10_9_7_3_HOTFIX1_REV2_DOCS3_ALIGNMENT.md`** — documentation-only checkpoint for the post-7.3 App desktop-host/session-integrity review and its pre-7.4/M11/M13 ownership decisions.

The approved post-Phase-I implementation sequence is documented in `ROADMAP.md`. Detailed milestone contracts are:

- `milestones/M10.9.5.md` — Contextual Command Consequence Model;
- `milestones/M10.9.6.md` — Operational Challenge & Energy-Demand Framework;
- `milestones/M10.9.7.md` — Mission & Performance Workstation;
- `milestones/M10.9.8.md` — Integrated Human-Automation-HMI Validation Gate;
- `milestones/M11.md` — Release Hardening;
- `milestones/M12.md` — Extreme Operations Foundations (Epic A foundation);
- `milestones/M13.md` — Control-Room Experience (Epic C);
- `milestones/M14.md` — Spatial Reactor (Epic B);
- `milestones/M15.md` — Accident Progression & Consequence Models (Epic A consequence phase).

These are planning contracts, not current-status sources. `PROJECT.md` remains authoritative for what is active now. The post-M11 Epic A/B/C direction is mapped to M12–M15 in `ROADMAP.md`; the long-lived rationale and acceptance principles remain in `FUTURE_GAMEPLAY_CONTROL_ROOM_AND_ACCIDENT_DIRECTION.md`.

## Technical reference

Use [`TOP_LEVEL_DOCUMENT_INDEX.md`](TOP_LEVEL_DOCUMENT_INDEX.md) for the **complete** live top-level documentation catalog. The short lists below are curated entry points only.

### Physics and numerical model

- [`PHYSICAL_QUANTITIES.md`](PHYSICAL_QUANTITIES.md)
- [`PLANT_COMPOSITION.md`](PLANT_COMPOSITION.md)
- [`PLANT_NETWORK_ORCHESTRATION.md`](PLANT_NETWORK_ORCHESTRATION.md)
- [`PIPES_AND_FLOW.md`](PIPES_AND_FLOW.md)
- [`WATER_STEAM_MODEL.md`](WATER_STEAM_MODEL.md)
- [`MAIN_CIRCULATION_SYSTEM.md`](MAIN_CIRCULATION_SYSTEM.md)
- [`PUMPS.md`](PUMPS.md)
- [`VALVES.md`](VALVES.md)
- [`NEUTRON_KINETICS.md`](NEUTRON_KINETICS.md)
- [`CONTROL_RODS.md`](CONTROL_RODS.md)
- [`DECAY_HEAT.md`](DECAY_HEAT.md)
- [`KNOWN_MODEL_LIMITATIONS.md`](KNOWN_MODEL_LIMITATIONS.md)

### Operations, challenge, replay and persistence

- [`INITIAL_CONDITIONS_SCENARIO_FRAMEWORK.md`](INITIAL_CONDITIONS_SCENARIO_FRAMEWORK.md)
- [`DETERMINISTIC_FAULT_INJECTION_FRAMEWORK.md`](DETERMINISTIC_FAULT_INJECTION_FRAMEWORK.md)
- [`OPERATIONAL_CHALLENGE_LIFECYCLE.md`](OPERATIONAL_CHALLENGE_LIFECYCLE.md)
- [`OPERATIONAL_CHALLENGE_ENERGY_DEMAND.md`](OPERATIONAL_CHALLENGE_ENERGY_DEMAND.md)
- [`OPERATIONAL_CHALLENGE_SCORING.md`](OPERATIONAL_CHALLENGE_SCORING.md)
- [`OPERATIONAL_CHALLENGE_PACKS.md`](OPERATIONAL_CHALLENGE_PACKS.md)
- [`RECORDER_CHECKPOINT_FULL_REPLAY.md`](RECORDER_CHECKPOINT_FULL_REPLAY.md)
- [`APPLICATION_RECORDING_REPLAY_REVIEW.md`](APPLICATION_RECORDING_REPLAY_REVIEW.md)
- [`PERSISTENCE_PAYLOAD_INTEGRITY_ERROR_CONTRACT.md`](PERSISTENCE_PAYLOAD_INTEGRITY_ERROR_CONTRACT.md)
- [`POST_INCIDENT_ANALYSIS.md`](POST_INCIDENT_ANALYSIS.md)

### HMI and operator experience

- [`usermanual/MANUALE_UTENTE_NUCLEAR_REACTOR_SIMULATOR.md`](usermanual/MANUALE_UTENTE_NUCLEAR_REACTOR_SIMULATOR.md) — manuale utente educativo e operativo in italiano;
- [`OPERATOR_EXPERIENCE_HMI_ARCHITECTURE.md`](OPERATOR_EXPERIENCE_HMI_ARCHITECTURE.md)
- [`HMI_VISUAL_DESIGN_SYSTEM.md`](HMI_VISUAL_DESIGN_SYSTEM.md)
- [`INTERACTIVE_FULL_PLANT_MIMIC.md`](INTERACTIVE_FULL_PLANT_MIMIC.md)
- [`SUBSYSTEM_ENGINEERING_SCHEMATICS.md`](SUBSYSTEM_ENGINEERING_SCHEMATICS.md)
- [`OPERATOR_COMPUTER_INTEGRATED_UI.md`](OPERATOR_COMPUTER_INTEGRATED_UI.md)
- [`MISSION_PERFORMANCE_PRESENTATION_CONTRACT.md`](MISSION_PERFORMANCE_PRESENTATION_CONTRACT.md)
- [`MISSION_PERFORMANCE_WORKSTATION_NAVIGATION.md`](MISSION_PERFORMANCE_WORKSTATION_NAVIGATION.md)
- [`MISSION_PERFORMANCE_LIVE_WORKSPACE.md`](MISSION_PERFORMANCE_LIVE_WORKSPACE.md)
- [`MISSION_PERFORMANCE_DETERMINISTIC_TIMELINE.md`](MISSION_PERFORMANCE_DETERMINISTIC_TIMELINE.md)
- [`MISSION_PERFORMANCE_CLOSURE.md`](MISSION_PERFORMANCE_CLOSURE.md)
- [`M10_9_8_1_VALIDATION_MATRIX.md`](M10_9_8_1_VALIDATION_MATRIX.md)
- [`M10_9_8_1_MATRIX_ACCEPTANCE_CHECKLIST.md`](M10_9_8_1_MATRIX_ACCEPTANCE_CHECKLIST.md)
- [`M10_9_8_2_AUTOMATED_HEALTHY_ASSISTANCE_AUTHORITY_MATRIX.md`](M10_9_8_2_AUTOMATED_HEALTHY_ASSISTANCE_AUTHORITY_MATRIX.md)
- [`M10_9_8_2_HOTFIX1_MANUAL_SMOKE_CHECKLIST.md`](M10_9_8_2_HOTFIX1_MANUAL_SMOKE_CHECKLIST.md)
- [`M10_9_8_2_REV5_INTERACTIVE_LIST_STABILITY_AUDIT.md`](M10_9_8_2_REV5_INTERACTIVE_LIST_STABILITY_AUDIT.md)
- [`M10_9_8_3_DEGRADED_FAULT_PROTECTION_TAKEOVER_MATRIX.md`](M10_9_8_3_DEGRADED_FAULT_PROTECTION_TAKEOVER_MATRIX.md)
- [`M10_9_8_4_REPLAY_CHECKPOINT_SAME_SEED_INTEGRITY.md`](M10_9_8_4_REPLAY_CHECKPOINT_SAME_SEED_INTEGRITY.md)
- [`M10_FINAL_PRE_M11_VALIDATION_PLAN.md`](M10_FINAL_PRE_M11_VALIDATION_PLAN.md)

### Documentation governance

- [`DOCUMENTATION_ARCHITECTURE_AND_INDEXING.md`](DOCUMENTATION_ARCHITECTURE_AND_INDEXING.md)
- [`adr/README.md`](adr/README.md) — complete ADR index and normalized navigation status;
- [`history/ARCHITECTURE_MILESTONE_LEDGER.md`](history/ARCHITECTURE_MILESTONE_LEDGER.md) — frozen milestone-led architecture provenance.

## Structured collections

- [`adr/README.md`](adr/README.md) — indexed architectural decisions, including superseded decisions when provenance matters;
- `milestones/` — milestone summaries and approved forward milestone planning contracts; **not** current-status sources;
- `reference/` — supporting reference assets;
- `research/` — non-authoritative research notes;
- `usermanual/` — user-facing manual;
- `history/` — completed/superseded engineering chronology and old administrative snapshots.

## Evidence policy

Large generated audit payloads are not documentation and are not bundled in source candidates. Current contracts use:

- `../eng/frozen-evidence/ordinary/` for small immutable ordinary-test prerequisites;
- `../eng/frozen-evidence/large-payload-manifest.csv` for omitted large-trace identities;
- `../eng/evidence-manifests/` for compact decision/reference provenance;
- local `artifacts/` directories for generated validation output.

## Maintenance rule

Do not create another status/handoff/restart/candidate-summary file when the information belongs in `PROJECT.md`. Create a new document only when it has a distinct responsibility: stable architecture, subsystem reference, limitation register, ADR, user manual, research, acceptance artifact or historical provenance.

Acceptance checklists are milestone artifacts, not stable architecture references. They remain discoverable through [`TOP_LEVEL_DOCUMENT_INDEX.md`](TOP_LEVEL_DOCUMENT_INDEX.md) and their owning milestone documents rather than being appended ad hoc to this curated README.
