# M10 Final — VR2 Engineering Repair Planning 1 — RP1B C4 Returned-Evidence Adjudication

## Status

**RETURNED-EVIDENCE ADJUDICATION: PASS**

Accepted classification:

```text
C4-QUALIFIED-ALLOCATION-TAIL-CLOSED
```

This adjudication accepts only the returned test-only C4 evidence. It does not select C4 for production and does not authorize a production/runtime thermodynamic repair, threshold change, exact-v9 change, VR3, P3-R1 or a second replacement-long baseline.

It advances authority only to **RP1C planning**. RP1C selection remains a separate decision.

## 1. Returned artifact integrity

The returned C4 evidence tree contains exactly the frozen contract shape:

```text
9 aggregate files
10 timing process directories * 5 files
= 59 files
```

The ten timing directories are Lane A process-01..05 and Lane B process-01..05. All ten timing processes have distinct process IDs and report .NET runtime 10.0.5, processor count 32 and Stopwatch frequency 10,000,000 Hz.

The complete returned tree is frozen under:

```text
eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_C4_Artifacts/
```

## 2. Semantic equivalence adjudication

The semantic corpus is complete:

```text
VR2 inverse-applicable state rows = 39
exact-v9 state rows               = 360
seam state rows                   = 1280
state observations                = 1679
hydraulic observations            = 288
total semantic comparisons        = 1967
```

Independent review of the returned CSV payloads confirms:

```text
state bit mismatches      = 0
hydraulic bit mismatches  = 0
repeat mismatches         = 0
resolve mismatches        = 0
```

C4 is therefore bit-equivalent to frozen C3 on the complete planned semantic corpus, including deterministic repeat evidence.

## 3. Allocation and fallback closure

The timing campaign contains ten fresh processes and exactly:

```text
20,480 measured calls / process
10 processes
= 204,800 measured C4 calls
```

Independent review of all per-call timing CSV files confirms:

```text
unresolved calls                    = 0
nonzero candidate allocation calls  = 0
candidate allocated bytes           = 0
whole-region harness allocated bytes= 0
fallback calls                      = 0
GC-active measured calls            = 0
```

The measured resolution-path distribution is:

```text
C2-NEAR-BOUNDARY-LIQUID-PREFIX = 198,400 calls
C2-LIQUID-TABLE-PREFIX         =   6,400 calls
C2-FALLBACK                    =       0 calls
```

This exactly matches 310 near-boundary rows plus 10 liquid-table rows per 320-boundary pass.

The C4 R1 allocation contract is therefore closed at **0 B/call**, and the measured harness is allocation-neutral.

## 4. Wall-clock adjudication

Frozen ceilings remain unchanged:

```text
median <= 94.8 us
p95    <= 158.80666666666667 us
max    <= 409.30666666666673 us
```

All ten independent lane/run distributions satisfy all three limits.

Observed per-process summary:

| Lane | Run | Median us | p95 us | Max us | Over max | Allocation | Fallback |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| A | 1 | 5.0 | 5.2 | 42.4 | 0 | 0 B | 0 |
| A | 2 | 8.2 | 8.4 | 28.3 | 0 | 0 B | 0 |
| A | 3 | 5.1 | 5.3 | 28.8 | 0 | 0 B | 0 |
| A | 4 | 15.9 | 18.5 | 329.9 | 0 | 0 B | 0 |
| A | 5 | 5.2 | 5.4 | 50.3 | 0 | 0 B | 0 |
| B | 1 | 5.0 | 5.2 | 23.3 | 0 | 0 B | 0 |
| B | 2 | 5.2 | 5.3 | 23.5 | 0 | 0 B | 0 |
| B | 3 | 5.2 | 5.3 | 23.9 | 0 | 0 B | 0 |
| B | 4 | 8.2 | 8.5 | 27.4 | 0 | 0 B | 0 |
| B | 5 | 14.7 | 15.6 | 33.3 | 0 | 0 B | 0 |

The worst observed call is Lane A / run 4 / boundary 196 / pass 24:

```text
temperature = 270.47928985357294 C
elapsed     = 329.9 us
allocation  = 0 B
GC delta    = 0
path        = C2-NEAR-BOUNDARY-LIQUID-PREFIX
```

It remains 79.40666666666675 us below the unchanged strict maximum ceiling.

Independent recomputation of all 320 cross-process boundary summary rows matches the returned aggregate summary exactly: every boundary has 10 lane/run observations, 640 measured calls, zero max-ceiling exceedances, zero GC-active calls, zero nonzero-allocation calls and zero fallback calls.

## 5. Engineering interpretation

Refinement 5 established that immutable C3 allocated 416 B/call and repeatedly placed the same boundary-3 sample on a Gen0-associated wall-clock tail. The historical whole-region recorder was not allocation-neutral, so candidate-only ownership of the exact GC cadence was intentionally not claimed.

C4 removes that ambiguity in the qualified R1 timing campaign:

```text
C3 historical allocation = 416 B/call
C4 measured allocation   =   0 B/call

C3 Refinement-5 exceedances = 8 / 102,400 calls
C4 exceedances              = 0 / 204,800 calls

C4 measured GC collections  = 0
C4 immutable-C2 fallbacks   = 0
```

The C4 campaign therefore supports the narrower engineering conclusion that the planned allocation-neutral prefix design preserves frozen C3 semantics while closing the R1 allocation and strict wall-clock tail contract on the frozen qualification corpus.

The result does not establish production suitability by itself. C4 remains a test/reference/shadow candidate until a separate RP1C selection decision evaluates the full repair-selection contract and production-version/requalification implications.

## 6. Authority after adjudication

The following authority is now granted:

```text
RP1C-PLANNING-AUTHORIZED=True
```

The following remain explicitly false:

```text
RP1C-SELECTION-AUTHORIZED=False
PRODUCTION-REPAIR-AUTHORIZED=False
THRESHOLD-CHANGE-AUTHORIZED=False
EXACT-V9-CHANGE-AUTHORIZED=False
VR3-AUTHORIZED=False
P3-R1-AUTHORIZED=False
SECOND-REPLACEMENT-LONG-AUTHORIZED=False
```

The next permitted action is a separately versioned **RP1C planning gate**. It may consume this qualified C4 evidence together with the frozen RP1A/RP1B candidate history, but it may not modify production thermodynamics or exact-v9.

## 7. Adjudication decision

```text
RETURNED-C4-EVIDENCE=COMPLETE
SEMANTIC-BIT-EQUIVALENCE=PASS
ALLOCATION-CLOSURE=PASS
WALL-CLOCK-TAIL-CLOSURE=PASS
C4-CLASSIFICATION=C4-QUALIFIED-ALLOCATION-TAIL-CLOSED
RETURNED-EVIDENCE-ADJUDICATION=PASS
NEXT-AUTHORIZED-ACTION=RP1C-PLANNING-ONLY
```
