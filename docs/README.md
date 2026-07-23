# Documentation Map

This directory is the architectural and continuity record for Nuclear Reactor Simulator.

## Start here

- `PROJECT_HANDOFF.md` — **authoritative current checkpoint**, ownership rules and exact continuation point.
- `NEW_CHAT_START.md` — ready-to-paste bootstrap for restarting work in a new conversation.
- `PROJECT_STATUS.md` — current capability map, current candidate and deliberate omissions.
- `ROADMAP.md` — milestone sequence, phase gates and future scope.
- `ARCHITECTURE.md` — system composition, state ownership and cross-domain boundaries.
- `milestones/M10.9.2.md` — validated advanced-instrument/gauge baseline.
- `milestones/M10.9.3.md` — current interactive full-plant mimic candidate and validation boundary.
- `INTERACTIVE_FULL_PLANT_MIMIC.md` — whole-plant mimic contracts, selection and rendering boundary.
- `SUBSYSTEM_ENGINEERING_SCHEMATICS.md` — detailed reactor/primary/turbine/generator/instrumentation engineering schematic grammar.
- `GAMEPLAY_LONG_RUNNING_SYSTEM_TESTS.md` — separately runnable explicit long operator-journey/endurance acceptance tests.
- `ADVANCED_INSTRUMENT_GAUGE_SYSTEM.md` — gauge semantics, provenance/quality, off-scale and logical-step trend rules.
- `milestones/M10.9.1.md` — validated HMI information-architecture/visual-language baseline.
- `OPERATOR_EXPERIENCE_HMI_ARCHITECTURE.md` — approved M10.9.1–M10.9.8 operator-experience, schematics, consequence and challenge architecture.
- `milestones/M10.8.md` / `OPERATOR_COMPUTER_INTEGRATED_UI.md` — validated integrated operator-computer baseline retained beneath the refactor.

## Decision records

`adr/` contains Architecture Decision Records. Later work must preserve accepted decisions unless an explicit superseding ADR is created. The newest control-room/runtime/scenario/fault/replay/fidelity/operator-automation/HMI decisions are ADR 0046–0076.

## Milestone records

`milestones/` records delivered scope and validation state per milestone. A milestone file describing delivered code does **not** by itself mean the milestone is validated; validation requires explicit local build/test confirmation recorded in the handoff/status/roadmap.

## Domain documents

The remaining top-level Markdown files document subsystem contracts and their ownership boundaries: reactor physics, primary circuit, turbine island, electrical system, control/protection, instrumentation, alarms, control-room presentation, the M7 operating/training framework, M8.1 deterministic fault injection, M8.2 hydraulic component faults, M8.3 instrumentation/control faults, M8.4 secondary-system transients, M8.5 educational leak/LOCA-class scenarios, M8.6 electrical-loss/station-blackout-class scenarios, M8.7 safety-response evaluation/debrief composition, M9.1 recorder/checkpoint/full-replay reconstruction, M9.2 post-incident analysis, validated M9.3 advanced xenon/low-power integration, validated M9.4 spatial/quasi-spatial refinement, validated M9.5 historical-inspired scenario framework, validated M9.6 calibration/reference-validation + GUI hardening, validated M9.7 advanced-fidelity integration gate, validated M10.1–M10.9.2 Hotfix 2 operator-computer/supervisory/session/integrated-UI/HMI/gauge capabilities, validated M10.9.2 advanced-instrument/gauge baseline and current M10.9.3 interactive full-plant mimic candidate and the approved M10.9.1–M10.9.8 operator-experience architecture.

When modifying a subsystem, update its domain document together with the milestone/ADR/handoff documents rather than leaving architecture knowledge only in source comments or chat history.

## Current restart checkpoint

At this documentation refresh:

- M7 gate is complete / validated.
- M8.1–M8.7 hotfix 2 are validated and the M8 gate is complete.
- M9.1–M9.7 are validated and the M9 gate is complete.
- M9 gate: COMPLETE / VALIDATED.
- M10.1–M10.9.2 Hotfix 2 are validated.
- Current validated baseline: M10.9.1 HMI Information Architecture & Visual Language.
- Current implementation candidate: M10.9.3 Interactive Full-Plant Mimic.
- M10 closes after M10.9.8 Integrated Human-Automation-HMI Validation Gate.
- M10 is IN PROGRESS under the approved Operator Computer & Supervisory Automation architecture.

See `PROJECT_HANDOFF.md` for the full authoritative statement.

- `SAFETY_RESPONSE_SCENARIO_PACK.md` — M8.7 capstone safety-response scenarios, acceptance/scoring and logical operator-action debrief boundary.

- `RECORDER_CHECKPOINT_FULL_REPLAY.md` — M9.1 every-step recorder, versioned replay-backed checkpoints, fingerprint contract and full replay/seek boundary.

- `POST_INCIDENT_ANALYSIS.md` — M9.2 deterministic evidence windows, response metrics, checkpoint linkage and debrief-report semantics.

- `ADVANCED_XENON_LOW_POWER_TRANSIENTS.md` — M9.3 canonical M2.8 poison runtime integration, exact-version compatibility and xenon/low-power scenario boundaries.

- `SPATIAL_QUASI_SPATIAL_FIDELITY.md` — validated M9.4 opt-in committed-state local feedback weighting, explicit zone coupling and deterministic aggregated-core power-shape refinement.

- `HISTORICAL_INSPIRED_SCENARIO_FRAMEWORK.md` — M9.5 versioned provenance, claim classification, capability review and fail-closed historical-inspired scenario loading.

- `OPERATOR_COMPUTER_SUPERVISORY_AUTOMATION.md` — approved M10 fixed-page terminal, dual assistance/control-authority model, M5 supervisory ownership, degraded/manual-takeover and replay-backed session-persistence plan.

- `CALIBRATION_REFERENCE_VALIDATION.md` — M9.6 versioned reference cases, tolerance budgets, model-version tracking and sensitivity reports
- `MANUAL_GUI_VALIDATION_CHECKLIST.md` — M9.6-origin manual desktop checklist, now carried into the final M9.7 phase-gate evidence

- `M9_ADVANCED_FIDELITY_INTEGRATION_GATE.md` — M9.7 cross-feature replay/fidelity/calibration/UI integration invariants and phase-gate semantics.
- `M9_FINAL_MANUAL_VALIDATION_CHECKLIST.md` — final desktop GUI validation evidence required before M9 gate completion and M10.

- `OPERATOR_COMPUTER_INFORMATION_GUIDANCE_DIAGNOSTICS.md` — M10.2 canonical GUIDANCE/INFO/DIAGNOSTICS projection contract.
- `milestones/M10.2.md` — M10.2 implementation-candidate milestone record.

- `OPERATOR_COMPUTER_ALARM_LOG_INCIDENT_WORKSTATION.md` — M10.3 read-only alarm, bounded live history, optional M9.1 session evidence and optional M9.2 incident workstation contracts.
- `OPERATOR_COMPUTER_CONTEXTUAL_COMMAND_CONSOLE.md` — M10.4 contextual typed-command catalog, advisory availability/block reasons and canonical dispatcher boundary.
- `DUAL_ASSISTANCE_CONTROL_AUTHORITY.md` — M10.5 independent training-assistance vs physical plant-control-authority model and replay semantics.
- `SUPERVISORY_AUTOMATIC_OPERATION.md` — M10.6 M5-owned bounded supervisory objectives, measured-signal degradation, protection priority and bumpless takeover.

- `OPERATOR_COMPUTER_SESSION_CHECKPOINT_REPLAY_SAVE.md` — M10.7 replay-backed session/checkpoint/save/load workspace.
