# Approved Roadmap

## M0 — Engineering foundation and deterministic runtime

### M0.1 Engineering Foundation & Architectural Baseline — VALIDATED

- solution bootstrap;
- architecture boundaries;
- compiler/analyzer baseline;
- project dependency rules;
- composition root;
- architecture tests;
- ADRs and roadmap.

### M0.2 Deterministic Simulation Runtime — VALIDATED THROUGH M1.1.1

- deterministic fixed-timestep clock;
- generic headless simulation runtime;
- pause/run/single-step;
- exact simulation-speed multipliers independent from UI cadence;
- monotonic FIFO command queue;
- immutable snapshot envelopes;
- deterministic repeatability tests;
- command/snapshot ownership rules.

### M0.3 Simulation Test Harness & Runtime Hardening — VALIDATED THROUGH M1.1.1

- reusable deterministic scenario harness;
- invariant assertion infrastructure;
- runtime fault semantics;
- command trace/replay primitives;
- stress and long-run determinism verification.

## M1 — First physical plant primitives

### M1.1 Physical Quantities & Units — VALIDATED VIA M1.1.1

- strongly typed immutable physical quantities;
- canonical SI storage and explicit conversions;
- finite-value and absolute-quantity validation;
- distinct absolute/difference temperature and pressure types;
- dimensionally meaningful arithmetic required by upcoming models;
- automated unit and safety tests.

### M1.2 Fluid Node Model — VALIDATED VIA M1.2.1

- immutable lumped fluid control-volume state;
- conserved mass and internal-energy inventory;
- derived density and specific internal energy;
- deterministic signed mass/energy balance integration;
- thermodynamic closure interface without premature equation of state;
- depletion fail-fast semantics.

### M1.3 Pipes & Flow Resistance — VALIDATED

- bidirectional passive pipe definitions;
- quadratic hydraulic resistance;
- signed pressure-driven mass flow;
- conservative internal-energy advection;
- equal-and-opposite endpoint balances;
- deterministic runtime composition.

### M1.4 Valves — VALIDATED

- strongly typed normalized valve position;
- linear, quick-opening and equal-percentage characteristics;
- resistance modulation over the existing passive pipe solver;
- exact fully-closed/fully-open semantics;
- fail-closed, fail-open and hold-last-position behaviour;
- deterministic conservative runtime composition.

### M1.5 Pumps — VALIDATED

- active pressure source composed with existing hydraulic path;
- normalized pump speed and speed-squared affinity law;
- quadratic internal pump-curve resistance;
- signed bidirectional pressure-driven flow;
- hydraulic power exchange and non-regenerative shaft demand;
- deterministic energy-accounted runtime composition.

### M1.6 Heat Transfer — VALIDATED

- strongly typed heat capacity and thermal conductance;
- lumped thermal bodies with conserved stored energy and derived temperature;
- signed temperature-driven passive heat transfer;
- exactly conservative endpoint energy balances;
- explicit external heat-source accounting;
- deterministic wall-to-fluid thermal coupling.

### M1.7 Simplified Water/Steam Phase Model — VALIDATED

- explicit subcooled-liquid, saturated-mixture and superheated-vapor states;
- saturated-mixture vapor quality;
- Region-4 saturation-pressure reference boundary;
- deterministic mass/volume/internal-energy thermodynamic closure;
- fail-fast supported-state envelope;
- production implementation behind `IFluidThermodynamicModel`.

M1 physical-foundation scope is validated and complete.

## M2 — Reactor physics

### M2.1 Reactivity Model — VALIDATED

- strongly typed signed reactivity in `delta-k/k`, percent and pcm;
- named independent reactivity contributions;
- deterministic canonical diagnostic breakdown;
- compensated total and per-category summation;
- no direct reactivity-to-power shortcut.

### M2.2 Control Rods — VALIDATED

- normalized rod withdrawal position with explicit fully-inserted/fully-withdrawn semantics;
- deterministic persistent insert/withdraw/hold motion and mechanical travel limits;
- individual-rod and group command targeting with deterministic override ordering;
- canonical rod/group system definitions and immutable operational state;
- linear and smooth-step integral worth curves;
- one explicit `ControlRods` reactivity contribution per rod;
- fixed-step/pulse-segmentation deterministic runtime integration.

### M2.3 Neutron Kinetics — VALIDATED

- generic plant-independent point kinetics;
- arbitrary canonical delayed-neutron group set;
- explicit normalized neutron and precursor populations;
- critical-equilibrium initialization;
- deterministic bounded RK4 internal substepping within the fixed simulation timestep;
- prompt-critical margin and beta-relative dollars/cents diagnostics;
- signed instantaneous reactor-period diagnostics;
- no direct neutron-population-to-thermal-power shortcut.

### M2.4 Thermal Power — VALIDATED

- explicit neutron-population-to-fission-power calibration;
- deterministic instantaneous fission thermal power;
- canonical complete heat-deposition partition;
- exact power closure across fuel/structure/coolant energy paths;
- coupling through existing thermal-body and fluid-node energy balances;
- no decay-heat or thermal-feedback shortcut.

### M2.5 Decay Heat — VALIDATED

- configurable equivalent first-order decay-heat groups;
- latent decay-energy inventory with long-operation equilibrium initialization;
- exact analytic finite-step buildup and post-shutdown decay;
- explicit precursor-production energy and emitted-decay-energy accounting;
- average same-step heat deposition plus end-of-step instantaneous diagnostics;
- canonical complete decay-heat deposition partition;
- deterministic runtime/pulse-segmentation integration.

### M2.6 Temperature Feedback — VALIDATED

- strongly typed signed temperature-reactivity coefficients;
- explicit reference-temperature linear feedback law;
- fuel-temperature and coolant-temperature named contributions;
- canonical multi-feedback composition through the M2.1 reactivity model;
- committed-state deterministic thermal-to-neutronic coupling;
- closed-loop fixed-step/pulse-segmentation verification.

### M2.7 Void Feedback — VALIDATED VIA M2.7.1

- strongly typed volumetric void fraction distinct from vapor mass quality;
- deterministic saturated-mixture quality-to-void conversion using M1.7 phase densities;
- explicit reference-void linear reactivity feedback;
- signed configurable void-reactivity coefficient;
- canonical named `Void` contributions through the M2.1 reactivity model;
- committed-state thermohydraulic-to-neutronic fixed-step coupling;
- pulse-segmentation deterministic runtime verification.

### M2.8 Iodine/Xenon Dynamics — VALIDATED

- explicit normalized I-135 and Xe-135 inventories;
- fission-power-scaled iodine and direct-xenon production;
- iodine-to-xenon feeding and independent isotope decay;
- neutron-population-dependent xenon burnup;
- analytic deterministic finite-step inventory evolution;
- equilibrium initialization for long-running operating states;
- named configurable `Xenon` reactivity contribution.

M2 reactor-physics foundation is validated and complete.

### M2.8.1 M2 Closure & Roadmap Consolidation — DOCUMENTATION BASELINE

- records M2.8 as locally validated;
- adds an explicit current capability/status map;
- formalizes staged multi-component plant orchestration before M3 implementation;
- decomposes M3–M9 into small implementation milestones and validation gates;
- changes no reactor-physics or simulation equations.

## M3 — RBMK-like primary circuit and plant composition

### M3.1 Plant Composition & Topology Baseline — VALIDATED

- immutable `PlantDefinition` and `PlantState` composition boundaries;
- canonical component/node identifiers and deterministic registries;
- topology validation for missing endpoints, duplicates and illegal references;
- plant-level immutable snapshot structure;
- no new physics: compose already validated primitives first.

Validated with canonical global topology IDs, eager hydraulic/thermal reference validation, exact state ownership and `PlantSnapshot`.

### M3.2 Deterministic Multi-Component Network Orchestration — VALIDATED

- staged committed-state gather/solve/accumulate/integrate/commit pipeline;
- every hydraulic/thermal connection reads the same committed pre-integration state;
- all balances are accumulated before any conserved inventory is integrated;
- each fluid/thermal inventory is integrated exactly once per fixed step;
- component enumeration order must not change physical results;
- plant-level mass/energy audit diagnostics and explicit closure checks.

Validated with `PlantNetworkOrchestrator`, canonical balance accumulation, exactly-once inventory integration, `PlantNetworkAudit`, order-independence tests and fixed-step runtime integration.

### M3.3 Aggregated Core-Zone Model — VALIDATED

- configurable coarse core zones and logical coordinates with no fixed grid-size assumption;
- nominal/current normalized zone power-share state;
- references to canonical fuel/structure/coolant plant domains;
- deterministic exact-closure global fission-power projection into zones;
- local committed-state thermal/hydraulic/void diagnostics;
- global point kinetics remains the neutronic dynamics model for M3; per-zone heat deposition begins in M3.4.

### M3.4 Fuel-Channel Group Model — VALIDATED

- canonical equivalent groups representing many physical channels rather than individual full-core channels;
- each group references an existing passive hydraulic pipe plus canonical zone fuel/structure/outlet-coolant domains;
- per-zone group power shares close exactly and represented channel counts are explicit;
- fission and optional decay heat are partitioned deterministically and emitted as staged fuel/structure/coolant source terms;
- per-group committed-state diagnostics include flow, pressure difference, temperatures, phase, quality, void and per-channel power/flow;
- `PlantNetworkOrchestrator` accepts supplemental physical source terms before its single integration phase and audits their external power explicitly.

### M3.5 Main Circulation System — VALIDATED VIA M3.5.1

- canonical suction/pressure header nodes and passive return-path composition;
- main circulation pumps composed from the validated M1.5 pump primitive;
- every M3.4 channel group assigned to exactly one circulation loop;
- committed-state pump/channel/return flow and continuity diagnostics;
- pump hydraulic/shaft power and outlet phase/quality/void diagnostics;
- no second hydraulic integrator: M3.2 remains the only state-evolution boundary;
- pump trip/coastdown seam reserved without yet implementing full electrical/control logic.

### M3.6 Steam Drums, Separation & Recirculation — VALIDATED

- dedicated return-collector/drum inventory node per circulation loop;
- deterministic committed-state ideal phase separation for liquid, saturated mixture and vapor;
- conservative staged steam transfer to a canonical steam-outlet node and liquid recirculation to MCP suction;
- drum phase, quality, void and normalized liquid-level diagnostics;
- plant-network mass/energy audit remains closed because separation is an internal source-term transfer;
- external steam sink and feedwater source are outside M3.6 and are introduced by M3.7.

### M3.7 Feedwater & Steam Boundary Interfaces — VALIDATED

- explicit feedwater source boundary with mass, energy and controllable flow inputs;
- explicit steam sink/export boundary for pre-M4 operation;
- boundary accounting that keeps plant mass/energy audits closed and observable;
- interfaces designed so M4 can replace simplified boundaries with the turbine island without rewriting M3.

### M3.8 Integrated Primary-Circuit Baseline — VALIDATED

- canonical top-level definition composing core zones, channel groups, circulation loops, drums, feedwater and steam boundaries;
- one committed plant state for every subsystem solve and exactly one conserved-inventory integration boundary;
- configurable fixed-input reference operating point with no hidden controllers or corrective bookkeeping;
- deterministic headless long-run execution with raw mass/energy drift and maximum conservation-residual reporting;
- global mass/energy audit plus inherited order-independent staged source-term composition;
- first integrated plant-level snapshot exposing global and per-subsystem diagnostics for later instrumentation/UI.

**M3 gate — COMPLETE:** the validated M3.8 baseline satisfies deterministic long-run execution, bounded drift, closed accounting, order-independent staged composition and plant-level snapshot requirements.

## M4 — Turbine island and electrical power conversion

### M4.1 Main Steam Network & Turbine Admission — VALIDATED

- canonical steam lines/headers mapped from every M3 steam-export seam;
- exact stop/control/admission valve trains using validated M1 valve primitives;
- legacy M3 steam-export sink disabled while M4 owns downstream transport;
- replaceable turbine-admission boundary with explicit signed mass/energy accounting;
- conservative internal transport integrated once through the existing plant-network orchestrator;
- committed-state line, valve, continuity and turbine-inlet diagnostics.

### M4.2 Turbine Rotor & Expansion Model — VALIDATED

- canonical lumped stage groups bound one-to-one to M4.1 turbine-admission seams and canonical exhaust nodes;
- conservative inlet-to-exhaust steam mass transfer with explicit residual exhaust energy;
- steam-energy extraction to audited mechanical shaft power;
- separate immutable rotor state with inertia, speed, turbine/load/net torque and kinetic energy;
- deterministic torque integration including zero-speed startup and load limiting against numerical reversal;
- explicit manual external-load seam ready for later generator electromagnetic torque;
- overspeed indication and explicit trip-command seams without automatic protection logic/latching;
- inherited single plant-network integration for M3 + M4.1 + M4.2 thermofluid inventories plus separate mechanical energy audit.

### M4.3 Condenser, Vacuum & Hotwell — VALIDATED

- canonical one-to-one binding from every M4.2 turbine exhaust seam to a lumped condenser and hotwell;
- conservative steam-space-to-hotwell condensation with explicit external heat-rejection accounting;
- condensation limited by configured capacity, committed vapor inventory and replaceable cooling-boundary capacity;
- condenser absolute pressure/vacuum dynamics derived from canonical exhaust-node conserved inventory and thermodynamic closure;
- hotwell condensate inventory diagnostics;
- inherited single plant-network integration for M3 + M4.1 + M4.2 + M4.3 thermofluid inventories.

### M4.4 Condensate & Feedwater Train — VALIDATED

- canonical hotwell-to-drum return topology using existing condensate/feedwater `PumpDefinition` components;
- one M4.4 train per legacy M3 feedwater seam with eager endpoint/ownership validation;
- canonical feedwater inventory node and deterministic pump flow/pressure/power diagnostics;
- bounded educational thermal-conditioning input with explicit external-energy accounting;
- legacy M3 feedwater mass sources required to zero while M4.4 owns condensate return;
- inherited single thermofluid integration for M3 + M4.1 + M4.2 + M4.3 + M4.4.

### M4.5 Generator, Grid & Synchronization Physics — VALIDATED

- exact one-generator-per-M4.2-rotor topology over an explicit infinite-bus grid boundary;
- strongly typed electrical frequency, line-voltage and phase-angle quantities;
- deterministic generator/grid phase state and rotor-speed-derived electrical frequency diagnostics;
- manual breaker close/open state with explicit frequency/phase/voltage synchronization acceptance windows;
- legacy M4.2 manual load torque required to zero while M4.5 owns electromagnetic rotor loading;
- shaft-to-electrical conversion with explicit generator losses and electrical audit;
- electrical load torque fed through the existing single M4.2 rotor integrator;
- automatic excitation, synchronization, governing and protection deferred to M5.

### M4.6 Integrated Secondary-Cycle Heat Balance — VALIDATED

- canonical top-level composition over the complete validated M4.5 reactor-to-grid stack without new mutable physical state;
- closed steam/condensate/feedwater mass boundary surfaced from the authoritative plant-network audit;
- unified first-law reconciliation across thermofluid stored energy, rotor kinetic energy and generator electrical conversion;
- explicit nuclear heat, pump hydraulic power, feedwater conditioning, condenser rejection, turbine shaft, electrical export and generator-loss diagnostics;
- raw supplemental-power classification plus shaft-transfer and mechanical-to-electrical reconciliation residuals;
- deterministic repeated-step coupled verification with the M3 primary circuit and no hidden corrective bookkeeping.

### M4.7 Full-Plant Steady-State Baseline — VALIDATED

- canonical `FullPlantState` envelope over thermofluid, rotor and electrical state ownership;
- top-level `FullPlantSolver` / `FullPlantSnapshot` boundary without a new physical integrator;
- configurable fixed-input reference operating condition and explicit steady-state acceptance criteria;
- deterministic 1,000-step long-run drift verification without hidden correction;
- mass, coupled stored-energy, rotor-speed, electrical-output and first-law closure drift metrics;
- gross reactor-to-grid efficiency, heat-rate and heat-rejection diagnostics derived from audited power paths;
- plant-level true-state snapshot boundary ready for M5 instrumentation and automatic control.

**M4 gate:** do not build automatic plant control until the manually commanded full plant is dynamically stable, energy-accounted and testable headlessly.

## M5 — Instrumentation, control and protection

### M5.1 Instrumentation & Signal Model — VALIDATED

- measured signals separated from true plant state through a controller-facing `MeasuredSignalFrame`;
- canonical signal-source catalog over `FullPlantSnapshot` with stable semantic source ids;
- finite measurement ranges, linear scaling, deterministic first-order lag/filter state and explicit validity/quality;
- deterministic bias/freeze/failed-low/failed-high/unavailable sensor-fault seams for later scenario scheduling;
- instrumented full-plant composition that preserves M4.7 physical ownership and adds observation state only.

### M5.2 Controller & Actuator Primitives — VALIDATED

- P/PI/PID controllers with deterministic integration;
- manual/auto modes, limits, anti-windup and bumpless transfer where applicable;
- actuator command/state boundaries over valves, pumps and rods.

### M5.3 Reactor & Primary-System Control Loops — VALIDATED

- measured reactor-power regulation through M5.2 controllers and canonical M2 rod/group command, rod-worth, point-kinetics and fission-power physics;
- explicit non-rod-reactivity seam preserving the validated temperature/void/xenon/manual ownership boundaries;
- main-circulation pump support loops using measured total pump flow or header pressure rise and canonical `PumpState` command ownership;
- committed-state ordering: current rods drive current kinetics, while issued commands advance the next rod state;
- explicit setpoint, measurement-quality, controller-output, saturation, actuator-target, rod-reactivity and fission-power diagnostics;
- drum/feedwater-level and turbine/steam automatic loops intentionally deferred to M5.4.

### M5.4 Turbine, Steam & Feedwater Control Loops — VALIDATED

- turbine speed or generator-load admission governing through measured signals and canonical M4.1 control/admission valves;
- source-drum steam-pressure admission control with stop valves reserved for M5.5 isolation/trip logic;
- steam-drum level control through canonical M4.4 feedwater-pump operating commands;
- condenser hotwell-inventory support through canonical condensate-pump operating commands and a stable measured hotwell-mass source;
- M4.2 stage-group steam demand derived from the limiting projected flow through the commanded canonical stop/control/admission path, without a second hydraulic integration;
- integrated M5.3 + M5.4 composition over one measured frame, disjoint physical actuator ownership and one M4.7 full-plant physical step.

### M5.5 Interlocks, Trips & SCRAM — VALIDATED

- deterministic measured-signal trip functions with explicit high/low thresholds, reset hysteresis and fail-closed invalid-measurement policy;
- latched reactor SCRAM, turbine trip and generator trip actions with explicit manual trip seams and reset permissives;
- non-latching rod-withdrawal, turbine-admission-opening and generator-breaker-close interlocks;
- protection-over-normal-control arbitration applied to canonical M2 rods, M4.1 stop valves, M4.2 turbine trip seam and M4.5 generator breaker commands;
- one shared M5.1 measured frame and one authoritative M4.7 physical step;
- protection action/state diagnostics remain separate from M5.6 alarm/annunciator presentation.

### M5.6 Alarms & Annunciator State — VALIDATED

- alarm conditions over canonical M5.1 measured signals plus observational M5.5 protection state;
- explicit non-latching and latched-until-reset semantics with acknowledgement separated from physical protection reset;
- deterministic first-out grouping and monotonic logical event ordering for activation/clear/acknowledge/reset;
- immutable alarm, first-out and event snapshots for M6 annunciator views and recorder/timeline integration;
- alarm processing remains observational and cannot trigger or reset SCRAM, turbine trip, generator trip or interlocks.

### M5.7 Integrated Automatic-Operation Baseline — VALIDATED

- canonical M5.1–M5.6 multi-step automatic-operation state/input/snapshot composition;
- committed measured-frame ordering with candidate-state instrumentation published only for the next logical step;
- stable automatic-control verification around explicit reference phases;
- explicit setpoint-change and disturbance-input phases without a hidden scenario scheduler;
- deterministic protection/interlock expectation matrix through the same protected runtime path;
- measured tracking plus raw mass/energy closure, signal-validity and annunciator gate metrics;
- M5 gate complete; next: M6.1 control-room application shell.

## M6 — Operator control room

### M6.1 Control-Room Application Shell — VALIDATED

- stable Overview, Reactor, Primary, Turbine/Secondary, Electrical and Alarms/Events workspace navigation;
- narrow `ControlRoomSnapshot` presentation contract and snapshot-driven Avalonia view models;
- typed application command dispatch without UI physics or deterministic-time ownership;
- Avalonia project no longer directly references Simulation namespaces/project;
- scalable desktop layout and explicit presentation-only performance budget;
- validated scalable presentation shell and command boundary; next: M6.2 reusable instrument & control components.

### M6.2 Reusable Instrument & Control Components — VALIDATED

- reusable numeric indicators, linear meters, lamps, toggle switches, selectors and pushbuttons;
- shared semantic `Normal` / `Warning` / `Trip` / `Unavailable` presentation states;
- stable Application-layer component/interaction catalog without Avalonia coupling;
- predictable focus, keyboard and pointer semantics for simulator operation;
- validated reusable component vocabulary and shell gallery; next: M6.3 Reactor/Core Panel.

### M6.3 Reactor/Core Panel — VALIDATED

- measured reactor thermal-power indication plus explicitly labelled kinetics/reactivity model diagnostics;
- coarse aggregated core-zone map with fission power, fuel/coolant temperature and void diagnostics;
- canonical rod target/state presentation with insert/hold/withdraw operator command intents;
- reactor SCRAM, protection-reset and rod-withdrawal-interlock presentation/command seams;
- missing M2.8 xenon operational state shown explicitly unavailable rather than synthesized;
- validated first domain-specific reactor/core workspace; next: M6.4 Primary-Circuit Mnemonics.

### M6.4 Primary-Circuit Mnemonics — VALIDATED

- topology-aware circulation loops, MCPs, headers, channel groups and steam drums;
- measured loop flow/header ΔP and drum pressure/level with explicit model diagnostics for non-instrumented values;
- flow direction plus canonical primary-connected valve mechanical-state presentation;
- typed MCP operator intents routed only through Application command boundaries;
- validated topology-aware primary process mnemonic and MCP command seam; next: M6.5 Turbine, Generator & Electrical Panels.

### M6.5 Turbine, Generator & Electrical Panels — VALIDATED

- turbine/condenser/feedwater mnemonics with measured-versus-model diagnostic separation;
- measured rotor/condenser/generator instrumentation plus canonical M4 diagnostic presentation;
- turbine-speed and generator-load operator intent seams;
- synchronization-gated breaker close/open plus turbine/generator trip command intents;
- validated turbine/secondary and electrical operating panels with typed speed/load/breaker/trip intents; next: M6.6 Trends, Alarms & Event Timeline.

### M6.6 Trends, Alarms & Event Timeline — VALIDATED

- configurable bounded time trends from immutable presentation snapshots and logical steps;
- M5.6 annunciator, acknowledgement/reset and first-out presentation without protection ownership drift;
- deterministic event timeline ordered by validated monotonic alarm-event sequence;
- validated deterministic trends, annunciator/first-out and event timeline; next: M6.7 Control-Room Integration & Performance Baseline.

### M6.7 Control-Room Integration & Performance Baseline — VALIDATED

- complete operator path for normal plant operation through the validated M5.7 runtime;
- bounded cooperative accelerated execution with sparse presentation publication;
- rendering/publication cadence proven observational only;
- local build and complete tests explicitly confirmed successful on 2026-07-21; M6 gate complete; next: M7.1 Versioned Initial Conditions & Scenario Framework.

**M6 gate:** COMPLETE / VALIDATED through M6.7.

## M7 — Operations, initial conditions and training flow

### M7.1 Versioned Initial Conditions & Scenario Framework — VALIDATED

- exact-version initial-condition identity, descriptors, factories and registry with no silent latest-version fallback;
- versioned JSON scenario schema with deterministic v0 → v1 migration preserving exact initial-condition identity/version;
- scenario metadata, objectives and explicit fail-closed allowed operator actions;
- deterministic fresh-session load/start boundary, always paused after load;
- logical-step replay reusing the M0 command-trace primitive;
- local build and complete tests explicitly confirmed successful on 2026-07-21; M7.1 is the validated baseline for M7.2.

### M7.2 Cold Shutdown & Pre-Startup — VALIDATED

- exact-version built-in `cold-shutdown-pre-start` v1 recipe reconstructed through canonical M1–M5 owners and the validated water/steam closure;
- cold, subcritical baseline with rods inserted, modeled pumps stopped, steam-admission path closed, turbine stopped and generator breaker open;
- presentation-only readiness checklist for instrumentation, protection, shutdown, rods, circulation, turbine, breaker, steam isolation and annunciator state;
- declarative ordered pre-start guidance with suggested operator actions but no automatic command dispatch or state forcing;
- fail-closed scenario permissions deliberately exclude rod withdrawal and breaker closure so first criticality remains M7.3 ownership;
- desktop composition loads the exact v1 operational session paused through the M7.1 registry/session boundary;
- local build and complete tests explicitly confirmed successful on 2026-07-21; M7.2 hotfix 1 is the validated baseline for M7.3.

### M7.3 First Criticality & Low-Power Operation — VALIDATED

- exact-version `pre-criticality-source-range` v1 handoff reusing the validated M7.2 construction path with established main circulation and a tiny deterministic non-zero kinetics seed;
- controlled rod INSERT/HOLD/WITHDRAW through the validated M5.3 command seam, with fail-closed exclusion of turbine acceleration, breaker close and generator loading;
- observational approach-to-criticality, criticality, low-power-band and reactor-period checks over immutable `ControlRoomSnapshot`;
- explicit modeling boundary: the source-range seed is initial-condition data, not a hidden external-neutron-source solver;
- explicit xenon training boundary: quantitative xenon remains unavailable until canonical M2.8 state is promoted into the M5.7 operational envelope;
- local build and complete tests explicitly confirmed successful on 2026-07-21; M7.3 is the validated baseline for M7.4.

### M7.4 Heat-Up, Steam Raising & Turbine Startup — VALIDATED

- exact-version `low-power-steam-raising` v1 warm critical handoff reusing the canonical M7.2 construction path;
- versioned startup steam lineup with stop/admission availability and governing control valve initially closed, without creating a second stop-valve command owner;
- observational heat-up, steam-drum pressure/inventory and turbine-speed checks over immutable `ControlRoomSnapshot`;
- turbine roll/acceleration through the validated M5.4 `TurbineSpeedRaise/Lower` controller seam only;
- fail-closed scenario permissions continue to reject generator-breaker close and generator-load raise/lower;
- desktop composition validated with the exact M7.4 session paused; local build and complete tests passed for hotfix 1 on 2026-07-21.

### M7.5 Grid Synchronization & Load Increase — VALIDATED

- exact-version `pre-synchronization-grid-loading` v1 with canonical 3000 rpm/phase-matched breaker-open handoff;
- synchronization procedure observes the existing M4.5 frequency/phase/voltage close-check and never fabricates a permissive;
- breaker closure remains a one-step canonical M4.5 command;
- generator load raise/lower updates only bounded canonical requested electrical power in 5 MWe increments;
- coordinated reactor/turbine/electrical low-load guidance remains observational and uses validated rod and speed-governor seams;
- desktop composition validated with the exact M7.5 session paused; local build and complete tests passed on 2026-07-21.

### M7.6 Power Manoeuvring & Normal Shutdown — VALIDATED

- exact-version `stable-low-load-parallel-operation` v1 with canonical breaker-closed 5 MWe low-load handoff;
- bounded generator-load raise/lower through existing M4.5 requested electrical power only;
- coordinated reactor/turbine/electrical manoeuvring through validated M2/M5.3, M5.4 and M4.5 command seams;
- observational fuel/coolant temperature and void diagnostics, while quantitative xenon remains explicitly unavailable at the current M5.7 operational snapshot boundary;
- controlled normal-shutdown sequence: unload, breaker open, rod insertion, turbine rundown and post-shutdown main circulation;
- desktop composition validated with the exact M7.6 session paused; local build and complete tests passed on 2026-07-21.

### M7.7 Training Objectives, Procedure Guidance & Evaluation — VALIDATED

- deterministic accepted-operator-action journal at the scenario command boundary; host run/pause/step and rejected commands are excluded;
- historical first-achievement checkpoints observed on every deterministic fixed step, independent of sparse UI publication stride;
- generic criteria over checkpoint achievement and ordered accepted-action history mapped to declared scenario objectives;
- optional Hidden / ChecklistOnly / Guided assistance modes separated completely from physics and score semantics;
- 100-point integrated normal-operations capstone over validated `stable-low-load-parallel-operation` v1;
- emergency/protection actions remain physically available while their inappropriate routine use can be scored as a training deviation;
- local build and complete tests explicitly confirmed successful on 2026-07-21; M7 gate complete.

## M8 — Faults, transients and safety scenarios

### M8.1 Deterministic Fault-Injection Framework — VALIDATED

- faults are explicit immutable versioned-scenario inputs with stable fault/type/target IDs and deterministic parameters; never hidden randomness;
- activation/deactivation scheduling occurs only at committed boundaries by exact logical step or named committed-snapshot plant condition;
- fail-closed exact-ID binding for runtime fault applicators and plant-condition evaluators;
- deterministic single-pass `Pending → Active → Cleared` lifecycle with logical-step stamps and monotonic transition sequence;
- fault lifecycle state is projected into `ControlRoomSnapshot` and reconstructed by normal M7.1 replay from the same scenario definition;
- scenario JSON schema v2 persists fault schedules while v0/v1 migration preserves exact initial-condition identity and invents no faults;
- concrete hydraulic/instrumentation/control/transient fault effects remain M8.2+ ownership;
- local build and complete tests explicitly confirmed successful on 2026-07-21.

### M8.2 Hydraulic Component Faults — VALIDATED / HOTFIX 2

- pump trip and deterministic capacity degradation through canonical `PumpState` constraints before the existing pump solver;
- valve fail-open/fail-closed/stuck-at-activation-position through canonical `ValveState`;
- blocked/restricted **valve-controlled** paths via deterministic opening clamps, without a second topology/resistance owner;
- selected bounded fluid-node leaks as signed mass + carried-energy `PlantNetworkSourceTerms` integrated exactly once by `PlantNetworkOrchestrator`;
- built-in fail-closed hydraulic applicator registration and deterministic demonstration scenario pack;
- arbitrary raw-pipe break/resistance mutation remains M8.5 rather than being hidden inside M8.2.
- hotfix 2 adds presentation regression hardening and a headless App test project; local build and complete tests explicitly passed; it does not broaden M8.2 physical scope.

### M8.3 Instrumentation & Control Faults — VALIDATED

- deterministic M5.1 sensor bias/freeze/failed-low/failed-high/unavailable applicators with exact canonical channel binding;
- controller-output freeze/fail-low/fail-high as temporary bounded canonical `ControllerInput` overlays;
- actuator-command freeze/fail-low/fail-high with fail-closed one-controller/one-actuator target resolution;
- protection/interlock diagnostics remain causal consequences of the same committed faulted `MeasuredSignalFrame`; protection state is never injected directly;
- built-in demonstration and protection fail-safe diagnostic scenario definitions;
- local build and complete tests explicitly passed; M8.3 is validated.

### M8.4 Turbine/Generator/Feedwater/Condenser Transients — BASELINE CANDIDATE

- turbine trip through canonical M5.5 protection and M4 steam/rotor ownership;
- generator trip/load rejection through canonical M5.5/M4.5 breaker and electromagnetic-loading ownership;
- feedwater loss/degradation by composing validated M8.2 pump-fault effects;
- condenser-vacuum degradation/loss by reducing only canonical M4.3 cooling-boundary heat-rejection capacity;
- dedicated versioned transient-ready initial condition and four deterministic scenario definitions;
- local build and complete tests required before validation.

### M8.5 Educational Leak/LOCA-Class Scenarios

- simplified break/leak boundary models within the validated thermal-hydraulic envelope;
- inventory loss, depressurization and heat-removal consequences;
- explicit model limitations documented per scenario.

### M8.6 Electrical Loss & Station Blackout-Class Scenarios

- loss of external electrical supply;
- pump/control availability consequences;
- decay-heat removal challenge using the plant systems actually modeled.

### M8.7 Safety-Response Scenario Pack

- deterministic scenario definitions and acceptance criteria;
- protection/control response verification;
- operator-action timeline capture.

## M9 — Advanced analysis, fidelity and historical-inspired scenarios

### M9.1 Recorder, Checkpoints & Full Replay

- richer plant-state/event recording above the M0 logical command trace;
- versioned checkpoints;
- deterministic seek/replay support.

### M9.2 Post-Incident Analysis

- synchronized trends, alarms, commands and automatic actions;
- causal timeline views;
- conservation/fault diagnostics for training debriefs.

### M9.3 Advanced Xenon & Low-Power Transients

- restart-after-shutdown xenon scenarios;
- low-power manoeuvring and poisoning challenges;
- only phenomena supported by the validated model are exposed as scenarios.

### M9.4 Spatial/Quasi-Spatial Fidelity Refinement

- refine zone coupling, power distribution and feedback weighting;
- optional higher-resolution core aggregation;
- preserve the global point-kinetics seam unless a separately validated spatial kinetics model is introduced.

### M9.5 Historical-Inspired Scenario Framework

- historical-inspired initial conditions/procedures only after model-fidelity review;
- explicit separation between documented facts, educational approximation and simulator-specific assumptions;
- no scripted outcome that bypasses physics.

### M9.6 Calibration & Reference Validation Suite

- curated steady-state and transient reference cases;
- tolerance budgets and model-version tracking;
- sensitivity/regression reports for configurable plant parameters.

### M9.7 Release Hardening

- save/scenario migration hardening;
- performance and memory budgets;
- packaging/publish pipeline;
- user/developer documentation and known-model-limitations register.

## Cross-phase acceptance gates

Every implementation milestone must continue to satisfy:

- warnings-as-errors build;
- complete automated test suite;
- deterministic fixed-step/pulse-segmentation verification for new dynamic models;
- explicit conservation/invariant tests where mass or energy is involved;
- immutable snapshots and no UI-owned physics;
- configuration-driven plant constants rather than hidden RBMK constants in the generic engine;
- ADR/documentation update when a durable architecture decision changes;
- local user validation before a baseline is marked validated.

