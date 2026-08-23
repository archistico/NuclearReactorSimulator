# M10 Final Long Failure Diagnostic 7 — Governor-Droop / Steam-Path Owner Census

**CANDIDATE — Diagnostic 6 execution PASS, exact-v6 engineering NOT QUALIFIED; exact-v4 remains production; no exact-v7 exists; replacement long unauthorized.**

## 1. Returned Diagnostic 6 evidence

Diagnostic 6 completed successfully for 600 simulated seconds with zero trips and zero corrected-commit rollbacks. The full energy path remains conservative, but exact-v6 does not satisfy the engineering qualification rule.

Late evidence remains materially non-stationary:

```text
primary pressure slope          about -0.63 kPa/s
outlet mass slope               about -0.105 kg/s
drum mass slope                 about +0.378 kg/s
steam/header/stop-out mass      still decreasing
control-out/turbine-inlet mass  still increasing
mean net external power         about -1.157 MW
mean stored-energy change       about -1.157 MW
electrical export               4.9986 -> 5.2015 MWe
```

Therefore exact-v6 is retained as diagnostic evidence only and must not be activated.

## 2. New owner hypothesis from code + returned evidence

The exact-v6 analytical steam-path pressure grade was solved for 13.0280018984 kg/s with the control valve at about 27.3123% and rotor speed authored at 3000 rpm.

The unchanged governor droop contract, however, changes the automatic speed-controller setpoint while the generator breaker is closed:

```text
nominal synchronous speed               3000 rpm
full-load droop reference rise             1.5 rpm
requested load fraction at 5/10 MWe        0.5
------------------------------------------------
effective governor setpoint             3000.75 rpm
```

Thus exact-v6 is not actually bumpless at the governor owner: it starts with a positive speed error of approximately 0.75 rpm even though the hydraulic steam-path state itself is analytically closed at t=0.

A PI/PID speed controller with non-zero integral gain can then move the control valve away from the authored 27.3123% operating point. The Diagnostic-6 inventory signature — upstream steam/header/stop-out depletion with downstream control-out/turbine-inlet accumulation — is consistent with such a change in admission resistance, but the existing artifacts did not record governor diagnostics or individual valve/stage flows.

This is a hypothesis to freeze, not yet a justification for changing the seed.

## 3. Diagnostic 7 scope

Diagnostic 7 reruns exact-v6 unchanged for only 180 simulated seconds and records every 0.1 s:

- effective governor setpoint, measurement and error;
- P/I/D terms and governor output;
- physical control-valve position;
- rotor rpm;
- generator frequency, phase difference, mechanical input and electrical output;
- main-steam-line flow;
- stop/control/admission valve flows;
- turbine-stage commanded and effective flow;
- turbine shaft power;
- separated steam flow;
- mass and pressure for `steam`, `header`, `stop-out`, `control-out`, `turbine-inlet`.

The shorter workload is deliberate: the suspected owner begins at the first automatic-controller evaluation and is already visible well before 60 s. Another 600 s soak would add cost without improving causal separation.

## 4. Decision rule

If returned evidence shows all of the following:

1. effective governor setpoint is 3000.75 rpm while the authored initial rotor/measurement is near 3000 rpm;
2. governor error/integral drives output and physical control-valve position away from 27.3123%;
3. the resulting stop/control/admission/stage flow mismatch corresponds in sign and timing to the observed upstream/downstream steam-path inventory transfer;

then the residual exact-v6 drift is classified as a **coupled governor/generator operating-point seed mismatch**. The next candidate may then author a distinct exact-v7 that includes the governor/generator state required for bumpless loaded operation, while preserving the already-validated controller gains and droop law.

If those conditions do not hold, no governor retuning is authorized and owner diagnosis continues.

## 5. Gate

Run:

```bat
scripts\run-m10-final-long-failure-diagnostic7.cmd
```

The script performs:

1. Debug build with warnings-as-errors;
2. complete ordinary suite;
3. LR-M1 Hotfix 1 semantic-equivalence regression;
4. explicit 180 s exact-v6 governor/droop + steam-path owner census.

Artifacts:

```text
artifacts\m10-final-long-diagnostic7
  00-progress.txt
  80-v6-governor-steam-path-trajectory.csv
  81-v6-governor-steam-path-summary.txt
```

## 6. Hard non-scope

Diagnostic 7 changes no production source file, initial-condition identity, controller gain, droop law, valve resistance, turbine work law, generator coupling, hydraulic coefficient, thermodynamic envelope, I.3 budget or conservation ceiling. It does not create exact-v7, switch the exact-v4 production selector, authorize the replacement long, close M10 or unblock M11.
