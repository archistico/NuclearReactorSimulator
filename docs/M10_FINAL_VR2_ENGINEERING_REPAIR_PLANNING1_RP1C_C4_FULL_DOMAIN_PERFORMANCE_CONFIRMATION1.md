# M10 Final — VR2 Engineering Repair Planning 1 — RP1C C4 Full-Domain Performance Confirmation 1

## Purpose

This gate closes the final performance-evidence gap identified by RP1C Planning 1 before any RP1C selection can be considered.

It measures the **immutable** `C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE` over the frozen RP1A exact-v9 and seam corpora. It does not modify C4, regenerate the corpus, change thresholds, select a candidate or alter production/runtime behavior.

## Prerequisite adjudication

The returned RP1C Planning 1 artifact set is adjudicated PASS. It freezes:

- D3 not selection-ready because its historical seam maximum `16504.9 us` exceeds the unchanged `409.30666666666673 us` maximum;
- C4 physical/R1 qualification complete;
- C4 full-domain performance confirmation pending;
- selection-ready count `0` before this gate;
- `SELECT-C4 | SELECT-NONE` as the later decision space only after green returned confirmation evidence.

## Frozen protocol

Five fresh focused processes execute the same immutable C4 candidate.

Per process:

```text
exact-v9: 360 rows × 64 measured passes = 23,040 calls
           16 warm-up passes

seam:     1,280 rows × 16 measured passes = 20,480 calls
           4 warm-up passes

per process = 43,520 measured calls
five processes = 217,600 measured calls
rotation stride = 37
```

The timing recorder uses preallocated value-type arrays created before the measured region. Candidate allocation is measured per call; whole-region allocation is reconciled after both exact-v9 and seam measurement regions. Harness allocation must be exactly zero for evidence validity.

## Corrected performance predicate

Each of the five processes must independently satisfy the unchanged predicate:

```text
exact-v9 median <= 94.8 us
exact-v9 p95 <= 158.80666666666667 us
exact-v9 single-call max <= 409.30666666666673 us
all-seam-sides single-call max <= 409.30666666666673 us
exact-v9 median candidate allocation <= 2816 B
all exact-v9 calls resolved
all seam calls resolved
```

The previously established R1 `0 B/call` result remains frozen evidence. It is **not** generalized to every full-domain fallback path. Full-domain fallback is permitted by the planning contract.

A performance miss is negative engineering evidence, not a harness RED. xUnit failure is reserved for opt-in/configuration, frozen-corpus, structural or evidence-integrity failures.

## Evidence tree

The runner owns the artifact root reset once. Each fresh process owns only its `process-01` … `process-05` directory.

Each process writes five files:

1. `01-process-contract.txt`
2. `02-exact-v9-call-timing.csv`
3. `03-seam-call-timing.csv`
4. `04-runtime-context.txt`
5. `05-process-summary.txt`

The adjudicator then writes five aggregate files:

1. `01-contract-and-provenance.txt`
2. `02-cross-process-run-summary.csv`
3. `03-cross-process-seam-side-summary.csv`
4. `04-confirmation-evidence-adjudication.txt`
5. `05-rp1c-c4-full-domain-performance-confirmation1-summary.txt`

Total required files: **30**.

## Classification

The aggregate classification is one of:

```text
C4-FULL-DOMAIN-PERFORMANCE-CONFIRMED
C4-FULL-DOMAIN-PERFORMANCE-NOT-CONFIRMED
```

Even the positive classification does not itself authorize RP1C selection. Complete returned confirmation evidence must first be adjudicated. Only then may a separate RP1C selection gate consider `SELECT-C4` or `SELECT-NONE`.

## Authority boundary

This package keeps all of the following false:

```text
RP1C-SELECTION-AUTHORIZED=False
PRODUCTION-REPAIR-AUTHORIZED=False
THRESHOLD-CHANGE-AUTHORIZED=False
EXACT-V9-CHANGE-AUTHORIZED=False
VR3-AUTHORIZED=False
P3-R1-AUTHORIZED=False
SECOND-REPLACEMENT-LONG-AUTHORIZED=False
```


## Hotfix history

- **Hotfix 1:** Windows PowerShell 5.1 compatibility; removes the unsupported two-argument `String.Contains` overload from the static validator.
- **Hotfix 2:** ordinary Release compile repair; aligns `ReadCsvLines` declaration with its actual `string[]` return value so the existing array `.Length` usages compile.

Neither hotfix changes C4 semantics, the frozen corpora, thresholds, timing protocol, evidence classification or authority boundary.
