# M10.9.4.1-H.28.1-C Hotfix 2 — IReadOnlyList Probe-State Compile Fix

## Status

CANDIDATE. H.27 Hotfix 1 remains the authoritative validated numerical baseline; H.28.1-A Hotfix 2 remains validated diagnostic evidence; H.28 remains failed and H.29 remains blocked.

## Local failure addressed

After Hotfix 1 resolved the missing `FluidNodeState` namespace, the next local build failed with one CS0266 in `JacobianHydraulicCorrectorSolver.cs`: `iterateFluidNodes` had been inferred as `FluidNodeState[]` from its initializer, but an accepted line-search state is exposed as `IReadOnlyList<FluidNodeState>`.

## Repair

The local variable is declared explicitly as `IReadOnlyList<FluidNodeState>` from initialization. This is the abstraction already used by the optimized probe path and by `LineSearchAcceptance.FluidNodes`. The repair adds no cast and deliberately avoids `.ToArray()`, so it introduces no new probe-state allocation.

## Numerical contract

No H.9 mathematics, finite-difference probe count, Jacobian dimension, residual/tolerance, P060/F040 trigger, hysteresis, H.20 authority, H.22 ownership, physical coefficient or 10 ms fixed-step contract changes.
