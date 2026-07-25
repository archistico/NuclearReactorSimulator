# M10.9.4.1-D.3.1 Validation Checklist

D.1, D.2 and D.2 Hotfix 1 are user-validated. D.3 audit execution exposed the missing breaker-open rotor deceleration path. D.3.1 is the isolated current-v2 physics candidate that closes that gap.

## Build and ordinary suite

```text
dotnet clean
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

Expected:

- zero compilation errors and warnings under repository policy;
- complete ordinary suite passes;
- legacy sustained rotor definition has no mechanical-loss model;
- both current-v2 sustained profiles own 0.5 MW rated-speed loss;
- rotor mechanical-loss unit and energy-closure tests pass.

## D.2 admission-authority audit

```text
scripts\run-turbine-admission-authority-audit.cmd
```

Expected breaker-open behavior:

- the rotor decelerates instead of remaining fixed near 3301 rpm;
- if overspeed protection latched, speed falls to the canonical reset-safe region, the audit issues `PROTECTION RESET`, and the reset is accepted without clearing any reactor SCRAM;
- the rotor then reaches the controllable ±5 rpm band within 90 simulated seconds;
- control valve is no longer permanently pinned at zero;
- `SPEED RAISE` produces an opening response;
- passive-loss power is finite and positive near rated speed;
- all admission pressure/flow/power evidence remains finite.

## D.3 governor/actuator audit

```text
scripts\run-turbine-governor-actuator-tracking-audit.cmd
```

Review both blocks.

### Breaker open

- baseline is protection-clear and within ±5 rpm of its effective setpoint;
- any preceding turbine/generator overspeed latch has been explicitly and successfully reset;
- +10 rpm changes the effective setpoint exactly;
- controller output and physical valve initially move upward;
- restoring the reference reverses the response;
- passive-loss power remains finite and non-negative;
- no permanent 3301 rpm / valve-closed coast state remains.

### Breaker closed

- 5 → 10 → 5 MWe request and +0.75 rpm droop displacement remain unchanged;
- electromagnetic load and passive mechanical loss remain distinct;
- output confirms whether a separate tracking anti-windup law is actually justified.

## Long-running gates

```text
scripts\run-gameplay-long-tests.cmd
scripts\run-operational-envelope-audit.cmd
```

Pay particular attention to:

- sustained 5 MWe shaft-power margin after the additional 0.5 MW loss;
- turbine control-valve operating position and saturation;
- rotor-speed stability;
- condenser and steam-drum inventories;
- mechanical, thermofluid and complete energy-path closure;
- the previously tracked 300-second wall-clock performance observation.

## Acceptance

D.3.1 becomes validated only after build, ordinary suite, both D.2/D.3 explicit audits and long-running gates pass locally.

After review:

1. add tracking anti-windup only if the corrected evidence shows material integral recovery delay caused by actuator mismatch; or
2. close Phase D and move to Phase E scale/inertia/bidirectional generator coupling.
