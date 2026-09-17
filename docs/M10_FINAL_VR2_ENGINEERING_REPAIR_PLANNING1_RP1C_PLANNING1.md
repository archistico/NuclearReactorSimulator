# M10 Final — VR2 Engineering Repair Planning 1 — RP1C Planning 1

**Status:** CANDIDATE — PLANNING ONLY  
**Prerequisite:** returned C4 evidence independently adjudicated `PASS` as `C4-QUALIFIED-ALLOCATION-TAIL-CLOSED`.  
**Authority:** this document plans the final readiness/selection sequence. It does not select a repair, modify production thermodynamics, change thresholds, reinterpret exact-v9, execute VR3/P3-R1, or authorize a second replacement-long baseline.

## 1. Purpose

RP1C is the repair-family selection stage. Planning 1 freezes the corrected selection predicate, current C4/D3 evidence state, the remaining evidence gap and the later decision space before any selection is attempted.

The planning gate deliberately separates four questions:

1. which candidates still have authoritative evidence relevant to selection;
2. which physical/seam criteria are already closed;
3. whether the actual final candidate has complete performance evidence under the corrected predicate;
4. what a later RP1C selection decision may authorize.

A candidate is not selected by inference. A unique plausible candidate is not automatic selection.

## 2. Frozen candidate history

The candidate lineage remains immutable provenance:

```text
B1/C1/D1 first-generation evidence
C2/D2 refinement
C3/D3 seam-complete refinement
C3 performance attribution / localization / cross-process refinement
C4 allocation-neutral, C3-bit-equivalent R1 performance repair
```

Earlier candidates are not silently retuned or relabeled.

For the RP1C decision path, Planning 1 carries forward:

```text
C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE
D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR
NO-SELECTION
```

C3 remains provenance because C4 is bit-equivalent to C3 on all 1,967 planned semantic comparisons.

## 3. Corrected full selection predicate

The historical Refinement-2 `rp1c_selection_eligible` boolean is provenance only. Refinement 3 established that the seam worst-case must be included in selection.

A candidate may be selected only when frozen evidence supports all of:

```text
all frozen points resolved
no core wrong-phase result
existing VR2 25% blocking ceiling met
Planning 1 10% target met
100% exact-v9 phase agreement
all 1,280 seam probes resolved
seam phase mismatch count = 0
deterministic repeat passes
Resolve median <= 94.8 us
Resolve p95 <= 158.80666666666667 us
exact-v9 single-call max <= 409.30666666666673 us
all-seam-sides single-call max <= 409.30666666666673 us
median candidate allocation <= 2816 B
```

No threshold is changed by RP1C Planning 1.

## 4. D3 readiness — blocked by frozen evidence

D3 is physically complete:

```text
all frozen points resolved = true
exact-v9 phase agreement = 100%
planning target met = true
all 1,280 seam probes resolved = true
seam phase mismatch = 0
deterministic repeat = true
```

Its ordinary returned Resolve timing is also within the median/p95/max ceilings:

```text
median = 14.7 us
p95    = 15.3 us
max    = 208.5 us
```

But the same frozen artifact records:

```text
seam max = 16504.9 us
```

which exceeds:

```text
409.30666666666673 us
```

Refinement 3 already froze this omission in the old eligibility boolean. Therefore:

```text
D3 selection-ready now = false
blocker = SEAM-MAX-16504.9-US-EXCEEDS-409.30666666666673-US
```

D3 remains valuable comparator evidence, but it cannot be selected under the current corrected predicate.

## 5. C4 readiness — physical/R1 closure complete, full-domain timing still to confirm

C4 is not a new thermodynamic interpretation. Returned semantic evidence establishes bit-equivalence to C3 over:

```text
39 VR2 inverse rows
360 exact-v9 rows
1,280 seam rows
288 hydraulic rows
= 1,967 comparisons
```

All semantic mismatch counts are zero.

C3 already closes the physical/seam portion of the predicate:

```text
all frozen points resolved = true
no core wrong phase = true
VR2 blocking ceiling met = true
Planning 1 target met = true
exact-v9 phase agreement = 100%
all seam probes resolved = true
seam phase mismatch = 0
deterministic repeat = true
```

C4 then closes the specifically problematic R1 performance path with:

```text
204,800 measured R1 calls
0 unresolved
0 nonzero-allocation calls
0 candidate allocated bytes
0 harness allocated bytes
0 fallback calls
0 GC-active measured calls
0 strict-max exceedances
worst observed max = 329.9 us
```

However, C4 itself has not yet been timed over the complete 360-row exact-v9 corpus and all four seam sides. Historical C3 evidence shows those non-R1 paths were below ceiling, but C4 introduces allocation-neutral mixture/liquid prefixes. Bit-equivalent output does not prove identical wall-clock behavior.

Therefore the conservative current status is:

```text
C4 physical/seam qualification = complete
C4 R1 performance-tail closure = complete
C4 full-domain performance confirmation = incomplete
C4 selection-ready now = false
```

This is an evidence gap, not a failed C4 result.

## 6. Why a bounded final confirmation is required

The original RP1B contract required both steady-state and worst-case cost evidence against the RP1A ceiling. Refinement 3 corrected the selection predicate so both exact-v9 and seam maxima matter.

C4 was intentionally scoped to the R1 tail owner and its 204,800-call campaign successfully answered that question. RP1C must not silently extrapolate from R1 timing to every C4 path.

A final immutable-C4 full-domain confirmation therefore closes the only remaining performance-evidence gap before selection.

## 7. Planned C4 Full-Domain Performance Confirmation 1

After RP1C Planning 1 artifacts are returned and adjudicated, the next separately versioned evidence gate may be:

```text
RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION1
```

It must not modify C4, the corpus or thresholds.

Each of five fresh processes measures the immutable C4 candidate using the established Refinement-3 corpus/protocol:

```text
exact-v9:
  360 rows
  16 warm-up passes
  64 measured passes
  23,040 measured calls / process

seam:
  1,280 rows
  4 warm-up passes
  16 measured passes
  20,480 measured calls / process

per process = 43,520 calls
5 processes  = 217,600 calls
```

The measured harness must remain allocation-neutral. Candidate allocation is measured, but the full-domain criterion remains the original `median allocation <= 2816 B`; the R1-specific zero-allocation result remains frozen evidence and is not generalized to every Region-2/fallback path.

Every process must satisfy the unchanged corrected performance predicate. A ceiling miss is negative engineering evidence, not an infrastructure RED.

## 8. Selection state after confirmation

Only if returned Full-Domain Performance Confirmation 1 is complete and green may C4 become `selection-ready=True`.

At that point the RP1C selection decision space is:

```text
SELECT-C4
SELECT-NONE
```

`SELECT-NONE` remains mandatory even if C4 is the only ready candidate.

D3 does not enter `SELECT-D3` under the current evidence. Reopening D3 would require separately authorized/versioned corrected evidence rather than reinterpretation of its historical boolean.

## 9. Post-selection authority boundary

Even a later `SELECT-C4` result does not directly modify production code.

After returned RP1C selection adjudication, the conservative next action is:

```text
R1-IMPLEMENTATION-PLANNING-ONLY
```

Only a later explicit milestone may introduce a new opt-in closure mode and begin the authored R1-R6 production/requalification route.

RP1C Planning 1 therefore keeps all of these false:

```text
RP1C-SELECTION-AUTHORIZED=False
PRODUCTION-REPAIR-AUTHORIZED=False
THRESHOLD-CHANGE-AUTHORIZED=False
EXACT-V9-CHANGE-AUTHORIZED=False
VR3-AUTHORIZED=False
P3-R1-AUTHORIZED=False
SECOND-REPLACEMENT-LONG-AUTHORIZED=False
```

## 10. Planning artifacts

The local static audit writes exactly:

```text
01-contract-and-provenance.txt
02-selection-readiness-matrix.csv
03-selection-policy-summary.txt
04-preexecution-review.txt
```

The matrix must show **zero candidates currently selection-ready**:

- C4: `FULL-DOMAIN-C4-PERFORMANCE-CONFIRMATION-REQUIRED`;
- D3: `SEAM-MAX-EXCEEDS-STRICT-CEILING`.

That does not mean the repair program has failed. It means RP1C still has one bounded confirmation step before a defensible selection decision.

## 11. Gate semantics

A local Planning 1 PASS means only that:

- returned C4 authority is present;
- frozen C4/D3 evidence matches pinned evidence;
- D3 is correctly blocked by the existing strict seam max;
- C4 physical/R1 qualification is correctly recognized;
- the missing C4 full-domain timing evidence is not silently inferred;
- the five-process confirmation contract is frozen;
- no selection or production authority is granted.

## 12. Next permitted action

Return the complete RP1C Planning 1 artifact folder. Only after returned-planning adjudication may `RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION1` be implemented.


## 13. Returned Full-Domain Confirmation outcome

The separately authorized immutable-C4 Full-Domain Performance Confirmation 1 has now returned a complete 30-file evidence tree and is adjudicated valid evidence.

Frozen outcome:

```text
C4-FULL-DOMAIN-PERFORMANCE-NOT-CONFIRMED
```

Three of five fresh processes meet the corrected full-domain predicate. Runs 4 and 5 each contain one exact-v9 single-call maximum above the unchanged `409.30666666666673 us` ceiling. All exact-v9 median/p95/allocation criteria and all seam-side maxima remain green.

Therefore the planned `SELECT-C4 | SELECT-NONE` decision gate is **not opened**. C4 remains physically/R1 qualified but is not selection-ready under the frozen corrected full-domain predicate.

The next permitted activity is planning-only for bounded attribution/reproducibility of the rare exact-v9 wall-clock tail. No candidate mutation, threshold change or production repair is authorized by this result.
