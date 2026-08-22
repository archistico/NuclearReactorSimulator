# M10 Final Long Failure Diagnostic 2 / LR-M1 Hotfix 1

## Status

**CANDIDATE — LR-M1 production read-side scalability correction + LR-H1 diagnostic-only owner correlation.**

This package is stacked on **M10 Final Long Failure Diagnostic 1** after the user reported that its build and diagnostic tests passed. Diagnostic 1 is therefore accepted as evidence, not as an M10 promotion gate.

M10 remains open and M11 remains blocked.

## 1. Diagnostic 1 conclusions

### LR-M1 — confirmed Application scalability defect

The synthetic prefix census measured the unchanged full-prefix projectors at 100,000 demand samples as approximately:

- `OperationalChallengeScoreEvidenceProjector.ProjectLive`: **4,823 us/call**, **802,336 B/call**;
- `MissionPerformanceTimelineProjector.Project`: **3,379 us/call**, **2,592 B/call**;
- combined live projection work: approximately **8.2 ms per presentation step** before the rest of the session/runtime cost.

The output remained bounded (`recent_operational_count=1`, `timeline_count=1`) while the input prefix grew. The live path therefore performed O(n) historical work at step n and O(n^2) aggregate work over a long session.

This is now classified as:

```text
LR-M1 = APPLICATION / MISSION LIVE-PROJECTION SCALABILITY DEFECT
```

### LR-H1 — real primary inventory redistribution already visible inside 300 s

The exact-v4 300 s census showed:

- outlet mass: `7504.571944 kg -> 4604.960897 kg`;
- final-60 s outlet mass slope: `-7.9140056 kg/s`;
- final-60 s outlet pressure slope: `-3240.1104 Pa/s`;
- final-60 s outlet specific-volume slope: `+3.3805741e-6 m3/kg/s`;
- total final-60 s node mass slopes sum to numerical zero, so this is redistribution rather than global mass loss.

Using the production primary-circulation resistance `25 Pa*s2/kg2` and the sampled 300 s pressures gives approximately:

```text
channel flow ~= 253.23 kg/s
return flow  ~= 261.07 kg/s
residual     ~= -7.85 kg/s
```

That residual closely matches the measured outlet `dm/dt`. The immediate owner is therefore the primary branch / operating-point continuity balance, not an unexplained thermodynamic exception-site loss.

The upstream reason for the moving operating point is not yet frozen: controller bias, authored initial inventory/pressure/thermal distribution, steam-drum recirculation closure or another coupled reference-point mismatch may contribute.

## 2. LR-M1 Hotfix 1 design

The live MISSION/PERFORMANCE source no longer retains and rescans every deterministic demand sample.

A new internal `MissionPerformanceLiveDemandEvidenceAccumulator` maintains only the information needed by the already-authored semantics:

- current demand sample;
- paired sample count;
- `sum(abs(demand-output error))`;
- `sum(abs(external demand))`;
- at most 100 recent demand **change points**, matching the existing operational timeline retention bound.

The score formula is unchanged. For demand tracking:

```text
mean absolute error = sum(abs(error)) / paired sample count
mean demand         = sum(abs(demand)) / paired sample count
fraction            = 1 - clamp(mean error / mean demand, 0, 1)
```

The replay/offline full-prefix projectors remain unchanged. Only the live read-side adapter uses the incremental aggregate.

Strict logical-order enforcement moves to the incremental `Upsert` boundary, where it is O(1) per incoming sample instead of being rechecked over the complete prefix on every presentation refresh.

Timeline semantics remain unchanged because `MissionPerformanceTimelineProjector` already retains only the latest 100 operational entries. Supplying the latest 100 actual demand change points is exact: any older demand change is necessarily displaced by the 100 newer demand changes before recording/protection/scoring entries are even considered.

No command authority, challenge lifecycle, score policy, scenario identity, archive schema, replay fingerprint, physics or protection semantics change.

## 3. LR-H1 Diagnostic 2

No production plant correction is applied in this package.

The 300 s exact-v4 route is repeated only to capture, once per simulated second:

- `outlet` mass;
- pressure-header / outlet / drum pressures;
- main-circulation pump flow;
- channel flow;
- return flow;
- channel-minus-return continuity residual;
- drum incoming return flow;
- drum recirculated-liquid flow;
- drum separated-steam flow;
- reactor-primary `flow-control` error / integral / output;
- turbine-secondary `level-control` error / integral / output.

The final 60 s report correlates outlet `dm/dt` directly with the canonical channel-return residual and records controller-integral slopes. This decides whether the next production action is primarily:

```text
A/E. reference operating-point / inventory-flow mismatch
B.   closed-loop bias materially driving that mismatch
```

Only after that evidence is returned should an exact-v5 operating-point repair or a more focused physical-owner correction be designed.

## 4. Validation route

Run:

```bat
scripts\run-m10-final-long-failure-diagnostic2.cmd
```

It performs:

1. Debug build with warnings-as-errors;
2. complete ordinary suite;
3. focused LR-M1 incremental semantic-equivalence tests;
4. LR-M1 synthetic incremental scaling/equivalence census;
5. LR-H1 exact-v4 300 s primary branch/controller census.

Return the complete:

```text
artifacts\m10-final-long-diagnostic2
```

Do **not** start the replacement long campaign from this candidate before those artifacts are reviewed.

The historical `eng/m10-final-long-baseline-src.sha256` manifest intentionally remains frozen to the pre-hotfix long baseline. Because LR-M1 Hotfix 1 legitimately changes Application `src/`, the old long-validation route is not the validation route for this candidate and must **not** be rebased yet. A replacement-long contract/source manifest is authorized only after Diagnostic 2 closes LR-H1 and the resulting production candidate passes its focused/ordinary gates.

## 5. Hard non-scope

This candidate does not:

- widen the water/steam envelope;
- clamp `outlet` state;
- modify the 19 I.3 budgets;
- modify conservation ceilings;
- change hydraulic resistance, pump head, heat-transfer coefficients or controller tuning;
- reinterpret exact-v4;
- rebind an existing exact mission/scenario identity;
- weaken protection or numerical fail-closed behavior.

If LR-H1 ultimately requires a different authored production operating point, it must be introduced as a new exact version rather than editing exact-v4 in place.
