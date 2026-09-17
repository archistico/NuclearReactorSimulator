# M10 Final — RP1C C4 Exact-v9 Wall-Clock Tail Attribution Planning 1 — Pre-Execution Review

## Status

`STATIC-PREEXECUTION-REVIEW-PASS`

This review applies only to the planning/static-audit candidate. No attribution test implementation is present.

## Finding 1 — the open result is narrow

Returned FDPC1 evidence is complete and valid. The negative classification is owned only by two exact-v9 single-call maxima. Seam, median, p95, allocation and harness integrity remain green.

The planning therefore does not reopen seam, R1 allocation closure, thermodynamic semantics or thresholds.

## Finding 2 — a runtime/tiering hypothesis is justified but unproven

Exactly five exact-v9 calls exceed `100 us`, one in each returned process, at measured passes `15,17,17,18,18`. All use `C2-MIXTURE-PREFIX`, allocate `0 B` on the measured call and occur without measured-region GC collections.

This justifies a controlled runtime-mode experiment. It does not justify a causal label.

## Finding 3 — environment contamination must fail closed

The previous FDPC1 runtime-context files did not record JIT/tiering environment variables. The new runner therefore requires the five documented `DOTNET_*` compilation variables to be absent from the calling environment before it creates a control process.

It must not silently clear parent configuration.

## Finding 4 — use process isolation, not project mutation

The future four-mode protocol uses child-process environment settings only. Project files, runtimeconfig, C4, C2/C3 and corpora remain immutable.

This is appropriate for .NET 10 because documented runtime configuration environment variables can control tiered compilation/PGO/QuickJIT/ReadyToRun for the child process.

## Finding 5 — `100 us` is diagnostic only

The strict engineering maximum remains `409.30666666666673 us`. The `100 us` floor exists only to reconstruct the returned five-sample tail population and compare mode/pass/path distributions.

The validator must reject wording that promotes `100 us` to a new qualification threshold.

## Finding 6 — no automatic causal classification

The planned future gate is evidence-only. A difference between modes can support a later attribution adjudication, but the runner cannot promote tiering, Dynamic PGO or scheduling to proven cause.

Likewise it cannot authorize RP1C selection.

## Finding 7 — Windows PowerShell compatibility

The planning validator is constrained to Windows PowerShell-compatible constructs:

- one-argument `String.Contains` only;
- no `StringComparison` overload dependency;
- no ambiguous `$variable:` interpolation;
- invariant-culture numeric parsing;
- ASCII-only PowerShell/CMD files;
- normalized-LF SHA-256 pins for frozen text evidence.

This explicitly incorporates the infrastructure REDs already encountered in FDPC1.

## Conclusion

The planning candidate is suitable for local static audit if and only if it preserves the returned FDPC1 evidence, immutable C4/corpus identities, four-mode protocol, environment fail-closed rule, unchanged engineering ceiling and all authority blocks.
