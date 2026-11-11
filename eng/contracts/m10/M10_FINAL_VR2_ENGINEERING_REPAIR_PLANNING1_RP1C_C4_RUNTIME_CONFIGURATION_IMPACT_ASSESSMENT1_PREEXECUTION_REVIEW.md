# RP1C C4 Runtime Configuration Impact Assessment 1 - Pre-Execution Review

## Status

**STATIC-PREEXECUTION-REVIEW-PASS**

This review covers the A2 implementation candidate only. It does not pre-adjudicate returned profile evidence.

## Anti-RED findings

1. Runtime Configuration Impact Assessment Planning 1 returned `PASS-AS-AUTHORED` and its four returned artifacts are frozen and hash-pinned.
2. Host-Provenance Amendment 1 returned `PASS-AS-AUTHORED` and its four returned artifacts are frozen and hash-pinned.
3. Frozen Evidence Ordinary Compaction 1 returned `PASS-AS-AUTHORED`; its four returned artifacts are frozen and hash-pinned and the compact-store boundary is preserved.
4. C4, the exact-v9 corpus and the frozen performance baseline remain hash-pinned and unchanged.
5. Canonical `src/` remains byte-identical to the Compaction 1 baseline when generated `bin/` and `obj/` subtrees are excluded from repository identity. Local generated output is permitted in a working copy after prior builds and is not source drift.
6. All pre-existing canonical test sources remain byte-identical with generated `bin/` and `obj/` excluded; A2 adds one explicit focused test only.
7. The caller must have all five controlled DOTNET variables unset. The runner fails closed instead of silently clearing them.
8. A2 child processes receive only the frozen `AMBIENT-UNSET` or `EXPLICIT-REFERENCE-ALL-ON` runtime profiles.
9. A2-specific `NRS_*` focused variables are scrubbed from every child process and are added only to exact-v9 focused children when required.
10. Host fingerprint is SHA-256 based, privacy-preserving, captured at start/end and propagated into child-run contracts and exact-v9 runtime contexts.
11. The active Windows power scheme is captured at start/end and must remain stable.
12. Exact-v9 executes exactly ten fresh processes in the frozen counterbalanced schedule and emits exactly 40 process files.
13. Each exact-v9 process contains exactly 23,040 timing rows; the adjudicator reconstructs every 64 x 360 identity/order pair against the frozen corpus.
14. `100 us` remains diagnostic only and strict max remains `409.30666666666673 us`.
15. Ordinary and replay/determinism test counts are derived from xUnit XML, not localized console text.
16. `failed`, `errors` and `not-run` are recorded explicitly for ordinary and replay families.
17. A valid test failure is engineering evidence; missing/malformed structured result files are infrastructure RED.
18. The existing M10.9.7.2 hot-path owner is executed three fresh times per profile and generated metrics are copied immediately after each run.
19. Hot-path source artifacts are treated as transient staging output and are not part of the final 78-file A2 tree.
20. The final adjudicator requires exactly 78 files and no temporary `_work` directory.
21. No automatic ambient/default equivalence, runtime recommendation, FDPC2 authorization or RP1C selection is performed.
22. The compact frozen-evidence retention policy remains intact; A2 raw output is not added to `eng/frozen-evidence/ordinary` by this candidate.

## Windows PowerShell 5.1 compatibility review

Validator, orchestrator, adjudicator and CMD runner are 7-bit ASCII with CRLF line endings. They avoid PowerShell 7-only syntax, parse numeric data using invariant culture, use explicit UTF-8 reads/writes for text evidence and use ordinal case-sensitive tree hashing over canonical source files only. Generated `bin/` and `obj/` directories are excluded from tree identity because they are build products, while candidate ZIP cleanliness remains a packaging-time check.

The orchestrator uses xUnit v3 structured XML reporting so localized Italian/English console summaries cannot create false REDs.

## `Require-Text` anti-false-RED review

Every static marker in the validator is checked against its exact target before packaging. Markers avoid non-ASCII source literals, line wrapping dependencies and locale-formatted generated values.

## Conclusion

Candidate is suitable for local execution of `RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT1` only. Returned 78-file evidence is mandatory before Full-Domain Performance Confirmation 2 planning, RP1C selection or any production/runtime change.

## Second-pass execution review — Hotfix 2

A second independent pre-execution review found and closed two additional false-RED / false-contract risks before local execution:

1. `--results-directory` is a .NET 10 `dotnet test` / Microsoft Testing Platform option and is therefore placed before the literal `--`; xUnit-specific report/filter options remain after `--`.
2. Structured xUnit accounting is schema-aware. The observed xUnit v3 3.2.2 schema-3 report included `not-run` inside `total`, while published format documentation describes executed-only `total`. The harness therefore derives `executed = passed + failed + skipped`, accepts only either `total = executed` or `total = executed + not-run`, records the detected accounting mode, and never uses reporter `total` as the executed-test cardinality.
3. Every structured ordinary/replay/hot-path invocation also uses the native MTP guard `--minimum-expected-tests 1`; the XML parser retains its own non-zero check as independent defense in depth.
4. The previous COMPlus alias neutralization, repository-root current directory, structured XML parsing, single-host fingerprint and stable-power-plan controls remain unchanged.

These changes harden only the harness. They do not modify C4, exact-v9, the `409.30666666666673 us` ceiling, the two A2 profiles, the 78-file evidence shape or any downstream authority boundary.


## Hotfix 4 working-copy identity review

A local rerun demonstrated 959/959 canonical source files present and byte-identical, with 204 extra paths consisting exclusively of generated `bin/` and `obj/` output. The runtime validator previously counted those generated files in `src` tree identity and also rejected any local transient directory, creating a false infrastructure RED after normal build activity. Hotfix 4 scopes source/test identity to canonical files excluding generated `bin/` and `obj/` subtrees and removes the runtime prohibition on local build output. This does not relax candidate-package cleanliness, source identity, C4/exact-v9 hashes, thresholds or authority boundaries.


A previous failed A2 attempt may leave partial files under the owned A2 artifact root. This is permitted in the working copy because the A2 orchestrator deletes and recreates that entire owned root before new evidence collection, preventing stale/new evidence mixing.
