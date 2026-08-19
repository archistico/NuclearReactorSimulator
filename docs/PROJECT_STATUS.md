> **Current authoritative continuation:** M10.9.4.1-I.1 Hotfix 1 Profile Compatibility & Legacy Retirement Inventory VALIDATED. Phase H remains closed as `OPT-IN ONLY`; exact v2 explicit remains authoritative default/rollback/reference and exact v3 corrected remains qualified opt-in. **Current candidate:** M10.9.4.1-I.2 — Audit Consolidation & CI Baseline Hardening.

# Project Status

G.3 is user-validated. Its supplied audit confirms 12 remaining non-turbine current-v2 samples in enthalpy mode, including 3 pump paths, 2 drum paths, 2 condenser paths and 2 external-boundary owners; maximum flow-work rate is 2.817289248 MW, total absolute flow work is 8.972520763 MW and maximum ownership residual is 0 W. Pump hydraulic work, condenser heat rejection, relief external exchange and bypass internal transfer remain single-owned.

H.1 is user-validated. Its supplied fixed-step audit reports 10/5/2.5 ms cost of 0.824089/1.592215/3.188456 wall seconds per simulated second, maximum pump/channel/return one-step changes of 23.483512764/114.314039863/86.409873575 kg/s and non-improving maximum final-state relative difference: 0.005401937 coarse/medium versus 0.006028534 medium/fine. Conservation remains green.

H.4 is user-validated. Its bounded hybrid sweep selected `P060-F040-R015`: 2/50 corrections, 2/2 converged, deterministic work ratio 2.140000, observational wall-cost ratio 1.662880, chatter ratios pump/channel/return/pressure 0.432616/0.135885/0.086691/0.921832, tiny final-state gaps, zero conservation/ownership residuals and exact deterministic repeat. H.5 Hotfix 1 free-running activation failed ordinary validation. H.5 Hotfix 2 is user-validated: ordinary build/tests passed after production rollback; over 500 committed shadow intervals P060-F040-R015 triggered 7 corrections, converged 5/7, used deterministic work ratio 1.492000 and observational shadow cost ratio 1.480162, preserved exact determinism/conservation, and correctly returned `extended-shadow-qualification-passes=False`.

M0 through M9 are validated, with M7, M8 and M9 gates complete. **M10.1–M10.9.4 and M10.9.4.1 through H.30 are VALIDATED. Phase H is CLOSED as `OPT-IN ONLY`.** H.28 remains `bounded-but-costly`; the post-H.28 H.24 long-horizon requalification, H.29 activation candidate and H.30 cumulative closure are all green. Exact v2 `ExplicitCommittedState` remains authoritative default/rollback/reference and exact v3 corrected ownership remains qualified opt-in. Phase I is active. I.1 Hotfix 1 is validated and established the 12-version/9-ID compatibility baseline with zero delete-now profiles. I.2 is the current candidate and separates ordinary/current-evidence/scheduled-long/historical-frozen validation while adding CI entry points; it still authorizes no legacy-mode deletion.

Approved future product direction remains frozen in `FUTURE_GAMEPLAY_CONTROL_ROOM_AND_ACCIDENT_DIRECTION.md` plus ADR 0121–0123. These decisions do **not** change the immediate numerical-hardening sequence. H.10–H.12 localized the original failures to inverse thermodynamic branch selection; H.13 Hotfix 2 validated bounded 2%/5 K continuity at `steam|stop-out`; H.15 localized interval 723 to the same mechanism at `header`; H.16 validated `steam|stop-out|header` at 15/15. H.17 Hotfix 6 validated the 30,000-interval/four-profile diagnostic contract: 3,046 P060/F040 trigger intervals, 92 episodes, 473 representatives, 228/473 three-node convergence and 245 failures, including a new `turbine-inlet` disagreement class. H.18 Hotfix 1 is validated: unchanged H.9 plus unchanged 2%/5 K hysteresis at `steam|stop-out|header|turbine-inlet` converged 261/261 over all 245 H.17 failures plus 16 controls. H.19 is now validated: the exact 30,000-interval/four-profile census reproduced 3,046 triggers, 92 episodes and 473 frozen representative keys; all 473/473 converged, all 245 H.17 failures recovered, all 228 H.17 successes were preserved, committed selection stayed transparent and no new untargeted branch disagreement was found. H.20 is now validated: its default-disabled fail-closed contract produced 473/473 explicit decisions, 473/473 armed shadow eligibility, zero commits, 8/8 rollback challenges and exact repeat. H.21 Hotfix 1 is now validated after the audit-only CS0136 repair: 2,000/2,000 presentation equivalence, 15/15 H.20 eligibility, zero rollback/commit/untargeted disagreement and deterministic telemetry. H.22 is now validated: its separately opt-in committed path produced 443 actual corrected commits over 2,000 intervals with zero rollback/fallback-commit/unsafe-commit/untargeted-disagreement violations and exact repeat. H.23 Hotfix 2 is validated: 701 recorded steps, 242 corrected commits, exact full replay and checkpoint continuation, reverse-power latch/final generator trip, zero fallback/unsafe commits and deterministic trace fingerprint `7C8FBA8ECB197F65AB263A79268653E3C2988F700A5A863BB0304D377C82FB54`. H.24 Hotfix 1 qualified the unchanged committed path over the full H.19 30,000-interval/four-profile nominal domain. H.25 validated representative protection/action/supervision cases in 837 runtime steps; H.26 Hotfix 1 validated integrated typed rollback/fail-closed fallback; H.27 Hotfix 1 validated the off-design envelope; H.28 validated bounded performance/soak; and H.24 Requalification 1 validated the optimized committed runtime over the long-horizon domain. H.29 is the validated production activation candidate. H.30 is validated and closes Phase H as `OPT-IN ONLY`; I.1 Hotfix 1 is validated and Phase I now advances to I.2 audit consolidation/CI baseline hardening.


| Phase | Status | Validated capability |
|---|---|---|
| M0 | VALIDATED | deterministic runtime, transactional steps, invariants, replay primitives and test harness |
| M1 | VALIDATED | strongly typed physical quantities, fluid/thermal primitives, pipes, valves, pumps, heat transfer and simplified water/steam closure |
| M2 | VALIDATED | reactivity composition, rods, point kinetics, fission power, decay heat, temperature/void feedback and I-135/Xe-135 dynamics |
| M3 | VALIDATED | canonical integrated primary circuit through core zones, channels, circulation, steam drums, boundaries, plant snapshot and long-run conservation verification |
| M4 | VALIDATED | M4.1–M4.7 validated; manually commanded reactor-to-grid gate complete |
| M5 | VALIDATED | M5.1–M5.7 validated; integrated automatic-operation gate complete |
| M6 | VALIDATED | M6.1–M6.7 validated; complete control-room/runtime-integration gate |
| M7 | VALIDATED | M7.1–M7.7 validated; versioned sessions, normal operating path and deterministic training/evaluation gate complete |
| M8 | VALIDATED | M8.1–M8.7 validated; deterministic fault/scenario/safety-response gate complete |
| M9 | COMPLETE / VALIDATED | M9.1–M9.7 validated; 760/760 tests passed and final GUI layout integrated |
| M10 | IN PROGRESS | M10.1–M10.9.4, M10.9.4.1 through H.30 and I.1 Hotfix 1 are validated; Phase H is closed `OPT-IN ONLY`; current candidate is I.2 audit consolidation/CI baseline hardening; M10 closes at M10.9.8 |

## Validated M8 fault/scenario gate

M8.1 owns deterministic fault declaration/scheduling/lifecycle orchestration. M8.2 hotfix 2 validates hydraulic component constraints and conservative leaks. M8.3 validates instrumentation/control fault overlays. M8.4 hotfix 2 validates turbine/generator/feedwater/condenser transients. M8.5 hotfix 2 validates bounded pressure-driven educational break scenarios with thermodynamic-admissibility guarding. M8.6 validates external-supply-loss and station-blackout-class composition. M8.7 hotfix 2 validates capstone safety-response acceptance/scoring and deterministic operator-action debrief timelines.

The complete M8 chain passed local clean restore/build and the complete automated suite. **M8 is COMPLETE / VALIDATED.**

## Validated M9.1–M9.5

M9.1 is validated and provides deterministic per-step recording, replay-backed checkpoints and fail-closed full replay/seek verification. M9.2 is validated and adds evidence-based post-incident windows, synchronized presentation trends, event timelines, logical-step response metrics and versioned debrief reports over those immutable artifacts. Analysis remains observational and cannot own or restore private physical state directly.

M9.3 is **validated**. It connects the already validated M2.8 iodine/xenon owner to the integrated M5 reactor/primary runtime through optional versioned configuration/state, composes committed xenon worth through the existing non-rod-reactivity seam, promotes the committed diagnostic through the presentation snapshot, and adds two versioned xenon/low-power scenario seeds. Existing M7 v1 initial conditions remain xenon-disabled so exact-version replay semantics are not silently changed. Local compilation and the complete automated suite passed after two test-only hotfixes that did not alter production physics or replay/versioning semantics.

### Validated M9 gate / M10.1–M10.9.4 validated / M10.9.4.1-A failure evidence

M9.4 is **validated**. It adds an opt-in quasi-spatial refinement over the M3.3 aggregated-core boundary, evaluates existing M2 fuel-temperature/coolant-temperature/void feedback equations on committed zone domains, reduces them to one current-power-share-weighted scalar for the existing global point-kinetics seam, and evolves only normalized `AggregatedCoreState` power shares for the next committed step. Explicit symmetric zone couplings smooth only the shape-driving signal; coordinates do not imply adjacency and no local neutron populations or conserved inventories are introduced. Local compilation and the complete automated suite passed after one test-compilation-only namespace hotfix.

M9.5 is **VALIDATED**: optional `HistoricalContext` provenance/fidelity metadata, schema-v3 persistence, explicit fact/approximation/assumption separation and fail-closed capability review passed local compilation and the complete automated suite. M9.6 also passed local compilation and the complete automated suite after one test-compilation-only hotfix; it adds explicit versioned reference cases/tolerance budgets/model-version tracking, sensitivity/regression reports and stronger App/UI automated regression coverage. Bundled reference cases remain explicitly internal validated regression baselines, not external historical measurements.

M9.7 is **VALIDATED** and the M9 gate is complete. The user confirmed local compilation and **760/760 automated tests passed** after hotfix 5, including 6,000-step / 60-second direct-session and desktop-pump endurance. The final manual center-workspace clipping/overlap issue was corrected in the user-supplied `MainWindow.axaml`, now integrated as the authoritative layout baseline.

M10.1 through M10.9.3 are **VALIDATED**. The structural stabilization sequence through Hotfix 22 is also user-confirmed green: condenser UA·ΔT feedback, generator/grid phase-frequency stiffness, secondary-pump discharge check valves, measured secondary protections, finite actuator travel and breaker-aware governor droop all pass compilation, the ordinary suite and both explicit 60-second journeys. Hotfix 23 advances only turbine thermodynamic-work fidelity.

M10.1 through M10.9.3 are **VALIDATED**. M10.2 provides GUIDANCE/INFO/DIAGNOSTICS, M10.3 ALARMS/LOG, M10.4 contextual COMMANDS, M10.5 the independent assistance/control-authority model, M10.6 deterministic M5-owned bounded supervisory operation, M10.7 replay-backed checkpoint/save/load/restore, M10.7.1 validated trip/reset/synchronization plus unified persistent/momentary control feedback, M10.8 the integrated keyboard-first operator computer, and M10.9.1 the validated five-region HMI information architecture plus range-semantics contracts. M10.9.2 Hotfix 2 is validated, adding advanced linear/circular instruments, canonical target/setpoint/protection visualization, provenance/quality/off-scale semantics and logical-step trends without new plant physics/control logic. M10.9.3 is validated, adding the interactive whole-plant mimic, explicit equipment inputs/outputs, directional medium-aware paths and subsystem drill-down. M10.9.4 Hotfix 23 is the latest **validated structural checkpoint**, adding detailed subsystem engineering schematics, generator power-path diagnostics and opt-in long-running gameplay/system acceptance tests. The first executable long-gameplay gate proved the historical v1 desktop/synchronization operating seeds were not sustained generation points; Hotfix 6 preserves v1 exact replay identity and adds versioned v2 generation-ready balance plus effective turbine-stage-flow presentation.


### Current M10.9.4.1 checkpoint

The original A audit failure is now resolved at its actual source. The current-v2 sustained-generation seed had an energy/hydraulic mismatch: 80% of fission heat was deposited in explicit fuel/structure inventories without a conservative return path to the coolant, while primary circulation was far below the steam-export demand. Current-v2 now enables conservative solid-to-coolant links, matched primary hydraulic resistance and corrected steam-line/control-valve initial conditions. Historical v1 seeds and protection thresholds remain unchanged.

The corrected-seed checkpoint and cumulative D.4 source passed the complete automated gate: 944 ordinary tests and all 17 unique explicit tests. D.4.1, E.2 Hotfix 1, E.3.1 Hotfix 1 and E.3.2 Hotfix 3 then passed all requested gates. F.1 subsequently passed compilation, the ordinary suite, focused tests and its explicit capacity audit. F.2 then passed compilation and all requested tests; its supplied audit confirmed monotonic 13.531762568 kg/s relief capacity at 6.80 MPa and conservative external exchange. F.3 Hotfix 1 is validated with exact header-to-condenser transfer and no external exchange. G.1 is validated. G.2 Hotfix 2 is validated with passive and pump-path enthalpy transport, exact single-count pump work, and the signed grid-exchange stability contract. G.3 is validated for the remaining non-turbine component groups. G.4 is validated with current-v2 turbine enthalpy transport, 5.457103652 MW maximum audited shaft power and zero ownership residual; Phase G is complete. H.1 validates 10/5/2.5 ms fixed-step sensitivity, H.2 validates deterministic semi-implicit pressure/flow coupling as the method direction, and H.3 now prototypes it in isolation without changing the production 10 ms runtime.

## What the validated engine can already do

The validated core can run headlessly and deterministically with:

- fixed-timestep execution independent from UI cadence;
- transactional state commits with fail-closed fault semantics;
- typed physical quantities and conservation-oriented state boundaries;
- fluid nodes with simplified water/steam phase closure;
- passive/active hydraulic primitives: pipes, valves and pumps;
- thermal bodies, heat transfer and explicit heat sources;
- compositional reactor reactivity, rods, point kinetics, fission/decay power and feedbacks;
- stateful iodine/xenon poisoning;
- canonical plant topology/state composition;
- aggregated core zones and equivalent channel groups;
- main circulation, steam drums, phase separation and recirculation;
- explicit feedwater/steam external boundaries with signed mass/energy accounting;
- one integrated primary-circuit committed-state step and plant-level snapshot;
- deterministic long-run drift and conservation verification;
- canonical main-steam lines/headers and stop/control/admission valve trains;
- conservative turbine inlet-to-exhaust expansion with explicit shaft-work extraction;
- separate rotor mechanical state with inertia, torque, speed, kinetic-energy audit, manual load and explicit trip/overspeed seams;
- conservative condenser exhaust-to-hotwell condensation with explicit heat rejection;
- condenser pressure/vacuum diagnostics derived from canonical conserved exhaust inventory;
- canonical hotwell condensate inventory;
- canonical condensate/feedwater return through existing pumps and a conserved feedwater inventory;
- explicit feedwater thermal-conditioning energy accounting with legacy M3 feedwater mass source disabled.
- deterministic synchronous-generator/grid phase and breaker state with manual synchronization windows;
- electromagnetic rotor loading with explicit shaft-to-electrical export and generator-loss audit;
- integrated M4.6 closed-loop mass and reactor-to-grid first-law audit across thermofluid, rotor and generator domains.
- validated M5.1 measured-signal separation with deterministic range/scaling, lag, validity/quality and explicit sensor-fault seams;
- validated M5.2 measured-signal-only P/PI/PID primitives, manual/automatic operation, anti-windup/bumpless transfer and typed valve/pump/rod command seams.
- validated M5.3 reactor-power control through canonical M2 rods/point kinetics/fission power plus measured main-circulation pump support through canonical `PumpState`;
- validated M5.4 turbine speed/load and steam-pressure admission control plus drum-level/feedwater and hotwell/condensate loops over canonical valves/pumps;
- validated M5.5 measured-signal protection with latched trips, reset permissives, interlocks and explicit SCRAM/turbine/generator arbitration;
- validated M5.6 deterministic alarms/annunciator memory with ACK/reset separation, first-out and logical event ordering;
- validated M5.7 committed-measurement automatic-operation composition and deterministic multi-step gate across control, protection and annunciator state;
- validated M6.1 snapshot-driven control-room shell with typed Application command dispatch and no direct App→Simulation dependency;
- validated M6.2 reusable control-room instruments/controls with semantic Normal/Warning/Trip/Unavailable states;
- validated M6.3 Reactor/Core, M6.4 Primary-Circuit and M6.5 Turbine/Secondary/Electrical operating workspaces with measured-versus-diagnostic separation;
- validated M6.6 bounded logical-step trends, M5.6 annunciator/first-out presentation and deterministic sequence-ordered event timeline;
- validated M6.7 live runtime coordination and M7.1 exact-version initial-condition/scenario/session/replay framework;
- validated M7.2–M7.7 normal operating/training progression through deterministic observational evaluation and a complete M7 gate;
- validated M8.1 scenario-fault schema v2, exact logical-step/committed-condition triggers, fail-closed applicator/evaluator binding and snapshot-visible deterministic lifecycle state;
- validated M8.2 hotfix 2 concrete pump trip/degradation, valve fail/stuck, valve-controlled path restriction/blockage and selected audited fluid-node leaks;
- validated M8.3 deterministic sensor bias/freeze/failure plus controller-output and actuator-command freeze/fail-low/fail-high through canonical M5 seams;
- validated M8.4 hotfix 2 turbine trip, generator trip/load rejection, feedwater degradation/loss and condenser cooling/vacuum degradation/loss through canonical M4/M5/M8 seams;
- validated M8.5 hotfix 2 bounded pressure-driven break/leak scenarios with conservative mass/energy loss, deterministic thermodynamic-admissibility capping and explicit non-licensing fidelity limits;
- validated M8.6 explicit external-grid connection loss and station-blackout-class composition through canonical M4.5/M8.2/M8.3/M8.4 seams, with no synthetic station electrical distribution model;
- validated M8.7 hotfix 2 capstone safety-response exercises with deterministic acceptance/scoring and logical operator-action timeline capture over existing fault/protection/control owners;

## Current continuation point

**M7.7 — Training Objectives, Procedure Guidance & Evaluation** is validated and closes the M7 gate over the exact-version scenario/session/replay boundary plus validated operating procedures and observational training evaluation.

**M8.1 — Deterministic Fault-Injection Framework** is validated.

**M8.3 — Instrumentation & Control Faults** is validated.

**M8.4 — Turbine / Generator / Feedwater / Condenser Transients hotfix 2** is validated. It composes canonical M5.5 turbine/generator trip paths, validated M8.2 feedwater-pump faults and a bounded M4.3 condenser cooling-capacity overlay without duplicate physical state.

**M8.5 — Educational Leak/LOCA-Class Scenarios hotfix 2** is validated.

**M8.6 — Electrical Loss & Station Blackout-Class Scenarios** is validated. It adds exact external-grid connection loss plus explicit pump/control/turbine/generator consequences through existing typed fault seams.

**M8.7 — Safety-Response Scenario Pack hotfix 2** is validated and closes the M8 gate. It adds no new fault physics; it composes deterministic acceptance/scoring, protection/control response checks and logical operator-action debrief timelines over existing scenario/runtime owners.

**M9.3 — Advanced Xenon & Low-Power Transients** is validated. It composes canonical M2.8 poison state through an explicit opt-in seam into the integrated reactor/primary runtime, preserves legacy exact-version M7 v1 semantics, promotes committed xenon diagnostics through the presentation boundary, and adds two versioned xenon/low-power scenario seeds. **M9.4–M9.7 are also validated and the M9 phase gate is complete**; M9.7 hotfix 5 passed 760/760 automated tests and the final user-corrected GUI layout is integrated.

**Continuation note:** M8.1–M8.7, M9.1–M9.7, M10.1–M10.9.4, M10.9.4.1 through H.30 and I.1 Hotfix 1 are validated. Phase H is closed as `OPT-IN ONLY`: v2 explicit remains authoritative default/rollback/reference and exact v3 corrected remains qualified opt-in. Phase I current candidate is I.2 Audit Consolidation & CI Baseline Hardening. See `PROJECT_HANDOFF.md`, `NEW_CHAT_START.md`, `M10_9_4_1_I2_AUDIT_CONSOLIDATION_CI_BASELINE.md` and `milestones/M10.9.4.1.md`.

M8.2 hotfix 2 also established the first dedicated headless `NuclearReactorSimulator.App.Tests` coverage for `MainWindowViewModel` and XAML command-state wiring; that presentation regression boundary remains validated and unchanged by M8.3–M8.7.

## What is intentionally not built yet

The following are planned architecture boundaries, not missing bugs:

- no detailed feedwater-heater cascade, extraction drains or deaerator chemistry yet;
- no detailed three-element drum-level control, feedwater-valve cascade or heater/deaerator control yet; the first canonical drum/hotwell pump loops are provided by the validated M5.4 baseline;
- no detailed condenser tube-bundle/circulating-water hydraulics, non-condensable gas or ejector model;
- no detailed HP/IP/LP turbine maps, moisture separation/reheat or wet-steam erosion model;
- no detailed synchronous-machine transient/reactance model, AVR/excitation dynamics or grid load-flow physics;
- no persistent disk historian or wall-clock event timestamps; M6.6 history is bounded presentation state indexed only by logical step/event sequence;
- M7 and M8 gates are complete; M8.1–M8.7 fault/scenario/safety-response capabilities are validated;
- no opaque arbitrary full-state dump/restore format; validated M9.1 provides versioned replay-backed checkpoints and deterministic seek/full replay instead;
- no full-scope or licensing-grade thermal hydraulics/neutronics;
- M8.5 pressure-driven breaks are bounded educational source-term models only: no critical/choked two-phase discharge, detailed rupture mechanics, containment, ECCS, fuel damage or severe-accident progression.
- M8.6 does not model station AC/DC buses, diesels, batteries, transfer logic, ECCS electrical trains or quantitative decay-heat coastdown; powered consequences are explicit scenario declarations over modeled seams.

## Approved future M10 architecture

M10 is **IN PROGRESS** as **Operator Computer, Supervisory Automation & Human-Machine Integration**. M10.1–M10.9.4 and M10.9.4.1 through H.8 are validated. M10.9.4.1 carries the accumulated A/B/C/D hardening, 10 MWe bidirectional migration, evidence-derived electrical protection, validated F.1–F.3 steam-capacity/topology work and completed G.1–G.4 open-control-volume energy migration. H.5 Hotfix 2 restored explicit production after rejected direct hybrid activation; H.6 proved the bounded fixed-relaxation Picard rescue envelope insufficient; H.7 proved that true-residual monotone backtracking still fails 2/7 difficult events; H.8 then showed safeguarded Anderson also remains 5/7 with the same two line-search exhaustions. H.9 is the current shadow-only Jacobian-informed algorithm revision, with no production activation before broader qualification. The operator computer remains an Application/App aggregation surface; real Manual / Assisted / Supervisory Automatic plant control remains canonical M5 ownership. Training assistance (`TrainingGuidanceMode`) remains a separate independent axis.

Approved constraints include fixed menu/pages with no free-form prompt, measured-signal-only supervisory consumers, protection priority, fail-closed degraded operation, deterministic bumpless manual takeover, separation of plant/training/session intents, replay-backed session persistence, distinct instrument/operating/target/protection range semantics, and logical-time challenge scoring. See `OPERATOR_EXPERIENCE_HMI_ARCHITECTURE.md`, `OPERATOR_COMPUTER_SUPERVISORY_AUTOMATION.md`, `milestones/M10.md` and ADR 0070.

## Architecture debt to avoid going forward

- do not create a second steam/condensate/feedwater hydraulic graph outside `PlantDefinition`;
- do not integrate steam, condensate, feedwater or hotwell inventories separately from `PlantNetworkOrchestrator`;
- do not independently solve M4.4 pump balances for state evolution after the orchestrator already owns those pumps;
- do not leave the M3 external feedwater source active while the M4.4 closed return path is active;
- do not represent condenser vacuum as an unrelated synthetic state variable;
- do not hide condenser heat rejection or feedwater heating outside signed external energy accounting;
- do not store rotor kinetic energy as fake thermal energy;
- do not hide thermofluid-to-mechanical energy exchange in diagnostics only;
- do not implement turbine overspeed trip logic outside the validated M5.5 protection owner;
- do not place valve/turbine/condenser/feedwater physics in Avalonia Views/ViewModels;
- do not bypass validated M5 controller/actuator seams with UI-side or scenario-side direct physical commands;
- do not derive electrical phase from wall-clock time;
- do not apply M4.5 electromagnetic loading on top of a non-zero legacy M4.2 manual external-load torque;
- do not auto-close the generator breaker outside explicit synchronization windows.
- do not create a separate M4.6 energy inventory or second integrator merely to compute the integrated heat balance;
- do not count turbine shaft work as both an external thermofluid loss and a second full-plant loss.
- do not turn `FullPlantState` into a fourth independent integrator or duplicate existing subsystem state.
- do not alter physical state to force M4.7 steady-state criteria to pass; criteria only evaluate measured drift.
- do not let M5 controllers or M6 views bypass instrumentation by reading `FullPlantSnapshot` true state directly.
- do not mix sensor/filter memory into `PlantState`, rotor state or electrical state.
- do not hide range violations by clamping without degraded signal quality.
- do not introduce random sensor faults or scenario scheduling inside the M5.1 solver.
- do not use wall clock, timers, random generators or UI publication cadence to activate/deactivate M8 faults; scheduling is logical-step/committed-condition only.
- do not implement concrete M8.2+ fault physics inside the generic M8.1 scheduler; typed applicators must reuse canonical subsystem seams.
- do not infer missing fault applicators/conditions or silently ignore unknown fault types; fault-enabled session loading fails closed.

- do not map reactor-power controller output directly to thermal MW; M5.3 must traverse rods → reactivity → point kinetics → fission-power calibration;
- do not use newly commanded rod positions as if they had already existed in the committed state; current-step kinetics uses committed rod reactivity;
- do not hide temperature/void/xenon/manual reactivity inside the M5.3 controller layer; inject non-rod reactivity explicitly;
- do not create a second main-circulation hydraulic solver for automatic control; pump commands modify only canonical pump operating state before the one plant-network step;
- do not use stop valves as normal turbine governor actuators; reserve them for M5.5 isolation/trip ownership;
- do not map turbine-controller output directly to shaft power or rotor speed; regulate canonical admission valves and reuse the M4.2 expansion/rotor physics;
- do not create a second feedwater/condensate pump or inventory integrator; M5.4 only replaces canonical pump operating commands;
- do not let M5.3 and M5.4 command the same physical actuator target in one automatic-control composition;
- do not hide SCRAM/turbine/generator trips inside PID logic; M5.5 protection arbitrates explicitly above normal control;
- do not let protection read `FullPlantSnapshot` directly; it consumes measured M5.1 signals and their validity/quality semantics;
- do not treat alarm acknowledgement or alarm reset as protection reset; M5.5 physical protection reset remains explicit and permissive-gated;
- do not use wall-clock timestamps for first-out/event ordering; use deterministic logical alarm-event sequences;
- do not let candidate true-state instrumentation feed back into current-step M5.7 control/protection decisions; publish it only as the next committed measured frame;
- do not turn M5.7 verification phases into hidden scenario scripting or forced outcomes; phases only replace explicit immutable input bundles;
- do not alter plant/controller/protection/alarm state to force M5.7 acceptance criteria to pass; gate criteria are observational only;

## Phase gates

### Gate after M3 — COMPLETE

Primary circuit runs headlessly in a stable configured operating condition with deterministic repeatability, bounded long-run drift, closed/inspectable mass-energy accounting, order-independent network results and plant-level snapshots.

### Gate after M4 — COMPLETE

Reactor-to-grid full plant runs manually with a closed energy path and stable reference condition before automatic control is introduced.

### Gate after M5 — COMPLETE

Automatic controls, interlocks and protection are deterministic and testable headlessly before the operator UI becomes the primary interaction surface.

### Gate after M6 — COMPLETE

Every operator action visible in the control room maps to application commands; no physics is implemented in Views/ViewModels. M6.7 local build and complete tests were explicitly confirmed successful on 2026-07-21.

### Gate after M7 — COMPLETE

Versioned initial conditions, deterministic normal-operation procedures, guidance and observational training evaluation are validated. Local build and complete tests for M7.7 were explicitly confirmed on 2026-07-21.

### Gate after M8 — COMPLETE

Fault and transient scenarios are defined as explicit deterministic fault inputs plus commands over the physical plant. Scenario scripts must never force a predetermined physical outcome.

## Baseline discipline

A milestone becomes validated only after:

- local build succeeds with warnings as errors;
- the complete automated suite passes;
- relevant deterministic/conservation/invariant tests pass;
- documentation reflects the implemented behavior;
- user validation is explicitly recorded.

## Validated M9 gate / M10.1–M10.9.4 validated / M10.9.4.1-D.4 validated

M8.1–M8.7 are validated and the M8 gate is complete. M9.1–M9.7 are validated and the M9 gate is complete. **M10.1–M10.9.4 and M10.9.4.1-G.2 Hotfix 2 are VALIDATED**. The current G.3 candidate completes non-turbine current-v2 enthalpy advection while preserving internal-energy inventories and reserving turbine expansion/shaft-work ownership for G.4. M10 closes only after M10.9.8 integrated human-automation-HMI validation.


## H.26 Hotfix 1 validation note

The first H.26 candidate passed build and ordinary tests but exposed an audit-contract error for `ShadowCorrectionNotEvaluated`. Hotfix 1 corrected only that expectation and subsequently passed build, the complete ordinary suite and the focused H.26 gate. H.26 Hotfix 1 is now the validated baseline entering H.27.

## H.29 production activation candidate checkpoint

H.24 Requalification 1 post-H.28 is VALIDATED: 30,008 runtime steps, 9,626/9,626 corrected commits, zero rollback/fallback/unsafe/untargeted disagreement, all four profiles trip-free, deterministic fingerprint `7AF233CE51A866B3E00C2C032AA58EEFBD7290DE0940725E5F4B7860EA6287BE`. H.29 is VALIDATED: 400/400 qualified commits with zero rollback/fallback/unsafe/untargeted disagreement and exact replay/checkpoint compatibility. H.30 is VALIDATED and closes Phase H as `OPT-IN ONLY`. Exact v2 stays `ExplicitCommittedState` and authoritative; exact v3 is the qualified corrected opt-in. I.1 Hotfix 1 is the current Phase-I candidate.
