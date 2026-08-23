# M10 Final Long Failure Diagnostic 8 — Exact-v7 Grid-Droop Integral-Reference Requalification

**EXECUTION PASS / ENGINEERING NOT QUALIFIED — returned 2026-08-23. Exact-v7 removes the dominant governor integral windup but remains materially non-stationary; exact-v4 remains production; replacement long unauthorized.**

## 1. Diagnostic 7 result

Diagnostic 7 confirms the breaker-closed governor owner directly. At a 5 MWe request the effective droop reference remains 3000.75 rpm while the grid holds the rotor close to 3000 rpm. Over 120–180 s the returned evidence shows approximately:

```text
governor mean error                +0.738 rpm
Ki * mean error                    +0.01476 %/s
governor integral slope            +0.01476 %/s
governor output slope              +0.01474 %/s
physical control-valve slope       +0.01474 %/s
```

The measured integral slope therefore matches `Ki * error` essentially exactly. Steam/header/stop-out inventories decrease while control-out/turbine-inlet inventories increase in the same interval.

This cannot be repaired by choosing a different initial integral value. With the historical contract, any finite initial integral continues to accumulate because the intentional droop offset is treated as integral error while the rigid grid holds actual speed near synchronous speed.

## 2. Versioned control-law repair

Exact-v7 preserves the exact-v6 analytical whole-cycle authored state and component/controller gains. It adds a versioned governor integral-reference mode:

```text
breaker open:
  P/I/D reference = operator speed setpoint               [unchanged]

breaker closed, historical @4/@5/@6:
  P/I/D reference = synchronous speed + load droop offset [unchanged]

breaker closed, exact-v7:
  P/D reference   = synchronous speed + load droop offset
  I reference     = synchronous speed
```

This preserves the intended droop/load characteristic in proportional response while preventing integral action from erasing the droop offset. The generic controller input gains an optional integral setpoint; null preserves historical behavior exactly.

No gain, droop magnitude, valve resistance, turbine work law, pump coefficient, grid stiffness, protection threshold, thermodynamic envelope or hydraulic mode is retuned.

## 3. Exact-version preservation

The new mode defaults to `EffectiveDroopSetpoint`. Existing factories therefore retain historical semantics. Only the new exact-version

```text
integrated-operations-desktop-stable@7
```

opts into `SynchronousSpeedWhenParalleled`.

The authoritative production selector remains exact-v4. Exact-v5 and exact-v6 remain retained failed diagnostic evidence and are not reinterpreted as qualified operating points.

## 4. Qualification workload

Diagnostic 8 runs exact-v7 for 600 simulated seconds at the existing 10 ms fixed step. It records:

- complete whole-cycle mass/energy balance terms;
- all primary and secondary owner-node mass, pressure, temperature and specific-energy trajectories;
- feedwater/hotwell controller state;
- governor setpoint, measurement, error, integral, output and physical control-valve position;
- primary pump/channel/return flows;
- electrical export and full energy-path closure.

Artifacts:

```text
artifacts\m10-final-long-diagnostic8
  00-progress.txt
  90-v7-whole-cycle-requalification-trajectory.csv
  91-v7-node-state-trajectory.csv
  92-v7-final60-node-slopes.csv
  93-v7-whole-cycle-requalification-summary.txt
```

## 5. Decision rule

Exact-v7 is not automatically qualified by test completion. Returned evidence must show that the Diagnostic-7 governor drift is removed and that the whole-cycle state is genuinely bounded: near-zero late governor/control-valve drift, bounded mass/pressure/thermal slopes, stable approximately 5 MWe output, zero trips/rollbacks and conservative energy closure.

No numerical drift threshold is frozen in this candidate. If exact-v7 remains materially non-stationary, owner diagnosis continues instead of widening tolerances or activating production.

## 6. Gate

Run:

```bat
scripts\run-m10-final-long-failure-diagnostic8.cmd
```

Return the complete `artifacts\m10-final-long-diagnostic8` folder before any production activation or replacement-long authorization.


## 7. Returned result

Diagnostic 8 completed build, ordinary tests, LR-M1 regression and the 600 s explicit run successfully. The control-law repair is effective but insufficient for operating-point qualification. Returned evidence includes:

```text
governor output/control-valve late slope   ~ +0.000240 %/s
primary pump flow                          100.000 -> 122.698 kg/s
electrical export                           4.9986 -> 4.5644 MWe
late net external / stored-energy rate     ~ +2.402 MW
turbine-inlet late mass slope              +0.22150 kg/s
trip / rollback                            0 / 0
```

The governor drift is roughly sixty times smaller than Diagnostic 7 and is no longer large enough to explain the continuing whole-cycle motion. Exact-v7 is therefore **NOT QUALIFIED**. Diagnostic 9 continues owner diagnosis at the turbine-admission / condenser mass-transfer boundary without changing runtime semantics.
