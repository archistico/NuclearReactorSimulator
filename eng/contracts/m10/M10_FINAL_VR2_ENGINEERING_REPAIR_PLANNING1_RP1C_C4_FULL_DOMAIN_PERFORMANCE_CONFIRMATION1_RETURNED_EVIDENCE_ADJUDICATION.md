# M10 Final — VR2 Engineering Repair Planning 1 — RP1C C4 Full-Domain Performance Confirmation 1 — Returned-Evidence Adjudication

## Decision

The complete returned 30-file Full-Domain Performance Confirmation 1 tree is accepted as **valid engineering evidence**.

Frozen classification:

```text
C4-FULL-DOMAIN-PERFORMANCE-NOT-CONFIRMED
```

This is **not** an infrastructure RED and does not invalidate the earlier C4 physical/R1 qualification. It means only that the immutable C4 candidate did not satisfy the frozen corrected full-domain performance predicate in all five fresh processes.

RP1C selection remains unauthorized.

## Returned evidence integrity

The returned tree contains exactly:

- 5 aggregate/adjudication files;
- 5 fresh process directories;
- 5 files per process;
- 30 files total.

Each process completed:

- 23,040 measured exact-v9 calls;
- 20,480 measured seam calls;
- 43,520 measured calls total.

Across five processes the returned evidence therefore contains exactly **217,600 measured calls**.

All exact-v9 and seam calls resolved. Whole-region harness allocation is `0 B` in all five processes.

## Independent reconstruction

The aggregate files were independently reconciled against the 25 per-process files.

| Run | Exact median us | Exact p95 us | Exact max us | Seam max us | Exact median alloc B | Strict predicate |
|---:|---:|---:|---:|---:|---:|:---|
| 1 | 2.9 | 27.9 | 150.4 | 315.1 | 0 | PASS |
| 2 | 2.8 | 27.2 | 237.4 | 314.1 | 0 | PASS |
| 3 | 2.8 | 27.8 | 223.2 | 324.1 | 0 | PASS |
| 4 | 2.8 | 26.3 | 1027.1 | 301.1 | 0 | FAIL |
| 5 | 2.8 | 26.6 | 796.1 | 308.1 | 0 | FAIL |

Frozen ceilings remain unchanged:

```text
exact-v9 median <= 94.8 us
exact-v9 p95 <= 158.80666666666667 us
single-call max <= 409.30666666666673 us
exact-v9 median allocation <= 2816 B
```

Median, p95, seam maximum and median allocation are comfortably inside the frozen limits in every process. The full-domain classification is negative solely because two exact-v9 single calls exceed the unchanged maximum ceiling.

## Exact-v9 exceedances

Exactly two of the 115,200 exact-v9 measured calls exceed the strict maximum:

```text
run 4
measured pass 18
row 170
probe exact-v9-5-to-6mwe
logical step 140784
node suction
resolution path C2-MIXTURE-PREFIX
elapsed 1027.1 us
allocated 0 B

run 5
measured pass 18
row 40
probe background-reference
logical step 48000
node suction
resolution path C2-MIXTURE-PREFIX
elapsed 796.1 us
allocated 0 B
```

Neither exceedance coincides with a GC collection. All five runtime-context files report:

```text
gc-gen0-collections-during-measured-region=0
gc-gen1-collections-during-measured-region=0
gc-gen2-collections-during-measured-region=0
harness-allocated-bytes=0
```

## Tail pattern

Looking below the strict ceiling provides useful attribution evidence without changing the classification. The only five exact-v9 calls above `100 us` occur at measured passes `15, 17, 17, 18, 18`, and every one uses `C2-MIXTURE-PREFIX`:

```text
run 1 / pass 15 / 150.4 us
run 2 / pass 17 / 237.4 us
run 3 / pass 17 / 223.2 us
run 4 / pass 18 / 1027.1 us
run 5 / pass 18 / 796.1 us
```

This pattern supports a bounded **runtime/tiering attribution hypothesis** for the next planning step. It does **not** prove tiered compilation, dynamic PGO, OS scheduling or another runtime mechanism as the cause. No such causal label is promoted by this adjudication.

## Seam evidence

All 102,400 seam calls remain below the strict maximum. Cross-process maxima are:

```text
R1-SIDE          16.6 us
R4-LIQUID-SIDE   18.9 us
R4-VAPOR-SIDE   324.1 us
R2-SIDE           2.1 us
```

There are zero unresolved seam calls and zero seam exceedances.

The earlier C4 R1 closure therefore remains valid and is not reopened by this full-domain result.

## Allocation evidence

Candidate-call allocation summed across the five processes is `72,960 B`; the exact-v9 median allocation remains `0 B` in every process.

Independent inspection shows the non-zero allocations are confined to 160 seam calls on `C3-SATURATED-VAPOR-SEAM-FALLBACK`, each allocating `456 B`. This remains far below the historical median-allocation criterion and does not own the failed full-domain classification.

The two strict exact-v9 exceedances themselves allocate `0 B`.

## Engineering interpretation

The returned evidence supports all of the following:

1. C4 remains semantically/physically qualified from the earlier returned C4 gate.
2. C4 remains qualified for the R1 allocation/tail owner previously repaired.
3. Full-domain median, p95, seam maximum and exact-v9 median allocation are green.
4. Two rare exact-v9 wall-clock tails prevent full-domain confirmation under the unchanged single-call maximum.
5. Both strict misses are on `C2-MIXTURE-PREFIX`; neither is a GC event and neither allocates on the measured call.
6. The cross-process pass-position pattern merits runtime/tiering attribution before any new thermodynamic candidate mutation is considered.

The evidence does **not** justify:

- widening the `409.30666666666673 us` ceiling;
- selecting C4 now;
- changing C4 or immutable C2/C3 now;
- changing production thermodynamics;
- changing exact-v9;
- treating runtime/tiering as already proven.

## Authority

This returned-evidence adjudication freezes:

```text
RETURNED-FDPC1-EVIDENCE-ADJUDICATION=PASS
FDPC1-CLASSIFICATION=C4-FULL-DOMAIN-PERFORMANCE-NOT-CONFIRMED

RP1C-SELECTION-AUTHORIZED=False
PRODUCTION-REPAIR-AUTHORIZED=False
THRESHOLD-CHANGE-AUTHORIZED=False
EXACT-V9-CHANGE-AUTHORIZED=False
VR3-AUTHORIZED=False
P3-R1-AUTHORIZED=False
SECOND-REPLACEMENT-LONG-AUTHORIZED=False
```

The only newly justified next activity is **planning-only** for a bounded exact-v9 wall-clock-tail attribution/refinement gate. That future planning must preserve C4, C2/C3, the frozen corpora and all thresholds unless a later separately adjudicated decision explicitly changes authority.
