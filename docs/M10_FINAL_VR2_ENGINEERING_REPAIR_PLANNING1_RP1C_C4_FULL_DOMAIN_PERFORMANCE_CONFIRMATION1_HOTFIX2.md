# M10 Final — VR2 Engineering Repair Planning 1 — RP1C C4 Full-Domain Performance Confirmation 1 — Hotfix 2

## Scope

Hotfix 2 is a focused-test compile repair after Hotfix 1 successfully moved execution past static validation into the ordinary Release build/test phase.

The second local run failed while compiling `NuclearReactorSimulator.Simulation.Tests`; no focused confirmation process and no full-domain performance evidence completed.

## Failure

The compiler reported four `CS1061` errors in `M10FinalVr2EngineeringRepairPlanning1Rp1cC4FullDomainPerformanceConfirmation1Tests.cs` at the two CSV loaders.

`ReadCsvLines` was declared as returning `IReadOnlyList<string>`, while its callers use the array property `.Length`:

```text
lines.Length
```

`IReadOnlyList<string>` exposes `.Count`, not `.Length`.

The implementation of `ReadCsvLines` already terminates with `ToArray()`, so the declared interface type was less precise than the value actually returned.

## Repair

Hotfix 2 changes only the helper signature:

```csharp
private static string[] ReadCsvLines(string path, string expectedHeader)
```

The method body, filtering, header validation, row ordering and returned data are unchanged. The existing `.Length` callers therefore become type-correct without changing either loader algorithm.

This is deliberately preferred over changing four `.Length` expressions to `.Count`: the helper always returns an array, so the corrected signature reflects the actual contract and minimizes the semantic delta.

## Review

The reported compiler output contained exactly four errors, all from this single type mismatch. A full static scan of the focused test found no other `IReadOnlyList<string>`/`.Length` mismatch.

The remaining `.Length` usages are on arrays (`ExactRow[]`, `SeamRow[]`, timing sample arrays, split arrays or sorted `double[]`) and are therefore valid.

## Unchanged engineering contract

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

## Authority

The failed second run is a compile/infrastructure RED only. It produced no completed focused full-domain evidence and does not alter the C4 engineering classification.

The confirmation gate must be rerun from the beginning after applying Hotfix 2.
