## M8.2 — Hydraulic Component Faults (baseline candidate)

- Hotfix candidate 2: corrected the new App regression test to use the xUnit `Assert.Single(collection, predicate)` overload required by analyzer rule xUnit2031; production code and M8.2 behavior are unchanged.
- Hotfix candidate 1: corrected the electrical `GENERATOR TARGET` selector to use a neutral `GeneratorSelectionState` instead of inheriting the generator-trip visual state.
- Hotfix candidate 1: made turbine speed/load operator controls fail closed when either the turbine trip or generator trip is active; physical protection ownership remains unchanged.
- Hotfix candidate 1: added the first headless `NuclearReactorSimulator.App.Tests` project with ViewModel regression tests for generator selection, turbine-trip gating, breaker-close permissives, target-index clamping, typed dispatch and the XAML generator-selector binding contract.
- Recorded M8.1 hotfix 1 as locally validated after successful build and complete test suite.
- Added typed M8.2 hydraulic fault applicators for pump trip/degradation, valve fail-open/fail-closed/stuck, valve-controlled path restriction/blockage and selected node leaks.
- Added immutable `HydraulicComponentFaultInputs` consumed inside the existing protected full-plant step; no second pump/valve/network solver is introduced.
- Added selected leak mass + carried internal-energy removal through signed `PlantNetworkSourceTerms`, preserving the one `PlantNetworkOrchestrator` integration/audit boundary.
- Added `HydraulicComponentFaultScenarioPack.Demonstration`, built-in hydraulic applicator registration and end-to-end Application tests over real canonical plant state.
- Added ADR 0061, `docs/HYDRAULIC_COMPONENT_FAULTS.md` and M8.2 milestone/handoff/status/roadmap updates.

# Changelog

## M8.1 — Deterministic Fault-Injection Framework (baseline candidate)

- Hotfix candidate 1: corrected the scenario-v2 deserializer fallback for fault parameters so both operands of the null-coalescing expression are `SortedDictionary<string, string>`; no schema, ordering or fault semantics changed.
- Recorded explicit local validation of M7.7: compilation and complete tests passed; M7.7 is now the validated baseline and the M7 gate is complete.
- Added explicit immutable scenario fault declarations with stable fault/type/target IDs, deterministic parameters and activation/optional deactivation triggers.
- Added exact logical-step and named committed-`ControlRoomSnapshot` plant-condition trigger semantics with no wall-clock/random scheduling.
- Added fail-closed exact-ID registries for runtime-bound fault applicators and plant-condition evaluators.
- Added deterministic single-pass `Pending → Active → Cleared` lifecycle state with logical-step stamps and monotonic transition sequence.
- Added `ScenarioFaultRuntimeEngine` as a scheduling/lifecycle decorator around the canonical runtime; M8.1 itself adds no concrete subsystem fault physics.
- Added fault lifecycle projection to `ControlRoomSnapshot` and deterministic replay reconstruction from the same versioned scenario definition.
- Advanced scenario JSON persistence to schema v2 with deterministic v0/v1 migration that preserves exact initial-condition identity and invents no faults.
- Added M8.1 application/infrastructure tests, ADR 0060 and `docs/DETERMINISTIC_FAULT_INJECTION_FRAMEWORK.md`.

## M7.7 — Training Objectives, Procedure Guidance & Evaluation — VALIDATED

- Recorded explicit local validation of M7.6: compilation and complete tests passed; M7.6 is now the validated baseline.
- Added a deterministic accepted-operator-action journal at the scenario command boundary; runtime-host commands and rejected actions are excluded.
- Added `DeterministicStepCompleted` observation on the Application runtime coordinator so training evaluation sees every fixed simulation step independent of presentation publication stride.
- Added generic training checkpoints, evaluation criteria, objective scoring, procedure-deviation penalties and optional `Hidden` / `ChecklistOnly` / `Guided` assistance modes.
- Added historical first-achievement checkpoint tracking and ordered accepted-action sequence evaluation without mutating physics, control, protection or alarms.
- Added the 100-point `Integrated Normal Operations Training` capstone over the validated M7.6 `stable-low-load-parallel-operation` v1 initial condition.
- Added desktop training evaluation presentation, M7.7 application tests, ADR 0059 and `docs/TRAINING_OBJECTIVES_GUIDANCE_EVALUATION.md`.

## M7.6 — Power Manoeuvring & Normal Shutdown — VALIDATED

- Recorded explicit local validation of M7.5: compilation and complete tests passed; M7.5 is now the validated baseline.
- Added exact `stable-low-load-parallel-operation` v1 with canonical breaker-closed 5 MWe low-load handoff.
- Extended the canonical operational-seed helper with optional breaker/load seed parameters while preserving all earlier M7 defaults.
- Added bounded power-manoeuvring guidance using only validated generator-load, rod and turbine-speed command seams.
- Added observational temperature/void checks and preserved quantitative xenon as explicitly unavailable at the M5.7 operational snapshot boundary.
- Added controlled normal-shutdown guidance for unload, breaker open, rod insertion, turbine rundown and continued main circulation.
- Updated desktop composition to load the exact M7.6 session paused.
- Added M7.6 application tests, ADR 0058 and `docs/POWER_MANOEUVRING_NORMAL_SHUTDOWN.md`.

## M7.5 — Grid Synchronization & Load Increase — VALIDATED

- Added exact `pre-synchronization-grid-loading` v1, reusing canonical M7.2 construction with a 3000 rpm phase-matched breaker-open handoff.
- Added observational M7.5 synchronization/load checklist and seven-step guidance through the stable low-load M7.6 handoff.
- Enabled scenario-gated generator breaker close while preserving the authoritative M4.5 synchronization close-check.
- Completed `GeneratorLoadRaise/Lower` translation through bounded M4.5 requested electrical power; no direct rotor torque/output mutation.
- Desktop now loads the M7.5 session paused.

## M7.4 — Heat-Up, Steam Raising & Turbine Startup — validated

- Hotfix 1 validated after successful local build and complete tests.
- Hotfix candidate 1: made saturated steam-space recipe construction robust near the dry-saturated boundary. The existing 0.99 vapor-quality seed is preserved whenever the validated thermodynamic closure resolves it; otherwise initialization deterministically retries at 0.98, remaining inside the same two-phase model envelope without changing the solver.
- Recorded M7.3 as locally validated after successful build and complete tests.
- Added exact-version `low-power-steam-raising` v1 through `HeatUpTurbineStartupInitialConditionFactory`.
- Extended the canonical M7.2 recipe helper with explicit rod-position, primary-temperature and turbine-startup-lineup seed parameters while preserving existing M7.2/M7.3 defaults.
- Added a versioned startup lineup with stop/admission availability, governing control initially closed and no new direct stop-valve owner.
- Added observational heat-up, steam-pressure/inventory, turbine-roll/warm-up/near-synchronous and generator-isolation checks.
- Added declarative M7.4 guidance and fail-closed permissions: turbine speed control is enabled; generator breaker close/load raise/load lower remain blocked for M7.5.
- Updated desktop composition to load the exact M7.4 session paused and display M7.4 guidance/checks.
- Added M7.4 application tests, ADR 0056 and `docs/HEAT_UP_STEAM_RAISING_TURBINE_STARTUP.md`.

## M7.3 — First Criticality & Low-Power Operation — VALIDATED

- Recorded explicit local validation of M7.2 hotfix 1: compilation and complete tests passed; M7.2 is now the validated baseline.
- Added exact-version `pre-criticality-source-range` v1 initial condition reusing the canonical M7.2 construction path with established main circulation and a tiny deterministic non-zero point-kinetics seed.
- Added controlled rod INSERT/HOLD/WITHDRAW scenario permissions while continuing to fail closed on turbine speed, generator load and breaker-close actions.
- Added presentation-only first-criticality checks for source-range power, near-critical reactivity, first criticality, educational low-power band and reactor-period stabilization.
- Added declarative first-criticality/low-power guidance; guidance never auto-dispatches commands or forces physical state.
- Documented the source-range seed as versioned initial-condition data rather than an external neutron-source solver.
- Added an explicit xenon availability objective: quantitative xenon remains `Unavailable` until canonical M2.8 state is promoted through the M5.7 operational envelope.
- Updated desktop composition to load the exact M7.3 session paused and display scenario guidance/checks.
- Added M7.3 application tests, ADR 0055 and `docs/FIRST_CRITICALITY_LOW_POWER.md`.

## M7.2 — Cold Shutdown & Pre-Startup — VALIDATED

- Corrected the M7.2 candidate compile blocker in `ColdShutdownInitialConditionFactory`: added the missing Simulation runtime namespace imports for controller inputs, primary-circuit boundary/integration inputs and turbine-island state/input types; no physics or milestone scope changed.
- Recorded explicit local validation of M7.1: compilation and complete tests passed; M7.1 is now the validated baseline.
- Added exact-version `cold-shutdown-pre-start` v1 built-in Application initial-condition factory reconstructed through canonical M1–M5 owners and the validated simplified water/steam closure.
- Added an operational cold/subcritical seed with rods inserted, pumps stopped, steam admission isolated, turbine stationary and generator breaker open.
- Added presentation-only pre-start readiness definitions/evaluator for signal health, protection, reactor shutdown, rod insertion, circulation, turbine, breaker, steam isolation and annunciator state.
- Added ordered declarative preparation guidance with optional suggested operator actions; guidance never auto-dispatches commands or patches physical state.
- Added fail-closed M7.2 scenario permissions that allow circulation preparation but deliberately exclude rod withdrawal and breaker closure before M7.3.
- Promoted the desktop composition from the no-session shell fallback to a real paused exact-version M7.2 session through the validated M7.1 registry/session boundary.
- Added M7.2 application/runtime-composition tests, ADR 0054 and `docs/COLD_SHUTDOWN_PRESTART.md`.

## M7.1 — Versioned Initial Conditions & Scenario Framework — VALIDATED

- Recorded explicit local validation of M6.7: compilation and complete tests passed; M6 gate is now complete.
- Added immutable exact-version `InitialConditionReference` / descriptor contracts and `IVersionedInitialConditionFactory` reconstruction seam.
- Added `VersionedInitialConditionRegistry` with duplicate rejection and exact-version-only resolution; no silent latest-version fallback.
- Added immutable scenario metadata, descriptive objectives and explicit allowed operator command kinds.
- Added fail-closed `ScenarioCommandDispatcher` while keeping run/pause/single-step under runtime-host ownership.
- Added `ScenarioSessionFactory` as the canonical fresh paused load/start boundary over the validated M6.7 runtime coordinator.
- Added deterministic `ScenarioReplayRunner` reusing the M0 logical `SimulationCommandTrace<ControlRoomCommand>` seam.
- Added Infrastructure JSON scenario schema v1 with deterministic v0→v1 migration that preserves exact initial-condition identity/version and rejects unknown future versions.
- Added M7.1 application/infrastructure tests, ADR 0053 and `docs/INITIAL_CONDITIONS_SCENARIO_FRAMEWORK.md`.
- M7.1 intentionally does not invent the first operational cold-shutdown recipe; M7.2 owns that concrete initial condition and pre-start flow.

## Documentation continuity refresh after M6.7 candidate

- Hardened `PROJECT_HANDOFF.md` as the authoritative new-chat checkpoint with explicit validation state, ownership map, restart protocol and exact continuation point.
- Added `docs/NEW_CHAT_START.md` with a ready-to-paste conversation bootstrap.
- Added `docs/README.md` as a documentation navigation map.
- Clarified across status/roadmap/M6.7 docs that M6.6 is the last explicitly validated baseline and M6.7 remains candidate until local validation is explicitly confirmed.
- Renamed the legacy architecture-debt section to apply to all future phases rather than only M5.

## M6.7 — Control-Room Integration & Performance Baseline

- Added the live M5.7 `IntegratedAutomaticOperationRuntimeEngine` and `ControlRoomRuntimeCoordinator`.
- Added complete typed command translation for rods, MCPs, turbine/generator controls, breakers, protection and annunciator actions.
- Added one-step transient command consumption plus persistent immutable controller/setpoint updates.
- Added bounded accelerated batches and rendering-cadence-independent presentation publication tests.
- Added ADR 0052 and `docs/CONTROL_ROOM_INTEGRATION_PERFORMANCE.md`; after validation M6 is complete and M7.1 is next.

## M6.6 — Trends, Alarms & Event Timeline

- Recorded successful local validation of M6.5 Turbine, Generator & Electrical Panels.
- Added presentation-only M5.6 alarm/annunciator, first-out and event contracts to `ControlRoomSnapshot`.
- Added typed targeted/bulk alarm ACK and RESET Application command intents without changing M5.5 protection ownership.
- Added configurable bounded logical-step trend history over presentation values only, with deterministic same-step replacement and explicit unavailable gaps.
- Added bounded event history deduplicated and ordered by the validated M5.6 monotonic logical sequence number.
- Added the production Alarms & Events workspace with trends, annunciator controls, first-out groups and deterministic event timeline.
- Added M6.6 presentation/history tests, ADR 0051 and `docs/TRENDS_ALARMS_EVENT_TIMELINE.md`; next planned milestone after validation is M6.7 Control-Room Integration & Performance Baseline.

## M6.5 — Turbine, Generator & Electrical Panels

- Recorded successful local validation of M6.4 Primary-Circuit Mnemonics.
- Added `TurbineSecondaryPanelSnapshot` and `ElectricalPanelSnapshot` Application presentation contracts with canonical M4 topology/equipment identity.
- Added measured M5.1 turbine shaft power, rotor speed, condenser pressure/vacuum/hotwell mass, generator frequency/output and gross electrical output presentation.
- Added explicitly labelled model diagnostics for main steam/admission, turbine stages, condenser/feedwater and generator/grid synchronization details.
- Added turbine/generator trip presentation plus typed turbine-speed, generator-load, breaker close/open and trip operator command intents.
- Added fail-closed breaker-close UI gating from published synchronization permissives while keeping M4.5 authoritative.
- Added M6.5 presentation-contract tests, ADR 0050 and `docs/TURBINE_GENERATOR_ELECTRICAL_PANELS.md`; next planned milestone after validation is M6.6 Trends, Alarms & Event Timeline.

## M6.4 — Primary-Circuit Mnemonics

- Added presentation-only primary-circuit snapshots for loops, MCPs, fuel-channel branches, steam drums and primary-connected valves.
- Projected measured M5.1 loop flow/header ΔP and drum pressure/level separately from explicitly labelled model diagnostics.
- Added typed main-circulation-pump START/RUN and STOP Application command intents without changing M5.3/M3 ownership.
- Added the Primary Circuit mnemonic workspace, M6.4 tests, ADR 0049 and `docs/PRIMARY_CIRCUIT_MNEMONICS.md`.
- Recorded M6.3 as validated; next planned milestone after validation is M6.5 Turbine, Generator & Electrical Panels.

## M6.3 — Reactor/Core Panel

- Marked M6.2 as the locally validated baseline after successful build and complete test execution.
- Added Application-only reactor/core presentation contracts with measured reactor-power projection and explicitly labelled kinetics/reactivity/rod/core-zone diagnostics.
- Added the first domain-specific Reactor/Core workspace with coarse zone tiles, canonical rod state/target presentation and M5.5 SCRAM/interlock context.
- Added typed rod insert/hold/withdraw, SCRAM and protection-reset operator command intents without UI-side state mutation.
- Kept missing M2.8 xenon operational state explicitly unavailable rather than reconstructing/synthesizing it in presentation code.
- Added M6.3 tests, ADR 0048, `docs/REACTOR_CORE_CONTROL_ROOM_PANEL.md` and `docs/milestones/M6.3.md`; next planned milestone after validation is M6.4 Primary-Circuit Mnemonics.

## M6.2 — Reusable Instrument & Control Components

- Marked M6.1 as the locally validated baseline after successful build and complete test execution.
- Added shared Application-layer `ControlRoomVisualState` semantics for Normal, Warning, Trip and Unavailable presentation.
- Added a stable component/interaction catalog for numeric indicators, meters, lamps, toggle switches, selectors and pushbuttons without Avalonia coupling.
- Added reusable Avalonia control-room components plus a shell component gallery for visual validation.
- Standardized display-only versus interactive behavior, keyboard/pointer rules and fail-closed unavailable-state handling.
- Added M6.2 presentation-contract tests, ADR 0047, `docs/CONTROL_ROOM_COMPONENT_LIBRARY.md` and `docs/milestones/M6.2.md`; next planned milestone after validation is M6.3 Reactor/Core Panel.

## M6.1 — Control-Room Application Shell

- **VALIDATED** after successful local build and complete test execution.
- Marked M5.7 as the locally validated baseline and closed the complete M5 automatic-operation gate after successful build/test execution.
- Added stable Overview, Reactor, Primary, Turbine/Secondary, Electrical and Alarms/Events control-room workspaces.
- Added narrow Application-layer `ControlRoomSnapshot` projection/source contracts so Avalonia consumes presentation state rather than authoritative full-plant truth.
- Added typed `ControlRoomCommand` / `IControlRoomCommandDispatcher` seams and shell run/pause/single-step dispatch without UI-side physics.
- Removed the Avalonia project's direct Simulation project reference and added architecture tests forbidding direct Simulation namespace use from App source.
- Added scalable desktop shell layout, presentation-only performance budgets, ADR 0046, `docs/CONTROL_ROOM_APPLICATION_SHELL.md` and `docs/milestones/M6.1.md`; next planned milestone after validation is M6.2.

## M5.7 — Integrated Automatic-Operation Baseline

- Marked M5.6 as the locally validated baseline after the corrected build and complete test suite passed.
- Added canonical `IntegratedAutomaticOperationState` / inputs / snapshot composition over existing physical, instrumentation, controller, protection and annunciator owners.
- Added committed measured-frame ordering: current M5 decisions use one committed frame; instrumentation over candidate true state becomes the next-step frame.
- Added deterministic headless verification phases for reference hold, explicit setpoint/input changes and protection/interlock expectation cases without a hidden scenario scheduler.
- Added measured tracking and raw mass/energy closure, signal-validity and annunciator acceptance metrics with observational-only criteria.
- Added M5.7 integration tests, ADR 0045, `docs/INTEGRATED_AUTOMATIC_OPERATION.md` and `docs/milestones/M5.7.md`; next planned milestone after validation is M6.1.

## M5.6 — Alarms & Annunciator State

- Added deterministic alarm conditions over measured M5.1 channels and observational M5.5 protection state.
- Added explicit non-latching and latched-until-reset annunciator semantics with independent acknowledgement and safe reset.
- Added deterministic first-out grouping and monotonic logical alarm-event ordering without wall-clock dependencies.
- Added immutable alarm, first-out-group and event snapshots plus an observational M5.6 wrapper over the validated M5.5 protected step.
- Marked M5.5 validated and advanced the next planned milestone to M5.7 integrated automatic-operation baseline.



## M5.5 — Interlocks, Trips & SCRAM

- Marked M5.4 as the locally validated baseline after successful compilation and complete automated test-suite confirmation.
- Added measured-signal-only deterministic `ProtectionSystemDefinition` with latching high/low trip functions, reset hysteresis and explicit fail-closed invalid-measurement policy.
- Added reactor SCRAM, turbine trip and generator trip latching actions plus explicit manual trip/reset seams and measured reset permissives.
- Added non-latching rod-withdrawal, turbine-admission-opening and generator-breaker-close interlocks.
- Added explicit protection-over-normal-control arbitration through canonical M2 rod commands, M4.1 stop valves, M4.2 turbine `TripCommand` and M4.5 breaker-open commands.
- Added `ProtectedAutomaticFullPlantSolver` composing M5.3 + M5.4 + M5.5 over one measured frame and one M4.7 physical step.
- Added protection/arbitration diagnostics separated from alarm presentation, ADR 0043, `docs/PROTECTION_INTERLOCKS_TRIPS_SCRAM.md` and `docs/milestones/M5.5.md`.

## M5.4 — Turbine, Steam & Feedwater Control Loops

- Recorded successful local validation of M5.3 as the reactor/primary automatic-control baseline.
- Added semantic M5.4 turbine-speed/load, steam-pressure, steam-drum-level and hotwell-inventory loop definitions over measured signals and M5.2 controller/actuator primitives.
- Added canonical normal-operation admission-valve validation; stop valves remain reserved for M5.5 trip/isolation logic.
- Added hotwell-mass instrumentation source and canonical condensate/feedwater pump command adapters without duplicate pump or inventory state.
- Added automatic M4.2 stage-flow replacement from the limiting projected stop/control/admission valve path while preserving the single plant-network hydraulic integration.
- Added integrated M5.3 + M5.4 automatic-control composition over one measured frame, disjoint physical actuator targets and one M4.7 physical full-plant step.
- Added M5.4 simulation verification, ADR 0042, `docs/TURBINE_STEAM_FEEDWATER_CONTROL_LOOPS.md` and `docs/milestones/M5.4.md`; updated handoff/status/roadmap/architecture/application metadata.


## M5.3 — Reactor & Primary-System Control Loops

- Recorded successful local validation of M5.2 as the reusable controller/actuator primitive baseline.
- Added canonical reactor/primary loop definitions that bind measured-signal controllers and typed actuator commands to specific rod/group and main-circulation-pump owners.
- Added main-circulation semantic instrument sources for total pump flow and header pressure rise.
- Reused the validated M2 `ControlRodSystemSolver`, rod-reactivity model, point kinetics and fission-power scaling rather than introducing a synthetic controller-to-power shortcut.
- Added explicit non-rod-reactivity input seam for temperature/void/xenon/manual contributions composed outside the controller primitive.
- Added committed-state ordering: current committed rods determine current kinetics; controller commands advance rods for the next committed step.
- Added canonical MCP command application by replacing only operational `PumpState` before the one existing M4.7 physical step.
- Added controlled full-plant input rewriting that replaces only the M3 total-fission-power seam with point-kinetics-derived power while preserving downstream M3 spatial heat-deposition ownership.
- Added immutable reactor/primary control diagnostics and tests for rod motion, reactivity progression, kinetics/fission coupling, pump command application and topology/source validation.
- Added ADR 0041, `docs/REACTOR_PRIMARY_CONTROL_LOOPS.md` and `docs/milestones/M5.3.md`; updated handoff/status/roadmap/architecture/application metadata.

## M5.2 — Controller & Actuator Primitives

- Recorded successful local validation of M5.1 as the measured-signal instrumentation baseline.
- Added canonical P/PI/PID controller definitions bound exclusively to `MeasuredSignalFrame` channel ids.
- Added deterministic fixed-step controller state, manual/automatic modes, output limits, conditional-integration anti-windup and bumpless manual-to-auto transfer.
- Added explicit invalid/unavailable-measurement behavior that holds the last command without integrating hidden controller state.
- Added typed controller output frames and detailed controller diagnostics for P/I/D terms, saturation, anti-windup and transfer state.
- Added canonical controller-to-actuator bindings and typed valve-position, pump-speed/run and control-rod motion command seams.
- Kept actuator command memory separate from physical plant/rod ownership; plant-specific loop wiring remains deferred to M5.3/M5.4.
- Added M5.2 domain/simulation tests, ADR 0040, `docs/CONTROLLER_ACTUATOR_PRIMITIVES.md` and milestone documentation.

## M5.1 — Instrumentation & Signal Model

- Recorded M4.7 approval as the validated full-plant steady-state baseline and closed the M4 gate.
- Added canonical instrumentation/channel definitions, finite signal ranges and linear output scaling.
- Added stable semantic true-state source catalog over the immutable M4.7 `FullPlantSnapshot`, including aggregate and per-component plant signals.
- Added separate `InstrumentationState` for deterministic first-order lag/filter memory only.
- Added controller/UI-facing `MeasuredSignalFrame` with no direct true-state reference plus diagnostic-only processing traces.
- Added explicit signal validity, quality, out-of-range/clamp reporting and deterministic bias/freeze/failed-low/failed-high/unavailable fault seams.
- Added `InstrumentedFullPlantSolver` composition that preserves the single M4.7 physical evolution path and observes the resulting immutable snapshot without duplicate physical integration.
- Added M5.1 domain/simulation tests, ADR 0039, `docs/INSTRUMENTATION_SIGNAL_MODEL.md` and milestone documentation.

## M4.7 — Full-Plant Steady-State Baseline

- Recorded successful local validation of M4.6 as the new baseline.
- Added canonical `FullPlantState`, thin `FullPlantSolver` and immutable `FullPlantSnapshot` over the existing M4.6 state owners without a new physical integrator.
- Added fixed-input `FullPlantReferenceOperatingPoint`, explicit `FullPlantSteadyStateCriteria` and deterministic `FullPlantLongRunRunner` / result metrics.
- Added raw long-run mass, coupled stored-energy, rotor-speed, electrical-output and first-law closure drift reporting with no hidden state correction.
- Added gross plant-performance diagnostics for reactor heat, turbine shaft, generator mechanical input/electrical export, condenser rejection, generator losses, efficiency and heat rate.
- Added deterministic 1,000-step full-plant reference verification and criteria rejection tests.
- Added ADR 0038, `docs/FULL_PLANT_STEADY_STATE.md` and `docs/milestones/M4.7.md`; updated project handoff/status/roadmap/architecture/application metadata.
- M4.7 remains a baseline candidate until local build/test validation is reported.

## M4.6 — Integrated Secondary-Cycle Heat Balance

- Marked M4.5 as the locally validated baseline after successful build and complete automated test-suite confirmation.
- Added canonical `IntegratedSecondaryCycleDefinition`, inputs, solver, snapshot and step-result composition over the validated M4.5 reactor-to-grid stack without new mutable physical state.
- Added `SecondaryCycleHeatBalanceAudit` reconciling thermofluid stored energy, rotor kinetic energy, turbine shaft transfer, generator mechanical input, electrical export and conversion losses.
- Added explicit nuclear heat, pump hydraulic power, feedwater conditioning and condenser heat-rejection diagnostics in the integrated first-law boundary.
- Added raw supplemental-power classification, shaft-transfer, mechanical-to-electrical, coupled-domain and full-path closure residuals; no residual correction or hidden bookkeeping is introduced.
- Surfaced authoritative closed-loop external-mass and mass-closure diagnostics from the existing plant-network audit.
- Added deterministic repeated-step coupled verification across M3 + M4.1–M4.5 while preserving single thermofluid, rotor and electrical state ownership.
- Added ADR 0037, `docs/SECONDARY_CYCLE_HEAT_BALANCE.md` and `docs/milestones/M4.6.md`; updated project handoff/status/roadmap/architecture/application metadata.
- M4.6 local build and complete automated test-suite validation were subsequently reported successful; M4.6 is the validated baseline for M4.7.

## M4.5 — Generator, Grid & Synchronization Physics

- Marked M4.4 as the locally validated baseline after successful build and complete automated test-suite confirmation.
- Added strongly typed `Frequency`, `ElectricPotential`, deterministic normalized `PhaseAngle` and shortest-separation `PhaseAngleDifference` quantities.
- Added canonical `ElectricalGridDefinition`, `SynchronousGeneratorDefinition` and `GeneratorGridSystemDefinition` over the validated M4.4 secondary-cycle stack with exact one-generator-per-M4.2-rotor ownership.
- Added separate deterministic `GeneratorGridState` for grid phase, generator electrical phase and breaker state.
- Added rotor-speed/pole-pair-derived electrical frequency plus fixed-step grid/generator phase advancement with no wall-clock dependency.
- Added manual breaker close/open commands with explicit frequency, phase and voltage synchronization windows and observable rejected-close diagnostics.
- Required legacy M4.2 manual external-load torque to be zero while M4.5 owns generator electromagnetic loading.
- Added requested electrical-power to electromagnetic-torque feedback through the existing single M4.2 rotor integrator.
- Added shaft-to-electrical conversion efficiency, generator loss accounting, electrical export snapshots and `GeneratorElectricalAudit`.
- Preserved higher-phase supplemental thermofluid composition through the wrapped M4.2 solver.
- Added electrical quantity, topology, synchronization, breaker, load-seam, audit and determinism tests.
- Added ADR 0036, `docs/GENERATOR_GRID_SYNCHRONIZATION.md` and `docs/milestones/M4.5.md`; updated project handoff/status/roadmap/architecture/application metadata.
- M4.5 local build and complete automated test-suite validation were subsequently reported successful; M4.5 is the validated baseline for M4.6.

## M4.4 — Condensate & Feedwater Train

- Marked M4.3 as the locally validated baseline after successful build and complete automated test-suite confirmation.
- Added canonical `CondensateFeedwaterSystemDefinition` and per-train topology binding from M4.3 hotwells to every M3 feedwater seam.
- Reused existing canonical `PumpDefinition` components for condensate/feedwater transport; no second hydraulic graph or inventory integrator was introduced.
- Added exact hotwell → condensate pump → feedwater inventory → feedwater pump → steam-drum target validation.
- Required legacy M3 feedwater boundary mass-flow inputs to be zero while M4.4 owns the closed condensate return path.
- Added bounded lumped feedwater thermal-conditioning power as explicit positive external energy on the canonical feedwater inventory.
- Added deterministic pump, inventory, thermal-conditioning and inherited global-audit snapshots.
- Extended `CondenserSystemSolver` backward-compatibly with higher-phase supplemental source-term composition before the same single plant-network integration.
- Added closed-mass-path, legacy-source exclusion, conditioning-energy and determinism tests.
- Added ADR 0035, `docs/CONDENSATE_FEEDWATER_TRAIN.md` and `docs/milestones/M4.4.md`; updated project handoff/status/roadmap/architecture/application metadata.
- M4.4 local build and complete automated test-suite validation were subsequently reported successful; M4.4 is the validated baseline for M4.5.

## M4.3 — Condenser, Vacuum & Hotwell

- Marked M4.2 as the locally validated baseline after successful build/test confirmation, including the fixed saturated-mixture fixture and anti-reverse floating-point canonicalization.
- Added canonical `CondenserSystemDefinition`, `CondenserDefinition` and `CondenserCoolingBoundaryDefinition` over every validated M4.2 turbine exhaust seam.
- Added one-to-one turbine-stage-to-condenser topology validation with canonical steam-space and hotwell fluid nodes.
- Added complete M4.3 cooling-boundary inputs with explicit available heat-rejection power.
- Added deterministic committed-state condensation limited by condenser capacity, available vapor inventory and cooling-boundary capacity.
- Added conservative steam-space-to-hotwell mass transfer with explicit signed external condenser heat rejection.
- Added dynamic condenser pressure/vacuum, phase/quality, condensation-limit and hotwell inventory snapshots.
- Extended `TurbineExpansionSolver` backward-compatibly with higher-M4 supplemental source-term composition before the same single plant-network integration.
- Added topology, exact-input-coverage, cooling-limit, conservation, vacuum-response and determinism tests.
- Added ADR 0034, `docs/CONDENSER_VACUUM_HOTWELL.md` and `docs/milestones/M4.3.md`; updated project handoff/status/roadmap/architecture/application metadata.
- M4.3 local build and complete automated test-suite validation were subsequently reported successful; M4.3 is the validated baseline for M4.4.

## M4.2 — Turbine Rotor & Expansion Model

- Marked M4.1 as the locally validated baseline after successful build/test confirmation.
- Added strongly typed `AngularSpeed`, `Torque` and `MomentOfInertia` mechanical quantities plus `TurbineEfficiency`.
- Added canonical `TurbineExpansionSystemDefinition`, `TurbineRotorDefinition` and `TurbineStageGroupDefinition` over the validated M4.1 admission seams.
- Added explicit canonical exhaust-node ownership seams for the upcoming condenser milestone.
- Replaced the active M4.1 terminal sink in M4.2 operation with conservative inlet-to-exhaust mass transfer and explicit thermofluid-to-shaft energy extraction.
- Added immutable `TurbineExpansionState` / `TurbineRotorState`, rotor inertia, turbine/load/net torque, deterministic angular-speed integration and kinetic-energy diagnostics.
- Added manual external-load torque as a replaceable generator seam, including zero-speed reversal limiting with commanded/effective values both observable.
- Added `TurbineMechanicalAudit` to close shaft work against rotor kinetic-energy change plus external load.
- Added explicit trip-command flow blocking and diagnostic overspeed seams without hidden automatic protection or latching.
- Extended `MainSteamNetworkSolver` backward-compatibly with higher-phase supplemental source-term composition before the same single plant-network integration.
- Added M4.2 topology, conservation, rotor, trip and overspeed tests.
- Added ADR 0033, `docs/TURBINE_EXPANSION_AND_ROTOR.md` and `docs/milestones/M4.2.md`; updated project handoff/status/roadmap/architecture/application metadata.
- M4.2 local build and complete automated test-suite validation were subsequently reported successful; M4.2 is the validated baseline for M4.3.

## M4.1 — Main Steam Network & Turbine Admission

- M3.8 local build and complete test-suite validation closes the M3 primary-circuit gate.
- Added canonical `MainSteamNetworkDefinition` mapping every M3 steam-export seam to an existing plant main-steam pipe and header.
- Added exact stop → control → admission valve-train topology validation over existing canonical `ValveDefinition` components.
- Added replaceable turbine-admission boundary definitions and complete per-step terminal demand inputs.
- M4.1 inputs require legacy M3 steam-export sinks to be commanded to zero while the main-steam network owns downstream transport, preventing double steam removal.
- Added committed-state line/valve pressure, flow, energy and fail-safe diagnostics plus turbine-inlet state/continuity diagnostics.
- Added temporary turbine-admission signed external mass/energy accounting using committed turbine-inlet specific internal energy.
- Added a backward-compatible higher-phase supplemental-source-term seam to `IntegratedPrimaryCircuitSolver`, preserving exactly one `PlantNetworkOrchestrator` integration.
- Added topology, boundary-exclusion, fail-safe and integrated conservation tests.
- Added ADR 0032, `docs/MAIN_STEAM_NETWORK.md` and `docs/milestones/M4.1.md`.
- M4.1 local build and complete automated test-suite validation was reported successful by the user.

## M3.8 — Integrated Primary-Circuit Baseline — VALIDATED

- M3.7 local build and complete test-suite validation establishes the feedwater/steam boundary baseline.
- Added canonical `IntegratedPrimaryCircuitDefinition` spanning core zones, channel groups, main circulation, steam drums and M3 external boundaries.
- Added immutable integrated inputs for core state, fission power, decay heat and complete boundary inputs.
- Added `IntegratedPrimaryCircuitSolver`, which evaluates every subsystem from the same committed plant state and performs exactly one plant-network integration.
- Added integrated plant-level snapshot with global inventory/power/flow aggregates plus per-subsystem diagnostics and conservation audit.
- Added configurable reference operating points and a deterministic headless long-run runner reporting raw inventory drift and maximum audit residuals without corrective bookkeeping.
- Added integrated composition, canonical-lineage and long-run deterministic equilibrium tests.
- Added ADR 0031, `docs/INTEGRATED_PRIMARY_CIRCUIT.md` and `docs/milestones/M3.8.md`.
- M3.8 local build and complete automated test-suite validation was reported successful by the user.

## M3.7 — Feedwater & Steam Boundary Interfaces — VALIDATED

- M3.6 local build/test validation establishes the validated steam-drum/separation baseline.
- M3.7 local build and complete automated test-suite validation was reported successful by the user.
- Added canonical feedwater-source and steam-export boundary definitions with exactly one of each per steam drum.
- Added complete canonical per-step boundary inputs with controllable mass flow and explicit feedwater specific internal energy.
- Added committed-state steam-export energy removal from the canonical steam-outlet node.
- Extended `PlantNetworkSourceTerms` with signed external mass flow and signed external power while preserving the existing constructor.
- Added `PlantNetworkSourceTerms.Combine(...)` for staged source-term composition ahead of the single M3.2 integration boundary.
- Extended `PlantNetworkAudit` with declared external mass flow and an explicit balance mass-rate residual.
- Added immutable feedwater/export/system snapshots and topology/accounting/integration tests.
- Added ADR 0030 and `docs/PRIMARY_CIRCUIT_BOUNDARIES.md`.
- Integrated long-run primary-circuit reference-state validation remains deferred to M3.8.

## M3.6 — Steam Drums, Separation & Recirculation

- M3.5.1 local build/test validation establishes the validated main-circulation baseline.
- Added backward-compatible explicit loop `ReturnCollectorNodeId` so channel returns can terminate at dedicated drum inventory nodes.
- Added canonical steam-drum/system definitions with one drum per circulation loop and eager topology validation.
- Added `SteamDrumLevelFraction`, per-drum/system snapshots and committed-state phase/quality/void/level diagnostics.
- Added deterministic ideal phase separation with saturated liquid/vapor internal-energy routing from the M1.7 water/steam model.
- Added conservative internal `PlantNetworkSourceTerms` for steam export-node transfer and liquid recirculation to MCP suction headers.
- Added topology, separation, conservation and network-audit integration tests.
- Added ADR 0029 and `docs/STEAM_DRUMS.md`.
- Feedwater and external steam boundary accounting were deferred to the separately implemented M3.7 candidate.

## M3.5.1 — Main Circulation Test Namespace Hotfix

- Fixed two test-only `using` directives that referenced the nonexistent `Domain.Physics.Reactor.Core.ThermalPower` namespace.
- `HeatDepositionFraction` is now imported from its canonical `Domain.Physics.Reactor.ThermalPower` namespace.
- No production code or simulation behavior changed.

## M3.5 — Main Circulation System

- M3.4 local build/test validation establishes the validated fuel-channel-group baseline.
- Added canonical semantic main-circulation system/loop/branch definitions over existing plant components.
- Added eager validation of suction/pressure headers, MCP endpoint direction, group ownership and return-path closure.
- Added committed-state `MainCirculationSystemSolver` without a second integration boundary.
- Added per-pump, per-branch, per-loop and whole-system immutable diagnostics for flow, pressure, power, phase/quality/void and continuity residuals.
- Added order-independence, branch-resistance distribution, topology-validation and network-integration tests.
- Added ADR 0028 and `docs/MAIN_CIRCULATION_SYSTEM.md`.
- Steam drums/separation, pump coastdown/electrical dynamics and detailed two-phase hydraulic correlations remain deferred.

## M3.4 — Fuel-Channel Group Model

- Added canonical equivalent fuel-channel groups mapped to M3.3 core zones and existing M3.1 plant components.
- Added represented channel counts, per-zone group power shares and explicit fuel/structure/coolant heat-deposition fractions.
- Added exact deterministic group partitioning of zonal fission power plus optional global decay-heat routing.
- Added committed-state per-group hydraulic diagnostics using the validated passive `PipeFlowSolver`.
- Added `PlantNetworkSourceTerms` so nuclear heat enters M3.2 staged balance accumulation without direct inventory mutation.
- Extended `PlantNetworkAudit` with explicit supplemental external power.
- Added channel-group domain/simulation/integration tests and ADR 0027.
- No steam drums, main circulation headers/pumps integration or individual-channel fidelity is introduced in M3.4.

## M3.3 — Aggregated Core-Zone Model

- M3.2 local build/test validation establishes the validated deterministic network-orchestration baseline.
- Added configurable `CoreZoneCoordinate` and normalized `CoreZonePowerFraction` primitives without a fixed grid-size assumption.
- Added `CoreZoneDefinition` / `AggregatedCoreDefinition` with canonical ordering, unique coordinates, normalized nominal power shares and eager references to canonical plant fuel/structure/coolant domains.
- Added `CoreZoneState` / `AggregatedCoreState` with exact zone-set validation and normalized current power shares.
- Added `AggregatedCorePowerSolver` with deterministic exact-closure global-to-zone fission-power allocation while keeping point kinetics global.
- Added `CoreZoneSnapshot` / `AggregatedCoreSnapshot` local committed-state diagnostics including thermal state, coolant pressure/phase/quality and volumetric void projection.
- Added ADR 0026 and `docs/CORE_ZONE_MODEL.md`.
- No spatial neutron diffusion, channel hydraulics or per-zone heat-deposition physics is introduced in M3.3.

## M3.2 — Deterministic Multi-Component Network Orchestration

- M3.1 local build/test validation establishes the validated plant composition/topology baseline.
- Added `PlantNetworkOrchestrator` with staged committed-state-only solving for pipes, valves, pumps, heat-transfer links and heat sources.
- Added deterministic canonical balance accumulation before any conserved inventory integration.
- Added exactly-once `FluidNodeIntegrator`/`ThermalBodyIntegrator` execution per stateful inventory per network step.
- Added immutable `PlantNetworkStepResult` balance diagnostics and `PlantNetworkAudit` global mass/energy accounting.
- Added explicit pump hydraulic-work and heat-source external-power accounting without hidden conservation correction.
- Added parallel-connection committed-state tests, shuffled-registry order-independence tests and fixed-step runtime pulse-segmentation integration.
- Added ADR 0025 and `docs/PLANT_NETWORK_ORCHESTRATION.md`.


## M3.1 — Plant Composition & Topology Baseline

- Added immutable `PlantDefinition` with canonical registries for fluid nodes, passive pipes, valves, pumps, thermal bodies, heat-transfer links and heat sources.
- Added global topology-ID uniqueness, including wrapped valve/pump hydraulic-path IDs, to keep diagnostics and future recorder/UI addressing unambiguous.
- Added eager validation of all hydraulic endpoints, thermal-domain links and heat-source targets.
- Added immutable complete `PlantState` with exact state-set validation and canonical fluid/thermal definition consistency.
- Added plant-level immutable `PlantSnapshot` as the first committed whole-plant observation boundary.
- Added topology/state/snapshot tests covering canonical order, caller collection independence, missing references, ID collisions, incomplete states and typed lookups.
- Added `docs/PLANT_COMPOSITION.md`, ADR 0024 and milestone M3.1 documentation.
- M2.8 local validation closes M2 and establishes the validated reactor-physics baseline carried into M3.1.
- No physical equations or validated M1/M2 solver behavior changed.

## M2.8.1 — M2 Closure & Roadmap Consolidation

- Recorded successful local validation of M2.8 and closure of the complete M2 reactor-physics foundation.
- Added `docs/PROJECT_STATUS.md` with validated capability map, intentional gaps and phase gates.
- Added `docs/PRIMARY_CIRCUIT_PLAN.md` with the detailed M3.1–M3.8 integration sequence.
- Expanded the roadmap into granular M3–M9 milestones with explicit cross-phase acceptance gates.
- Added ADR 0023 requiring staged committed-state network solving, deterministic balance accumulation and one integration per conserved inventory.
- Updated application baseline/status metadata to identify M3.1 as the next implementation milestone.
- No simulation equations, public physics APIs or runtime behavior changed.

## M2.8 — Iodine/Xenon Dynamics

- Added explicit normalized immutable I-135 and Xe-135 inventories.
- Added configurable fission-power-scaled iodine/direct-xenon production, isotope decay constants and neutron-population-dependent xenon burnup.
- Added analytic finite-step integration of the coupled reduced I/Xe linear system for deterministic fixed-step evolution.
- Added long-operation equilibrium initialization and detailed production/decay/burnup diagnostics.
- Added signed configurable `XenonReactivityCoefficient` and one named `ReactivityContributionKind.Xenon` contribution through the validated M2.1 composition boundary.
- Added shutdown xenon-buildup, burnup, equilibrium, reactivity-composition and runtime pulse-segmentation tests.
- Added ADR 0022 and `docs/IODINE_XENON_DYNAMICS.md`.
- M2.7.1 local validation establishes the validated void-feedback baseline carried into M2.8.
- M2.8 subsequently passed local build/test validation and closes the complete M2 reactor-physics baseline.

## M2.7.1 — Void Feedback Composition Test Hotfix

- Fixed the expected total in `VoidFeedbackSolverTests.MultipleInputs_AreCanonicalAndComposeThroughReactivityModel`.
- The two configured contributions are `+10 pcm` for `void/a` and `-20 pcm` for `void/b`, so the physically correct composed total is `-10 pcm`, not `+30 pcm`.
- The production `VoidFeedbackSolver`, `ReactivityModel`, public APIs and physical behavior are unchanged.
- M2.7 remains the functional milestone; M2.7.1 was validated locally and closes the M2.7 baseline.

## M2.7 — Void Feedback

- Added strongly typed `VoidFraction` and signed `VoidFractionDifference`, explicitly distinct from saturated-mixture `VaporQuality`.
- Added signed `VoidReactivityCoefficient` with canonical delta-k/k per unit void fraction storage and explicit pcm per percentage-point void conversion.
- Added immutable `VoidReactivityFeedbackDefinition` with explicit reference void fraction.
- Added deterministic `WaterSteamVoidFractionSolver`: subcooled liquid maps to zero void, superheated vapor to full void, and saturated mixtures use quality plus M1.7 saturation densities.
- Added pure `VoidFeedbackSolver`, immutable diagnostics, canonical multi-feedback ordering and composition through the validated M2.1 `ReactivityModel`.
- Added water/steam-state-to-void-to-reactivity-to-point-kinetics runtime integration with fixed-step pulse-segmentation determinism.
- Added ADR 0021 and `docs/VOID_FEEDBACK.md`.
- M2.6 local validation establishes the validated temperature-feedback baseline carried into M2.7.

## M2.6 — Temperature Feedback

- Added strongly typed signed `TemperatureReactivityCoefficient` with canonical delta-k/k/K storage and explicit pcm/K conversion.
- Added immutable `TemperatureReactivityFeedbackDefinition` values restricted to fuel- and coolant-temperature contribution categories.
- Added pure `TemperatureFeedbackSolver` with linear `rho = alpha * (T - T_ref)` evaluation and immutable diagnostics.
- Added canonical multi-feedback ordering, duplicate-ID validation and composition through the validated M2.1 `ReactivityModel`.
- Added deterministic committed-state coupling from temperature feedback to point kinetics, fission power and subsequent thermal evolution.
- Added first closed thermal-neutronic feedback-loop runtime test with external pulse-segmentation invariance.
- Added ADR 0020 and `docs/TEMPERATURE_FEEDBACK.md`.
- M2.5 local validation establishes the validated decay-heat baseline carried into M2.6.

## M2.5 — Decay Heat

- Added strongly validated `DecayHeatGenerationFraction` and configurable equivalent `DecayHeatGroupDefinition` values.
- Added immutable latent `DecayHeatState`/`DecayHeatGroupState` energy inventories with empty and long-operation equilibrium initialization.
- Added `DecayHeatSolver` implementing exact analytic finite-step first-order group evolution `dE/dt = f*P_fission - lambda*E`.
- Added explicit precursor-production energy, emitted-decay-energy and latent-inventory balance accounting.
- Added average same-step decay-heat deposition for exact thermal integration and end-of-step instantaneous snapshot diagnostics.
- Added canonical complete decay-heat destination partition with adapters to thermal-body and zero-mass fluid-node energy balances.
- Added shutdown persistence, half-life, buildup, equilibrium, energy-conservation and fixed-step pulse-segmentation determinism tests.
- Added ADR 0019 and `docs/DECAY_HEAT.md`.
- M2.4 local validation establishes the validated thermal-power baseline carried into M2.5.

## M2.4 — Thermal Power

- Added explicit `FissionPowerCalibration` between normalized neutron population and reference fission thermal power.
- Added validated `HeatDepositionFraction`, canonical fission-heat destinations and complete partition validation.
- Added stateless deterministic `FissionPowerSolver` with linear neutron-population scaling and fail-fast numerical overflow protection.
- Added immutable `FissionPowerSnapshot` and named `FissionHeatDeposition` diagnostics.
- Added exact total-power closure across heat destinations using deterministic residual allocation.
- Added adapters to existing `ThermalEnergyBalance` and zero-mass `FluidNodeBalance` energy boundaries.
- Added thermal-body/fluid-node energy integration and M2.3 kinetics-to-M2.4 power fixed-step pulse-segmentation tests.
- Added ADR 0018 and `docs/THERMAL_POWER.md`.
- M2.3 local validation establishes the validated neutron-kinetics baseline carried into M2.4.

## M2.3 — Neutron Kinetics

- Added strongly validated delayed-neutron fraction, decay constant, normalized neutron population and precursor-population quantities.
- Added immutable canonical delayed-neutron group definitions, point-kinetics parameter sets and state.
- Added explicit critical-equilibrium initialization from `Λ`, `β_i` and `λ_i`.
- Added generic plant-independent `PointKineticsSolver` implementing point-reactor kinetics with arbitrary delayed-neutron groups.
- Added deterministic bounded RK4 internal substepping inside the already-fixed simulation timestep.
- Added prompt-critical margin, beta-relative dollars/cents, logarithmic population-rate and signed reactor-period diagnostics.
- Added fail-fast numerical-envelope protection for non-finite or materially negative kinetic populations.
- Added zero-reactivity equilibrium, positive/negative reactivity response, prompt-supercritical finite-envelope and runtime pulse-segmentation determinism tests.
- Added ADR 0017 and `docs/NEUTRON_KINETICS.md`.
- M2.2 local validation establishes the validated control-rod baseline carried into M2.3.

## M2.2 — Control Rods

- Added normalized `ControlRodPosition` with explicit fully-inserted/fully-withdrawn semantics.
- Added strongly validated normalized `ControlRodTravelRate`, persistent `Insert`/`Withdraw`/`Hold` motion and immutable rod state.
- Added immutable rod/group/system definitions with canonical ordering and bidirectional membership validation.
- Added deterministic individual-rod and group commands with ordered same-step override semantics.
- Added `ControlRodMotionSolver` with mechanical endpoint clamping and automatic hold at limits.
- Added linear and smooth-step integral rod-worth curves behind `ControlRodWorthSolver`.
- Added one canonical `ControlRods` reactivity contribution per rod and composition through the validated M2.1 `ReactivityModel`.
- Added domain, motion, worth, group-command, mechanical-limit and fixed-step/pulse-segmentation determinism tests.
- Added ADR 0016 and `docs/CONTROL_RODS.md`.
- M2.1 local validation establishes the validated reactivity-composition baseline carried into M2.2.

## M2.1 — Reactivity Model

- Added strongly typed signed `Reactivity` with canonical `delta-k/k` storage and explicit percent/pcm conversions.
- Added immutable named `ReactivityContribution` values with diagnostic source categories.
- Added deterministic `ReactivityModel` canonicalization, compensated summation and per-category subtotals.
- Added immutable `ReactivityBreakdownSnapshot` diagnostics and duplicate-ID fail-fast validation.
- Added unit, composition, permutation-independence, immutability and fixed-step runtime determinism tests.
- Added ADR 0015 and `docs/REACTIVITY_MODEL.md`.
- M1.7 local validation closes the complete M1 physical-foundation baseline carried into M2.1.

## M1.7 — Simplified Water/Steam Phase Model

- Added explicit `FluidPhase` classification and strongly validated saturated-mixture `VaporQuality`.
- Extended `FluidThermodynamicState` compatibly with phase, optional quality and derived vapor mass fraction.
- Added `SimplifiedWaterSteamThermodynamicModel` as the first production `IFluidThermodynamicModel` implementation.
- Added IAPWS-IF97 Region-4 saturation-pressure reference calculation with deterministic compact educational correlations for the remaining properties.
- Added deterministic closure for subcooled/compressed liquid, saturated mixtures and superheated vapor.
- Added `WaterSteamSaturationProperties` and explicit `WaterSteamStateOutOfRangeException`.
- Added phase round-trip, saturation-reference, unsupported-state, integration and determinism tests.
- Added ADR 0014 and `docs/WATER_STEAM_MODEL.md`.
- M1.6 local validation closes Heat Transfer as the validated baseline carried into M1.7.

## M1.6 — Heat Transfer

- Added strongly typed `HeatCapacity` and `ThermalConductance` with canonical SI storage.
- Added immutable lumped thermal bodies with conserved stored energy and derived temperature.
- Added stateless signed heat-transfer solving using lumped conductance and temperature difference.
- Added exactly equal-and-opposite thermal endpoint balances for internal energy conservation.
- Added deterministic thermal-body integration with below-absolute-zero fail-fast protection.
- Added explicit enabled/disabled external heat-source energy input.
- Added wall-to-fluid thermal coupling through the existing `FluidNodeBalance` energy boundary.
- Added fixed-step and pulse-segmentation determinism tests for the thermal model.
- Added ADR 0013 and `docs/HEAT_TRANSFER.md`.
- M1.5 local validation closes Pumps as the validated baseline carried into M1.6.

## M1.5 — Pumps

- Added strongly typed normalized pump speed and simplified pump efficiency.
- Added immutable pump definitions/states composed over existing hydraulic paths.
- Added speed-squared active pressure boost and quadratic internal pump-curve resistance.
- Added pressure-driven bidirectional pump flow without an imposed-flow solver.
- Added upstream-density volumetric flow and explicit hydraulic-work/shaft-power accounting.
- Added mass-conservative endpoint balances whose net energy equals active hydraulic work.
- Added stopped-pump passive-flow, reverse-flow, affinity-law and deterministic fixed-step tests.
- Added ADR 0012 and `docs/PUMPS.md`.
- M1.4 local validation closes Valves as the validated baseline carried into M1.5.

## M1.4 — Valves

- Added strongly typed normalized valve position and flow-capacity coefficient.
- Added linear, quick-opening and normalized equal-percentage valve characteristics.
- Added immutable valve definitions/states and explicit fail-safe actions.
- Added valve flow solving by resistance modulation over the existing M1.3 pipe solver.
- Added exact closed/open endpoint behaviour without infinite or magic resistances.
- Added conservative, reversal-aware and deterministic valve integration tests.
- M1.3 local validation closes Pipes & Flow Resistance as the validated baseline carried into M1.4.

## M1.3 — Pipes & Flow Resistance

- Added strongly typed `QuadraticHydraulicResistance` using canonical SI `Pa·s²/kg²`.
- Added immutable `PipeDefinition` with explicit reference endpoints and strictly positive resistance.
- Added stateless bidirectional `PipeFlowSolver` using a lumped quadratic pressure-loss relation.
- Added natural flow reversal from signed endpoint pressure difference.
- Added upstream specific-internal-energy advection and exactly equal-and-opposite endpoint balances.
- Added dimensional `SpecificEnergy × MassFlowRate -> Power` arithmetic.
- Added deterministic integration tests proving total mass/internal-energy conservation and fixed-step pulse-segmentation invariance.
- Added ADR 0010 and `docs/PIPES_AND_FLOW.md`.
- M1.2.1 local validation closes M1.2 as the validated baseline carried into M1.3.

## M1.2.1 — Thermodynamic Closure Test Hotfix

- Fixed the contradictory zero-balance expectation in `FluidNodeIntegratorTests`.
- A zero mass/energy balance still preserves conserved inventory, while thermodynamic closure remains intentionally resolved once through `IFluidThermodynamicModel`.
- The test now asserts the thermodynamic state returned by the configured closure model instead of incorrectly requiring the previous pressure/temperature to be preserved.
- No production code, public API, conservation semantics or runtime behavior changed.
- M1.2 remains the functional milestone; M1.2.1 was validated locally and closes the M1.2 baseline.

## M1.2 — Fluid Node Model

- Added immutable fluid-node domain model separating definition, conserved inventory and thermodynamic closure state.
- Added derived density and specific internal energy to avoid duplicated drifting state.
- Added signed `FluidNodeBalance` for net mass and energy rates.
- Added deterministic `FluidNodeIntegrator` with explicit integration interval.
- Added `IFluidThermodynamicModel` seam without introducing a premature production equation of state.
- Added fail-fast `FluidNodeDepletionException` when a candidate step would reach zero/negative fluid mass.
- Added Domain and Simulation tests for validity, conservation arithmetic, determinism and M0 runtime composition.
- Added ADR 0009 and `docs/FLUID_NODES.md`.
- M1.1.1 local validation also validates all carried-forward M0.2/M0.3 tests.

## M1.1.1 — Nullable Architecture Test Hotfix

- Fixed `CS8619` in `ArchitectureRulesTests.ReadProjectReferences`.
- Project-reference filename extraction now converts the nullable BCL return contract into an explicit non-null result with fail-fast diagnostics.
- No production code, physical quantity semantics, APIs or simulation behavior changed.
- M1.1 remains the functional milestone; M1.1.1 was validated locally and closes the M1.1 baseline.

## M1.1 — Physical Quantities & Units

- Added immutable strongly typed physical quantities in `Domain.Physics.Quantities`.
- Established canonical SI storage with explicit non-SI factories and conversions.
- Added geometry types: `Length`, `Area`, `Volume`.
- Added matter types: `Mass`, `Density`.
- Added thermal types: `Temperature`, `TemperatureDifference`.
- Added hydraulic types: `Pressure`, `PressureDifference`.
- Added energy types: `Energy`, `SpecificEnergy`, `Power`.
- Added flow types: `MassFlowRate`, `VolumetricFlowRate`.
- All quantity construction rejects `NaN` and infinities.
- Absolute quantities enforce non-negative physical domains where intrinsic to the dimension.
- Added explicit signed difference types for temperature and pressure.
- Added selected dimensionally meaningful arithmetic needed by upcoming physical models.
- Added Domain tests for construction, conversions, arithmetic, safety, equality and comparisons.
- Added ADR 0008 and `docs/PHYSICAL_QUANTITIES.md`.
- No fluid nodes, pumps, valves, heat transfer, phase model or reactor physics introduced yet.

## M0.3 — Simulation Test Harness & Runtime Hardening

- Added terminal `Faulted` runtime semantics and immutable fault diagnostics.
- Added transactional fixed-step commit: failed calculations do not commit logical state or clock.
- Failed-step commands are restored to the queue in original FIFO order.
- Added deterministic `ISimulationInvariant<TState>` validation before state commit.
- Added structured invariant results and invariant-violation diagnostics.
- Added immutable logical-step command traces and deterministic headless replay.
- Added a reusable generic scenario harness that captures initial and per-step snapshots.
- Added long-run 100,000-step pulse-segmentation determinism verification.
- Added large command-trace replay stress verification.
- Added fault rollback, invariant rollback and partial multi-step commit tests.
- Formalized immutable/copy-on-write state ownership for future physical kernels.
- Added ADR 0006 and ADR 0007.
- Reactor physics, fluids and thermodynamics remain intentionally out of scope until M1.

## M0.2 — Deterministic Simulation Runtime

- Added deterministic fixed-timestep `SimulationClock`.
- Added generic headless `SimulationRuntime<TState, TCommand, TStateSnapshot>`.
- Added pause, resume and paused single-step execution.
- Added exact 0.25×, 0.5×, 1×, 2×, 5× and 10× speed multipliers using fixed-point quarter-unit scaling.
- Added thread-safe FIFO command queue with monotonic sequence numbers.
- Commands are consumed only at fixed physical-step boundaries.
- Added immutable runtime and simulation snapshot envelopes.
- Added deterministic repeatability and pulse-segmentation tests.
- Added architecture enforcement against wall-clock/timer/delay APIs in Simulation.
- Added ADR 0005 for command scheduling and snapshot ownership.
- Consolidated the remaining M0 roadmap around M0.3 runtime hardening and simulation test harness.
- No reactor physics, thermodynamics or fluid modelling has been introduced yet.

## M0.1 — Engineering Foundation & Architectural Baseline — VALIDATED

- Created .NET 10 solution structure.
- Added Domain, Simulation, Application, Infrastructure and Avalonia App projects.
- Added four test projects using xUnit.net v3.
- Added centralized compiler/analyzer settings and package management.
- Isolated Avalonia dependencies to the App project.
- Added explicit project dependency rules and automated architecture tests.
- Added initial composition root.
- Added architectural documentation, ADRs and approved M0–M9 roadmap.
- No nuclear physics or plant simulation logic was introduced.
- The local validation suite was reported as passing on 2026-07-20.
