# M10.9.4.1-D.2 Validation Checklist

D.2 is audit-only and is cumulative with the still-pending local validation of D.1.

## Build and ordinary regression suite

```text
dotnet clean
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

Expected: zero build errors/warnings under repository policy and no ordinary-suite regressions.

## D.1 admission-phase gate

Verify the D.1 turbine admission tests remain green, especially pure-liquid blocking, wet-steam vapor-fraction transfer and legacy preservation.

## D.2 dedicated authority audit

```text
scripts\run-turbine-admission-authority-audit.cmd
```

Review the emitted baseline / +10 rpm / restored-reference evidence for:

- control-valve position;
- turbine-inlet pressure;
- commanded stage flow;
- effective D.1 stage flow;
- shaft power.

Do not choose a new stage/valve law until these values are captured.

## Long-running gates

```text
scripts\run-gameplay-long-tests.cmd
scripts\run-operational-envelope-audit.cmd
```

The cumulative D.1 + D.2 tree must preserve the existing physical/conservation gates. D.2 itself changes no production physics, so any new physical divergence is a regression.

## Acceptance

D.2 can be marked validated when:

- build and ordinary suite pass;
- D.1 admission-phase tests pass;
- dedicated authority audit passes and its evidence has been reviewed;
- 60/300-second gates remain physically/conservatively acceptable;
- no production resistance, governor gain, actuator travel, protection, replay or timestep value has changed in D.2.

The next correction step is selected from evidence, not predetermined.
