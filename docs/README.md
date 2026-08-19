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
- `M10_9_4_1_F2_VALIDATION_CHECKLIST.md` — validated F.2 promotion evidence.
- `M10_9_4_1_F3_TURBINE_BYPASS.md` — internal header-to-condenser bypass ownership, backpressure and conservation contract.
- `M10_9_4_1_F3_VALIDATION_CHECKLIST.md` — validated F.3 promotion gate.
- `M10_9_4_1_G1_OPEN_CONTROL_VOLUME_ENERGY.md` — target enthalpy convention, audit-only solver and phased migration boundary.
- `M10_9_4_1_G1_VALIDATION_CHECKLIST.md` — validated focused, ordinary and cumulative G.1 promotion gate.
- `M10_9_4_1_G2_PASSIVE_HYDRAULIC_ENTHALPY.md` — validated passive pipe/valve enthalpy migration and pump-work ownership contract.
- `M10_9_4_1_G2_VALIDATION_CHECKLIST.md` — validated G.2 Hotfix 2 gate and evidence.
- `M10_9_4_1_G3_REMAINING_NON_TURBINE_ENTHALPY.md` — validated remaining non-turbine enthalpy-migration contract.
- `M10_9_4_1_G3_VALIDATION_CHECKLIST.md` — validated G.3 promotion gate.
- `M10_9_4_1_G4_TURBINE_EXPANSION_ENTHALPY.md` — current turbine-expansion enthalpy/shaft-work ownership contract.
- `M10_9_4_1_G4_VALIDATION_CHECKLIST.md` — focused, ordinary and cumulative G.4 promotion gate.
- `REFERENCE_PLANT_SCALE_EVIDENCE.md` — reproducible E.2 rotor energy, inertia, droop, power-limit and synchronizing-authority calculations.
- `KNOWN_MODEL_LIMITATIONS.md` — current limitations, active hypotheses and deferred fidelity register.
- `GAMEPLAY_LONG_RUNNING_SYSTEM_TESTS.md` — validated 60-second journeys and non-green M10.9.4.1 extended-envelope tier.
- `ADVANCED_INSTRUMENT_GAUGE_SYSTEM.md` — gauge semantics, provenance/quality, off-scale and logical-step trend rules.
- `milestones/M10.9.1.md` — validated HMI information-architecture/visual-language baseline.
- `OPERATOR_EXPERIENCE_HMI_ARCHITECTURE.md` — approved M10.9.1–M10.9.8 operator-experience, schematics, consequence and challenge architecture.
- `HMI_VISUAL_DESIGN_SYSTEM.md` — normative visual design system for shell, alarms, schematics, controls, typography, spacing and UI acceptance.
- `FUTURE_GAMEPLAY_CONTROL_ROOM_AND_ACCIDENT_DIRECTION.md` — approved post-hardening direction for extreme operation/accident progression, spatial 2D core, IndustrialControls-based control-room refresh, mimic layout, procedures/presets and Instructor mode.
- `M10_9_4_1_H1_NUMERICAL_STIFFNESS_EVIDENCE.md` / `M10_9_4_1_H1_VALIDATION_CHECKLIST.md` — fixed-step refinement evidence and validation gate before the H.2 numerical-method decision.
- `M10_9_4_1_H2_NUMERICAL_METHOD_DECISION.md` / `M10_9_4_1_H2_VALIDATION_CHECKLIST.md` — validated evidence-derived selection of deterministic semi-implicit pressure/flow coupling; H.2 keeps production explicit at 10 ms.
- `M10_9_4_1_H3_SEMI_IMPLICIT_HYDRAULIC_PROTOTYPE.md` / `M10_9_4_1_H3_VALIDATION_CHECKLIST.md` — validated isolated frozen-forcing prototype: material chatter reduction, exact conservation/determinism, but ~15.895x full-time cost.
- `M10_9_4_1_H4_HYBRID_SEMI_IMPLICIT_ACTIVATION_GATE.md` / `M10_9_4_1_H4_VALIDATION_CHECKLIST.md` — validated deterministic explicit-predictor/semi-implicit-corrector sweep and bounded-work activation decision gate.
- `M10_9_4_1_H5_HYBRID_PRODUCTION_INTEGRATION.md` / `M10_9_4_1_H5_VALIDATION_CHECKLIST.md` — validated production rollback and 5 s extended shadow qualification; 7/500 corrections, 5/7 convergent, production explicit.
- `M10_9_4_1_H6_CORRECTOR_RESCUE_ENVELOPE.md` / `M10_9_4_1_H6_VALIDATION_CHECKLIST.md` — validated bounded primary/rescue Picard envelope on the exact H.5 difficult intervals.
- `M10_9_4_1_H7_CORRECTOR_ALGORITHM_REVISION.md` — validated true fixed-point residual and deterministic-backtracking corrector revision.
- `M10_9_4_1_H8_ACCELERATED_NONLINEAR_HYDRAULIC_CORRECTOR.md` — validated safeguarded-Anderson acceleration study.
- `M10_9_4_1_H9_JACOBIAN_INFORMED_NONLINEAR_HYDRAULIC_CORRECTOR.md` / `M10_9_4_1_H9_VALIDATION_CHECKLIST.md` — validated conservative-coordinate finite-difference Jacobian / damped-Newton study.
- `M10_9_4_1_H10_HYDRAULIC_MAP_SWITCHING_NONSMOOTHNESS_DIAGNOSIS.md` / `M10_9_4_1_H10_VALIDATION_CHECKLIST.md` — validated diagnosis separating hydraulic smoothness from thermodynamic switching at the two persistent failures.
- `M10_9_4_1_H11_THERMODYNAMIC_SWITCHING_LOCALIZATION_ACTIVE_SET_DIAGNOSIS.md` / `M10_9_4_1_H11_VALIDATION_CHECKLIST.md` — validated localization of the original interval-200/360 thermodynamic phase boundaries.
- `M10_9_4_1_H12_THERMODYNAMIC_INVERSE_BRANCH_SELECTION_AUDIT.md` / `M10_9_4_1_H12_VALIDATION_CHECKLIST.md` — validated inverse-map overlapping-root/coarse-priority diagnosis.
- `M10_9_4_1_H13_THERMODYNAMIC_BRANCH_CONTINUITY_HYSTERESIS_SHADOW_EXPERIMENT.md` / `M10_9_4_1_H13_VALIDATION_CHECKLIST.md` — validated targeted branch-continuity/hysteresis experiment.
- `M10_9_4_1_H14_BROADER_THERMODYNAMIC_BRANCH_CONTINUITY_SHADOW_QUALIFICATION.md` / `M10_9_4_1_H14_VALIDATION_CHECKLIST.md` — validated broader 2,000-interval qualification evidence, 14/15 with interval 723 as the sole failure.
- `M10_9_4_1_H15_EXTENDED_TRIGGER_723_ROOT_CAUSE_DIAGNOSIS.md` / `M10_9_4_1_H15_VALIDATION_CHECKLIST.md` — validated interval-723 all-path/all-node root-cause diagnosis.
- `M10_9_4_1_H16_EXTENDED_THREE_NODE_BRANCH_CONTINUITY_QUALIFICATION.md` / `M10_9_4_1_H16_VALIDATION_CHECKLIST.md` — validated three-node `steam|stop-out|header` qualification that recovered the H.15-localized interval 723 and established the pre-H.17 baseline.
- `milestones/M10.8.md` / `OPERATOR_COMPUTER_INTEGRATED_UI.md` — validated integrated operator-computer baseline retained beneath the refactor.

- `M10_9_4_1_PHASE_H_COMPLETION_ROADMAP_H24_H30.md` — authoritative H.24–H.30 completion sequence from committed duration through protection/transients, rollback, off-design, performance, activation candidate and final Phase H closure.
- `M10_9_4_1_H24_COMMITTED_LONG_HORIZON_CROSS_PROFILE_QUALIFICATION.md` / `M10_9_4_1_H24_VALIDATION_CHECKLIST.md` — validated H.24 committed 30,000-interval four-profile qualification contract; focused duration 4h31m55s, retained as a rare qualification gate.
- `M10_9_4_1_H24_STATIC_REVIEW.md` — package-time H.24 structural/provenance review.
- `M10_9_4_1_H25_COMMITTED_PROTECTION_OPERATIONAL_TRANSIENT_MATRIX.md` / `M10_9_4_1_H25_VALIDATION_CHECKLIST.md` — validated H.25 targeted protection/transient matrix contract.
- `M10_9_4_1_H25_STATIC_REVIEW.md` — package-time H.25 isolation/cost-policy review.
- `M10_9_4_1_H26_INTEGRATED_ROLLBACK_FAIL_CLOSED_STRESS.md` / `M10_9_4_1_H26_VALIDATION_CHECKLIST.md` — validated H.26 integrated rollback/fail-closed stress contract.
- `M10_9_4_1_H26_STATIC_REVIEW.md` — H.26 isolation/test-seam review, later confirmed by executable validation.
- `M10_9_4_1_H27_OFF_DESIGN_ROBUSTNESS_QUALIFICATION_ENVELOPE.md` / `M10_9_4_1_H27_VALIDATION_CHECKLIST.md` — validated H.27 staged off-design qualification-envelope contract.
- `M10_9_4_1_H27_STATIC_REVIEW.md` — H.27 package-time isolation/runtime-cost review.
- `M10_9_4_1_H28_1A_PERFORMANCE_ATTRIBUTION.md` / `M10_9_4_1_H28_1A_VALIDATION_CHECKLIST.md` — validated H.28.1-A attribution contract after the failed H.28 performance gate.
- `M10_9_4_1_H28_1A_STATIC_REVIEW.md` — H.28.1-A isolation, weak-registry determinism and baseline-discipline review.
- `M10_9_4_1_H28_1C_H9_JACOBIAN_PROBE_HOT_PATH_OPTIMIZATION.md` / `M10_9_4_1_H28_1C_VALIDATION_CHECKLIST.md` — current conservative H.9 Jacobian/probe allocation and hot-path optimization contract.
- `adr/0155-remove-h9-probe-object-graph-churn-before-changing-newton-mathematics.md` — implementation-optimization boundary preserving finite-difference Newton mathematics and thermodynamic equations/search order.

## Decision records

`adr/` contains Architecture Decision Records. Later work must preserve accepted decisions unless an explicit superseding ADR is created. The newest control-room/runtime/scenario/fault/replay/fidelity/operator-automation/HMI/hardening and approved future-product decisions are ADR 0046–0155.

## Milestone records

`milestones/` records delivered scope and validation state per milestone. A milestone file describing delivered code does **not** by itself mean the milestone is validated; validation requires explicit local build/test confirmation recorded in the handoff/status/roadmap.

## Domain documents

The remaining top-level Markdown files document subsystem contracts and their ownership boundaries: reactor physics, primary circuit, turbine island, electrical system, control/protection, instrumentation, alarms, control-room presentation, the M7 operating/training framework, M8.1 deterministic fault injection, M8.2 hydraulic component faults, M8.3 instrumentation/control faults, M8.4 secondary-system transients, M8.5 educational leak/LOCA-class scenarios, M8.6 electrical-loss/station-blackout-class scenarios, M8.7 safety-response evaluation/debrief composition, M9.1 recorder/checkpoint/full-replay reconstruction, M9.2 post-incident analysis, validated M9.3 advanced xenon/low-power integration, validated M9.4 spatial/quasi-spatial refinement, validated M9.5 historical-inspired scenario framework, validated M9.6 calibration/reference-validation + GUI hardening, validated M9.7 advanced-fidelity integration gate, validated M10.1–M10.9.2 Hotfix 2 operator-computer/supervisory/session/integrated-UI/HMI/gauge capabilities, validated M10.9.3 interactive full-plant mimic baseline, validated M10.9.4, active M10.9.4.1 operational-envelope/numerical hardening through the validated H.26 Hotfix 1 integrated rollback/fail-closed stress / current H.27 off-design qualification-envelope candidate, completed Phase G, and the approved M10.9.1–M10.9.8 operator-experience architecture.

When modifying a subsystem, update its domain document together with the milestone/ADR/handoff documents rather than leaving architecture knowledge only in source comments or chat history.

## Current restart checkpoint

- M7, M8 and M9 gates are complete / validated.
- M10.1–M10.9.4 and M10.9.4.1-D.4 are validated.
- D.4 validation evidence is 944 ordinary tests plus all 17 unique explicit tests with zero failures.
- M10.9.4.1-D.4.1 remains validated: STOP-owned optional travel rate, differential travel regression, deterministic valve replay/in-flight checkpoint restoration and post-trip reset travel resumption.
- M10.9.4.1-E.3.2 Hotfix 3 remains the validated electrical-protection checkpoint: current-v2 10 MWe signed coupling plus reviewed breaker-supervised delayed electrical protection.
- M10.9.4.1-F.1, F.2 and F.3 Hotfix 1 are validated: compressible capacity law, atmospheric header relief and conservative header-to-condenser bypass.
- The authoritative numerical continuation is M10.9.4.1-H.27 Hotfix 1 VALIDATED and Phase G is complete. Standard production remains explicit at 10 ms. H.28 failed only its performance qualification; H.28.1-A Hotfix 2 is validated diagnostic evidence that localizes cost to H.9 Jacobian/probes. H.28.1-C Hotfix 2 is validated allocation/hot-path optimization (~97.6% Jacobian/H.9 allocation reduction with unchanged fingerprint). H.28.1-B is validated historical-explicit predictor reuse and H.28.1-D is the current hydraulic-probe CPU hot-path candidate. H.24 is not chained into each optimization iteration, but one post-optimization H.24 rerun is required before H.29 because committed-runtime implementation code changes.
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
- `M10_9_4_1_D4_1_VALIDATION_CHECKLIST.md` — validated STOP travel ownership, replay/checkpoint and trip-reset resumption gate.
- `adr/0101-governor-effective-setpoint-and-actuator-tracking-are-audited-before-new-anti-windup.md` — evidence boundary before any tracking anti-windup law.
- `adr/0102-current-v2-breaker-open-rotor-has-passive-mechanical-losses.md` — current-v2 passive rotor-loss and explicit recovery decision.

- `M10_9_4_1_H17_LONG_HORIZON_CROSS_PROFILE_BRANCH_CONTINUITY_QUALIFICATION.md` — validated H.17 long-horizon/cross-profile diagnostic that exposed the turbine-inlet split later resolved by H.18/H.19.
- `M10_9_4_1_H17_VALIDATION_CHECKLIST.md` — H.17 local validation contract.
- `M10_9_4_1_H18_TURBINE_INLET_CONTINUITY_RESIDUAL_FLOOR_SPLIT_DIAGNOSIS.md` — validated H.18 split-diagnosis design and result.
- `M10_9_4_1_H18_VALIDATION_CHECKLIST.md` — validated H.18 local validation contract.
- `M10_9_4_1_H19_FOUR_NODE_LONG_HORIZON_CROSS_PROFILE_QUALIFICATION.md` — validated H.19 four-node long-horizon qualification evidence.
- `M10_9_4_1_H19_VALIDATION_CHECKLIST.md` — completed H.19 local validation contract.
- `M10_9_4_1_H20_FOUR_NODE_ACTIVATION_ROLLBACK_SHADOW_TELEMETRY_CONTRACT.md` — validated H.20 fail-closed authority/rollback/telemetry contract.
- `M10_9_4_1_H20_VALIDATION_CHECKLIST.md` — validated H.20 local gate.
- `M10_9_4_1_H21_FOUR_NODE_ORCHESTRATOR_SHADOW_WIRING_TELEMETRY_INTEGRATION.md` — validated H.21 orchestrator sidecar-wiring design/result.
- `M10_9_4_1_H21_VALIDATION_CHECKLIST.md` — completed H.21 local validation contract.
- `M10_9_4_1_H21_HOTFIX1_FOCUSED_AUDIT_COMPILE_FIX.md` — audit-only CS0136 compile failure, cause, minimal fix and unchanged H.21 contract.
- `M10_9_4_1_H21_DOCUMENTATION_STATIC_AUDIT.md` — documentation/static consistency review plus the subsequent Hotfix 1 compiler-finding addendum; the static review did not itself promote H.21, while the later Hotfix 1 executable gate did.
- `M10_9_4_1_H22_FOUR_NODE_CORRECTED_CANDIDATE_COMMIT_SEAM.md` — validated H.22 opt-in corrected-state ownership design/result.
- `M10_9_4_1_H22_VALIDATION_CHECKLIST.md` — completed H.22 ordinary/cumulative focused validation contract.
- `M10_9_4_1_H22_STATIC_REVIEW.md` — package-time H.22 non-runtime source/evidence/document consistency audit.
- `M10_9_4_1_H23_DETERMINISTIC_REPLAY_CHECKPOINT_PROTECTION_QUALIFICATION.md` — active H.23 replay/checkpoint/protection qualification design.
- `M10_9_4_1_H23_VALIDATION_CHECKLIST.md` — H.23 local promotion contract.
- `M10_9_4_1_H23_STATIC_REVIEW.md` — package-time H.23 static/runtime-isolation review.
- `M10_9_4_1_H23_HOTFIX1_COMPILE_FIX.md` — first-build CS0246 diagnosis and single-import Hotfix 1 contract.
- `adr/0148-introduce-opt-in-corrected-commit-seam-behind-h20-authority.md` — H.22 two-stage authority and explicit-first fallback decision.
