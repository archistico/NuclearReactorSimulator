# M10 Final — VR2 Engineering Repair Planning 1 — RP1B C4 Planning 1

**Status:** CANDIDATE REV2 — second pre-execution hardening; planning/audit only; C4 implementation is not contained in this package.  
**Prerequisite:** RP1B Refinement 5 returned evidence is VALIDATED as `C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED`.  
**Authority:** a local planning-audit PASS does **not** authorize C4 implementation. Only return of the complete planning artifact folder followed by returned-evidence adjudication may authorize a separately versioned **test-only C4 implementation/evidence candidate**. It does not authorize RP1C selection, production thermodynamic repair, tolerance changes, exact-v9 changes, VR3, P3-R1 or second replacement-long execution.

## 1. Why C4 is now justified

Refinement 5 executed five fresh .NET test processes over immutable C3 and the frozen 320-boundary R1-side seam corpus. It returned:

```text
5 independent processes
20,480 measured C3 calls per process
102,400 measured calls total
8 calls above the unchanged 409.30666666666673 us ceiling
boundary 3 above ceiling in 5/5 processes
same-boundary-confirmed-count = 1
machine classification = C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED
```

The returned engineering adjudication also established a stronger allocation/runtime correlation:

```text
every measured C3 call = 416 allocated bytes
every process = exactly one Gen0 collection in the measured region
every repeated boundary-3 exceedance = same call as that process's Gen0 collection
every repeated boundary-3 exceedance = measured pass 58
boundary-3 median/p95 remain ordinary outside that pause
```

The frozen same-boundary rule therefore fired correctly, but the evidence does not demonstrate a boundary-3-specific thermodynamic branch. C4 must target the reproduced allocation/managed-runtime mechanism while preserving C3 mathematics and all prior physical/seam evidence.

## 2. Static source attribution before implementation

The current test-only C3 delegates its broad C2 path to `Rp1bExtendedTabulatedReferenceSurrogateCandidate`. In that path, `Rp1bRefinementSaturationTable.TryBuildReachabilityBoundary` currently contains both:

```csharp
var fractions = new List<double>(capacity: 2);
```

and an ordered enumeration of that temporary collection through `OrderBy`.

This is a concrete resolve-time heap-allocation site on the immutable C3/C2 code path and is consistent with the returned uniform per-call allocation. It is **not** yet treated as proof that this source site alone accounts for exactly 416 bytes. That causal claim belongs to the later C4 evidence gate.

The C4 implementation must not special-case boundary 3. Boundary 3 is treated as the deterministic observation point at which the historical Refinement-5 measured loop repeatedly recorded a Gen0 pause; REV2 does not attribute the exact collection position to candidate allocation alone.

## 3. Frozen C4 identity and implementation topology

The planned candidate identity is:

```text
C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE
```

The implementation is test/reference/shadow only. C2/C3/D3 historical code remains logically immutable. C2/C3 source identity is pinned by SHA-256 over UTF-8 text normalized to LF, so real content changes fail while CRLF/LF-only checkout normalization does not create a false RED.

The new source is frozen in advance as:

```text
tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/Reference/Rp1bC4AllocationNeutralShadowThermodynamicCandidate.cs
```

with candidate type:

```text
Rp1bAllocationNeutralVaporSeamCompleteTabulatedSurrogateCandidate
```

The frozen implementation-topology token is:

```text
ALLOCATION-NEUTRAL-C3-VAPOR-PRECEDENCE-C2-MIXTURE-LIQUID-PREFIX-THEN-IMMUTABLE-C2-FALLBACK
```

The `IRp1bShadowThermodynamicCandidate` metadata is also frozen before implementation:

```text
CandidateId = C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE
FamilyId = C-BOUNDED-IF97-DERIVED-TABLE-SURROGATE
InitializationReferencePointCount = _fallback.InitializationReferencePointCount + _denseSaturation.Nodes.Count + _prefixSaturation.Nodes.Count + _prefixLiquidReferencePointCount
MaximumIterativeSolveIterations = 48
UsesDirectIf97AtResolveTime = false
```

The initialization count intentionally reports every reference structure physically materialized by C4: the immutable C2 fallback references, the dense C3-style vapor-seam table, a separate `0.5 K` saturation table for the allocation-neutral C2 prefix, and the separately materialized liquid-table reference points used by that prefix. This prevents an implementation-time metadata choice from being made after performance evidence exists.

### 3.1 REV2 correction: a mixture-only pre-resolver cannot close R1 allocation

The frozen Refinement-2 C3 seam evidence shows that all 320 R1-side timing rows resolve as `SubcooledLiquid`: 310 through `REGION-1-NEAR-BOUNDARY-C2` and 10 through `REGION-1-TABLE-C2`. Therefore a mixture-only pre-resolver would correctly decline those rows and immediately fall through to immutable C2. Immutable C2 would then execute `_saturation.TryResolveMixture(...)` again before reaching its liquid resolver, preserving the very resolve-time allocation C4 is intended to remove. A mixture-only pre-resolver is therefore **structurally insufficient** for the frozen R1 zero-allocation objective.

C4 must not attempt to override or edit `Rp1bRefinementSaturationTable.TryBuildReachabilityBoundary`: that method is private inside the frozen C2 source, and C2 itself is sealed. Instead REV2 freezes this narrow prefix topology:

1. reproduce C3's near-vapor superheated-side discriminator first, with identical arithmetic, comparison ordering and labels, so C2 mixture logic cannot steal C3's historical vapor-seam owner;
2. run a new allocation-neutral reproduction of the **C2 mixture prefix** over a separately built `Rp1bRefinementSaturationTable.Build(0.5d)` node set;
3. if mixture does not resolve, run a new allocation-neutral reproduction of **C2 `TryResolveFromTable` for the liquid rows only**, using the same row construction, interpolation arithmetic, tolerances, region label and ordering but indexed loops rather than interface enumeration;
4. if liquid-table resolution does not succeed, run a new allocation-neutral reproduction of **C2 `TryResolveNearBoundaryLiquid`** with identical arithmetic and output labels;
5. only after all three C2-prefix stages fail, delegate unchanged to a composed `Rp1bExtendedTabulatedReferenceSurrogateCandidate`; C2's vapor table and near-vapor fallback remain historical code and are not cloned;
6. if immutable C2 still fails, reproduce C3's near-vapor saturated-side fallback last.

The duplicated C2 prefix is therefore exactly `MIXTURE + LIQUID-TABLE + NEAR-BOUNDARY-LIQUID`; C2 vapor-resolution logic remains forbidden to clone. For the frozen 320-row R1 timing corpus, **fallback invocation count must be zero**. Resolution-path telemetry is required so this property is demonstrated rather than inferred from allocation totals.

For the reachability boundary itself, C4 must replace the temporary `List<double>` + LINQ ordering behavior with two local fraction slots and explicit presence flags. The two fractions are generated in the same historical order — liquid crossing first, vapor crossing second — and are evaluated in ascending numerical order. A swap occurs only when the second value is strictly less than the first, so equal fractions preserve the stable insertion order produced by historical `OrderBy`.

A second allocation risk is also frozen by REV2: historical `TryResolveFromTable` receives an `IReadOnlyList<SurrogateTemperatureRow>` and enumerates it with `foreach`. The exact contribution of that enumeration to the observed 416 B/call is not claimed as proven, but C4 must not reproduce that risk. All C4 prefix scans use indexed `for`/`while` access only; no `foreach` of any collection type, LINQ, temporary collection, boxing or per-call array/list creation is allowed on the R1 timed resolve path. This avoids depending on enumerator implementation details for the zero-allocation claim.

The following are forbidden and force replanning rather than implementation widening:

- editing C2, C3 or D3 historical source;
- copying the complete C2 resolver into C4;
- changing table step sizes, root tolerances, bisection count, interpolation expressions, phase/region strings or fallback ordering;
- introducing a boundary-3 branch;
- direct IF97 calls during `TryResolve`;
- an input-keyed cache or production dependency.

## 4. Mandatory semantic-equivalence gate before performance interpretation

C4 performance evidence is interpretable only after exact C3 semantic equivalence is demonstrated over the complete frozen RP1A/RP1B evidence domains:

```text
39 inverse-applicable VR2 reference rows
360 frozen exact-v9 node rows
1,280 frozen seam probes
288 frozen hydraulic-context rows
```

For the 1,679 thermodynamic-state observations, the gate compares C3 and C4 as follows:

- resolved/unresolved status: exact Boolean equality;
- region and phase: ordinal string equality;
- temperature and pressure: `BitConverter.DoubleToInt64Bits` equality;
- nullable vapor quality: exact presence equality, then `BitConverter.DoubleToInt64Bits` equality when present;
- no new C4 output-region or output-phase labels are permitted.

For the 288 hydraulic-context observations, the gate compares derived C3/C4 resolved status, driving-pressure bits, flow bits and sign-change-from-production status. C4 must repeat every state and hydraulic semantic observation at least twice and reproduce the same bit patterns on each repeat.

Direct `double ==`, tolerance comparisons or xUnit floating overloads are not sufficient for the bit-equivalence claim because `+0.0/-0.0` and NaN payloads can otherwise be normalized by equality semantics.

Any semantic mismatch classifies C4 as `C4-SEMANTIC-EQUIVALENCE-FAILED` and blocks performance-based qualification regardless of speed.

## 5. Allocation-causality evidence contract

The later C4 gate must measure current-thread allocation outside the timed interval and must make the **entire measured loop allocation-neutral apart from the candidate call itself**, not merely the interval between `beforeBytes` and `afterBytes`.

REV2 records a limitation of the historical Refinement-5 harness without changing its validated result: the `416 B/call` figure remains valid because it was measured strictly around `candidate.TryResolve`, and the Gen0 coincidence remains valid, but the old harness created the sample list after the full-GC baseline and allocated a heap `R1CallTimingSample` record after each measured call. Therefore `candidate allocation alone caused the exact Gen0 position` is **not proven**. The observed tail remains GC-correlated and the frozen C4 trigger remains valid.

The C4 harness must preallocate all timing storage **before** the post-warm-up full GC, use value-type/array storage, and perform no `List.Add`, `new` reference-type sample creation, LINQ, string formatting, artifact writing or other current-thread heap allocation from the post-GC measured-region baseline until the last measured call has completed. It must record both the whole-region current-thread allocation delta and the sum of per-call candidate allocation deltas; their difference must be exactly zero or the evidence is `C4-EVIDENCE-INCOMPLETE`.

The timing campaign measures C4 on the frozen 320-boundary R1-side seam corpus after warm-up; Refinement 5 remains the frozen C3 allocation control and is referenced from provenance rather than rerun inside C4 processes. The primary allocation closure requirement is:

```text
C4 measured resolve allocation = 0 bytes per call
```

for every measured R1-side seam call across all ten timing lane/run processes.

The frozen C3 evidence remains the historical control:

```text
C3 measured resolve allocation = 416 bytes per call in Refinement 5
```

Refinement 5 is the frozen C3 allocation control. No additional C3 control may execute inside the dedicated semantic process or any of the ten C4 timing processes, because doing so would contaminate the runtime/allocation state being qualified and would make the frozen 11-process protocol ambiguous.

If C4 remains nonzero-allocation on any measured R1 call, the engineering classification is `C4-ALLOCATION-NOT-CLOSED` even if wall-clock measurements happen to be clean.

## 6. Cross-process wall-clock protocol for C4

After the dedicated semantic-equivalence process completes, C4 is measured through **two lanes, each with five fresh runtime processes**. Those ten timing processes collect both allocation-closure and wall-clock evidence; allocation closure is therefore adjudicated from the same timed calls rather than assumed before them. Lane A and Lane B must never execute sequentially in the same test process; otherwise Lane B would inherit GC/tiered-runtime state from Lane A and would not be an independent decorrelation check.

Frozen shape:

```text
resolve max ceiling = 409.30666666666673 us
logical runs per lane = 5
fresh process per lane/run = True
cross-process timing invocations = 10
semantic-equivalence focused invocation = 1
total focused process invocations = 11
warm-up passes per lane/run = 16
measured passes per lane/run = 64
R1 boundaries per pass = 320
measured calls per lane/run = 20,480
total measured C4 calls = 204,800
```

Before the measured region, each fresh lane/run must instantiate its candidate and preallocate all value-type timing storage, complete all 16 warm-up passes, then perform the same explicit full-GC preparation used by the historical harness before taking allocation/GC baselines. The measured-pass index then restarts at `0`: warm-up uses pass indices `0..15`, measured timing uses pass indices `0..63`, matching Refinement 5 rather than continuing at pass 16.


### 6.0 Process isolation and runner ownership

Semantic equivalence is collected in **one dedicated focused process that never supplies wall-clock evidence**. The wall-clock campaign then uses the ten fresh timing processes described below. Thus the future runner performs 11 focused `dotnet test` invocations in total: 1 semantic process + 10 timing processes. The ordinary Release/CI gate is separate and is not counted in that number.

The runner is the single owner of artifact-root reset. It deletes `artifacts/m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-c4` exactly once before semantic/timing evidence begins. The semantic process may replace only the semantic aggregate files it owns. A lane/run process may delete/recreate **only its own** `lane-a|lane-b/process-XX` directory. No focused test process may delete the root or another process directory. This prevents later fresh-process runs from destroying earlier evidence. Aggregate ownership is frozen as well: the semantic process owns only `02-state-semantic-equivalence.csv`, `03-hydraulic-semantic-equivalence.csv` and `04-semantic-equivalence-summary.txt`; the runner/adjudicator owns `01-contract-and-provenance.txt` and aggregate files `05` through `09`. No process may rewrite another owner's aggregate evidence.

### Lane A — historical-order reproduction

Lane A reproduces Refinement 5 exactly:

```text
startIndex(pass) = (pass * 37) % 320
rowIndex(offset) = (startIndex + offset) % 320
```

All five Lane-A processes use this same historical ordering so the new candidate can be compared directly with Refinement 5 geometry.

### Lane B — pre-frozen decorrelated deterministic permutations

Lane B uses an affine permutation over the same 320 rows. For run `r` and measured/warm-up pass `p`:

```text
start = (baseOffset[r] + p * passAdvance[r]) % 320
rowIndex(offset) = (start + offset * permutationStride[r]) % 320
```

The exact parameters are frozen **before C4 exists**:

| Run | baseOffset | passAdvance | permutationStride |
| ---: | ---: | ---: | ---: |
| 1 | 11 | 41 | 73 |
| 2 | 47 | 53 | 107 |
| 3 | 83 | 61 | 149 |
| 4 | 119 | 71 | 181 |
| 5 | 157 | 83 | 213 |

Every `permutationStride` is coprime with 320, so every pass visits all 320 boundary identities exactly once. The runner/validator must verify this property before timing starts rather than infer it from returned performance data.

Resolution-path telemetry inside the measured region must be stored as a **value-type enum/code**, not created as a per-call string. Human-readable `resolution_path` text is rendered only when the preallocated samples are written after timing. This prevents the telemetry intended to prove zero fallback from becoming a new allocation source.

Each lane/run must record:

- lane and logical run index;
- boundary/pass/row identity;
- elapsed wall-clock microseconds;
- current-thread allocated bytes;
- Gen0/Gen1/Gen2 collection-count deltas sampled outside the timed call;
- lane/run aggregate median, p95 and maximum;
- lane/run maximum owner boundary;
- per-boundary median, p95, max and exceedance count.

The statistics algorithm is frozen as part of the contract, not left to the implementation. Elapsed microseconds use `ticks * 1_000_000d / Stopwatch.Frequency`. Median sorts ascending and, for an even sample count, averages the two middle values. p95 uses the same nearest-rank helper as Refinement 5: sort ascending, compute `ceil(0.95 * N) - 1` as a zero-based index, then clamp to the available range. A max exceedance means strictly `elapsed_us > ceiling`; median/p95 qualification uses `<=`. All parsing/formatting used for evidence calculations is invariant-culture. No interpolation-based percentile implementation may replace this rule.

Qualification uses the absolute RP1A ceilings on **each of the ten lane/run distributions separately**. A combined-lane or all-process aggregate may be reported for context but may not hide a failing individual lane/run. The strict maximum remains a per-call condition over all 204,800 measured C4 calls. The C4-specific allocation closure is likewise required on every one of those 204,800 calls; a single nonzero measured allocation is negative allocation evidence.

### 6.1 Frozen future evidence artifact contract

The future implementation/evidence gate identity is frozen now, before C4 exists. Both focused test methods are xUnit-v3 explicit tests and cannot enter the ordinary suite. The runner uses `setlocal`, keeps all C4 opt-in/lane/run variables unset through `eng\ci-ordinary.cmd`, then sets the C4 opt-in only for focused work. The semantic process runs with lane/run variables still unset; each timing process requires both lane and run-index variables. Every focused invocation uses `--explicit only`, the exact method filter, `--parallel none` and `--no-build`. The runner sequence is also frozen: static contract preflight -> ordinary Release gate with C4 opt-in unset -> one semantic focused process -> five Lane-A fresh timing processes -> five Lane-B fresh timing processes -> adjudication/evidence-integrity pass. Negative semantic/allocation/performance outcomes return process success after complete evidence; only harness/evidence-integrity defects return a focused-test failure. In particular, a semantic mismatch is an engineering result and **must not short-circuit the ten timing processes**: the runner still completes the full 59-file evidence tree, after which classification precedence makes `C4-SEMANTIC-EQUIVALENCE-FAILED` authoritative.

The gate identity is:

```text
test project = tests/NuclearReactorSimulator.Simulation.Tests/NuclearReactorSimulator.Simulation.Tests.csproj
test file = tests/NuclearReactorSimulator.Simulation.Tests/Physics/Fluids/M10FinalVr2EngineeringRepairPlanning1Rp1bC4Tests.cs
test class = M10FinalVr2EngineeringRepairPlanning1Rp1bC4Tests
semantic method = Rp1bC4_EstablishesBitEquivalentSemanticsAcrossFrozenCorpus
timing method = Rp1bC4_MeasuresOneIndependentR1LaneRun
both methods = [Fact(Explicit = true)]
focused invocation = dotnet test --project <test-project> --configuration Release --no-build -- --explicit only --filter-method <fully-qualified-method> --parallel none
runner = scripts/run-m10-final-vr2-engineering-repair-planning1-rp1b-c4.cmd
artifact root = artifacts/m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-c4
opt-in env = NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4
lane env = NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4_LANE
run-index env = NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4_RUN_INDEX
lane values = A | B
```

The implementation/evidence gate must write exactly these aggregate artifacts at that root:

```text
01-contract-and-provenance.txt
02-state-semantic-equivalence.csv
03-hydraulic-semantic-equivalence.csv
04-semantic-equivalence-summary.txt
05-allocation-closure-summary.txt
06-cross-process-run-summary.csv
07-cross-process-boundary-summary.csv
08-c4-evidence-adjudication.txt
09-rp1b-c4-summary.txt
```

Semantic evidence is deliberately split by data type. `02-state-semantic-equivalence.csv` contains exactly **1,679 thermodynamic state observations**: 39 inverse-applicable VR2 rows + 360 exact-v9 node rows + 1,280 seam rows. The RP1A VR2 source contains 40 rows, but `VR2-SAT-360C-PONLY` is boundary-only because its inverse inputs are non-finite/non-applicable; it is therefore not silently dropped but explicitly excluded by the same applicability rule used by Refinement 2.

`03-hydraulic-semantic-equivalence.csv` contains exactly **288 derived hydraulic observations**. These are not assigned fictitious region/phase fields: C3 and C4 use their bit-exact node-pressure results to reproduce the frozen hydraulic law, then compare resolved status, driving-pressure bits, flow bits and production-sign-change status. Total semantic comparisons remain **exactly 1,967 observations = 1,679 + 288**.

The cross-process evidence contains exactly ten subdirectories:

```text
lane-a/process-01 ... lane-a/process-05
lane-b/process-01 ... lane-b/process-05
```

Every lane/process directory contains exactly:

```text
01-process-contract.txt
02-c4-r1-call-timing.csv
03-c4-r1-boundary-summary.csv
04-runtime-context.txt
05-process-summary.txt
```

The complete future evidence tree therefore contains exactly 59 required files: 9 aggregate files plus 10 process directories x 5 files. Each call-timing CSV contains 20,480 measured rows and each boundary summary contains 320 rows. Missing, duplicated or extra required evidence is `C4-EVIDENCE-INCOMPLETE`; artifact naming or counts are not allowed to drift after timing results are known.

## 7. Performance qualification remains strict, but negative evidence must not become a harness RED

No threshold is changed by C4 Planning 1. The frozen RP1A ceilings remain:

```text
Resolve median <= 94.8 us
Resolve p95    <= 158.80666666666667 us
Resolve max    <= 409.30666666666673 us
median allocation <= 2816 B
```

C4 adds the stricter candidate-specific zero-byte R1 allocation requirement because removing the reproduced transient allocation is the purpose of this candidate. This does not widen any historical ceiling.

The future focused evidence test must fail only for harness/evidence-integrity defects: missing corpus rows, non-finite timing data, missing artifacts, malformed lane coverage, semantic-comparison infrastructure failure or similar inability to complete the authored measurement. A measured allocation, semantic mismatch or wall-clock exceedance is **valid negative engineering evidence** and must be written to artifacts and adjudicated into the appropriate C4 classification rather than converted into an xUnit failure after evidence generation.

Consequently, neither semantic mismatch nor a C4 `TryResolve` return value of `false` on a frozen observation may be implemented as an xUnit assertion failure. The semantic process records the mismatch and continues. Warm-up and measured timing likewise record unresolved C4 calls and continue so that all ten timing processes can complete. An unexpected exception from the candidate remains an evidence-integrity failure because the authored observation cannot be completed.

This preserves the existing project rule: evidence completion PASS and engineering qualification are separate decisions.

## 8. Frozen later adjudication classes

The C4 evidence gate must end in exactly one engineering evidence classification:

```text
C4-QUALIFIED-ALLOCATION-TAIL-CLOSED
C4-SEMANTIC-EQUIVALENCE-FAILED
C4-ALLOCATION-NOT-CLOSED
C4-WALL-CLOCK-TAIL-REMAINS
C4-EVIDENCE-INCOMPLETE
```

`C4-QUALIFIED-ALLOCATION-TAIL-CLOSED` requires all of the following:

- complete C3/C4 semantic bit-equivalence over the frozen domains;
- zero C4 measured allocation on every one of the 204,800 cross-process R1 calls;
- zero immutable-C2 fallback invocations on the 320-row R1 timing path in every lane/run;
- zero measured-region harness allocation after subtracting the sum of candidate-call deltas;
- no C4 single-call exceedance of `409.30666666666673 us` in either five-process lane;
- median and p95 within the unchanged RP1A ceilings;
- complete deterministic evidence with no missing boundary/lane/run rows.

Classification precedence is fail-closed:

1. incomplete/malformed evidence -> `C4-EVIDENCE-INCOMPLETE`;
2. any semantic mismatch -> `C4-SEMANTIC-EQUIVALENCE-FAILED`;
3. semantic PASS but any nonzero R1 allocation -> `C4-ALLOCATION-NOT-CLOSED`;
4. semantic/allocation PASS but any wall-clock ceiling failure -> `C4-WALL-CLOCK-TAIL-REMAINS`;
5. otherwise -> `C4-QUALIFIED-ALLOCATION-TAIL-CLOSED`.

No classification authorizes RP1C automatically. Returned C4 artifacts require a separate engineering adjudication before RP1C selection can be planned.

## 9. Pre-execution hardening findings closed by REV1 and REV2

REV1 closed the first set of planning ambiguities. REV2 adds the deeper feasibility and harness corrections found by the second pre-execution review:

- frozen Refinement-2 evidence proves the R1 timing corpus is 320/320 `SubcooledLiquid` (310 near-boundary + 10 table), so the former mixture-only pre-resolver topology is rejected as structurally unable to bypass C2 allocation on R1;
- C4 now duplicates only the exact C2 prefix needed for R1 allocation closure: mixture + liquid table + near-boundary liquid; the vapor half of C2 remains immutable fallback code;
- the historical R5 sample recorder is recognized as a whole-region allocation confounder while preserving the validity of the per-call 416-byte measurement and GC correlation; C4 therefore requires allocation-neutral preallocated value-type timing storage;
- one dedicated semantic focused process is separated from the ten fresh timing processes, for 11 focused invocations total;
- warm-up and measured pass indices explicitly restart exactly as in Refinement 5;
- the runner owns the artifact-root reset once, while each process may reset only its own directory;
- the full `IRp1bShadowThermodynamicCandidate` metadata contract is frozen before implementation, including the initialization-reference count formula;
- the future C4 test/runner identity, artifact root, lane/run environment tokens and exact 59-file evidence-tree shape are frozen before implementation;
- the private/sealed C2 topology no longer leaves an impossible "replace the private method without editing C2" instruction;
- C4 is constrained to the narrow C3-precedence + C2 `MIXTURE + LIQUID-TABLE + NEAR-BOUNDARY-LIQUID` allocation-neutral prefix followed by immutable C2 fallback; C2 vapor logic is not cloned;
- Lane A and Lane B are separated into ten fresh process invocations so runtime state cannot leak from one lane to the other;
- Lane B's exact permutations are frozen now rather than chosen after C4 timing exists;
- bit-equivalence has an exact machine comparison method rather than relying on generic floating equality;
- negative performance/allocation results are explicitly engineering evidence, not reasons for the focused evidence-generation test to fail;
- C2 and C3 historical files are SHA-256 pinned in the contract and static audit.

## 10. Explicit non-goals

C4 Planning 1 does not authorize or propose:

- changing C3/C2/D3 historical source;
- special-casing boundary 3;
- changing IF97 reference data or seam coordinates;
- changing the 10% RP1C planning target or 25% VR2 blocking threshold;
- widening/deleting the strict single-call maximum;
- changing exact-v9 or production closure semantics;
- adding a production closure mode;
- executing VR3 or P3-R1;
- authorizing a second replacement-long baseline.

## 11. Exit from this planning gate

A returned static planning audit PASS means only:

```text
C4 design/evidence contract = FROZEN
C4 separate test-only implementation candidate = AUTHORIZED NEXT
C3/C2/D3 historical source = IMMUTABLE
RP1C = NOT AUTHORIZED
production repair = NOT AUTHORIZED
performance thresholds = UNCHANGED
exact-v9 = IMMUTABLE
VR3 / P3-R1 / second replacement-long = NOT AUTHORIZED
```

Return the complete planning-audit artifact folder before implementing C4.
