# M10 Final — VR2 Engineering Repair Planning 1 — RP1B Refinement 4 C3 R1-Seam Worst-Case Localization

**Status:** VALIDATED RETURNED EVIDENCE — Hotfix 1 completed; C3 remained immutable; returned localization found isolated exceedances on different R1 boundaries and does not justify C4  
**Prerequisite:** RP1A, first-generation RP1B, Refinement 1, Refinement 2 and returned Refinement 3 evidence reviewed.  
**Authority:** diagnostic localization/reproducibility only on immutable C3. No C4, RP1C selection, production source change, thermodynamic repair, tolerance change, exact-v9 change, VR3, P3-R1 or second replacement-long execution is authorized.

## 1. Why Refinement 4 exists

Refinement 3 removed the earlier exact-v9 performance concern for C3:

```text
23040 exact-v9 measured Resolve calls
exact-v9 max = 150.5 us
12 targeted exact-v9 states
targeted max = 162.7 us
```

Both are below the frozen RP1A single-call maximum `409.30666666666673 us`.

The strict contract remained RED only because the seam stage observed one R1-side call above the ceiling:

```text
R1-SIDE calls = 5120
median = 7.5 us
p95 = 14.6 us
max = 3667.1 us
calls above ceiling = 1
```

No other seam side exceeded the ceiling. Refinement 3 aggregated timing by side and therefore did not retain the boundary/pass identity of that single call. C4 is not justified until the outlier is localized and its reproducibility is tested.

## 2. Frozen candidate and corpus

C3 remains exactly:

```text
C3-VAPOR-SEAM-COMPLETE-SURROGATE
```

`Rp1bRefinement2ShadowThermodynamicCandidates.cs` is unchanged. No C4 identity exists.

Refinement 4 reuses the immutable RP1A seam corpus and filters only the existing `R1-SIDE` rows:

```text
1280 total seam probes
320 R1-SIDE probes
320 unique seam boundaries
same pressure offset = 1e-5
same quality offset = 1e-6
```

No seam coordinate or threshold is changed.

## 3. R1 screen protocol

The 320 R1 probes are warmed for 16 full passes and measured for 64 full passes with deterministic rotation stride 37:

```text
320 x 64 = 20480 measured screen calls
```

Every screen call records:

- boundary index and boundary temperature;
- pass index and row index;
- elapsed microseconds;
- current-thread allocation delta;
- strict ceiling exceedance flag;
- Gen0/Gen1/Gen2 collection-count delta observed around the timed call.

GC counters are sampled outside the timed region and are attribution context only; they do not alter the ceiling.

## 4. Boundary localization

The screen is reduced to exactly 320 boundary summaries containing median, p95, max, max pass/row identity, exceedance count/fraction, GC-overlap counts and a descriptive screen classification.

Target boundaries are the union of:

- every boundary with any screen exceedance;
- top 12 boundaries by p95;
- top 12 boundaries by max.

The union is deterministic and does not remove historical evidence if a clean rerun fails to re-observe the Refinement-3 spike.

## 5. Targeted reproducibility protocol

Each selected boundary receives:

```text
64 warm-up calls
16 measured blocks
128 calls per block
2048 measured calls per boundary
```

The artifact records target median/p95/max, count of calls above the same frozen ceiling, blocks containing exceedances, max block/call identity and GC-overlap counts.

Possible descriptive target classifications are:

```text
TARGETED-WITHIN-CEILING
TARGETED-ISOLATED-EXCEEDANCE
TARGETED-REPEATED-EXCEEDANCE
TARGETED-PERSISTENT-SLOW-PATH
```

These labels never replace the strict single-call maximum.

## 6. Frozen ceiling and interpretation

The only performance threshold used is still:

```text
409.30666666666673 us
```

Refinement 4 does not change or statistically reinterpret it.

The evidence attribution distinguishes:

```text
R1-SEAM-HISTORICAL-EXCEEDANCE-NOT-OBSERVED-IN-REFINEMENT4
R1-SEAM-SCREEN-EXCEEDANCE-NOT-REPRODUCED
R1-SEAM-TARGETED-ISOLATED-EXCEEDANCE
R1-SEAM-REPEATED-PERFORMANCE-TAIL-CONFIRMED
R1-SEAM-DETERMINISTIC-SLOW-PATH-CONFIRMED
```

A clean Refinement-4 run does **not** erase the historical Refinement-3 `3667.1 us` observation. Conversely, a reproduced exceedance does not automatically authorize C4. Both outcomes require returned-artifact engineering review.

## 7. Outputs

The focused gate writes:

```text
01-contract-and-provenance.txt
02-c3-r1-seam-call-timing.csv
03-c3-r1-boundary-summary.csv
04-c3-r1-target-repeats.csv
05-c3-r1-runtime-context.txt
06-c3-r1-performance-attribution.txt
07-rp1b-refinement4-summary.txt
```

## 8. Execution

From PowerShell:

```powershell
.\scripts\run-m10-final-vr2-engineering-repair-planning1-rp1b-refinement4.cmd
```

Return the complete folder:

```text
.\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-refinement4
```

before RP1C, C4 or any production thermodynamic change.

## Attempt 1 build result and Hotfix 1

The first local execution passed the static prerequisite/contract audit and entered the ordinary Release build, but `NuclearReactorSimulator.Simulation.Tests` stopped before the focused diagnostic because xUnit analyzer rule `xUnit2012` rejected the collection-existence assertion at line 186:

```text
Assert.True(seamLines.Any(predicate))
```

No Refinement 4 timing/localization artifact was produced and C3 was never exercised by the focused gate. Hotfix 1 changes only this test assertion to the analyzer-approved equivalent:

```text
Assert.Contains(seamLines, predicate)
```

The asserted historical Refinement-3 R1 evidence, localization protocol, timing ceilings and candidate identity are unchanged. A full analyzer-oriented scan of the Refinement 4 test found no second `Assert.True(...Any(...))` or `Assert.Single(...Where(...))` pattern. The validator now rejects reintroduction of the xUnit2012-prone form before build.


## Returned Hotfix 1 evidence and engineering review

Hotfix 1 completed the full Refinement 4 localization protocol.

Returned evidence:

```text
screen calls = 20480
screen exceedances = 1
screen max = 2620.6 us
screen owner = boundary 3 at 47.26746165007364 C
screen exceedance GC activity = Gen0 observed

targeted boundaries = 23
targeted calls per boundary = 2048
targeted exceedances = 1
targeted max = 853.5 us
targeted owner = boundary 191 at 270.3515844941138 C
targeted exceedance GC activity = none
```

The screen and targeted exceedance owners are different. Boundary 3 does not reproduce as the targeted exceedance owner; boundary 191 did not exceed during the screen. Both retain ordinary median/p95 timing.

Engineering review therefore records:

```text
C4 justified now = false
RP1C authorized now = false
strict max evidence erased = false
next step = RP1B Performance Measurement Replanning 1
```

Refinement 4 is closed as validated localization evidence.
