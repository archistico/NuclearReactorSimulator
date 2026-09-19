# M10 Final VR2 R3 Diagnostic 3 REV1 — Validator Contract Hygiene Hotfix 1

Date: 2026-09-19  
Status: **READY FOR EXECUTION**

<!-- NRS-MARKER:DIAG3-REV1-VALIDATOR-CONTRACT-HYGIENE-HOTFIX1 -->

## Trigger

The first execution after Planning Amendment 1 stopped in static audit before build with:

`REV1 document technical identifier missing: IMPLEMENTED-NOT-EXECUTED`

The JSON contract already carried `status=IMPLEMENTED-NOT-EXECUTED` and the diagnostic document already carried the stable implementation marker. The failure therefore came from a redundant legacy Markdown literal-presence check that bypassed the marker contract. Two additional legacy navigation filename checks were present immediately after it and could have created further typography/navigation false REDs.

## Correction

The active REV1 validator now validates all Markdown presence requirements exclusively through `documentation_marker_files` in the REV1 JSON contract. Each `(path, marker)` pair must occur exactly once. Index and documentation navigation therefore receive dedicated ASCII markers.

Status, adjudication classes, authority, CI hold, guard values and repair ownership remain machine semantics of JSON/source/adjudicator evidence and are not inferred from Markdown text.

`docs/VALIDATOR_AUTHORING_RULES.md` is strengthened accordingly: technical identifiers may be asserted literally in JSON/source/runner/machine evidence, but their textual presence inside Markdown is never a gate; Markdown is marker-only.

## Scope

This hotfix changes no production code, historical test semantics, scenario, seed, physical guard, counterfactual mathematics, C4 payload, canonical exact-v9 behavior, R3/R4 state or repair authority.

## Authority

The only next authorized activity remains `EXECUTE-DIAGNOSTIC3-REV1-TEST-ONLY`.
