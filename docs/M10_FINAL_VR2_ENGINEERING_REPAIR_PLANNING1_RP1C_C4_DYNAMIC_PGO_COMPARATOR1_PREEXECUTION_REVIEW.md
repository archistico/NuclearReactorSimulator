# RP1C C4 Exact-v9 Dynamic PGO Comparator 1 - Pre-Execution Review

## Status

**STATIC-PREEXECUTION-REVIEW-PASS**

This review covers only the candidate implementation. It does not pre-adjudicate returned timing evidence.

## Anti-RED findings

1. Returned Dynamic PGO Comparator Planning 1 artifacts are frozen and hash-pinned.
2. C4, exact-v9 corpus and frozen performance baseline remain hash-pinned and unchanged.
3. The test is explicit and ordinary CI runs with the comparator opt-in unset.
4. The two modes differ only in `DOTNET_TieredPGO`.
5. The runner executes exactly ten fresh processes in the frozen OFF/ON counterbalanced schedule.
6. Caller DOTNET compilation variables are fail-closed and are not silently cleared.
7. The adjudicator validates exactly 23,040 timing rows per process and all 64 x 360 call identities/order against the frozen corpus.
8. Per-call timing, allocation, resolution state/path and diagnostic/strict flags are retained.
9. Runtime context captures all five controlled DOTNET variables plus allocation and GC deltas.
10. Harness allocation must be zero; allocation or matrix-integrity defects are infrastructure RED.
11. Strict exceedances, PGO OFF/ON differences, neutral results or inconclusive results are engineering evidence and do not fail the harness by themselves.
12. Aggregate output is exactly six files, for 46 required files total.
13. `100 us` remains diagnostic only; the strict max remains the frozen baseline value.
14. No ambient/unset mode, effective-default test or Runtime Configuration Impact Assessment is included.
15. No automatic PGO causal promotion or RP1C selection is performed.
16. `src/` is unchanged.

## Windows PowerShell 5.1 compatibility review

Validator, adjudicator and CMD runner are required to remain 7-bit ASCII. PowerShell uses explicit UTF-8 reads for documentation and frozen text artifacts, invariant-culture numeric parsing, no PowerShell 7-only syntax, no `StringComparison` overload dependency and no variable-colon interpolation hazard.

## `Require-Text` anti-false-RED review

Every static `Require-Text` marker in the validator is checked against the exact target file before packaging. No marker depends on wrapping, locale-formatted generated numbers or non-ASCII source literals.

## Conclusion

Candidate is suitable for local execution of `RP1C-C4-EXACT-V9-DYNAMIC-PGO-COMPARATOR1` only. Returned evidence is mandatory before PGO causal adjudication or Branch A2 planning.
