# M10.9.4.1-E.3.1 Validation Checklist

## Candidate

**Signed Electrical Protection Trajectory Audit — Hotfix 1 candidate**

Validated parent: **M10.9.4.1-E.2 Hotfix 1**. The candidate includes a test-only compile hotfix that keeps each `FormattableString.Invariant` argument as a real `FormattableString`; runtime and audit semantics are unchanged.

## Scope lock

- no reverse-power relay;
- no generator underfrequency relay;
- no loss-of-synchronism relay;
- no protection threshold or delay change;
- no reactor, primary, turbine, condenser, governor or timestep retuning;
- no historical/default profile migration.

## Automated validation

### Build

```text
dotnet build
```

Expected: zero warnings and zero errors.

### Focused E.3.1 audit

```text
scripts\run-electrical-protection-trajectory-audit.cmd
```

Expected:

- 4/4 explicit trajectory tests pass;
- summary is printed to the console;
- CSV and summary files are created under `artifacts\e3-protection-trajectories`;
- turbine-trip/zero-request/breaker-closed trajectory contains negative grid exchange;
- breaker-open coastdown contains falling frequency without generator-trip action;
- all phase-sweep values remain finite, breaker closed and conversion loss non-negative.

### Ordinary suite

```text
dotnet test
```

Expected discovery if the validated E.2 count is unchanged:

- 952 passed;
- 0 failed;
- 23 explicit skipped;
- 975 total.

### Existing explicit gates

```text
scripts\run-generator-grid-bidirectional-tests.cmd
scripts\run-turbine-admission-authority-audit.cmd
scripts\run-turbine-governor-actuator-tracking-audit.cmd
scripts\run-gameplay-long-tests.cmd
scripts\run-operational-envelope-audit.cmd
scripts\run-reference-plant-scale-audit.cmd
```

All previously validated results must remain green.

## Evidence handoff

After the focused audit, preserve or paste:

```text
artifacts\e3-protection-trajectories\*.summary.txt
```

The CSV files should remain available for detailed threshold review.

## Promotion rule

E.3.1 becomes validated only after the user confirms:

1. compilation succeeds;
2. all four trajectory tests pass and reports are produced;
3. ordinary and existing explicit/long-running gates remain green;
4. the generated summaries are reviewed for E.3.2 threshold design.

Passing E.3.1 does **not** itself validate any reverse-power, underfrequency or loss-of-synchronism threshold.
