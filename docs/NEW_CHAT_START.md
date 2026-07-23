# New Chat Start — Nuclear Reactor Simulator

We are continuing the **Nuclear Reactor Simulator** project.

## Read first

1. `docs/PROJECT_HANDOFF.md`
2. `docs/PROJECT_STATUS.md`
3. `docs/ROADMAP.md`
4. `docs/ARCHITECTURE.md`
5. `docs/milestones/M10.9.4.md`
6. `docs/SUBSYSTEM_ENGINEERING_SCHEMATICS.md`
7. `docs/GAMEPLAY_LONG_RUNNING_SYSTEM_TESTS.md`
8. `docs/milestones/M10.9.3.md`
9. `docs/INTERACTIVE_FULL_PLANT_MIMIC.md`
10. `docs/ADVANCED_INSTRUMENT_GAUGE_SYSTEM.md`
11. `docs/OPERATOR_EXPERIENCE_HMI_ARCHITECTURE.md`
12. ADR 0075–ADR 0087

## Exact checkpoint

- M7, M8, M9 gates: **COMPLETE / VALIDATED**.
- M10.1–M10.9.3: **VALIDATED**.
- Official baseline: **M10.9.3 — Interactive Full-Plant Mimic**.
- Latest validated structural checkpoint: **M10.9.4 Hotfix 19 — Secondary-Pump Discharge Check Valves**.
- Current candidate: **M10.9.4 Hotfix 20 Fix 1 — Meaningful Secondary Protection Set / Initial Measured-Frame Completeness**.
- M10 closes only after **M10.9.8 — Integrated Human-Automation-HMI Validation Gate**.

## Approved M10.9 sequence

1. M10.9.1 HMI Information Architecture & Visual Language — VALIDATED
2. M10.9.2 Hotfix 2 Advanced Instrument & Gauge System — VALIDATED
3. M10.9.3 Interactive Full-Plant Mimic — VALIDATED
4. M10.9.4 Hotfix 17 — Condenser UA·ΔT Pressure Feedback — VALIDATED STRUCTURAL CHECKPOINT
5. M10.9.4 Hotfix 18 — Generator/Grid Synchronous Phase-Frequency Stiffness — VALIDATED STRUCTURAL CHECKPOINT
6. M10.9.4 Hotfix 19 — Secondary-Pump Discharge Check Valves — VALIDATED STRUCTURAL CHECKPOINT
7. M10.9.4 Hotfix 20 Fix 1 — Meaningful Secondary Protection Set / Initial Measured-Frame Completeness — CURRENT CANDIDATE
5. M10.9.5 Contextual Command Consequence Model
6. M10.9.6 Operational Challenge & Energy-Demand Framework
7. M10.9.7 Mission & Performance Workstation
8. M10.9.8 Integrated Human-Automation-HMI Validation Gate


## Hotfix 19 validated checkpoint / Hotfix 20 current step

Hotfix 17 established condenser UA·ΔT feedback and was validated. Hotfix 18 added generator/grid synchronous phase-frequency stiffness and was validated. Hotfix 19 then added current-v2 condensate/feedwater discharge check valves and was validated by the user with compilation, the ordinary suite and both explicit 60-second journeys green. Hotfix 20 is based directly on validated Hotfix 19 and changes only the current-v2 protection set: turbine overspeed, condenser high backpressure and generator overfrequency become measured latching trips; underfrequency is deferred until breaker-state supervision exists.

Current-v2 condenser law:

```text
ΔT = max(0, Tsteam - Tcoolant)
Qsurface = UA * ΔT
Qeffective = min(Qavailable, Qsurface)
```

Current v2 design values: `UA = 1.225 MW/K`, `Tcoolant = 20 °C`; at the existing 40 °C exhaust design point this reproduces exactly 24.5 MW. Run ordinary tests and both explicit journeys before advancing to generator-grid synchronous coupling.

## What M10.9.4 changes

- five detailed Application-owned engineering schematics: reactor/core, primary/steam-drum, turbine/secondary, generator/grid, instrumentation/control/protection;
- explicit IN/OUT and process/signal directions;
- distinct process/energy vs signal-flow grammar;
- generator GRID diagnostic separating shaft power, requested load, actual MWe, synchronization, breaker and protection state;
- requested generator load promoted as presentation-only data;
- explicit explanation that amber SHAFT means mechanical energy, not warning;
- separately runnable xUnit v3 explicit long-gameplay acceptance tests for sustained turbine→generator→grid behavior;
- versioned v2 generation-ready desktop/synchronization seeds while historical v1 origins remain exact;
- HMI turbine steam admission now uses actual effective stage-group flow instead of the legacy zero-valued M4.1 boundary seam.

## Important open verification

The first executable long-gameplay gate **did expose a real integrated balance defect** in the historical v1 operating seeds: the desktop journey reached ~1442.6 rpm / ~2.406 MWe with zero MODEL rotor shaft power after 10 simulated seconds, and the synchronization journey drove `control-out` outside the supported simplified water/steam envelope. Hotfix 6 preserves those historical v1 replay origins and introduces generation-ready v2 seeds with staged steam pressure, matched admission/condenser/feedwater capacity and bumpless control bias. The ordinary suite and explicit 60-second pack must both pass before M10.9.4 can be validated.

## Architecture rules that must not be broken

- deterministic fixed timestep independent of wall-clock/UI cadence;
- canonical M2/M3/M4/M5 ownership unchanged;
- measured consumers never substitute true/model state;
- protection overrides normal/supervisory control;
- Application owns presentation topology; Avalonia renders only;
- no UI-side predictive physics;
- save/load/checkpoint remains M9.1 replay-backed;
- no free-form/NLP control surface.

## Validate M10.9.4

```text
dotnet clean
dotnet restore
dotnet build --no-restore
dotnet test --no-build
scripts\run-gameplay-long-tests.cmd
```

Then manually verify `docs/milestones/M10.9.4.md`.

If the normal suite passes but a long gameplay test fails, diagnose the reported checkpoint/power-path evidence before promoting M10.9.4.


## M10.9.4 Hotfix 13 current correction

Hotfix 13 is a deliberate rebase on Hotfix 10, the last locally reported ordinary-green candidate. Hotfix 11/12 workaround branches are not part of the current package. The root cause is structural: the historical stage-flow law `min(stop, control, admission)` made the admission-train inventory monotonic because all three valves already transfer real mass while the stage source term drains only `turbine-inlet`. Current v2 uses an explicit pressure-driven turbine-inlet→exhaust expansion resistance; legacy null definitions retain the old law only for isolated compatibility.

Repeated `exhaust` failures were traced upstream of the condenser: the v2 recipe initialized both drum `steam` and main-steam `header` at 280 °C saturation, yielding zero canonical `steam → header` pressure difference and therefore zero main-steam-line replenishment. Downstream staged inventories initially masked this by feeding the turbine until they drained. Hotfix 8 adds an optional backward-compatible header steam initialization and uses a continuous v2 pressure staircase 280 / 275 / 269.5 / 253 / 246 °C through turbine inlet, targeting roughly 13 kg/s across every canonical steam-path element. v1 remains unchanged. Standard build/test must pass before the explicit 60-second gameplay pack is rerun.

## M10.9.4 Hotfix 15 historical correction

The Hotfix 14 ordinary suite is locally green. Long gameplay exposed a second monotonic inventory defect in the steam drum: historical separation cancelled canonical return inflow while leaving feedwater as a one-way addition. Hotfix 15 changes current v2 liquid recirculation to committed MCP demand, removing `dm_drum/dt = F_feedwater >= 0` by construction. Legacy behavior is isolated explicitly; current-model correctness has priority.
