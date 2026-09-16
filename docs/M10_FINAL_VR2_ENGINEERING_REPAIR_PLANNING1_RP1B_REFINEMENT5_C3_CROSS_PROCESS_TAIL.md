# M10 Final — VR2 Engineering Repair Planning 1 — RP1B Refinement 5 — C3 Cross-Process Wall-Clock Tail Reproducibility

**Status:** VALIDATED RETURNED EVIDENCE — cross-process execution complete; see returned-evidence adjudication.  
**Prerequisite:** RP1B Performance Measurement Replanning 1 returned `PASS-AS-AUTHORED`.  
**Authority:** C3 remains immutable. C4 implementation, RP1C selection, production thermodynamic changes, tolerance changes, exact-v9 changes, VR3, P3-R1 and second replacement-long execution remain unauthorized.

## 1. Purpose

Refinement 3 and Refinement 4 left a strict wall-clock maximum blocker without identifying a stable candidate-specific slow path. Refinement 4 observed one screen exceedance at boundary 3 with Gen0 activity and one targeted exceedance at boundary 191 without GC activity. The same boundary did not reproduce.

Performance Measurement Replanning 1 therefore froze a cross-process rule before collecting any new timing data. Refinement 5 implements that rule without changing C3, the RP1A corpus or the `409.30666666666673 us` strict single-call ceiling.

## 2. Frozen process protocol

Refinement 5 uses **5 independent focused-test processes**. Each process starts a fresh .NET test runtime and measures exactly the same immutable C3 R1-side seam corpus:

```text
320 R1 boundaries
16 warm-up passes per process
64 measured passes per process
20,480 measured calls per process
rotation stride = 37
strict max ceiling = 409.30666666666673 us
```

The runner owns process repetition. C3 does not know that five processes exist and is not modified between runs.

Each process records:

- complete call identity and boundary identity;
- elapsed wall-clock time;
- allocation delta;
- Gen0/Gen1/Gen2 collection-count deltas outside the timed region;
- per-boundary median, p95, max and exceedance count;
- process-level max and owner boundary.

## 3. Candidate-specific slow-path rule

The exact frozen machine rule is:

```text
SAME-BOUNDARY-EXCEEDS-IN-AT-LEAST-2-INDEPENDENT-PROCESS-RUNS
```

A candidate-specific C3 slow path is confirmed only when the **same boundary identity** exceeds the unchanged `409.30666666666673 us` ceiling in at least two of the five independent process runs.

The three possible evidence classifications are:

```text
C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED
C3-CROSS-PROCESS-ISOLATED-WALL-CLOCK-TAIL
C3-CROSS-PROCESS-NO-EXCEEDANCE-OBSERVED
```

These classifications do not themselves authorize a production change.

## 4. Interpretation boundaries

If `C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED` is returned, the evidence is sufficient only to justify a later C4 **planning** decision. C4 implementation remains unauthorized until the returned Refinement 5 artifacts are reviewed.

If the result is `C3-CROSS-PROCESS-ISOLATED-WALL-CLOCK-TAIL` or `C3-CROSS-PROCESS-NO-EXCEEDANCE-OBSERVED`, C4 remains unjustified and the next engineering step is a separate performance-contract adjudication. RP1C remains unauthorized in either case until explicit returned-evidence review.

Historical Refinement 3/4 maxima are preserved regardless of Refinement 5 outcome. A clean Refinement 5 does not erase them and no performance threshold is widened.

## 5. Runner structure

The runner executes:

```text
[1/4] static returned-replanning / contract audit
[2/4] ordinary Release gate
[3/4] five independent focused test processes
[4/4] cross-process same-boundary adjudication
```

The focused test may complete successfully even when the timing ceiling is exceeded. Exceedances are evidence, not harness failures. The gate fails only for incomplete/inconsistent evidence, build/test failure or adjudication failure.

## 6. Evidence layout

Each process writes five files under:

```text
artifacts/m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-refinement5/process-01/
...
process-05/
```

Each process folder contains:

```text
01-process-contract.txt
02-c3-r1-call-timing.csv
03-c3-r1-boundary-summary.csv
04-runtime-context.txt
05-process-summary.txt
```

The root aggregate folder contains:

```text
01-contract-and-provenance.txt
02-cross-process-run-summary.csv
03-cross-process-boundary-summary.csv
04-performance-reproducibility-adjudication.txt
05-rp1b-refinement5-summary.txt
```

## 7. Exit rule

A completed Refinement 5 freezes only cross-process timing evidence. Return the full artifact folder before any C4 planning, performance-contract adjudication or RP1C selection.

## 8. Pre-execution static review

The candidate includes `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT5_PREEXECUTION_REVIEW.md`, which records the dirty-working-tree, xUnit-analyzer, PowerShell 5.1, process-isolation and byte-identity checks performed before packaging. It does not replace the local Release build/test gate.

## 9. Returned outcome and review

The complete returned artifact set has now been reviewed and frozen. The machine result is `C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED`, with one same-boundary owner: boundary 3 exceeds the unchanged ceiling in all five independent process runs.

Returned review also establishes that all five boundary-3 exceedances coincide with the only Gen0 collection in their process, while every measured C3 call reports exactly 416 allocated bytes. Boundary 3 retains ordinary non-GC median/p95 timing. The accepted interpretation is therefore reproducible C3 allocation/GC wall-clock tail evidence, not a proven boundary-specific thermodynamic slow branch.

See `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT5_RETURNED_EVIDENCE_ADJUDICATION.md`. C4 planning is now justified; C4 implementation and RP1C remain unauthorized.
