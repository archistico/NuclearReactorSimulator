# M10.9.4.1-F.1 Validation Checklist

**Status:** VALIDATED — user-confirmed 2026-07-26

**Validated parent:** M10.9.4.1-E.3.2 Hotfix 3

## Scope

F.1 adds an isolated typed ideal-vapor compressible-flow capacity law. It does not integrate relief or bypass topology into the plant runtime.

## Build and focused gate

```bat
dotnet build
scripts\run-choked-steam-flow-tests.cmd
```

The focused script must pass:

- `SpecificGasConstant` conversion and safety tests;
- compressible-flow definition validation;
- choked plateau invariance below the critical pressure ratio;
- continuous subcritical-to-critical transition;
- monotonic capacity as backpressure is reduced;
- linear area/opening scaling;
- one-way zero-flow behavior for closed area or non-positive forward head;
- one explicit current-v2 representative pressure-ratio audit;
- production of both F.1 CSV and summary artifacts.

## Ordinary suite

```bat
dotnet test
```

Validated candidate inventory expectation used for the successful gate:

- **970 passed**;
- **0 failed**;
- **27 explicit skipped**;
- **997 total**.

## Cumulative gates

```bat
scripts\run-electrical-protection-implementation-tests.cmd
scripts\run-electrical-protection-trajectory-audit.cmd
scripts\run-generator-grid-bidirectional-tests.cmd
scripts\run-turbine-admission-authority-audit.cmd
scripts\run-turbine-governor-actuator-tracking-audit.cmd
scripts\run-gameplay-long-tests.cmd
scripts\run-operational-envelope-audit.cmd
scripts\run-reference-plant-scale-audit.cmd
```

F.1 is mathematically isolated, but the cumulative gates must confirm that adding the new seam and metadata does not change the validated runtime baseline.

## Evidence review

Review the printed F.1 summary and confirm:

- mass flow is zero at pressure ratio 1.0;
- mass flow increases monotonically as downstream pressure falls;
- the sampled transition agrees with the analytic critical ratio;
- all choked samples share one capacity plateau;
- projected capacity scales linearly with area;
- the report states explicitly that no relief/bypass topology is active.

## Promotion result

F.1 was promoted after the user confirmed compilation and all tests passed and supplied the generated audit bundle. The reviewed summary confirmed critical ratio `0.545728`, choked capacity `0.788008677 kg/s` at `100 mm²`, linear area projections, monotonicity and the choked plateau. F.2 may therefore consume the capacity seam while remaining a separate topology candidate.
