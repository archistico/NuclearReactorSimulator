# M10 Final — VR2 Engineering Repair Planning 1 — RP1B C4 Test-Only Implementation 1

**Status:** CANDIDATE — implementation/evidence gate authorized only by the returned C4 Planning 1 REV2 adjudication.  
**Scope:** test/reference/shadow code and evidence generation only.  
**Not authorized:** RP1C selection, production thermodynamic repair, threshold/tolerance change, exact-v9 change, VR3, P3-R1 or second replacement-long execution.

## 1. Authority and immutable prerequisites

C4 Planning 1 REV2 returned `PASS-AS-AUTHORED`, and its returned artifact set was separately adjudicated `PASS`. That adjudication authorizes only the separately versioned test-only C4 implementation defined by the REV2 contract.

The following historical sources remain immutable and are independently pinned by normalized-LF SHA-256 in the C4 validator:

- `Rp1bRefinement1ShadowThermodynamicCandidates.cs` — C2;
- `Rp1bRefinement2ShadowThermodynamicCandidates.cs` — C3/D3;
- `M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement5Tests.cs` — historical R5 timing harness.

The complete four-file returned C4 Planning 1 artifact set is frozen under `eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_C4_Planning1_Artifacts/`. The user-returned adjudication decision is stored separately so the planning artifacts themselves are not rewritten.

## 2. C4 implementation boundary

The new test-only candidate is:

```text
C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE
```

Source:

```text
tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/Rp1bC4AllocationNeutralShadowThermodynamicCandidate.cs
```

The implementation follows the frozen topology exactly:

```text
ALLOCATION-NEUTRAL-C3-VAPOR-PRECEDENCE-C2-MIXTURE-LIQUID-PREFIX-THEN-IMMUTABLE-C2-FALLBACK
```

The resolve order is:

1. C3 near-vapor superheated-side discriminator;
2. allocation-neutral reproduction of the C2 mixture prefix;
3. allocation-neutral reproduction of the C2 liquid table prefix;
4. allocation-neutral reproduction of C2 near-boundary liquid;
5. immutable C2 fallback for remaining paths only;
6. C3 saturated-vapor-side fallback.

Only `MIXTURE + LIQUID-TABLE + NEAR-BOUNDARY-LIQUID` are duplicated. C2 vapor table/near-vapor logic is not cloned. There is no boundary-3 special case and no direct IF97 call from `TryResolve`.

The allocation-neutral reachability boundary replaces the historical per-call temporary `List<double>` plus LINQ ordering with two local fraction slots while preserving historical crossing generation and stable ascending evaluation order. Resolve-time prefix scans use indexed loops only. Initialization may allocate and may use LINQ because initialization is outside the measured resolve path.

## 3. Semantic gate

The explicit method

```text
Rp1bC4_EstablishesBitEquivalentSemanticsAcrossFrozenCorpus
```

runs in one dedicated process with lane/run environment variables unset. It evaluates C4 against the frozen C3 Refinement-2 evidence rather than running a fresh C3 timing/control path.

The state corpus is exactly:

```text
39 VR2 inverse-applicable rows
360 exact-v9 node rows
1280 seam rows
= 1679 thermodynamic state observations
```

The derived hydraulic corpus is exactly 288 observations, producing 1,967 total semantic comparisons.

Resolved state, region, phase, nullable quality presence and all doubles are compared bit-for-bit. Doubles use `BitConverter.DoubleToInt64Bits`. Every C4 state is evaluated twice and deterministic repeat is part of the evidence. Hydraulic driving pressure, flow and sign-change semantics are derived from the two corresponding C4 exact-v9 state repeats and compared bit-for-bit with the frozen C3 hydraulic replay.

A semantic mismatch is a negative engineering result, not an xUnit failure. The semantic process still writes all three owned aggregate artifacts.

## 4. Allocation and wall-clock gate

The explicit method

```text
Rp1bC4_MeasuresOneIndependentR1LaneRun
```

runs ten times in ten fresh test processes:

```text
Lane A: 5 runs, historical stride-37 order
Lane B: 5 runs, pre-frozen affine permutations
```

Each run performs 16 warm-up passes and 64 measured passes over 320 R1 boundaries, for 20,480 calls/run and 204,800 measured calls total.

Before the full GC, the timing process also primes the measurement primitives (`Stopwatch`, current-thread allocation counters, GC counters, phase coding and the value-type timing sample constructor) outside the measured region, so first-use/JIT setup cannot be misclassified as candidate or harness allocation. The measured sample array is a value-type array allocated before the post-warm-up full GC. No `List.Add`, record-class allocation, LINQ, string formatting or collection enumeration occurs in the measured recorder. Candidate allocation is measured tightly around every `TryResolveWithPath` call; whole-region current-thread allocation is also measured and reconciled against the sum of candidate-call deltas.

Required C4 closure remains:

```text
allocated bytes per measured R1 call = 0
whole measured-region harness allocation = 0
immutable-C2 fallback calls on R1 = 0
median <= 94.8 us in every lane/run
p95 <= 158.80666666666667 us in every lane/run
max <= 409.30666666666673 us for every measured call
```

Timing unresolved states, nonzero allocation or wall-clock exceedances are recorded as engineering-negative evidence; they do not deliberately turn the focused test RED. Unexpected exceptions or incomplete/corrupt evidence remain harness failures.

## 5. Evidence tree and classification

The runner owns a single root reset before the focused processes. The semantic process owns files 02–04. Each timing process may delete/write only its own `lane-*/process-XX` directory. The PowerShell adjudicator owns aggregate files 01 and 05–09.

The complete evidence tree contains exactly 59 files:

```text
9 aggregate files
+ 10 process directories * 5 files
= 59 files
```

Classification precedence is frozen as:

```text
C4-EVIDENCE-INCOMPLETE
C4-SEMANTIC-EQUIVALENCE-FAILED
C4-ALLOCATION-NOT-CLOSED
C4-WALL-CLOCK-TAIL-REMAINS
C4-QUALIFIED-ALLOCATION-TAIL-CLOSED
```

No classification automatically authorizes RP1C.

## 6. Execution

From the repository root in PowerShell:

```powershell
.\scripts\run-m10-final-vr2-engineering-repair-planning1-rp1b-c4.cmd
```

The runner performs:

1. static returned-planning/source/implementation contract audit;
2. ordinary Release gate with all C4 environment variables unset;
3. one semantic focused process;
4. five fresh Lane-A timing processes plus five fresh Lane-B timing processes;
5. aggregate evidence integrity and engineering adjudication.

Return the complete folder:

```text
.\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-c4
```

before any RP1C planning, selection or production/runtime change.

## 7. Final pre-execution review

The conclusive static preflight is `PASS`. The four returned C4 Planning 1 REV2 artifacts frozen in this candidate are byte-for-byte identical to the user-returned files. The final review also corrected one pre-execution validator defect before packaging: `future-required-file-count=59` is asserted against returned `01-contract-and-provenance.txt`, where the frozen marker actually resides, rather than against `03-c4-planning-summary.txt`.

The preflight confirms:

```text
C4 candidate normalized-LF SHA-256 = 6D4C8EDD53906ACD7B13C004B05A24890D8736FB8469DB20E5AACE1E307645D6
C4 tests normalized-LF SHA-256     = 1DE6B1F977A4307FFB06A5FA3CBB5B942FDA849B633425DDFCB0061C1806C24C
returned planning artifacts        = 4/4 byte-exact
src                                = 959/959 byte-identical to Planning 1 REV2
historical tests changed           = 0
new test-only files                = 2
focused process contract           = 1 semantic + 5 Lane A + 5 Lane B = 11
future evidence contract           = 9 aggregate + (10 * 5 process files) = 59
```

The candidate resolve-time sections and measured timing recorder contain no `List` creation, LINQ ordering/projection, `foreach`, direct IF97 calls, string formatting or reference-type sample allocation. Initialization remains intentionally outside the measured region.

A real .NET 10 build cannot be performed in the packaging environment because the required SDK is not installed there and the environment has no network access. Therefore the authoritative compile, ordinary-suite and focused evidence decision remains the local Windows runner execution described above.

## 8. Returned execution and engineering adjudication

The complete returned 59-file C4 artifact tree has now been independently reviewed and accepted.

Returned classification:

```text
C4-QUALIFIED-ALLOCATION-TAIL-CLOSED
```

The returned evidence confirms 1,967/1,967 semantic comparisons bit-equivalent to frozen C3, 204,800/204,800 timing calls resolved, zero candidate and harness allocation, zero immutable-C2 fallback calls, zero GC activity in measured regions and zero strict-ceiling exceedances.

The worst returned call is 329.9 us against the unchanged 409.30666666666673 us strict maximum ceiling. All ten independent lane/run distributions satisfy the frozen median and p95 ceilings.

The authoritative engineering review is:

`M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_C4_RETURNED_EVIDENCE_ADJUDICATION.md`

This closes C4 Test-Only Implementation 1 as qualified returned evidence. It authorizes only a separately versioned RP1C planning gate. RP1C selection and every production/runtime change remain unauthorized.

