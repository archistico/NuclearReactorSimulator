# M10 Final — VR2 Engineering Repair Planning 1 — RP1C C4 Full-Domain Performance Confirmation 1 — Pre-Execution Review

## Verdict

**STATIC PRE-EXECUTION REVIEW: PASS, pending local Windows/.NET execution.**

The gate is intentionally performance-only and does not mutate C4, frozen corpora or thresholds.

## Reviewed failure modes

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

## Predicate scope

The review preserves the Refinement-3 interpretation rather than inventing a stricter one:

- exact-v9: median, p95, single-call max and median allocation;
- all seam sides: single-call max;
- both domains: every call resolved;
- R1 zero-allocation evidence remains frozen but is not generalized to unrelated full-domain fallback paths.

## Engineering-negative outcomes

`C4-FULL-DOMAIN-PERFORMANCE-NOT-CONFIRMED` is a valid returned classification. It must not be converted to runner failure if the evidence is complete and internally consistent.

## Environment limitation

This review environment does not provide the repository's .NET SDK/Windows PowerShell runtime, so the authoritative compile/test result remains the user's local execution. The candidate is therefore not declared execution-validated by this document.

## Conclusive static preflight

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

## Hotfix 1 — Windows PowerShell compatibility after first local preflight RED

The first local Windows run stopped during static validation before ordinary-suite or focused-evidence execution. The failure was infrastructural:

```text
Impossibile trovare un overload per "Contains" e il numero di argomenti: "2".
```

The validator used `String.Contains(string, StringComparison)`, an overload not exposed by the .NET Framework runtime used by Windows PowerShell 5.1. Hotfix 1 removes the overload dependency entirely. Marker checks now use the single-argument `String.Contains(string)` API; this remains case-sensitive for the exact markers frozen by the validator.

No C4 implementation, corpus, focused test, runner protocol, adjudication predicate, threshold or evidence count changed. The failed first attempt completed no focused evidence and therefore does not change any engineering classification or authority boundary.


## Hotfix 2 — C# collection type-contract compile repair after second local run

After Hotfix 1, static validation progressed and the ordinary Release build reached compilation of `NuclearReactorSimulator.Simulation.Tests`. The compiler reported four `CS1061` errors because `ReadCsvLines` was declared as `IReadOnlyList<string>` while the two loaders used `.Length`.

The helper already returns `ToArray()`. Hotfix 2 therefore makes the declared return type `string[]`; no loader logic, corpus content, ordering, timing path or performance predicate changes. The compiler output listed exactly the four occurrences caused by this mismatch. Static review confirms the other `.Length` expressions in the focused test operate on arrays.

The second failed run produced no focused full-domain evidence and does not alter authority or engineering classification.
