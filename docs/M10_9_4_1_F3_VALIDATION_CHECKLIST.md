# M10.9.4.1-F.3 Hotfix 1 Validation Checklist

Status: **CANDIDATE — user validation pending**

## Build and focused gate

```text
dotnet build
scripts/run-turbine-bypass-tests.cmd
```

The focused gate must confirm:

- typed bypass definition and condenser-system topology contracts;
- exact internal header-to-exhaust mass and internal-energy transfer;
- committed condenser backpressure and reverse-flow blocking;
- vapor-quality capacity limiting;
- current-v2 opt-in and legacy empty default;
- both explicit audit sweeps and all four artifacts.

## Ordinary suite

```text
dotnet test
```

Expected discovery if no unrelated tests change:

```text
passed:   994
failed:   0
skipped:  29 explicit
total:    1023
```

## Cumulative gates

```text
scripts/run-main-steam-relief-tests.cmd
scripts/run-choked-steam-flow-tests.cmd
scripts/run-electrical-protection-implementation-tests.cmd
scripts/run-electrical-protection-trajectory-audit.cmd
scripts/run-generator-grid-bidirectional-tests.cmd
scripts/run-turbine-admission-authority-audit.cmd
scripts/run-turbine-governor-actuator-tracking-audit.cmd
scripts/run-gameplay-long-tests.cmd
scripts/run-operational-envelope-audit.cmd
scripts/run-reference-plant-scale-audit.cmd
```

## Required audit review

Confirm that:

- bypass remains closed through 6.40 MPa and is fully open from 6.50 MPa;
- source-pressure capacity is monotonic and exceeds 12 kg/s at the high end;
- active low-backpressure samples are choked;
- the backpressure sweep uses positive absolute pressure, shows a stable choked plateau below the analytic critical ratio and declines monotonically thereafter;
- equal source/destination pressure produces zero flow;
- external mass and power remain zero;
- source removal exactly equals destination addition for both mass and internal energy;
- F.2 atmospheric relief remains separate and unchanged.

## Promotion rule

Promote F.3 only after compilation, the focused gate, ordinary suite, cumulative gates and generated summaries/CSVs are confirmed by the user.
