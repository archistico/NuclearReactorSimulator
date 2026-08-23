# M10 Final Long Failure Diagnostic 9 — Exact-v7 Turbine-Admission / Closed-Cycle Mass-Owner Census

**CANDIDATE — Diagnostic 8 execution PASS / exact-v7 NOT QUALIFIED; evidence-only; exact-v4 remains production; no exact-v8, production activation or replacement-long authorization.**

## 1. Why Diagnostic 8 does not qualify exact-v7

Diagnostic 8 proves that the versioned synchronous integral reference fixes the dominant breaker-closed governor windup. Late governor/control-valve drift falls from about `+0.01474 %/s` in Diagnostic 7 to about `+0.000240 %/s`.

The whole-cycle state nevertheless remains materially non-stationary at 600 s:

```text
primary pump flow             100.000 -> 122.698 kg/s
electrical export               4.9986 -> 4.5644 MWe
late stored-energy rate        ~+2.402 MW
turbine-inlet dm/dt            +0.22150 kg/s
control-out dm/dt              +0.21362 kg/s
trip / rollback                0 / 0
```

The energy-path closure remains conservative, so widening numerical tolerances is not justified. Production activation remains blocked.

## 2. New owner hypothesis

The current turbine stage uses `VaporMassFractionLimited` admission. In the canonical solver:

```text
stage effective mass flow = stage commanded mass flow * inlet vapor fraction
```

Only `stage effective mass flow` is removed from `turbine-inlet` and added to `exhaust`. Therefore the inlet control-volume mass balance can contain two distinct residuals:

```text
admission valve - stage commanded     hydraulic / stage-capacity residual
stage commanded - stage effective     vapor-fraction residual
---------------------------------------------------------------
admission valve - stage effective     total turbine-inlet mass residual
```

Diagnostic 7 already showed at the exact-v6 initial point `13.0277 kg/s` commanded but only `12.7107 kg/s` effective. Diagnostic 8 ends with turbine-inlet vapor quality about `0.9820` and measured late inlet mass accumulation `+0.2215 kg/s`. The implied `q*(1-x)` throughput is about `12.31 kg/s`, consistent with the observed secondary-cycle flow scale. This is strong evidence but must be frozen directly under exact-v7 before any semantic repair.

## 3. Diagnostic 9 workload

Diagnostic 9 changes no runtime source file and reuses exact-v7 unchanged for 180 simulated seconds at the existing 10 ms step. It samples every 0.1 s:

- governor integral/output and physical control-valve position;
- admission-valve, stage-commanded and stage-effective mass flows;
- turbine-inlet vapor fraction and mass;
- `admission-commanded`, `commanded-effective`, `commanded*(1-x)` and `admission-effective`;
- condenser actual/thermal-limited condensation and exhaust mass;
- condensate-pump flow and hotwell mass;
- feedwater-pump flow and feedwater-inventory mass;
- corrected M4.4 drum mass balance and measured drum mass;
- turbine shaft power, condenser heat rejection and electrical export.

Artifacts:

```text
artifacts\m10-final-long-diagnostic9
  00-progress.txt
  100-v7-turbine-admission-mass-owner-trajectory.csv
  101-v7-turbine-admission-mass-owner-summary.txt
```

## 4. Decision rule

If the returned late-window evidence shows:

```text
measured turbine-inlet dm/dt ~= admission - stage effective
stage commanded - stage effective ~= stage commanded * (1 - vapor fraction)
```

and the downstream exhaust/hotwell/feedwater/drum algebraic balances independently close their measured inventory slopes, the vapor-fraction-limited turbine-stage mass ownership is classified as a structural contributor to the exact-v7 drift.

That result does **not** authorize a seed-only exact-v8. A separate design decision must then choose between:

1. transporting total admitted mass through the turbine while limiting shaft work by vapor fraction;
2. adding an explicit moisture-separation/drain owner for rejected liquid; or
3. using another already-modeled physical path if evidence shows one exists.

If the closures do not match, owner diagnosis continues without changing turbine semantics. No drift tolerance is widened.

## 5. Gate

Run:

```bat
scripts\run-m10-final-long-failure-diagnostic9.cmd
```

Return the complete `artifacts\m10-final-long-diagnostic9` folder before exact-v8, turbine-admission semantic changes, production activation or replacement-long authorization.
