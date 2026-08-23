# M10 Final Long Failure Diagnostic 11 — Exact-v9 Post-Moisture Analytical Whole-Cycle Equilibrium

**VALIDATED — Diagnostic 11 Hotfix 2 build/ordinary suite/600 s exact-v9 requalification PASS; returned artifacts qualify exact-v9; production activation remains separate.**

## Returned qualification result

User-returned Diagnostic 11 Hotfix 2 artifacts complete 600 s / 60,000 steps with final electrical export `4.999999982116509 MWe`, primary pump/channel/return `100.000000974 / 100.000001357 / 100.000000320 kg/s`, drum level `0.4999999996725085`, final-60 drum mass slope `-1.06214247e-8 kg/s`, governor/control-valve slope about `-3.89e-10 %/s`, late net/stored energy about `9.78e-8 MW`, mean absolute full-energy closure `1.12160695e-5 J`, zero trip steps and zero rollbacks. Exact-v9 is therefore **QUALIFIED** as the post-moisture whole-cycle operating point.


### Hotfix 2 post-preconditioning regression correction

Hotfix 1 correctly replaced the stale historical integral range, but one assertion still treated the governor proportional contribution as the ideal pre-step value `0.75 %` with a `+/-1e-9` band. The exact-v9 factory deliberately performs 20 ms of deterministic seed preconditioning before the test reads `LatestCanonicalSnapshot`. By then the rotor measurement has changed by only a few nanorpm, but that is enough for the production expression `Setpoint - Measurement` to yield the observed `0.75000000640329745 %`.

Hotfix 2 therefore freezes the controller semantics rather than a mathematically ideal value that no longer corresponds to the sampled instant:

```text
Error = Setpoint - Measurement
P = Kp * Error, with unchanged Kp = 1
Output = UnsaturatedOutput (candidate must not be saturated)
I = UnsaturatedOutput - P - D
```

The authored exact-v9 governor/control root is still checked independently at `29.281329697436618 %` to 6 decimal places. This is not a tolerance relaxation in production physics; it is a correction of the test's temporal contract. No runtime file or authored operating-point value changes.

### Hotfix 1 regression correction

The original Diagnostic 11 regression inherited the historical `25..28 %` governor integral range from exact-v7/v8. Exact-v9 intentionally raises the authored governor/control output to `29.2813296974 %`. With the unchanged breaker-closed droop proportional contribution of `0.75 %`, the bumpless PI preload is therefore:

```text
29.2813296974 - 0.7500000000 = 28.5313296974 %
```

The runtime produced exactly that value. Hotfix 1 replaces the stale range with a decomposition contract that checks the expected P/output/I region and `I = unsaturated output - P - D` to 9 decimal places. No runtime or authored operating-point value changes.

## 1. Diagnostic 10 result

Diagnostic 10 Hotfix 1 completed build, ordinary suite, LR-M1 regression and the 600 s exact-v8 requalification. The structural repairs are successful:

- explicit moisture-drain ownership removes the former turbine-inlet accumulation;
- late turbine-inlet mass slope is only about `+8.4e-5 kg/s` instead of the previous `+0.22..0.27 kg/s` class;
- governor/control-valve late drift is about `-3.8e-6 %/s`;
- primary circulation remains near `99.98 kg/s`;
- global mass slope closes to numerical zero and stage/full-cycle energy ownership remain conservative.

Exact-v8 is nevertheless not the final operating point. Its late electrical export is about `4.8682 MWe` and its late net external/stored-energy rate is about `+0.2553 MW`. This is expected because exact-v8 intentionally preserved the pre-drain exact-v7 authored state while changing turbine-admission ownership.

## 2. Why exact-v8 is off-root

The pre-drain analytical seed treated `13.0280018984 kg/s` as the complete turbine throughput. Under the validated moisture-drain policy, only vapor reaches the work-producing stage. Therefore `13.0280018984 kg/s` must instead be the **vapor** flow required to provide:

```text
5 MWe / 0.98 generator efficiency + 0.5 MW rotor loss
= 5.602040816 MW turbine shaft power

5.602040816 MW / 430 kJ/kg
= 13.028001898433793 kg/s vapor
```

The saturated-vapor drum-source enthalpy, turbine expansion resistance and condenser UA are then solved simultaneously. The resulting post-drain root is:

```text
total admission       13.339237135405003 kg/s
work-producing vapor  13.028001898433793 kg/s
moisture drain         0.311235236971211 kg/s
control valve          29.2813296974 %
```

The steam-path pressure/quality grade is recomputed from unchanged resistances and unchanged enthalpy transport. The condenser root is:

```text
exhaust temperature  42.5253661313 °C
exhaust pressure       8.438344971 kPa
condenser rejection   27.5935735108 MW
```

## 3. Liquid-loop root

The explicit drain does not pass through the condenser. The hotwell therefore receives two conservative streams:

```text
13.0280018984 kg/s saturated condensate at exhaust pressure
0.3112352370 kg/s saturated-liquid moisture drain at turbine-inlet pressure
```

The mass-weighted enthalpy root gives:

```text
hotwell temperature   47.3356594370 °C
```

Keeping the already-authored feedwater-inventory compression root and solving the unchanged pump equations gives:

```text
condensate pump       42.9665153700 %
feedwater temperature 47.3784886658 °C
feedwater pump        96.9308268016 %
```

These are equation-derived values; the 600 s exact-v8 terminal snapshot is not copied as the new seed.

## 4. Energy root

At the post-drain mass root the unchanged hydraulic pumps exchange about `0.2244378206 MW` with the fluid. Therefore the external first-law root is:

```text
27.5935735108 MW condenser rejection
+5.0000000000 MW electrical export
+0.1020408163 MW generator conversion loss
+0.5000000000 MW passive rotor loss
-0.2244378206 MW hydraulic pump input
=
32.9711765066 MW fission power
```

The primary remains at the already-derived `100 kg/s` root. The new fission value changes only the authored primary outlet quality and solid temperatures needed to keep the same conservative heat-transfer ownership:

```text
outlet quality       0.2151419126
fuel temperature   305.6251490647 °C
structure           289.1395608114 °C
```

No pump head, hydraulic resistance, valve law, turbine efficiency, condenser UA, controller gain, thermodynamic envelope or conservation tolerance is changed.

## 5. Exact-version rule

Diagnostic 11 adds only:

```text
integrated-operations-desktop-stable@9
```

Exact-v8 remains frozen Diagnostic-10 evidence. Exact-v4 remains the authoritative production selector. Exact-v9 preserves the exact-v8 governor integral-reference and moisture-drain semantics; only the authored operating point changes.

## 6. Gate

Run:

```bat
scripts\run-m10-final-long-failure-diagnostic11.cmd
```

The gate performs build with warnings-as-errors, complete ordinary suite, LR-M1 Hotfix-1 semantic-equivalence regression and a 600 s exact-v9 whole-cycle requalification.

Return:

```text
artifacts\m10-final-long-diagnostic11
  00-progress.txt
  120-v9-whole-cycle-equilibrium-trajectory.csv
  121-v9-node-state-trajectory.csv
  122-v9-final60-node-slopes.csv
  123-v9-whole-cycle-equilibrium-summary.txt
```

## 7. Decision rule

Exact-v9 advances only if the returned evidence shows bounded primary/secondary inventories, negligible governor and turbine-inlet drift, stable operation near 5 MWe, late net-external and stored-energy rates approaching zero together, zero trip/rollback events, and conservative stage/full-cycle energy closure. No new drift tolerance is frozen in advance.

If those conditions pass, the next step is a **separate production-activation and cumulative requalification candidate**. The replacement long is still not authorized by Diagnostic 11 itself.
