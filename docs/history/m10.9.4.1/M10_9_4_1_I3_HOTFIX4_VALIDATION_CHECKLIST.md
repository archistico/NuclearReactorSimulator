# M10.9.4.1-I.3 Hotfix 4 Classifier Fix 1 — Validation Checklist

Run:

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-phase-i-explicit-vs-corrected-branch-discontinuity-comparison-audit.cmd
```

Expected ordinary gate: build and complete `dotnet test` green.

Expected focused classification if the H.18/H.22 corrected path suppresses the I.3 failure class. The classifier is targeted-train based: every explicit drop must coincide with reverse flow on stop, control or admission; corrected must have zero targeted-train reverse flow:

```text
explicit-drops-with-targeted-reverse-flow=338/338
explicit-targeted-reverse-flow-that-are-drops=338/338
explicit-only-branch-discontinuity-classified=True
i3-hotfix4-comparison-audit-passes=True
```

Inspect:

- `artifacts\i3-hotfix4-explicit-vs-corrected-branch-discontinuity-comparison\01-phase-i-v2-v3-branch-discontinuity-comparison.summary.txt`
- `02-v2-v3-ten-millisecond-trace.csv`
- `03-generation-drop-comparison.csv`
- `04-drop-episodes.csv`

Do not freeze I.3 tolerance budgets and do not alter H.30 policy merely because the diagnostic passes. A separate reviewed policy decision is required.
