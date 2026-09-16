# M10 Final — VR2 Engineering Repair Planning 1 — RP1B C4 Planning 1 REV2 — Second Pre-Execution Review

**Status:** CANDIDATE REVIEW COMPLETE / STATIC PRE-EXECUTION HARDENING.  
**Scope:** planning, source-attribution and future evidence-contract review only. No C4 implementation is contained or authorized by this document.

## 1. Review result

The REV1 planning runner/validator was mechanically strong enough to avoid the known marker, PowerShell-double, Unicode, path-separator and generated-directory RED classes, but the second review found two deeper engineering defects in the authored future C4 plan plus three process-contract ambiguities. REV2 closes them before the planning audit is executed.

## 2. Blocking finding A — mixture-only topology was structurally unable to meet 0 B/call on R1

Frozen Refinement-2 C3 evidence contains exactly 320 C3 R1-side rows. All 320 resolve as `SubcooledLiquid`:

- 310 resolve as `REGION-1-NEAR-BOUNDARY-C2`;
- 10 resolve as `REGION-1-TABLE-C2`;
- 0 resolve as Region 4 on the C3 R1 timing path.

Therefore the REV1 mixture-only pre-resolver would correctly decline every R1 state, then enter immutable C2. Immutable C2 always invokes `_saturation.TryResolveMixture(...)` before the liquid table/near-boundary paths, preserving the allocation C4 is meant to remove. The REV1 zero-byte objective was therefore not implementable under its own topology.

REV2 replaces that topology with a frozen C3-precedence/C2-prefix design: C3 superheated-vapor discriminator first; allocation-neutral C2 mixture; allocation-neutral C2 liquid table; allocation-neutral C2 near-boundary liquid; immutable C2 fallback only for remaining paths; C3 saturated-vapor fallback last. C2 vapor resolution is not cloned. R1 timing requires zero fallback invocations.

## 3. Blocking finding B — historical R5 whole-region harness was not allocation-neutral

Refinement 5 measured candidate allocation correctly around each `TryResolve` call, so the returned `416 B/call` remains valid. It also correctly recorded that the repeated boundary-3 exceedance coincided with a Gen0 collection inside that candidate-call window.

However, after the post-warm-up full-GC baseline, R5 also:

- allocated the `List<R1CallTimingSample>` backing storage;
- allocated a reference-type `R1CallTimingSample` after every measured call.

Consequently the returned evidence supports **GC correlation**, but it does not prove that C3's 416 bytes alone determined the precise Gen0 cadence. REV2 does not rewrite or weaken the validated `C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED` machine classification; it narrows the causal claim used to design C4.

The future C4 timing harness must preallocate value-type storage before the post-warm-up full GC and remain current-thread allocation-neutral throughout the complete measured region apart from the candidate call itself. Whole-region allocation minus the sum of candidate-call deltas must equal zero.

## 4. Additional source risk — interface enumeration

`Rp1bRefinementSaturationTable.TryBuildReachabilityBoundary` remains a concrete allocation site because it creates a temporary `List<double>` and applies LINQ ordering. The second review also identifies `TryResolveFromTable(IReadOnlyList<...>)` using `foreach` as an additional allocation risk through interface enumeration. REV2 does not assert an exact byte contribution for either source site. The C4 R1 prefix simply forbids both risk shapes by using indexed loops and no resolve-time temporary collections/LINQ/boxing.

## 5. Process-contract corrections

REV2 freezes these additional rules before implementation:

1. semantic equivalence runs in one dedicated focused process and never contributes timing evidence;
2. wall-clock/allocation timing uses ten additional fresh processes, giving 11 focused `dotnet test` invocations total;
3. warm-up pass indices are `0..15`; measured pass indices restart at `0..63`, exactly matching Refinement 5;
4. the runner deletes the C4 artifact root once; a focused test process can reset only the files/directory it owns;
5. the future measured-loop recorder uses preallocated value-type storage and records resolution-path telemetry, with zero immutable-C2 fallback calls required on R1.

## 6. Non-findings / checks that remain green

The review re-confirms:

- C2 and C3 SHA-256 pins match the frozen sources;
- `src/` and `tests/` remain byte-identical to the validated Refinement-5 Handoff 2 package;
- no C4 implementation or future C4 focused test/runner exists in this planning package;
- the planning validator remains ASCII-only and Windows PowerShell 5.1 compatible by construction;
- float contract values are checked with explicit tolerance rather than textual or direct object inequality;
- Lane-B strides are coprime with 320 and cover all 320 identities for each warm-up and measured pass;
- the future required evidence tree is corrected to 59 files: 9 aggregate + 10 process directories × 5 files, because state and derived-hydraulic semantic evidence use distinct schemas;
- RP1C, production repair, threshold/tolerance changes, exact-v9, VR3, P3-R1 and second replacement-long remain unauthorized.

## 7. Exit condition

Execute only the REV2 planning runner. A local `PASS-AS-AUTHORED` freezes only the candidate planning evidence. Return the complete planning artifact folder for adjudication; only that returned-evidence adjudication may authorize a separately versioned **test-only C4 implementation/evidence candidate**. It does not authorize RP1C or production changes.
## Additional hardening from the second pass

The second pass also removed four procedural false-RED risks before execution:

- historical source/test SHA-256 pins are evaluated over UTF-8 text with line endings normalized to LF, so `core.autocrlf` or CRLF/LF checkout differences cannot invalidate an otherwise identical C2/C3/R5 source;
- Refinement 5 remains the only C3 allocation control; no extra C3 control may run inside the semantic or timing processes;
- semantic mismatch cannot short-circuit timing: negative engineering outcomes still produce the complete 59-file tree before classification;
- measured resolution-path telemetry is stored as a value-type code and formatted to text only after timing, so the fallback proof cannot itself allocate inside the measurement region.
- semantic and timing evidence are frozen as two different xUnit-v3 explicit methods, with exact method filters, `--parallel none` and `--no-build`; the ordinary gate executes first with C4 environment variables unset.

Aggregate evidence ownership is also explicit: semantic process -> files 02-04; runner/adjudicator -> file 01 and files 05-09; timing processes -> only their own lane/process directory.
## Semantic-schema correction

A further schema review found that the 288 hydraulic-context observations are derived path results, not thermodynamic states. Treating them with the same `region/phase/temperature/pressure` CSV schema would have created an implementation-time mismatch. REV2 therefore freezes two semantic CSVs: 1,679 state observations and 288 hydraulic observations. The latter compare derived driving pressure, flow and sign-change semantics bit-for-bit. The total remains 1,967 comparisons. The 39-row VR2 count is now traced explicitly to the 40-row source corpus minus the single `VR2-SAT-360C-PONLY` boundary-only/non-inverse row.


## 8. Conclusive pre-execution review closure

The concluding review found and removed two certain procedural REDs before local execution: the machine contract had advanced to schema `v4` while the validator still required/output `v3`, and the validator still referenced a superseded authority property that no longer existed in the contract. Both are now aligned to the current REV2 contract.

The final hardening also removes three false-PASS/late-RED risks. C2, C3 and the historical Refinement-5 timing test are now checked against validator-authoritative normalized-text SHA-256 constants rather than trusting hash values supplied only by the editable JSON contract. The returned Refinement-5 prerequisite is checked for exact classification, counts, `c4-planning-justified=True`, exact aggregate/process artifact shape and the unique boundary-3 cross-process confirmation row. Finally, the median/p95/max calculation is machine-readable and frozen to the same deterministic rules documented by Planning 1: ascending sort, ordinary even-sample median, nearest-rank p95 at `ceil(0.95 * N) - 1`, and strict `elapsed_us > max_ceiling` exceedance semantics.

The local planning audit remains an evidence-generation gate only. A local `PASS-AS-AUTHORED` does not authorize C4 implementation. The complete planning artifact folder must be returned and adjudicated first; only that returned-evidence adjudication may authorize a separately versioned test-only C4 implementation/evidence candidate. RP1C and all production/runtime authority remain unchanged and unauthorized.

Conclusive static review result: **GO FOR LOCAL REV2 PLANNING AUDIT**, subject to the remaining environmental limitation that this review environment cannot execute Windows PowerShell 5.1 or .NET. No C4 implementation is present; `src/` and `tests/` remain byte-identical to the validated Handoff 2 baseline.
