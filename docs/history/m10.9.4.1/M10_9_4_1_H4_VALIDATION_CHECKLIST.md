# M10.9.4.1-H.4 Validation Checklist

**Validation result: PASSED.** H.3 Hotfix 1 was the prerequisite baseline; H.4 passed its audit/decision gate and authorized H.5 while correctly leaving production explicit in the validated H.4 source.

## Gate

```bat
APPLY_UPDATE.cmd
dotnet build
scripts\run-hybrid-semi-implicit-activation-gate.cmd
dotnet test
```

Expected ordinary discovery after H.4 additions:

```text
passed:   1041
failed:      0
skipped:    36 explicit
total:    1077
```

Then rerun the cumulative H/G/F/E/D gates listed in `PROJECT_HANDOFF.md` before promoting H.4.

## Focused H.4 requirements

Confirm that:

- all `HybridSemiImplicitHydraulicGateSolverTests` pass;
- the H.3 prototype regressions remain green;
- the explicit reference replay still reproduces the validated current-v2 inventories;
- every swept candidate preserves inventory integration and hydraulic ownership residual limits;
- the selected candidate is exactly deterministic when repeated;
- trigger and candidate selection depend only on simulation values, never wall-clock time;
- the summary reports corrections, deterministic work ratio, chatter ratios, final-state gaps and observational wall cost;
- `production-hybrid-active=False` remains printed;
- `production-fixed-step=10.000 ms` remains printed;
- the H.3/H.4 summaries no longer emit a UTF-8 BOM marker when printed by the Windows script.

## Decision interpretation

If the summary reports:

```text
activation-criteria-met=True
```

H.4 permits preparation of a **separate production-integration candidate**. Do not treat the H.4 source itself as production-hybrid enabled.

If it reports:

```text
activation-criteria-met=False
```

retain the validated explicit 10 ms production path. Continue numerical optimization/redesign; do not weaken conservation, deterministic replay, physical coefficients or activation thresholds merely to force activation.

## Source-delta review

Confirm that H.4:

- adds `HybridSemiImplicitHydraulicGateSolver` beside the H.3 prototype;
- does not modify `PlantNetworkOrchestrator` production routing;
- does not change `PipeFlowSolver`, `ValveFlowSolver`, `PumpFlowSolver` or `FluidNodeIntegrator` laws;
- changes no resistance, pump boost, valve characteristic, controller tuning or protection threshold;
- changes no production initial-condition factory route;
- preserves fixed 10 ms production current-v2;
- the validated H.4 source records H.3 Hotfix 1 as its prerequisite baseline and contains no production hybrid activation; H.5 is the later separately authorized integration candidate.
