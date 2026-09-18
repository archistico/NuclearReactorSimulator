# M10 FINAL VR2 RP1C CONFIRMATION HISTORY DOSSIER
> Historical consolidation dossier. The source documents below were completed/superseded and had no executable references at consolidation time. Their content is retained here for provenance while the individual top-level files are removed.
## Source manifest
| Original top-level file | Lines | SHA-256 (normalized LF UTF-8) |
| --- | ---: | --- |
| `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_PLANNING1_PREEXECUTION_REVIEW.md` | 74 | `79F0044BDA2CFACAB76A5E5D4241AABC6F993A591F3DF4593CEC82CA9D5BB51A` |
| `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION1.md` | 134 | `422253CFFBB938D94173BE7659CE2045CD2DF198964D9394E93BDBAB48DDA4D5` |
| `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION1_HOTFIX1.md` | 44 | `98DDF3639335DEE1598AE55C0014F4DE8A755C2D10AE59EF91A0B697A5D6AABD` |
| `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION1_HOTFIX2.md` | 59 | `55B61182375FFF7CE02367BB228AA5FE4CA5E48889F39730CC1A0D9EFB768A5F` |
| `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION1_HOTFIX3.md` | 25 | `37868982FAF729503A56C8DD9F0EAC53BA90E0EA3230F22417F0C1578606C62B` |
| `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION1_PREEXECUTION_REVIEW.md` | 82 | `1ECE7A06555BF8D8EA571DD6003603B273151D35CDB781C8F9C4D14BFA67E152` |
| `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_EXACT_V9_WALL_CLOCK_TAIL_ATTRIBUTION_PLANNING1_RETURNED_EVIDENCE_ADJUDICATION.md` | 52 | `3F57CB843ACB6F9E78812504B2B4790ECA68535D5D5C8311DB1A0DEF0F31BEAC` |

## Retained source snapshots

---

## Source snapshot — `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_PLANNING1_PREEXECUTION_REVIEW.md`

### M10 Final — VR2 Engineering Repair Planning 1 — RP1C Planning 1 Pre-Execution Review

**Review status:** STATIC REVIEW COMPLETE — local Windows PowerShell execution still required.  
**Scope:** planning/selection-policy audit only. No RP1C selection implementation or production change is present.

#### 1. Review result

The first RP1C Planning 1 draft was intentionally not frozen as final because the review found a real evidence-boundary issue: returned C4 timing closes the 320-boundary R1 path, but C4 itself has not yet been timed over the complete exact-v9 + four-side seam corpus required by the corrected full RP1C performance predicate.

The corrected planning therefore does **not** mark C4 selection-ready yet.

#### 2. D3 historical eligibility correction

Refinement 2 returned D3 with `rp1c_selection_eligible=True`, but the boolean omitted the separately recorded seam worst case. Frozen evidence is:

```text
D3 resolve median = 14.7 us
D3 resolve p95    = 15.3 us
D3 resolve max    = 208.5 us
D3 seam max       = 16504.9 us
strict max        = 409.30666666666673 us
```

Refinement 3 already declared that future selection must include seam maximum. Planning 1 therefore preserves the old flag as provenance while reconstructing current D3 readiness as false.

#### 3. C4 evidence boundary

Returned C4 evidence is strong and remains fully accepted:

```text
1,967/1,967 semantic comparisons bit-equivalent
204,800 R1 timing calls
0 unresolved
0 candidate allocation
0 harness allocation
0 fallback
0 strict-max exceedance
worst R1 max = 329.9 us
```

The remaining gap is narrow but material for selection governance: C4 introduces new allocation-neutral mixture/liquid prefixes, so C3's historical exact-v9/non-R1 seam timing cannot be treated as measured C4 timing merely because the output state is bit-equivalent.

#### 4. Corrected next evidence gate

The planning now freezes a five-process immutable-C4 confirmation:

```text
360 exact-v9 rows x 64 measured passes = 23,040 calls/process
1,280 seam rows x 16 measured passes   = 20,480 calls/process
                                           43,520 calls/process
5 fresh processes                         217,600 calls total
```

The C4 candidate, corpus and thresholds remain immutable. The harness must remain allocation-neutral. Candidate allocation remains subject to the original median ceiling of 2816 B rather than an artificial zero-allocation requirement on every non-R1 path. All five processes must satisfy the corrected full performance predicate.

#### 5. RED-class hardening

The validator was reviewed specifically for recurring project failure modes:

- evidence paths and SHA-256 pins are independent of editable contract values;
- all frozen numeric CSV values are parsed explicitly with invariant culture, avoiding Italian-locale decimal parsing failures;
- exact floating-point contract constants are compared with explicit bounded checks rather than fragile string equality;
- the PowerShell validator is ASCII-only for Windows PowerShell 5.1 stability;
- the future confirmation test and runner are required to be absent from this planning-only package;
- exactly four planning artifacts are required;
- `SELECT-NONE` remains mandatory;
- no authority is inferred from candidate uniqueness;
- negative future performance evidence is not defined as an infrastructure/xUnit failure.

#### 6. Authority after a local planning PASS

A local `PASS-AS-AUTHORED` freezes only this planning contract. It does not authorize C4 full-domain confirmation until the complete planning artifact folder has been returned and adjudicated.

RP1C selection, production repair, thresholds, exact-v9, VR3, P3-R1 and second replacement-long remain blocked.

---

## Source snapshot — `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION1.md`

### M10 Final — VR2 Engineering Repair Planning 1 — RP1C C4 Full-Domain Performance Confirmation 1

#### Purpose

This gate closes the final performance-evidence gap identified by RP1C Planning 1 before any RP1C selection can be considered.

It measures the **immutable** `C4-ALLOCATION-NEUTRAL-VAPOR-SEAM-COMPLETE-SURROGATE` over the frozen RP1A exact-v9 and seam corpora. It does not modify C4, regenerate the corpus, change thresholds, select a candidate or alter production/runtime behavior.

#### Prerequisite adjudication

The returned RP1C Planning 1 artifact set is adjudicated PASS. It freezes:

- D3 not selection-ready because its historical seam maximum `16504.9 us` exceeds the unchanged `409.30666666666673 us` maximum;
- C4 physical/R1 qualification complete;
- C4 full-domain performance confirmation pending;
- selection-ready count `0` before this gate;
- `SELECT-C4 | SELECT-NONE` as the later decision space only after green returned confirmation evidence.

#### Frozen protocol

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

#### Corrected performance predicate

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

#### Evidence tree

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

#### Classification

The aggregate classification is one of:

```text
C4-FULL-DOMAIN-PERFORMANCE-CONFIRMED
C4-FULL-DOMAIN-PERFORMANCE-NOT-CONFIRMED
```

Even the positive classification does not itself authorize RP1C selection. Complete returned confirmation evidence must first be adjudicated. Only then may a separate RP1C selection gate consider `SELECT-C4` or `SELECT-NONE`.

#### Authority boundary

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


#### Hotfix history

- **Hotfix 1:** Windows PowerShell 5.1 compatibility; removes the unsupported two-argument `String.Contains` overload from the static validator.
- **Hotfix 2:** ordinary Release compile repair; aligns `ReadCsvLines` declaration with its actual `string[]` return value so the existing array `.Length` usages compile.
- **Hotfix 3:** fixes Windows PowerShell variable interpolation in the adjudicator (`${runIndex}:`) and adds adjudication-only completion over already-produced per-process evidence.

Neither hotfix changes C4 semantics, the frozen corpora, thresholds, timing protocol, evidence classification or authority boundary.


#### Returned evidence adjudication

The complete 30-file evidence tree has been returned and independently reconciled against all 25 per-process files.

Final classification:

```text
C4-FULL-DOMAIN-PERFORMANCE-NOT-CONFIRMED
```

Runs 1–3 satisfy the complete corrected predicate. Runs 4–5 fail only the exact-v9 single-call maximum:

```text
run 4 exact-v9 max = 1027.1 us
run 5 exact-v9 max = 796.1 us
strict ceiling     = 409.30666666666673 us
```

Both misses are `C2-MIXTURE-PREFIX`, allocate `0 B` on the measured call and occur without measured-region GC collections. Every seam side remains below the strict maximum; exact-v9 median/p95 and median candidate allocation are green in all five processes.

The negative classification is therefore accepted engineering evidence, not a runner failure. RP1C selection remains blocked. See `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION1_RETURNED_EVIDENCE_ADJUDICATION.md`.

---

## Source snapshot — `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION1_HOTFIX1.md`

### M10 Final — VR2 Engineering Repair Planning 1 — RP1C C4 Full-Domain Performance Confirmation 1 — Hotfix 1

#### Scope

Hotfix 1 is a validator-only compatibility repair for the confirmation gate. The first local run failed in step `[1/4]` before ordinary-suite execution and before any focused evidence was produced.

#### Failure

Windows PowerShell reported that no two-argument overload of `String.Contains` could be found for:

```text
$testText.Contains('Assert.True(strictMet', [StringComparison]::Ordinal)
```

That overload is available on newer .NET runtimes but is not available to Windows PowerShell 5.1 on .NET Framework.

#### Repair

The validator now uses only the long-established one-argument API:

```text
$Text.Contains($Needle)
$testText.Contains('Assert.True(strictMet')
```

The checks remain exact and case-sensitive for the frozen markers. The validator no longer contains any `StringComparison` dependency.

#### Unchanged engineering contract

Hotfix 1 does not change:

- the immutable C4 candidate;
- exact-v9 or seam corpora;
- the focused C# confirmation test;
- five-process protocol or 217,600 measured-call count;
- 30-file evidence contract;
- median, p95, max or allocation ceilings;
- the evidence adjudicator;
- RP1C selection authority;
- production/runtime code, thresholds, exact-v9, VR3, P3-R1 or second replacement-long authority.

#### Authority

The failed first run is classified as an infrastructure/preflight RED only. It produced no completed focused evidence and grants no RP1C selection or production authority. The confirmation gate must be rerun from the beginning after applying Hotfix 1.

---

## Source snapshot — `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION1_HOTFIX2.md`

### M10 Final — VR2 Engineering Repair Planning 1 — RP1C C4 Full-Domain Performance Confirmation 1 — Hotfix 2

#### Scope

Hotfix 2 is a focused-test compile repair after Hotfix 1 successfully moved execution past static validation into the ordinary Release build/test phase.

The second local run failed while compiling `NuclearReactorSimulator.Simulation.Tests`; no focused confirmation process and no full-domain performance evidence completed.

#### Failure

The compiler reported four `CS1061` errors in `M10FinalVr2EngineeringRepairPlanning1Rp1cC4FullDomainPerformanceConfirmation1Tests.cs` at the two CSV loaders.

`ReadCsvLines` was declared as returning `IReadOnlyList<string>`, while its callers use the array property `.Length`:

```text
lines.Length
```

`IReadOnlyList<string>` exposes `.Count`, not `.Length`.

The implementation of `ReadCsvLines` already terminates with `ToArray()`, so the declared interface type was less precise than the value actually returned.

#### Repair

Hotfix 2 changes only the helper signature:

```csharp
private static string[] ReadCsvLines(string path, string expectedHeader)
```

The method body, filtering, header validation, row ordering and returned data are unchanged. The existing `.Length` callers therefore become type-correct without changing either loader algorithm.

This is deliberately preferred over changing four `.Length` expressions to `.Count`: the helper always returns an array, so the corrected signature reflects the actual contract and minimizes the semantic delta.

#### Review

The reported compiler output contained exactly four errors, all from this single type mismatch. A full static scan of the focused test found no other `IReadOnlyList<string>`/`.Length` mismatch.

The remaining `.Length` usages are on arrays (`ExactRow[]`, `SeamRow[]`, timing sample arrays, split arrays or sorted `double[]`) and are therefore valid.

#### Unchanged engineering contract

Hotfix 2 does not change:

- C4 candidate source or semantics;
- frozen exact-v9 or seam corpora;
- warm-up/measured-pass counts;
- five-process / 217,600 measured-call protocol;
- corrected median, p95, max or allocation ceilings;
- measured-region allocation instrumentation;
- runner or evidence adjudicator;
- 30-file evidence contract;
- RP1C selection or any production/runtime authority.

#### Authority

The failed second run is a compile/infrastructure RED only. It produced no completed focused full-domain evidence and does not alter the C4 engineering classification.

The confirmation gate must be rerun from the beginning after applying Hotfix 2.

---

## Source snapshot — `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION1_HOTFIX3.md`

### M10 Final VR2 RP1C C4 Full-Domain Performance Confirmation 1 — Hotfix 3

#### Scope

Hotfix 3 repairs only the Windows PowerShell parser failure in the aggregate evidence adjudicator. The failed line interpolated `$runIndex:` inside a double-quoted string; Windows PowerShell parses the colon as part of a variable reference. The corrected form is `${runIndex}:`.

The five focused confirmation processes had already completed before the parser failure. Their 25 per-process artifacts are preserved and remain the evidence source. This hotfix therefore adds an adjudication-only runner that consumes those files without rebuilding or rerunning the 217,600 measured calls.

#### Frozen boundaries

No C4 source, production source, focused test, corpus, threshold, allocation policy, full-domain predicate, process count or measurement count changes. RP1C selection and production repair remain unauthorized.

#### Expected classification from independently reconstructed returned process evidence

The returned 25 process artifacts are structurally complete. Independent reconstruction shows all five processes satisfy median, p95, median-allocation, seam-max, unresolved and harness-allocation criteria. Runs 2 and 5 each contain one exact-v9 single-call exceedance of the immutable 409.30666666666673 us ceiling (979.4 us and 894.8 us respectively). Therefore the adjudicator is expected to emit `C4-FULL-DOMAIN-PERFORMANCE-NOT-CONFIRMED`. This is an engineering-negative classification, not a harness failure.

#### Execution

From the repository root, after replacing the project with this Hotfix 3 candidate and preserving the existing artifact directory, run:

```powershell
.\scripts\run-m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation1-adjudication-only.cmd
```

Return the complete artifact folder after the command completes.

---

## Source snapshot — `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERFORMANCE_CONFIRMATION1_PREEXECUTION_REVIEW.md`

### M10 Final — VR2 Engineering Repair Planning 1 — RP1C C4 Full-Domain Performance Confirmation 1 — Pre-Execution Review

#### Verdict

**STATIC PRE-EXECUTION REVIEW: PASS, pending local Windows/.NET execution.**

The gate is intentionally performance-only and does not mutate C4, frozen corpora or thresholds.

#### Reviewed failure modes

The review explicitly checks the recurrent VR2 failure classes:

- returned-planning artifacts are frozen and hash-pinned independently;
- C4 source is hash-pinned and unchanged;
- exact-v9/seam corpora and performance baseline are hash-pinned;
- one explicit xUnit method is used and is excluded from the ordinary suite unless explicitly requested;
- runner resets the artifact root once, while each process resets only its own `process-XX` directory;
- run index is mandatory and limited to `1..5`;
- exact-v9 and seam sample arrays are preallocated before the full GC baseline;
- timing/GC/allocation primitives are primed before the measured region;
- no assertion turns a valid negative performance classification into a focused-test RED;
- harness allocation is independently reconciled and must be exactly zero for valid evidence;
- the adjudicator recomputes median, nearest-rank p95, max, allocation median and seam maxima from raw CSV rather than trusting process summaries;
- all numeric parsing in PowerShell is invariant-culture;
- five complete process directories and exactly 30 files are required.

#### Predicate scope

The review preserves the Refinement-3 interpretation rather than inventing a stricter one:

- exact-v9: median, p95, single-call max and median allocation;
- all seam sides: single-call max;
- both domains: every call resolved;
- R1 zero-allocation evidence remains frozen but is not generalized to unrelated full-domain fallback paths.

#### Engineering-negative outcomes

`C4-FULL-DOMAIN-PERFORMANCE-NOT-CONFIRMED` is a valid returned classification. It must not be converted to runner failure if the evidence is complete and internally consistent.

#### Environment limitation

This review environment does not provide the repository's .NET SDK/Windows PowerShell runtime, so the authoritative compile/test result remains the user's local execution. The candidate is therefore not declared execution-validated by this document.

#### Conclusive static preflight

The final candidate was checked against the RP1C Planning 1 baseline with these results:

```text
src byte-identical = True
historical tests byte-identical = True
C4 candidate byte-identical = True
returned planning artifact normalized hashes = 4/4 PASS
RP1A exact-v9/seam/performance pins = 3/3 PASS
measured-region obvious harness-allocation scan = PASS
contract semantics/counts = PASS
validator/adjudicator/runner ASCII = PASS
stale active-status scan = PASS
generated bin/obj/artifacts directories = none
```

The local Windows/.NET execution remains authoritative for build/analyzer/runtime validation.

#### Hotfix 1 — Windows PowerShell compatibility after first local preflight RED

The first local Windows run stopped during static validation before ordinary-suite or focused-evidence execution. The failure was infrastructural:

```text
Impossibile trovare un overload per "Contains" e il numero di argomenti: "2".
```

The validator used `String.Contains(string, StringComparison)`, an overload not exposed by the .NET Framework runtime used by Windows PowerShell 5.1. Hotfix 1 removes the overload dependency entirely. Marker checks now use the single-argument `String.Contains(string)` API; this remains case-sensitive for the exact markers frozen by the validator.

No C4 implementation, corpus, focused test, runner protocol, adjudication predicate, threshold or evidence count changed. The failed first attempt completed no focused evidence and therefore does not change any engineering classification or authority boundary.


#### Hotfix 2 — C# collection type-contract compile repair after second local run

After Hotfix 1, static validation progressed and the ordinary Release build reached compilation of `NuclearReactorSimulator.Simulation.Tests`. The compiler reported four `CS1061` errors because `ReadCsvLines` was declared as `IReadOnlyList<string>` while the two loaders used `.Length`.

The helper already returns `ToArray()`. Hotfix 2 therefore makes the declared return type `string[]`; no loader logic, corpus content, ordering, timing path or performance predicate changes. The compiler output listed exactly the four occurrences caused by this mismatch. Static review confirms the other `.Length` expressions in the focused test operate on arrays.

The second failed run produced no focused full-domain evidence and does not alter authority or engineering classification.

---

## Source snapshot — `M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1_RP1C_C4_EXACT_V9_WALL_CLOCK_TAIL_ATTRIBUTION_PLANNING1_RETURNED_EVIDENCE_ADJUDICATION.md`

### M10 Final VR2 Engineering Repair Planning 1 — RP1C C4 Exact-v9 Wall-Clock Tail Attribution Planning 1 Returned-Evidence Adjudication

#### Status

**PASS — returned planning evidence accepted.**

The four returned planning artifacts are complete and internally consistent. They freeze an evidence-only four-mode runtime attribution experiment over immutable C4 and the frozen exact-v9 corpus. They do not prove tiering/PGO causality and do not authorize RP1C selection or production repair.

#### Returned planning evidence

- `01-contract-and-provenance.txt`: `PASS-AS-AUTHORED`
- `02-tail-attribution-plan-summary.txt`: planning contract frozen with no causal promotion
- `03-runtime-mode-matrix.csv`: four runtime modes × five fresh processes
- `04-preexecution-review.txt`: static pre-execution review PASS

The frozen protocol is:

- 4 runtime modes
- 5 fresh processes per mode
- 20 total fresh processes
- 360 exact-v9 rows
- 16 warm-up passes
- 64 measured passes
- 23,040 measured calls per process
- 460,800 measured calls total
- unchanged strict max ceiling `409.30666666666673 us`
- diagnostic tail floor `100 us`, explicitly **not** a qualification threshold
- 86 required evidence files

The caller environment must fail closed when any of the five planned `DOTNET_*` compilation variables is already set.

#### Authority decision

This adjudication authorizes **only** implementation and execution of:

`RP1C-C4-EXACT-V9-WALL-CLOCK-TAIL-ATTRIBUTION1`

The gate remains evidence-only. No automatic causal interpretation is authorized. No result from the runner may by itself select C4.

The following remain unauthorized:

- RP1C selection
- production repair
- threshold/tolerance changes
- exact-v9 changes
- VR3
- P3-R1
- second replacement-long baseline

#### Next action

Implement and execute the separately versioned Attribution 1 evidence gate, return the complete 86-file evidence tree, and perform a new returned-evidence adjudication before any causal conclusion or RP1C selection planning.


---

## Source snapshot — Runtime Factor Isolation 1 returned-evidence adjudication and Dynamic PGO Comparator Planning 1

Runtime Factor Isolation 1 returned a complete 86-file, 20-process, 460,800-call tree. Returned adjudication accepts evidence integrity and the single-factor chain. TieredCompilation is materially causal for the gross slowdown under the frozen A↔B intervention; QuickJit is not promoted as a rare-tail causal owner; QuickJitForLoops has no material tail benefit. Dynamic PGO remains materially unresolved only for the QJFL-enabled configuration needed before Branch A2.

The successor is planning-only `RP1C-C4-DYNAMIC-PGO-COMPARATOR-PLANNING1`. Its future gate is `RP1C-C4-EXACT-V9-DYNAMIC-PGO-COMPARATOR1`, 2 modes × 5 fresh processes, 230,400 calls, changing only `DOTNET_TieredPGO`. Ambient/default equivalence, Runtime Configuration Impact Assessment, RP1C selection and production/runtime change remain unauthorized.


## Dynamic PGO Comparator 1 returned adjudication — 2026-09-17

The complete 46-file comparator tree is returned and adjudicated. With TieredCompilation, QuickJit, QuickJitForLoops and ReadyToRun fixed ON, Dynamic PGO ON reduces the central exact-v9 timing distribution in every counterbalanced block; all 360 row medians improve. Aggregate process medians change 4.7→2.1 us and process p95 changes 5.4→2.5 us. Sparse tail evidence changes 13→8 calls above 100 us and 1→0 strict exceedances; rare strict-tail causality is therefore not promoted. The successor is planning-only Branch A2 Runtime Configuration Impact Assessment Planning 1.
