# M10.9.4.1-H.1 Validation Checklist

> **Status:** VALIDATED — user-confirmed H.1 focused evidence and test gate passed. The checklist is retained as the reproducible historical gate.

## Required local gate

```bat
APPLY_UPDATE.cmd
dotnet build
scripts\run-numerical-stiffness-decision-audit.cmd
dotnet test
```

Expected ordinary discovery after H.1 additions:

```text
passed:   1031
failed:      0
skipped:    34 explicit
total:    1065
```

The focused gate must generate:

```text
artifacts\h1-numerical-stiffness\
    01-current-v2-fixed-step-stiffness-sweep.csv
    01-current-v2-fixed-step-stiffness-sweep.summary.txt
    02-current-v2-final-state-convergence.csv
```

## Evidence requirements

Confirm that:

- production desktop current-v2 still owns a 10 ms fixed step;
- H.1 evidence runs use 10, 5 and 2.5 ms exactly;
- all three preserve the same 20 ms seed-preconditioning duration;
- each five-second run remains finite and trip-free at the healthy desktop point;
- mass/energy conservation residuals remain inside the existing validated bounds;
- raw primary pump/channel/return one-step changes are reported rather than filtered;
- maximum fractional mass, internal-energy and subcooled-liquid pressure changes include the owning node id;
- final-state 10→5 and 5→2.5 ms differences are recorded for every listed metric;
- wall-time cost is observational only and never changes the deterministic simulation path;
- `adaptive-substepping-active=False`;
- `semi-implicit-treatment-active=False`;
- `decision-deferred-to-H.2=True`.

## Cumulative gates

After the focused H.1 gate and ordinary suite are green, rerun at minimum:

```bat
scripts\run-turbine-expansion-enthalpy-tests.cmd
scripts\run-remaining-non-turbine-enthalpy-tests.cmd
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

## Promotion rule

Do not select adaptive substepping or semi-implicit coupling merely because H.1 compiles. First promote H.1 only after the user confirms the gate and supplies/reviews the generated evidence. H.2 is the explicit method-selection milestone.
