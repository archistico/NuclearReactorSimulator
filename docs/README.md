# Documentation Map

This directory is the architectural and continuity record for Nuclear Reactor Simulator.

## Start here

- `PROJECT_HANDOFF.md` — **authoritative current checkpoint**, ownership rules and exact continuation point.
- `NEW_CHAT_START.md` — ready-to-paste bootstrap for restarting work in a new conversation.
- `PROJECT_STATUS.md` — current capability map, latest validated checkpoint and deliberate omissions.
- `ROADMAP.md` — milestone sequence, phase gates and future scope.
- `ARCHITECTURE.md` — system composition, state ownership and cross-domain boundaries.
- `milestones/M10.9.2.md` — validated advanced-instrument/gauge baseline.
- `milestones/M10.9.3.md` — validated interactive full-plant mimic baseline.
- `INTERACTIVE_FULL_PLANT_MIMIC.md` — whole-plant mimic contracts, selection and rendering boundary.
- `SUBSYSTEM_ENGINEERING_SCHEMATICS.md` — detailed reactor/primary/turbine/generator/instrumentation engineering schematic grammar.
- `milestones/M10.9.4.md` — validated subsystem-engineering-schematics milestone.
- `M10_9_4_FINAL_MANUAL_VALIDATION_CHECKLIST.md` — completed user-facing M10.9.4 sign-off.
- `milestones/M10.9.4.1.md` — active operational-envelope and numerical-hardening milestone before M10.9.5.
- `M10_9_4_1_A_EXTENDED_AUDIT.md` — executed non-green extended audit and exact ~70-second trip evidence.
- `M10_9_4_1_EXTERNAL_TECHNICAL_AUDIT_REVIEW.md` — adjudication of the two external LLM reviews and accepted planning decisions.
- `OPERATIONAL_ENVELOPE_NUMERICAL_HARDENING_PLAN.md` — revised A.1–I evidence/physics/numerical hardening sequence.
- `REFERENCE_PLANT_SCALE_CONTRACT.md` — validated E.2 current-v2 10 MWe/bidirectional contract and legacy compatibility boundary.
- `ELECTRICAL_PROTECTION_TRAJECTORY_AUDIT.md` — validated E.3.1 evidence-only trajectory plan and artifact contract.
- `M10_9_4_1_E3_2_PROTECTION_EVIDENCE.md` — reviewed normal, reverse-power, underfrequency and phase-slip envelopes plus the derived E.3.2 thresholds.
- `M10_9_4_1_F1_CHOKED_STEAM_FLOW.md` — validated isolated ideal-vapor subcritical/choked capacity contract and pressure-ratio evidence.
- `M10_9_4_1_F2_MAIN_STEAM_RELIEF.md` — conservative current-v2 atmospheric header-relief topology, conservation ownership and deferred scope.
- `M10_9_4_1_F2_VALIDATION_CHECKLIST.md` — focused, ordinary and cumulative F.2 promotion gate.
- `REFERENCE_PLANT_SCALE_EVIDENCE.md` — reproducible E.2 rotor energy, inertia, droop, power-limit and synchronizing-authority calculations.
- `KNOWN_MODEL_LIMITATIONS.md` — current limitations, active hypotheses and deferred fidelity register.
- `GAMEPLAY_LONG_RUNNING_SYSTEM_TESTS.md` — validated 60-second journeys and non-green M10.9.4.1 extended-envelope tier.
- `ADVANCED_INSTRUMENT_GAUGE_SYSTEM.md` — gauge semantics, provenance/quality, off-scale and logical-step trend rules.
- `milestones/M10.9.1.md` — validated HMI information-architecture/visual-language baseline.
- `OPERATOR_EXPERIENCE_HMI_ARCHITECTURE.md` — approved M10.9.1–M10.9.8 operator-experience, schematics, consequence and challenge architecture.
- `HMI_VISUAL_DESIGN_SYSTEM.md` — normative visual design system for shell, alarms, schematics, controls, typography, spacing and UI acceptance.
- `milestones/M10.8.md` / `OPERATOR_COMPUTER_INTEGRATED_UI.md` — validated integrated operator-computer baseline retained beneath the refactor.

## Decision records

`adr/` contains Architecture Decision Records. Later work must preserve accepted decisions unless an explicit superseding ADR is created. The newest control-room/runtime/scenario/fault/replay/fidelity/operator-automation/HMI and hardening decisions are ADR 0046–0114.

## Milestone records

`milestones/` records delivered scope and validation state per milestone. A milestone file describing delivered code does **not** by itself mean the milestone is validated; validation requires explicit local build/test confirmation recorded in the handoff/status/roadmap.

## Domain documents

The remaining top-level Markdown files document subsystem contracts and their ownership boundaries: reactor physics, primary circuit, turbine island, electrical system, control/protection, instrumentation, alarms, control-room presentation, the M7 operating/training framework, M8.1 deterministic fault injection, M8.2 hydraulic component faults, M8.3 instrumentation/control faults, M8.4 secondary-system transients, M8.5 educational leak/LOCA-class scenarios, M8.6 electrical-loss/station-blackout-class scenarios, M8.7 safety-response evaluation/debrief composition, M9.1 recorder/checkpoint/full-replay reconstruction, M9.2 post-incident analysis, validated M9.3 advanced xenon/low-power integration, validated M9.4 spatial/quasi-spatial refinement, validated M9.5 historical-inspired scenario framework, validated M9.6 calibration/reference-validation + GUI hardening, validated M9.7 advanced-fidelity integration gate, validated M10.1–M10.9.2 Hotfix 2 operator-computer/supervisory/session/integrated-UI/HMI/gauge capabilities, validated M10.9.3 interactive full-plant mimic baseline, validated M10.9.4, active M10.9.4.1 operational-envelope/numerical hardening through validated F.1 and candidate F.2, and the approved M10.9.1–M10.9.8 operator-experience architecture.

When modifying a subsystem, update its domain document together with the milestone/ADR/handoff documents rather than leaving architecture knowledge only in source comments or chat history.

## Current restart checkpoint

- M7, M8 and M9 gates are complete / validated.
- M10.1–M10.9.4 and M10.9.4.1-D.4 are validated.
- D.4 validation evidence is 944 ordinary tests plus all 17 unique explicit tests with zero failures.
- M10.9.4.1-D.4.1 remains validated: STOP-owned optional travel rate, differential travel regression, deterministic valve replay/in-flight checkpoint restoration and post-trip reset travel resumption.
- M10.9.4.1-E.3.2 Hotfix 3 remains the validated electrical-protection checkpoint: current-v2 10 MWe signed coupling plus reviewed breaker-supervised delayed electrical protection.
- M10.9.4.1-F.1 is validated: isolated ideal-vapor subcritical/choked capacity law and deterministic pressure-ratio evidence.
- The working source is M10.9.4.1-F.2 CANDIDATE: one conservative pressure-actuated atmospheric header-relief boundary; turbine bypass and enthalpy migration remain deferred.
- M10 closes only after M10.9.8 Integrated Human-Automation-HMI Validation Gate.

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


- `usermanual/MANUALE_UTENTE_NUCLEAR_REACTOR_SIMULATOR.md` — manuale utente educativo e operativo completo.
- `M10_9_4_1_B1_VALIDATION_CHECKLIST.md` — gate di validazione mirato per inventario liquido del corpo cilindrico, feedback SPEED/LOAD e regressione penalità manuali.
- `M10_9_4_1_B2_VALIDATION_CHECKLIST.md` — gate di validazione della sorgente corpo cilindrico→linea vapore basata su pressione, energia e inventario.
- `M10_9_4_1_B3_VALIDATION_CHECKLIST.md` — gate di validazione per diagnostica di basso inventario, allarme livello basso e protezione low-low current-v2.

## M10.9.4.1 Phase-D evidence

- `TURBINE_ADMISSION_AUTHORITY_EVIDENCE.md` — D.2 analytical and breaker-open turbine-admission authority map.
- `TURBINE_GOVERNOR_ACTUATOR_TRACKING_EVIDENCE.md` — D.3 effective-setpoint, PID saturation/anti-windup and physical control-valve tracking audit.
- `M10_9_4_1_D3_VALIDATION_CHECKLIST.md` — superseded D.3 evidence checklist retained as the audit record.
- `TURBINE_ROTOR_MECHANICAL_LOSS_CLOSURE.md` — D.3.1 passive-loss law, energy ownership and breaker-open recovery method.
- `M10_9_4_1_D3_1_VALIDATION_CHECKLIST.md` — cumulative D.3.1 build, ordinary, protection-reset, D.2/D.3 and long-running gates.
- `M10_9_4_1_D4_VALIDATION_CHECKLIST.md` — validated operator turbine-valve station and complete ordinary/explicit gate evidence.
- `M10_9_4_1_D4_1_VALIDATION_CHECKLIST.md` — current candidate gate for STOP travel ownership, replay/checkpoint and trip-reset resumption.
- `adr/0101-governor-effective-setpoint-and-actuator-tracking-are-audited-before-new-anti-windup.md` — evidence boundary before any tracking anti-windup law.
- `adr/0102-current-v2-breaker-open-rotor-has-passive-mechanical-losses.md` — current-v2 passive rotor-loss and explicit recovery decision.
