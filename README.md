# Nuclear Reactor Simulator

Educational full-plant nuclear reactor simulator built with C#/.NET 10 and Avalonia.


## Continuing development in a new conversation

Use `docs/PROJECT_HANDOFF.md` as the authoritative current checkpoint and `docs/NEW_CHAT_START.md` as the ready-to-paste restart bootstrap. `docs/README.md` provides the documentation map. Do not infer milestone validation from implementation alone: local build/test success must be explicitly recorded before advancing the roadmap.

## Current validated baseline

**M8.2 — Hydraulic Component Faults hotfix 2 — VALIDATED**

M0, M1, M2, the complete M3 primary-circuit phase, M4.1 through M4.7, M5.1 through M5.7, M6.1–M6.7, M7.1–M7.7, M8.1 and M8.2 hotfix 2 are validated. The M3, M4, M5, M6 and M7 gates are complete. M8.1 owns deterministic fault scheduling/lifecycle and M8.2 adds validated hydraulic component fault effects through canonical seams.

The current implementation candidate is **M8.3 — Instrumentation & Control Faults**. M8.1 deterministic scheduling/lifecycle and M8.2 hydraulic component faults hotfix 2 are validated; M8.3 adds deterministic sensor, controller-output and actuator-command failures through the existing M5 measured-signal/control/protection seams.

M8.2 hotfix 2 also introduced `NuclearReactorSimulator.App.Tests`, a headless regression suite for control-room ViewModel/XAML command-state wiring; M8.3 preserves that boundary and adds no UI-side physics.

## Architectural principles

1. The GUI contains no reactor physics or simulation calculations.
2. Gameplay must emerge from the simulated plant state, not from arbitrary scripted outcomes.
3. The simulation engine uses a deterministic fixed timestep and remains independent from UI refresh and wall-clock time.
4. Simulation state is exposed through immutable snapshot envelopes.
5. Commands execute in monotonic FIFO order only at fixed-step boundaries.
6. Physical states use immutable/copy-on-write semantics across kernel steps.
7. Candidate states must satisfy registered invariants before becoming committed simulation state.
8. Runtime faults fail closed: no partial logical step is reported as committed.
9. Physical quantities use strongly typed immutable value objects with canonical SI storage at model boundaries.
10. Physical fidelity can increase incrementally without rewriting the architecture.
11. The simulation engine remains executable headlessly from automated tests.
12. Multi-component plant steps use staged committed-state solving: components read a common committed state, accumulate balances, and each conserved inventory is integrated exactly once before commit.

## Validated physical foundation and current reactor quantities

Strong value types now cover:

```text
Geometry:       Length, Area, Volume
Matter:         Mass, Density
Thermal:        Temperature, TemperatureDifference
Hydraulic:      Pressure, PressureDifference
Energy:         Energy, SpecificEnergy, Power
Mechanical:     AngularSpeed, Torque, MomentOfInertia
Electrical:     Frequency, ElectricPotential, PhaseAngle, PhaseAngleDifference
Flow:           MassFlowRate, VolumetricFlowRate
Resistance:     QuadraticHydraulicResistance
Heat transfer:  HeatCapacity, ThermalConductance
Reactor:        Reactivity, DelayedNeutronFraction, DecayConstant,
                NeutronPopulation, DelayedNeutronPrecursorPopulation,
                HeatDepositionFraction, DecayHeatGenerationFraction,
                TemperatureReactivityCoefficient, VoidReactivityCoefficient,
                IodineInventory, XenonInventory, XenonReactivityCoefficient
```

Key rules:

- canonical SI storage;
- explicit conversions such as bar/MPa/°C/tonnes/litres;
- no implicit conversion to/from `double`;
- `NaN` and infinities rejected;
- absolute temperature cannot cross 0 K;
- absolute pressure cannot cross vacuum;
- signed differences and directional rates remain signed;
- useful dimensional relationships are encoded explicitly.

Examples:

```csharp
var pressure = Pressure.FromBar(70);
var inlet = Temperature.FromDegreesCelsius(270);
var outlet = Temperature.FromDegreesCelsius(285);
var deltaT = outlet - inlet;
var density = Mass.FromKilograms(998) / Volume.FromCubicMetres(1);
var energy = Power.FromMegawatts(2).Over(TimeSpan.FromSeconds(30));
```

M1.2 adds fluid control-volume primitives:

```text
fixed geometry
+ conserved mass/internal energy
+ pressure/temperature closure
= immutable FluidNodeState
```

`FluidNodeIntegrator` applies signed net mass and energy rates over the fixed step. Density and specific internal energy are derived from conserved state. Pressure and temperature are resolved through `IFluidThermodynamicModel`; no placeholder production equation of state is embedded before M1.7.

M1.3 adds passive hydraulic connections:

```text
FromNode ───── PipeDefinition / PipeFlowSolver ───── ToNode
   ↑                    signed flow                    ↓
   └──────── equal-and-opposite conservative balances ┘
```

The current lumped law is `Δp = R · m_dot · |m_dot|`. Flow reverses naturally when the endpoint pressure difference reverses. The solver advects the upstream node's specific internal energy and returns exactly conservative endpoint balances. Detailed friction correlations and final enthalpy/two-phase transport remain deferred to their dedicated milestones. Valve restriction in M1.4 and active pump pressure in M1.5 both compose over the same hydraulic sign convention and network boundary.

M1.6 adds lumped thermal bodies and passive heat-transfer links. Thermal bodies conserve stored energy and derive temperature from constant heat capacity. Internal thermal links evaluate signed heat flow from temperature difference and return exactly equal-and-opposite endpoint balances. Fluid nodes receive thermal power through their existing conserved energy balance, while external heat sources remain explicit system-boundary inputs.

M1.7 finally supplies a production `SimplifiedWaterSteamThermodynamicModel`. Conserved mass, fixed volume and conserved internal energy now resolve deterministically to pressure, temperature and phase. Saturated mixtures expose vapor quality, while subcooled-liquid and superheated-vapor states remain explicit. The implementation uses a Region-4 saturation-pressure reference with deliberately simplified educational correlations and remains replaceable behind `IFluidThermodynamicModel`.



## M3 plant composition and network orchestration

M3.1 provides the validated structural boundary:

```text
PlantDefinition
    + canonical topology registries
    + validated hydraulic/thermal references
        ↓
PlantState
    + exact complete state ownership
        ↓
PlantSnapshot
```

M3.2 composes the validated M1 hydraulic/thermal primitives without adding new equations:

```text
Committed PlantState
        ↓
solve pipes / valves / pumps / heat links / sources
from the same committed endpoint state
        ↓
accumulate canonical balances
        ↓
integrate each fluid/thermal inventory exactly once
        ↓
PlantNetworkAudit
        ↓
candidate PlantState
```

The orchestrator exposes global mass/energy closure diagnostics and keeps pump hydraulic work plus explicit heat-source power visible as system-boundary energy inputs. It never silently corrects state to force conservation.

## M2 reactor-physics foundation

M2.1 introduces signed `Reactivity` with canonical `delta-k/k` storage and explicit `% delta-k/k` and `pcm` conversions. Named contributions remain independently observable:

```text
Control rods
Fuel temperature
Coolant temperature
Void
Xenon
Other
       │
       ▼
ReactivityModel
       │
       ├── canonical diagnostic breakdown
       └── total reactivity rho
```

The model only composes reactivity. It does **not** convert reactivity directly into neutron flux or thermal power; that dynamic response belongs to M2.3 neutron kinetics.

M2.2 adds the first concrete source of `ControlRods` reactivity:

```text
rod/group command
      ↓
insert / withdraw / hold
      ↓
deterministic mechanical motion
      ↓
normalized withdrawal position
      ↓
integral worth curve
      ↓
named ControlRods reactivity contribution
```

`0%` withdrawal means fully inserted and `100%` means fully withdrawn. Motion commands persist until changed or a mechanical endpoint is reached. Group commands fan out to rods; later commands in the same fixed step deterministically override earlier commands for overlapping targets.

M2.3 makes total reactivity dynamic through plant-independent point kinetics:

```text
ReactivityModel total rho
      +
PointKineticsParameters (Λ, β_i, λ_i)
      ↓
PointKineticsSolver
      ↓
normalized neutron population + precursor populations
      ↓
reactor-period / prompt-critical diagnostics
```

The generic engine hardcodes no RBMK kinetic constants. The point-kinetics state is initialized explicitly at critical equilibrium when requested, and deterministic bounded RK4 internal substeps preserve the runtime's external fixed-step contract.

M2.4 now owns the explicit neutron-to-thermal boundary:

```text
NeutronPopulation
      +
FissionPowerCalibration
      ↓
FissionPowerSolver
      ↓
instantaneous fission thermal power
      ↓
canonical heat-deposition partition
      ↓
fuel / structures / coolant energy balances
```

The generic engine still hardcodes no plant nominal power. Fission power scales from a configuration-supplied reference population/reference power pair. The complete heat-deposition partition must sum to unity, and the solver closes the total power exactly across named destinations. M2.4 remains the validated direct-fission thermal source boundary; stateful decay heat is implemented separately in M2.5.

M2.5 adds an explicitly stateful decay-heat boundary:

```text
Fission thermal power history
      ↓
equivalent decay-energy groups
      ↓
latent stored decay energy
      ↓ radioactive/equivalent first-order release
decay heat power
      ↓
fuel / structures / coolant energy balances
```

Each configured group obeys `dE/dt = f · P_fission - λE`. The solver uses the analytic finite-step solution for constant fission power over each fixed timestep, so group buildup, shutdown decay and emitted energy are deterministic and independent from UI cadence. The same-step thermal integrators receive **average emitted decay power**, while diagnostics expose **end-of-step instantaneous decay power**. Group coefficients and the deposition partition are configuration data; no RBMK-specific decay-heat constants are hardcoded.

The M2.4 fission-power path and M2.5 decay-heat path remain separate source terms. M2.5 treats current fission power as the driver that creates latent radioactive-decay energy inventory; emitted decay heat persists after the direct fission source collapses.

M2.6 adds explicit temperature-reactivity feedback without bypassing the existing reactivity/kinetics boundary:

```text
committed fuel/coolant temperature
        ↓
alpha_T * (T - T_ref)
        ↓
named FuelTemperature/CoolantTemperature contribution
        ↓
ReactivityModel → PointKineticsSolver
        ↓
fission/decay heat → next thermal state
```

The generic engine hardcodes neither coefficient sign nor magnitude. M2.6 intentionally evaluates committed temperatures once per fixed step rather than introducing hidden same-step multiphysics iteration.

M2.7 adds the thermohydraulic-to-neutronic void boundary:

```text
committed water/steam phase state
        ↓
VaporQuality (mass fraction)
        ↓ density-aware conversion for saturated mixtures
VoidFraction (volume fraction)
        ↓
alpha_void * (void - void_ref)
        ↓
named Void reactivity contribution
        ↓
ReactivityModel → PointKineticsSolver
```

Subcooled liquid maps to zero void, superheated vapor to full void, and saturated mixtures use liquid/vapor saturation densities to convert quality into homogeneous-equilibrium volumetric void. The coefficient sign and magnitude remain plant configuration; the generic engine hardcodes no RBMK-specific void worth.

M2.8 adds history-dependent iodine/xenon poisoning:

```text
fission thermal power
      ↓
I-135 production ── decay ──► Xe-135
                               │
neutron population ────────────┴── burnup
                               ↓
                    named Xenon reactivity
                               ↓
                     ReactivityModel
```

The reduced model keeps explicit normalized I-135 and Xe-135 inventories and integrates the coupled linear equations analytically over each fixed step. Production rates, decay constants, burnup coefficient and xenon worth are all configuration data; no RBMK-specific poison constants are embedded in the generic engine.

## Solution structure

```text
src/
  NuclearReactorSimulator.Domain
  NuclearReactorSimulator.Simulation
  NuclearReactorSimulator.Application
  NuclearReactorSimulator.Infrastructure
  NuclearReactorSimulator.App

tests/
  NuclearReactorSimulator.Domain.Tests
  NuclearReactorSimulator.Simulation.Tests
  NuclearReactorSimulator.Application.Tests
  NuclearReactorSimulator.Infrastructure.Tests
```

Dependency direction:

```text
Domain
  ↑
Simulation
  ↑
Application
  ↑
App

Infrastructure → Application / Domain
App            → Application / Simulation / Infrastructure
```

`App` is the composition root and the only production project allowed to reference Avalonia packages.

## Deterministic runtime carried forward from M0

- deterministic fixed-timestep `SimulationClock`;
- generic headless simulation runtime;
- pause/resume/single-step and exact speed multipliers;
- FIFO command queue with monotonic sequence numbers;
- immutable snapshots;
- transactional step commit;
- invariant validation;
- terminal fault diagnostics;
- logical-step command traces and deterministic replay;
- reusable headless scenario harness;
- long-run determinism verification;
- architecture guard against wall-clock/timer APIs in `Simulation`.

## Toolchain

- .NET 10
- Avalonia 12.1.0
- xUnit.net v3 3.2.2
- Central Package Management
- Nullable reference types enabled
- Warnings treated as errors
- Latest .NET analyzers enabled
- Deterministic compilation enabled

## Validate locally

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

Or run:

```powershell
./eng/verify.ps1
```

## M1.7 validated water/steam model

M1.7 closes the first physical-foundation roadmap with a simplified deterministic water/steam phase closure. It is explicitly an educational approximation rather than a complete IAPWS-IF97 implementation. Saturation pressure follows the Region-4 reference relation; compact correlations resolve liquid, saturated-mixture and superheated-vapor states from the existing conserved fluid inventory.

The abstraction boundary remains unchanged:

```text
FluidNodeIntegrator
        ↓
IFluidThermodynamicModel
        ↓
SimplifiedWaterSteamThermodynamicModel
```

## Documentation

- `docs/ARCHITECTURE.md` — architecture and runtime boundaries
- `docs/PHYSICAL_QUANTITIES.md` — physical unit conventions from M1.1 onward, including M4.2 rotational-mechanical quantities
- `docs/FLUID_NODES.md` — fluid control-volume and conservation model introduced in M1.2
- `docs/PIPES_AND_FLOW.md` — passive pipe, resistance and conservative transport model introduced in M1.3
- `docs/VALVES.md` — valve positions, characteristics, fail-safe semantics and resistance modulation introduced in M1.4
- `docs/PUMPS.md` — active pressure source, affinity laws, hydraulic work and shaft demand introduced in M1.5
- `docs/HEAT_TRANSFER.md` — lumped thermal inertia, conservative heat transfer and external heat sources introduced in M1.6
- `docs/WATER_STEAM_MODEL.md` — simplified water/steam closure, phase semantics, limits and replacement boundary introduced in M1.7
- `docs/REACTIVITY_MODEL.md` — signed reactivity, contribution categories and deterministic composition introduced in M2.1
- `docs/CONTROL_RODS.md` — rod/group mechanics, commands, limits and integral worth mapping introduced in M2.2
- `docs/NEUTRON_KINETICS.md` — point kinetics, delayed-neutron groups, numerical integration and diagnostics introduced in M2.3
- `docs/THERMAL_POWER.md` — neutron-to-fission-power calibration and deterministic heat-deposition partition introduced in M2.4
- `docs/DECAY_HEAT.md` — stateful equivalent-group decay-energy inventory, shutdown release and deposition model introduced in M2.5
- `docs/TEMPERATURE_FEEDBACK.md` — signed reference-temperature feedback and deterministic thermal-neutronic coupling introduced in M2.6
- `docs/VOID_FEEDBACK.md` — vapor-quality/void separation and thermohydraulic-neutronic coupling introduced in M2.7
- `docs/IODINE_XENON_DYNAMICS.md` — stateful I-135/Xe-135 production, decay, burnup and poisoning reactivity introduced in M2.8
- `docs/PROJECT_STATUS.md` — validated capability map, open boundaries and phase gates after M2 closure
- `docs/PROJECT_HANDOFF.md` — compact versioned continuity brief for starting a new project chat without losing the validated baseline and next-step context
- `docs/PRIMARY_CIRCUIT_PLAN.md` — detailed M3 decomposition and integration strategy for the RBMK-like primary circuit
- `docs/PLANT_COMPOSITION.md` — M3.1 canonical plant definition/state/topology and snapshot boundaries
- `docs/PLANT_NETWORK_ORCHESTRATION.md` — M3.2 committed-state staged network solve, balance accumulation and global audits
- `docs/CORE_ZONE_MODEL.md` — M3.3 configurable aggregated core-zone projection and local diagnostics
- `docs/FUEL_CHANNEL_GROUPS.md` — M3.4 equivalent channel groups, zonal power routing and staged nuclear heat source terms
- `docs/MAIN_CIRCULATION_SYSTEM.md` — M3.5 canonical MCP/header/channel/return circulation composition
- `docs/STEAM_DRUMS.md` — M3.6 dedicated drum inventory, separation, steam outlet and liquid recirculation model
- `docs/PRIMARY_CIRCUIT_BOUNDARIES.md` — M3.7 feedwater/steam external boundary contracts and signed mass/energy accounting
- `docs/INTEGRATED_PRIMARY_CIRCUIT.md` — M3.8 committed-state integration, plant snapshot, operating-point and long-run verification contracts
- `docs/MAIN_STEAM_NETWORK.md` — M4.1 main-steam topology, admission-valve chain and replaceable turbine boundary
- `docs/TURBINE_EXPANSION_AND_ROTOR.md` — M4.2 conservative turbine expansion, shaft work, rotor state/dynamics and trip/overspeed seams
- `docs/CONDENSER_VACUUM_HOTWELL.md` — M4.3 condenser condensation, vacuum/pressure, hotwell inventory and cooling-boundary accounting
- `docs/CONDENSATE_FEEDWATER_TRAIN.md` — M4.4 canonical condensate/feedwater return, pump ownership, feedwater inventory and thermal-conditioning accounting
- `docs/GENERATOR_GRID_SYNCHRONIZATION.md` — M4.5 deterministic generator/grid coupling, manual synchronization and electrical audit
- `docs/SECONDARY_CYCLE_HEAT_BALANCE.md` — M4.6 integrated reactor-to-grid first-law and closed-loop mass audit
- `docs/FULL_PLANT_STEADY_STATE.md` — M4.7 full-plant state/snapshot, reference operating point, drift gate and performance diagnostics
- `docs/INSTRUMENTATION_SIGNAL_MODEL.md` — M5.1 measured-signal separation, range/scaling, lag, quality and fault seams
- `docs/CONTROLLER_ACTUATOR_PRIMITIVES.md` — M5.2 measured-signal-only P/PI/PID behavior, manual/auto transfer, anti-windup and typed actuator command seams
- `docs/ROADMAP.md` — approved and granular M0–M9 roadmap
- `docs/milestones/` — milestone definitions and acceptance checklists
- `docs/adr/` — Architecture Decision Records
- `docs/research/REFERENCES.md` — initial research references

## Milestone status

M0.1–M0.3 are **validated** through the carried-forward local suite.

M1.1 and hotfix M1.1.1 are **validated**.

M1.2 and hotfix M1.2.1 are **validated**.

M1.3 is **validated**.

M1.4 is **validated**.

M1.5 is **validated**.

M1.6 is **validated**.

M1.7 is **validated**, closing M1.

M2.1 is **validated**.

M2.2 is **validated**.

M2.3 is **validated**.

M2.4 is **validated**.

M2.5 is **validated**.

M2.6 is **validated**.

M2.7 and hotfix M2.7.1 are **validated**.

M2.8 is **validated**, closing M2 — Reactor Physics.

M2.8.1 is a documentation/roadmap consolidation baseline: it changes no simulation physics and establishes the detailed M3–M9 execution plan.

M3.1–M3.8, M4.1–M4.7, M5.1–M5.7, M6.1–M6.7, M7.1–M7.7, M8.1 and M8.2 hotfix 2 are **validated**; the M3, M4, M5, M6 and M7 gates are complete. M8.3 is the current **baseline candidate**, adding concrete deterministic instrumentation/control fault effects over canonical M5 seams.


## Generator, grid and synchronization physics (M4.5)

M4.5 introduces a separate deterministic electrical state without moving ownership of turbine mechanical state or plant thermofluid inventories. The `GeneratorGridSystemDefinition` composes over the validated M4.4 condensate/feedwater system; every embedded M4.2 turbine rotor is coupled to exactly one `SynchronousGeneratorDefinition` and a validated breaker identifier.

Generator electrical frequency is derived from rotor angular speed and pole-pair count. Grid and generator phase are explicit fixed-step state, never wall-clock derived. A manual breaker-close command is accepted only when configured frequency, phase and line-voltage mismatch windows are satisfied; rejected closes remain observable diagnostics.

While M4.5 owns rotor loading, legacy M4.2 manual external-load torque must be zero. Requested electrical power is converted into electromagnetic load torque and fed through the existing M4.2 rotor integrator. `GeneratorElectricalAudit` then reconciles actual mechanical input power with electrical export and explicit conversion losses. Automatic excitation, governing, synchronization and electrical protection remain deferred to M5. See `docs/GENERATOR_GRID_SYNCHRONIZATION.md` and ADR 0036.

## Integrated secondary-cycle heat balance (M4.6)

M4.6 adds `IntegratedSecondaryCycleDefinition` and `IntegratedSecondaryCycleSolver` as a thin top-level composition boundary over M4.5. Physical state evolution still occurs only in the already validated plant-network, rotor and electrical owners.

`SecondaryCycleHeatBalanceAudit` reconciles nuclear heat and other modeled energy inputs against condenser rejection, rotor stored-energy change, electrical export and generator conversion losses. Turbine shaft work is treated as an internal thermofluid-to-mechanical transfer and cancels exactly once in the full reactor-to-grid balance. Raw mass, power-classification and cross-domain closure residuals remain visible; no corrective bookkeeping is introduced. See `docs/SECONDARY_CYCLE_HEAT_BALANCE.md` and ADR 0037.


## Full-plant steady-state baseline (M4.7)

M4.7 wraps the validated M4.6 stack in a canonical `FullPlantState` and `FullPlantSnapshot` without duplicating any physical state or integration. A fixed-input `FullPlantReferenceOperatingPoint` can be executed headlessly by `FullPlantLongRunRunner`, which reports raw mass, coupled-energy, rotor-speed, electrical-output and first-law closure drift against explicit criteria. `FullPlantPerformanceDiagnostics` derives gross efficiency and heat-rate values only from audited reactor-to-grid powers. No automatic controllers or hidden steady-state trim logic are introduced.

## Instrumentation and measured-signal boundary (M5.1)

M5.1 observes the immutable M4.7 `FullPlantSnapshot` through stable semantic `InstrumentSignalSource` seams and publishes a separate `MeasuredSignalFrame`. Future controllers and operator displays consume measured channel IDs, values, validity and quality rather than traversing perfect physical truth directly. `InstrumentationState` stores only deterministic sensor/filter memory. Range clamping remains observable, first-order lag uses fixed simulation time, and sensor faults are explicit deterministic inputs rather than hidden randomness.


## Controller and actuator primitives (M5.2)

M5.2 consumes only `MeasuredSignalFrame`: controller definitions bind to measured channel ids and never traverse `FullPlantSnapshot`. Deterministic P/PI/PID execution supports manual/automatic modes, bounded outputs, conditional-integration anti-windup, bumpless manual-to-auto transfer, and explicit behavior when measurements are invalid/unavailable. `ActuatorCommandFrame` translates controller outputs into typed valve-position, pump-speed/run and control-rod motion commands. Command memory remains separate from physical valve/pump/rod state; applying these commands to specific plant loops begins in M5.3/M5.4.

## M5.3 reactor and primary-system control boundary

M5.3 is the first plant-specific automatic-control composition. Controllers still consume `MeasuredSignalFrame` only; typed commands are then adapted to existing physical owners. Reactor-power control drives canonical M2 rods, committed rod reactivity plus explicit non-rod reactivity advances the validated point-kinetics state, and the resulting fission power replaces only the existing M3 total-fission-power seam. Main-circulation support loops may regulate measured loop flow or header pressure rise and command only canonical MCP `PumpState` values before the one M4.7 plant step. See `docs/REACTOR_PRIMARY_CONTROL_LOOPS.md` and ADR 0041.

## M5.4 turbine, steam and feedwater control boundary

M5.4 is the second plant-specific automatic-control composition. Controllers still consume only `MeasuredSignalFrame`. Turbine-speed or generator-load and source-drum pressure loops command canonical M4.1 control/admission valves; drum-level and hotwell-inventory loops command only canonical M4.4 feedwater/condensate `PumpState`. The existing M4.2 manual stage-flow seam is replaced in the M5.4 integrated path by the non-negative limiting projection through the commanded stop/control/admission valve chain, without adding a second hydraulic integrator. `TurbineSecondaryControlledFullPlantSolver` composes validated M5.3 reactor/primary control and M5.4 secondary control over one measured frame and one M4.7 physical step. See `docs/TURBINE_STEAM_FEEDWATER_CONTROL_LOOPS.md` and ADR 0042.

## M5.5 protection, interlocks and trip boundary

M5.5 evaluates deterministic trip/interlock logic only from `MeasuredSignalFrame`. Latching trip functions support high/low thresholds, reset hysteresis and explicit invalid-measurement fail-closed behavior. Protection has priority over normal M5.3/M5.4 commands but acts only through validated physical seams: SCRAM issues M2 rod insert commands, turbine trip closes M4.1 stop valves and asserts M4.2 `TripCommand`, and generator trip asserts the M4.5 breaker-open command. Non-latching interlocks can inhibit rod withdrawal, turbine admission opening and generator breaker close. Reset is explicit and measured-permissive-gated. See `docs/PROTECTION_INTERLOCKS_TRIPS_SCRAM.md` and ADR 0043.



## M5.6 alarms and annunciator boundary

M5.6 adds operator-facing alarm memory without changing plant or protection ownership. `AlarmSystemSolver` observes canonical M5.1 measured channels and M5.5 protection snapshots, supports non-latching and latched-until-reset alarms, keeps acknowledgement separate from physical protection reset, assigns deterministic first-out ownership and emits monotonic logical activation/clear/acknowledge/reset events. `AlarmedProtectedAutomaticFullPlantSolver` runs the validated M5.5 protected physical path once and advances alarm state observationally afterwards. See `docs/ALARMS_ANNUNCIATOR_STATE.md` and ADR 0044.

## M5.7 integrated automatic-operation boundary

M5.7 adds `IntegratedAutomaticOperationSolver`, which uses the committed `MeasuredSignalFrame` for the current control/protection decision, executes the protected full-plant path once, then advances M5.1 instrumentation from the candidate immutable true-state snapshot to produce the measured frame for the next logical step. `AutomaticOperationVerificationRunner` executes explicit immutable verification phases for reference operation, setpoint changes, disturbance inputs and protection/interlock cases, with observational tracking/mass/energy/signal/alarm criteria and no hidden state correction. See `docs/INTEGRATED_AUTOMATIC_OPERATION.md` and ADR 0045.

## M6.1 control-room application shell

M6.1 moves the project into the operator-control-room phase without moving physics into Avalonia. The App consumes a narrow `ControlRoomSnapshot` presentation contract, exposes stable Overview/Reactor/Primary/Turbine-Secondary/Electrical/Alarms workspaces and dispatches shell commands through `IControlRoomCommandDispatcher`. The Avalonia project no longer directly references the Simulation project. Presentation refresh/visibility budgets are explicit UI concerns only and never influence deterministic simulation cadence. See `docs/CONTROL_ROOM_APPLICATION_SHELL.md` and ADR 0046.

## M6.2 reusable instrument and control components

M6.2 defines one semantic presentation vocabulary across the future control room: `Normal`, `Warning`, `Trip` and `Unavailable`. Reusable Avalonia primitives now cover numeric indicators, linear meters, status lamps, toggle switches, selectors and pushbuttons. Application owns the component/interaction semantics without depending on Avalonia; concrete controls remain in App and never calculate physical thresholds or execute simulation physics. The shell includes a component gallery for visual validation before M6.3 begins the Reactor/Core panel. See `docs/CONTROL_ROOM_COMPONENT_LIBRARY.md` and ADR 0047.


## M6.3 reactor/core control-room panel

M6.3 composes the validated M6.1 shell and M6.2 reusable controls into the first domain-specific operator workspace. Reactor thermal power is projected from the M5.1 measured frame with validity/quality semantics; reactor period, reactivity, rod state and aggregated core-zone values are explicitly labelled model diagnostics projected by Application rather than read directly by Avalonia. The panel exposes canonical rod target selection and insert/hold/withdraw command intents plus SCRAM/protection-reset seams and M5.5 protection/interlock status. M2.8 xenon physics remains validated but is shown `Unavailable` because xenon state is not yet promoted into the M5.7 automatic-operation snapshot; no UI-side value is fabricated. See `docs/REACTOR_CORE_CONTROL_ROOM_PANEL.md` and ADR 0048.


## M6.4 primary-circuit mnemonics

M6.4 turns the Primary Circuit workspace into a topology-aware process mimic. Existing M5.1 channels provide measured loop total flow/header ΔP and steam-drum pressure/level; non-instrumented header, MCP, branch, drum and boundary values are explicitly labelled model diagnostics. Canonical valves are shown only when hydraulically connected to primary-circuit nodes. MCP START/RUN and STOP controls emit typed Application pump intents only; M5.3 remains command owner and M3 remains the sole hydraulic/inventory integrator. See `docs/PRIMARY_CIRCUIT_MNEMONICS.md` and ADR 0049.

## M6.5 turbine, generator & electrical panels

M6.5 completes the principal turbine-island and electrical operating workspaces over validated M4/M5 ownership. Measured rotor, condenser and generator instrumentation remains distinct from explicitly labelled model diagnostics. Speed/load, breaker and trip controls emit typed Application intents only, while synchronization and breaker authority remain in M4.5/M5.5. See `docs/TURBINE_GENERATOR_ELECTRICAL_PANELS.md` and ADR 0050.

## M6.6 trends, alarms & event timeline

M6.6 completes the historical/annunciator presentation surface without adding a second notion of simulation time. Configured trend series sample only immutable `ControlRoomSnapshot` values by logical step; duplicate publication of the same step replaces that sample instead of creating refresh-dependent history. M5.6 annunciator, first-out and alarm-event state is projected into Application-only records, while ACK/RESET leave Avalonia as typed intents that cannot reset M5.5 protection. The event timeline is bounded and ordered by the validated monotonic alarm-event sequence, never by wall-clock time. See `docs/TRENDS_ALARMS_EVENT_TIMELINE.md` and ADR 0051.


## M6.7 control-room integration & performance baseline

M6.7 adds `IntegratedAutomaticOperationRuntimeEngine` and `ControlRoomRuntimeCoordinator`, completing the operator path from Avalonia command intents to the validated M5.7 runtime and back to immutable presentation snapshots. Accelerated execution is split into bounded cooperative batches; presentation publication may be sparse without skipping deterministic simulation steps. The desktop does not invent state in Avalonia: validated M7.1 supplies exact-version scenario/session ownership, validated M7.2 supplies the cold-shutdown/pre-start recipe, validated M7.3 composes the pre-criticality/source-range handoff, and M7.4 adds the warm steam-raising/turbine-startup handoff through those boundaries. See `docs/CONTROL_ROOM_INTEGRATION_PERFORMANCE.md` and ADR 0052.

## M7.1 versioned initial conditions & scenario framework

M7.1 adds exact `(InitialConditionId, Version)` references, registered factories, versioned scenario JSON migration, declarative objectives/allowed-action metadata, fail-closed command gating, fresh paused session loading and deterministic logical-step replay over the M0 command-trace seam. It introduces no new physical state owner and no implicit latest-version resolution. See `docs/INITIAL_CONDITIONS_SCENARIO_FRAMEWORK.md` and ADR 0053.

## M7.2 cold shutdown & pre-startup

M7.2 registers `cold-shutdown-pre-start` v1 as a deterministic construction recipe over canonical M1–M5 owners. The scenario starts paused with zero modeled fission power, fully inserted modeled rods, main/condensate/feedwater pumps stopped, steam-admission valves closed, turbine stationary and generator breaker open. Pre-start readiness is evaluated only from `ControlRoomSnapshot`; guidance is declarative and may suggest operator commands but never dispatches them automatically. M7.2 hotfix 1 is locally validated. See `docs/COLD_SHUTDOWN_PRESTART.md` and ADR 0054.



## M7.4 heat-up, steam raising & turbine startup

M7.4 adds exact `low-power-steam-raising` v1 as the warm critical handoff for startup training. The factory reuses the canonical M7.2 construction path, seeds low-power critical kinetics, establishes main circulation and a 120 °C warm primary/steam condition, and records a versioned startup lineup with stop/admission availability while the governing control valve remains closed. Turbine roll uses only `TurbineSpeedRaise/Lower` through the validated M5.4 controller/actuator seam; scenario checks remain observational over `ControlRoomSnapshot`. Generator-breaker close and generator-load commands remain fail-closed for M7.5 synchronization/loading. See `docs/HEAT_UP_STEAM_RAISING_TURBINE_STARTUP.md` and ADR 0056.

## M7.3 first criticality & low-power operation

M7.3 adds exact `pre-criticality-source-range` v1 as the prepared handoff for first-criticality training. The factory reuses the validated M7.2 construction path, starts main circulation established and provides a tiny deterministic non-zero neutron-population seed required by the homogeneous M2 point-kinetics equations. Rod INSERT/HOLD/WITHDRAW remains operator-commanded through the validated M5.3 seam; scenario permissions continue to block turbine acceleration, generator-breaker closure and load control. Observational guidance covers reactivity approach, first criticality, a 0.01–5 MWth educational low-power band and reactor-period stabilization. Quantitative xenon remains explicitly unavailable because canonical M2.8 state is not yet in the M5.7 operational envelope; M7.3 does not synthesize it. See `docs/FIRST_CRITICALITY_LOW_POWER.md` and ADR 0055.


## M7.5 grid synchronization and initial load

M7.5 seeds a breaker-open 3000 rpm phase-matched handoff and relies exclusively on the published M4.5 synchronization permissive for closure. Generator load raise/lower changes canonical requested electrical power in bounded increments; M4.5 owns electromagnetic loading, M5.4 owns speed-governor steam admission, and M2/M5.3 own reactor-power response. See `docs/GRID_SYNCHRONIZATION_LOAD_INCREASE.md` and ADR 0057.


## M7.6 power manoeuvring and normal shutdown

M7.6 starts from exact `stable-low-load-parallel-operation` v1 with the generator already paralleled at a bounded 5 MWe requested load. Load raise/lower changes only canonical M4.5 requested electrical power; reactor response remains M2/M5.3 rod-reactivity-kinetics ownership and turbine governing remains M5.4 ownership. Guidance observes published fuel/coolant temperature and void diagnostics while preserving quantitative xenon as explicitly unavailable at the current M5.7 operational boundary. Normal shutdown is ordered as unload → breaker open → controlled rod insertion → turbine rundown → continued main circulation. See `docs/POWER_MANOEUVRING_NORMAL_SHUTDOWN.md` and ADR 0058.


## M7.7 training objectives, procedure guidance & evaluation

M7.7 closes the M7 gate with deterministic first-achievement checkpoints observed on every fixed step, a journal of scenario-accepted operator actions, optional Hidden/ChecklistOnly/Guided assistance and observational objective scoring/penalties. Training state never mutates physics, controls, protection or alarms, and sparse UI publication cannot change evaluation results. See `docs/TRAINING_OBJECTIVES_GUIDANCE_EVALUATION.md` and ADR 0059.

## M8.1 deterministic fault-injection framework

M8.1 adds explicit `ScenarioFaultDefinition` entries to versioned scenario schema v2 and is validated. M8.2 hotfix 2 is validated and adds pump trip/degradation, valve fail/stuck behavior, valve-controlled path restriction/blockage and selected node leaks through typed runtime seams. M8.3 adds M5.1 sensor bias/freeze/failure modes plus bounded controller-output and actuator-command faults without true-state fallback or direct physical writes. See `docs/DETERMINISTIC_FAULT_INJECTION_FRAMEWORK.md`, `docs/HYDRAULIC_COMPONENT_FAULTS.md`, `docs/INSTRUMENTATION_CONTROL_FAULTS.md`, ADR 0060–0062.
