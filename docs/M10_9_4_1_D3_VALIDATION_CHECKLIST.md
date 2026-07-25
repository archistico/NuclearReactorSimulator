# M10.9.4.1-D.3 Validation Checklist — superseded by D.3.1

D.1, D.2 and D.2 Hotfix 1 are user-validated. D.3 evidence executed and exposed a missing breaker-open rotor deceleration path. Use `M10_9_4_1_D3_1_VALIDATION_CHECKLIST.md` for the active candidate.

## Build and ordinary suite

```text
dotnet clean
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

Expected:

- zero compilation errors;
- repository warning policy remains green;
- ordinary regression suite passes;
- `CurrentV2GovernorContracts_FreezeDistinctSustainedProfilesWithoutRetuning` passes.

## Re-run corrected D.2 authority audit

```text
scripts\run-turbine-admission-authority-audit.cmd
```

Confirm the operational ±10 rpm journey now uses the breaker-open sustained synchronization seed and reports:

- breaker open;
- effective governor setpoint exactly +10 rpm and then restored;
- control-valve, turbine-inlet pressure, commanded/effective flow and shaft-power evidence.

## D.3 governor/actuator audit

```text
scripts\run-turbine-governor-actuator-tracking-audit.cmd
```

Review both evidence blocks.

### Breaker-open block

- effective setpoint changes by +10 rpm and returns;
- the first committed `0.01 s` event sample is present after both commands;
- the raise event produces a directional controller-output and physical-valve response before the audit resumes its `0.1 s` cadence;
- P/I/D, saturation and existing anti-windup fields remain finite;
- command-to-valve gap is quantified;
- rotor/flow/power response remains finite.

### Breaker-closed block

- requested load changes 5 → 10 → 5 MWe;
- effective droop setpoint changes by +0.75 rpm and returns;
- controller/valve/integral response is captured;
- no isolated retuning is performed to compensate for the unresolved 1,000 MWe scale contract.

## Existing long-running gates

```text
scripts\run-gameplay-long-tests.cmd
scripts\run-operational-envelope-audit.cmd
```

D.3 changes no production physics, so any new physical divergence is a regression or an integration error.

## Acceptance and next decision

D.3 Hotfix 1 can be marked validated when build, ordinary suite, corrected D.2 audit and D.3 dedicated audit pass and their output is reviewed. The first-step capture fixes audit observability only; it must not be interpreted as a controller retune.

Then choose exactly one outcome:

1. **No material actuator windup:** close Phase D without a tracking-law change and proceed to Phase E reference-scale/bidirectional-coupling work.
2. **Material actuator-induced windup:** prepare a separate D.3.x versioned tracking anti-windup candidate, without combining it with scale migration or turbine hydraulic retuning.
