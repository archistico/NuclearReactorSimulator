# M10.9.4.1-H.28.1-E Hotfix 1 — Domain Fluid-State Namespace Compile Fix

## Status

CANDIDATE. Built over the unvalidated H.28.1-E candidate after its first local build attempt.

## Observed failure

The user-reported local `dotnet build` failed only in `SemiImplicitHydraulicEvaluation.cs` with three `CS0246` errors: `FluidNodeState`, `ValveState` and `PumpState` were referenced by the new internal `HydraulicComponentEvaluationSnapshot`, but their Domain namespace was not imported.

All three types are declared in `NuclearReactorSimulator.Domain.Physics.Fluids`.

## Repair

The runtime delta is exactly one import:

```csharp
using NuclearReactorSimulator.Domain.Physics.Fluids;
```

No cast, copy, state conversion or algorithm change is introduced.

## Preserved contracts

- H.9 finite-difference Newton mathematics unchanged.
- 35 logical hydraulic evaluations / 32 probes / Jacobian dimension 32 unchanged.
- Exact component-reference reuse and fail-closed full-evaluation fallback unchanged.
- Continuity/corrected thermodynamic fast path unchanged; standard ExplicitCommittedState path unchanged.
- P060/F040, 2%/5 K continuity limits, H.20 authority, H.22 commit ownership and 10 ms fixed step unchanged.
- H.28 Requalification 1 remains failed only on p95 and H.29 remains blocked.

## Local gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-hydraulic-probe-cpu-tail-reduction-audit.cmd
```
