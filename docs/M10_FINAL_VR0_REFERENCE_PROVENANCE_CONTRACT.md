# M10 Final — VR0 Reference / Provenance Contract Freeze

**Status: CANDIDATE — documentation/reference-contract only.**

**Baseline prerequisite:** M10 Final Plan Amendment 3 / Physical Reference Assessment — locally VALIDATED on 2026-09-14.

VR0 exists to prevent a circular or post-hoc external-assessment campaign. It freezes the reference sources, equations, parameter sets, comparison points, independent reference methods, numerical precision and interpretation rules **before** VR1–VR4 inspect production comparison results.

VR0 changes no production source, exact-v9 runtime, workload, authority policy, generator-load semantics, protection semantics, mission pack or second replacement-long baseline.

## 1. Gate question

> Can VR1–VR4 each be executed against an authoritative, independent and reproducible reference contract without using the production implementation to generate its own expected values?

The only VR0 dispositions are:

- `VR0-REFERENCE-CONTRACT-PASS`;
- `VR0-REFERENCE-GAP`;
- `VR0-NONCIRCULARITY-FAIL`.

Only `VR0-REFERENCE-CONTRACT-PASS` authorizes VR1.

## 2. Cross-cutting non-circularity contract

The following rules apply to all four work packages.

1. Production classes may be **evaluated**, but they may not produce expected/reference values.
2. Reference equations and constants must be traceable to the sources frozen in `research/M10_FINAL_VR0_EXTERNAL_REFERENCE_REGISTER.md`.
3. Reference implementations are test-only C# helpers. No production project may reference them.
4. No Python reference implementation is introduced. Windows orchestration remains PowerShell/CMD; numerical reference code is C#.
5. No new NuGet/runtime dependency is required merely to obtain a reference value.
6. Shared published constants may appear in both production-input construction and the independent reference path, but the algorithms performing the comparison must remain independent.
7. Tolerances and claim bands below are frozen before the first production comparison artifact is read.
8. The first VR1–VR4 assessment run is observational. A mismatch is recorded; coefficients are not tuned inside the same gate.
9. A benchmark of the generic solver equations does not silently validate plant-specific parameterization.
10. A subsystem that is not active in exact-v9 cannot be used to block P3-R1 merely because a future/reference configuration is absent; the gap must instead be classified by applicability.

## 3. VR1 — Point kinetics independent benchmark

### 3.1 Authority and equations

Primary source: Alain Hébert, *Applied Reactor Physics*, 3rd ed. (2020), §5.4.1, especially Eqs. (5.240), (5.251), (5.252), (5.256) and Exercises 5.10–5.11.

The external equation set is:

```text
dn/dt  = ((rho - beta) / Lambda) n + sum(lambda_i C_i)
dCi/dt = (beta_i / Lambda) n - lambda_i C_i
```

Critical-equilibrium initial precursors are frozen as:

```text
Ci(0) = beta_i / (Lambda * lambda_i) * n0
```

The six-group benchmark parameters from Hébert Exercise 5.11 are:

| Group | lambda [1/s] | beta_i |
| ---: | ---: | ---: |
| 1 | 0.0127 | 0.000266 |
| 2 | 0.0317 | 0.001491 |
| 3 | 0.1150 | 0.001316 |
| 4 | 0.3110 | 0.002849 |
| 5 | 1.4000 | 0.000896 |
| 6 | 3.8700 | 0.000182 |

Frozen totals/initial data:

```text
beta = 0.007000
Lambda = 2.0e-5 s
n0 = 1.0
external source = 0
```

One dollar is `rho = beta` for this benchmark. The five canonical VR1 cases are therefore:

| Case | Dollars | rho = delta-k/k |
| --- | ---: | ---: |
| VR1-K0 | 0.00 $ | 0.000000 |
| VR1-KP25 | +0.25 $ | +0.001750 |
| VR1-KP50 | +0.50 $ | +0.003500 |
| VR1-KN50 | -0.50 $ | -0.003500 |
| VR1-KN100 | -1.00 $ | -0.007000 |

Frozen comparison times are:

```text
0, 0.01, 0.10, 0.50, 1, 5, 20, 60 s
```

### 3.2 Independent numerical method

VR1 expected values will be produced by a **test-only adaptive Dormand–Prince 5(4) C# integrator** written independently from `PointKineticsSolver`.

Reference integrator contract:

```text
relative tolerance = 1e-10
absolute tolerance = 1e-12
maximum accepted step = 1e-3 s
minimum step = 1e-10 s
production PointKineticsSolver code may not be called from the reference path
```

Before it is trusted for the six-group cases, the independent reference integrator must reproduce the one-group analytical benchmark in Hébert Exercise 5.10:

```text
beta = 0.0065
Lambda = 1e-4 s
rho = 0.0025
lambda = 0.0766 1/s
normalized n0 = 1
sample times = 0.01, 0.1, 0.5, 1, 10 s
```

The one-group self-qualification ceiling is a maximum relative neutron-population difference of `1e-8` versus the two-mode closed-form solution. Failure is a **reference-harness failure**, not a production-model failure.

### 3.3 Production comparison and classification

The production solver is evaluated at the canonical 10 ms caller step and repeated at 5 ms and 2.5 ms as a numerical-refinement diagnostic. Internal production RK4 substepping remains untouched.

`REFERENCE-CONCORDANT` requires all of:

- zero-reactivity neutron-population drift `<= 1e-10` relative over 60 s;
- maximum neutron-population relative error `<= 0.25%` at nonzero sample times;
- maximum precursor-component relative error `<= 0.50%` where the reference component is materially nonzero;
- deterministic repeat equality;
- no non-finite/negative-population failure.

`BOUNDED-NUMERICAL-DISCREPANCY` is allowed only when:

- neutron-population relative error remains `<= 1.0%`;
- precursor-component relative error remains `<= 2.0%`;
- the 10→5→2.5 ms refinement trend is non-divergent and materially reduces, or already renders negligible, the numerical discrepancy.

Anything larger, non-finite, sign-inconsistent or non-convergent under refinement is `MODEL-DISCREPANCY`. Missing/ambiguous source data is `REFERENCE-GAP`.

### 3.4 Claim boundary

VR1 assesses the **generic point-kinetics equation implementation**. It does **not** physically validate exact-v9's deliberately reduced one-group plant parameterization (`Lambda=0.1 s`, `beta=0.0065`, `lambda=0.08 1/s`). That plant parameterization remains a reduced educational configuration unless separately calibrated.

## 4. VR2 — IAPWS-IF97 water/steam error map

### 4.1 Authority

Primary authority: IAPWS R7-97(2012), *Revised Release on the IAPWS Industrial Formulation 1997 for the Thermodynamic Properties of Water and Steam*.

The production model already uses the IF97 Region-4 saturation-pressure equation but deliberately simplified density, internal-energy and latent-energy correlations. VR2 therefore measures an error map rather than assuming full-IF97 equivalence.

### 4.2 Independent C# reference path

A test-only C# IF97 reference helper will implement only the official equations needed by the frozen matrix:

- Region 4 saturation pressure/temperature;
- Region 1 compressed/subcooled liquid properties;
- Region 2 superheated-vapor properties;
- official release verification-table checks for the implemented regions before production comparison.

It may not call `SimplifiedWaterSteamThermodynamicModel` to produce expected values.

Reference arithmetic uses `double`; the helper must reproduce the applicable official IAPWS verification-table values to a relative difference `<= 1e-8` before it is accepted as a reference path.

### 4.3 Frozen point matrix

Full saturated-property points (`Psat`, `rho_f`, `rho_g`, `u_f`, `u_g`, phase-energy difference) at:

```text
100, 125, 150, 175, 200, 225, 250, 280, 300, 340 degC
```

Additional Region-4 pressure-only stress point:

```text
360 degC
```

Compressed/subcooled liquid inverse-state points `(T, p)`:

```text
(100 degC, 0.5 MPa)
(150 degC, 1.0 MPa)
(200 degC, 2.0 MPa)
(250 degC, 7.0 MPa)
(280 degC, 10.0 MPa)
```

Superheated-vapor inverse-state points `(T, p)`:

```text
(200 degC, 0.2 MPa)
(250 degC, 0.5 MPa)
(300 degC, 1.0 MPa)
(400 degC, 5.0 MPa)
```

For inverse-state cases, reference `(rho,u)` is converted to a 1 kg control-volume input `(V=1/rho, U=u)` and passed to the production inverse closure. VR2 then compares resolved `T`, `p` and phase. This avoids using the production closure to generate its own state.

### 4.4 Frozen reporting

Every row records:

```text
reference value
production value
absolute error
relative error
reference region/phase
production phase
inside production documented envelope
M10-core-envelope flag
```

Summary statistics are `max`, `median` and `p95` absolute-relative error by property/domain.

For the purpose of M10 impact, the **M10-core envelope** is the subset at 200–300 degC plus the compressed/superheated points at or below 300 degC. The lower-temperature and 340/360/400 degC cases characterize the wider documented educational envelope but do not independently block P3-R1.

### 4.5 Frozen claim bands

Per property/domain:

- `QUANTITATIVE-EDUCATIONAL`: maximum relative error `<= 2%`, no phase mismatch;
- `BOUNDED-EDUCATIONAL`: maximum relative error `> 2% and <= 10%`, no phase mismatch;
- `QUALITATIVE-ONLY`: finite/ordered behavior but maximum relative error `> 10% and <= 25%`;
- `MODEL-DISCREPANCY-BLOCKING`: any non-finite/unresolvable reference state inside the documented production envelope, a wrong phase in the M10-core envelope, or a material M10-core property error `> 25%`.

A large error outside the M10-core envelope reduces the documented support claim but is not by itself an M10 P3-R1 blocker.

## 5. VR3 — I-135 / Xe-135 shutdown reference trajectory

### 5.1 Authority and physical case

Primary source: Lamarsh & Baratta, *Introduction to Nuclear Engineering*, 3rd ed., §7.5, Eqs. (7.90)–(7.103), Tables 7.5–7.6 and Fig. 7.14.

Frozen U-235 thermal-fission values:

```text
gamma_I  = 0.0639 atoms/fission
gamma_Xe = 0.00237 atoms/fission
lambda_I = 2.87e-5 1/s
lambda_Xe = 2.09e-5 1/s
sigma_a_Xe = 2.65e6 barn
pre-shutdown thermal flux = 5.0e13 n/cm2/s
```

The corresponding pre-shutdown xenon absorption-removal coefficient used by the normalized comparison is:

```text
sigma_a_Xe * phi = 1.325e-4 1/s
```

The case assumes long operation at constant flux to iodine/xenon equilibrium followed by instantaneous shutdown at `t=0`; after shutdown fission source and neutron absorption are zero.

Because the I/Xe equations are linear in the fission-production source scale, the benchmark uses an arbitrary normalized fission-rate scale `Rf=1`. Absolute concentration is therefore **not** a validated plant claim; trajectory shape, equilibrium ratios and shutdown peak are the assessment targets.

Frozen equilibrium initial conditions:

```text
I0  = gamma_I * Rf / lambda_I
Xe0 = (gamma_I + gamma_Xe) * Rf / (lambda_Xe + sigma_a_Xe * phi)
```

Frozen post-shutdown reference equations:

```text
I(t) = I0 * exp(-lambda_I t)
Xe(t) = Xe0 * exp(-lambda_Xe t)
      + lambda_I * I0 * (exp(-lambda_I t) - exp(-lambda_Xe t))
        / (lambda_Xe - lambda_I)
```

Lamarsh's qualitative cross-check is frozen as: xenon poisoning peaks at approximately 10 h after shutdown for the shown equilibrium U-235 cases; this is a sanity check, not the numerical acceptance criterion. For the specific frozen `5.0e13 n/cm2/s` case, the independent closed-form reference is also required to place the peak inside the pre-result sanity window `9–11 h`; the exact closed-form peak time, not the width of that window, is used for numerical error calculation.

### 5.2 Frozen sample times and independent peak finder

Trajectory samples:

```text
0
15 min
1 h
4 h
8 h
9 h
10 h
12 h
16 h
24 h
36 h
48 h
```

The reference peak time is found from the analytical derivative using bracketed bisection to `<=1 s` time uncertainty. Production peak search uses the canonical solver with 60 s post-shutdown stepping plus 1 s local refinement around the detected peak interval.

### 5.3 Classification

`REFERENCE-CONCORDANT` requires:

- maximum relative I and Xe trajectory error `<= 1e-5` at frozen samples;
- Xe peak-time error `<= 60 s`;
- Xe peak-magnitude relative error `<= 1e-5`;
- deterministic repeat equality.

`BOUNDED-NUMERICAL-DISCREPANCY` permits trajectory/peak-magnitude error `<= 0.1%` and peak-time error `<= 300 s` when the discrepancy is demonstrably timestep/resolution bounded.

Larger disagreement is `MODEL-DISCREPANCY`; missing source/mapping evidence is `REFERENCE-GAP`.

### 5.4 Applicability boundary

The current M9.3 `core-poison-m93-v1` coefficients are explicitly educational/configuration-relative and are **not** the Lamarsh physical constants. VR3 therefore assesses whether the generic `IodineXenonSolver` reproduces the published I/Xe equations when configured with the frozen external case. It does not relabel the built-in M9.3 configuration as plant-calibrated.

The current exact-v9 sustained-generation path does not supply an iodine/xenon definition/state to the desktop factory; xenon is therefore not an active owner of the P1B 5→6 MWe trajectory. A VR3 discrepancy remains important for M9.3/M14 claims but blocks P3-R1 only if later evidence proves the owner is active in the exact-v9 path.

## 6. VR4 — ANS-5.1 decay-heat reference readiness / model assessment

### 6.1 Authority

Current scope authority: ANSI/ANS-5.1-2014 (R2023), *Decay Heat Power in Light Water Reactors*.

Supporting project-held source: Todreas/Kazimi, *Nuclear Systems, Volume I*, §3.9, including discussion of the 2005 ANS decay-power standard and operating-history dependence.

No copyrighted full ANS standard is bundled in the repository. Provenance records official ANS pages/previews and the project-held textbook only.

### 6.2 Critical production-applicability finding frozen by VR0

Static source audit finds:

- `DecayHeatSolver` is a generic configurable equivalent-group solver;
- no `DecayHeatDefinition` is constructed in production `src/` outside the solver/domain library;
- `DesktopSustainedGenerationInitialConditionFactory` reaches `ColdShutdownInitialConditionFactory` without a decay-heat model owner;
- the initial `IntegratedPrimaryCircuitInputs` used by that path sets `TotalDecayHeatPower = Power.Zero`;
- current exact-v9 P1/P1A/P1B therefore does **not** contain a canonical physically calibrated decay-heat trajectory to compare with ANS-5.1.

Consequently VR4 is frozen as a **reference-readiness and production-configuration-gap assessment**, not as permission to invent/fix a multi-exponential fit during the observational pass.

### 6.3 Frozen VR4 outputs

VR4 must report:

1. current ANSI/ANS-5.1 edition/reaffirmation and scope provenance;
2. whether a reproducible numerical reference table available to the project can be lawfully and authoritatively traced;
3. the current production wiring/configuration audit above;
4. whether a canonical production `DecayHeatDefinition` exists to assess;
5. the impact on M10 and on future M12.5 claims.

If no canonical production definition exists, the correct VR4 classification is:

```text
REFERENCE-GAP-NO-CANONICAL-PRODUCTION-CONFIG
```

This classification is **non-blocking for P3-R1/exact-v9** while `TotalDecayHeatPower` remains zero and no decay-heat owner is active in the P1B trajectory. It **does block** any claim that the current decay-heat subsystem is quantitatively ANS-5.1 assessed, and it becomes a prerequisite for M12.5 before post-trip decay-heat fidelity is promoted.

No ANS-derived production coefficients may be fitted or introduced inside VR4. Such work requires a separately authorized model-definition/repair milestone and re-assessment. If a canonical decay-heat definition is later authorized, the previously planned comparison times remain frozen for that future assessment: `1 s, 10 s, 100 s, 1 h, 10 h, 1 day`, with any additional standard-table points added without deleting these anchors.

## 7. VR5 materiality rule frozen at VR0

VR5 evaluates both discrepancy magnitude and whether the assessed owner was active/material in the frozen exact-v9/P1B trajectory.

`PROCEED-P3R1-EXACTV9` remains possible when:

- VR1/VR2 show no material blocking discrepancy in owners active in P1B; and
- any VR3/VR4 gap is demonstrated non-material to exact-v9 while being carried forward as an explicit limitation/future prerequisite.

`BLOCK-P3R1-MODEL-REPAIR` requires a material external-reference discrepancy in an owner active in the frozen P1B path and a defined repair/requalification impact.

`PLAN-STOP-REFERENCE-GAP` is reserved for an unresolved reference/provenance gap that is itself material to interpreting the exact-v9 slow-state evidence.

This materiality rule prevents an inactive future subsystem from blocking M10 while still preventing unsupported physical claims about that subsystem.

## 8. VR0 exit evidence

A passing VR0 artifact must state:

```text
vr0-reference-contract-passes=True
vr1-reference-ready=True
vr2-reference-ready=True
vr3-reference-ready=True
vr4-reference-ready=True
vr4-current-exact-v9-applicability=INACTIVE-NO-CANONICAL-PRODUCTION-CONFIG
production-src-changed=False
pre-existing-tests-changed=False
exact-v9-changed=False
p3r1-authorized=False
next-authorized-gate=VR1-Point-Kinetics-Independent-Benchmark
```

`vr4-reference-ready=True` means its **gap-detection contract** is reproducible; it does not assert that a canonical decay-heat production model exists.

## 9. What VR0 does not authorize

VR0 does not authorize:

- changes to `PointKineticsSolver`;
- changes to water/steam correlations or closure ordering;
- xenon calibration of `core-poison-m93-v1`;
- creation/fitting of a production decay-heat group set;
- P3-R1 execution;
- P3-W;
- exact-v10;
- protection/workload retuning;
- second replacement-long baseline.

Any of those requires the later decision gate defined by Plan Amendment 3.
