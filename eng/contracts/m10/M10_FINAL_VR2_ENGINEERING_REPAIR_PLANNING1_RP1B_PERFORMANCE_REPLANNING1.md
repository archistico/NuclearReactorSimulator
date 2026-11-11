# M10 Final — VR2 Engineering Repair Planning 1 — RP1B Performance Measurement Replanning 1

**Status:** VALIDATED / CLOSED — returned static audit `PASS-AS-AUTHORED`; Refinement 5 has now returned validated `C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED` evidence.  
**Prerequisite:** returned RP1B Refinement 3 and Refinement 4 evidence reviewed.  
**Authority:** planning only. C3 remains immutable; C4, RP1C selection, production source changes, thermodynamic repair, tolerance changes, exact-v9 changes, VR3, P3-R1 and second replacement-long execution remain unauthorized.

## 1. Why this replanning exists

Refinement 3 removed the original exact-v9 candidate-cost concern for immutable C3:

```text
23040 exact-v9 measured Resolve calls
exact-v9 max = 150.5 us
targeted exact-v9 max = 162.7 us
```

The strict performance contract remained blocked by one R1-side seam wall-clock observation at `3667.1 us`.

Refinement 4 then localized and repeated only the existing 320 R1-side seam probes. Its returned evidence contains two isolated wall-clock exceedances, but they are not localized to the same candidate boundary/path:

```text
screen:
  20480 calls
  one exceedance
  max = 2620.6 us
  boundary index = 3
  boundary temperature = 47.26746165007364 C
  Gen0 activity observed around the call = true

targeted repeats:
  23 selected boundaries
  2048 calls per boundary
  one exceedance
  max = 853.5 us
  boundary index = 191
  boundary temperature = about 270.351584 C
  GC activity around the exceedance = false
```

Boundary 3 does not reproduce as the targeted exceedance owner. Boundary 191 did not exceed during the screen. Median and p95 timing for both boundaries remain in the ordinary single-digit / low-double-digit microsecond range.

This evidence does **not** justify changing C3. It also does **not** erase the frozen strict single-call maximum of `409.30666666666673 us`. The unresolved question is now measurement attribution: candidate-specific deterministic work versus managed-runtime / scheduler wall-clock tail.

## 2. Frozen decisions from Refinement 4 review

The following decisions are frozen before any additional timing data is collected:

- C3 remains `C3-VAPOR-SEAM-COMPLETE-SURROGATE` byte-for-byte;
- C4 is **not authorized** from Refinement 4 alone;
- RP1C is **not authorized**;
- the `409.30666666666673 us` single-call ceiling is neither deleted nor widened;
- the Refinement 3 and Refinement 4 exceedances remain historical evidence even if later runs are clean;
- no thermodynamic mathematics, seam coordinate, corpus row or planning target may change in this replanning step.

## 3. Why C4 is not yet justified

A production-oriented C4 would need a candidate-specific slow path that can be tied to a stable input/path characteristic.

The returned evidence instead shows:

1. Refinement 3: one isolated R1-side exceedance without boundary identity;
2. Refinement 4 screen: one isolated exceedance at boundary 3 with Gen0 activity;
3. Refinement 4 targeted stage: one isolated exceedance at boundary 191 without GC activity;
4. no same-boundary reproduction between screen and targeted stages;
5. normal median/p95 timing on both boundary 3 and 191.

Changing C3 from this evidence would risk optimizing physics/reference code for environmental scheduling noise rather than algorithmic work.

## 4. Authorized next diagnostic — Refinement 5

Only the following next diagnostic is authorized by this planning candidate after its returned audit is reviewed:

```text
RP1B Refinement 5 — C3 Cross-Process Wall-Clock Tail Reproducibility
```

Refinement 5 must not create C4 and must not modify C3.

### 4.1 Independent-process structure

Use **five independent focused-test process runs**. Each process starts from a fresh runtime process and applies the same immutable R1 seam corpus:

```text
320 R1-side boundaries
16 full warm-up passes per process
64 measured passes per process
20480 measured C3 seam calls per process
same 409.30666666666673 us strict ceiling
```

The runner, not the candidate, owns the five-process repetition.

### 4.2 Required evidence per process

Each process must record at least:

- process-run index;
- boundary index and temperature;
- measured-pass/call identity;
- elapsed wall-clock microseconds;
- allocation delta;
- Gen0/Gen1/Gen2 collection-count delta;
- whether the same strict ceiling was exceeded;
- process-level maximum and owning boundary;
- per-boundary median, p95, max and exceedance count.

A lightweight fixed no-allocation control timing lane may be recorded as environment context, but it cannot replace C3 timing and cannot change the ceiling.

### 4.3 Candidate-specific slow-path rule frozen before execution

A C3 slow path is considered candidate-specific enough to justify C4 planning only if **the same boundary identity exceeds the unchanged maximum ceiling in at least two independent process runs**.

This rule is not an RP1C acceptance criterion. It answers only whether C4 is justified.

Possible engineering interpretations after returned Refinement 5 evidence review are:

```text
C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED
C3-CROSS-PROCESS-ISOLATED-WALL-CLOCK-TAIL
C3-CROSS-PROCESS-NO-EXCEEDANCE-OBSERVED
```

### 4.4 Consequences

If `C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED`:

- C4 may be planned, but remains separately versioned and test-only first;
- the affected path/boundary must be identified before any code change;
- C3 remains frozen provenance.

If no candidate-specific same-boundary slow path is confirmed:

- C4 remains unjustified;
- do **not** silently promote C3 to RP1C;
- return to a formal performance-contract adjudication step that decides how managed-runtime wall-clock tails should be represented, while retaining all historical max observations.

## 5. What this replanning does not change

It does not change:

- RP1A corpus or seam offsets;
- VR2 `25%` blocking ceiling;
- `10%` planning target;
- exact-v9;
- `CorrelationConsistentInverseDomain`;
- C3 mathematics;
- C3/D3 frozen evidence;
- the `409.30666666666673 us` strict single-call observation ceiling;
- the requirement for a new opt-in closure mode and new exact identity before any later production activation.

## 6. Exit from this planning gate

A PASS from the static audit means only:

```text
returned Refinement 4 evidence = accepted as planning input
C4 planning now = JUSTIFIED BY RETURNED REFINEMENT 5 RULE
C4 implementation now = NOT AUTHORIZED
RP1C now = NOT AUTHORIZED
Refinement 5 returned evidence = VALIDATED / CLOSED
```

Return the planning-audit artifact before implementing Refinement 5.

## 7. Hotfix 1 — validator/document marker alignment

Attempt 1 stopped in the static validator before producing a planning PASS artifact because the validator required the literal machine token `SAME-BOUNDARY-EXCEEDS-IN-AT-LEAST-2-INDEPENDENT-PROCESS-RUNS` inside this human-readable Markdown file. The rule itself was already present here semantically and was already frozen exactly in the machine-readable JSON contract.

Hotfix 1 keeps the JSON token authoritative for machine checks and validates this document using the stable semantic phrase “same boundary identity exceeds the unchanged maximum ceiling in at least two independent process runs”. No engineering decision, threshold, candidate identity or authority flag changes.
