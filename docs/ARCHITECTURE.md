# Architecture

## Purpose

Nuclear Reactor Simulator is designed as an educational full-plant simulator. The architecture must support progressively richer physical models without coupling simulation mathematics to the UI, persistence, or wall-clock timing.

## Current architecture checkpoint

- M3, M4, M5 and M6 phase gates are validated and complete.
- M6.1–M6.7 are locally validated.
- M7.1 is locally validated and establishes exact-version initial-condition/scenario/session boundaries over the validated M6 runtime surface.
- M7.2 hotfix 1 is locally validated and supplies the concrete `cold-shutdown-pre-start` v1 recipe plus observational readiness/guidance.
- M7.3 is locally validated and provides exact `pre-criticality-source-range` v1 plus controlled first-criticality/low-power operation.
- M7.4 is locally validated and supplies exact `low-power-steam-raising` v1 plus turbine-startup guidance through M5.4.
- M7.5 is locally validated and supplies exact `pre-synchronization-grid-loading` v1, canonical M4.5 synchronization/breaker closure and bounded requested electrical-load commands.
- M7, M8 and M9 gates are complete. M8.1–M8.7 hotfix 2 and M9.1–M9.7 are validated; M9.7 hotfix 5 passed all 760 automated tests and the final user-corrected `MainWindow.axaml` is integrated as the validated GUI layout baseline. M10.1–M10.9.3 are validated; M10.9.3 Interactive Full-Plant Mimic is the official application baseline. M10.9.4 Hotfix 22 is the latest user-validated structural checkpoint and Hotfix 23 Pressure/Temperature/Vapor-Dependent Turbine Work is the current candidate. ADR 0075–ADR 0090 record the HMI, replay, hydraulic, condenser, grid, pump, protection, actuator, governor and turbine-work boundaries.
- M8.2 hotfix 2 introduced a headless `NuclearReactorSimulator.App.Tests` boundary for ViewModel/XAML interaction contracts. These tests may reference `App`, but production dependency direction is unchanged: `App` still has no `Simulation` reference and owns no physics.

For the exact validation/restart state, `PROJECT_HANDOFF.md` is authoritative.

## Production projects

### NuclearReactorSimulator.Domain

Contains stable domain concepts and value-oriented abstractions that describe the problem space.

M1.1 establishes `Domain.Physics.Quantities` as the canonical boundary for physical units. Physical quantities are immutable value types, use SI internally, reject non-finite values, and expose explicit conversions. Absolute temperature/pressure are distinct from signed temperature/pressure differences.

M1.2 adds `Domain.Physics.Fluids` for immutable lumped fluid control-volume state. Fluid geometry, conserved inventory and thermodynamic closure variables are separated so conservation logic does not depend on a premature equation of state.

M1.3 extends the same domain with immutable passive `PipeDefinition` connections and a strongly typed quadratic hydraulic resistance. Pipe from/to identifiers establish a sign convention only; actual flow is bidirectional.

M3.1 adds `Domain.Plant` as the canonical plant-composition boundary. `PlantDefinition` owns deterministic validated topology registries, while `PlantState` owns the exact complete state set for stateful components. Topology validation happens before simulation orchestration; no plant solver exists in the Domain layer.

Rules:

- no Avalonia references;
- no filesystem/database dependencies;
- no infrastructure dependencies;
- no wall-clock timers;
- no application orchestration.

### NuclearReactorSimulator.Simulation

Contains the simulation runtime and physical models.

Current responsibilities include:

- deterministic fixed-timestep execution;
- generic command scheduling and immutable snapshot publication;
- transactional step boundaries and runtime fault semantics;
- deterministic invariant validation;
- logical command trace/replay primitives;
- deterministic fluid-node balance integration and thermodynamic closure boundaries;
- deterministic passive pipe flow and conservative connection balances;
- point-reactor neutron kinetics and delayed-neutron state evolution;
- explicit neutron-population-to-fission-power conversion and thermal deposition boundaries;
- stateful equivalent-group decay-heat inventory, release and thermal deposition boundaries;
- committed-state temperature/void feedback and stateful iodine/xenon poisoning boundaries;
- plant-level immutable `PlantSnapshot` projection from a committed `PlantState`;
- deterministic `PlantNetworkOrchestrator` staged solving with canonical balance accumulation, exactly-once inventory integration and plant-level mass/energy audit diagnostics.

M3.2 adds the plant-level execution seam: every component solver reads the same committed `PlantState`, returns balances only, and conserved inventories are integrated once after all balances are accumulated. `PlantNetworkAudit` makes mass/energy closure and explicit external pump/source power observable without hidden correction.

Validated responsibilities now also include:

- coarse core-zone and equivalent fuel-channel-group composition;
- integrated primary-circuit, steam-drum, feedwater and steam-boundary physics;
- main-steam, turbine/rotor, condenser/hotwell, condensate/feedwater and generator/grid models;
- full-plant audit/composition boundaries;
- measured-signal instrumentation and deterministic sensor-fault seams;
- controller/actuator primitives and reactor/primary plus turbine/secondary automatic controls;
- protection/interlocks/SCRAM and alarm/annunciator logical state;
- integrated automatic-operation composition and deterministic verification runners.

Simulation does **not** own Avalonia presentation, wall-clock pacing, persistence format policy or scenario authoring semantics. Later fidelity refinements must remain behind validated seams and preserve existing authoritative state ownership.

Allowed production dependency: `Domain`.

### NuclearReactorSimulator.Application

Contains use cases, orchestration contracts and application-level coordination between the operator-facing application and the simulation engine.

Validated M6 responsibilities include:

- presentation-safe `ControlRoomSnapshot` projection over validated simulation snapshots;
- typed operator command contracts and command-dispatch boundaries;
- reusable control-room semantic state/component catalogs;
- Reactor/Core, Primary-Circuit, Turbine/Secondary, Electrical and Alarms/Events presentation contracts;
- bounded logical-step trend/event presentation history;
- validated M6.7 runtime adapter/coordinator for run/pause/single-step, typed command translation and sparse observational snapshot publication;
- validated M7.1 exact-version initial-condition/session registry, scenario command gating and deterministic replay orchestration;
- validated M7.2 concrete cold-shutdown recipe plus presentation-only pre-start readiness and declarative guidance;
- validated M7.3 pre-criticality/source-range initial condition, controlled rod permissions and observational criticality/low-power guidance;
- M7.4–M7.7 and M8.1–M8.7 are validated; the M7 and M8 gates are complete. M9.1 adds only observational recording, versioned replay-backed checkpoints and deterministic full replay/seek verification over existing owners.

M8.1 fault orchestration remains Application state. `ScenarioFaultRuntimeEngine` decorates the existing runtime at committed step boundaries, while `IScenarioFaultApplicator` implementations own only typed adaptation into validated subsystem seams. Plant-condition evaluators consume `ControlRoomSnapshot` only. Neither scheduler nor evaluator may traverse authoritative true state or create a second integrator.

M8.2 hydraulic effects are represented as immutable per-step `HydraulicComponentFaultInputs`. The Application runtime exposes only a typed `IHydraulicComponentFaultTarget`; scenario applicators never receive `PlantState`. Pump/valve constraints are applied after canonical control/protection command arbitration and before the existing physical solvers. Selected leaks become signed `PlantNetworkSourceTerms` that traverse the existing M4→M3 composition and are integrated exactly once by `PlantNetworkOrchestrator`.

M8.3 instrumentation/control effects bind through `IInstrumentationControlFaultTarget`. Sensor effects replace only canonical per-step `SensorFaultInput` entries and remain owned by `InstrumentationSolver`. Controller/actuator-command effects are temporary bounded `ControllerInput` overlays that still traverse `ControllerSystemSolver` and `ActuatorSystemSolver`; no fault writes physical actuator state or protection latches directly. Faulted measurements reach M5.5 only through the same committed `MeasuredSignalFrame` ordering used in normal operation.

M8.4 secondary-system transient effects bind through `ISecondaryTransientFaultTarget`. Turbine/generator trip events become one-shot canonical protection inputs; feedwater degradation/loss reuses M8.2 pump constraints; condenser degradation/loss scales only the existing M4.3 `CondenserCoolingBoundaryInput`. Trip latches, breaker state, rotor response, feedwater inventory and condenser pressure/vacuum remain owned by M4/M5 and the single plant-network integration boundary.

M8.5 leak/LOCA-class effects bind through `ILossOfCoolantFaultTarget`. A `loca.pressure-driven-break` is stored as immutable per-step `PressureDrivenBreakInput`; the protected full-plant composition derives bounded discharge from committed source-node pressure and emits only negative mass plus carried-internal-energy source terms. `PlantNetworkOrchestrator` remains the single inventory integrator, and thermodynamic pressure/temperature/phase remain derived state. The per-step inventory bound is an explicit educational-model validity guard, not ECCS/containment physics or a scripted accident correction.

M8.6 electrical-loss effects bind through `IElectricalLossFaultTarget`. `electrical.external-supply-loss` constrains only the existing M4.5 `GeneratorGridInputs` so breaker-open forcing dominates close requests while active; M4.5 remains the sole breaker/electrical/rotor owner. Station-blackout-class scenarios declare pump trips and powered command-path losses as explicit M8.2/M8.3 faults rather than inferring consequences from an unmodeled bus network. M2.5 stateful decay heat remains outside the current M5.7 integrated runtime envelope, so M8.6 must not fabricate scenario-owned constant decay heat.

Application may depend on Simulation to coordinate validated runtime seams, but Avalonia must not bypass Application and reference Simulation directly.

Allowed production dependencies: `Domain`, `Simulation`.

### NuclearReactorSimulator.Infrastructure

Contains persistence and external technical adapters such as save files, scenario storage, settings and future replay serialization.

Allowed production dependencies: `Domain`, `Application`.

Infrastructure must not contain simulation physics.

### NuclearReactorSimulator.App

Avalonia presentation layer and composition root.

Responsibilities:

- create and wire the desktop application graph;
- translate operator gestures into typed Application commands;
- render immutable **presentation** snapshots supplied through Application boundaries;
- host reusable instruments/controls and control-room workspaces;
- contain Avalonia-specific Views, ViewModels and presentation code only.

App must not implement physics, advance deterministic simulation time, traverse authoritative `FullPlantSnapshot`/`PlantState` directly, or reference `NuclearReactorSimulator.Simulation` namespaces. This is the only production project allowed to reference Avalonia packages.

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

The App is the composition root but, since M6.1, references Application and Infrastructure only; Simulation remains reachable through Application/runtime composition rather than a direct Avalonia dependency. Lower layers must never reference the App.

## Physical quantity boundary

Future physical models use strongly typed quantities at subsystem/API boundaries:

```text
explicit input units
      ↓
strong quantity type
      ↓ canonical SI
numerical solver internals
      ↓ validated construction
strong quantity type
```

Raw primitive numeric arrays are permitted inside numerical kernels where useful, but unit semantics must be restored and validated at solver boundaries. There are no implicit conversions between physical quantities and `double`. See `docs/PHYSICAL_QUANTITIES.md` and ADR 0008.

## Fluid-node boundary

M1.2 models a populated lumped control volume as:

```text
FluidNodeDefinition (identity + fixed volume)
            +
FluidNodeInventory (mass + internal energy)
            +
FluidThermodynamicState (pressure + temperature)
            =
FluidNodeState
```

Density and specific internal energy are derived, not stored independently. `FluidNodeIntegrator` applies signed net mass/energy rates over an explicit deterministic interval and delegates intensive-state resolution to `IFluidThermodynamicModel`.

M1.2 established the closure seam without a production equation of state. M1.7 now supplies the first simplified production water/steam implementation behind that unchanged seam. See `docs/FLUID_NODES.md`, `docs/WATER_STEAM_MODEL.md`, ADR 0009 and ADR 0014.

## Passive pipe boundary

M1.3 connects two committed fluid-node states without giving the pipe its own fluid inventory:

```text
FromNodeState ── pressure difference ──> PipeFlowSolver <── ToNodeState
                                      ↓
                              signed mass/energy flow
                                      ↓
                    equal-and-opposite FluidNodeBalance
```

The current law is the lumped quadratic relation `Δp = R · m_dot · |m_dot|`. The reference from/to direction defines only the sign convention; the pressure difference can reverse the solved flow naturally.

Transported energy still uses the upstream node's specific internal energy as a conservative interim model. M1.7 now provides phase closure, while a future open-system enthalpy/two-phase transport refinement remains separate from the pipe law. All future network connections in one step must be solved from the same committed pre-step state before node integration, preventing connection-order dependence. See `docs/PIPES_AND_FLOW.md` and ADR 0010.


## Decay-heat inventory boundary

M2.5 keeps decay heat separate from instantaneous M2.4 fission power. Each configured equivalent group owns a conserved latent energy inventory:

```text
current fission power
        ↓ generation fraction f_i
latent group energy E_i
        ↓ lambda_i E_i
emitted decay heat
        ↓
thermal/fluid energy balances
```

For each group the model is `dE_i/dt = f_i P_fission - lambda_i E_i`. `DecayHeatSolver` applies the analytic finite-step solution over the deterministic fixed timestep. This provides power-history memory without adding a new wall-clock or adaptive-time dependency.

The step result deliberately distinguishes:

- **average emitted decay power**, used by same-step thermal/fluid integrators so deposited energy equals the exact integrated release;
- **end-of-step instantaneous decay power**, used for snapshots and diagnostics.

The latent inventory obeys the explicit balance `E_old + E_produced = E_new + E_emitted` within floating-point precision. Group generation fractions and decay constants are injected configuration; no plant-specific constants are hardcoded. See `docs/DECAY_HEAT.md` and ADR 0019.

## Temperature-feedback boundary

M2.6 closes the first thermal-neutronic loop through named reactivity contributions rather than direct state mutation:

```text
committed fuel/coolant temperature
        ↓
TemperatureFeedbackSolver
        ↓
FuelTemperature / CoolantTemperature ReactivityContribution
        ↓
ReactivityModel → PointKineticsSolver
        ↓
fission/decay heat → thermal/fluid integrators
        ↓
next committed temperature
```

The current law is `rho_T = alpha_T * (T - T_ref)`. Coefficients are signed configuration data stored canonically as delta-k/k/K. M2.6 deliberately uses committed start-of-step temperatures and does not perform hidden algebraic iteration, preserving deterministic transaction semantics and replay. See `docs/TEMPERATURE_FEEDBACK.md` and ADR 0020.

## Simulation boundary

The runtime flow established through M0.2/M0.3 is:

```text
Operator input / deterministic trace
              ↓
         queued command
              ↓
       fixed-step boundary
              ↓
          plant kernel
              ↓
       candidate state
              ↓
    registered invariants
         ↓          ↓
       pass        fail
         ↓          ↓
      commit      Faulted
         ↓
 immutable snapshot
         ↓
  Application / UI
```

The UI refresh rate is independent from the physical simulation timestep. The Simulation project never reads wall-clock time directly; an external scheduler or test harness supplies elapsed duration explicitly.

Commands are assigned monotonic sequence numbers and are consumed FIFO only at fixed-step boundaries. Runtime metadata and model-specific state are published through immutable snapshot envelopes.

## Transactional step boundary

A candidate physical step is not considered committed until kernel execution and every registered invariant succeed.

On failure:

- logical time remains at the last committed step;
- the last committed state remains published;
- commands drained for the failed step are restored in original order;
- the runtime becomes terminally `Faulted`;
- stable fault metadata remains inspectable through snapshots.

Concrete plant kernels must treat previously committed state as immutable/copy-on-write. The generic runtime cannot undo arbitrary in-place mutation of a referenced state object.

## Invariant boundary

`ISimulationInvariant<TState>` provides deterministic pre-commit validation for properties that must always hold, for example future rules such as:

```text
mass >= 0
pressure is finite and within model bounds
temperature is finite
valve position within allowed range
no NaN / Infinity in physical state
```

M0.3 establishes the mechanism only; physical invariants arrive with the corresponding physical models.

## Command traces and replay

Replay primitives use logical simulation steps, never wall-clock timestamps:

```text
(step 12, command A)
(step 12, command B)
(step 40, command C)
```

Commands sharing a step execute in trace order. `SimulationReplayRunner` drives a paused runtime through deterministic `StepOnce()` calls.

Persistence/serialization of traces belongs to Infrastructure and is intentionally deferred.

## Determinism target

A simulation run must satisfy:

```text
same initial state
+ same fixed timestep
+ same ordered logical command trace
+ same explicit external-duration/speed sequence (when wall-driven)
= same resulting simulation state and snapshots
```

Randomness, where required, must be explicit and seeded.

## Automated simulation test harness

`NuclearReactorSimulator.Simulation.Tests` contains a reusable generic deterministic scenario harness that:

- schedules commands at logical fixed-step boundaries;
- runs completely headlessly;
- captures the initial snapshot and every committed step snapshot;
- supports assertions on intermediate as well as final state.

This becomes the standard verification path for M1+ physical models.

## Architectural enforcement

`NuclearReactorSimulator.Application.Tests` contains architecture tests that inspect project references and package references directly from the repository. They fail if:

- a core project references a forbidden production project;
- Avalonia leaks outside `NuclearReactorSimulator.App`;
- project dependency boundaries differ from the approved graph;
- wall-clock, timer, sleeping or delay APIs leak into the Simulation project.

## M1.4 valve composition boundary

M1.4 treats a valve as a controllable restriction composed over a validated passive `PipeDefinition`. The pipe resistance remains the fully-open reference resistance. `ValveCharacteristicSolver` maps immutable mechanical position to normalized capacity, while `ValveFlowSolver` converts that capacity to effective resistance and delegates the actual pressure-driven conservative transfer to `PipeFlowSolver`.

This preserves one passive hydraulic law and keeps future actuator/control dynamics separate from hydraulic conservation. Closed valves are handled explicitly as zero transfer rather than by constructing infinite or arbitrary giant resistances.


## M1.5 active pump boundary

M1.5 introduces the first external mechanical-energy source without creating a second hydraulic network law. `PumpFlowSolver` composes an active pressure source and positive internal quadratic resistance with an existing `PipeDefinition`. Endpoint pressure, pump speed and total resistance determine signed flow.

Pump speed is explicit state. Active pressure follows the speed-squared affinity law; the solver never imposes a requested mass flow. Mass remains conservative between fluid nodes, while the sum of endpoint energy balances equals the signed hydraulic power exchanged by the active source. Positive shaft demand is derived through explicit pump efficiency; reverse hydraulic exchange does not create regenerative generation in this milestone.

Rotor dynamics, electrical motors, cavitation, detailed performance maps and thermal dissipation remain outside the hydraulic primitive and can be layered later without changing the network boundary.


## M1.6 thermal boundary

Heat transfer remains inside `Simulation` and depends only on Domain thermal definitions/states and strongly typed quantities. `ThermalBodyState` stores conserved energy and derives temperature; `HeatTransferSolver` is stateless and returns signed equal-and-opposite balances. Fluid coupling occurs only by composing thermal power into `FluidNodeBalance.NetEnergyRate`. External heat sources are explicit energy-boundary inputs. No Avalonia or wall clock is introduced into the thermal solver; M1.7 water/steam closure remains a separate `IFluidThermodynamicModel` implementation.
## M1.7 thermodynamic closure boundary

`FluidNodeInventory` remains the owner of conserved mass and internal energy. `FluidNodeDefinition` remains the owner of fixed control-volume geometry. `SimplifiedWaterSteamThermodynamicModel` is the first production implementation that maps those conserved values to pressure, temperature and coarse phase.

```text
Definition(volume) + Inventory(mass, internal energy)
                    ↓
          IFluidThermodynamicModel
                    ↓
pressure + temperature + phase + optional vapor quality
```

Hydraulic and thermal solvers may consume the resulting state but must not duplicate or override the closure equations. A future higher-fidelity water/steam backend must replace the implementation behind the same seam rather than leaking steam-table logic into pipes, valves, pumps or UI code.


## M2.1 reactivity composition boundary

Reactivity is an explicit signed dimensionless input to M2.3 point kinetics, not a direct power calculation:

```text
independent physical mechanisms
          │
          ▼
named ReactivityContribution values
          │
          ▼
     ReactivityModel
          │
          ├── total rho
          └── immutable diagnostic breakdown
          │
          ▼
PointKineticsSolver
```

Contributions are canonicalized by kind and ordinal identity before compensated summation. This prevents caller enumeration order from becoming a hidden simulation input. Rod worth, temperature coefficients, void feedback and xenon dynamics remain owned by their dedicated later milestones. No direct `reactivity -> thermal power` mapping is permitted. See `docs/REACTIVITY_MODEL.md` and ADR 0015.


## M2.2 control-rod mechanics boundary

Control-rod mechanics remain separate from both reactivity composition and neutron kinetics:

```text
rod/group commands
      │
      ▼
persistent ControlRodMotion state
      │
      ▼
ControlRodMotionSolver + mechanical limits
      │
      ▼
ControlRodPosition
      │
      ▼
ControlRodWorthSolver
      │
      ▼
named ReactivityContribution(kind = ControlRods)
      │
      ▼
ReactivityModel
      │
      ▼
PointKineticsSolver
```

`ControlRodPosition` uses withdrawal fraction as the canonical convention: `0` is fully inserted and `1` is fully withdrawn. Group commands are convenience fan-out over individual immutable rod states; they do not create a second physical state. Worth curves map position to integral reactivity contribution only. They do not alter neutron population, thermal power or fluid state directly. The current linear/smooth-step curves are educational approximations behind a replaceable solver boundary, allowing later RBMK-specific axial worth behavior without changing motion commands or runtime semantics. See `docs/CONTROL_RODS.md` and ADR 0016.


## M2.3 point-kinetics boundary

Total reactivity is now a dynamic input to generic point kinetics:

```text
ReactivityModel total rho
        │
        ▼
PointKineticsParameters
  Λ + {β_i, λ_i}
        │
        ▼
PointKineticsSolver
        │
        ├── normalized neutron population n
        ├── delayed precursor populations C_i
        └── prompt-critical / reactor-period diagnostics
        │
        ▼
M2.4 thermal-power coupling
```

The solver is plant-independent. No RBMK kinetic constants are embedded in `Simulation`; later plant configuration supplies parameter sets. External runtime fixed-step semantics remain unchanged. Internal deterministic RK4 substeps are derived only from the physical coefficients, reactivity and requested fixed timestep. Point kinetics does not directly modify thermal bodies, coolant or UI state. See `docs/NEUTRON_KINETICS.md` and ADR 0017.

## M2.4 neutron-to-thermal-power boundary

M2.4 introduces an explicit one-way coupling from validated point kinetics into thermal generation:

```text
PointKineticsState.NeutronPopulation
              ↓
      FissionPowerCalibration
              ↓
       FissionPowerSolver
              ↓
 instantaneous fission thermal power
              ↓
 canonical heat-deposition partition
         ↙          ↓          ↘
       fuel     structures    coolant
         ↓          ↓          ↓
 ThermalEnergyBalance / FluidNodeBalance
```

The point-kinetics solver never mutates thermal state. `FissionPowerSolver` is stateless, plant-independent and configured by an explicit reference neutron population/reference thermal-power pair. Heat-deposition targets are named domain boundaries whose fractions form a complete partition.

All integrated energy changes remain owned by the already validated thermal/fluid integrators. Fission heat delivered to a fluid node uses a zero-mass `FluidNodeBalance`, so thermal generation cannot create coolant inventory. Decay heat is intentionally excluded from this value and is implemented as an independent stateful source in M2.5. See `docs/THERMAL_POWER.md` and ADR 0018.



## M2.7 void-feedback boundary

M2.7 keeps thermohydraulic phase interpretation separate from neutron reactivity mapping:

```text
committed FluidThermodynamicState
        │
        ▼
WaterSteamVoidFractionSolver
        │  VaporQuality != VoidFraction
        ▼
VoidFraction
        │
        ▼
VoidFeedbackSolver
        │
        ▼
named ReactivityContribution(kind = Void)
        │
        ▼
ReactivityModel → PointKineticsSolver
```

For saturated mixtures, vapor quality is converted from mass fraction to volumetric fraction using the M1.7 saturation liquid/vapor densities. Subcooled liquid maps to zero void and superheated vapor to full void. The generic engine hardcodes neither the sign nor magnitude of void reactivity. Like M2.6 temperature feedback, M2.7 evaluates committed state once per fixed step and introduces no hidden same-step nonlinear iteration. See `docs/VOID_FEEDBACK.md` and ADR 0021.


## M2.8 iodine/xenon poisoning boundary

M2.8 keeps slow poison memory as explicit immutable state rather than deriving xenon directly from current power:

```text
committed fission power + committed neutron population
        ↓
IodineXenonSolver
        ↓
I-135 / Xe-135 candidate inventories
        ↓
XenonReactivityCoefficient × Xe inventory
        ↓
named Xenon ReactivityContribution
        ↓
ReactivityModel
```

The reduced equations are linear for constant inputs over one fixed step and are integrated analytically. Production rates, decay constants, neutron-burnup coefficient and reactivity worth are configuration data. The generic engine does not embed plant-specific yields, cross sections or RBMK constants. Like M2.6/M2.7 feedback, the model uses committed inputs for the step and introduces no hidden same-step nonlinear iteration. See `docs/IODINE_XENON_DYNAMICS.md` and ADR 0022.

## M3 plant-composition and staged-network boundary

M2.8 closes the validated reactor-physics foundation. M3 must assemble existing primitives without collapsing them into a monolithic reactor update method.

The plant-level composition target is:

```text
PlantDefinition + committed PlantState
        ↓
canonical component/topology registries
        ↓
component solvers read common committed state
        ↓
signed hydraulic/thermal/source balances
        ↓
deterministic accumulation per inventory
        ↓
each conserved inventory integrated once
        ↓
thermodynamic closure + invariants
        ↓
transactional commit + immutable PlantSnapshot
```

A component must not mutate a shared node and thereby change the input observed by a later component in the same fixed step. Enumeration order is not a physical input.

M3 initially keeps global point kinetics and introduces coarse zones only as configurable power-distribution/thermal-hydraulic/feedback domains. Individual full-core channels and spatial neutron transport are explicitly deferred.

See `docs/PROJECT_STATUS.md`, `docs/PRIMARY_CIRCUIT_PLAN.md` and ADR 0023.


## M9.4 quasi-spatial refinement over the aggregated-core boundary

M9.4 preserves the M3.3/M2 ownership split and adds an optional refinement layer rather than a second reactor model:

```text
committed canonical per-zone fuel/coolant domains
        +
committed AggregatedCoreState power shares
        ↓
existing linear M2 fuel/coolant/void feedback equations
        ↓
local zone feedbacks
        ├── power-share weighted scalar → existing non-rod seam → global PointKineticsSolver
        └── explicit configured zone coupling → deterministic candidate power shape
```

The candidate shape is normalized `AggregatedCoreState` only and applies on the following committed step. Coupling affects only shape-driving signals; it is not neutron diffusion. Logical coordinates never imply adjacency. Configurations without `QuasiSpatialCoreFeedbackDefinition` retain the prior path exactly and are not silently upgraded. Higher-resolution full-plant profiles must still provide matching canonical M3 topology/channel groups rather than duplicating physical inventories. See `SPATIAL_QUASI_SPATIAL_FIDELITY.md` and ADR 0071.

## M9.5 historical-inspired provenance and fidelity boundary

M9.5 extends the existing M7/M8 scenario document with optional descriptive provenance metadata; it does not create a new simulation owner:

```text
ScenarioDefinition schema v3
        ├─ exact initial-condition/objective/action/fault data (existing owners)
        └─ optional HistoricalScenarioContextDefinition
                ├─ declared sources
                ├─ DocumentedFact / EducationalApproximation / SimulatorSpecificAssumption claims
                ├─ required validated model-capability IDs
                └─ deliberate non-claims
                        ↓
HistoricalScenarioFidelityReviewer
                        ↓ fail closed before runtime creation
ScenarioSessionFactory
                        ↓
existing canonical scenario/runtime owners
```

Historical metadata is immutable Application-layer description. It may not write physical state, add hidden historical physics, script a target trajectory or turn chronological evidence into causal inference. JSON schema v3 persists the context; v0/v1/v2 migration always yields no historical context rather than inventing provenance. `ScenarioSessionFactory` gates historical-inspired content against an explicit capability set before resolving the runtime. A passed review means only that declared simulator capabilities are available; it is not quantitative historical calibration or proof of historical truth. See `HISTORICAL_INSPIRED_SCENARIO_FRAMEWORK.md` and ADR 0072.

## M9.6 reference-validation and M9.7 integration-gate boundary

M9.6 observes immutable presentation/canonical solver evidence through versioned reference cases and explicit tolerance budgets. It never fits parameters automatically or reclassifies internal regression numbers as historical measurements.

M9.7 is verification-only and introduces no new runtime owner:

```text
M9.1 replay evidence ─┐
M9.2 analysis evidence ├─→ M9.7 integration tests / gate documents
M9.3 xenon state ──────┤
M9.4 spatial feedback ─┤
M9.5 fidelity metadata ┤
M9.6 reference evidence┘
```

Cross-feature tests may compose existing owners, but production state still belongs to the original M2–M5/M7–M9 contracts. In particular, simultaneous xenon and quasi-spatial feedback must still feed one global point-kinetics solve exactly once. Real-runtime App tests verify the presentation/command boundary without moving state ownership into the ViewModel. The final manual GUI checklist is phase-gate evidence only; it does not create a new runtime behavior.

See `M9_ADVANCED_FIDELITY_INTEGRATION_GATE.md` and `milestones/M9.7.md`.

## Aggregated core-zone projection (M3.3)

M3.3 adds a spatial identity/projection layer above the validated global point kinetics and composed plant state.

```text
PointKineticsSolver (global)
        ↓
FissionPowerSolver (global total)
        ↓
AggregatedCoreState power shares
        ↓
AggregatedCorePowerSolver
        ↓
CoreZoneSnapshot[]
        ↘ reads committed PlantState fuel/structure/coolant domains
```

Core zones do not duplicate conserved inventories. Each zone references canonical `PlantDefinition` domains and projects local diagnostics from the committed `PlantState`. Logical coordinates and zone count are configurable; no 3×3 shape is embedded in the engine.

Per-zone channel hydraulics and heat deposition are deliberately deferred to M3.4.


## Fuel-channel group composition (M3.4)

M3.4 adds a semantic channel-group layer above the M3.3 zone projection and M3.1 plant topology. A group references an existing passive hydraulic pipe plus canonical zone fuel, structure and outlet-coolant domains; it does not own duplicate inventories.

The step boundary is:

```text
AggregatedCoreSnapshot
        +
optional global decay heat
        ↓
FuelChannelGroupSolver
        ├── immutable group diagnostics
        └── PlantNetworkSourceTerms
                    ↓
PlantNetworkOrchestrator
        solve canonical network from committed state
        + accumulate source terms
        + integrate each inventory once
        + audit external power
```

`PlantNetworkSourceTerms` is a generic staged seam for physical source models. Its declared external power is included explicitly in `PlantNetworkAudit`; it does not bypass conservation accounting or mutate `PlantState` directly.

Global point kinetics remains unchanged. M3.4 spatially routes power but does not introduce spatial neutron diffusion or per-group neutron populations.

## Main-circulation semantic composition (M3.5)

M3.5 groups canonical M3.1/M3.4 components into meaningful circulation loops without introducing another hydraulic state or solver boundary.

```text
suction header FluidNode
        │
        ├── PumpDefinition/PumpState (MCPs)
        ▼
pressure header FluidNode
        │
        ├── FuelChannelGroup hydraulic pipe
        ▼
group outlet FluidNode
        │
        └── passive return PipeDefinition
                ↓
        suction header
```

`MainCirculationSystemDefinition` validates semantic ownership and endpoint closure. `MainCirculationSystemSolver` reads a committed `PlantState` and delegates to the validated `PumpFlowSolver`, `PipeFlowSolver` and water/steam void diagnostic. It returns immutable flow/pressure/power/continuity snapshots only.

The solver never integrates inventories. `PlantNetworkOrchestrator` remains the sole gather/solve/accumulate/integrate boundary. This ensures that the M3.5 RBMK-like circulation composition cannot create order-dependent state evolution or duplicate the M1 hydraulic equations.

M3.6 extends the loop with an explicit `ReturnCollectorNodeId`, preserving the original constructor as a backward-compatible suction-header return. Steam-drum configurations route channel returns into a dedicated canonical fluid-node inventory before staged separation. See `docs/MAIN_CIRCULATION_SYSTEM.md` and ADR 0028.

## Steam-drum separation and recirculation (M3.6)

M3.6 adds a semantic steam-drum layer without adding another state integrator. Each circulation loop owns exactly one `SteamDrumDefinition`; its inventory and steam outlet are canonical plant fluid nodes.

```text
committed PlantState
        ↓
MainCirculationSystemSolver
        ↓ committed return-flow diagnostics
SteamDrumSeparationSolver
        ↓ conservative PlantNetworkSourceTerms
PlantNetworkOrchestrator
        ↓ single integration / closure / audit
Candidate PlantState
```

For a committed saturated mixture, vapor quality determines the ideal mass split. Saturated-liquid and saturated-vapor internal energies from the M1.7 simplified water/steam model determine the two separated energy rates. Subcooled liquid recirculates entirely as liquid; superheated vapor leaves entirely through the steam outlet.

The source terms remove mass/energy from the drum inventory and add the same mass/energy to the steam-outlet and MCP-suction nodes. This is an internal transfer with zero declared external mass flow and zero declared external power. See `docs/STEAM_DRUMS.md` and ADR 0029.

## Feedwater and steam external boundaries (M3.7)

M3.7 adds temporary replaceable plant-boundary interfaces without adding a new integrator. Each steam drum has exactly one feedwater source targeting its canonical inventory node and one steam-export sink sourcing its canonical steam-outlet node.

`PrimaryCircuitBoundarySolver` reads one committed `PlantState` plus complete per-step boundary inputs and emits `PlantNetworkSourceTerms`. Feedwater declares positive external mass/power. Steam export declares negative external mass/power, using the committed steam-outlet specific internal energy.

`PlantNetworkSourceTerms` therefore carries signed `ExternalMassFlowRate` and signed `ExternalPower`. `PlantNetworkAudit` compares total accumulated node mass rate against the declared external mass rate through an explicit balance residual, while the existing interval mass/energy closure remains observable. Independent staged contributions can be combined before the single `PlantNetworkOrchestrator` integration phase.

See `docs/PRIMARY_CIRCUIT_BOUNDARIES.md` and ADR 0030.


## Integrated primary-circuit baseline (M3.8)

M3.8 adds a semantic `IntegratedPrimaryCircuitDefinition` over the validated M3.3–M3.7 lineage. It does not own duplicate topology or state; canonical physical ownership remains in `PlantDefinition`/`PlantState`.

`IntegratedPrimaryCircuitSolver` enforces the final M3 coupling rule: aggregated core projection, channel-group heat allocation, main-circulation diagnostics, steam-drum separation and feedwater/steam external boundaries all read the same committed plant state. Their source terms are combined before one `PlantNetworkOrchestrator` call, so no component sees another component's same-step candidate state and every conserved inventory is integrated exactly once.

`IntegratedPrimaryCircuitSnapshot` is the first full M3 plant-level diagnostic projection. It aggregates global inventory/power/flow/audit values while retaining the detailed subsystem snapshots. `PrimaryCircuitReferenceOperatingPoint` and `PrimaryCircuitLongRunRunner` provide deterministic headless gate verification; they measure drift and residuals but never correct state to manufacture steady operation. See `docs/INTEGRATED_PRIMARY_CIRCUIT.md` and ADR 0031.

## Main steam network and turbine admission (M4.1)

M4.1 extends the validated M3.8 plant without creating a secondary hydraulic graph. `MainSteamNetworkDefinition` is a semantic composition layer over the same canonical `PlantDefinition` pipes, valves and fluid nodes already owned by the plant network.

Every M3 `SteamExportBoundaryDefinition` seam must map to exactly one `MainSteamLineDefinition`. While M4.1 is active, the legacy M3 steam-export input is required to be zero, so steam leaves the drum outlet only through the canonical main-steam pipe/header topology rather than being removed twice.

Each `TurbineAdmissionTrainDefinition` validates a strict series chain:

```text
main steam header
    ↓
stop valve
    ↓
control valve
    ↓
admission valve
    ↓
turbine inlet node
```

The valves remain ordinary validated M1 `ValveDefinition` components. `MainSteamNetworkSolver` may project their committed-state flow/position diagnostics, but physical transport is integrated only once by `PlantNetworkOrchestrator` together with all M3 source terms.

M4.1 terminates at a replaceable `TurbineAdmissionBoundaryDefinition`. Its temporary sink removes explicitly declared signed mass/energy from the committed turbine-inlet node. M4.2 replaces this boundary with turbine expansion/shaft-power physics while preserving upstream main-steam topology and the M3.8 single-integration contract. See `docs/MAIN_STEAM_NETWORK.md` and ADR 0032.

## Turbine expansion and explicit mechanical rotor state (M4.2)

M4.2 composes over the validated M4.1 `MainSteamNetworkDefinition` rather than creating another steam topology. Every M4.1 turbine-admission boundary feeds exactly one `TurbineStageGroupDefinition`, which targets an existing canonical exhaust fluid node and a defined `TurbineRotorDefinition`.

While M4.2 owns the inlet seam, the temporary M4.1 `TurbineAdmissionBoundaryInput` is required to be zero. `TurbineExpansionSolver` stages conservative inlet-to-exhaust mass transfer and removes only the explicit shaft-work difference from the thermofluid inventory domain. Those balances are passed through the backward-compatible M4.1 supplemental-source seam and still reach exactly one `PlantNetworkOrchestrator` call together with all M3 and M4.1 physics.

Rotor kinetic energy is not represented as a thermal body. `TurbineExpansionState` owns immutable `TurbineRotorState` angular speed separately from `PlantState`. Each deterministic step computes stage driving torque, external mechanical load torque, net torque, candidate speed and kinetic-energy change from committed state. `TurbineMechanicalAudit` closes shaft power against rotor kinetic-energy change plus external load while the inherited plant-network audit closes the equal energy removed from thermofluid inventories.

Overspeed is an observable threshold diagnostic only. An explicit trip command can block stage expansion, but M4.2 does not secretly latch protection state or mutate upstream stop/control/admission valves. Protection sequencing remains an M5 responsibility. See `docs/TURBINE_EXPANSION_AND_ROTOR.md` and ADR 0033.

## Condenser, vacuum and hotwell composition (M4.3)

M4.3 composes directly over the validated M4.2 `TurbineExpansionSystemDefinition`. Every turbine stage group is assigned exactly one `CondenserDefinition`; its `SteamSpaceNodeId` must be the stage group's existing canonical exhaust node. The condenser hotwell is another canonical `FluidNodeDefinition`, so no parallel condensate inventory model is introduced.

`CondenserSystemSolver` evaluates condensation from the committed steam-space/hotwell state and external cooling-boundary capacity. It stages an internal steam-space-to-hotwell mass transfer plus a signed external heat-rejection power term. Those source terms are passed through a backward-compatible M4.2 supplemental-source seam and reach the same single `PlantNetworkOrchestrator` integration as M3, M4.1 and M4.2 thermofluid physics.

Condenser pressure/vacuum is not integrated separately. Initial pressure is read from the committed canonical exhaust node; final pressure is read from the candidate canonical exhaust node after the one network integration and thermodynamic closure. This keeps vacuum coupled to conserved mass/energy inventory.

The cooling-water/environment model remains a replaceable boundary that currently supplies only available heat-rejection power. Hotwell pumping, feedwater conditioning and replacement of the simplified M3 feedwater source are deferred to M4.4. See `docs/CONDENSER_VACUUM_HOTWELL.md` and ADR 0034.

## M4.4 condensate/feedwater closure

M4.4 extends the validated turbine-island stack without introducing a second hydraulic ownership boundary.

`CondensateFeedwaterSystemDefinition` semantically binds each M3 feedwater seam to a path composed entirely from canonical `PlantDefinition` nodes and pumps:

```text
M4.3 hotwell
  -> canonical condensate PumpDefinition
  -> canonical feedwater inventory FluidNodeDefinition
  -> canonical feedwater PumpDefinition
  -> M3 feedwater target / steam-drum inventory
```

The semantic M4.4 solver may invoke `PumpFlowSolver` against committed states for diagnostics, but it does not apply those balances independently. `PlantNetworkOrchestrator` remains the sole owner of pump balance accumulation and conserved fluid integration.

While M4.4 is active, the legacy M3.7 `FeedwaterBoundaryInput` mass flows are required to be zero. This preserves the stable semantic feedwater seam while preventing duplicate external mass addition.

Lumped educational feedwater conditioning is represented as bounded explicit source-term energy on the canonical feedwater-inventory node with matching signed external-power declaration. Detailed extraction-steam heater/deaerator physics remains replaceable future scope.


## M4.5 electrical-domain composition

M4.5 adds a third explicit state domain alongside thermofluid `PlantState` and M4.2 mechanical `TurbineExpansionState`: `GeneratorGridState`. It owns only deterministic electrical phase and breaker state. No electrical value is stored as fake fluid, thermal or rotor state.

`GeneratorGridSystemDefinition` composes over the validated M4.4 `CondensateFeedwaterSystemDefinition` and binds exactly one synchronous generator to every embedded M4.2 turbine rotor. Generator frequency is derived from rotor speed and pole-pair count. Grid and generator phase advance only from committed state and fixed `TimeSpan`, preserving deterministic replay.

Breaker closure is manual-first. A close command is accepted only if committed generator/grid frequency, phase and voltage mismatches lie inside configured synchronization windows. M5 may later add automatic synchronizer/protection logic around the same command/state seam.

M4.5 does not integrate rotor dynamics independently or bypass M4.3/M4.4. `GeneratorGridSolver` rewrites only the nested M4.2 external-load-torque input, then delegates through the full M4.4 → M4.3 → M4.2 stack so condensate/feedwater, condenser, turbine and primary/main-steam composition remain intact before the single plant-network integration. Therefore legacy manually supplied M4.2 external-load torque is required to zero while M4.5 owns that seam.

The energy path is explicit and auditable:

```text
thermofluid shaft extraction
    -> M4.2 shaft power / rotor dynamics
    -> M4.5 mechanical generator input
    -> electrical grid export + generator conversion losses
```

`GeneratorElectricalAudit` exposes closure residuals rather than correcting state. Detailed synchronous-machine transients, excitation, governor/load control, automatic synchronization and protection remain deferred.

## M4.6 integrated secondary-cycle audit boundary

M4.6 introduces `IntegratedSecondaryCycleDefinition` as a top-level composition wrapper over the validated M4.5 generator/grid system. It does not own a new graph or new mutable state. `IntegratedSecondaryCycleSolver` delegates physical evolution to `GeneratorGridSolver`, preserving the existing single `PlantNetworkOrchestrator` thermofluid integration, single M4.2 rotor integration and deterministic M4.5 electrical-state advancement.

The new `SecondaryCycleHeatBalanceAudit` reconciles the existing thermofluid, mechanical and electrical audits. Turbine shaft work is explicitly recognized as an internal cross-domain transfer: M4.2 removes it from thermofluid stored energy and adds it to the rotor domain, so M4.6 cancels it exactly once when deriving the reactor-to-grid external first-law balance. Generator mechanical load is then reconciled with electrical export plus explicit conversion loss.

M4.6 also classifies supplemental thermofluid power into nuclear heat, primary-boundary net exchange, feedwater conditioning, turbine shaft extraction and condenser heat rejection, surfacing any unexplained residual rather than correcting it. Formal reference operating-point and long-run steady-state ownership remains deferred to M4.7.


## M4.7 full-plant steady-state gate

M4.7 introduces `FullPlantState` as a canonical immutable envelope over the existing `PlantState`, `TurbineExpansionState` and `GeneratorGridState`. It does not create a fourth physical state owner. `FullPlantSolver` delegates the complete physical step to the validated M4.6 `IntegratedSecondaryCycleSolver` and only packages candidate cross-domain state plus derived diagnostics.

`FullPlantSnapshot` is the top-level true-state observation boundary intended for M5 instrumentation. It exposes the nested M4.6 snapshot, canonical candidate plant snapshot, candidate mechanical/electrical states, heat-balance audit and derived performance diagnostics. Later sensors/controllers must compose over this boundary rather than introducing UI-side or controller-side physics.

`FullPlantReferenceOperatingPoint` and `FullPlantLongRunRunner` provide the M4 gate verification seam. Fixed manual inputs are propagated without trims or resets; mass, coupled stored energy, rotor speed, electrical output and closure residuals are measured against explicit criteria. Criteria evaluation is observational only and cannot mutate simulation state.

## M5.1 instrumentation and measured-state boundary

M5.1 introduces an observation layer above the validated M4.7 full-plant true-state boundary without adding physical plant ownership.

```text
FullPlantSnapshot
        ↓ stable semantic source ids
InstrumentSignalSourceCatalog
        ↓
InstrumentationSolver + InstrumentationState
        ↓
MeasuredSignalFrame
        ↓
M5.2+ controllers / M6 UI
```

Architecture rules:

- `FullPlantSnapshot` remains the authoritative simulated truth boundary;
- future controllers and operator-facing presentation consume `MeasuredSignalFrame`, not perfect true state directly;
- `InstrumentationState` contains only sensor/filter memory and is not a conserved plant inventory;
- range, scale, lag, validity and quality are deterministic simulation semantics;
- diagnostic snapshots may expose source truth for verification, but controller-facing measured signals do not;
- sensor faults enter as explicit deterministic inputs; M8.1 owns generic deterministic scenario scheduling/lifecycle, while M8.3 owns concrete sensor-fault applicators over the existing M5.1 seam;
- `InstrumentedFullPlantSolver` delegates physical evolution to M4.7 exactly once, then observes the immutable candidate snapshot.

M5.1 does not introduce controller, actuator, trip, SCRAM, alarm or automatic synchronization behavior.


## M5.2 controller and actuator primitive boundary

M5.2 adds deterministic control-algorithm state above the validated M5.1 measurement boundary:

```text
MeasuredSignalFrame -> ControllerSystemSolver -> ControllerOutputFrame -> ActuatorSystemSolver -> ActuatorCommandFrame
```

`ControllerSystemDefinition` references canonical instrumentation channel ids; controller solvers do not accept `FullPlantSnapshot`. `ControllerSystemState` owns only integral/derivative history, mode and last-command memory. `ActuatorSystemState` owns only command-side memory. Neither is a conserved physical state.

Typed actuator commands target existing valve, pump and control-rod seams. M5.3 now supplies the first concrete reactor/primary adapters; turbine/steam/feedwater adapters and their arbitration remain M5.4 responsibilities.


## M5.3 reactor and primary-system control boundary

M5.3 is the first plant-specific consumer of the generic M5.1/M5.2 control chain. Controllers still consume only `MeasuredSignalFrame`; semantic loop definitions bind controller outputs to existing authoritative rod/group or main-circulation pump targets.

Reactor-power control deliberately reuses the validated M2 chain: committed control-rod state → rod-worth/reactivity evaluation → point kinetics → fission-power calibration. A controller never writes MW directly. Commands generated in a step advance rod state for later committed-state physics, preserving the global committed-state rule. Temperature, void, xenon and any manual/external reactivity contribution enter through an explicit non-rod-reactivity seam rather than being recomputed inside the controller layer.

Main-circulation support loops may use measured total pump flow or header pressure rise. Their pump commands replace only canonical `PumpState` operating commands before the one M4.7 plant step; M5.3 does not add another hydraulic graph or integration pass.

`ReactorPrimaryControlledFullPlantSolver` is a thin composition boundary: it evaluates control/neutronics, rewrites only the pre-existing total-fission-power input, applies typed commands to existing state owners, and delegates physical evolution to `FullPlantSolver`. M3 remains authoritative for core/channel spatial deposition.

## M5.4 turbine, steam and feedwater control boundary

M5.4 is the second plant-specific consumer of the M5.1/M5.2 control chain. Secondary-cycle controllers still consume only `MeasuredSignalFrame`. Semantic loop definitions bind their typed outputs to already authoritative M4.1 admission valves and M4.4 condensate/feedwater pumps.

Normal turbine governing targets only control/admission valves. Stop valves are intentionally excluded from the normal process-control topology so M5.5 can own isolation/trip overrides without ambiguous command ownership. Turbine speed or generator electrical load may drive an admission-governor loop; source-drum pressure may drive a separate admission valve in the same series train.

M4.2 originally exposed stage-group steam flow as a manual replaceable seam. In the integrated M5.4 path, that input is derived from the non-negative limiting committed-state projection through the commanded canonical stop/control/admission valve chain. This projection reuses `ValveFlowSolver` only to couple existing seams; `PlantNetworkOrchestrator` remains the sole hydraulic and conserved-inventory integrator.

Drum-level and hotwell-inventory loops replace only canonical feedwater/condensate `PumpState` operating commands before the one physical step. No second pump model, hotwell inventory or feedwater inventory is created.

`TurbineSecondaryControlledFullPlantSolver` composes validated M5.3 reactor/primary control and M5.4 secondary control over the same instrumentation definition and measured frame. Physical actuator targets must be disjoint. Validated M5.3 kinetics rewrites only total fission power, M5.4 rewrites only the existing turbine stage-flow demand seam, and one M4.7 `FullPlantSolver` call remains authoritative for physical evolution.


## M5.5 protection, interlock and SCRAM boundary

M5.5 adds a dedicated logical protection owner above the normal M5.3/M5.4 process-control layers. Protection consumes the same canonical `MeasuredSignalFrame`; it never traverses perfect full-plant true state directly.

`ProtectionSystemState` contains only trip/manual latches. It does not duplicate rod, valve, rotor or breaker physical state. Latching protection functions, non-latching interlocks and reset permissives are evaluated deterministically from measured values and validity.

Protection arbitration has explicit priority over normal control and acts only through already validated seams:

```text
Reactor SCRAM -> M2 control-rod Insert commands
Turbine trip  -> M4.1 stop valves closed + M4.2 TripCommand
Generator trip -> M4.5 breaker-open command
```

Committed-state ordering remains intact: current-step reactor kinetics uses committed rod state; SCRAM advances candidate rod position for subsequent committed-state reactivity. Turbine stage demand is still derived from the canonical valve path and all physical evolution still passes through one M4.7 `FullPlantSolver` step.

Alarm/annunciator latching, acknowledgement and first-out presentation are intentionally not part of protection state and remain M5.6 responsibilities.

## M5.6 alarm and annunciator boundary

M5.6 adds operator-facing annunciator memory above the validated M5.1 measurement and M5.5 protection boundaries without adding a new physical or protection owner.

```text
MeasuredSignalFrame ───────────────┐
                                   ├─> AlarmSystemSolver -> AlarmSystemState
ProtectionSystemSnapshot ──────────┘                         ↓
                                                    immutable alarm/events
```

Measured alarm conditions never fall back to perfect true state. Protection-derived alarms observe already-decided M5.5 function/action/interlock state and cannot initiate or clear protection themselves.

`AlarmSystemState` owns only edge/latch/acknowledgement/first-out/event-sequence memory. Acknowledgement changes annunciator presentation only. Alarm reset may clear only a safe latched alarm and is completely separate from M5.5 protection reset.

First-out groups and activation/clear/acknowledge/reset events use monotonic logical sequence numbers, preserving deterministic replay and providing a stable source for M6 annunciator/timeline views.

`AlarmedProtectedAutomaticFullPlantSolver` delegates to the validated M5.5 protected full-plant step exactly once and advances alarm memory afterwards. M5.6 therefore cannot alter the protected physical candidate state.


## M5.7 integrated automatic-operation boundary

M5.7 composes the validated M5.1–M5.6 owners without introducing a new physical owner. `IntegratedAutomaticOperationState` is an envelope over full-plant physical state, instrumentation/filter memory, the committed measured frame, reactor/primary and turbine/secondary controller state, protection latches and annunciator memory.

The current logical step consumes one committed measured frame for M5.3/M5.4 control, M5.5 protection and M5.6 alarm observation. The protected full-plant path evolves once. Only after that candidate immutable `FullPlantSnapshot` exists does M5.1 instrumentation advance and publish the measured frame for the next logical step. Candidate true state therefore never feeds back retroactively into current-step decisions.

`AutomaticOperationVerificationRunner` is a headless gate tool, not a scenario engine. Explicit finite phases replace immutable input bundles to represent reference hold, setpoint changes, disturbances or protection-matrix cases. Tracking and closure criteria are observational and cannot correct state.

## M6.1 control-room application shell boundary

M6.1 begins the operator-control-room phase by introducing a presentation contract between validated M5.7 automatic-operation state and Avalonia.

```text
M5.7 measured/protection/alarm snapshots
        ↓
Application ControlRoomSnapshot projection
        ↓
IControlRoomSnapshotSource
        ↓
Avalonia ViewModel / View

Operator action
        ↓
ControlRoomCommand
        ↓
IControlRoomCommandDispatcher
        ↓
future runtime coordinator
```

Architecture rules:

- the Avalonia project has no direct project reference to `NuclearReactorSimulator.Simulation`;
- App source must not reference Simulation namespaces directly;
- view models consume presentation snapshots and cannot traverse `FullPlantSnapshot`, `PlantState`, rotor or generator authoritative state;
- run/pause/step and later operator actions dispatch through Application command seams rather than executing solver logic in UI code;
- presentation refresh and rendering budgets never influence deterministic simulation timestep or result ordering;
- M6.1 provides only shell/navigation/workspace hosting; detailed reusable instruments remain M6.2 responsibilities.

## M6.2 reusable instrument/control component boundary

M6.2 builds the reusable operator-control vocabulary on top of the validated M6.1 shell without weakening the UI/application boundary.

```text
Application presentation semantics
        ↓
ControlRoomVisualState
ControlRoomComponentCatalog
        ↓
Avalonia reusable components
        ├── numeric indicator
        ├── linear meter
        ├── status lamp
        ├── toggle switch
        ├── selector
        └── pushbutton
        ↓
M6.3–M6.6 workspace composition
```

Rules:

- visual state is semantic (`Normal`, `Warning`, `Trip`, `Unavailable`) and is supplied by presentation logic rather than inferred from authoritative plant truth inside the control;
- display-only components cannot mutate operator or simulation state;
- interactive components use normal desktop focus/keyboard/pointer semantics and operational bindings leave App through Application command seams;
- unavailable interactive controls are disabled and cannot emit fallback/default commands;
- reusable controls remain in the Avalonia App project while the stable component/interaction catalog remains in Application with no Avalonia dependency;
- rendering and interaction cadence are presentation concerns only and cannot affect deterministic simulation time or results.

See `docs/CONTROL_ROOM_COMPONENT_LIBRARY.md` and ADR 0047.

## M6.3 Reactor/Core presentation boundary

M6.3 creates the first domain-specific control-room panel without changing simulation ownership. `ControlRoomSnapshotProjector` maps the immutable M5.7 automatic-operation snapshot into a `ReactorCorePanelSnapshot` composed only of Application presentation types. Avalonia binds to that projection and never references Simulation or Domain.Physics types directly.

The panel distinguishes **measured instruments** from **model diagnostics**. Reactor thermal power is selected from the candidate M5.1 `MeasuredSignalFrame` by semantic source id and preserves validity/quality semantics. Reactor period, reactivity, rod state and core-zone values are explicitly labelled diagnostics projected at the Application boundary; they are informational only and never become hidden UI-side controller/protection inputs.

Operator actions remain one-way intents: rod insert/hold/withdraw commands carry a canonical target id through `ControlRoomCommand`, while SCRAM and protection-reset intents use the same dispatcher. Views/ViewModels never mutate rods, reactivity, point kinetics or protection state. Runtime-dependent controls are disabled when the operational coordinator is unavailable.

Historically, M2.8 iodine/xenon physics was validated but not included in the M5.7 operational snapshot. The M6.3/M7 v1 presentation therefore correctly exposed xenon as `Unavailable`; presentation code must never reconstruct hidden physical state. See ADR 0048.

M9.3 adds an opt-in versioned integration seam rather than retroactively changing those exact-version configurations. `ReactorPrimaryControlSystemDefinition` may carry the canonical M2.8 `IodineXenonDefinition`; the corresponding `ReactorPrimaryControlState` then carries `IodineXenonState`, and `ReactorPrimaryControlSolver` invokes the existing M2.8 solver. Committed xenon worth is added to the explicit external non-rod-reactivity seam before point kinetics, and the committed poison snapshot is promoted through `ControlRoomSnapshotProjector`. Configurations without the definition remain explicitly unavailable. Existing M7 v1 seeds stay xenon-disabled to preserve M9.1 exact-version replay identity. See ADR 0069 and `ADVANCED_XENON_LOW_POWER_TRANSIENTS.md`.


## M6.4 Primary-Circuit mnemonic boundary

M6.4 projects the canonical M3 primary-circuit topology into presentation-only loop, MCP, channel-group, steam-drum and valve records. Avalonia never traverses `IntegratedPrimaryCircuitSnapshot`, `PlantSnapshot` or Domain.Physics types directly.

Measured instruments and model diagnostics stay distinct:

```text
M5.1 MeasuredSignalFrame
  ├─ loop total pump flow
  ├─ loop header pressure rise
  ├─ steam-drum pressure
  └─ steam-drum level
          ↓
ControlRoomSnapshotProjector
          ├─ preserves measurement validity/quality
          └─ adds explicitly labelled model diagnostics
          ↓
PrimaryCircuitPanelSnapshot
          ↓
Avalonia mnemonic
```

Canonical topology determines which pumps, branches, drums and valves are presented. Valve filtering follows hydraulic endpoint membership in primary-circuit nodes; no synthetic equipment is added for visual completeness. MCP START/RUN and STOP controls emit typed `ControlRoomCommand` pump intents only. M5.3 remains the command/arbitration owner and M3 remains the single authoritative hydraulic/inventory integration boundary.

See `docs/PRIMARY_CIRCUIT_MNEMONICS.md` and ADR 0049.


## M6.5 Turbine, Generator & Electrical presentation boundary

M6.5 projects validated M4/M5 turbine-island and electrical state into two Application-only presentation contracts. Avalonia continues to have no Simulation project/namespace dependency.

```text
M5.7 immutable snapshot
        ↓
ControlRoomSnapshotProjector
        ├─ M5.1 measured instruments
        ├─ labelled M4/M5 diagnostics
        └─ M5.5 trip state
        ↓
TurbineSecondaryPanelSnapshot / ElectricalPanelSnapshot
        ↓
Avalonia display + typed operator intents
```

Measured channels are preferred wherever the canonical M5.1 source catalog exposes them. Main-steam/admission details, stage diagnostics, feedwater-train details, grid reference values and synchronization diagnostics remain explicitly labelled model values rather than masquerading as measured instrumentation.

Operator speed/load commands are command intents only: no MW increment, rpm ramp, governor law or physical response is defined in Avalonia. Breaker-close presentation fails closed when the published synchronization permissive is false, but M4.5 remains the final close-check and breaker-state authority. Turbine/generator trip intents remain subordinate to M5.5 protection ownership.

See `docs/TURBINE_GENERATOR_ELECTRICAL_PANELS.md` and ADR 0050.


## M6.6 deterministic operational-history presentation boundary

M6.6 adds presentation history without adding a new physical or simulation-time owner.

```text
M5.7 immutable snapshot
        ↓
ControlRoomSnapshotProjector
        ├─ process/electrical presentation values
        └─ M5.6 alarm/first-out/events
        ↓
ControlRoomSnapshot
        ↓
ControlRoomOperationalHistoryAccumulator
        ├─ bounded logical-step trends
        └─ bounded sequence-deduplicated event history
        ↓
Avalonia Alarms & Events workspace
```

Trend source configuration references only Application presentation values. Sampling uses `LogicalStep`; observing the same logical step replaces the point so render cadence cannot change history. Missing values remain nullable gaps.

Alarm rows and first-out groups are projections of validated M5.6 state. Avalonia does not re-evaluate thresholds, latches or first-out order. Targeted/bulk ACK and RESET leave through typed Application command seams and address annunciator memory only; they cannot reset M5.5 protection.

Event ordering uses the M5.6 monotonic `Sequence`. M6.6 records the publishing logical step for context but introduces no wall-clock ordering. History buffers are bounded presentation state and cannot influence deterministic simulation results.

See `docs/TRENDS_ALARMS_EVENT_TIMELINE.md` and ADR 0051.


## M6.7 live control-room runtime boundary

`IntegratedAutomaticOperationRuntimeEngine` is the Application adapter over the validated M5.7 solver/state/input chain. `ControlRoomRuntimeCoordinator` owns run/pause/single-step and snapshot publication only. Accelerated execution is cooperatively batch-bounded; every logical step still executes exactly once at fixed `deltaTime`, while `publicationStride` may reduce UI traffic without changing results. Avalonia retains no direct Simulation reference. Production initialized-session creation is owned by the M7.1 exact-version scenario/session framework; concrete operational initial-condition content begins in M7.2.


## M7.1 versioned initial-condition and scenario-session boundary

M7.1 introduces no new physical state owner. Instead it defines how a training scenario selects and reconstructs an already-canonical runtime composition.

```text
ScenarioDefinition
  ├─ scenario metadata
  ├─ descriptive objectives
  ├─ allowed operator command kinds
  └─ InitialConditionReference(id, version)
                ↓ exact resolve only
VersionedInitialConditionRegistry
                ↓
IVersionedInitialConditionFactory
                ↓ fresh canonical IControlRoomRuntimeEngine
ScenarioSessionFactory
  ├─ paused ControlRoomRuntimeCoordinator
  └─ fail-closed ScenarioCommandDispatcher
                ↓
logical-step operator commands / replay
```

Rules:

- initial-condition identity is exact-versioned and immutable; no implicit latest-version fallback is permitted;
- factory implementations reconstruct canonical lower-layer state/definitions through validated seams rather than exposing a generic UI-side state deserializer;
- scenario metadata and objectives are declarative and cannot patch the returned runtime to force an outcome;
- run/pause/single-step are runtime-host controls, while physical/operator actions must be explicitly whitelisted by the scenario;
- Infrastructure owns the JSON scenario representation and schema migration; migration preserves exact initial-condition identity/version and unknown future versions fail closed;
- `ScenarioReplayRunner` reuses the M0 `SimulationCommandTrace<ControlRoomCommand>` and advances one explicit fixed logical step at a time;
- Validated M9.1 provides versioned replay-backed checkpoints/seek; opaque arbitrary private-state dump/restore remains deliberately absent.

M7.1 supplies the validated ownership/framework seam. M7.2 composes its concrete factory in Application, where Simulation composition is already an allowed dependency, while Infrastructure remains responsible for scenario persistence/schema adapters.

See `docs/INITIAL_CONDITIONS_SCENARIO_FRAMEWORK.md` and ADR 0053.

## M7.2 cold-shutdown and pre-start boundary

`ColdShutdownInitialConditionFactory` is a built-in Application-layer composition recipe registered under exact reference `cold-shutdown-pre-start` v1. It constructs fresh canonical Domain/Simulation owners and seeds the validated M5.7 runtime; it is not a second state integrator and it does not expose mutable state to scenario or UI code.

`PreStartupChecklistEvaluator` consumes only immutable `ControlRoomSnapshot` presentation data. `PreStartupGuidancePlan` references named checks and may expose suggested typed operator actions, but it cannot dispatch those actions or advance time. `ScenarioCommandDispatcher` remains the fail-closed permission boundary.

The production desktop composition loads the exact v1 scenario paused through `VersionedInitialConditionRegistry` and `ScenarioSessionFactory`. Avalonia receives snapshot/command/guidance boundaries only. Rod withdrawal and breaker closure are excluded from M7.2 permissions so first criticality and synchronization remain later milestone ownership.

See `docs/COLD_SHUTDOWN_PRESTART.md` and ADR 0054.


## M8.7 safety-response composition boundary

M8.7 is an Application/training composition layer only. `SafetyResponseScenarioPack` reuses exact prior M8 fault declarations and registered applicators; `SafetyResponseCheckpointEvaluator` reads committed presentation snapshots; `SafetyResponseEvaluationSession` combines the M7.7 assessment tracker with the existing accepted-operator-action journal. None of these types may own physical state, protection latches, controller outputs or fault-effect integration.

Acceptance/scoring and debrief timelines are observational and replay-compatible. UI publication cadence, wall clock and random state cannot change checkpoint first-achievement, scoring or operator-action ordering.


## M9.1 recorder/checkpoint/full-replay boundary

M9.1 is an Application/Infrastructure observability and reconstruction layer. `ScenarioRecorder` subscribes to `DeterministicStepCompleted` plus the accepted-action journal and retains one immutable `ControlRoomSnapshot` frame per logical step. It may derive recorder events from committed alarm/fault/protection presentation state but must never feed those events back into simulation.

`ScenarioCheckpoint` schema v1 is a versioned replay anchor containing exact scenario/initial-condition identity, logical step, accepted-action prefix and a versioned snapshot fingerprint. It is not an authoritative physical-state serialization. `ScenarioFullReplayRunner` reconstructs through `ScenarioSessionFactory`, replays accepted operator actions at exact step boundaries, lets M8.1 reconstruct fault lifecycle from scenario data, verifies every frame fingerprint and fails closed at first divergence. Host run/pause state and UI publication stride remain outside physical replay identity.


## M9.2 post-incident analysis boundary

Post-incident analysis is an Application-layer observer over immutable M9.1 `ScenarioRecording` artifacts.

Ownership rules:

- analysis never mutates or integrates physical state;
- analysis never bypasses M9.1 replay/checkpoint verification;
- windows and response latencies use logical steps only, never wall-clock time;
- event order is preserved from the recorder monotonic sequence;
- temporal precedence must not be silently presented as physical causality;
- persisted debrief reports are derived artifacts and are not authoritative simulation state.


## Planned M10 operator-computer and supervisory-automation boundary

M10 introduces two deliberately separate concepts:

```text
Training assistance                     Plant control authority
Hidden / ChecklistOnly / Guided         Manual / Assisted / Supervisory Automatic
        │                                         │
presentation/training only                       physical-operation authority
        └──────── independent axes ───────────────┘
```

The operator computer is an Application aggregation contract with an App presenter. It may project guidance, measured/model-diagnostic information, alarms, contextual command availability, training/control modes, procedure diagnostics, history, recorder/replay/analysis and session lifecycle state. It does not become authoritative for any of them.

Real supervisory automation is owned below the presentation boundary:

```text
OperatorComputer / other operator surface
        ↓ typed intent
Application boundary
        ↓
M5 supervisory coordinator
        ↓
existing controller modes + setpoints + typed canonical commands
        ↓
M5.2/M5.3/M5.4 local controllers
        ↓
canonical actuators → M2/M3/M4 physics
```

Rules:

- App/UI never owns control algorithms, physical setpoints as source-of-truth, deterministic timing or automation state;
- supervisory automation never directly assigns derived plant outcomes;
- measured-signal consumers never fall back silently to true internal state;
- M5.5 protection/interlocks always arbitrate above supervisory normal control;
- requested/effective/degraded mode are explicit; invalid required measurements/equipment availability cause deterministic fail-closed degradation;
- Manual takeover is deterministic and bumpless, reusing M5.2 semantics;
- plant commands, training/presentation intents and session lifecycle intents remain distinct typed seams;
- M10 session persistence packages exact scenario identity + M9.1 recording/checkpoint evidence and restores through replay/fingerprint verification, never opaque private-state dumps;
- terminal navigation is fixed-page/menu based, deterministic and keyboard-operable; no free-form NLP/LLM command interpretation.

See ADR 0070 and `OPERATOR_COMPUTER_SUPERVISORY_AUTOMATION.md`.


## M10.1 operator-computer shell ownership

M10.1 adds a presentation aggregation seam only:

```text
ControlRoomSnapshot
        ↓
OperatorComputerSnapshotProjector
        ↓
immutable OperatorComputerSnapshot
        ↓
OperatorComputerViewModel
        ↓
ControlRoomComputerControl
```

The fixed page set is GUIDANCE / INFO / ALARMS / COMMANDS / MODES / DIAGNOSTICS / LOG / SESSION. Page selection and keyboard focus are App-only presentation state. M10.1 does not own guidance rules, alarms, plant commands, control authority, history, recorder/replay or session persistence. M10.1 established shell-only pages. M10.2 promotes only GUIDANCE, INFO and DIAGNOSTICS through generic immutable Application projections; the other pages remain staged until their owning milestones.


## M10.2 information/guidance/diagnostics projection ownership

M10.2 activates three operator-computer pages without moving their source-of-truth ownership:

```text
M7 guidance plan + canonical checklist evaluator
M6 ControlRoomSnapshot / panel presentation contracts
        ↓
Application generic immutable operator-computer projections
        ↓
App terminal formatting only
```

GUIDANCE never owns procedure rules; DIAGNOSTICS never invents readiness criteria; INFO never reaches into private Simulation state. `Unavailable` remains explicit and is never replaced with zero or true-state fallback. Training assistance remains a presentation-only `TrainingGuidanceMode` axis. Runtime permissives/interlocks remain authoritative even when a checklist diagnostic reports readiness.


### M10.3 alarm/log/incident projection boundary

M10.3 remains presentation-only. `OperatorComputerAlarmLogProjector` may consume canonical M5.6/M6.6 alarm state/history, optional M9.1 `ScenarioRecordingEvent` evidence and optional immutable M9.2 `PostIncidentAnalysisReport` data. It never owns alarm state, history storage, recorder lifecycle, replay restoration or causal inference. Default desktop composition does not auto-enable full `ScenarioRecorder` capture merely to populate the terminal.


### M10.4 contextual command-console boundary

M10.4 remains an Application/App interaction surface. `OperatorComputerCommandConsoleProjector` may derive contextual command rows only from already-published `ControlRoomSnapshot` presentation state. The resulting AVAILABLE/BLOCKED/UNAVAILABLE state is advisory/presentational and is not a new permissive/interlock owner. Exact `ControlRoomCommand` intents are dispatched only through `IControlRoomCommandDispatcher`; scenario/runtime validation remains authoritative and fail-closed. Training-assistance intents and session-lifecycle intents remain separate from `ControlRoomCommandKind`.


## M10.5/M10.6 control-authority and supervisory ownership

Training assistance and physical control authority are independent. `TrainingGuidanceMode` remains presentation/training state; `PlantControlAuthorityMode` is a distinct M5-facing authority seam. Application exposes typed intents and immutable requested/effective/health/per-loop presentation snapshots, but the deterministic `SupervisoryOperationCoordinator` lives in Simulation/M5.

```text
training intent --------------------------> guidance presentation only

plant authority/objective intent
        ↓
Application typed seam
        ↓
M5 SupervisoryOperationCoordinator
        ↓
existing controller modes/setpoints
        ↓
existing M5.3/M5.4 control + canonical actuators/physics
```

Required supervisory feedback comes only from `MeasuredSignalFrame`. Invalid/unavailable required measurements degrade fail-closed; no true-state fallback is allowed. Canonical SCRAM/turbine/generator trips suspend supervisory decisions. Manual takeover uses committed controller `LastOutput` values as manual outputs for deterministic bumpless handover.

Authority/objective changes are not `ControlRoomCommandKind` values. `ScenarioAutomationIntentJournal` records those semantic intents separately and M9.1 replay reapplies them at the `N -> N+1` boundary. This preserves existing snapshot fingerprint v1 while keeping automation-using sessions replayable. See ADR 0074.


## M10.7 replay-backed session persistence

M10.7 does not serialize private solver memory. A persistent session archive is packaging around existing deterministic owners:

```text
exact ScenarioDefinition / InitialCondition version
        +
per-step ControlRoomSnapshot fingerprint evidence
        +
operator-action journal
        +
M10.5/M10.6 automation-intent journal
        +
recorder events + M9.1 checkpoints
        ↓
ScenarioSessionArchive v1
        ↓
ScenarioFullReplayRunner
        ↓
verified reconstructed session
```

Normal desktop startup does not silently enable `ScenarioRecorder`; the operator must explicitly restart as a recorded session before checkpoint/save operations. Load and checkpoint restore always replay exact inputs and verify fingerprints fail-closed. A verified restored prefix may be reattached to `ScenarioRecorder` for continuation, but the recorder verifies scenario identity, logical step, fingerprint and journals before accepting the prefix.

Avalonia owns only file-picker/presentation orchestration. It never restores physics directly.


## M10.9.4 Hotfix 23 turbine-work ownership

Current-v2 turbine stage work remains owned by the M4.2 `TurbineExpansionSolver`. The stage definition may opt into an educational thermodynamic-work closure; legacy/null definitions preserve the historical fixed nominal-specific-work law.

```text
committed inlet temperature
+ committed inlet/exhaust pressure ratio
+ committed inlet vapor mass fraction
        ↓
pressure/temperature available specific work
        ↓ min(nominal design cap, inlet-energy reserve)
effective ideal specific work
        ↓ turbine efficiency + rotor mechanics
extracted shaft work / conservative exhaust energy
```

The closure does not move steam-table physics into Application or the UI. `TurbineStageGroupSnapshot` publishes the committed pressure/temperature availability, inlet-energy bound, effective ideal work and limitation state. Application projects only immutable diagnostics. A future higher-fidelity enthalpy/entropy backend may replace the educational closure without changing ownership or integration seams. See ADR 0090.
