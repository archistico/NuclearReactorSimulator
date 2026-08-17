# M10.9.4.1-H.3 Validation Checklist

H.3 Hotfix 1 is **VALIDATED**. Compilation, ordinary tests and the focused explicit audit passed. This validation does **not** authorize production activation; H.4 owns the bounded-cost hybrid gate.

Hotfix 1 is build-only/test-only: it replaces the collection-size assertion rejected by xUnit analyzer `xUnit2013` with `Assert.Single`; prototype behavior and expected inventory are unchanged.

## Gate

```bat
APPLY_UPDATE.cmd
dotnet build
scripts\run-semi-implicit-hydraulic-prototype-audit.cmd
dotnet test
```

Expected ordinary discovery:

```text
passed:   1037
failed:      0
skipped:    35 explicit
total:    1072
```

This gate was user-confirmed green. Preserve it as a cumulative regression while validating H.4 and later milestones.

## Focused H.3 requirements

Confirm that:

- all `SemiImplicitHydraulicPrototypeSolverTests` pass;
- the explicit frozen-forcing replay reproduces the reference inventory within 1e-6 kg / 1e-2 J;
- all 50 current-v2 prototype intervals converge within the bounded iteration budget;
- inventory integration residual remains within 1e-6 kg / 1e-2 J;
- hydraulic mass-rate closure remains within 1e-8 kg/s;
- hydraulic pump-energy ownership residual remains within 1e-3 W;
- deterministic repeat is exact;
- the audit emits chatter ratios, pressure ratio, iteration statistics, final-state gap and cost ratio;
- `production-semi-implicit-active=False` remains printed.

Do **not** promote H.4 merely because the H.3 test passes. Review the generated ratios first. Material numerical improvement, bounded cost and acceptable final-state behavior must be explicit evidence.

## Source-delta review

Confirm that H.3:

- adds the prototype beside, not inside, `PlantNetworkOrchestrator`;
- does not change `PipeFlowSolver`, `ValveFlowSolver`, `PumpFlowSolver` or `FluidNodeIntegrator` laws;
- changes no resistance, pump pressure boost, valve characteristic, controller tuning or protection threshold;
- changes no production factory/runtime route to select the prototype;
- preserves the fixed 10 ms current-v2 runtime and replay/checkpoint path;
- records H.2 as VALIDATED; H.3 Hotfix 1 was subsequently user-validated and is now the prerequisite for H.4.
