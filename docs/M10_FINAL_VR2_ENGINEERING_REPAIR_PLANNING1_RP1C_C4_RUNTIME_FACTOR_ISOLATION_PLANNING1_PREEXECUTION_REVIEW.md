# RP1C C4 Runtime Factor Isolation Planning 1 — Pre-Execution Review

## Status

**STATIC-PREEXECUTION-REVIEW-PASS**

This review applies only to the planning gate. No runtime-factor experiment is implemented in this package.

## Findings

1. Attribution 1 returned evidence is complete and materially runtime-sensitive, but the strongest historical comparison changes more than one runtime factor.
2. The next gate therefore uses a chained single-factor matrix: TieredCompilation, then QuickJit, then QuickJitForLoops.
3. Dynamic PGO is deliberately excluded from this first isolation gate and requires a separate QJFL=1 comparator planning gate if still material afterward.
4. C4, exact-v9 corpus, `409.30666666666673 us` ceiling and `100 us` diagnostic-only floor remain immutable.
5. The future gate retains five fresh processes per mode and counterbalanced blocked-by-run execution.
6. Negative/inconclusive evidence must not become an xUnit or harness RED.
7. Caller `DOTNET_*` contamination remains fail-closed.
8. Windows PowerShell compatibility: validator uses no two-argument `String.Contains` overload, no `$variable:` interpolation hazard and invariant-culture parsing for evidence numbers.
9. The future implementation test and runner are absent from this planning package.
10. RP1C selection and every production/runtime authority remain false.

## Review conclusion

The planning contract is internally consistent and suitable for returned-planning adjudication before implementation.
