# M10.9.4.1-H.28.1-C Hotfix 1 — FluidNodeState Namespace Compile Fix

## Status

CANDIDATE. The authoritative numerical baseline remains H.27 Hotfix 1 VALIDATED; H.28 remains FAILED performance evidence; H.28.1-A Hotfix 2 remains validated diagnostic evidence; H.29 remains blocked.

## Failure observed

The first local H.28.1-C build failed with eight CS0246 errors in `JacobianHydraulicCorrectorSolver.cs` because the optimized probe path now names `FluidNodeState` directly but the file did not import its declaring namespace.

`FluidNodeState` is declared in:

```text
NuclearReactorSimulator.Domain.Physics.Fluids
```

## Repair

Hotfix 1 adds exactly:

```csharp
using NuclearReactorSimulator.Domain.Physics.Fluids;
```

to `JacobianHydraulicCorrectorSolver.cs`.

No formulas, finite-difference probe count, Jacobian dimension, residual thresholds, H.20/H.22 authority/ownership behavior, P060/F040 trigger, hysteresis limits, target nodes, physical coefficients or fixed timestep are changed. The H.28.1-C allocation/hot-path optimization itself is unchanged.

## Validation

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-h9-jacobian-probe-hot-path-optimization-audit.cmd
```

H.28.1-C Hotfix 1 must remain CANDIDATE until all three validation layers pass locally.
