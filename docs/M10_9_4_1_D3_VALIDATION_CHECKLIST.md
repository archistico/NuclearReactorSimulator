# M10.9.4.1-D.3 Validation Checklist

D.1 and D.2 Hotfix 1 are locally validated. D.3 is audit-only and must not change production physics/control laws.

## Build and ordinary suite

```text
dotnet clean
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

## Dedicated D.3 gate

```text
scripts\run-turbine-governor-actuator-tracking-audit.cmd
```

The audit must:

1. create a controller-command/control-valve-position gap > 5 percentage points;
2. print the maximum gap, maximum integral excursion while lagged, final integral offset and maximum speed error;
3. remain below 2 controller-output percentage points of integral excursion while materially lagged to close D.3 without a control-law change.

If the second explicit audit test fails at the 2-point gate, do not loosen the threshold. Treat the printed evidence as the trigger for D.3.1 actuator-position tracking anti-windup.

## Existing physical gates

After the D.3 audit, preserve the validated physical envelope:

```text
scripts\run-gameplay-long-tests.cmd
scripts\run-operational-envelope-audit.cmd
```
