# M10 Final VR2 RP1C C4 Full-Domain Performance Confirmation 1 — Hotfix 3

## Scope

Hotfix 3 repairs only the Windows PowerShell parser failure in the aggregate evidence adjudicator. The failed line interpolated `$runIndex:` inside a double-quoted string; Windows PowerShell parses the colon as part of a variable reference. The corrected form is `${runIndex}:`.

The five focused confirmation processes had already completed before the parser failure. Their 25 per-process artifacts are preserved and remain the evidence source. This hotfix therefore adds an adjudication-only runner that consumes those files without rebuilding or rerunning the 217,600 measured calls.

## Frozen boundaries

No C4 source, production source, focused test, corpus, threshold, allocation policy, full-domain predicate, process count or measurement count changes. RP1C selection and production repair remain unauthorized.

## Expected classification from independently reconstructed returned process evidence

The returned 25 process artifacts are structurally complete. Independent reconstruction shows all five processes satisfy median, p95, median-allocation, seam-max, unresolved and harness-allocation criteria. Runs 2 and 5 each contain one exact-v9 single-call exceedance of the immutable 409.30666666666673 us ceiling (979.4 us and 894.8 us respectively). Therefore the adjudicator is expected to emit `C4-FULL-DOMAIN-PERFORMANCE-NOT-CONFIRMED`. This is an engineering-negative classification, not a harness failure.

## Execution

From the repository root, after replacing the project with this Hotfix 3 candidate and preserving the existing artifact directory, run:

```powershell
.\scripts\run-m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation1-adjudication-only.cmd
```

Return the complete artifact folder after the command completes.
