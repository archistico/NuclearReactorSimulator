## M10.9.4 Hotfix 20 Fix 2 — Meaningful Secondary Protection Set / Physical Frequency Regression Contract — IMPLEMENTATION CANDIDATE

- Test-only correction: the current-v2 protection bootstrap regression no longer requires an exact literal 50.000000 Hz after deterministic seed preconditioning. It now derives expected generator frequency from the committed turbine rotor angular speed through `SynchronousGeneratorDefinition.ElectricalFrequencyAt(...)`, asserts measured telemetry matches that physical value, and separately requires the healthy operating point to remain within 49.9–50.1 Hz. No production code, protection thresholds/actions, plant physics or replay semantics changed.

## M10.9.4 Hotfix 20 Fix 1 — Meaningful Secondary Protection Set / Initial Measured-Frame Completeness — SUPERSEDED BY FIX 2

- Fixes the current-v2 bootstrap contract after Hotfix 20 added `condenser-pressure` and `generator-frequency` channels without adding matching initial measured signals. `MeasuredSignalFrame` again contains exactly one signal per instrumentation channel from logical step 0.
- Initial `condenser-pressure` is seeded from the committed `exhaust` node pressure and `generator-frequency` from `SynchronousGeneratorDefinition.ElectricalFrequencyAt(initial rotor speed)`, preventing fabricated bootstrap values and false protection trips.
- Adds a direct regression comparing instrumentation-channel IDs with initial measured-frame signal IDs and checking physically safe initial condenser pressure / 50 Hz generator frequency. No protection threshold/action or plant physics changes.
- Promotes Hotfix 19 to the latest validated structural checkpoint after the user confirmed compilation, the ordinary suite and both explicit 60-second gameplay journeys all pass.
- Adds an opt-in current-v2 protection profile while preserving the historical minimal legacy protection definition by default.
- Current-v2 adds measured `turbine-overspeed` at 3300 rpm (reset-safe 3150 rpm) with turbine + generator trip, `condenser-high-backpressure` at 30 kPa absolute (reset-safe 20 kPa) with turbine + generator trip, and `generator-overfrequency` at 53 Hz (reset-safe 51.5 Hz) with generator trip.
- Adds canonical measured channels for condenser absolute pressure and generator frequency only when the enhanced current-v2 protection profile is enabled. Protection continues to consume measured M5.1 signals only.
- Adds current-v2 warning/trip annunciation for condenser high backpressure plus turbine/generator trip actions.
- Intentionally defers generator underfrequency protection until breaker/load-state supervision is available; a disconnected machine must not latch underfrequency merely because it is not synchronized.
- Adds regressions proving legacy/current version ownership, exact thresholds/actions, healthy initial v2 state and actual latching from measured overspeed/backpressure/overfrequency signals.
- Adds ADR 0087. Actuator travel rates, governor/load-control cleanup and adaptive substepping remain separate follow-on work.
- M10.9.3 remains the official validated milestone baseline; Hotfix 19 is the latest validated M10.9.4 structural checkpoint and Hotfix 20 requires ordinary + both explicit 60-second gates before promotion.

## M10.9.4 Hotfix 19 — Secondary-Pump Discharge Check Valves — IMPLEMENTATION CANDIDATE

- Compile fix: current/legacy seed tests now resolve pump definitions through `IntegratedSecondaryCycleDefinition.PlantDefinition` before calling `GetPump(...)`; no pump/check-valve physics or seed configuration changed.
- Promotes Hotfix 18 to the latest validated structural checkpoint after the user confirmed compilation, the ordinary suite and both explicit 60-second gameplay journeys all pass.
- Adds opt-in `PumpDefinition.HasDischargeCheckValve`; default `false` preserves existing bidirectional pump-path semantics for legacy definitions and components where reverse flow is intentional.
- `PumpFlowSolver` now closes an enabled discharge check valve whenever the unconstrained hydraulic solution would reverse through the pump path. The blocked state transfers zero mass, zero advected/internal pump energy and zero shaft-demand credit while retaining the committed pump speed/head state.
- Enables discharge check valves only on current-v2 `condensate-pump` and `feedwater-pump`; the main circulation pump and all legacy/default definitions remain unchanged.
- Adds direct regressions for running/stopped reverse-flow blocking, passive forward opening, zero mass/energy balance when closed, and v1/v2 topology ownership.
- Adds ADR 0086 and updates the structural stabilization roadmap. Protection expansion, actuator travel rates and adaptive substepping remain explicitly deferred.
- M10.9.3 remains the official validated milestone baseline; Hotfix 18 is the latest validated M10.9.4 structural checkpoint and Hotfix 19 requires ordinary + both explicit 60-second gates before promotion.

## M10.9.4 Hotfix 18 — Generator/Grid Synchronous Phase-Frequency Stiffness — IMPLEMENTATION CANDIDATE

- Compile correction: import the canonical Domain turbine namespace in `GeneratorGridSolver.cs` so the `TurbineRotorDefinition` parameter used by the new synchronous-coupling helper resolves correctly. No generator/grid equations, coefficients, seed values, replay semantics, protection logic or control authority changed.

- Promotes Hotfix 17 to the latest validated structural checkpoint after the user confirmed compilation, the ordinary suite and both explicit 60-second gameplay journeys all pass.
- Adds optional `SynchronousGridCouplingDefinition` to M4.5 generator definitions; null preserves the historical dispatch-torque-only legacy seam.
- Current-v2 paralleled generators now apply deterministic infinite-bus corrections around the dispatch setpoint: `Pphase = Psync,max*sin(delta)` plus `Pfrequency = Pdamp@1Hz*(fgenerator-fgrid)`.
- Positive electrical phase lead / positive frequency slip increase electromagnetic load; negative slip unloads the rotor, creating restoring phase/frequency stiffness instead of allowing a paralleled machine to drift freely away from 50 Hz.
- The final mechanical load is bounded to `[0, generator maximum mechanical power]` before conversion to canonical M4.2 external rotor torque. Rotor inertia remains integrated exactly once by `TurbineExpansionSolver`.
- Current sustained-generation and pre-synchronization v2 definitions use `Psync,max = 10 MW` and `Pdamp@1Hz = 10 MW`; at the validated 50 Hz / zero-phase-error design point the correction is exactly zero, preserving the Hotfix 17 initial operating point.
- Adds direct M4.5 regressions for phase lead/lag restoring direction, slow/fast rotor frequency damping and exact legacy-null-coupling dispatch behavior. Current-v2 seed tests assert the coupling is explicitly present.
- Adds ADR 0085. No pump check-valve, protection expansion, actuator travel-rate or adaptive-substep changes are mixed into this hotfix.
- M10.9.3 remains the official validated milestone baseline; Hotfix 17 is the latest validated M10.9.4 structural checkpoint and Hotfix 18 requires ordinary + both explicit 60-second gates before promotion.

## M10.9.4 Hotfix 17 — Condenser UA·ΔT Pressure Feedback — VALIDATED STRUCTURAL CHECKPOINT

- Takes user-corrected Hotfix 16 as the current green working checkpoint: solution build, 870 ordinary tests and both explicit 60-second gameplay journeys are documented green there.
- Replaces the current-v2 condenser's capacity-only heat-removal assumption with a canonical surface-condenser feedback law: `Q_effective = min(Q_available, UA * max(0, T_steam-space - T_coolant))`.
- Adds optional `CondenserDefinition.OverallHeatTransferConductance`; null remains isolated legacy capacity-only behavior, while current sustained-generation/synchronization v2 definitions use `1.225 MW/K`.
- Extends `CondenserCoolingBoundaryInput` with effective coolant temperature. Current v2 uses 20 °C; condenser cooling-capacity faults preserve that temperature while scaling only available rejection power.
- Derives `UA = 24.5 MW / (40 °C - 20 °C) = 1.225 MW/K`, preserving the Hotfix 16 initial design point instead of introducing a new tuning discontinuity.
- Publishes coolant temperature, steam-to-coolant ΔT, UA surface limit and effective heat-rejection capacity in condenser snapshots for direct diagnostics.
- Adds direct M4.3 regressions proving UA-limited rejection below installed capacity, weaker condensation as ΔT falls, zero condensation at non-positive ΔT and explicit legacy isolation.
- Removes the obsolete duplicate-number Hotfix 11 condenser ADR and records the current decision as ADR 0084.
- No generator synchronous-coupling, pump check-valve, protection, actuator-rate or adaptive-substep changes are mixed into this hotfix. Those remain ordered follow-on structural items.
- User validation complete: compilation, ordinary suite and both explicit 60-second gameplay journeys passed. Hotfix 17 is the validated base for Hotfix 18.

## M10.9.4 Hotfix 16 — Conservative Main-Steam Supply Closure — IMPLEMENTATION CANDIDATE

- Reproduced both explicit long-gameplay failures at the exact conserved `drum` states and found two successive defects rather than one numerical blow-up.
- Removed the artificial `p < pcrit` rejection from the subcritical-temperature compressed-liquid branch. The failing states are finite compressed liquid at about 548 K and 22.069 MPa; the critical-pressure bound remains in the saturation/vapor branches.
- Closed the current-v2 `drum -> steam outlet -> main-steam line` inventory path: when `CirculationDemandBalanced` is active, M4.1 supplements return-separated steam up to the positive committed main-steam-line demand using an exactly mass/energy-conservative internal transfer. Legacy `LegacyReturnSplit` profiles retain the historical behavior.
- Added direct regressions for both reported thermodynamic states, conservative current-mode steam replenishment and legacy isolation.
- Added versioned operational-seed speed-controller gains. Historical callers retain `P=1`, `D=0`; the already-loaded desktop v2 keeps `P=1`, `I=0.02 s⁻¹` and adds `D=0.2 s` to damp its small 10-second overshoot, while pre-synchronization v2 uses `P=0.5`, `I=0.02 s⁻¹` to prevent post-close 0%/100% cycling. Both preserve the bumpless 46% handoff.
- Long-test failures now retain all completed checkpoint diagnostics and include drum pressure/temperature/phase plus return, steam, recirculation and cycle-flow evidence.
- Final local validation is green: solution build has 0 warnings/errors, the ordinary suite passes 870 tests with only the 2 explicit journeys skipped, and both 60-s explicit journeys pass separately. M10.9.3 remains the validated baseline pending release-candidate promotion.

## M10.9.4 Hotfix 15 — Steam-Drum Inventory Closure — IMPLEMENTATION CANDIDATE

- Ordinary build/tests and the Hotfix 14 200-step turbine hydraulic invariant passed locally; the explicit long gameplay journeys then exposed the next structural failure in the canonical `drum` node.
- Root-caused the historical closed-cycle drum mass balance: physical return flow was added by the canonical return pipe and cancelled by the separator, while M4.4 feedwater remained a one-way drum addition, giving `dm_drum/dt = F_feedwater >= 0` by construction.
- Added explicit `SteamDrumLiquidRecirculationMode`: legacy profiles retain `LegacyReturnSplit`; current v2 sustained-generation/synchronization profiles use `CirculationDemandBalanced`.
- Current v2 liquid recirculation now follows positive committed MCP demand and separator drain is `F_steam + F_liquid`, yielding `dm_drum/dt = F_return + F_feedwater - F_MCP - F_steam` instead of a sign-only accumulator.
- Added direct M3.6 regression coverage for the new source-term closure and ADR 0082. No seed-volume/feedwater tuning is used as the fix.
- M10.9.3 remains the validated baseline; M10.9.4 Hotfix 15 remains candidate pending ordinary and explicit long-gameplay gates.

## M10.9.4 Hotfix 14 — Turbine Hydraulic Invariant Regression Contract — IMPLEMENTATION CANDIDATE

- Keeps the Hotfix 13 pressure-driven `turbine-inlet -> exhaust` expansion law unchanged; no production physics/control/protection code changes in this hotfix.
- Corrects the 200-step structural regression: `F_admission == F_stage` is not an instantaneous invariant for a compressible `turbine-inlet` plenum because their difference changes plenum inventory during transients.
- Retains the direct ±5% final combined admission-train inventory bound and adds a trajectory assertion requiring at least one negative inventory increment, which directly disproves the historical `dM_train/dt >= 0` ratchet.
- Keeps finite positive/in-range admission and stage flow checks without conflating transient valve flow with turbine expansion flow.
- Adds `docs/STRUCTURAL_PLANT_MODEL_STABILIZATION_PLAN.md`, classifying the external structural audit against the current code and fixing the validation order: turbine hydraulic closure first, then condenser pressure feedback, synchronous generator-grid coupling, pump non-return behavior, protections/actuator dynamics, and later fidelity/integration hardening.
- M10.9.3 remains the validated baseline; M10.9.4 Hotfix 14 remains candidate pending ordinary and explicit long-gameplay gates.

## M10.9.4 Hotfix 10 — Deterministic Seed Preconditioning & Steam-Path Control Authority — IMPLEMENTATION CANDIDATE

- Keeps M10.9.3 as the validated baseline and preserves all v1 initial-condition defaults/replay identities.
- Adds an optional deterministic fixed-step preconditioning count to the operational-seed factory; historical callers remain at exactly one seed step, while the new v2 desktop/synchronization seeds use two committed fixed steps before public logical STEP 0.
- This removes the first-snapshot bootstrap seam where the main-steam line was already flowing but turbine stage demand could still present as zero before controller/measurement/derived-input state had mutually committed.
- Increases only v2 steam-path hydraulic authority by reducing main-steam/admission resistance from 1800 to 1000 Pa·s²/kg².
- Reduces the v2 initial governor bias from 61% to 46%, preserving approximately the same initial steam-flow order while leaving substantially more opening authority when rotor speed falls under electrical load.
- Keeps the generation-scale 1000 m³ exhaust steam-space and 24.5 MW condenser boundary introduced by Hotfix 9.
- Updates the v2 synchronization seed contract to accept the intentional 46% bumpless governor bias without weakening synchronization or runtime stability requirements.
- Ordinary and explicit gameplay acceptance remain required before M10.9.4 promotion.

## M10.9.4 Hotfix 9 — Generation-Scale Condenser Steam-Space — IMPLEMENTATION CANDIDATE

- Replaces the v2-only 10 m³ condenser `exhaust` node with a 1,000 m³ low-pressure steam-space while preserving the historical 10 m³ default for all v1 replay identities and existing callers.
- Adds the optional `exhaustSteamSpaceVolumeCubicMetres` operational-seed parameter with strict finite/positive validation; no canonical M4 solver law or node connectivity is changed.
- Increases v2 steam-path hydraulic margin by reducing main-steam/admission resistance from 2,800 to 1,800 Pa·s²/kg² so the initial staged path has comfortable mechanical-power margin before the PI governor settles toward the 5 MWe equilibrium.
- Reduces v2 initial condenser heat rejection from 25.0 to 24.5 MW so the target low-load point is not systematically biased toward exhaust mass depletion.
- Strengthens the v2 seed regression to expose the actual stage-flow and shaft-power values with `Assert.InRange` rather than an opaque boolean failure.
- M10.9.3 remains the validated baseline; Hotfix 9 remains candidate pending the ordinary suite and the explicit 60-second gameplay pack.

## M10.9.4 Hotfix 8 — Continuous Main-Steam Supply Gradient — IMPLEMENTATION CANDIDATE

- Root-caused the repeated `exhaust` depletion: v2 initialized `steam` and `header` at the same saturation temperature/pressure, so the canonical main-steam line supplied 0 kg/s while the downstream admission train consumed only its finite preloaded inventories.
- Added an optional operational-seed header steam temperature with a backward-compatible default equal to primary steam temperature; historical v1 recipes therefore remain unchanged.
- Generation-ready v2 desktop and pre-synchronization seeds now initialize a continuous pressure staircase `steam 280 °C → header 275 °C → stop-out 269.5 °C → control-out 253 °C → turbine-inlet 246 °C`, yielding approximately 13 kg/s through every canonical steam-path element with the existing v2 resistances and 61% governor bias.
- Added ordinary regressions requiring positive >10 kg/s canonical main-steam-line replenishment and positive source-to-header pressure difference before accepting the v2 generation-ready seed.
- No M4 solver law, condenser law, turbine law, protection logic, historical v1 identity or replay payload was changed. M10.9.3 remains the validated baseline; Hotfix 8 remains candidate pending the ordinary suite and explicit long-gameplay pack.

## M10.9.4 Hotfix 7 — Condenser Balance & Spinning-Reserve Seed Correction — IMPLEMENTATION CANDIDATE

- Preserves M10.9.3 as the validated baseline and keeps historical `integrated-operations-desktop-stable` v1 / `pre-synchronization-grid-loading` v1 replay identities unchanged.
- Corrects the generation-ready v2 condenser heat-rejection boundary from 30 MW to 25 MW, matching the modeled ~12.9 kg/s low-load turbine steam flow and exhaust-to-hotwell specific-energy drop instead of progressively over-condensing the exhaust steam-space.
- Starts the v2 pre-synchronization profile with the same bumpless ~61% turbine-governor bias used by the sustained-generation profile, establishing spinning reserve before breaker closure rather than leaving downstream steam inventory to drain while the control valve is closed.
- Adds an ordinary one-simulated-second pre-synchronization thermodynamic smoke regression, so control-out/exhaust envelope failures are caught by the standard suite before the explicit 60-second gameplay pack.
- Restores the exact `ApplicationDescriptor` contract phrases required by the validated descriptor test (`long-gameplay acceptance tests`, `without moving plant topology`).
- No M4 condenser/turbine solver law, replay fingerprint schema, protection priority or Avalonia plant-control ownership is changed.

## M10.9.4 Hotfix 6 — Generation-Ready Power-Path Balance — IMPLEMENTATION CANDIDATE

- The first real explicit long-gameplay execution proved the historical desktop v1 seed was not a sustained generation point: at 10 simulated seconds it had decayed to ~1442.6 rpm and ~2.406 MWe with MODEL rotor shaft power 0 MW; the separate synchronization journey also drove `control-out` outside the simplified water/steam envelope.
- Preserved historical `integrated-operations-desktop-stable` v1 and `pre-synchronization-grid-loading` v1 unchanged for exact-version replay/archive compatibility; added generation-ready v2 factories and registered both versions.
- Current desktop integrated operations now references v2, with a staged 280→275→260→250 °C pressurized steam path, matched low-load admission hydraulics, bumpless PI governor bias, explicit turbine-shaft instrumentation, finite condenser heat rejection/capacity and matched condensate/feedwater pump capacity/bias.
- The explicit synchronization long journey uses a v2 initial condition without changing the historical M7.5 v1 scenario.
- Added presentation-only `[JsonIgnore]` `EffectiveTurbineSteamFlow`, derived from actual turbine stage-group effective mass flow. HMI, mimic, schematics, diagnostics and Operator Computer now use this value instead of the legacy zero-valued M4.1 turbine-boundary seam, while fingerprint-v1 keeps the historical serialized field unchanged.
- Strengthened the ordinary generation-ready seed regression to require finite synchronous speed, >4.5 MW MODEL shaft support, >10 kg/s effective steam/condensation/condensate/feedwater flow, and strengthened both explicit journeys to require sustained shaft plus electrical output.
- Added ADR 0079. M10.9.3 remains the validated baseline; M10.9.4 Hotfix 6 remains candidate until the ordinary suite and explicit 60-second gameplay pack both pass locally.

## M10.9.4 Hotfix 5 — Cooperative Long-Gameplay Batching — IMPLEMENTATION CANDIDATE

- Fixed both explicit long-running gameplay journeys so each 1,000-step checkpoint respects `ControlRoomRuntimeCoordinator.ExecutionBudget.MaximumSimulationStepsPerBatch` instead of calling `AdvanceRunning(1000, ...)` directly.
- A 1,000-step/10-second checkpoint is now executed cooperatively as runtime-budget-sized chunks (desktop default: 256 + 256 + 256 + 232), while assertions remain at the same 10-second checkpoints through 60 simulated seconds.
- The runtime execution budget is not widened or bypassed; no plant physics, seed, control, protection, replay, HMI semantics or acceptance thresholds changed.
- M10.9.3 remains the validated baseline; M10.9.4 Hotfix 5 is the current cumulative candidate pending the ordinary gate and explicit long-gameplay pack.

## M10.9.4 Hotfix 4 — Nullable Measured-Shaft Compile Guard — IMPLEMENTATION CANDIDATE

- Fixed CS8629 in `ControlRoomSubsystemSchematicProjector.BuildGeneratorPowerPathDiagnostic`: the near-zero measured-shaft branch now explicitly checks `shaft.HasValue` before reading `shaft.Value`.
- Preserves Hotfix 3 semantics exactly: unavailable measured shaft remains `UNAVAILABLE`, is never coerced to zero, and MODEL rotor-shaft remains a separate diagnostic datum.
- No runtime physics/control/protection/seed/replay changes.
- M10.9.3 remains the validated baseline; M10.9.4 Hotfix 4 is the current cumulative candidate.

## M10.9.4 Hotfix 3 — Unavailable Measured-Shaft Semantics & Long-Gameplay Gate Correction — IMPLEMENTATION CANDIDATE

- Corrected the ordinary projection test: the initial aggregate turbine-shaft MEASURED presentation channel is `UNAVAILABLE` (`NumericValue = null`) because that measured source is not published by the desktop instrumentation definition; it must not be coerced to numeric zero.
- Updated generator power-path diagnostics to distinguish `MEASURED shaft unavailable` from `measured shaft near zero`, while explicitly exposing the finite MODEL rotor-shaft evidence without silently substituting true state for an unavailable measurement.
- Corrected the explicit desktop gameplay journey so step 0 requires a finite MODEL rotor-shaft value but sustained mechanical/electrical production is enforced at 10, 20, 30, 40, 50 and 60 simulated seconds.
- Preserved Hotfix 2 Microsoft Testing Platform runner syntax using `dotnet test --project ... -- --explicit only`.
- No canonical plant physics/control/protection/replay behavior changed; M10.9.3 remains the validated baseline until the ordinary suite and explicit long-gameplay gate both pass.

## M10.9.4 Hotfix 2 — Test contracts, MTP runner syntax & desktop power-path evidence — IMPLEMENTATION CANDIDATE

- Fixed the M10.9.4 historical instrumentation-label regression to inspect XAML `Label` attributes, matching the already validated M10.9.2/M10.9.3 contract.
- Corrected the desktop power-path projection regression: the actual seed currently has ~5 MWe requested/actual output with near-zero turbine shaft production, so the diagnostic correctly reports a shaft deficit rather than `POWER PATH ACTIVE`.
- Strengthened the explicit desktop gameplay journey to fail immediately when the handoff is powered only by rotor kinetic energy, before proceeding to 60-second sustained-export checks.
- Fixed both long-gameplay helper scripts to use Microsoft Testing Platform syntax `dotnet test --project <csproj> --no-build -- --explicit only`.
- No production physics/control/protection code changed in this hotfix; the potential desktop seed/integrated-balance defect remains intentionally exposed for the explicit gameplay gate.

# Changelog

## M10.9.4 Hotfix 1 — xUnit2009 Assertion Contract Fix — IMPLEMENTATION CANDIDATE

- Replaced the two new `Assert.True(string.StartsWith(...))` assertions in `ControlRoomSubsystemSchematicProjectionTests` with canonical `Assert.StartsWith(...)` assertions required by xUnit analyzer rule xUnit2009.
- No production code, schematic semantics, gameplay logic, physics, controls, protection, bindings or long-running test behavior changed.
- M10.9.3 remains the official validated baseline until M10.9.4 Hotfix 1 passes the normal gate and the explicit long-gameplay pack.

## M10.9.4 — Subsystem Engineering Schematics — IMPLEMENTATION CANDIDATE

- Promoted M10.9.3 Interactive Full-Plant Mimic to VALIDATED after the user confirmed successful compilation and the complete automated suite passed.
- Added five Application-owned subsystem engineering schematic families and the Avalonia `ControlRoomSubsystemSchematicControl` renderer.
- Added explicit reactor/core feedback, primary recirculation/steam-drum, turbine/secondary, generator/grid and instrumentation/control/protection process/signal-flow diagrams with IN/OUT semantics.
- Promoted generator requested electrical power as presentation-only metadata and added a live power-path diagnostic separating shaft power, mechanical input, requested load, actual MWe, synchronization, breaker and protection state.
- Clarified that amber SHAFT is the mechanical-energy medium color, not warning severity.
- Added two xUnit v3 explicit long-running gameplay/system acceptance journeys, plus separate CMD/PowerShell launchers, so 60-second integrated turbine→generator→grid verification does not run in the ordinary fast suite.
- Added M10.9.4 Application/App regression coverage, `SUBSYSTEM_ENGINEERING_SCHEMATICS.md`, `GAMEPLAY_LONG_RUNNING_SYSTEM_TESTS.md` and ADR 0078.

## M10.9.3 — Interactive Full-Plant Mimic — IMPLEMENTATION CANDIDATE

- Promoted M10.9.2 Hotfix 2 to VALIDATED after the user confirmed successful compilation and the complete automated suite passed.
- Added immutable Application-layer `ControlRoomPlantMimic*` contracts and `ControlRoomPlantMimicProjector` over the existing `ControlRoomSnapshot` presentation boundary.
- Added an interactive whole-plant PLANT overview with eight macro-equipment groups, nine directed process/energy connections, explicit IN/OUT semantics, medium-specific visual grammar and key live operating evidence.
- Added equipment selection, connected-path emphasis and navigation-only `OPEN SUBSYSTEM` drill-down to existing REACTOR/PRIMARY/TURBINE/GRID workspaces.
- Added `ControlRoomPlantMimicControl` with recognizable equipment glyphs and flow arrows; Avalonia renders supplied semantics and owns no topology/physics inference.
- Added Application/App regression coverage plus `docs/milestones/M10.9.3.md`, `docs/INTERACTIVE_FULL_PLANT_MIMIC.md` and ADR 0077.

## M10.9.2 Hotfix 2 — Measured Instrument Label Contract Restoration — IMPLEMENTATION CANDIDATE

- Restored the canonical static XAML labels `ROTOR SPEED · MEASURED` and `ELECTRICAL OUTPUT · MEASURED` after the App contract suite exposed the first compatibility regression and review found the second latent assertion before another test cycle.
- Preserved the new runtime provenance badge; no gauge scale, threshold, trend, projection, replay, physics, protection or control semantics changed.
- M10.9.1 remains the validated baseline until the cumulative M10.9.2 Hotfix 2 package passes the local build/test/manual gate.

## M10.9.2 Hotfix 1 — Nullable Setpoint Compile Correction — IMPLEMENTATION CANDIDATE

- Fixed `CS0173` in `ControlRoomSnapshotProjector` for steam-drum level and steam-pressure setpoint projection by explicitly typing the two conditional locals as `double?`.
- No formula, threshold, scale, target, protection, trend, UI binding or replay semantics changed.
- M10.9.1 remains the validated baseline until M10.9.2 Hotfix 1 passes the local build/test/manual gate.

## M10.9.2 — Advanced Instrument & Gauge System — IMPLEMENTATION CANDIDATE

- Recorded explicit local validation of M10.9.1: the user confirmed successful compilation and the complete automated suite passed; M10.9.1 is the official validated baseline.
- Added reusable advanced linear and circular gauge controls that render published scale/band/target/setpoint/protection semantics without UI-owned thresholds, including compact numeric semantics text so color is never the only carrier of limit meaning.
- Added presentation-only provenance, quality and explicit off-scale metadata to control-room scalar snapshots while preserving replay/fingerprint-v1 identity.
- Added deterministic logical-step trend snapshots with reset/backwards-step invalidation and no wall-clock dependency.
- Projected canonical gauge metadata for reactor power, rod position, steam-drum pressure/level, rotor speed, generator synchronization quantities and electrical output.
- Kept total turbine shaft power numeric because no defensible canonical display scale is currently published; M10.9.2 does not invent one.
- Added M10.9.2 Application/App regression coverage, `ADVANCED_INSTRUMENT_GAUGE_SYSTEM.md` and ADR 0076.

## M10.9.1 — HMI Information Architecture & Visual Language — IMPLEMENTATION CANDIDATE

- Recorded explicit local validation of M10.8: the user confirmed successful compilation and the complete automated suite passed; M10.8 is the official validated baseline.
- Reframed the desktop around a five-region operator-experience shell: persistent situation strip, compact system navigation, central workspace, contextual inspector and persistent alarm/event strip.
- Added high-salience runtime, logical-step, gross-output, training-score, alarm, protection, assistance and control-authority summaries without inventing the future external-demand capability planned for M10.9.6.
- Reduced developer-facing milestone terminology in operator headings while preserving explicit MEASURED/MODEL provenance and all validated M10.8 keyboard/command capabilities.
- Added immutable Application-layer HMI contracts that separate displayed instrument scale, operating bands, scenario/controller target bands, setpoint markers and protection thresholds; Avalonia remains presentation-only and does not own safety semantics.
- Added shell/range-semantics regressions plus ADR 0075 and the approved M10.9.1–M10.9.8 operator-experience/HMI roadmap.
- Added the user-provided plant, reactor-core, turbine-island and instrumentation/protection schematics under `docs/reference/hmi/` as design references only; they are not runtime topology or authoritative physics/control data.

## M10.8 — Integrated Operator Computer UI — VALIDATED

- Recorded explicit local validation of M10.7.1 Hotfix 3: compilation and the complete automated suite passed; M10.7.1 is the official validated baseline.
- Integrated all eight operator-computer pages into a fixed header/menu/status/footer workstation with a single scrollable page-content region.
- Moved the Computer workspace out of the normal center-workspace outer scroll into a dedicated center viewport so the terminal menu/status regions remain genuinely fixed while only page content scrolls; non-Computer workspaces retain the validated scroll layout.
- Reworked the page menu into a readable 4×2 F1–F8 layout with persistent selected-page indicators independent of keyboard focus.
- Kept runtime, logical step, alarm, signal-health and protection summaries always visible above scrolling page content.
- Reduced rigid COMMANDS/SESSION sizing to bounded responsive list layouts suitable for the validated center viewport.
- Preserved keyboard-first operation (F1–F8, Tab/Shift+Tab, arrows, Enter), full mouse support and the no-free-form-command rule.
- Added dedicated M10.8 ViewModel/XAML regression coverage and rebuilt authoritative `PROJECT_HANDOFF.md` / `NEW_CHAT_START.md` for a clean post-M10.8 chat handoff.

## M10.7.1 Hotfix 3 — Committed breaker-state regression expectation — IMPLEMENTATION CANDIDATE

- Updated the single stale App regression exposed by Hotfix 2: when a snapshot reports the generator breaker as physically CLOSED while a generator trip is active, `CLOSE BREAKER` must still present the committed CLOSED/active state rather than becoming visually `Unavailable`.
- The already-satisfied/affected close command remains non-clickable (`BreakerCloseCommandEnabled = false`), so the test now verifies the intended separation between committed-state feedback and command availability.
- Test-only correction; no production ViewModel, command-dispatch, physics, protection, replay, archive, or UI behavior changed.

## M10.7.1 Hotfix 2 — Persistent control-state and momentary-command feedback — IMPLEMENTATION CANDIDATE

- Extended `ControlRoomPushButton` with a presentation-only `IsActive` state and a short click-feedback pulse; active normal controls use a filled green background with dark text while command availability remains a separate concern.
- Reactor `INSERT / HOLD / WITHDRAW` now reflect the actual committed motion of the selected canonical rod/group; group targets report `MIXED` when member motions differ, and the already-active motion command is disabled.
- Primary `START / RUN` and `STOP` now reflect the actual committed selected-pump state and disable the already-satisfied command.
- Electrical `CLOSE BREAKER` and `OPEN BREAKER` now reflect the actual committed breaker position and disable the already-satisfied side; close remains warning/blocked when the canonical synchronization permissive is not satisfied.
- `SPEED LOWER / SPEED RAISE / LOAD LOWER / LOAD RAISE` remain explicitly momentary setpoint-step commands: they flash on press, never latch visually, and the UI retains a `LAST CONTROL ACTION · ACCEPTED/BLOCKED` status so the operator can distinguish click feedback from committed state.
- Added presentation-only rod-target effective-motion projection excluded from fingerprint-v1 serialization, plus App/Application/XAML/replay regressions protecting actual-state semantics and historical replay compatibility.

## M10.7.1 — Operator Control-State & Synchronization Usability Hotfix — IMPLEMENTATION CANDIDATE

- Recorded M10.7 Hotfix 1 as VALIDATED after the user confirmed successful compilation and the complete automated suite passed; M10.7 is the new official baseline.
- Separated `ControlRoomPushButton` visual state from command availability so latched SCRAM/turbine/generator trips remain strongly filled/visible while the one-shot trip command is disabled.
- Added contextual `RESET PROTECTION` access in reactor, turbine and electrical areas plus presentation-only canonical M5.5 reset readiness/blocker projection; F4 COMMANDS now uses the same readiness/blocker state instead of advertising every active-trip reset as available.
- Made M4.5 synchronization presentation breaker-aware: open breaker shows detailed Δf/Δphase/ΔV close-check status; closed breaker shows `PARALLELED`/normal rather than a stale synchronization warning.
- Added Overview operator action guidance: current condition, next canonical procedure action and a cold-shutdown-to-first-electrical-output command map composed from validated M7 guidance.
- Kept new reset/synchronization diagnostics excluded from `ControlRoomSnapshotFingerprint` v1 so replay/checkpoint identity remains unchanged.
- Added App/Application/XAML/fingerprint regression coverage and `docs/OPERATOR_CONTROL_STATE_SYNCHRONIZATION_USABILITY.md`.

## M10.7.1 Hotfix 1 — Fingerprint-v1 compatibility and descriptor regression fix

- Fixed the stale `ApplicationDescriptorTests` expectation so M10.7.1 is correctly asserted on the validated M10.7 baseline.
- Preserved the exact M10.7 serialized semantics of `GeneratorPresentationSnapshot.SynchronizationState` and `SynchronizationText` used by `ControlRoomSnapshotFingerprint` v1.
- Moved the new breaker-aware synchronization UX to `[JsonIgnore]` presentation-only `DisplaySynchronizationState` / `DisplaySynchronizationText` properties; the new label/detail diagnostics are also excluded from fingerprint serialization.
- Strengthened replay regression coverage so presentation-only reset/synchronization diagnostics cannot change fingerprint-v1 output.

## M10.7 Hotfix 1 — xUnit analyzer compliance

- Replaced the single `Where(...)+Assert.Single(...)` pattern in `ScenarioSessionArchiveReplayTests` with the predicate overload of `Assert.Single`, satisfying xUnit analyzer rule `xUnit2031` under warnings-as-errors.
- Test-only correction; no production, archive, replay, serializer, UI, runtime, or physics behavior changed.

## M10.7 — Session, Checkpoint, Replay & Save Workspace — IMPLEMENTATION CANDIDATE

- Promoted the cumulative M10.2–M10.6 Hotfix 1 chain to VALIDATED after the user confirmed successful compilation and complete automated tests; M10.6 is the new official baseline.
- Added replay-backed `ScenarioSessionArchive` schema v1 with compact per-step fingerprint/event evidence, exact embedded scenario identity, operator actions, M10.5/M10.6 automation intents, recorder events and M9.1 checkpoints.
- Added JSON archive persistence plus canonical `ScenarioFullReplayRunner` archive replay/seek verification; no opaque solver-state dump or second checkpoint/restore owner.
- Added exact checkpoint-prefix event reconstruction so operator-action evidence accepted between committed frames is retained iff that action belongs to the applied replay prefix.
- Added `ScenarioRecorder.Capture()` and verified-prefix resume support so loaded/restored sessions continue one deterministic recording trace.
- Activated F8 SESSION with explicit recorded-session restart, checkpoint creation/listing, replay verification, file save/load and selected-checkpoint restore.
- Kept normal desktop recording opt-in to avoid hidden per-step fingerprint/frame overhead.
- Reduced routine desktop/full-plant endurance regressions from 6,000 to 1,000 steps / 10 simulated seconds after the original thermodynamic failures were isolated by dedicated direct resolver regressions; historical M9.7 60-second validation evidence remains unchanged.
- Added `docs/milestones/M10.7.md` and `docs/OPERATOR_COMPUTER_SESSION_CHECKPOINT_REPLAY_SAVE.md`.

## M10.6 Hotfix 1 — Automation replay test compilation

- Added the missing `NuclearReactorSimulator.Application.Scenarios.Recording` import to `ScenarioAutomationReplayTests`.
- Changed the recorder-completion exception assertion to an explicit `Action` block so xUnit v3 selects the synchronous `Assert.Throws<T>` overload unambiguously.
- Test-only correction: no production runtime, supervisory-control, authority, recorder, replay, or checkpoint behavior changed.

## M10.6 — Supervisory Automatic Operation — IMPLEMENTATION CANDIDATE

- Preserved M10.1 as the last explicitly validated M10 baseline; M10.2–M10.4 remain unvalidated candidates. M10.5 is included as the minimum prerequisite and M10.6 is the current candidate layered on that chain.
- Added independent training-assistance and physical plant-control-authority axes with requested/effective/health/degraded presentation state.
- Added deterministic M5-owned `SupervisoryOperationCoordinator` with bounded Hold Reactor Power, Hold Turbine Speed and Hold Current Operating Point objectives over existing local controller modes/setpoints only.
- Added measured-signal-only supervisory validation, fail-closed degradation, canonical protection suspension and deterministic bumpless Manual takeover using committed controller outputs.
- Activated the MODES terminal page for training assistance, MANUAL/ASSISTED/SUPERVISORY selection, current-operating-point hold and per-loop mode/status visibility.
- Added separate scenario automation-intent journaling and M9.1 full-replay/checkpoint reconstruction without changing the versioned `ControlRoomSnapshot` fingerprint schema or recasting automation intents as physical `ControlRoomCommandKind` values.
- Added M10.5/M10.6 integration, protection, invalid-measurement, replay and App/ViewModel/XAML regression coverage.
- Added `docs/milestones/M10.5.md`, `docs/milestones/M10.6.md`, `docs/DUAL_ASSISTANCE_CONTROL_AUTHORITY.md`, `docs/SUPERVISORY_AUTOMATIC_OPERATION.md` and ADR 0074.

## M10.4 — Contextual Command Console — IMPLEMENTATION CANDIDATE

- Preserved M10.1 as the last explicitly validated M10 baseline; M10.2 and M10.3 remain unvalidated, with M10.4 layered on that candidate chain.
- Activated the fixed COMMANDS terminal page with immutable Application-layer command catalog contracts.
- Added contextual expansion of canonical typed commands by exact target (rod/group, MCP, rotor, generator, breaker, alarm) plus runtime/protection/global commands.
- Added explicit AVAILABLE / BLOCKED / UNAVAILABLE presentation states, current-state text and blocking reasons without creating a second permissive/interlock owner.
- Added keyboard/list selection and Enter/explicit execute dispatch through the existing `IControlRoomCommandDispatcher`; blocked commands are not dispatched and runtime/scenario rejection remains authoritative/fail-closed.
- Kept training/presentation intents and session lifecycle intents outside `ControlRoomCommandKind`, preserving ADR 0070 ownership boundaries.
- Added Application/App/XAML regression coverage and `docs/OPERATOR_COMPUTER_CONTEXTUAL_COMMAND_CONSOLE.md` / `docs/milestones/M10.4.md`.

# Changelog

## M10.3 — Alarm, Log & Incident Workstation — IMPLEMENTATION CANDIDATE

- Preserved M10.1 as the last explicitly validated M10 baseline; M10.2 remains unvalidated and M10.3 is layered on that candidate.
- Added immutable operator-computer alarm/log/incident presentation contracts and `OperatorComputerAlarmLogProjector`.
- Activated the ALARMS terminal page as a read-only projection over canonical M5.6/M6.6 annunciator state and bounded logical-step alarm-event history; no terminal ACK/RESET action is introduced before M10.4.
- Activated the LOG page with explicit LIVE / SESSION / INCIDENT evidence scopes: bounded M6.6 trends/events, optional M9.1 recorder events, and optional immutable M9.2 post-incident reports.
- Kept default desktop operation free of hidden full-recorder overhead: M9.1 evidence is shown only when a recorder is explicitly supplied by a session owner.
- Reordered MainWindow history observation before operator-computer reprojection so the terminal sees the same committed snapshot/history step instead of lagging one publication.
- Added Application/App regressions for annunciator projection, bounded event ordering, M6.6 trend reuse, optional M9.1 evidence, optional M9.2 incident projection and read-only desktop terminal behavior.
- Added `docs/OPERATOR_COMPUTER_ALARM_LOG_INCIDENT_WORKSTATION.md` and `docs/milestones/M10.3.md`; no new ADR is required because ownership follows ADR 0070 and validated M5.6/M6.6/M9.1/M9.2 boundaries.

## M10.2 — Unified Information, Guidance & Diagnostics — IMPLEMENTATION CANDIDATE

- Recorded explicit local validation of M10.1: compilation and the complete automated suite passed and the terminal shell worked correctly.
- Added generic immutable operator-computer content contracts for information, guidance steps and procedure diagnostics.
- Added `OperatorComputerInformationProjector`, sourcing only already-published `ControlRoomSnapshot`/M6 panel values and preserving explicit `[MEASURED]`, `[MODEL]`, `[STATE]` and `[UNAVAILABLE]` provenance.
- Added `OperatorComputerScenarioContentProjector` adapters for the existing M7.2–M7.6 guidance/checklist families; canonical evaluator results remain the only readiness criteria.
- Activated GUIDANCE/INFO/DIAGNOSTICS terminal content while leaving ALARMS/LOG, COMMANDS, MODES and SESSION staged for their planned M10 milestones.
- Preserved `TrainingGuidanceMode`: Hidden/ChecklistOnly suppress step-by-step guidance without changing diagnostic evaluation or scoring semantics.
- Added Application/App/XAML regressions for unavailable-value preservation, canonical checklist reuse, guidance-mode suppression, page staging and real desktop-scenario terminal integration.

## M10.1 — Operator Computer Contracts & Terminal Shell — VALIDATED

- Recorded explicit validation of M9.7 hotfix 5 and the M9 phase gate: local compilation succeeded and all 760 automated tests passed.
- Integrated the user-supplied `MainWindow.axaml` as the authoritative validated layout basis; the manual corrections remove the previous center-workspace overlap/clipping behavior without restoring the discarded synthetic minimum-width/horizontal-scroll workaround.
- Added immutable Application-layer operator-computer page/status/snapshot contracts and a fixed eight-page catalog: GUIDANCE, INFO, ALARMS, COMMANDS, MODES, DIAGNOSTICS, LOG and SESSION.
- Added `OperatorComputerSnapshotProjector`, projecting only already-published `ControlRoomSnapshot` shell status; all page content remains explicitly `ShellOnly` in M10.1.
- Added `OperatorComputerViewModel`, seventh `Computer` workspace, monospace HUD-style `ControlRoomComputerControl`, fixed status line and global F1–F8 page navigation. Page selection/focus remain App presentation state and dispatch no plant commands.
- Added Application/App/XAML regressions for fixed page-set immutability, status projection, selection persistence, keyboard navigation, dedicated terminal binding and preservation of the user-validated MainWindow layout contract.
- No M10.2+ guidance/info/diagnostic content, alarm/log aggregation, command catalog, control-authority model, supervisory automation or session persistence is implemented yet.

## M9.7 — Advanced Fidelity Integration Gate — VALIDATED / M9 GATE COMPLETE

- User confirmed local compilation and complete automated validation: **760/760 tests passed** after hotfix 5.
- The 6,000-step / 60-second Application and real desktop-pump endurance gates pass, including the boundary-aware saturated and superheated root-bracketing regressions.
- Final manual GUI layout corrections were supplied and validated by the user in `MainWindow.axaml`; that file is integrated as the authoritative M9.7 layout baseline.
- M9.7 and the full M9 phase gate are complete; M10.1 begins on this validated baseline.

## M9.7 — Advanced Fidelity Integration Gate — IMPLEMENTATION CANDIDATE

### M9.7 hotfix 5 — superheated phase-boundary root bracketing

- Extended the M1.7 numerical root-search correction symmetrically to the superheated-vapor branch after the 60-second endurance gate exposed `exhaust` at `v=65.477888248812704 m^3/kg`, `u=2434381.9782870663 J/kg`.
- Verified that the conserved state has a genuine solution inside the existing superheated closure (about 17.907 °C and 2.052 kPa); the fixed 512-segment scan missed it because the first admissible superheated temperature lies between coarse samples.
- Added a deterministic boundary-aware superheated fallback that locates the exact admissible temperature interval, injects its valid endpoints, and reuses the existing superheated equations/bisection. No state clamp, envelope widening or new thermodynamic correlation is introduced.
- Added direct regression coverage for the observed low-pressure `exhaust` state and a negative regression proving that a true correlation gap with no root still fails closed.
- Retained the 6,000-step / 60-second direct-session and real desktop-pump endurance gates unchanged.

### M9.7 hotfix 4 — saturation-boundary thermodynamic root bracketing

- Investigated the 60-second endurance failures at `drum` and `exhaust` and confirmed a structural numerical gap in the simplified water/steam closure rather than another seed-only defect. The fixed 512-point full-range saturated-mixture scan could miss a valid root when the admissible saturation interval terminated between two samples near quality 0 or quality 1.
- Preserved the long-validated resolver order/fast paths. Only after saturated-mixture, subcooled-liquid and superheated-vapor resolution all fail, a deterministic boundary-aware saturated-mixture fallback computes the exact upper temperature of the physically valid specific-volume interval and rescans that interval before declaring the state unsupported.
- Added direct regressions using the exact conserved `(v,u)` states observed in manual/endurance failures: the `drum` case resolves near quality 0 at about 120 °C, while the `exhaust` case resolves as wet steam near quality 0.990 at about 39.93 °C. No arbitrary phase clamp or broadened thermodynamic envelope is introduced.
- Removed reliance on the desktop-only inflated `0.001` liquid-compression override; the M9.7 desktop seed returns to the shared historical default margin while retaining its separately versioned balanced 5 MWe / finite-condenser-cooling steam-path lineup.
- Updated stale M9.7 descriptor lineage text and made runtime-pump tests report an actual host failure before generic null/step-count assertions.
- Hotfix 3 layout, full-session reset and 6,000-step/60-second endurance requirements remain intact and must be revalidated locally.

### M9.7 hotfix 3 — workspace viewport, deterministic session reset and extended desktop endurance

- Recorded successful local compilation and complete automated-suite validation of M9.7 hotfix 2.
- Reworked the center workspace viewport so padding belongs to scrollable content rather than the `ScrollViewer` viewport, added explicit horizontal scrolling for wide dashboard grids, a clipped center-column host and a trailing scroll extent so the final card remains fully reachable above the fixed footer.
- Added an explicit `Reset session` desktop action that reconstructs the exact versioned M9.7 desktop scenario through the composition root instead of mutating or partially zeroing live physical state. The old ViewModel unsubscribes from runtime/training events before replacement.
- Extended both the Application-level desktop integration endurance regression and the real App `DesktopControlRoomRuntimePump` path to 6,000 fixed steps (60 simulated seconds), deliberately crossing the manually observed step-3111 block. The candidate desktop seed now uses the validated low-load 5 MWe handoff, a small finite 0.1 MW condenser cooling boundary and a 0.001 compressed-liquid margin; validated M7 identities/defaults remain unchanged.
- Added a fresh-session reload regression proving reset semantics return to logical step 0, PAUSED host mode and the exact original snapshot fingerprint.
- Added XAML contract coverage for the reset action, bidirectional center scrolling, inner scroll padding and explicit trailing extent.

### M9.7 hotfix 2 — desktop drum thermodynamic-margin correction

- Preserved the hotfix-1 continuous-RUN regression at 1,000 logical steps rather than weakening the test after it exposed a second real desktop-seed weakness at the primary steam drum.
- Added an optional primary-liquid compression-margin parameter to the shared operational-seed factory with the exact historical default (`0.000001`) preserved for all previously validated M7/M8/M9 initial-condition call sites.
- The separately versioned M9.7 desktop seed alone opts into a modest `0.0001` density-compression fraction, moving its primary liquid inventories deterministically inside the simplified subcooled-liquid envelope instead of starting effectively on the saturation boundary.
- The thermodynamic resolver/envelope, canonical M3 ownership, historical versioned seeds and scenario physics are unchanged.

### M9.7 hotfix 1 — desktop runtime/manual-GUI gate corrections

- Updated the stale `ApplicationDescriptorTests` expectation from M9.6 to the actual M9.7 integration-gate descriptor.
- Added a real App-layer `DispatcherTimer`/`DesktopControlRoomRuntimePump` so RUN advances bounded deterministic fixed-step batches; PAUSE stops host advancement and SINGLE STEP remains exact. Wall-clock cadence remains outside physics.
- Added a separately versioned `DesktopIntegratedOperationsInitialConditionFactory` / desktop scenario wrapper instead of mutating validated M7 v1 identities. The new seed uses a turbine steam-path inventory aligned with the upstream steam-space condition plus explicit governor droop/opening, with a regression that advances 1,000 logical steps (10 simulated seconds) past the former step-5 `control-out` failure.
- Hardened `ControlRoomPushButton` hit targets with a non-null surface, full stretch/content alignment, minimum height and pointer cursor so the complete visual button rectangle is interactive.
- Added a RUNNING/PAUSED + logical-step progress indicator beside the host controls and in the footer.
- Clipped the center workspace row above the footer and added bottom scroll breathing room so the final `ARCHITECTURE CONTRACT` content can be scrolled fully above the status bar.
- Added App/Application regression coverage for continuous desktop RUN, pause behavior, runtime progress bindings and footer-safe scrolling.

- Recorded successful local compilation and complete automated-suite validation of M9.6 hotfix 1; M9.6 is the validated code baseline for M9.7.
- Added a cross-feature Simulation regression proving M9.3 canonical xenon and M9.4 quasi-spatial feedback compose exactly once through the single global point-kinetics/non-rod-reactivity seam.
- Added a real M9.3 xenon-session integration test spanning M9.1 recorder/checkpoint/full replay, M9.2 post-incident analysis and M9.6 immutable snapshot metric extraction with identical original/replay evidence.
- Added M9.5/M9.6 fidelity-evidence consistency checks that preserve the explicit distinction between validated model capabilities and external historical calibration claims.
- Added real-runtime App/ViewModel integration tests for xenon availability, legacy `Unavailable` semantics and RUN/PAUSE/SINGLE STEP synchronization through the canonical scenario/coordinator boundary.
- Added `docs/M9_ADVANCED_FIDELITY_INTEGRATION_GATE.md`, `docs/M9_FINAL_MANUAL_VALIDATION_CHECKLIST.md` and `docs/milestones/M9.7.md`.
- M9.7 introduces no new physics. Final M9 promotion requires local clean build/tests plus explicit completion of the manual GUI checklist before M10 starts.

### M9.6 hotfix 1 — advanced GUI test compilation fix

- Replaced an invalid object-initializer syntax applied to the `CreateViewModel(...)` factory result in `MainWindowViewModelAdvancedTests.SelectionIndices_ClampWhenRodPumpGeneratorAndAlarmCollectionsShrink` with explicit property assignments.
- Test-compilation-only correction: no App/ViewModel production code, GUI behavior, M9.6 reference validation, physics, runtime, scenario, replay, or ownership semantics changed.

## M9.6 — Calibration & Reference Validation Suite — IMPLEMENTATION CANDIDATE

- Recorded explicit local validation of M9.5: compilation and the complete automated suite passed; M9.5 is now the official validated baseline.
- Added versioned steady-state/transient reference-validation contracts with exact logical-step targets, explicit absolute/relative tolerance budgets, model-version tracking and fail-closed missing-evidence semantics.
- Added stable `ControlRoomSnapshot` reference metric IDs/extraction and curated internal regression baselines for cold shutdown, pre-synchronization and first generator loading; these are explicitly not external historical measurements.
- Added deterministic sensitivity/regression reports for explicit parameter perturbations, including a real `FissionPowerCalibration` sensitivity regression.
- Expanded `NuclearReactorSimulator.App.Tests` with advanced workspace, snapshot-refresh, target-clamping, typed-command routing, alarm/protection/interlock and XAML binding/provenance contract tests before M10.
- Added `docs/CALIBRATION_REFERENCE_VALIDATION.md`, `docs/MANUAL_GUI_VALIDATION_CHECKLIST.md`, `docs/milestones/M9.6.md` and ADR 0073.
- M9.6 remains a candidate until local clean build/complete automated tests and the requested manual GUI validation are explicitly confirmed. M9.5 remains the official validated baseline until then.

## M9.5 — Historical-Inspired Scenario Framework — VALIDATED

- Local compilation and the complete automated suite passed; M9.5 is the official validated baseline for M9.6.
- The implementation content below is unchanged from the previously delivered M9.5 candidate.

- Recorded explicit local validation of M9.4 after hotfix 1: compilation and the complete automated suite passed; M9.4 is now the official validated baseline.
- Added optional versioned `ScenarioDefinition.HistoricalContext` with explicit source references, claim classification (`DocumentedFact`, `EducationalApproximation`, `SimulatorSpecificAssumption`), required model-capability IDs, fidelity statement and deliberate non-claims.
- Added deterministic fail-closed `HistoricalScenarioFidelityReviewer` and `HistoricalScenarioFidelityException`; `ScenarioSessionFactory` now blocks historical-inspired content before runtime creation when declared validated capabilities are missing.
- Advanced JSON scenario persistence to schema v3; v0/v1/v2 migration preserves existing scenario/initial-condition semantics and never invents historical metadata.
- Added `docs/HISTORICAL_INSPIRED_SCENARIO_FRAMEWORK.md`, `docs/milestones/M9.5.md` and ADR 0072.
- M9.5 introduces no named historical reconstruction, source-network access, calibration or scenario-owned physical outcome.

### M9.4 hotfix 1 — test namespace compilation fix

- Added the missing `NuclearReactorSimulator.Domain.Physics.Fluids` namespace import to `ReactorPrimaryControlSolverTests`, allowing the M9.4 quasi-spatial regression to resolve the canonical `VoidFraction` value object.
- Test-compilation-only correction: no production physics, runtime integration, scenario, initial-condition, replay, or M9.4 ownership semantics changed.

## M9.4 — Spatial/Quasi-Spatial Fidelity Refinement — VALIDATED

- Built on the validated M9.3 baseline without changing existing versioned M7/M8/M9.3 scenarios or initial conditions.
- Added opt-in `QuasiSpatialCoreFeedbackDefinition` over the validated M3.3 aggregated-core boundary; arbitrary zone identifiers/coordinates remain supported and adjacency is never inferred from coordinates.
- Reused the existing M2 fuel-temperature, coolant-temperature and void feedback solvers per committed core zone, then reduced local contributions to one current-power-share-weighted global reactivity contribution through the existing non-rod/global point-kinetics seam.
- Added explicit symmetric zone-coupling definitions that smooth only the power-shape-driving signal; coupling does not create local neutron populations, duplicate kinetics, new conserved inventories or implicit grid topology.
- Added deterministic normalized power-shape relaxation with explicit sensitivity and time constant. Candidate zone shares affect the next committed full-plant step; the existing single global point-kinetics owner is preserved.
- Added domain/simulation regressions for definition invariants, weighted feedback, explicit coupling, shape closure/evolution, determinism, zero-sensitivity behavior, and opt-in integration through `ReactorPrimaryControlSolver`.
- Added `docs/SPATIAL_QUASI_SPATIAL_FIDELITY.md`, `docs/milestones/M9.4.md` and ADR 0071.
- Local compilation and the complete automated suite subsequently passed after hotfix 1; M9.4 became the official validated baseline for M9.5.

## M9.3 — Advanced Xenon & Low-Power Transients — VALIDATED

- Recorded explicit local validation after hotfix 2: compilation and the complete automated suite passed; M9.3 is the official validated baseline.
- Approved future M10 `Operator Computer, Supervisory Automation & Human-Machine Integration` architecture and roadmap: fixed menu terminal, independent training-assistance/control-authority axes, M5-owned supervisory automation, fail-closed degradation, bumpless manual takeover, intent taxonomy and replay-backed session archive direction.
- Added `docs/OPERATOR_COMPUTER_SUPERVISORY_AUTOMATION.md`, planned `docs/milestones/M10.md` and ADR 0070.
- Renamed planned M9.7 to `Advanced Fidelity Integration Gate` and moved final release hardening/packaging to M11 after M10.
- Hotfix candidate 2: bounded the Application-level restart-seed determinism regression to a short end-to-end window; the previous 200-step full-plant loop accidentally coupled an M2.8 wiring assertion to the unrelated simplified M3 drum water/steam envelope. No production physics, seed data, scenario semantics or replay/versioning contracts changed.
- Hotfix candidate 1: added the missing Simulation iodine/xenon namespace import in `ReactorPrimaryControlSolverTests`; this is test-compilation-only and does not change production physics, runtime behavior, scenario semantics or replay/versioning contracts.
- The implementation package was subsequently validated locally after the two test-only hotfixes; M9.3 is now the official validated baseline.
- Promoted the canonical validated M2.8 I-135/Xe-135 state into the M5 reactor/primary runtime through an optional `IodineXenonDefinition` / `IodineXenonState` seam rather than introducing Application/scenario physics.
- Composed committed xenon reactivity through the existing explicit non-rod-reactivity seam before point kinetics and advanced poison inventories with the existing M2.8 solver after candidate kinetics/fission power.
- Promoted only immutable committed M2.8 xenon diagnostics through the control-room presentation boundary; configurations without canonical poison state remain explicitly `Unavailable`.
- Preserved exact-version compatibility by leaving existing M7 v1 initial conditions xenon-disabled instead of silently changing prior replay/checkpoint semantics.
- Added versioned post-shutdown restart and poisoned low-power initial conditions plus `AdvancedXenonScenarioPack`; scenarios use existing typed rods, circulation, alarm and protection commands and do not script xenon/power trajectories or recovery outcomes.
- Added deterministic Simulation/Application regression coverage, M9.3 milestone/domain documentation and ADR 0069.


## M9.2 validated handoff maintenance checkpoint

- Recorded explicit local validation of M9.2: compilation and the complete automated test suite passed; M9.2 is the official functional baseline.
- Synchronized authoritative status/roadmap/architecture/readme documentation for the transition to M9.3.
- Rebuilt `docs/PROJECT_HANDOFF.md` and `docs/NEW_CHAT_START.md` as the authoritative restart pair for a new project chat.
- Performed conservative dead-code review across production/test symbols. Removed `ShellControlRoomCommandDispatcher`, an internal legacy shell fallback with zero production/test references; retained low-reference serializer interfaces and scenario packs because they are deliberate public/architectural seams.
- The cleanup changes occur after the already validated M9.2 run; run a clean restore/build/test on this maintenance package before treating the cleanup delta as revalidated or beginning M9.3 implementation.


## M9.2 — Post-Incident Analysis (baseline candidate)

- Recorded M9.1 as locally validated after successful build and complete test suite.
- Added deterministic `ScenarioPostIncidentAnalyzer` over immutable M9.1 recordings.
- Added exact/automatic incident anchors, logical-step pre/post windows, ordered evidence timeline and start/anchor/end state summaries.
- Added observed response metrics for alarms, protection activation, operator action, fault clearance and peak signal/alarm/fault indicators.
- Added nearest preceding replay-backed checkpoint linkage without creating a second restore mechanism.
- Added versioned `PostIncidentAnalysisReport` schema v1 plus JSON serializer with fail-closed unknown-schema handling.
- Added ADR 0068 formalizing that temporal ordering is evidence and must not be silently promoted to causal inference.
- Added regression tests and updated handoff/status/roadmap documentation.



## M9.1 — Recorder, Checkpoints & Full Replay (baseline candidate)

- Recorded explicit local validation of the exact M8.5 hotfix 2 → M8.6 → M8.7 hotfix 2 chain after `dotnet clean`, restore/build and the complete automated suite passed; M8.7 hotfix 2 is now the official validated baseline and the M8 gate is complete.
- Added `ScenarioRecorder` capturing the initial frame plus every deterministic fixed step independently from presentation publication stride.
- Added immutable `ScenarioRecording` with retained control-room frames, accepted typed operator actions and a monotonic recorder event stream for operator actions, alarm events, fault transitions and protection transitions.
- Added versioned `ControlRoomSnapshotFingerprint` v1 and replay-backed `ScenarioCheckpoint` schema v1; checkpoints never serialize or own private physical solver state.
- Added `ScenarioFullReplayRunner` with exact scenario/initial-condition reconstruction, accepted-action replay, per-frame fail-closed fingerprint verification, event-stream verification and deterministic seek-to-checkpoint verification.
- Added `JsonScenarioCheckpointSerializer` with explicit unsupported-schema rejection.
- Added M9.1 Application/Infrastructure regression tests, ADR 0067 and `docs/RECORDER_CHECKPOINT_FULL_REPLAY.md`.


## M8.7 stacked baseline candidate / hotfix 2 — M8.5 committed-node thermodynamic-admissibility guard

- Fixed the only failing stacked-chain regression: an intentionally extreme pressure-driven break could respect `MaximumInventoryFractionPerStep` yet still move a near-saturated liquid node outside the simplified water/steam state envelope.
- Pressure-driven breaks now use a deterministic committed-node admissibility probe and further cap only the M8.5 mass/energy removal when required by the existing thermodynamic closure; no second full-plant predictor step is performed.
- The declared inventory fraction remains a strict upper bound; the guard never adds inventory, mutates committed state, relaxes the thermodynamic model or changes M3 single-integration ownership.
- The existing severe-break regression now verifies both positive additional loss and compliance with the declared maximum without causing `WaterSteamStateOutOfRangeException`.
- M8.5, M8.6 and M8.7 remain unvalidated stacked candidates; the last official validated baseline remains M8.4 hotfix 2.


## M8.7 — Safety-Response Scenario Pack (stacked baseline candidate)

- M8.5 and M8.6 remain unvalidated stacked candidates because the user is temporarily away from the validation environment; M8.7 is intentionally stacked on that exact chain and does not change the official validated baseline (M8.4 hotfix 2).
- Added three capstone safety-response exercises reusing exact M8.3/M8.5/M8.6 fault declarations: protection fail-safe, large-break-class response and station-blackout-class response.
- Added `SafetyResponseCheckpointEvaluator` with committed-presentation-only acceptance checks and 100-point M7.7 training plans; acceptance criteria never inject physical/protection outcomes.
- Added `SafetyResponseEvaluationSession`, exposing deterministic assessment plus the existing accepted-operator-action logical timeline for debrief.
- Added M8.7 regression tests, ADR 0066, `docs/SAFETY_RESPONSE_SCENARIO_PACK.md` and stacked-candidate handoff/status/roadmap updates.

## M8.6 — Electrical Loss & Station Blackout-Class Scenarios (stacked baseline candidate)

- M8.5 remains unvalidated because the user is temporarily away from the validation environment; M8.6 is intentionally stacked on that candidate and does not change the official validated baseline (M8.4 hotfix 2).
- Added `electrical.external-supply-loss`, bound fail-closed to the exact canonical M4.5 grid id. While active it forces canonical generator breakers open through `GeneratorGridInputs` and overrides close requests without writing breaker state directly.
- Added deterministic external-supply-loss and station-blackout-class scenario definitions.
- Station-blackout-class consequences are explicit composition of validated M8.2 pump trips, M8.3 powered actuator-command fail-low faults and M8.4 turbine/generator trips; no synthetic AC/DC bus, diesel, battery or ECCS electrical model is introduced.
- Fault clearance removes external-supply forcing only; generator reconnection remains a deliberate synchronization/close operation.
- Documented that M2.5 stateful decay heat is not yet promoted into the M5.7 integrated operational runtime and deliberately avoided fabricating a fixed post-shutdown heat source.
- Added M8.6 Application regression tests, ADR 0065, `docs/ELECTRICAL_LOSS_STATION_BLACKOUT_SCENARIOS.md` and stacked-candidate handoff/status/roadmap updates.

## M8.5 — Educational Leak/LOCA-Class Scenarios (baseline candidate)

- Recorded explicit local validation of M8.4 hotfix 2: compilation and complete tests passed; M8.4 is now the validated baseline.
- Added `loca.pressure-driven-break`, a deterministic bounded break boundary driven only by committed canonical node pressure and immutable scenario parameters.
- Added conservative break mass plus carried internal-energy removal through existing `PlantNetworkSourceTerms`; `PlantNetworkOrchestrator` remains the sole fluid/thermal inventory integrator.
- Added an explicit per-step inventory-removal bound as a lumped-model validity/numerical guard; it does not represent ECCS, containment response or scripted accident correction.
- Added small primary leak, large break-class and steam-space leak/depressurization deterministic scenario definitions over the validated M7.6 operating initial condition.
- Added fail-closed target/parameter/conflict validation and regression tests for mass/energy loss, relative depressurization, zero driving-pressure flow, inventory bounds and built-in registry binding.
- Added ADR 0064, `docs/EDUCATIONAL_LEAK_LOCA_SCENARIOS.md` and M8.5 milestone/handoff/status/roadmap updates with explicit non-licensing fidelity limits.

## M8.4 — Turbine / Generator / Feedwater / Condenser Transients — VALIDATED / HOTFIX 2

- Hotfix 2 scales the transient-ready condenser cooling seed from 20 MW to 0.1 MW so the compact conserved exhaust inventory remains inside the simplified water/steam closure envelope during deterministic seed/runtime steps; fault semantics and canonical M4.3 ownership are unchanged.

- Hotfix candidate 1: added the missing Simulation condenser/feedwater namespace imports in `IntegratedAutomaticOperationRuntimeEngine`; no transient, fault, solver or scenario semantics changed.
- Recorded explicit local validation of M8.3: compilation and complete tests passed; M8.3 is now the validated baseline.
- Added exact `secondary-transient-ready` v1 initial condition reusing canonical M7 owners with finite 0.1 MW M4.3 condenser cooling-boundary capacity scaled to the compact educational exhaust inventory.
- Added deterministic turbine-trip and generator-trip/load-rejection fault applicators that feed existing M5.5 protection inputs rather than writing valves, breakers, rotor speed or electrical power directly.
- Added condenser cooling degradation/loss as a bounded per-step overlay on canonical `CondenserCoolingBoundaryInput.AvailableHeatRejectionPower`; condenser pressure/vacuum remain derived from conserved state.
- Added feedwater degradation/loss scenarios by composing validated M8.2 `hydraulic.pump-degradation` and `hydraulic.pump-trip` effects on the canonical feedwater pump.
- Added four M8.4 scenario definitions, built-in applicator registration, fail-closed target validation and Application regression tests.
- Added ADR 0063, `docs/SECONDARY_SYSTEM_TRANSIENTS.md` and M8.4 milestone/handoff/status/roadmap updates.

## M8.3 — Instrumentation & Control Faults — VALIDATED

- M8.2 Hydraulic Component Faults hotfix 2 promoted to validated baseline after explicit local build/test success.
- Added built-in deterministic sensor bias/freeze/failed-low/failed-high/unavailable applicators reusing the canonical M5.1 `SensorFaultInput` seam.
- Added controller-output freeze/fail-low/fail-high and actuator-command freeze/fail-low/fail-high as bounded temporary overlays on canonical controller inputs; no direct physical state writes.
- Added fail-closed canonical target/conflict validation and one-controller/one-actuator ambiguity checks for actuator-specific command faults.
- Added `InstrumentationControlFaultScenarioPack` demonstration and protection fail-safe diagnostic scenarios.
- Added M8.3 regression tests for measured-signal semantics, committed-frame protection ordering, control-command forcing/clearance, actuator-command freeze and built-in registry binding.
- Added ADR 0062, `docs/INSTRUMENTATION_CONTROL_FAULTS.md` and M8.3 milestone/handoff/status/roadmap updates.

## M8.2 — Hydraulic Component Faults — VALIDATED / HOTFIX 2

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

## M8.1 — Deterministic Fault-Injection Framework — VALIDATED / HOTFIX 1

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
