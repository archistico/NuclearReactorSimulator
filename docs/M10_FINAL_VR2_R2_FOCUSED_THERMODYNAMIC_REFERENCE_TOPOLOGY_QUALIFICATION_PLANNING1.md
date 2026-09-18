# M10 Final — VR2 — R2 Focused Thermodynamic / Reference / Topology Qualification — Planning 1

## Status

**PLANNING-ONLY CANDIDATE**

Prerequisite: returned R1 implementation evidence is adjudicated PASS and frozen. This document authorizes no R2 execution by itself.

## 1. Purpose

R1 proved that the selected C4 semantics were staged faithfully in production as explicit opt-in mode `ReferenceConsistentTabulatedInverseDomain = 2`. R2 now plans the first **independent physical requalification of that production path**. R2 is not another C4 equivalence test: the selected C4 shadow implementation is not the acceptance oracle. The acceptance oracle is the existing independent IAPWS-IF97 reference helper plus the already frozen RP1A reference/topology corpora.

R2 answers one bounded question:

> When the new production mode 2 is explicitly selected, does it remove the previously blocking thermodynamic/reference and phase-topology defects over the frozen VR2, exact-v9-state and seam domains without changing any production/default/exact-v9 behavior?

## 2. Authority boundary

Planning 1 and the future R2 gate are **test/reference only**. They may not:

- modify `src/`;
- modify or regenerate `NRSVR2C4.v1.bin`;
- change the mode-2 resolver, precedence or payload schema;
- switch the default closure mode;
- modify exact-v9 or compose mode 2 into exact-v9 factories/scenarios;
- create a new exact identity;
- change VR2 claim bands, the Planning 1 10% pressure target or seam coordinates;
- run the R3 exact-v9-equivalent composition gate;
- run the R4 long materiality recheck;
- authorize VR3, P3-R1 or a second replacement-long baseline.

The future R2 test explicitly constructs `SimplifiedWaterSteamThermodynamicModel(WaterSteamThermodynamicClosureMode.ReferenceConsistentTabulatedInverseDomain)` and leaves all default/runtime call sites untouched.

## 3. Independent reference provenance

The reference remains `IapwsIf97Reference.cs`, implementing the bounded IAPWS R7-97(2012) Region 1, Region 2 and Region 4 equations already used by VR2. The helper must remain independent from production and must first pass the existing official-table self-check with maximum relative error `<= 1e-8`.

R2 does **not** accept the embedded production payload as its own reference oracle. Payload/C4 evidence is prerequisite provenance only; expected physical results are reconstructed from the independent helper and frozen RP1A inputs.

## 4. Frozen qualification topology

R2 consumes the immutable RP1A corpora:

- 40 VR2 reference rows: 20 saturation endpoints, 10 saturation-mixture rows, 5 compressed-liquid rows, 4 superheated-vapor rows and one Region-4 pressure-only boundary row;
- 360 exact-v9 node-state rows: 72 observations each for `suction`, `pressure`, `outlet`, `drum` and `feedwater-inventory`;
- 1,280 seam probes: 320 boundaries × exactly four sides (`R1-SIDE`, `R4-LIQUID-SIDE`, `R4-VAPOR-SIDE`, `R2-SIDE`).

The 39 VR2 rows carrying `(v,u)` are re-resolved through production mode 2. The one `VR2-SAT-360C-PONLY` row remains a direct Region-4 saturation-pressure reference check and is not fabricated into an inverse state.

## 5. Frozen acceptance criteria

### 5.1 Reference harness

- official IAPWS self-check maximum relative error `<= 1e-8`;
- deterministic reference outputs;
- no production call from the reference helper.

### 5.2 VR2 reference matrix

For the 39 inverse-applicable rows:

- all 39 resolve;
- phase mismatch count = 0;
- no non-finite temperature or pressure;
- M10-core numerical comparison remains below the existing VR2 blocking ceiling `25%`;
- compressed-liquid / hot-primary pressure remains within the already approved Planning 1 target `10%`;
- deterministic repeat is exact.

The boundary-only 360 C Region-4 saturation pressure must remain finite and within the original VR2 nonblocking policy.

### 5.3 Exact-v9 frozen state topology

R2 does not run the scenario. It re-resolves the frozen 360 committed `(v,u)` node states through explicit mode 2 and compares them with the independent IF97 classifications already frozen in RP1A:

- 360/360 resolved;
- phase mismatch count = 0;
- phase agreement = 100%;
- no M10-core numerical comparison above 25%;
- hot-primary/compressed-liquid pressure maximum relative error `<= 10%`;
- deterministic repeat is exact.

This is state-topology qualification only. Exact-v9 composition/execution belongs to R3.

### 5.4 Seam ownership and continuity

For all 320 boundaries:

- exactly four frozen probe sides exist;
- all 1,280 probes resolve;
- phase mismatch count = 0;
- `R1-SIDE` is liquid-side ownership;
- `R4-LIQUID-SIDE` and `R4-VAPOR-SIDE` remain Region-4 mixture ownership;
- `R2-SIDE` is superheated-vapor ownership;
- deterministic repeat is exact;
- no unowned seam interval is observed.

The continuity ceilings are inherited non-regression limits from the already qualified C3/C4 topology evidence; they are **not new thermodynamic tolerances**:

```text
max liquid-side pressure jump <= 0.0005137228525280754 MPa
max liquid-side temperature jump <= 3.5596193356468575E-05 C
max vapor-side pressure jump <= 0.00026625516826150886 MPa
max vapor-side temperature jump <= 0.001903154074568647 C
```

Any larger observed jump is blocking and requires returned evidence/replanning; the limit may not be widened inside R2 execution.

## 6. Future R2 gate

The future executable gate is:

```text
R2-FOCUSED-THERMODYNAMIC-REFERENCE-TOPOLOGY-QUALIFICATION1
```

It adds exactly one new focused Simulation test source under `tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/` and no production source. It runs the ordinary Release suite with the R2 opt-in unset, then one explicit focused R2 test process with the mode 2 candidate selected only inside that test.

The R2 execution evidence tree is frozen to exactly nine files:

```text
01-contract-and-provenance.txt
02-reference-selfcheck.csv
03-vr2-reference-point-qualification.csv
04-exact-v9-topology-qualification.csv
05-seam-topology-continuity.csv
06-topology-qualification-summary.txt
07-deterministic-repeat.txt
08-r2-qualification-summary.txt
09-prequalification-review.txt
```

A successful local run still requires returned-evidence adjudication before R3 planning.

## 7. Blocking classification

Any of the following returns R2 blocking:

- reference self-check > `1e-8`;
- unresolved in-envelope VR2/exact-v9/seam state;
- wrong reference phase;
- non-finite result;
- M10-core numerical error > `25%`;
- compressed-liquid/hot-primary pressure error > `10%`;
- seam continuity ceiling exceeded;
- missing/duplicate seam side or boundary;
- deterministic-repeat mismatch;
- ordinary Release failure;
- any production/default/exact-v9 mutation.

The success classification is `PASS-R2-FOCUSED-THERMODYNAMIC-REFERENCE-TOPOLOGY-QUALIFIED`. A PASS authorizes only returned-evidence review; after that review, the successor may be planning-only `R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFICATION-PLANNING1`.
