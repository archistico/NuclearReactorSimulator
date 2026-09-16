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
- **`PRE_M11_IMPLEMENTATION_DECISIONS.md`** — consolidated decisions from the nuclear-code V&V and Digital-I&C/Human-System reviews: what M11 implements as assurance, what M13 implements as product behavior and what remains explicitly non-scope.
- **`M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md`** — active P0–P6 replacement-long closure route: D1–D6 evidence freeze, asymptotic qualification, branch decision, short 5→10→5 qualification, second-long freeze/execution and M10 closure.
- **`M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P2_DECISION.md`** — P2 Decision Gate 1: returned P1 `INCONCLUSIVE`, mandatory planning stop, no P3 authorization, and Plan Amendment 1 defining bounded P1A before P2R.
- **`M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P2R_DECISION_PLAN_AMENDMENT2.md`** — P2R1 planning stop after returned P1A `INCONCLUSIVE`; Plan Amendment 2 freezes bounded P1B slow-state/owner observation scope and requires Todreas/Kazimi Deep Review Pass 2 before implementation.
- **`M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P2R2_DECISION_REENTRY2.md`** — returned P1B PASS review; selects P3-R owner localization only, keeps P3-W/production repair/second-long unauthorized, and defines the bounded P3-R1 owner-localization question.
- **`M10_FINAL_NEXT_STEPS_DETAILED_EXECUTION_PLAN.md`** — detailed P2R2→VR0–VR5→P3-R1/P3-R2→P4→P5→P6 execution route with branch/stop rules.
- **`M10_FINAL_PHYSICAL_REFERENCE_MODEL_ASSESSMENT_PLAN1.md`** — independent point-kinetics, IAPWS-IF97, iodine/xenon and ANS-5.1 model-assessment program.
- **`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1.md`** — validated VR2 repair-planning authority, immutable exact-v9/new-closure-version boundary and RP1A/RP1B decision chain.
- **`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_PERFORMANCE_REPLANNING1.md`** — validated interpretation of Refinement 3/4 timing evidence and the frozen cross-process reproducibility rule.
- **`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT5_C3_CROSS_PROCESS_TAIL.md`** — frozen Refinement 5 cross-process diagnostic contract and returned outcome.
- **`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT5_RETURNED_EVIDENCE_ADJUDICATION.md`** — authoritative returned-evidence engineering review; records the 5/5 same-boundary result, GC/allocation attribution and the C4-planning-only next authority.
- **`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_C4_RETURNED_EVIDENCE_ADJUDICATION.md`** — authoritative C4 qualification review; closes semantic/allocation/tail evidence and authorizes RP1C planning only.
- **`M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_PLAN_AMENDMENT3_EXTERNAL_MODEL_ASSESSMENT.md`** — formal Plan Amendment 3 holding P3-R1 until VR5.
- **`M10_FINAL_MODEL_ASSESSMENT_CLAIM_POLICY.md`** — TEST PASS / VERIFIED / MODEL-ASSESSED / QUALIFIED / PHYSICALLY VALIDATED terminology policy.
- **`M10_FINAL_P3R1_TO_P6_DETAILED_GATE_MATRIX.md`** — compact gate-by-gate authority, outputs and successor matrix.
- **`POST_M10_REPOSITORY_RELEASE_PLAYABLE_SLICE_PLAN.md`** — post-M10 Git/version/script/ADR cleanup and playable control-room vertical-slice priorities.
- **`M10_FINAL_CLOSURE_AND_M11_BOOTSTRAP_PLAN.md`** — final green/red handoff from Replacement-Long Execution 2 through explicit M10 closure and M11.1 bootstrap.
- **`POST_M10_TO_M15_EXECUTION_MASTER_PLAN.md`** — detailed branch-aware execution sequence from the final M10 long gate through M11 release hardening and the M12–M15 epics.
- **`CHANGE_IMPACT_REVALIDATION_POLICY.md`** — minimum rerun ladder for documentation, harness, HMI/Application, persistence, control/protection, physics/numerical and release/package changes.
- **`M11_RELEASE_EVIDENCE_MATRIX_PLAN.md`** — planned 30-row release-readiness matrix complementing the 27-row M10 phenomenon V&V provenance.
- **`M11_DIGITAL_IC_RELEASE_ASSURANCE_PLAN.md`** — M11.1–M11.6 Digital-I&C release-assurance deliverables and gates.
- **`M13_DIGITAL_IC_DEGRADATION_AUTOMATION_TRANSPARENCY_PLAN.md`** — planned M13.9 deterministic information-degradation and automation-transparency implementation.

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
- [`M10_9_8_5_MANUAL_INTEGRATED_HMI_ACCEPTANCE_CHECKLIST.md`](M10_9_8_5_MANUAL_INTEGRATED_HMI_ACCEPTANCE_CHECKLIST.md)
- [`M10_9_8_CLOSURE.md`](M10_9_8_CLOSURE.md)
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

- [`M10_FINAL_VV_MATRIX.md`](M10_FINAL_VV_MATRIX.md) — frozen 27-row pre-M11 phenomenon/evidence matrix and cumulative-gate status.
- [`PRE_M11_NUCLEAR_CODE_VV_REVIEW.md`](PRE_M11_NUCLEAR_CODE_VV_REVIEW.md) — selective nuclear-code V&V review used to design the final M10 gates.
- [`M10_FINAL_LONG_VALIDATION_METRICS_SPEC.md`](M10_FINAL_LONG_VALIDATION_METRICS_SPEC.md) — pre-freeze workload/metrics specification for the separate long gate.
- [`M10_FINAL_REPLACEMENT_LONG_BASELINE_FREEZE.md`](M10_FINAL_REPLACEMENT_LONG_BASELINE_FREEZE.md) — current exact-v9 replacement-long manifests, information-dense 1,920 s workload and 35–45 / 60 minute workstation budget.
- [`M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC1.md`](M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC1.md) — returned exact-v9 5→10 MWe protection-owner diagnostic; generator loss-of-synchronism owns the shared RL-M1/RL-R1 trip.
- [`M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC2.md`](M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC2.md) — returned authority/rod-coordination discrimination; Assisted rod motion does not improve the step-636 loss-of-synchronism path.
- [`M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC3.md`](M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC3.md) — returned breaker-closed governor, valve-preload and exact-v4/exact-v9 discrimination; no supported first repair.
- [`M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC4.md`](M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC4.md) — returned fixed-time ramp/energy-support discrimination; slower schedules delay but do not establish 10 MWe.
- [`M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC5.md`](M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC5.md) — returned measured-readiness staging; no trip, but 20 s does not reach a qualified 6 MWe stage.
- [`M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC6.md`](M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC6.md) — returned 180 s first-stage settling evidence; frequency recovers near 50 Hz without strict requested-load convergence.
- [`M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md`](M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md) — authoritative active P0–P6 closure route; no ad hoc diagnostic continuation.

## Pre-M11 engineering review consolidation

- [`PRE_M11_ENGINEERING_REVIEW_CONSOLIDATION.md`](PRE_M11_ENGINEERING_REVIEW_CONSOLIDATION.md) — single index of the source-driven review streams and their roadmap consequences.
- [`PRE_M11_NUCLEAR_CODE_VV_REVIEW.md`](PRE_M11_NUCLEAR_CODE_VV_REVIEW.md) — nuclear-code development/V&V review and final-M10 qualification method.
- [`PRE_M11_DIGITAL_IC_HUMAN_SYSTEM_SAFETY_REVIEW.md`](PRE_M11_DIGITAL_IC_HUMAN_SYSTEM_SAFETY_REVIEW.md) — Digital I&C, software-safety and human-system review.
- [`PRE_M11_PLANT_DYNAMICS_THERMAL_HYDRAULICS_REACTOR_PHYSICS_REVIEW.md`](PRE_M11_PLANT_DYNAMICS_THERMAL_HYDRAULICS_REACTOR_PHYSICS_REVIEW.md) — five-source Reviews 1–2 covering coordinated load/energy balance, flexibility, nuclear load following, steam/water circulation/separation and reactor-physics/spatial-fidelity limits; interpretation-only for P1A/P2R.
- [`research/PRE_M11_DEEP_ENGINEERING_SECTION_REVIEW_2.md`](research/PRE_M11_DEEP_ENGINEERING_SECTION_REVIEW_2.md) — source-by-source detailed re-read of the previously selected “study deeply” sections, with corrections, transferability boundaries and roadmap consequences.
- [`research/PRE_M11_DEEP_REVIEW_TRACEABILITY_2.md`](research/PRE_M11_DEEP_REVIEW_TRACEABILITY_2.md) — compact section → finding → transferability → project-owner traceability matrix.
- [`DIGITAL_IC_ARCHITECTURE_INVARIANTS.md`](DIGITAL_IC_ARCHITECTURE_INVARIANTS.md) and [`HUMAN_AUTOMATION_FUNCTION_ALLOCATION.md`](HUMAN_AUTOMATION_FUNCTION_ALLOCATION.md) — review-derived architecture/function-allocation planning contracts.
- [`DIGITAL_IC_HAZARD_CATALOG.md`](DIGITAL_IC_HAZARD_CATALOG.md) and [`HMI_CLASSIC_FAILURE_MODES_CHECKLIST.md`](HMI_CLASSIC_FAILURE_MODES_CHECKLIST.md) — deterministic software/I&C hazard and operator-interface review inputs.
- [`REFERENCE_OPERATING_POINT_EQUILIBRIUM_AND_STABILITY_PLAN.md`](REFERENCE_OPERATING_POINT_EQUILIBRIUM_AND_STABILITY_PLAN.md) — Lamarsh-derived self-consistency/equilibrium planning, with M12.0 as the formal implementation home.
- [`research/PRE_M11_ENGINEERING_REVIEW_SOURCES.md`](research/PRE_M11_ENGINEERING_REVIEW_SOURCES.md) — bibliographic provenance, retained principles and explicit non-imports.

## Operating-point equilibrium planning

- [`REFERENCE_OPERATING_POINT_EQUILIBRIUM_AND_STABILITY_PLAN.md`](REFERENCE_OPERATING_POINT_EQUILIBRIUM_AND_STABILITY_PLAN.md) — residual taxonomy, closed-loop/fixed-input qualification, domain-headroom diagnostics, bounded trimmer and M12.0 roadmap.
- [`M10_LR_H1_EQUILIBRIUM_DIAGNOSTIC_PLAN.md`](M10_LR_H1_EQUILIBRIUM_DIAGNOSTIC_PLAN.md) — evidence-first diagnostic route for the current/future long healthy-reference drift or envelope blocker before any production fix.

- [`research/PRE_M11_TODREAS_KAZIMI_THERMAL_HYDRAULIC_DEEP_REVIEW_PASS1.md`](research/PRE_M11_TODREAS_KAZIMI_THERMAL_HYDRAULIC_DEEP_REVIEW_PASS1.md) — pre-Plan-Amendment-2 deep review of Todreas/Kazimi Volumes I–II.
- [`research/PRE_M11_TODREAS_KAZIMI_DEEP_REVIEW_TRACEABILITY_PASS1.md`](research/PRE_M11_TODREAS_KAZIMI_DEEP_REVIEW_TRACEABILITY_PASS1.md) — source-section to project-consequence traceability for that pass.
- [`research/PRE_M11_TODREAS_KAZIMI_THERMAL_HYDRAULIC_DEEP_REVIEW_PASS2.md`](research/PRE_M11_TODREAS_KAZIMI_THERMAL_HYDRAULIC_DEEP_REVIEW_PASS2.md) — post-Plan-Amendment-2 audit; candidate disposition `PASS-AS-AUTHORED`.

- [`M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P1B_SLOW_STATE_OWNER_QUALIFICATION.md`](M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P1B_SLOW_STATE_OWNER_QUALIFICATION.md) — returned P1B slow-state/owner evidence gate; execution PASS, `COUPLED-MULTI-DOMAIN`, now frozen for P2R2.


## M10 Final external physical-reference assessment

- [`M10_FINAL_VR0_REFERENCE_PROVENANCE_CONTRACT.md`](M10_FINAL_VR0_REFERENCE_PROVENANCE_CONTRACT.md) — frozen VR0 non-circular reference/tolerance/applicability contract.
- [`M10_FINAL_VR1_POINT_KINETICS_INDEPENDENT_BENCHMARK.md`](M10_FINAL_VR1_POINT_KINETICS_INDEPENDENT_BENCHMARK.md) — validated VR1 independent Hébert point-kinetics benchmark and error/refinement contract.
- [`M10_FINAL_VR2_IAPWS_IF97_WATER_STEAM_ERROR_MAP.md`](M10_FINAL_VR2_IAPWS_IF97_WATER_STEAM_ERROR_MAP.md) — returned RED VR2 official IAPWS-IF97 Regions 1/2/4 water/steam error-map and inverse-closure assessment provenance.
- [`M10_FINAL_VR2_MATERIALITY_DIAGNOSTIC1_RETURNED_EVIDENCE_ADJUDICATION.md`](M10_FINAL_VR2_MATERIALITY_DIAGNOSTIC1_RETURNED_EVIDENCE_ADJUDICATION.md) — returned PASS adjudication that closes Materiality Diagnostic 1 as `HYDRAULIC-MATERIALITY-CONFIRMED` without authorizing repair.
- [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1.md) — validated repair-domain and staged selection contract.
- [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1A_REFERENCE_DOMAIN_CORPUS_SEAM_MAP_FREEZE.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1A_REFERENCE_DOMAIN_CORPUS_SEAM_MAP_FREEZE.md) — validated RP1A REV1 corpus, seam-map and pre-candidate performance freeze.
- [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_TEST_ONLY_SHADOW_CANDIDATE_MATRIX.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_TEST_ONLY_SHADOW_CANDIDATE_MATRIX.md) — validated first-generation B1/C1/D1 evidence matrix against the immutable RP1A corpus.
- [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT1_C2_D2.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT1_C2_D2.md) — validated returned C2/D2 evidence; non-selecting.
- [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT2_C3_D3.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT2_C3_D3.md) — validated C3/D3 vapor-side seam-completion evidence.
- [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT3_C3_PERFORMANCE_TAIL.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT3_C3_PERFORMANCE_TAIL.md) — validated immutable-C3 performance-tail attribution evidence.
- [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT4_C3_R1_SEAM_LOCALIZATION.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT4_C3_R1_SEAM_LOCALIZATION.md) — validated R1-side worst-case localization/reproducibility evidence.
- [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_PERFORMANCE_REPLANNING1.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_PERFORMANCE_REPLANNING1.md) — validated cross-process measurement replanning; C3 and strict max remain immutable.
- [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT5_C3_CROSS_PROCESS_TAIL.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT5_C3_CROSS_PROCESS_TAIL.md) — validated five-process immutable-C3 cross-process tail reproducibility evidence.
- [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT5_RETURNED_EVIDENCE_ADJUDICATION.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT5_RETURNED_EVIDENCE_ADJUDICATION.md) — returned review that preserves the machine classification while attributing the repeated owner to allocation/Gen0 cadence.
- [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_C4_PLANNING1.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_C4_PLANNING1.md) — returned/adjudicated REV2 planning contract for the separately versioned allocation-neutral, C3-bit-equivalent test-only C4 candidate; corrects the R1 prefix topology and freezes the allocation-neutral timing harness, source pins, one-semantic-plus-ten-timing-process protocol and separate state/hydraulic semantic schemas. The active successor is C4 Test-Only Implementation 1.
- [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_C4_PLANNING1_REV2_PREEXECUTION_REVIEW.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_C4_PLANNING1_REV2_PREEXECUTION_REVIEW.md) — second pre-execution review documenting the structurally insufficient mixture-only topology, the historical R5 recorder-allocation confounder and the REV2 corrections.
- [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_C4_TEST_ONLY_IMPLEMENTATION1.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_C4_TEST_ONLY_IMPLEMENTATION1.md) — closed test-only C4 implementation/evidence gate; returned evidence is qualified as `C4-QUALIFIED-ALLOCATION-TAIL-CLOSED` with unchanged RP1C-selection/production blocks.
- [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_C4_RETURNED_EVIDENCE_ADJUDICATION.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_C4_RETURNED_EVIDENCE_ADJUDICATION.md) — authoritative returned-C4 engineering adjudication; independently reconciles all 59 artifacts, semantic bit-equivalence, zero-allocation/fallback closure and two-lane wall-clock evidence, and advances authority only to RP1C planning.
- [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_PLANNING1.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_PLANNING1.md) — RP1C planning-only selection policy; freezes the corrected full performance predicate, C4/D3 readiness matrix, mandatory `NO-SELECTION` option and the required immutable-C4 full-domain timing confirmation before selection.
- [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_PLANNING1_PREEXECUTION_REVIEW.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_PLANNING1_PREEXECUTION_REVIEW.md) — pre-execution review that closes D3 eligibility interpretation, the remaining C4 full-domain timing gap, invariant-culture parsing and planning-only authority.
- [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT5_PREEXECUTION_REVIEW.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT5_PREEXECUTION_REVIEW.md) — static pre-execution review of process isolation, xUnit/analyzer, PowerShell 5.1 and byte-identity constraints.
- [`research/M10_FINAL_VR0_EXTERNAL_REFERENCE_REGISTER.md`](research/M10_FINAL_VR0_EXTERNAL_REFERENCE_REGISTER.md) — authoritative bibliographic/source provenance for VR1–VR4.
- [`research/M10_FINAL_VR0_RUNTIME_APPLICABILITY_AUDIT.md`](research/M10_FINAL_VR0_RUNTIME_APPLICABILITY_AUDIT.md) — static audit of whether each assessed owner is active in exact-v9/P1B.
- [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT2_PREEXECUTION_REVIEW.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT2_PREEXECUTION_REVIEW.md) — static pre-execution review of C3/D3 contract, seam-specific algorithms and prior failure classes.
- [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_PLANNING1_RETURNED_EVIDENCE_ADJUDICATION.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_PLANNING1_RETURNED_EVIDENCE_ADJUDICATION.md) — returned RP1C Planning 1 adjudication; authorizes only the immutable-C4 full-domain confirmation implementation.
- [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION1.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION1.md) — five-process exact-v9 + seam performance confirmation contract required before any RP1C selection gate.
- [`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION1_PREEXECUTION_REVIEW.md`](M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION1_PREEXECUTION_REVIEW.md) — anti-RED review of process isolation, frozen pins, allocation-neutral harness, predicate scope and evidence ownership.
