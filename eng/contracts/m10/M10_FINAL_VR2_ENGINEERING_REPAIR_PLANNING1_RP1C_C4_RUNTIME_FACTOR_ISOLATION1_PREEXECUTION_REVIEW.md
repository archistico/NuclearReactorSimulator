# RP1C C4 Runtime Factor Isolation 1 — Pre-Execution Review

## Status

**STATIC-PREEXECUTION-REVIEW-PASS — HOTFIX1 RECHECK PASS**

This review covers the executable candidate before local execution. It does not claim runtime evidence and grants no new authority.

## Review findings

1. The candidate starts from the returned Runtime Factor Isolation Planning 1 contract and frozen four-file planning evidence, not from a partial implementation workspace.
2. C4 and the RP1A exact-v9 corpus remain immutable and are pinned by normalized-LF SHA-256.
3. The strict max remains `409.30666666666673 us`; `100 us` is diagnostic-only and never becomes an acceptance threshold.
4. The four runtime modes match the returned planning matrix exactly. `DOTNET_TieredPGO=0` and `DOTNET_ReadyToRun=1` are fixed in every mode.
5. A↔B changes only `DOTNET_TieredCompilation`; B↔C only `DOTNET_TC_QuickJit`; C↔D only `DOTNET_TC_QuickJitForLoops`.
6. The runner uses 20 fresh processes in the frozen counterbalanced blocked-by-run sequence and fails closed on caller `DOTNET_*` contamination.
7. Every process records raw per-call evidence, runtime context and a process summary. The adjudicator verifies all `64 × 360` call identities per process against the frozen corpus.
8. The adjudicator produces per-process run evidence, per-mode aggregate evidence, three single-factor contrast rows and tail row/path provenance without promoting causality.
9. Negative or inconclusive engineering evidence does not produce xUnit RED. Infrastructure/evidence-integrity failures remain RED.
10. The complete tree is constrained to exactly 86 files.
11. Dynamic PGO is excluded from the gate. No PGO causal claim may be inferred from Runtime Factor Isolation 1.
12. Production `src/` is unchanged; the gate is test-only/evidence-only.

## Windows PowerShell 5.1 compatibility review

**HOTFIX1 RECHECK PASS**

The first local attempt exposed one pre-execution review defect before build or focused evidence: the validator source was UTF-8 without BOM and contained a literal multiplication sign in a `Require-Text` needle. Windows PowerShell 5.1 parsed that source through the legacy code page, turning `×` into `Ã—` before `Read-Utf8Text` compared it with the correctly decoded Markdown. This was an infrastructure/documentary false RED, not runtime evidence.

Hotfix 1 removes non-ASCII bytes from executable gate sources and makes the validator fail closed if the validator, adjudicator or runner later acquires a non-ASCII byte. The exact Markdown marker is still checked, but the multiplication sign is constructed at runtime as `[char]0x00D7`, so the `.ps1` source remains 7-bit ASCII.

The executable sources also avoid APIs and syntax that caused prior Windows PowerShell failures:

- no two-argument `String.Contains(string, StringComparison)` calls;
- no `StringComparison` dependency;
- no `$variable:` interpolation form;
- no PowerShell 7-only ternary/null-coalescing operators;
- invariant-culture numeric parsing and formatting are explicit;
- generic-list construction follows the existing repository pattern compatible with Windows PowerShell 5.1.

## `Require-Text` anti-false-RED review

**HOTFIX1 RECHECK PASS**

Every `Require-Text` marker in the static validator was replayed against the exact UTF-8 target file. The first attempt proved that target-text presence alone is insufficient on Windows PowerShell 5.1 when the source needle itself contains non-ASCII bytes. Hotfix 1 therefore adds source-encoding safety to the review: all executable gate sources are 7-bit ASCII, while UTF-8 evidence/document files continue to be decoded explicitly. No marker depends on generated runtime numbers, prose wrapping, or a value that belongs to a different artifact.

## Hotfix 1 scope

The failed first local attempt stopped in step `[1/4]` before the ordinary Release gate, before creation of focused process evidence and before any engineering measurement. Hotfix 1 changes only the static validator plus documentation/changelog describing the correction. C4, `src/`, exact-v9 corpus, performance baseline, focused test, runtime contract, runner, adjudicator, runtime-mode matrix, thresholds and authority boundaries are unchanged.

## Conclusion

The candidate is internally consistent for local execution of `RP1C-C4-EXACT-V9-RUNTIME-FACTOR-ISOLATION1`. Runtime results must still be returned and adjudicated before any RP1C selection, production/runtime configuration change, production repair, threshold change, exact-v9 change, VR3, P3-R1 or second replacement-long authorization.
