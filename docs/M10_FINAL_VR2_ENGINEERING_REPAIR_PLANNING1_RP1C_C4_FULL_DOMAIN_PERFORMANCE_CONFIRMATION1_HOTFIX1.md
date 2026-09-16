# M10 Final — VR2 Engineering Repair Planning 1 — RP1C C4 Full-Domain Performance Confirmation 1 — Hotfix 1

## Scope

Hotfix 1 is a validator-only compatibility repair for the confirmation gate. The first local run failed in step `[1/4]` before ordinary-suite execution and before any focused evidence was produced.

## Failure

Windows PowerShell reported that no two-argument overload of `String.Contains` could be found for:

```text
$testText.Contains('Assert.True(strictMet', [StringComparison]::Ordinal)
```

That overload is available on newer .NET runtimes but is not available to Windows PowerShell 5.1 on .NET Framework.

## Repair

The validator now uses only the long-established one-argument API:

```text
$Text.Contains($Needle)
$testText.Contains('Assert.True(strictMet')
```

The checks remain exact and case-sensitive for the frozen markers. The validator no longer contains any `StringComparison` dependency.

## Unchanged engineering contract

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

## Authority

The failed first run is classified as an infrastructure/preflight RED only. It produced no completed focused evidence and grants no RP1C selection or production authority. The confirmation gate must be rerun from the beginning after applying Hotfix 1.
