# M10 Final — VR2 Engineering Repair Planning 1 — RP1B Refinement 5 — Returned-Evidence Adjudication

**Status:** VALIDATED RETURNED EVIDENCE — engineering review complete.  
**Returned machine classification:** `C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED`.  
**Authority:** this review closes only Refinement 5 returned-evidence adjudication. C4 planning is now justified by the frozen rule; C4 implementation, RP1C selection, production thermodynamic repair, tolerance changes, exact-v9 changes, VR3, P3-R1 and second replacement-long execution remain unauthorized.

## 1. Reviewed evidence

The complete returned folder is frozen in the repository handoff under:

```text
eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement5_Artifacts/
```

The returned evidence contains five independent .NET test-process runs over immutable C3 and the frozen 320-boundary R1 seam corpus. Each process records 64 measured passes / 20,480 measured calls after the authored warm-up and uses the unchanged strict single-call ceiling `409.30666666666673 us`.

Aggregate returned facts:

```text
independent process runs = 5
measured calls per process = 20,480
total measured calls = 102,400
processes with at least one exceedance = 5
total calls above strict ceiling = 8
same-boundary confirmed count = 1
cross-process maximum = 3598.6 us
confirmed boundary = 3 at 47.26746165007364 C
```

The machine adjudicator therefore correctly returns:

```text
C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED
c4-planning-justified=True
c4-implementation-authorized=False
rp1c-selection-authorized=False
```

## 2. Cross-process recurrence

Boundary 3 exceeds the strict ceiling exactly once in every process:

| Process | Pass | Boundary | Max | GC activity |
| --- | ---: | ---: | ---: | --- |
| 1 | 58 | 3 | 2963.9 us | Gen0 |
| 2 | 58 | 3 | 2598.6 us | Gen0 |
| 3 | 58 | 3 | 2666.1 us | Gen0 |
| 4 | 58 | 3 | 3598.6 us | Gen0 |
| 5 | 58 | 3 | 3373.8 us | Gen0 |

Boundary 3 otherwise retains ordinary timing. Across the five processes its median remains `7.8–8.8 us` and p95 remains `8.7–10.4 us`. No returned boundary-3 exceedance occurs without GC activity.

Process 4 also contains three isolated non-GC exceedances: boundary 290 at `817.7 us`, boundary 6 at `490.9 us`, and boundary 49 at `1597.4 us`. None reproduces in another process and none satisfies the frozen same-boundary rule.

## 3. Allocation / GC attribution discovered by returned review

The returned per-call CSV evidence adds an important engineering attribution that the aggregate classifier intentionally did not use as a veto:

- every one of the 20,480 measured C3 calls in every process reports exactly `416` allocated bytes;
- each process therefore reports `8,519,680` measured allocated bytes across the timing corpus;
- each process records exactly one Gen0 collection and zero Gen1/Gen2 collections during the measured region;
- that single Gen0 collection occurs on the same measured call as the boundary-3 exceedance in all five processes;
- the recurrence is at pass 58 under the identical deterministic rotated ordering in every process.

This means the frozen machine rule is satisfied, but the evidence does **not** demonstrate that boundary 3 has an intrinsically expensive thermodynamic branch. The stronger supported interpretation is that immutable C3 has a reproducible per-call allocation pattern and the deterministic measurement sequence repeatedly aligns the resulting Gen0 pause with boundary 3.

The historical strict maxima remain real wall-clock observations and are not erased. The attribution only changes what a later C4 plan is allowed to claim about their owner.

## 4. Engineering adjudication

The returned classification is accepted exactly as authored:

```text
C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED
```

The corresponding engineering interpretation is narrower:

```text
C3 cross-process wall-clock tail is reproducible.
The reproduced owner is strongly GC/allocation-correlated.
Boundary 3 is the deterministic observation point, not yet a proven boundary-specific thermodynamic slow path.
```

Therefore:

- the frozen rule has fired and **C4 planning is justified**;
- C4 must remain separately versioned and test-only first;
- a C4 plan must target the demonstrated allocation / managed-runtime tail mechanism rather than special-case boundary 3 merely because it owns the repeated observed pause;
- any later C4 evidence must preserve physical/seam completeness and the unchanged strict single-call maximum ceiling;
- varied/decorrelated boundary ordering should be included in later performance evidence so a GC cadence cannot masquerade as a thermodynamic boundary owner;
- C4 implementation is **not** authorized by this review;
- RP1C remains **not authorized** until separately returned C4 evidence is reviewed.

A separate performance-contract adjudication is not selected at this point because the returned outcome follows the `C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED` branch of the already-frozen replanning contract. No threshold is widened.

## 5. Frozen authority after review

```text
Refinement 5 returned evidence = VALIDATED
machine classification = C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED
C3 source = IMMUTABLE
strict single-call ceiling = UNCHANGED
historical Refinement 3/4 maxima = PRESERVED
C4 planning = JUSTIFIED / NEXT AUTHORIZED PLANNING GATE
C4 implementation = NOT AUTHORIZED
RP1C = NOT AUTHORIZED
production thermodynamic repair = NOT AUTHORIZED
thermodynamic tolerance change = NOT AUTHORIZED
exact-v9 change = NOT AUTHORIZED
VR3 = NOT AUTHORIZED
P3-R1 = NOT AUTHORIZED
second replacement-long = NOT AUTHORIZED
```

## 6. Next gate

The next allowed project action is a **C4 planning gate only**. It may define a new test-only candidate and its evidence/acceptance contract, but it must not implement C4 in production and must not perform RP1C selection in the same step.
