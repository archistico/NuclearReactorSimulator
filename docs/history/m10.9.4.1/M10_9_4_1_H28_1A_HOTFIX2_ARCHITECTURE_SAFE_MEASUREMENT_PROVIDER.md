# M10.9.4.1-H.28.1-A Hotfix 2 — Architecture-Safe Measurement Provider

## Status

**CANDIDATE.** H.27 Hotfix 1 remains the authoritative validated baseline. H.28 remains failed performance evidence and H.29 remains blocked.

## Trigger

H.28.1-A Hotfix 1 compiled, but the ordinary suite failed exactly one architecture rule:

```text
ArchitectureRulesTests.SimulationProject_DoesNotUseWallClockTimerOrDelayApis
Forbidden timing API token 'Stopwatch' found in FourNodeBranchContinuityShadowIntegrationSolver.cs
```

The direct timing calls were diagnostic-only, but the architecture rule is intentional: deterministic Simulation code must not own wall-clock/timer APIs.

## Fix

Hotfix 2 keeps the same cost-center attribution while moving measurement-source ownership outside Simulation:

- `src/NuclearReactorSimulator.Simulation` contains no direct `Stopwatch`, `DateTime.*`, timer or delay API use;
- Simulation calls only internal `PerformanceAttributionMeasurement.ReadTimestamp()` / `ReadAllocatedBytes()`;
- the measurement seam returns zero when no audit provider is installed;
- the explicit H.28.1-A Application test temporarily injects `Stopwatch.GetTimestamp` and `GC.GetAllocatedBytesForCurrentThread`;
- scope disposal restores the previous provider;
- public/production factories cannot configure the provider.

Timing/allocation values remain observational only and stay outside deterministic result/telemetry equality in weak-reference registries.

## Unchanged contracts

No change to P060/F040, H.9 finite-difference Newton mathematics/tolerances, 2%/5 K hysteresis, the four-node target set, H.20 authority, H.22 commit seam, physical coefficients, fixed step, standard factory mode or frozen H.27/H.28 evidence.

## Validation

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-four-node-performance-attribution-audit.cmd
```

The ordinary architecture rule must pass before the focused attribution gate is accepted.
