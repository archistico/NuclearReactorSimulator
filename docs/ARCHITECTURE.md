# Architecture

## Purpose and authority

Nuclear Reactor Simulator is an educational full-plant simulator. The architecture is designed so that physical models can become richer without coupling deterministic simulation mathematics to Avalonia, persistence formats, wall-clock timing or presentation-only gameplay state.

This document describes **stable layer boundaries and current subsystem ownership by topic**. It deliberately does not carry the active milestone, candidate or validation checkpoint. For current state use [`PROJECT.md`](PROJECT.md). Historical milestone-by-milestone architecture additions are preserved in [`history/ARCHITECTURE_MILESTONE_LEDGER.md`](history/ARCHITECTURE_MILESTONE_LEDGER.md).

The core rule is ownership before convenience: every quantity, command, transition, score, replay artifact and UI state must have one authoritative owner. Projection layers may explain or aggregate that truth but must not create a second physical or control authority.

## Production projects

### `NuclearReactorSimulator.Domain`

Owns stable problem-space definitions and immutable/value-oriented contracts:

- strongly typed SI physical quantities;
- canonical plant topology and definition registries;
- fluid-node, pipe, valve, pump, thermal-body and turbine/electrical definitions;
- reactor/core, control-rod, kinetics, decay-heat, iodine/xenon and feedback definitions;
- controller/actuator and protection/alarm definitions;
- exact canonical identity rules for plant definitions and state membership.

Domain performs construction-time validation but owns no numerical integration, scenario orchestration, persistence adapter or Avalonia presentation.

Rules:

- no Avalonia references;
- no filesystem/database dependencies;
- no Infrastructure dependency;
- no wall-clock timing;
- no application workflow orchestration.

### `NuclearReactorSimulator.Simulation`

Owns deterministic runtime state evolution and physical/numerical models:

- fixed 10 ms deterministic stepping;
- transactional candidate-state calculation and commit;
- plant-network balance accumulation and exactly-once conserved-inventory integration;
- fluid/steam thermodynamic closure;
- passive hydraulic flow, valve/pump flow and secondary-cycle composition;
- reactor kinetics, control-rod motion, fission/thermal power, decay heat, xenon and feedback;
- turbine/rotor, condenser/feedwater and generator/grid dynamics;
- instrumentation, controllers, actuators, protection/interlocks and alarm logical state;
- deterministic invariant checking and failure semantics;
- qualified hydraulic/thermodynamic corrected-commit path and numerical diagnostics.

Simulation owns neither Avalonia UI nor scenario/challenge authoring semantics. Wall-clock performance instrumentation may observe cost through architecture-safe seams but must not enter a numerical decision.

Allowed production dependency: `Domain`.

### `NuclearReactorSimulator.Application`

Owns operator-facing application contracts and deterministic orchestration above Simulation:

- immutable `ControlRoomSnapshot` presentation projection;
- typed operator commands and dispatch boundaries;
- runtime coordinator for run/pause/single-step and sparse presentation publication;
- exact-version initial conditions and scenario/session composition;
- deterministic fault orchestration through typed subsystem seams;
- scenario recording, checkpoints, full replay/seek and post-incident evidence;
- contextual command-consequence/dependency/observed-response projections;
- operational challenge lifecycle, external energy demand, scoring and pack composition;
- challenge replay/checkpoint reconstruction from canonical recordings;
- Mission/Performance read models, live evidence aggregation and presentation-only navigation contracts.

Application may aggregate truth from validated owners, but may not implement new reactor physics, bypass Simulation control/protection ownership or let scoring/challenge state command the plant.

Allowed production dependencies: `Domain`, `Simulation`.

### `NuclearReactorSimulator.Infrastructure`

Owns persistence and external technical adapters:

- scenario-definition JSON;
- checkpoint JSON;
- session-archive JSON;
- post-incident-analysis JSON;
- format-version/error-boundary behavior and DTO ownership.

Infrastructure persists Application/Domain contracts but owns no simulation physics. Persistence errors are normalized at adapter boundaries; unsupported future schema versions remain distinct from malformed data.

Allowed production dependencies: `Domain`, `Application`.

### `NuclearReactorSimulator.App`

Avalonia presentation/composition layer:

- desktop application composition root;
- Views, ViewModels and custom visual controls;
- workspace selection and keyboard/HMI interaction;
- translation of gestures into typed Application commands;
- rendering of immutable Application presentation state;
- desktop session save/load user interaction.

App may not reference Simulation directly, own plant physics or create command authority not exposed by Application.

## Dependency graph

```text
Domain
  ↑
Simulation
  ↑
Application
  ↑
App

Infrastructure → Application
Infrastructure → Domain
App            → Application
App            → Infrastructure
```

Lower layers never reference App. Infrastructure is an adapter beside App, not an alternative physical owner.

## Deterministic runtime and transactional state boundary

One simulation step is conceptually:

```text
committed state
    ↓
commands / canonical control & protection inputs
    ↓
component solvers read the same committed state
    ↓
balances / candidate subsystem results
    ↓
conserved inventories integrated exactly once
    ↓
thermodynamic closure + invariants
    ↓
commit candidate state
    ↓
immutable snapshot publication
```

A failed candidate step must not partially commit time or plant state. Runtime faults remain explicit and deterministic. Desktop pacing is an App/Application concern; physical time is the fixed logical step, not elapsed wall-clock time.

See [`PLANT_NETWORK_ORCHESTRATION.md`](PLANT_NETWORK_ORCHESTRATION.md), [`SIMULATION_NUMERICAL_REGULARITY_AND_RUNTIME_REVIEW.md`](SIMULATION_NUMERICAL_REGULARITY_AND_RUNTIME_REVIEW.md) and ADRs 0002, 0006 and 0183.

## Physical quantity and canonical-topology boundary

Physical API boundaries use strongly typed quantities with SI internals. Primitive numeric arrays are allowed inside numerical kernels, but units must be restored at solver boundaries.

`PlantDefinition` owns validated immutable topology and canonical definition identity. `PlantState` contains the exact state set corresponding to that topology and requires canonical definition references rather than structurally equal substitutes. Immutable topology indices may accelerate lookup but do not change canonical ordering.

See [`PHYSICAL_QUANTITIES.md`](PHYSICAL_QUANTITIES.md), [`PLANT_COMPOSITION.md`](PLANT_COMPOSITION.md), [`DOMAIN_DEFINITION_INVARIANT_CLOSURE.md`](DOMAIN_DEFINITION_INVARIANT_CLOSURE.md), ADR 0008 and ADR 0179.

## Fluid, hydraulic and thermodynamic ownership

### Fluid nodes and conservative transport

Fluid-node state separates fixed geometry, conserved inventory and derived thermodynamic closure. Network components return signed mass/energy balances; `PlantNetworkOrchestrator` integrates conserved inventories once.

Passive pipe direction defines a sign convention, not a one-way ownership rule. The reduced law is quadratic and bidirectional where the component permits reverse flow.

### Pumps, valves and one-way behavior

Pump and valve definitions belong to Domain; their solved flow and hydraulic power belong to Simulation. One-way behavior, such as the ideal discharge check, is component-owned and must not be recreated in Application or UI logic.

Near-zero quadratic-flow regularity, ideal check-valve non-smoothness and near-closed valve conditioning are known numerical/modeling questions. They are intentionally deferred to the dedicated M12 evidence gate rather than changed opportunistically during presentation work.

### Water/steam branch semantics

The base simplified inverse water/steam resolver is **memoryless**: coordinates determine the selected branch without using previous state as production memory.

The qualified four-node corrected path may wrap that inverse map with bounded previous-phase continuity/hysteresis. When the corrected path is triggered, eligible and authorized, its candidate can become committed state. Therefore the historical phrase “shadow-only wrapper” describes provenance, not the current conditional operational effect.

Any retirement, unification or constitutive regularization requires dedicated numerical/physical requalification.

See [`FLUID_NODES.md`](FLUID_NODES.md), [`PIPES_AND_FLOW.md`](PIPES_AND_FLOW.md), [`PUMPS.md`](PUMPS.md), [`VALVES.md`](VALVES.md), [`WATER_STEAM_MODEL.md`](WATER_STEAM_MODEL.md), [`SIMULATION_NUMERICAL_REGULARITY_AND_RUNTIME_REVIEW.md`](SIMULATION_NUMERICAL_REGULARITY_AND_RUNTIME_REVIEW.md) and ADR 0183.

## Reactor, rod-motion and thermal ownership

Reactor state is reduced-order but stateful:

- point kinetics owns neutron population/delayed-neutron evolution;
- fission-power conversion owns instantaneous fission thermal power;
- equivalent decay-heat groups own latent decay-energy inventory;
- temperature/void and iodine/xenon owners provide stateful feedback;
- quasi-spatial/core-zone projections remain bounded educational approximations.

Control-rod **physical motion is not instantaneous**. `ControlRodDefinition` owns a `ControlRodTravelRate` and `ControlRodMotionSolver` advances physical rod position over deterministic elapsed simulation time. The generic controller-side `ActuatorDefinition.ControlRod` intentionally has no separate actuator ramp because the physical rod subsystem already owns travel-rate mechanics; it maps controller intent into the canonical rod-command seam rather than duplicating rod motion.

See [`CONTROL_RODS.md`](CONTROL_RODS.md), [`NEUTRON_KINETICS.md`](NEUTRON_KINETICS.md), [`THERMAL_POWER.md`](THERMAL_POWER.md), [`DECAY_HEAT.md`](DECAY_HEAT.md), [`TEMPERATURE_FEEDBACK.md`](TEMPERATURE_FEEDBACK.md), [`VOID_FEEDBACK.md`](VOID_FEEDBACK.md) and [`IODINE_XENON_DYNAMICS.md`](IODINE_XENON_DYNAMICS.md).

## Secondary cycle, turbine and electrical ownership

The secondary side composes canonical steam headers, turbine admission/expansion, rotor state, condenser/hotwell, condensate/feedwater and generator/grid coupling.

The turbine/rotor solver owns mechanical power/rotor response; generator/grid owners control breaker/electrical state. Protection remains a separate owner and dominates operator or supervisory requests when its conditions require it.

Current relief and turbine-bypass pressure-opening laws are reduced-order and stateless. Their lack of blowdown/reseat hysteresis is a documented fidelity limitation, not an implicit actuator state owned elsewhere.

Pump hydraulic power and shaft demand are modeled, but full motor/electrical/loss-to-heat ownership remains a future M12 closure item before severe full-plant energy/consequence claims.

See [`MAIN_STEAM_NETWORK.md`](MAIN_STEAM_NETWORK.md), [`TURBINE_EXPANSION_AND_ROTOR.md`](TURBINE_EXPANSION_AND_ROTOR.md), [`CONDENSER_VACUUM_HOTWELL.md`](CONDENSER_VACUUM_HOTWELL.md), [`CONDENSATE_FEEDWATER_TRAIN.md`](CONDENSATE_FEEDWATER_TRAIN.md), [`GENERATOR_GRID_SYNCHRONIZATION.md`](GENERATOR_GRID_SYNCHRONIZATION.md) and [`KNOWN_MODEL_LIMITATIONS.md`](KNOWN_MODEL_LIMITATIONS.md).

## Instrumentation, control, protection and faults

Instrumentation owns measured-signal generation and deterministic sensor-fault effects. Controllers consume canonical measured/control inputs; actuator solvers map bounded command requests into the physical subsystem seams they are allowed to influence.

Protection/interlocks/SCRAM are authoritative over safety trips and latches. Alarm/annunciator state presents conditions but does not become a protection owner.

Scenario faults are Application orchestration. They may inject typed, bounded inputs through explicit fault-target interfaces but may not receive `PlantState`, integrate inventories directly or write physical/protection state arbitrarily.

See [`INSTRUMENTATION_SIGNAL_MODEL.md`](INSTRUMENTATION_SIGNAL_MODEL.md), [`CONTROLLER_ACTUATOR_PRIMITIVES.md`](CONTROLLER_ACTUATOR_PRIMITIVES.md), [`PROTECTION_INTERLOCKS_TRIPS_SCRAM.md`](PROTECTION_INTERLOCKS_TRIPS_SCRAM.md), [`ALARMS_ANNUNCIATOR_STATE.md`](ALARMS_ANNUNCIATOR_STATE.md) and [`DETERMINISTIC_FAULT_INJECTION_FRAMEWORK.md`](DETERMINISTIC_FAULT_INJECTION_FRAMEWORK.md).

## Scenario, challenge, demand and scoring ownership

Versioned scenarios own exact startup identity, allowed operator actions and deterministic automation/fault schedules. They do not own plant physics.

Operational challenges are Application state built above canonical session evidence:

```text
Scenario/session + canonical recorder evidence
                ↓
challenge lifecycle / conditions
                ↓
external demand reference + score evidence
                ↓
Mission/Performance presentation
```

Important boundaries:

- `GRID DEMAND` is an external reference, not a generator command;
- requested generator load is a command/control target, not demand or actual output;
- actual electrical output is measured plant evidence;
- scoring consumes evidence and never commands the plant;
- a generic trip is not globally success or failure: pack semantics decide what evidence is required;
- challenge state can be reconstructed from canonical recordings/checkpoints rather than persisted as an opaque challenge-state blob.

See [`OPERATIONAL_CHALLENGE_LIFECYCLE.md`](OPERATIONAL_CHALLENGE_LIFECYCLE.md), [`OPERATIONAL_CHALLENGE_ENERGY_DEMAND.md`](OPERATIONAL_CHALLENGE_ENERGY_DEMAND.md), [`OPERATIONAL_CHALLENGE_SCORING.md`](OPERATIONAL_CHALLENGE_SCORING.md), [`OPERATIONAL_CHALLENGE_PACKS.md`](OPERATIONAL_CHALLENGE_PACKS.md) and [`OPERATIONAL_CHALLENGE_REPLAY_CHECKPOINT_CLOSURE.md`](OPERATIONAL_CHALLENGE_REPLAY_CHECKPOINT_CLOSURE.md).

## Recording, checkpoints, replay and persistence ownership

`ScenarioRecorder` observes deterministic committed-step evidence and records a contiguous v1 frame/fingerprint sequence. Checkpoints identify deterministic reconstruction points; full replay re-dispatches canonical recorded actions and verifies fingerprints/evidence.

The recorder is observational with respect to plant state and command authority, but it is synchronous work: recording/fingerprinting can consume time and an evidence failure is not equivalent to “free background logging”. Future failure-policy or streaming changes require explicit contracts.

Infrastructure owns JSON DTOs/schema/error boundaries. Current session archive v1 preserves numeric enum ordinals and complete command payloads, including `NumericValue`; malformed data fail closed at the adapter boundary. Future schema/fingerprint versions must be explicit rather than silently redefining v1.

See [`RECORDER_CHECKPOINT_FULL_REPLAY.md`](RECORDER_CHECKPOINT_FULL_REPLAY.md), [`APPLICATION_RECORDING_REPLAY_REVIEW.md`](APPLICATION_RECORDING_REPLAY_REVIEW.md), [`PERSISTENCE_PAYLOAD_INTEGRITY_ERROR_CONTRACT.md`](PERSISTENCE_PAYLOAD_INTEGRITY_ERROR_CONTRACT.md), [`POST_INCIDENT_ANALYSIS.md`](POST_INCIDENT_ANALYSIS.md), ADR 0181 and ADR 0184.

## Control-room and Mission/Performance presentation ownership

Application produces immutable presentation models; App renders and navigates them.

The main HMI remains workspace-oriented rather than one giant control screen. COMPUTER retains its fixed F1–F8 page contract. Mission/Performance is a dedicated peer workspace reached through contextual selection, with no F9 and no plant-command authority.

Mission/Performance projects:

- objective/lifecycle state;
- logical time/progress;
- external grid demand;
- requested load;
- actual output;
- score and dimension decomposition;
- bounded recent protection/scoring evidence.

Presentation refresh cadence is separate from deterministic evidence sampling. Explicit structural comparison suppresses redundant UI publication; generated record equality over collection-bearing snapshots is not an update contract.

A future mission timeline must preserve a protected lifecycle spine separately from bounded recent operational evidence so dense protection/scoring traffic cannot erase the mission narrative.

See [`OPERATOR_EXPERIENCE_HMI_ARCHITECTURE.md`](OPERATOR_EXPERIENCE_HMI_ARCHITECTURE.md), [`OPERATOR_COMPUTER_INTEGRATED_UI.md`](OPERATOR_COMPUTER_INTEGRATED_UI.md), [`MISSION_PERFORMANCE_PRESENTATION_CONTRACT.md`](MISSION_PERFORMANCE_PRESENTATION_CONTRACT.md), [`MISSION_PERFORMANCE_WORKSTATION_NAVIGATION.md`](MISSION_PERFORMANCE_WORKSTATION_NAVIGATION.md), [`MISSION_PERFORMANCE_LIVE_WORKSPACE.md`](MISSION_PERFORMANCE_LIVE_WORKSPACE.md) and ADRs 0177, 0178, 0182 and 0184.

## Desktop host and session-write boundary

The desktop host requests bounded cooperative deterministic batches through Application; it does not drive Simulation from elapsed wall-clock catch-up. Avalonia remains single-threaded for UI callbacks, so long physical/projection work delays responsiveness rather than creating concurrent timer execution.

Expected numerical-step failure containment and non-destructive session replacement are App-host integrity responsibilities, not physics changes. The desktop host classifies only expected fail-closed step failures for PAUSE + diagnostic handling; unknown programming failures remain visible. Session overwrite selects the target before export and replaces an existing local file only after a temporary sibling has been completely written and durably flushed.

See [`DESKTOP_HOST_FAILURE_AND_SESSION_SAVE_INTEGRITY_REVIEW.md`](DESKTOP_HOST_FAILURE_AND_SESSION_SAVE_INTEGRITY_REVIEW.md) and ADR 0185.

## Determinism and replay target

Determinism means equal exact-version inputs, logical command/fault schedules and supported replay contracts produce equivalent canonical evidence without wall-clock dependence.

The following must remain deterministic unless a versioned contract explicitly changes:

- fixed-step state evolution;
- canonical topology/order;
- command ordering;
- scenario automation/fault ordering;
- challenge lifecycle/demand/scoring reconstruction;
- checkpoint continuation;
- snapshot fingerprint algorithm semantics;
- post-incident evidence ordering.

Performance measurement may use wall clock only as observational evidence and may never change numerical decisions.

## Architectural enforcement

Architecture is enforced by multiple layers:

1. project references prevent lower-layer/UI inversion;
2. constructors and definition registries fail closed on invalid topology/configuration;
3. typed command/fault interfaces constrain authority;
4. deterministic runtime tests verify state/replay contracts;
5. Application/App tests verify presentation and HMI ownership boundaries;
6. ADRs record deliberate ownership/versioning decisions;
7. `KNOWN_MODEL_LIMITATIONS.md` records fidelity not yet claimed;
8. focused gates produce evidence for milestone promotion.

A green gate proves the tested reduced-order contract, not industrial fidelity outside its declared envelope.

## Navigation and historical provenance

- Current project/candidate status: [`PROJECT.md`](PROJECT.md)
- Future work only: [`ROADMAP.md`](ROADMAP.md)
- Model limitations: [`KNOWN_MODEL_LIMITATIONS.md`](KNOWN_MODEL_LIMITATIONS.md)
- Full top-level documentation index: [`TOP_LEVEL_DOCUMENT_INDEX.md`](TOP_LEVEL_DOCUMENT_INDEX.md)
- ADR index: [`adr/README.md`](adr/README.md)
- Historical architecture chronology: [`history/ARCHITECTURE_MILESTONE_LEDGER.md`](history/ARCHITECTURE_MILESTONE_LEDGER.md)
