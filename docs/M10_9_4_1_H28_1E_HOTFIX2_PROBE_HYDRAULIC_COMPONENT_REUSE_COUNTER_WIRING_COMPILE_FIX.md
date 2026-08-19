# M10.9.4.1-H.28.1-E Hotfix 2 — Probe Hydraulic-Component Reuse Counter Wiring Compile Fix

## Failure observed

The user-applied H.28.1-E Hotfix 1 progressed past the namespace errors but `NuclearReactorSimulator.Simulation` failed compilation with one CS0103 at `JacobianHydraulicCorrectorSolver.cs`: `probeHydraulicComponentReuse` did not exist in the local `TryEvaluateProbeTrial` context.

## Root cause

H.28.1-E introduced a diagnostic-only hydraulic-component reuse counter at the outer H.9 invocation. The innermost probe residual call consumed that counter, but the parameter had not been propagated through the complete private helper chain.

## Repair

The existing counter is passed explicitly through:

`TryBuildNewtonTarget` → `TryEvaluateProbeResidual` → `TryEvaluateProbeResidualWithSignedStep` → `TryEvaluateProbeTrial`.

Both positive and negative finite-difference signed-step paths pass the same counter. No new counter is created per probe.

## Numerical impact

None. The counter is attribution-only. H.9 mathematics, finite-difference perturbations, 32 probes, 35 logical hydraulic evaluations, component exact-reference reuse decisions, H.20/H.22, P060/F040 and the 10 ms fixed step are unchanged.

## Static verification

The modified helper declarations and calls were checked structurally: 12/12, 18/18, 17/17 and 13/13 arguments. The Simulation wall-clock architecture scan remains unchanged and no additional runtime source file is modified by this hotfix.
