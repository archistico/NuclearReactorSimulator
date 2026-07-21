# Cold Shutdown & Pre-Startup

## Scope

M7.2 is the first concrete operational content built on the validated M7.1 versioned initial-condition/scenario framework. It introduces one exact initial condition, one bounded training scenario and a presentation-only readiness/guidance layer.

It deliberately ends before first criticality.

## Exact initial condition

The canonical reference is:

```text
InitialConditionId = cold-shutdown-pre-start
Version            = 1
```

`ColdShutdownInitialConditionFactory` is the registered Application factory for that exact version. Resolution remains exact: no "latest" fallback is introduced.

The factory reconstructs a fresh lower-layer composition rather than deserializing or patching individual authoritative states. It uses the validated M1–M5 ownership chain, `IntegratedAutomaticOperationSolver`, `IntegratedAutomaticOperationRuntimeEngine` and `SimplifiedWaterSteamThermodynamicModel`.

The v1 recipe establishes an educational cold/pre-start baseline with:

- modeled fission/neutron baseline at zero;
- modeled control rods fully inserted;
- main-circulation, condensate and feedwater pumps initially stopped;
- stop/control/admission valves closed;
- turbine rotor stationary;
- generator breaker open;
- healthy initial instrumentation and clear protection/alarm baseline.

These are initial-condition recipe choices, not new physics models. Once loaded, all evolution remains owned by the validated deterministic runtime.

## Deterministic seed boundary

M5.7 publishes its complete immutable operational snapshot as the result of a committed deterministic step. The factory therefore performs one deterministic fixed-step seed from the constructed recipe and uses that committed candidate/snapshot as the exact v1 runtime seed while exposing logical step `0` to the new session.

This seed is part of the versioned construction recipe. It is not a scenario-time patch, checkpoint restore or UI correction. Changing its semantics requires a new initial-condition version.

## Scenario permissions

`ColdShutdownPreStartupProgram.Scenario` permits only actions needed to preserve/verify shutdown and prepare modeled main circulation, including SCRAM/reset, MCP start/stop, turbine/generator trip/open isolation and annunciator memory actions.

The scenario intentionally does **not** permit:

- control-rod withdrawal;
- generator-breaker closure;
- turbine speed/load increase commands.

The permission boundary therefore stops before M7.3 first criticality and later turbine/grid milestones.

## Readiness checks

`PreStartupChecklistEvaluator` reads only `ControlRoomSnapshot`. It never traverses `FullPlantSnapshot`, `PlantState` or Simulation state directly.

The M7.2 checklist observes:

1. measured-signal health;
2. protection clear;
3. reactor shutdown thermal-power baseline;
4. control rods inserted;
5. initial MCP stopped condition;
6. prepared MCP running condition;
7. turbine stopped;
8. generator breakers open;
9. steam-admission path closed;
10. annunciator clear.

Thresholds are presentation/readiness tolerances only. They do not alter protection thresholds or physical solver behavior.

## Guided preparation

`PreStartupGuidancePlan` orders four educational steps:

1. verify the safe cold-shutdown baseline;
2. verify steam isolation and auxiliary/pre-start conditions;
3. establish modeled main circulation through the normal typed MCP command path;
4. confirm the pre-criticality handoff.

A guidance step may expose a `SuggestedOperatorAction`, but guidance never executes it. Completion is inferred only from named observational checks after the operator/runtime has acted.

## Desktop composition

M7.2 changes the production composition root from the previous no-session fallback to:

1. register `ColdShutdownInitialConditionFactory` in `VersionedInitialConditionRegistry`;
2. load `ColdShutdownPreStartupProgram.Scenario` through `ScenarioSessionFactory`;
3. start the coordinator in `Paused` state;
4. provide only `IControlRoomSnapshotSource`, `IControlRoomCommandDispatcher` and declarative guidance to Avalonia.

Avalonia still has no Simulation dependency and constructs no physical state.

## Pre-criticality handoff

M7.2 is complete at the operational boundary where the cold-shutdown baseline is verified and modeled main circulation can be established while:

- reactor shutdown remains preserved;
- rods remain inserted;
- turbine remains stopped;
- generator remains isolated;
- steam admission remains closed;
- protection/instrumentation remain healthy.

M7.3 owns controlled rod withdrawal, approach to criticality and low-power stabilization.
