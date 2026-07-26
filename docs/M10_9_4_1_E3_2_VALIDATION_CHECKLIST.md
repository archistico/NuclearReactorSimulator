# M10.9.4.1-E.3.2 Hotfix 3 Validation Checklist

**Milestone:** Evidence-Derived Electrical Protection

**Status:** VALIDATED on 2026-07-26

**Validated parent:** M10.9.4.1-E.3.1 Hotfix 1

## Scope

E.3.2 adds canonical M5.5 delayed and supervised protection functions for current-v2 reverse power, underfrequency and loss of synchronism. Legacy/default definitions remain immediate and unsupervised.

Hotfix 1 added exactly one initial measured signal for each new E.3.2 instrumentation channel: breaker state and absolute frequency slip. Hotfix 2 corrected the compile-time grid contract used by that seed to `ElectricalGridDefinition.NominalFrequency`. Hotfix 3 corrected only the explicit breaker-open audit helper so `GeneratorBreakerOpen` targets the canonical breaker id with `ControlRoomCommandTargetKind.Breaker`.

## Validated automatic gate

The user confirmed all of the following passed:

```bat
dotnet build
scripts\run-electrical-protection-implementation-tests.cmd
dotnet test
scripts\run-electrical-protection-trajectory-audit.cmd
scripts\run-generator-grid-bidirectional-tests.cmd
scripts\run-turbine-admission-authority-audit.cmd
scripts\run-turbine-governor-actuator-tracking-audit.cmd
scripts\run-gameplay-long-tests.cmd
scripts\run-operational-envelope-audit.cmd
scripts\run-reference-plant-scale-audit.cmd
```

The focused implementation gate passed:

- 9/9 Simulation tests;
- 13/13 non-explicit Application tests;
- 3/3 explicit implementation journeys.

## Reviewed implementation evidence

The user supplied the complete generated `artifacts/e3-protection-implementation` CSV and summary bundle.

### Normal 5 -> 0 -> 5 MWe

- 5,000 samples over 50.000 s;
- grid exchange -0.704181 to 5.643619 MWe;
- frequency 48.719440 to 50.671967 Hz;
- maximum absolute slip 1.280560 Hz;
- maximum reverse-power pickup 0.080 s;
- maximum underfrequency pickup 0.640 s;
- no synchronism pickup;
- no generator trip.

### Turbine trip / reverse power

- reverse-power pickup accumulated exactly 2.000 s;
- generator trip occurred at step 701 / 7.010 s;
- only reverse power latched;
- underfrequency and synchronism remained unlatched;
- the breaker opened through the canonical generator-trip path.

### Breaker-open coastdown

- frequency fell to 43.154407 Hz;
- maximum absolute slip reached 6.845593 Hz;
- breaker-closed samples: zero;
- all three pickup timers remained 0.000 s;
- no generator trip occurred.

This confirms that pickup delays separate normal transients from persistent hazards and that measured breaker supervision blocks disconnected underfrequency/slip trips.

## Promotion rule

E.3.2 Hotfix 3 is **VALIDATED**. Any later edit to protection thresholds, reset hysteresis, pickup timing, supervision, measured-channel ownership, generator-trip arbitration or HMI projection reopens the applicable E.3.2 gates.
