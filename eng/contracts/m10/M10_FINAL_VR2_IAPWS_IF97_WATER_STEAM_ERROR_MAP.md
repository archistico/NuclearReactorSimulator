# M10 Final — VR2 IAPWS-IF97 Water/Steam Error Map

**Status:** RETURNED EXECUTION RED — `MODEL-DISCREPANCY-BLOCKING`; superseded for execution by VR2 Replanning / Materiality Diagnostic 1  
**Prerequisite:** VR1 Point-Kinetics Independent Benchmark — VALIDATED from returned local artifact review  
**Authority boundary:** observational external model assessment only; no production repair, exact-v9 change, P3-R1 execution or second replacement-long authorization.

## Purpose

VR2 quantifies the existing `SimplifiedWaterSteamThermodynamicModel` against an independent implementation of the official IAPWS Industrial Formulation 1997 instead of treating the production closure only as an internally consistent educational approximation.

The assessed production path is the current authoritative closure mode:

```text
WaterSteamThermodynamicClosureMode.CorrelationConsistentInverseDomain
```

The reference path is test-only C# and does not call production thermodynamic equations. No third-party IF97 runtime package is introduced and no file under `src/` is changed by this gate.

## Primary authority

The sole numerical authority for the VR2 helper is:

> International Association for the Properties of Water and Steam, **IAPWS R7-97(2012), Revised Release on the IAPWS Industrial Formulation 1997 for the Thermodynamic Properties of Water and Steam**.

VR2 implements only the minimum official equations required by the frozen matrix:

- Region 1 basic Gibbs equation for compressed/subcooled liquid states;
- Region 2 basic Gibbs equation for superheated-vapor states;
- Region 4 saturation-pressure / saturation-temperature relation.

The helper is located at:

```text
tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/IapwsIf97Reference.cs
```

It is deliberately independent from `SimplifiedWaterSteamThermodynamicModel` and is never referenced by production code.

## Fail-closed reference self-check

Before any production comparison is interpreted, the helper must reproduce the official IAPWS verification values from:

- Region 1 Table 5;
- Region 2 Table 15;
- Region 4 Table 35;
- Region 4 Table 36.

The frozen ceiling is:

```text
maximum relative error <= 1e-8
```

The Region 1/2 checks cover specific volume and specific internal energy. The Region 4 checks cover both `p_sat(T)` and `T_sat(p)`.

If this self-check fails, VR2 is classified `REFERENCE-HARNESS-FAIL`; no production error-map result may be used for an engineering decision.

## Frozen assessment matrix

### Saturation — full-property points

```text
100 C
125 C
150 C
175 C
200 C
225 C
250 C
280 C
300 C
340 C
```

At each point VR2 compares:

- saturation pressure;
- saturated-liquid density;
- saturated-vapor density;
- saturated-liquid specific internal energy;
- saturated-vapor specific internal energy;
- vapor-minus-liquid internal-energy difference.

### Saturation — pressure-only extension

```text
360 C
```

This point checks the production Region-4 saturation-pressure behavior without attempting Region-1/Region-2 saturated-property evaluation outside the frozen full-property matrix.

### Compressed/subcooled liquid inverse states

```text
T = 100 C, p = 0.5 MPa
T = 150 C, p = 1.0 MPa
T = 200 C, p = 2.0 MPa
T = 250 C, p = 7.0 MPa
T = 280 C, p = 10.0 MPa
```

### Superheated-vapor inverse states

```text
T = 200 C, p = 0.2 MPa
T = 250 C, p = 0.5 MPa
T = 300 C, p = 1.0 MPa
T = 400 C, p = 5.0 MPa
```

For every inverse point, the reference `(rho,u)` is converted to a one-kilogram control-volume inventory:

```text
mass = 1 kg
volume = 1 / rho
internal energy = u
```

The production `Resolve(...)` path must then recover temperature, pressure and the expected coarse phase without using production-generated expected states.

## M10 core envelope

The blocking M10 core is frozen as:

- saturation points from 200 C through 300 C inclusive;
- compressed-liquid inverse points at 200 C, 250 C and 280 C;
- superheated-vapor inverse points at 200 C, 250 C and 300 C.

The wider points remain model-assessment evidence and determine how broadly the educational claim can be stated, but a large finite error outside the M10 core does not by itself invalidate the current P1B/P3-R1 reasoning.

## Claim bands

These are engineering claim-policy bands frozen before executing VR2; they are not IAPWS constants.

```text
QUANTITATIVE-EDUCATIONAL   maximum relative error <=  2%
BOUNDED-EDUCATIONAL       maximum relative error >   2% and <= 10%
QUALITATIVE-ONLY          maximum relative error >  10% and <= 25%
MODEL-DISCREPANCY-BLOCKING                         > 25%
```

Each property/domain summary records max, median and deterministic nearest-rank p95 errors for all points and separately for M10-core points.

## Blocking conditions

VR2 is RED if any of these occurs:

1. the official IF97 self-check exceeds `1e-8` relative error;
2. a frozen inverse state inside the production documented envelope is unresolved;
3. a numerical comparison inside that envelope is non-finite;
4. a resolved M10-core inverse state has the wrong coarse phase;
5. any finite M10-core numerical comparison exceeds 25% relative error;
6. the exact deterministic repeat differs across two independent production-model instances.

A RED result does **not** authorize an immediate thermodynamic repair or tolerance widening. Return the complete VR2 artifact directory first and perform impact/replanning explicitly.

## Evidence package

VR2 writes under `artifacts/m10-final-physical-reference-vr2`:

1. `01-source-provenance-manifest.txt`;
2. `02-frozen-point-matrix.csv`;
3. `03-reference-selfcheck.csv`;
4. `04-numerical-error-map.csv`;
5. `05-inverse-state-comparison.csv`;
6. `06-property-domain-summary.csv`;
7. `07-vr2-assessment-summary.txt`;
8. `08-impact-known-limitations.txt`;
9. `09-deterministic-repeat.txt`.

## Gate semantics

A nonblocking PASS is reported as:

```text
VR2-PASS-NONBLOCKING
```

and authorizes only:

```text
VR3-I135-Xe135-Shutdown-Reference-Trajectory
```

`REFERENCE-HARNESS-FAIL` or `MODEL-DISCREPANCY-BLOCKING` stops the sequence and requires return of the complete VR2 artifacts before any repair, tolerance change or further physical-reference gate.

VR2 never authorizes P3-R1, P3-W, production repair, exact-v9 changes, workload/protection retuning or a second replacement-long baseline.
