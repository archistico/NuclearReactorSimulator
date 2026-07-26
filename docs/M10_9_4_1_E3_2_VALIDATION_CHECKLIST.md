# M10.9.4.1-E.3.2 Hotfix 3 Validation Checklist

**Candidate:** Evidence-Derived Electrical Protection with Typed Breaker-Command Audit Fix

**Validated parent:** M10.9.4.1-E.3.1 Hotfix 1

## Scope

E.3.2 adds canonical M5.5 delayed and supervised protection functions for current-v2 reverse power, underfrequency and loss of synchronism. Legacy/default definitions remain immediate and unsupervised.

Hotfix 1 added exactly one initial measured signal for each new E.3.2 instrumentation channel: breaker state and absolute frequency slip. Hotfix 2 corrected the compile-time grid contract used by that seed: `ElectricalGridDefinition.NominalFrequency` is the canonical definition member. The Hotfix 2 focused run then passed compilation, all nine Simulation tests and all thirteen non-explicit Application tests; two of the three explicit implementation journeys passed. Hotfix 3 corrects only the remaining breaker-open audit helper so `GeneratorBreakerOpen` targets the canonical breaker id with `ControlRoomCommandTargetKind.Breaker`. No protection calibration or runtime behavior is otherwise changed.

## Build and focused gate

```bat
dotnet build
scripts\run-electrical-protection-implementation-tests.cmd
```

The focused script must pass:

- generic delayed/supervised M5.5 solver regressions;
- current-v2 desktop/synchronization definition, threshold and HMI marker contracts;
- normal 5 -> 0 -> 5 MWe no-trip trajectory;
- turbine-trip reverse-power generator trip and breaker opening;
- breaker-open coastdown supervision;
- replay and checkpoint restoration while reverse-power pickup is in flight;
- three generated implementation summaries and detailed CSV files under `artifacts\e3-protection-implementation`.

## Ordinary suite

```bat
dotnet test
```

Expected candidate inventory, assuming no unrelated test changes:

- **960 passed**;
- **0 failed**;
- **26 explicit skipped**;
- **986 total**.

## Evidence and cumulative explicit gates

```bat
scripts\run-electrical-protection-trajectory-audit.cmd
scripts\run-generator-grid-bidirectional-tests.cmd
scripts\run-turbine-admission-authority-audit.cmd
scripts\run-turbine-governor-actuator-tracking-audit.cmd
scripts\run-gameplay-long-tests.cmd
scripts\run-operational-envelope-audit.cmd
scripts\run-reference-plant-scale-audit.cmd
```

The E.3.1 evidence script deliberately uses the protected-current-v2 physical recipe with the E.3.2 relay set disabled, so the original calibration trajectories remain reproducible. The normal current-v2 runtime uses E.3.2 protection.

## Manual GENERATOR-station check

Confirm:

- the signed electrical-power scale shows a reverse-power trip marker at -0.3 MWe;
- the frequency scale shows underfrequency at 48.8 Hz and overfrequency at 53 Hz;
- normal load reduction and restoration do not latch generator trip;
- prime-mover trip with breaker closed eventually opens the breaker through generator trip;
- after the initiating condition is no longer eligible, canonical reset can clear the latch;
- breaker-open coastdown does not create a generator trip.

## Promotion rule

Promote E.3.2 Hotfix 3 only after build, focused tests, ordinary suite, all cumulative explicit gates and the manual marker/reset review pass with zero failures.
