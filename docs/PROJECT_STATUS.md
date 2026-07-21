# Project Status

## Validated baseline

The current validated functional baseline is **M8.1 — Deterministic Fault-Injection Framework** hotfix 1.

M0, M1, M2, the complete M3 phase, M4.1 through M4.7, M5.1 through M5.7, M6.1–M6.7, M7.1–M7.7 and M8.1 are validated through local build/test execution/approval. The M3, M4, M5, M6 and M7 gates are complete. M8.2 hotfix 1 is the current baseline candidate.

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
| M8 | IN PROGRESS | M8.1 validated; M8.2 Hydraulic Component Faults baseline candidate |
| M9 | PLANNED | advanced analysis, fidelity refinement and historical-inspired scenarios |

## Validated M8.1 / current M8.2 candidate

M7.1 through M7.7 are validated and the M7 gate is complete. M8.1 is now validated and owns deterministic fault declaration/scheduling/lifecycle orchestration. M8.2 is the current candidate for concrete pump/valve/valve-controlled-path constraints plus selected audited node-leak effects through canonical runtime seams.

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
- M8.2 candidate concrete pump trip/degradation, valve fail/stuck, valve-controlled path restriction/blockage and selected audited fluid-node leaks;

## Current implementation candidate

**M7.7 — Training Objectives, Procedure Guidance & Evaluation** is validated and closes the M7 gate over the exact-version scenario/session/replay boundary plus validated operating procedures and observational training evaluation.

**M8.1 — Deterministic Fault-Injection Framework** is validated.

**M8.2 — Hydraulic Component Faults hotfix 1** is the current baseline candidate. It adds runtime-bound hydraulic fault effects over canonical pump/valve state and selected node leaks through the existing plant-network source-term boundary, without a second hydraulic solver or inventory owner.

**Restart note:** M8.1 is explicitly validated. M8.2 remains a baseline candidate until local build and the complete test suite are explicitly confirmed. See `PROJECT_HANDOFF.md` and `NEW_CHAT_START.md`.

The current hotfix also adds the first dedicated headless `NuclearReactorSimulator.App.Tests` coverage for `MainWindowViewModel` and XAML command-state wiring; this is presentation regression hardening only and does not change M8.2 hydraulic fault semantics.

## What is intentionally not built yet

The following are planned architecture boundaries, not missing bugs:

- no detailed feedwater-heater cascade, extraction drains or deaerator chemistry yet;
- no detailed three-element drum-level control, feedwater-valve cascade or heater/deaerator control yet; the first canonical drum/hotwell pump loops are provided by the validated M5.4 baseline;
- no detailed condenser tube-bundle/circulating-water hydraulics, non-condensable gas or ejector model;
- no detailed HP/IP/LP turbine maps, moisture separation/reheat or wet-steam erosion model;
- no detailed synchronous-machine transient/reactance model, AVR/excitation dynamics or grid load-flow physics;
- no persistent disk historian or wall-clock event timestamps; M6.6 history is bounded presentation state indexed only by logical step/event sequence;
- M7.1–M7.7 are validated and the M7 gate is complete; M8.1 fault orchestration is validated; M8.2 currently owns concrete hydraulic component constraints and selected bounded leaks, while instrumentation/control/transient effects remain M8.3+;
- no arbitrary full-state checkpoint/save/seek format yet; M9.1 owns that boundary;
- no full-scope or licensing-grade thermal hydraulics/neutronics.

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

### Gate after M8

Fault and transient scenarios are defined as explicit deterministic fault inputs plus commands over the physical plant. Scenario scripts must never force a predetermined physical outcome.

## Baseline discipline

A milestone becomes validated only after:

- local build succeeds with warnings as errors;
- the complete automated suite passes;
- relevant deterministic/conservation/invariant tests pass;
- documentation reflects the implemented behavior;
- user validation is explicitly recorded.

## Current M8.2 candidate

M8.1 Deterministic Fault-Injection Framework is locally validated. M8.2 adds concrete deterministic hydraulic component effects through typed runtime seams: pump trip/degradation, valve fail/stuck behavior, valve-controlled path restriction/blockage and selected signed mass/energy leaks. Canonical M3/M4/M5 owners remain authoritative and `PlantNetworkOrchestrator` remains the sole inventory integrator.
