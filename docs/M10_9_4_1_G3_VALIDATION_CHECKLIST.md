# M10.9.4.1-G.3 validation checklist

> **Status: VALIDATED.** Gate confirmed by the user; retained as the historical promotion contract.

## Build and focused gate

```bat
APPLY_UPDATE.cmd
dotnet build
scripts\run-remaining-non-turbine-enthalpy-tests.cmd
```

Expected focused behavior:

- all new definition-owned modes default to historical `SpecificInternalEnergy`;
- both current-v2 sustained profiles opt all remaining non-turbine owners into `SpecificEnthalpy`;
- legacy/current-v1 definitions remain historical;
- current-v2 pump paths use enthalpy while hydraulic fluid work and shaft demand remain single-counted;
- drum steam/liquid separation applies enthalpy and closes mass/energy internally;
- positive current-v2 external feedwater requires explicit incoming enthalpy;
- external feedwater/export/admission source terms match their selected energy exchange;
- condenser heat rejection equals selected steam energy removed minus selected condensate energy added;
- atmospheric relief remains a matching external exchange;
- turbine bypass remains a conservative internal transfer with zero external exchange;
- turbine expansion remains unchanged for G.4;
- the G.3 CSV and summary are generated and printed.

## Ordinary suite

```bat
dotnet test
```

Expected ordinary discovery after G.3 additions:

```text
passed:   1025
failed:   0
skipped:  32 explicit
total:    1057
```

## Generated evidence

Confirm these files exist:

```text
artifacts\g3-remaining-non-turbine-enthalpy\
    01-current-v2-remaining-non-turbine-enthalpy.csv
    01-current-v2-remaining-non-turbine-enthalpy.summary.txt
```

The summary must report:

- `runtime-remaining-migration-active=True`;
- `node-inventories-remain-internal-energy=True`;
- `pump-hydraulic-work-single-count=True`;
- `condenser-heat-rejection-single-count=True`;
- `relief-external=True`;
- `bypass-internal=True`;
- `turbine-expansion-migration-active=False`;
- negligible maximum ownership residual.

## Cumulative gates

```bat
scripts\run-passive-hydraulic-enthalpy-tests.cmd
scripts\run-open-control-volume-energy-tests.cmd
scripts\run-turbine-bypass-tests.cmd
scripts\run-main-steam-relief-tests.cmd
scripts\run-choked-steam-flow-tests.cmd
scripts\run-electrical-protection-implementation-tests.cmd
scripts\run-electrical-protection-trajectory-audit.cmd
scripts\run-generator-grid-bidirectional-tests.cmd
scripts\run-turbine-admission-authority-audit.cmd
scripts\run-turbine-governor-actuator-tracking-audit.cmd
scripts\run-gameplay-long-tests.cmd
scripts\run-operational-envelope-audit.cmd
scripts\run-reference-plant-scale-audit.cmd
```

## Promotion evidence

Promote G.3 only after the user confirms:

- clean build with zero warnings/errors;
- focused G.3 gate passes, including the explicit audit;
- ordinary suite passes;
- all cumulative gates pass;
- no ownership residual or unexpected external exchange appears;
- current-v2 operational trajectories remain acceptable without hidden turbine/controller retuning.
