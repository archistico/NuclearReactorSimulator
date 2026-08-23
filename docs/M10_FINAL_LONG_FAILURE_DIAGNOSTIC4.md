# M10 Final Long Failure Diagnostic 4 — Exact-v5 Full-Plant Mass / Energy Balance Census

## Status

**CANDIDATE — Diagnostic 3 Hotfix 1 completed locally, but exact-v5 is NOT QUALIFIED for production activation.**

This package is stacked directly on the user-validated **M10 Final Long Failure Diagnostic 3 Hotfix 1** package. It does not alter exact-v5, exact-v4, production selection, physics, controller tuning or validation tolerances. It adds only a deeper evidence census over the unchanged exact-v5 600 s trajectory.

M10 remains open. M11 remains blocked. The replacement long campaign remains **not authorized**.

## 1. Diagnostic 3 execution result versus engineering decision

The Diagnostic-3 script completed successfully: build, complete ordinary suite, LR-M1 Hotfix-1 regression and the explicit exact-v5 600 s run all passed their executable gates. That means exact-v5 is deterministic, finite, trip-free and rollback-free across the historical exact-v4 LR-H1 failure interval.

That executable PASS is **not** the same as operating-point qualification. The returned artifacts show a large monotonic redistribution before the hydraulic branch residual becomes small.

Initial exact-v5 state:

```text
outlet mass       4609.759 kg
drum mass         3918.241 kg
drum level        0.500027
pump flow          259.843 kg/s
channel flow       260.093 kg/s
return flow        259.926 kg/s
```

At 600 s:

```text
outlet mass       1425.894 kg
drum mass         7267.712 kg
drum level        0.956714
pump flow          103.196 kg/s
channel flow       103.023 kg/s
return flow        103.205 kg/s
```

The 260 kg/s pressure-grade probe therefore did what Diagnostic 3 was designed to test: it demonstrated that **instantaneous hydraulic equation matching is not sufficient to author a full-plant equilibrium seed**.

## 2. What did improve

The primary branch continuity residual becomes small after the long transient. Over the final 60 s of Diagnostic 3:

```text
mean channel-return residual = -0.17836 kg/s
outlet mass slope             = -0.17960 kg/s
pump flow mean                ~= 103.31 kg/s
channel flow mean             ~= 103.14 kg/s
return flow mean              ~= 103.32 kg/s
```

This is a major improvement over exact-v4, where the final-60 s outlet loss and channel-return residual were both about `-7.914 kg/s` at 300 s.

Diagnostic 3 therefore confirms the earlier ownership finding: branch continuity directly explains outlet inventory drift. But exact-v5 reaches near-continuity only after moving to a very different full-plant state.

## 3. Why exact-v5 is not qualified

The final 60 s remain materially non-stationary:

```text
drum mass slope       +0.79872 kg/s
drum level slope      +8.6082e-5 fraction/s
outlet pressure slope -1001 Pa/s
drum pressure slope    -984 Pa/s
fuel temperature slope -0.01063 °C/s
structure slope        -0.01154 °C/s
```

The final drum level is already `0.956714`. A positive level slope of this order is not a bounded half-full operating-point condition. In parallel, suction / pressure-header / outlet / drum pressures all continue a common-mode decline of roughly 1 kPa/s while the solid thermal bodies continue cooling.

Therefore the next missing evidence is no longer another guessed primary flow target. We need to decompose the remaining state drift into the canonical full-plant balance owners:

1. **drum mass balance** — incoming return + feedwater - separated steam - liquid recirculation;
2. **primary boundary mass balance** — feedwater versus steam export;
3. **secondary-cycle closure** — condensation, condensate-pump and feedwater-pump flows;
4. **coupled energy balance** — nuclear heat, condenser rejection, turbine/electrical export, pump work and stored-energy change.

## 4. Diagnostic 4 scope

Diagnostic 4 reuses **exact-v5 unchanged** for the same 600 s horizon and records once per simulated second:

### Primary/drum mass terms

- outlet mass;
- drum mass and level;
- pump / channel / return flows;
- channel-return residual;
- drum incoming return flow;
- separated steam flow;
- requested and actual liquid recirculation;
- recirculation inventory-limit flag;
- feedwater boundary flow;
- steam-export boundary flow;
- derived feedwater-minus-steam-export residual;
- derived drum algebraic net mass rate;
- primary total mass and primary audit mass rates.

### Secondary mass terms

- feedwater pump flow;
- condensate pump flow;
- condenser condensation flow;
- full-thermofluid expected and accumulated external mass rates.

### Energy/power terms

- fission, decay and total nuclear heat power;
- primary boundary net external power;
- pump hydraulic power;
- feedwater conditioning power;
- condenser heat rejection;
- turbine shaft power;
- electrical export;
- generator conversion loss;
- passive rotor mechanical loss;
- `NetReactorToGridExternalPower`;
- per-step coupled stored-energy change converted to MW;
- full energy-path closure residual;
- generator requested and actual output.

### Thermal/common-mode state

- outlet / drum pressure;
- outlet / fuel / structure temperature;
- level-controller output.

## 5. Diagnostic 4 artifacts

Run:

```bat
scripts\run-m10-final-long-failure-diagnostic4.cmd
```

The focused route performs:

1. Debug build with warnings-as-errors;
2. complete ordinary suite;
3. LR-M1 Hotfix-1 semantic-equivalence regression;
4. exact-v5 600 s full-plant balance census.

Return the complete:

```text
artifacts\m10-final-long-diagnostic4
```

Expected files:

```text
00-progress.txt
50-v5-full-plant-balance-trajectory.csv
51-v5-final60-balance-summary.txt
```

## 6. Decision rule after Diagnostic 4

No new acceptance threshold is frozen by this diagnostic.

The returned evidence is used to identify the next authored degree of freedom:

- if `feedwater - steam export` accounts for drum accumulation, the next candidate must balance the secondary mass boundary / level-control bias rather than alter primary hydraulics;
- if drum algebraic mass rate is dominated by return/recirculation mismatch, the steam-drum recirculation/reference state remains the owner;
- if coupled stored energy is materially negative while conservation residuals remain small, the next candidate must solve the thermal/full-plant operating point rather than merely match hydraulic pressure drops;
- only after both mass and energy owners are quantitatively identified may a new exact seed be authored.

Exact-v5 must **not** be promoted merely because it survived 600 s without exception.

## 7. Hard non-scope

Diagnostic 4 does not:

- edit exact-v4 or exact-v5 runtime semantics;
- add exact-v6;
- switch the production selector;
- change primary flow target, resistance or pump head;
- change drum level setpoint, feedwater-pump bias or controller gains;
- change fission power, thermal conductances or heat capacities;
- widen the thermodynamic state envelope;
- change I.3 budgets or conservation ceilings;
- authorize the replacement long campaign;
- close M10 or unblock M11.

The historical first-long manifest remains frozen. Diagnostic 4 is evidence collection only.
