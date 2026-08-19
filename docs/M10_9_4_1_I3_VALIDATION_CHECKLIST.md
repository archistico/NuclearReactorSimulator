# M10.9.4.1-I.3 Validation Checklist

## Candidate

M10.9.4.1-I.3 — Reference Trajectories, Conservation/Inventory Baseline & Tolerance Budgets.

## Prerequisite

- I.2 user-validated.
- H.30 remains `OPT-IN ONLY`.
- exact v2 remains authoritative `ExplicitCommittedState` default/rollback/reference.
- exact v3 remains qualified corrected opt-in.

## Required local gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-phase-i-reference-trajectory-conservation-inventory-baseline-audit.cmd
```

## Required focused evidence

- frozen I.2 fingerprints match;
- exact reference contract is v2 explicit, 10 ms, 300 s / 30,000 steps;
- 301 one-second samples are produced;
- the 300 operating samples from t=1 s through t=300 s remain trip-free and breaker-closed;
- requested/gross/shaft floors remain healthy across those 300 operating samples; t=0 is retained only as the initial reference point;
- mass closure <= `1e-6 kg`;
- energy closure <= `1e-2 J`;
- balance mass-rate residual <= `1e-8 kg/s`;
- balance power residual <= `1e-3 W`;
- seven final-window inventory slopes are finite;
- exactly 19 versioned tolerance-budget entries are produced, finite and positive;
- H.24 and H.28 are not rerun;
- runtime behavior remains unchanged.

## Promotion flags

```text
phase-i-reference-trajectory-baseline-passes=True
phase-i-conservation-inventory-baseline-passes=True
i3-audit-passes=True
phase-i-reference-tolerance-baseline-established=True
```

Do not promote I.3 from implementation alone. Record the user's local build/test/focused-gate confirmation before using its generated budgets as frozen reference evidence.

## Hotfix 1 diagnostic amendment

The initial I.3 run failed at 55 s on `shaft power > 4.5 MW`. Hotfix 1 does not relax this criterion. It must complete all 300 s and write `06-generation-health-violations.csv` plus `07-shaft-drop-episodes.csv` before the final health assertion. A red focused result with complete diagnostic artifacts is expected if the runtime drop persists; that result is diagnostic evidence, not validation.
