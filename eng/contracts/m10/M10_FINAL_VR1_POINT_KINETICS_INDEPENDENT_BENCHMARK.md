# M10 Final — VR1 Point-Kinetics Independent Benchmark

**Status: VALIDATED — returned artifact review confirmed the independent point-kinetics benchmark.**

**Validated prerequisite:** VR0 Reference / Provenance Contract Freeze.

VR1 is the first execution gate of Plan Amendment 3. It answers a deliberately narrow question:

> Does the generic production `PointKineticsSolver` reproduce the externally specified point-kinetics equations, for the frozen Hébert one-group and six-group benchmark contracts, within the numerical-error bands frozen before comparison results are inspected?

VR1 does **not** calibrate the exact-v9 plant parameters and does not create an RBMK-specific physical-validation claim.

## Authority and non-circularity

Primary numerical source: Alain Hébert, *Applied Reactor Physics*, 3rd ed. (2020), §5.4.1, Eqs. (5.240), (5.251), (5.252), (5.256), Exercises 5.10–5.11 and Table 5.4 as frozen by VR0.

The reference path is a new **test-only C# adaptive Dormand–Prince 5(4) integrator**. It does not call `PointKineticsSolver`; production does not generate expected values; no new runtime/NuGet dependency is introduced; no Python project tooling is introduced.

Before the six-group comparison is trusted, the reference integrator must reproduce the one-group analytical two-mode solution from Exercise 5.10 to a maximum relative neutron-population error `<= 1e-8` at `0.01, 0.1, 0.5, 1, 10 s`. A failure here is `REFERENCE-HARNESS-FAIL`, not a production-model failure.

## Frozen six-group matrix

```text
beta total = 0.007
Lambda     = 2.0e-5 s
n0         = 1
```

| Group | beta_i | lambda_i [1/s] |
| ---: | ---: | ---: |
| 1 | 0.000266 | 0.0127 |
| 2 | 0.001491 | 0.0317 |
| 3 | 0.001316 | 0.1150 |
| 4 | 0.002849 | 0.3110 |
| 5 | 0.000896 | 1.4000 |
| 6 | 0.000182 | 3.8700 |

Frozen reactivity steps:

```text
VR1-K0     0.00 $   rho =  0.000000
VR1-KP25  +0.25 $   rho = +0.001750
VR1-KP50  +0.50 $   rho = +0.003500
VR1-KN50  -0.50 $   rho = -0.003500
VR1-KN100 -1.00 $   rho = -0.007000
```

Frozen comparison times:

```text
0, 0.01, 0.1, 0.5, 1, 5, 20, 60 s
```

## Reference numerical contract

```text
method                 Dormand-Prince 5(4)
relative tolerance     1e-10
absolute tolerance     1e-12
maximum accepted step  1e-3 s
minimum step           1e-10 s
```

This reference code is deliberately separate from production's deterministic RK4/fixed-count internal substepping.

## Production comparison

Production is evaluated without modification at caller steps:

```text
10 ms  — canonical comparison
5 ms   — refinement diagnostic
2.5 ms — refinement diagnostic
```

The production solver's internal substepping is not changed.

`REFERENCE-CONCORDANT` requires at 10 ms:

- zero-reactivity neutron drift `<= 1e-10` over 60 s;
- maximum neutron relative error `<= 0.25%` at nonzero sample times;
- maximum precursor-component relative error `<= 0.50%`;
- bitwise deterministic repeat;
- no non-finite/negative-population failure.

`BOUNDED-NUMERICAL-DISCREPANCY` requires:

- neutron error `<= 1%`;
- precursor error `<= 2%`;
- 10→5→2.5 ms normalized error score non-divergent (`<= 1.05x` per refinement); and
- 2.5 ms normalized error score at least 20% lower than the 10 ms score when the 10 ms result is not already reference-concordant.

Anything outside these bands is `MODEL-DISCREPANCY`. The first run remains observational: no production coefficient or tolerance may be changed inside VR1 after viewing the result.

## Evidence package

VR1 writes under `artifacts/m10-final-physical-reference-vr1`:

1. `01-source-provenance-manifest.txt`;
2. `02-frozen-parameter-inputs.csv`;
3. `03-reference-selfcheck.csv`;
4. `04-reference-trajectory.csv`;
5. `05-production-trajectory.csv`;
6. `06-error-map.csv`;
7. `07-refinement-summary.csv`;
8. `08-deterministic-repeat.txt`;
9. `09-vr1-assessment-summary.txt`;
10. `10-impact-known-limitations.txt`.

## Claim boundary

A VR1 PASS supports the statement:

> The generic point-kinetics equation implementation is quantitatively model-assessed against an independent six-group reference over the frozen step-reactivity matrix.

It does **not** support:

- exact-v9 one-group parameter calibration;
- RBMK-specific kinetics validation;
- spatial/space-time kinetics validation;
- safety-analysis-grade accident prediction.

The exact-v9 sustained-generation configuration remains a deliberately reduced one-group parameterization (`Lambda=0.1 s`, `beta=0.0065`, `lambda=0.08 1/s`) unless separately assessed/calibrated.

## Gate semantics

A `REFERENCE-CONCORDANT` or `BOUNDED-NUMERICAL-DISCREPANCY` result authorizes only **VR2 — IAPWS-IF97 Water/Steam Error Map**.

`MODEL-DISCREPANCY` or `REFERENCE-HARNESS-FAIL` stops the sequence and requires return of the complete VR1 artifacts before any repair or tolerance change.

VR1 does not authorize P3-R1, P3-W, production repair, exact-v9 changes, workload/protection retuning or a second replacement-long baseline.
