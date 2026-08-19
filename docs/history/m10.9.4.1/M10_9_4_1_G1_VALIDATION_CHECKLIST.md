# M10.9.4.1-G.1 Validation Checklist

## Build and focused gate

```bat
APPLY_UPDATE.cmd
dotnet build
scripts\run-open-control-volume-energy-tests.cmd
```

Expected focused behavior:

- all `OpenControlVolumeEnergyTransportSolverTests` pass;
- the current-v2 representative audit contract passes;
- one explicit audit runs and emits both expected artifacts;
- the printed summary reports `runtime-migration-active=False`.

## Ordinary suite

```bat
dotnet test
```

Expected inventory relative to validated F.3 Hotfix 1:

```text
ordinary passed: 1003
explicit skipped: 30
total: 1033
failed: 0
```

## Generated evidence

```text
artifacts\g1-energy-transport-convention\
    01-current-v2-representative-open-control-volume-gap.csv
    01-current-v2-representative-open-control-volume-gap.summary.txt
```

Review:

- `h = u + p/rho` identity;
- `enthalpy transport = internal-energy advection + flow work`;
- exact equal-and-opposite internal closure;
- larger steam-path gap than liquid-path gap;
- no runtime migration or work retuning.

## Cumulative gates

```bat
scripts\run-turbine-bypass-tests.cmd && scripts\run-main-steam-relief-tests.cmd && scripts\run-choked-steam-flow-tests.cmd && scripts\run-electrical-protection-implementation-tests.cmd && scripts\run-electrical-protection-trajectory-audit.cmd && scripts\run-generator-grid-bidirectional-tests.cmd && scripts\run-turbine-admission-authority-audit.cmd && scripts\run-turbine-governor-actuator-tracking-audit.cmd && scripts\run-gameplay-long-tests.cmd && scripts\run-operational-envelope-audit.cmd && scripts\run-reference-plant-scale-audit.cmd
```

Promote G.1 only after compilation, focused/ordinary gates, cumulative gates and the generated summary/CSV are confirmed by the user.
