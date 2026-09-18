# M10 FINAL VR2 RP1B REFINEMENTS DOSSIER
> Historical consolidation dossier. The source documents below were completed/superseded and had no executable references at consolidation time. Their content is retained here for provenance while the individual top-level files are removed.
## Source manifest
| Original top-level file | Lines | SHA-256 (normalized LF UTF-8) |
| --- | ---: | --- |
| `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_C4_PLANNING1_REV2_PREEXECUTION_REVIEW.md` | 89 | `0DE4856BD3E6FB5EF1AF355DDCCCB08E7FE59057723A3C05982CD0213DD4E7D8` |
| `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_C4_TEST_ONLY_IMPLEMENTATION1.md` | 197 | `0959E1D1612E5160D913784AA72586298FB2BCA30A4ADF41AB1D31E02F43DB70` |
| `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT1_C2_D2.md` | 178 | `0FA0517B43EA0AEBDB4553898ACB49FE3FC50F98EDC7811C1AE24BB3B58C03B6` |
| `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT1_HOTFIX1_PREEXECUTION_REVIEW.md` | 97 | `AE961F5CF06FA7244FDF58281B5306DCA5281C48669344CE18D21EA75E21118B` |
| `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT2_C3_D3.md` | 126 | `1327B77880ABAC2C2232E519B5D986BAC51D540C69F442D1C67612BB50E33C09` |
| `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT2_PREEXECUTION_REVIEW.md` | 49 | `13E7D3583C0BE93C810503864CBEA1F40B38C633866636C86002F8636DFDD694` |
| `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT3_C3_PERFORMANCE_TAIL.md` | 182 | `60BEFD4F8581A7057786B2663C45C6B1A532B2A5E6B0E04E3565CDF4734F7967` |
| `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT3_PREEXECUTION_REVIEW.md` | 118 | `62F28BB2579B28C86293400958244EC66092F3A4DA0FD8889E1EA7A83BFA83E7` |
| `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT4_C3_R1_SEAM_LOCALIZATION.md` | 210 | `55A248D845207A6662C17A57866B99BBE7E35FBC734610ADB2B0AF40B83CDA0C` |
| `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT4_PREEXECUTION_REVIEW.md` | 40 | `2A980EE6315531160848AC1699BC4A4D99BA016F0EFC9871229D031C724EE740` |
| `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT5_PREEXECUTION_REVIEW.md` | 57 | `AE80204601578962A11C9591530AB10461F966959A853971F2BBC189AFEAACF8` |
| `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_TEST_ONLY_SHADOW_CANDIDATE_MATRIX.md` | 291 | `68E53D442DC9DCEE771268E6C0670BA39579941178588F0B19709091BCAECF39` |

## Retained source snapshots

---

## Source snapshot — `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_C4_PLANNING1_REV2_PREEXECUTION_REVIEW.md`

### M10 Final — VR2 Engineering Repair Planning 1 — RP1B C4 Planning 1 REV2 — Second Pre-Execution Review

**Status:** CANDIDATE REVIEW COMPLETE / STATIC PRE-EXECUTION HARDENING.  
**Scope:** planning, source-attribution and future evidence-contract review only. No C4 implementation is contained or authorized by this document.

#### 1. Review result

The REV1 planning runner/validator was mechanically strong enough to avoid the known marker, PowerShell-double, Unicode, path-separator and generated-directory RED classes, but the second review found two deeper engineering defects in the authored future C4 plan plus three process-contract ambiguities. REV2 closes them before the planning audit is executed.

#### 2. Blocking finding A — mixture-only topology was structurally unable to meet 0 B/call on R1

Frozen Refinement-2 C3 evidence contains exactly 320 C3 R1-side rows. All 320 resolve as `SubcooledLiquid`:

- 310 resolve as `REGION-1-NEAR-BOUNDARY-C2`;
- 10 resolve as `REGION-1-TABLE-C2`;
- 0 resolve as Region 4 on the C3 R1 timing path.

Therefore the REV1 mixture-only pre-resolver would correctly decline every R1 state, then enter immutable C2. Immutable C2 always invokes `_saturation.TryResolveMixture(...)` before the liquid table/near-boundary paths, preserving the allocation C4 is meant to remove. The REV1 zero-byte objective was therefore not implementable under its own topology.

REV2 replaces that topology with a frozen C3-precedence/C2-prefix design: C3 superheated-vapor discriminator first; allocation-neutral C2 mixture; allocation-neutral C2 liquid table; allocation-neutral C2 near-boundary liquid; immutable C2 fallback only for remaining paths; C3 saturated-vapor fallback last. C2 vapor resolution is not cloned. R1 timing requires zero fallback invocations.

#### 3. Blocking finding B — historical R5 whole-region harness was not allocation-neutral

Refinement 5 measured candidate allocation correctly around each `TryResolve` call, so the returned `416 B/call` remains valid. It also correctly recorded that the repeated boundary-3 exceedance coincided with a Gen0 collection inside that candidate-call window.

However, after the post-warm-up full-GC baseline, R5 also:

- allocated the `List<R1CallTimingSample>` backing storage;
- allocated a reference-type `R1CallTimingSample` after every measured call.

Consequently the returned evidence supports **GC correlation**, but it does not prove that C3's 416 bytes alone determined the precise Gen0 cadence. REV2 does not rewrite or weaken the validated `C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED` machine classification; it narrows the causal claim used to design C4.

The future C4 timing harness must preallocate value-type storage before the post-warm-up full GC and remain current-thread allocation-neutral throughout the complete measured region apart from the candidate call itself. Whole-region allocation minus the sum of candidate-call deltas must equal zero.

#### 4. Additional source risk — interface enumeration

`Rp1bRefinementSaturationTable.TryBuildReachabilityBoundary` remains a concrete allocation site because it creates a temporary `List<double>` and applies LINQ ordering. The second review also identifies `TryResolveFromTable(IReadOnlyList<...>)` using `foreach` as an additional allocation risk through interface enumeration. REV2 does not assert an exact byte contribution for either source site. The C4 R1 prefix simply forbids both risk shapes by using indexed loops and no resolve-time temporary collections/LINQ/boxing.

#### 5. Process-contract corrections

REV2 freezes these additional rules before implementation:

1. semantic equivalence runs in one dedicated focused process and never contributes timing evidence;
2. wall-clock/allocation timing uses ten additional fresh processes, giving 11 focused `dotnet test` invocations total;
3. warm-up pass indices are `0..15`; measured pass indices restart at `0..63`, exactly matching Refinement 5;
4. the runner deletes the C4 artifact root once; a focused test process can reset only the files/directory it owns;
5. the future measured-loop recorder uses preallocated value-type storage and records resolution-path telemetry, with zero immutable-C2 fallback calls required on R1.

#### 6. Non-findings / checks that remain green

The review re-confirms:

- C2 and C3 SHA-256 pins match the frozen sources;
- `src/` and `tests/` remain byte-identical to the validated Refinement-5 Handoff 2 package;
- no C4 implementation or future C4 focused test/runner exists in this planning package;
- the planning validator remains ASCII-only and Windows PowerShell 5.1 compatible by construction;
- float contract values are checked with explicit tolerance rather than textual or direct object inequality;
- Lane-B strides are coprime with 320 and cover all 320 identities for each warm-up and measured pass;
- the future required evidence tree is corrected to 59 files: 9 aggregate + 10 process directories × 5 files, because state and derived-hydraulic semantic evidence use distinct schemas;
- RP1C, production repair, threshold/tolerance changes, exact-v9, VR3, P3-R1 and second replacement-long remain unauthorized.

#### 7. Exit condition

Execute only the REV2 planning runner. A local `PASS-AS-AUTHORED` freezes only the candidate planning evidence. Return the complete planning artifact folder for adjudication; only that returned-evidence adjudication may authorize a separately versioned **test-only C4 implementation/evidence candidate**. It does not authorize RP1C or production changes.
#### Additional hardening from the second pass

The second pass also removed four procedural false-RED risks before execution:

- historical source/test SHA-256 pins are evaluated over UTF-8 text with line endings normalized to LF, so `core.autocrlf` or CRLF/LF checkout differences cannot invalidate an otherwise identical C2/C3/R5 source;
- Refinement 5 remains the only C3 allocation control; no extra C3 control may run inside the semantic or timing processes;
- semantic mismatch cannot short-circuit timing: negative engineering outcomes still produce the complete 59-file tree before classification;
- measured resolution-path telemetry is stored as a value-type code and formatted to text only after timing, so the fallback proof cannot itself allocate inside the measurement region.
- semantic and timing evidence are frozen as two different xUnit-v3 explicit methods, with exact method filters, `--parallel none` and `--no-build`; the ordinary gate executes first with C4 environment variables unset.

Aggregate evidence ownership is also explicit: semantic process -> files 02-04; runner/adjudicator -> file 01 and files 05-09; timing processes -> only their own lane/process directory.
#### Semantic-schema correction

A further schema review found that the 288 hydraulic-context observations are derived path results, not thermodynamic states. Treating them with the same `region/phase/temperature/pressure` CSV schema would have created an implementation-time mismatch. REV2 therefore freezes two semantic CSVs: 1,679 state observations and 288 hydraulic observations. The latter compare derived driving pressure, flow and sign-change semantics bit-for-bit. The total remains 1,967 comparisons. The 39-row VR2 count is now traced explicitly to the 40-row source corpus minus the single `VR2-SAT-360C-PONLY` boundary-only/non-inverse row.


#### 8. Conclusive pre-execution review closure

The concluding review found and removed two certain procedural REDs before local execution: the machine contract had advanced to schema `v4` while the validator still required/output `v3`, and the validator still referenced a superseded authority property that no longer existed in the contract. Both are now aligned to the current REV2 contract.

The final hardening also removes three false-PASS/late-RED risks. C2, C3 and the historical Refinement-5 timing test are now checked against validator-authoritative normalized-text SHA-256 constants rather than trusting hash values supplied only by the editable JSON contract. The returned Refinement-5 prerequisite is checked for exact classification, counts, `c4-planning-justified=True`, exact aggregate/process artifact shape and the unique boundary-3 cross-process confirmation row. Finally, the median/p95/max calculation is machine-readable and frozen to the same deterministic rules documented by Planning 1: ascending sort, ordinary even-sample median, nearest-rank p95 at `ceil(0.95 * N) - 1`, and strict `elapsed_us > max_ceiling` exceedance semantics.

The local planning audit remains an evidence-generation gate only. A local `PASS-AS-AUTHORED` does not authorize C4 implementation. The complete planning artifact folder must be returned and adjudicated first; only that returned-evidence adjudication may authorize a separately versioned test-only C4 implementation/evidence candidate. RP1C and all production/runtime authority remain unchanged and unauthorized.

Conclusive static review result: **GO FOR LOCAL REV2 PLANNING AUDIT**, subject to the remaining environmental limitation that this review environment cannot execute Windows PowerShell 5.1 or .NET. No C4 implementation is present; `src/` and `tests/` remain byte-identical to the validated Handoff 2 baseline.

---

## Source snapshot — `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_C4_TEST_ONLY_IMPLEMENTATION1.md`

### M10 Final — VR2 Engineering Repair Planning 1 — RP1B C4 Test-Only Implementation 1

**Status:** CANDIDATE — implementation/evidence gate authorized only by the returned C4 Planning 1 REV2 adjudication.  
**Scope:** test/reference/shadow code and evidence generation only.  
**Not authorized:** RP1C selection, production thermodynamic repair, threshold/tolerance change, exact-v9 change, VR3, P3-R1 or second replacement-long execution.

#### 1. Authority and immutable prerequisites

C4 Planning 1 REV2 returned `PASS-AS-AUTHORED`, and its returned artifact set was separately adjudicated `PASS`. That adjudication authorizes only the separately versioned test-only C4 implementation defined by the REV2 contract.

The following historical sources remain immutable and are independently pinned by normalized-LF SHA-256 in the C4 validator:

- `Rp1bRefinement1ShadowThermodynamicCandidates.cs` — C2;
- `Rp1bRefinement2ShadowThermodynamicCandidates.cs` — C3/D3;
- `M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement5Tests.cs` — historical R5 timing harness.

The complete four-file returned C4 Planning 1 artifact set is frozen under `eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_C4_Planning1_Artifacts/`. The user-returned adjudication decision is stored separately so the planning artifacts themselves are not rewritten.

#### 2. C4 implementation boundary

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

#### 3. Semantic gate

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

#### 4. Allocation and wall-clock gate

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

#### 5. Evidence tree and classification

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

#### 6. Execution

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

#### 7. Final pre-execution review

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

#### 8. Returned execution and engineering adjudication

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


---

## Source snapshot — `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT1_C2_D2.md`

### M10 Final — VR2 Engineering Repair Planning 1 — RP1B Refinement 1 C2/D2

**Status:** HOTFIX 1 EXECUTION CANDIDATE — Attempt 1 was static-preflight RED only; no C2/D2 evidence exists from that attempt  
**Prerequisite:** RP1A REV1 validated; returned RP1B B1/C1/D1 matrix complete and reviewed  
**Authority:** test/reference/shadow refinement only. No production source change, thermodynamic repair, tolerance change, exact-v9 change, RP1C selection, VR3, P3-R1 or second replacement-long execution is authorized.

#### 0. Attempt 1 preflight finding and Hotfix 1

The first local Refinement 1 attempt did not start the ordinary build or the focused C2/D2 matrix. Windows PowerShell `ConvertFrom-Json` supplied numeric values that were then compared by the validator with exact `-ne` floating literals. The first false RED occurred on the frozen p95 ceiling `158.80666666666667 us`. This was a validator contract defect, not a corpus, C2, D2, threshold or performance result.

Hotfix 1 retains the exact same JSON values and engineering limits, but validates floating values through finite double conversion plus bounded absolute error. It also restores the CSV schema/key/output/authority checks already proven in the validated first-generation RP1B gate. C2/D2 source and the focused test remain unchanged.

#### 1. Why Refinement 1 exists

The returned first-generation RP1B matrix is valid evidence but yields no selectable candidate.

B1 demonstrates that a reduced closure can preserve exact-v9 phase ownership and stay inexpensive, but it misses the already-authored `<=10%` planning target and leaves substantial seam incompleteness. C1 demonstrates that a table surrogate can be extremely accurate and inexpensive on the states it resolves, but it leaves 12 exact-v9 rows unresolved; all 12 are the same logical owner, `feedwater-inventory`, around 47.52 °C in Region 1. D1 demonstrates that an IF97-consistent result is reachable over the exact-v9 path, but its worst-case scan/fallback paths exceed the frozen performance ceiling and it still leaves many near-boundary seam probes unresolved.

The correct response is not to retune B1/C1/D1 in place. Their identities and returned results are frozen. Refinement 1 therefore creates two new identities:

```text
C2-EXTENDED-TABULATED-SURROGATE
D2-SEAM-COMPLETE-IF97-COMPARATOR
```

RP1C remains blocked until these new identities have returned evidence on exactly the same RP1A corpus.

#### 2. Frozen evidence and corpus

The complete first-generation RP1B artifact set is frozen under:

```text
eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Artifacts/
```

The reviewed first-generation summary is frozen at:

```text
eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_UserReturnedSummary.txt
```

Refinement 1 does not regenerate or move any RP1A coordinate. It reuses:

```text
40 VR2 rows
  39 inverse-applicable
   1 boundary-only pressure row
360 exact-v9 node rows
288 hydraulic rows
1,280 seam probes
320 seam boundaries
```

The four seam sides and their RP1A offsets remain unchanged. A C2/D2 difficulty on those coordinates is evidence; it is not permission to move the probe.

#### 3. Frozen thresholds and performance ceilings

Refinement 1 preserves the existing comparison policy:

```text
VR2 blocking ceiling                 25%
Planning selection target            10%
Exact-v9 phase-agreement target      100%
Resolve median ceiling               94.8 us
Resolve p95 ceiling                  158.80666666666667 us
Resolve max ceiling                  409.30666666666673 us
Resolve median allocation ceiling    2816 B
```

The 10% value remains a planning/selection target and does not replace the existing VR2 25% blocking ceiling.

Candidate qualification remains evidence rather than the execution PASS criterion. The gate must still complete if C2 or D2 is inaccurate, incomplete or too slow, provided the matrix itself is complete, deterministic and finite.

#### 4. C2 — extended tabulated surrogate

C2 is a new version of family C, not a rewrite of C1.

The first-generation C1 failure pattern is narrow enough to justify bounded refinement: all 12 exact-v9 unresolved rows belong to `feedwater-inventory`, while all 39 inverse-applicable VR2 rows resolve and the candidate remains inside the RP1A performance ceiling.

C2 therefore changes the test-only table design before execution as follows:

- Region-4 saturation nodes are generated at 0.5 K spacing rather than the C1 1 K spacing;
- Region-4 inversion explicitly brackets transitions between unreachable and reachable mixture geometry so a root immediately inside q≈0 or q≈1 is not skipped merely because the first sampled node lies past it;
- Region-1 rows use 1 K temperature spacing rather than C1 5 K spacing;
- Region-1 pressure support adds low-pressure nodes and deterministic pressure offsets immediately above saturation, specifically to cover cold feedwater and liquid-side seam states without direct IF97 at resolve time;
- Region-2 rows use 2 K temperature spacing rather than C1 10 K spacing;
- Region-2 pressure support adds deterministic fractions immediately below the Region-2 upper pressure boundary, including the official B23-limited boundary above 623.15 K;
- bounded near-boundary liquid/vapor fallbacks use only initialization-time reference-derived local slopes; they do not call IF97 during `TryResolve`.

C2 remains a surrogate. `UsesDirectIf97AtResolveTime` must remain `false`.

The denser initialization table is allowed to cost more to build. RP1B compares initialization cost separately from steady-state `TryResolve` cost; only the already-frozen steady-state ceilings are used for later selection evidence.

#### 5. D2 — seam-complete IF97 comparator

D2 is a new version of family D, not a rewrite of D1.

D1 already proved the fidelity end of the trade space on resolved states, but its worst paths were dominated by broad reference scans/fallbacks and it remained seam-incomplete. D2 therefore changes the comparator topology rather than relaxing any threshold:

1. C2 supplies only a deterministic phase-aware initial seed.
2. If the seed is mixture, D2 refines temperature along the Region-4 saturation curve with direct IF97 evaluation.
3. If the seed is Region 1 or Region 2, D2 applies a bounded two-variable `(T,p)` Newton refinement under the same IF97 domain guards.
4. Qualified independent Region-4/Region-1 reference inverse helpers remain fail-closed fallback only; if Region 2 is still unresolved, D2 performs a bounded multi-seed Region-2 Newton fallback over the official Region-2 domain.

D2 still uses direct IF97 at resolve time and remains a fidelity/cost comparator. A favorable D2 performance result does not by itself make direct IF97 the preferred production design.

The intended engineering question is whether better phase-aware seeding can remove D1's long-tail behavior and near-boundary gaps. The result is measured, not assumed.

#### 6. Comparison dimensions

C2 and D2 are evaluated with the same machinery already validated by RP1B:

##### VR2 error map

All 40 frozen rows are preserved. The 39 `(v,u)` rows are scored for resolution, phase, pressure and temperature. `VR2-SAT-360C-PONLY` remains boundary-only/not inverse-scored.

##### Exact-v9 node map

All 360 rows are evaluated. The returned C1 `feedwater-inventory` gap is not special-cased or removed: C2 either resolves those rows under its new fixed design or records the failure.

##### Seam map

All 1,280 probes are evaluated without moving the boundary coordinates. Resolution, phase agreement and liquid/vapor cross-seam jumps are recorded.

##### Hydraulic replay

Candidate node pressures are replayed over all 288 frozen path rows using the already-qualified quadratic law and check-valve rule. The frozen IF97 pressure-only replay law is self-checked before candidate evaluation with the existing `<=1e-9 kg/s` harness tolerance.

##### Determinism

Each candidate is constructed independently twice and must reproduce resolution, branch identity, phase, pressure, temperature and quality bit-for-bit across VR2 + exact-v9 + seam inputs for the evidence gate to complete.

##### Performance and complexity

The same two warm-up passes and eight measured exact-v9 passes are used. Initialization, median/p95/max resolve cost, seam maximum and median allocation are recorded against the RP1A ceilings. Complexity records initialization reference-point count, conservative iteration ceiling and whether direct IF97 is called during resolve.

#### 7. RP1B Refinement 1 PASS semantics

The gate passes only when:

- returned first-generation RP1B evidence is present and internally consistent;
- the RP1A corpus remains unchanged;
- exactly C2 and D2 are evaluated;
- every candidate produces 40 VR2 rows, 360 node rows, 1,280 seam rows and 288 hydraulic rows;
- measured call counts match the frozen performance contract;
- deterministic repeat passes for both candidates;
- all timing evidence is finite;
- all nine expected artifacts are written.

The gate does **not** fail merely because C2 or D2 is not selection-eligible. That remains engineering evidence for the returned review.

#### 8. Selection remains deferred

Refinement 1 does not choose a candidate. In particular:

- an eligible C2 does not authorize production repair;
- a fast D2 does not authorize copying direct IF97 into production;
- a non-eligible C2 and D2 do not authorize changing the frozen thresholds;
- no result authorizes changing exact-v9 in place;
- no result authorizes VR3 before the VR2 repair/re-entry route is explicitly closed.

Returned review may authorize RP1C only after the complete C2/D2 artifact set is examined.

#### 9. Execution

From PowerShell:

```powershell
.\scripts\run-m10-final-vr2-engineering-repair-planning1-rp1b-refinement1.cmd
```

Return the complete folder:

```text
artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-refinement1
```

before RP1C implementation or any production thermodynamic change.

---

## Source snapshot — `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT1_HOTFIX1_PREEXECUTION_REVIEW.md`

### M10 Final — VR2 Engineering Repair Planning 1 — RP1B Refinement 1 Hotfix 1 Pre-Execution Review

**Scope:** static pre-execution review after Refinement 1 Attempt 1 static-preflight RED.  
**Engineering authority:** validation-harness hardening only. No C2/D2 retuning, production repair, tolerance change, exact-v9 change, RP1C selection, VR3, P3-R1 or second replacement-long authorization.

#### 1. Attempt 1 classification

Attempt 1 stopped in `[1/3]` before ordinary build and before the focused C2/D2 matrix. The failing check compared the JSON-deserialized p95 ceiling to an exact PowerShell floating literal with `-ne` and reported drift for the frozen value `158.80666666666667 us`.

This is a validator defect. It produced no C2/D2 evidence and does not alter any RP1A/RP1B engineering conclusion.

#### 2. Confirmed blocker corrected

The Refinement 1 validator had regressed from the already-validated first-generation RP1B validator, which used bounded `Math.Abs(...)` comparisons for long decimal values. Hotfix 1 replaces every floating contract equality with a finite-double check plus explicit absolute tolerance. The JSON contract values themselves are unchanged.

The hardened validator now checks, without exact floating equality:

- 25% existing VR2 blocking ceiling;
- 10% planning target;
- 100% exact-v9 phase-agreement target;
- `1e-9 kg/s` frozen-law replay self-check tolerance;
- 94.8 us median ceiling;
- 158.80666666666667 us p95 ceiling;
- 409.30666666666673 us max ceiling;
- 2816 B median-allocation ceiling.

#### 3. Preflight protections restored from validated RP1B

The Refinement 1 fork had also dropped checks already present in the validated RP1B Hotfix 2 validator. Hotfix 1 restores them before the next run:

- exact contract output count and ordering;
- all RP1A CSV headers;
- the `VR2-SAT-360C-PONLY` boundary-only identity;
- 40 / 39+1 VR2 shape;
- 360 reference-resolved exact-v9 node rows;
- 288 reference-resolved hydraulic rows;
- 1280 seam rows / 320 boundaries / 320 rows per seam side;
- compound node/hydraulic/seam key uniqueness;
- candidate warmup/measured-pass protocol;
- deterministic-repeat requirement;
- frozen hydraulic replay and seam-corpus requirements;
- Region-2 B23 domain guard;
- first-generation evidence freeze;
- complete no-authority flag set;
- null-safe UTF-8 text reads;
- a fail-closed guard against reintroducing the xUnit2031 `Assert.Single(...Where(...))` pattern.

#### 4. C2/D2 and focused-test static review

The Hotfix does not change C2, D2, the focused test, the runner or the machine-readable engineering contract. Static review found:

- no C2/D2 identity under `src/`;
- `src/` byte-identical to validated RP1B Hotfix 2;
- first-generation B1/C1/D1 test/reference files, contract, validator and runner byte-identical to validated RP1B Hotfix 2;
- frozen returned RP1B evidence byte-identical 9/9 to the user-returned files;
- C2 remains initialization-derived/tabulated and does not call IF97 directly from its resolve path;
- D2 remains a test-only direct-IF97 comparator seeded by C2;
- Region-2 pressure bounds continue to use the saturation boundary below 623.15 K and B23 through 863.15 K;
- hydraulic replay uses `(probe_id, logical_step, node_id/path_id)` identities and the already-qualified frozen law;
- no `Assert.Single(...Where(...))` analyzer pattern remains;
- all candidate loops/refinement paths inspected are explicitly bounded;
- runner filter method, opt-in environment variable and all nine artifact names align with the focused test and contract.

#### 5. Frozen-corpus audit repeated

Independent static parsing of the packaged RP1A evidence confirms:

```text
VR2 rows                 40
inverse-applicable        39
boundary-only              1
exact-v9 node rows        360
hydraulic rows            288
seam rows                1280
seam boundaries           320
R1-SIDE rows              320
R4-LIQUID-SIDE rows       320
R4-VAPOR-SIDE rows        320
R2-SIDE rows              320
```

All node, hydraulic and seam compound keys are unique and all node/hydraulic reference rows are resolved.

#### 6. What static review cannot certify

This environment does not provide the repository's .NET/Windows-PowerShell execution stack. Therefore Hotfix 1 does **not** pre-claim:

- Release build/analyzer PASS;
- actual C2/D2 initialization or resolve timing;
- physical accuracy or seam completeness;
- RP1C selection eligibility.

Those remain outputs of `[2/3]` and `[3/3]` on the validation workstation.

#### 7. Decision

Refinement 1 Attempt 1 is frozen as preflight-only RED. Hotfix 1 is the replacement execution candidate. No engineering threshold, C2/D2 equation/table, corpus coordinate, first-generation result or production source has been changed.

---

## Source snapshot — `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT2_C3_D3.md`

### M10 Final — VR2 Engineering Repair Planning 1 — RP1B Refinement 2 C3/D3

**Status:** VALIDATED RETURNED EVIDENCE — `PASS-EVIDENCE-MATRIX-COMPLETE`; engineering review authorizes only Refinement 3 immutable-C3 performance-tail attribution  
**Prerequisite:** RP1A REV1 validated; first-generation RP1B validated evidence; RP1B Refinement 1 C2/D2 returned `PASS-EVIDENCE-MATRIX-COMPLETE` and was reviewed as non-selecting.  
**Authority:** test/reference/shadow only. No production source change, thermodynamic repair, tolerance change, exact-v9 change, RP1C selection, VR3, P3-R1 or second replacement-long execution is authorized.

#### 1. Why Refinement 2 exists

Refinement 1 materially improved both candidate families but did not produce a selectable candidate.

C2 resolves all 39 inverse-applicable VR2 rows and all 360 exact-v9 rows with 100% exact-v9 phase agreement, meets the 10% planning target and all frozen steady-state performance ceilings, but leaves 260/1280 seam probes unresolved and 55 resolved seam probes with wrong phase ownership. The residual is highly localized: two unresolved `R4-VAPOR-SIDE` probes plus 258 unresolved and 55 wrong-phase `R2-SIDE` probes.

D2 likewise resolves all core VR2 and exact-v9 rows, meets the planning target and the frozen Resolve ceilings, and has zero seam phase mismatches. Its remaining gap is even narrower: 310 unresolved probes, all on `R4-VAPOR-SIDE`; `R2-SIDE` is complete.

The response is therefore not another broad surrogate redesign. C2 and D2 are frozen evidence. Refinement 2 creates new identities that touch only the vapor-side seam problem:

```text
C3-VAPOR-SEAM-COMPLETE-SURROGATE
D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR
```

#### 2. Frozen inputs

The complete returned Refinement 1 artifact set is frozen under:

```text
eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement1_Artifacts/
```

The engineering review summary is frozen at:

```text
eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Refinement1_UserReturnedSummary.txt
```

No RP1A coordinate is moved or regenerated. Refinement 2 reuses exactly:

```text
40 VR2 rows
  39 inverse-applicable
   1 boundary-only
360 exact-v9 node rows
288 hydraulic rows
1,280 seam probes
320 seam boundaries
4 seam sides × 320 probes
```

The seam offsets remain exactly `1e-5` relative pressure and `1e-6` quality.

#### 3. C3 — vapor-seam-complete surrogate

C3 preserves C2 as an immutable primary resolver. It adds only a dense saturation-boundary discriminator for states immediately around the saturated-vapor curve.

The new discriminator is generated entirely at initialization from IF97 reference states. `TryResolve` itself does not call IF97. The dense table uses 0.02 K saturation spacing and compares the target energy with the saturated-vapor energy interpolated at the same specific volume.

This geometry is deliberately narrow:

- a small positive energy margin identifies a state immediately outside the vapor dome and returns Region 2 / `SuperheatedVapor` before C2 mixture search can steal the state;
- C2 then remains the unchanged primary resolver for all other states;
- only if C2 fails, a small negative energy margin immediately inside the saturated-vapor boundary may return a Region-4 mixture fallback.

The discriminator is bounded to ±100 J/kg around the vapor boundary, so ordinary core vapor states are not redirected through this seam-specific path.

C3 remains a table/surrogate candidate and must keep `UsesDirectIf97AtResolveTime=false`.

#### 4. D3 — vapor-seam-complete IF97 comparator

D3 preserves D2 as the complete primary path. Therefore every state already resolved by D2 is returned unchanged.

Only D2-unresolved states may enter the new fallback:

1. C3 supplies a phase-aware near-vapor seed.
2. For a mixture seed, D3 performs a bounded local direct-IF97 Region-4 scan around the seed temperature.
3. A sign-changing energy residual is refined by bisection.
4. If the near-boundary direct refinement cannot improve the seed, the already phase-consistent C3 seed is retained rather than reopening broad D1-style scans.

D3 is still a fidelity/cost comparator, not a production recommendation.

#### 5. Selection criterion is stricter, not relaxed

Refinement 2 preserves every existing RP1A/RP1B criterion:

```text
VR2 blocking ceiling            25%
Planning target                 10%
Exact-v9 phase agreement        100%
Resolve median ceiling          94.8 us
Resolve p95 ceiling             158.80666666666667 us
Resolve max ceiling             409.30666666666673 us
Median allocation ceiling       2816 B
```

In addition, a candidate cannot be marked RP1C-selection-eligible unless:

```text
all 1,280 seam probes resolve
AND
seam phase mismatch count = 0
```

No threshold is loosened and the seam probe coordinates remain immutable.

#### 6. PASS semantics

The Refinement 2 execution gate remains an evidence-completion gate. It passes when the C3/D3 matrix is complete, deterministic and finite. A physically poor or too-slow candidate is recorded as such; it does not make the evidence harness itself fail.

For each candidate the gate emits the same nine artifact families used by Refinement 1, now with explicit `vapor_seam_completion_met` evidence in the candidate summary.

RP1C selection is not performed by this gate.

#### 7. Execution

From PowerShell:

```powershell
.\scripts\run-m10-final-vr2-engineering-repair-planning1-rp1b-refinement2.cmd
```

Return the complete folder:

```text
artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-refinement2
```

before implementing RP1C or changing production thermodynamics.

---

## Source snapshot — `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT2_PREEXECUTION_REVIEW.md`

### M10 Final — VR2 Engineering Repair Planning 1 — RP1B Refinement 2 Pre-Execution Review

**Candidate:** C3/D3 vapor-side seam completion  
**Review status:** STATIC REVIEW COMPLETE — runtime build/test still requires the user environment  
**Production impact:** none; `src/` must remain byte-identical to the validated Refinement 1 Hotfix 1 baseline.

#### Reviewed failure classes

The review explicitly re-checks the failure classes that previously caused avoidable RED attempts:

- Windows PowerShell 5.1 ASCII/UTF-8 source stability;
- null-safe text reads in validators;
- tolerant floating-point contract comparisons rather than exact `-ne` comparisons;
- RP1A CSV schema, row counts and compound-key uniqueness;
- xUnit2031-prone `Assert.Single(...Where(...))` patterns;
- runner environment variable, filter-method and output-name agreement;
- immutable boundary-only VR2 row handling;
- immutable 40/360/288/1280 RP1A corpus counts;
- frozen first-generation and Refinement 1 evidence identity;
- no C3/D3 identity under production `src/`;
- no new production closure mode and no exact-v9 semantic change.

#### Candidate-specific review

##### C3

C3 wraps C2 rather than editing it. The only pre-C2 path is a narrow saturated-vapor-boundary discriminator. It is built at initialization from a 0.02 K saturation table and never calls IF97 from `TryResolve`.

The discriminator is bounded to an energy margin of 0.05–100 J/kg from the same-specific-volume saturated-vapor curve. Positive margin is interpreted as immediate Region-2 side; a negative margin is used only after immutable C2 has failed and therefore acts as the near-vapor Region-4 fallback. This ordering is intended to prevent the 55 C2 R2-side mixture steals without perturbing already-resolved C2 core states.

##### D3

D3 calls immutable D2 first. The new code can execute only when D2 returns unresolved. C3 then supplies a phase-aware seed. Mixture seeds receive a bounded local direct-IF97 scan over ±0.25 K with 0.005 K scan spacing and at most 48 bisection refinements. If no stronger direct root is found, the C3 seed remains available as a fail-closed phase-consistent fallback rather than reopening broad D1-style scans.

#### Selection semantics

Refinement 2 does not perform selection. The candidate summary is stricter than Refinement 1: `rp1c_selection_eligible` additionally requires zero seam unresolved rows and zero seam phase mismatches across all 1,280 frozen probes.

No result from this gate authorizes production thermodynamics, tolerance changes, exact-v9 changes, VR3, P3-R1 or a second replacement-long baseline.

#### Remaining runtime-only checks

This environment does not provide the project's .NET toolchain or Windows PowerShell 5.1, so the following remain intentionally unresolved until the local run:

- Release compilation and analyzers;
- ordinary suite;
- actual C3/D3 seam completion counts;
- actual resolve and seam timing distributions;
- deterministic-repeat evidence produced by the focused test.

---

## Source snapshot — `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT3_C3_PERFORMANCE_TAIL.md`

### M10 Final — VR2 Engineering Repair Planning 1 — RP1B Refinement 3 C3 Performance-Tail Attribution

**Status:** VALIDATED RETURNED EVIDENCE — exact-v9/targeted performance qualified; one isolated R1-side seam worst-case remains for Refinement 4 localization  
**Prerequisite:** RP1A validated; first-generation RP1B validated evidence; RP1B Refinement 1 C2/D2 validated evidence; RP1B Refinement 2 C3/D3 returned `PASS-EVIDENCE-MATRIX-COMPLETE` and reviewed.  
**Authority:** performance attribution only on immutable C3. No C4 implementation, production source change, thermodynamic repair, tolerance change, exact-v9 change, RP1C selection, VR3, P3-R1 or second replacement-long execution is authorized.

#### 1. Why Refinement 3 exists

Refinement 2 closed the remaining physical/seam problem. C3 and D3 both returned:

```text
39/39 inverse-applicable VR2 rows resolved
360/360 exact-v9 rows resolved
100% exact-v9 phase agreement
1280/1280 seam probes resolved
0 seam phase mismatch
planning target <=10% met
deterministic repeat true
```

The remaining issue is performance interpretation.

C3 is the production-oriented surrogate family and returned:

```text
median Resolve   7.2 us
p95 Resolve      124.6 us
max Resolve      823.4 us
seam max         175.1 us
```

The frozen RP1A max ceiling is `409.30666666666673 us`, therefore C3 was not performance-qualified even though its seam maximum was already inside the ceiling.

D3 returned `resolve max=208.5 us` but `seam max=16504.9 us`. The Refinement-2 boolean `within_frozen_ceilings` did not include `seam_max`, so the recorded D3 `rp1c-selection-eligible=True` is retained as frozen evidence but is not treated as sufficient engineering authorization. Planning 1 requires worst-case boundary/inverse-search cost to be considered explicitly.

Refinement 3 therefore does not modify either candidate. It attributes C3's exact-v9 performance tail and applies a corrected full performance predicate that includes seam maximum.

#### 2. Immutable candidate and corpus

C3 remains exactly:

```text
C3-VAPOR-SEAM-COMPLETE-SURROGATE
```

No C4 identity is introduced. `Rp1bRefinement2ShadowThermodynamicCandidates.cs` remains unchanged from the returned Refinement-2 candidate.

The timing corpus remains the RP1A freeze:

```text
360 exact-v9 node states
1280 seam probes
320 seam boundaries
4 seam sides x 320 probes
```

No state coordinate, seam offset, VR2 ceiling or planning target is changed.

#### 3. Full-corpus timing protocol

To reduce ordering bias without changing the corpus, C3 is warmed for 16 full passes and then measured for 64 full passes. Each pass rotates the first state by a fixed stride of 37 rows; the same 360 states are still measured exactly once per pass.

The exact-v9 stage therefore produces:

```text
360 x 64 = 23040 measured Resolve calls
```

For each state the artifact records median, p95, max, the pass containing the max, number/fraction of calls above the frozen max ceiling, and a descriptive tail classification.

A full GC is requested before the measured full-corpus stage and generation collection counts are recorded as attribution context. GC evidence does not change the pass/fail ceiling.

#### 4. Targeted tail-repeat protocol

Refinement 3 selects a bounded set consisting of:

- every state that exceeded the frozen max ceiling during screening; plus
- the highest-p95 states, ensuring at least the top 12 are represented.

For each selected state:

```text
32 warm-up calls
8 measured blocks
64 calls per block
512 measured calls per target state
```

This stage distinguishes:

```text
TARGETED-WITHIN-CEILING
TARGETED-ISOLATED-EXCEEDANCE
TARGETED-REPEATED-EXCEEDANCE
TARGETED-PERSISTENT-SLOW-PATH
```

These labels are descriptive attribution, but the targeted calls are still strict measured evidence: any targeted single-call value above the frozen maximum keeps performance qualification false. Targeted statistics never replace or relax the frozen single-call maximum requirement.

#### 5. Seam-side timing protocol

The same immutable 1280 seam probes are warmed for 4 passes and measured for 16 passes. Evidence is aggregated independently for:

```text
R1-SIDE
R4-LIQUID-SIDE
R4-VAPOR-SIDE
R2-SIDE
ALL-SEAM-SIDES
```

Every measured call must remain resolved and phase-consistent with the frozen reference.

#### 6. Corrected full performance predicate

Refinement 3 preserves every RP1A ceiling:

```text
median Resolve <= 94.8 us
p95 Resolve    <= 158.80666666666667 us
max exact-v9   <= 409.30666666666673 us
max seam       <= 409.30666666666673 us
median alloc   <= 2816 B
```

The key correction is explicit:

```text
full-performance-qualified =
    median <= median ceiling
    AND p95 <= p95 ceiling
    AND exact-v9 single-call max <= max ceiling
    AND seam single-call max <= same max ceiling
    AND targeted-repeat single-call max <= same max ceiling
    AND median allocation <= allocation ceiling
```

The prior D3 eligibility flag is not rewritten; it remains provenance of the Refinement-2 harness. Refinement 3 simply prevents future engineering selection from omitting the seam worst case.

#### 7. Gate semantics

Refinement 3 is an evidence-completion gate. It may PASS even if C3 remains performance-unqualified.

Possible engineering attribution values include:

```text
STRICT-PERFORMANCE-CONTRACT-MET
SEAM-WORST-CASE-BLOCKING
DETERMINISTIC-SLOW-PATH-CONFIRMED
REPEATED-PERFORMANCE-TAIL-CONFIRMED
ISOLATED-PERFORMANCE-TAIL-NOT-QUALIFIED
PERFORMANCE-CONTRACT-NOT-MET
```

A returned `STRICT-PERFORMANCE-CONTRACT-MET` may support an RP1C authorization decision only after artifact review. Any other attribution keeps RP1C blocked; C4 is still not automatically authorized.

#### 8. Execution

From PowerShell:

```powershell
.\scripts\run-m10-final-vr2-engineering-repair-planning1-rp1b-refinement3.cmd
```

Return the complete folder:

```text
artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-refinement3
```

before RP1C, C4 or any production thermodynamic change.

#### 9. Attempt 1 / Hotfix 1

The first execution stopped in the static preflight before build because the production-source leak scan admitted a generated native library from `src/**/bin`. No C3 performance measurement ran and no Refinement-3 engineering attribution exists for that attempt.

Hotfix 1 changes only validator/provenance behavior: the leak scan now examines real `.cs` files outside `bin`/`obj`. C3 and the complete timing/qualification protocol in sections 3–7 remain byte-identical.


#### 10. Returned result and successor

Returned Refinement 3 completed with `PASS-PERFORMANCE-ATTRIBUTION-EVIDENCE-COMPLETE`. Exact-v9 max was `150.5 us` and targeted-repeat max `162.7 us`, both below the frozen ceiling, while seam max was `3667.1 us`. The only seam exceedance was one R1-side call among 5,120 R1 measurements. The aggregate artifact did not retain its boundary identity, so RP1B Refinement 4 is authorized only for immutable-C3 R1-seam localization/reproducibility. RP1C and C4 remain unauthorized pending returned Refinement 4 review.

---

## Source snapshot — `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT3_PREEXECUTION_REVIEW.md`

### M10 Final — VR2 Engineering Repair Planning 1 — RP1B Refinement 3 Pre-Execution Review

**Review status:** STATIC REVIEW COMPLETE — build/runtime still require the local .NET/Windows PowerShell gate.  
**Scope:** Refinement 3 harness, contract, validator, runner, frozen evidence and package boundaries.  
**Production scope:** none.

#### Frozen identity checks

The candidate was constructed directly from the RP1B Refinement 2 C3/D3 package. Before packaging Refinement 3:

```text
src/                                             byte-identical
Rp1bRefinement2ShadowThermodynamicCandidates.cs  byte-identical
Refinement 2 focused test                         byte-identical
Refinement 2 JSON contract                        byte-identical
Refinement 2 validator                            byte-identical
Refinement 2 runner                               byte-identical
returned Refinement 2 artifacts                   byte-identical 9/9
```

C3 therefore remains the exact version that produced the returned Refinement-2 evidence. No C4 identity exists.

#### C# review

The new focused test was reviewed for the failure classes previously encountered in VR2/RP1B:

- no direct access to `internal` Application members;
- no `Assert.Single(...Where(...))` xUnit2031 pattern;
- nullable use is explicit for the optional timing-sample list;
- CSV schemas are exact-string checked before parsing;
- node and seam counts are asserted before timing;
- C3 physical resolution/phase behavior is rechecked before any timing interpretation;
- timing loops are finite and bounded;
- tuple keys use the full `(probe_id, logical_step, node_id)` identity;
- braces, parentheses and brackets are structurally balanced in a lexical scan;
- no new thermodynamic candidate implementation is present in the focused test.

The test intentionally does not assert that C3 meets the performance ceiling. It asserts only evidence completeness/finite timing so a RED/green engineering result is written to artifacts instead of destroying the attribution evidence.

#### Performance semantics review

The frozen ceilings remain:

```text
median       94.8 us
p95          158.80666666666667 us
max          409.30666666666673 us
allocation   2816 B
```

Refinement 3 makes the full worst-case predicate explicit:

```text
screen median <= median ceiling
screen p95 <= p95 ceiling
screen exact-v9 max <= max ceiling
seam max <= same max ceiling
targeted-repeat max <= same max ceiling
median allocation <= allocation ceiling
```

Targeted repeat labels are diagnostic only in meaning; the targeted calls themselves are additional strict measurements and cannot relax an observed ceiling violation.

#### Validator review

The validator was checked for the PowerShell failure classes already observed in this project:

- ASCII-only source for Windows PowerShell 5.1;
- null-safe UTF-8 reads via `System.IO.File.ReadAllText`;
- all floating values use finite bounded comparisons through `Require-NearDouble`;
- numeric `-ne` is used only for integer counts/protocol constants;
- returned Refinement-2 9/9 artifact existence and row counts are checked;
- C3/D3 returned timing values and physical completion markers are checked;
- RP1A timing corpus counts and compound-key uniqueness are checked;
- contract output order and complete authority-false set are checked;
- source scan forbids C3/Refinement3/C4 identities from production `src/`;
- exact-v9 identity and existing closure mode markers remain required.

#### Package review boundary

Refinement 3 may add only:

- returned Refinement-2 frozen evidence and engineering summary;
- Refinement-3 focused performance-attribution test;
- Refinement-3 contract, validator and runner;
- Refinement-3 documentation/provenance updates.

It must not add `bin/`, `obj/`, generated `artifacts/` or production thermodynamic changes.

#### Residual limitation

This environment does not provide .NET 10 or Windows PowerShell, so compilation, xUnit analyzer execution and actual machine-local timing cannot be certified statically. Those remain the purpose of the local `[2/3]` and `[3/3]` gate.

#### Attempt 1 returned preflight RED and Hotfix 1 review

The first local Refinement-3 run did not reach ordinary build or the focused timing test. The static validator reported an RP1B Refinement-3 identity leak in:

```text
src/NuclearReactorSimulator.App/bin/Release/net10.0/runtimes/osx/native/libSkiaSharp.dylib
```

This path is generated native build output, not production C# source. The original review incorrectly validated the source scan only against the clean packaged tree. In a real working tree, prior builds leave `src/**/bin` and `src/**/obj` populated. On Windows PowerShell 5.1 the original `Get-ChildItem -LiteralPath 'src' -Recurse -File -Include *.cs` form did not reliably restrict the returned descendants to C# source, so binary content could reach `Read-Utf8Text` and accidentally satisfy a short forbidden marker.

Hotfix 1 changes only the validator/provenance path. The production identity scan now:

```text
enumerates descendants of src/
keeps only Extension == .cs
excludes any /bin/ or /obj/ path segment
rejects an empty production-source set
rechecks that every selected item is .cs and outside bin/obj before reading text
```

C3, the focused test, timing protocol, thresholds, contract and runner are unchanged. Attempt 1 therefore produces no performance evidence and no engineering attribution.

#### Hotfix 2 build review

Attempt 2 passed the generated-output-safe static preflight and entered the ordinary Release build. The build stopped on a single test-only `CS1061`: `TailRepeat` exposes `TargetP95Microseconds`, while line 119 referenced `P95Microseconds`. Hotfix 2 changes only that member access. A complete scan of `StateTiming`, `TailRepeat` and `SeamSideTiming` member usage found no second name drift, and the xUnit2031 pre-check remains clean. No C3 timing evidence was produced by Attempt 2.

---

## Source snapshot — `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT4_C3_R1_SEAM_LOCALIZATION.md`

### M10 Final — VR2 Engineering Repair Planning 1 — RP1B Refinement 4 C3 R1-Seam Worst-Case Localization

**Status:** VALIDATED RETURNED EVIDENCE — Hotfix 1 completed; C3 remained immutable; returned localization found isolated exceedances on different R1 boundaries and does not justify C4  
**Prerequisite:** RP1A, first-generation RP1B, Refinement 1, Refinement 2 and returned Refinement 3 evidence reviewed.  
**Authority:** diagnostic localization/reproducibility only on immutable C3. No C4, RP1C selection, production source change, thermodynamic repair, tolerance change, exact-v9 change, VR3, P3-R1 or second replacement-long execution is authorized.

#### 1. Why Refinement 4 exists

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

#### 2. Frozen candidate and corpus

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

#### 3. R1 screen protocol

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

#### 4. Boundary localization

The screen is reduced to exactly 320 boundary summaries containing median, p95, max, max pass/row identity, exceedance count/fraction, GC-overlap counts and a descriptive screen classification.

Target boundaries are the union of:

- every boundary with any screen exceedance;
- top 12 boundaries by p95;
- top 12 boundaries by max.

The union is deterministic and does not remove historical evidence if a clean rerun fails to re-observe the Refinement-3 spike.

#### 5. Targeted reproducibility protocol

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

#### 6. Frozen ceiling and interpretation

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

#### 7. Outputs

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

#### 8. Execution

From PowerShell:

```powershell
.\scripts\run-m10-final-vr2-engineering-repair-planning1-rp1b-refinement4.cmd
```

Return the complete folder:

```text
.\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-refinement4
```

before RP1C, C4 or any production thermodynamic change.

#### Attempt 1 build result and Hotfix 1

The first local execution passed the static prerequisite/contract audit and entered the ordinary Release build, but `NuclearReactorSimulator.Simulation.Tests` stopped before the focused diagnostic because xUnit analyzer rule `xUnit2012` rejected the collection-existence assertion at line 186:

```text
Assert.True(seamLines.Any(predicate))
```

No Refinement 4 timing/localization artifact was produced and C3 was never exercised by the focused gate. Hotfix 1 changes only this test assertion to the analyzer-approved equivalent:

```text
Assert.Contains(seamLines, predicate)
```

The asserted historical Refinement-3 R1 evidence, localization protocol, timing ceilings and candidate identity are unchanged. A full analyzer-oriented scan of the Refinement 4 test found no second `Assert.True(...Any(...))` or `Assert.Single(...Where(...))` pattern. The validator now rejects reintroduction of the xUnit2012-prone form before build.


#### Returned Hotfix 1 evidence and engineering review

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

---

## Source snapshot — `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT4_PREEXECUTION_REVIEW.md`

### M10 Final — VR2 Engineering Repair Planning 1 — RP1B Refinement 4 Pre-Execution Review

**Status:** PRE-EXECUTION STATIC REVIEW COMPLETE  
**Scope:** immutable-C3 R1 seam localization harness, returned Refinement-3 freeze, contract, validator, runner and package boundary.

#### Review conclusions

- C3/D3 reference implementation is not modified.
- Production `src/` is not modified.
- Returned Refinement-3 artifacts are frozen byte-for-byte as six prerequisites.
- The validator is ASCII-only, uses null-safe UTF-8 reads and tolerance-based floating comparisons.
- Production source scanning enumerates only real `.cs` files outside `bin/obj`, so dirty working trees cannot repeat the native-binary false positive seen in Refinement 3 Attempt 1.
- The new focused test uses no `Assert.Single(...Where(...))` pattern and accesses no Application internal members.
- The seam CSV header, 1280 total rows, 320 R1 rows and 320 unique R1 boundaries are checked before build.
- The strict maximum remains `409.30666666666673 us`; no percentile or repeat statistic substitutes for it.
- The evidence gate may complete regardless of whether an exceedance is reproduced; interpretation is deferred to returned-artifact review.

The remaining checks that require the user's environment are the ordinary Release build/analyzers and the actual timing evidence on the validation machine.

#### Mechanical audit results

The packaged candidate was compared against RP1B Refinement 3 Hotfix 2 before ZIP creation:

```text
production src/                    byte-identical
C3/D3 reference implementation    byte-identical
returned Refinement-3 artifacts   byte-identical 6/6
production C# files scanned        951
new production files               0
new focused tests                  1
new C4 identity                    0
```

Static contract checks also confirm `20480` R1 screen calls, `320` unique R1 boundaries and `2048` targeted calls per selected boundary. The new C# file passed lexical delimiter balance and known xUnit2031-pattern scans. The environment used to build this package does not contain .NET/Windows PowerShell, so compilation/analyzer success remains intentionally owned by the user's ordinary Release gate.

#### Post-Attempt-1 review correction

The initial pre-execution review missed xUnit analyzer rule `xUnit2012` on one assertion that tested collection membership through `Assert.True(seamLines.Any(predicate))`. Attempt 1 therefore passed static audit but failed the ordinary Release build before any focused timing run.

Hotfix 1 corrects only that assertion to `Assert.Contains(seamLines, predicate)` and adds a validator regression check. A full scan of the Refinement 4 test found no additional `Assert.True(...Any(...))` or `Assert.Single(...Where(...))` analyzer-prone pattern. All immutable-C3 timing/localization logic remains unchanged.

---

## Source snapshot — `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_REFINEMENT5_PREEXECUTION_REVIEW.md`

### M10 Final — VR2 Engineering Repair Planning 1 — RP1B Refinement 5 — Pre-Execution Review

#### Scope

Static pre-execution review of the C3 cross-process wall-clock tail reproducibility candidate. This review does not claim a .NET build/test PASS; the executable confirmation remains the local runner.

#### Frozen invariants checked

- production `src/` is byte-identical to the validated Performance Measurement Replanning 1 baseline;
- C3/D3 test-only implementation is byte-identical;
- returned Refinement 4 evidence remains byte-identical;
- returned Performance Measurement Replanning 1 audit is frozen as prerequisite evidence;
- C3 identity and strict `409.30666666666673 us` ceiling are unchanged;
- C4 is not created and RP1C remains unauthorized.

#### Runner / process isolation review

- the runner deletes the Refinement 5 artifact root once, before measurements;
- it performs ordinary Release build before focused measurement;
- it launches five separate `dotnet test` commands, therefore five fresh test runtime processes;
- each process receives a frozen run index `1..5` through `NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_REFINEMENT5_RUN_INDEX`;
- each process writes only its own `process-01` ... `process-05` subdirectory;
- cross-process adjudication runs only after all five focused processes return PASS evidence-generation status.

#### C# focused-test review

- 320 unique R1-side boundaries are loaded from the immutable RP1A seam map;
- each process uses 16 warm-up passes, 64 measured passes and rotation stride 37;
- exactly 20,480 measured calls are required per process;
- each measured call records boundary/pass identity, wall time, allocation and GC deltas;
- timing exceedance does not fail the evidence gate;
- physical resolution/phase mismatch, malformed corpus or missing artifacts still fail closed;
- no xUnit2012 `Assert.True(collection.Any(...))` pattern is present;
- no xUnit2031 `Assert.Single(...Where(...))` pattern is present.

#### PowerShell 5.1 review

- validator and adjudicator source are ASCII-only;
- UTF-8 text reads are explicit/null-safe where validator source text is inspected;
- floating-point contract values use tolerance comparisons in the validator;
- production source scans admit only `.cs` outside `bin`/`obj`;
- the adjudicator parses invariant-culture floating values and writes UTF-8 without BOM;
- the same-boundary rule is applied by counting distinct process runs with at least one exceedance for each boundary.

#### Cross-process adjudication contract

A C3 slow path is candidate-specific only when the same boundary exceeds the unchanged maximum ceiling in at least two independent processes. Refinement 5 may therefore return evidence with exceedances and still complete successfully.

Possible classifications remain:

```text
C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED
C3-CROSS-PROCESS-ISOLATED-WALL-CLOCK-TAIL
C3-CROSS-PROCESS-NO-EXCEEDANCE-OBSERVED
```

No classification directly authorizes C4 implementation or RP1C.

---

## Source snapshot — `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1B_TEST_ONLY_SHADOW_CANDIDATE_MATRIX.md`

### M10 Final — VR2 Engineering Repair Planning 1 — RP1B Test-Only Shadow Candidate Matrix

**Status:** VALIDATED EVIDENCE MATRIX — SUPERSEDED FOR ACTIVE EXECUTION BY RP1B REFINEMENT 1 C2/D2  
**Prerequisite:** RP1A REV1 returned evidence reviewed and promoted to `VALIDATED`  
**Authority:** test/reference/shadow comparison only. No production source change, repair activation, tolerance change, exact-v9 reinterpretation, VR3, P3-R1 or second replacement-long execution is authorized.


#### 0. Attempt 1 preflight failure and Hotfix 1

The first local RP1B attempt did not complete the static preflight. The validator failed before the ordinary build and before the focused B1/C1/D1 matrix because the production-source identity scan used `Get-Content -Raw` followed by `.Contains(...)` without guarding a null result. No candidate evidence was produced and no engineering interpretation is attached to that attempt.

Hotfix 1 changes only validator text-reading/provenance. All validator text reads now use fail-closed `System.IO.File.ReadAllText(..., UTF8)` semantics: a readable text file yields a non-null string, while a read failure throws an explicit path-qualified error. Candidate implementations, frozen RP1A corpus, focused RP1B test, runner, thresholds and production `src/` remain unchanged.

The failed attempt is frozen at `eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Attempt1_PreflightRedSummary.txt`.

#### 0.1 Attempt 2 build failure and Hotfix 2

Hotfix 1 passed the static preflight, but the Release build stopped before focused execution on one xUnit analyzer finding: `Assert.Single(vr2Rows.Where(predicate))` violates xUnit2031 under warnings-as-errors. No B1/C1/D1 focused evidence was produced.

Hotfix 2 changes only that assertion to the semantically equivalent xUnit predicate overload `Assert.Single(vr2Rows, predicate)`. The candidate implementations, immutable RP1A corpus, runner, RP1B contract, thresholds, performance ceilings and production `src/` remain unchanged. A repository-local scan of the RP1B test/reference files found no second LINQ-filtered `Assert.Single` pattern.

The failed build attempt is frozen at `eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Attempt2_BuildRedSummary.txt`.


#### 0.2 Returned first-generation matrix and Refinement 1 authorization

Hotfix 2 completed locally and returned all nine RP1B artifacts. The matrix is frozen as `PASS-EVIDENCE-MATRIX-COMPLETE`; no B1/C1/D1 identity is selection-eligible. The complete returned artifact set is preserved under:

```text
eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Artifacts/
```

The reviewed summary is frozen at `eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_UserReturnedSummary.txt`.

The first-generation engineering findings are intentionally non-ranking but decisive for refinement scope:

- B1 keeps 100% exact-v9 phase agreement and meets the frozen performance ceiling, but misses the `<=10%` planning target and remains seam-incomplete;
- C1 resolves all 39 inverse-applicable VR2 rows and meets the performance ceiling, but leaves 12 exact-v9 rows unresolved, all owned by `feedwater-inventory`, and is seam-incomplete;
- D1 meets the planning fidelity target with 100% exact-v9 phase agreement, but violates the frozen performance ceiling and remains seam-incomplete because long-tail reference fallbacks dominate its worst cases.

The returned review therefore authorizes only versioned **RP1B Refinement 1** with `C2-EXTENDED-TABULATED-SURROGATE` and `D2-SEAM-COMPLETE-IF97-COMPARATOR`. B1/C1/D1 remain immutable evidence. RP1C remains unauthorized until returned Refinement 1 review.

#### 1. Purpose

RP1B compares the three repair families advanced by validated Planning 1 against exactly the same corpus frozen by RP1A. It is deliberately a comparison gate, not a selection gate.

The three first-version candidates are immutable identities once this RP1B run produces final evidence:

```text
B1-PIECEWISE-REDUCED
C1-TABULATED-SURROGATE
D1-BOUNDED-IF97-SUBSET
```

If a result later motivates a mathematical change, the original result remains frozen and a new candidate version such as B2/C2/D2 must be created. RP1B never silently retunes B1/C1/D1 after seeing their final matrix.

#### 2. Returned RP1A authority

The complete returned RP1A artifact set is frozen under:

```text
eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1A_Artifacts/
```

The reviewed returned summary is frozen at:

```text
eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1A_UserReturnedSummary.txt
```

RP1A is now `VALIDATED`. The frozen evidence shape is:

- 40 VR2 reference rows: 39 inverse-applicable `(v,u)` rows plus the preserved `VR2-SAT-360C-PONLY` boundary-only row;
- 360 exact-v9 node rows;
- 288 hydraulic-context rows;
- 1,280 seam probes across 320 boundaries;
- 348 Region-4 and 12 Region-1 exact-v9 reference rows;
- deterministic seam repeat;
- 4 current-production seam unresolved rows and 638 resolved phase mismatches retained as baseline evidence.

The machine-local candidate ceilings were fixed before any B/C/D implementation or timing inspection:

```text
median Resolve <= 94.8 us
p95 Resolve    <= 158.80666666666667 us
max Resolve    <= 409.30666666666673 us
median allocation <= 2816 B
```

These are candidate-cost ceilings inherited from RP1A. They are not thermodynamic tolerances.

#### 3. Candidate B1 — piecewise reduced closure

B1 is a test-only reduced closure. Its design intentionally avoids turning the candidate into a direct full IF97 inverse implementation.

It uses:

- a bounded Region-4 saturation table generated from the independent IF97 helper;
- reference-consistent mixture ownership from `(v,u)` over that table;
- a reduced compressed-liquid branch using saturation liquid properties and a temperature-dependent effective secant bulk modulus derived at initialization;
- a reduced superheated branch using an ideal-gas pressure relation plus a bounded vapor internal-energy continuation from saturation.

The purpose is to determine whether a relatively small educational closure can repair phase ownership and materially reduce pressure error while remaining cheap enough for the 10 ms simulation architecture.

B1 does not modify the existing production closure and is not a production prototype.

#### 4. Candidate C1 — bounded IF97-derived table surrogate

C1 is a test-only structured interpolation surrogate generated deterministically from the independent IF97 helper.

It contains:

- a 1 K saturation table for Region-4 ownership and mixture inversion;
- Region-1 temperature rows at 5 K spacing with bounded pressure nodes up to 100 MPa;
- Region-2 temperature rows at 10 K spacing with bounded pressure nodes up to 20 MPa;
- deterministic interpolation at fixed specific volume followed by interpolation across temperature rows.

The table is generated before candidate evaluation from the already frozen RP1A domain design. No final RP1B result is used to move table nodes.

The study separately records initialization cost and steady-state `TryResolve(v,u)` cost so one-time table construction is not confused with fixed-step runtime cost. Region-2 table generation is additionally bounded by the official IF97 B23 Region-2/3 pressure boundary between 623.15 K and 863.15 K; the candidate never labels a B23-side Region-3 point as Region 2 merely because it lies below the generic 20 MPa study cap. The complexity artifact reports the 32-iteration Region-4 table-bisection ceiling rather than claiming that the surrogate is iteration-free.

#### 5. Candidate D1 — bounded IF97 subset comparator

D1 is the fidelity/cost comparator. It uses C1 only as a deterministic initial seed, then refines states with direct IF97 Region 1/2/4 equations.

The direct path uses:

- Region-4 mixture refinement along the saturation curve;
- bounded two-variable Region-1 and Region-2 refinement in `(T,p)`;
- existing qualified independent inverse helpers as fail-closed fallback where available.

D1 is not the preferred production design by construction. Its role is to establish the fidelity/cost end of the trade space. Region-2 refinement uses the same B23 domain guard as C1, and the complexity artifact records a conservative 1200-iteration worst-path scan/refinement/fallback ceiling so the comparator cannot look artificially cheap. A favorable accuracy result cannot by itself authorize copying the test helper into production.

#### 6. Immutable comparison corpus

Every candidate is evaluated against the same RP1A artifacts:

```text
02-vr2-reference-point-corpus.csv     40 rows
03-exact-v9-node-corpus.csv          360 rows
04-hydraulic-context.csv             288 rows
05-seam-probe-map.csv              1,280 rows
06-performance-baseline.csv
```

RP1B does not regenerate, filter, rebalance or discard points based on candidate behavior.

#### 7. Evidence dimensions

##### 7.1 VR2 inverse error map

For the 39 inverse-capable VR2 rows RP1B records the inverse result. The 40th frozen row, `VR2-SAT-360C-PONLY`, is preserved in the output as boundary-only/not-applicable and is not falsely scored as an inverse-closure failure. For inverse-applicable rows RP1B records:

- resolved/unresolved;
- candidate branch/phase;
- candidate temperature and pressure;
- phase agreement;
- pressure relative error;
- temperature relative error on an absolute-Kelvin basis.

The existing 25% VR2 blocking ceiling remains unchanged. The 10% figure remains only the stricter Planning 1 selection target and is not a replacement tolerance.

##### 7.2 exact-v9 node corpus

For all 360 frozen exact-v9 rows RP1B records the same inverse comparison. Planning 1 requires 100% phase agreement as the target for a selectable repair design.

##### 7.3 seam ownership and continuity

All 1,280 RP1A seam probes are evaluated without moving the boundary coordinates.

RP1B records:

- unresolved rows;
- phase mismatches;
- candidate-reference pressure and temperature differences;
- maximum R1-side versus R4-liquid-side pressure/temperature jumps;
- maximum R4-vapor-side versus R2-side pressure/temperature jumps.

These continuity values are comparison evidence. RP1B does not invent a new continuity tolerance after seeing candidate results.

##### 7.4 hydraulic replay

Candidate pressures from the 360 node rows are applied offline to the exact same 288 Attempt-5 hydraulic rows.

The replay preserves:

- frozen resistance;
- frozen active pump boost;
- canonical flow only as observation;
- the exact-v9 feedwater discharge check-valve directionality;
- no reinjection into runtime state.

Before any candidate is evaluated, RP1B reconstructs all 288 frozen IF97 pressure-only counterfactual flows from the frozen reference pressures, resistance, pump boost and check-valve rule; the maximum reproduction error must remain `<=1e-9 kg/s`. For every candidate row RP1B then records candidate driving pressure and flow, absolute shift from canonical flow, and absolute distance from the previously frozen IF97 pressure-only counterfactual flow.

##### 7.5 deterministic repeat

Each candidate is reconstructed independently and reevaluated over VR2 + exact-v9 + seam rows. Resolution state, branch identity, phase, pressure, temperature and quality must repeat bit-for-bit for the RP1B evidence gate to complete.

##### 7.6 performance

Performance measurement uses:

```text
warm-up passes: 2
measured passes: 8
measured domain: 360 exact-v9 node states
additional seam worst-case pass: 1 x 1,280 probes
```

RP1B separately records candidate initialization time/allocation, steady-state median/p95/max resolve cost, seam maximum resolve cost and median steady-state allocation.

A candidate may exceed the frozen ceiling and RP1B may still complete successfully: that exceedance is a candidate result for RP1C, not corruption of the RP1B evidence harness.

#### 8. Evidence-completion versus candidate qualification

This distinction is binding.

RP1B itself passes when:

- all three frozen candidate identities execute;
- each preserves all 40 VR2 rows (39 inverse-applicable + 1 boundary-only) and produces the required 360 / 1,280 / 288 inverse/path result rows;
- deterministic repeat passes;
- timing evidence is finite;
- all nine RP1B artifacts are written.

RP1B does **not** fail merely because a candidate has unresolved states, phase mismatches, >25% error, misses the 10% planning target, or exceeds the performance ceiling. Those are candidate findings.

The candidate summary separately records `rp1c_selection_eligible`. The stricter Planning 1 target is exactly the already-authored one: hot/compressed-liquid M10-core pressure error `<=10%` together with 100% phase agreement on the frozen reference-classifiable exact-v9 node corpus. It remains selection evidence for RP1C and is not a replacement VR2 tolerance. That field is evidence only. RP1B does not choose a winner.

#### 9. Candidate qualification fields

For transparent RP1C input, each candidate summary records at least:

- `all_frozen_points_resolved`;
- `no_core_wrong_phase`;
- `vr2_blocking_ceiling_met`;
- `planning_target_met`;
- `deterministic_repeat`;
- `performance_ceiling_met`;
- `rp1c_selection_eligible`.

A candidate is marked selection-eligible only if the mandatory resolution, core phase, existing VR2 ceiling, Planning 1 target, deterministic and performance conditions are simultaneously met. RP1C remains responsible for the engineering choice and may still select no candidate.

#### 10. Outputs

The focused gate writes:

```text
01-contract-and-provenance.txt
02-candidate-vr2-error-map.csv
03-candidate-exact-v9-node-map.csv
04-candidate-seam-map.csv
05-candidate-hydraulic-replay.csv
06-candidate-performance.csv
07-candidate-complexity.csv
08-candidate-summary.csv
09-rp1b-summary.txt
```

#### 11. Explicit non-authorizations

RP1B does not authorize:

```text
RP1C selection before returned RP1B review
production source change
thermodynamic repair activation
thermodynamic tolerance change
exact-v9 change
existing closure reinterpretation
new exact-version activation
VR3
P3-R1
second replacement-long
```

#### 12. Execution

From PowerShell:

```powershell
.\scripts\run-m10-final-vr2-engineering-repair-planning1-rp1b.cmd
```

Return the complete folder:

```text
.\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b
```

before implementing RP1C or changing production thermodynamics.
