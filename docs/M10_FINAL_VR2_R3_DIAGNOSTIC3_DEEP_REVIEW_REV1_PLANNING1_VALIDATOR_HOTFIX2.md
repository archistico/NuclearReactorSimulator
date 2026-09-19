# M10 Final VR2 R3 Diagnostic 3 Deep Review & REV1 Planning 1 — Validator Hotfix 2

Date: 2026-09-19  
Status: VALIDATOR-HYGIENE HOTFIX

## Defect

Hotfix 1 corrected one Markdown-format-coupled assertion but the next assertion still depended on an editorial ROADMAP sentence containing a Unicode em dash. The validator script was UTF-8 without BOM and is launched by Windows PowerShell 5.1, so non-ASCII source literals are not a portable comparison contract.

## Root cause class

`PROSE-AND-ENCODING-COUPLED-DOCUMENT-VALIDATION`

The defect is broader than one missing marker: human prose, Markdown formatting and Unicode punctuation were being treated as a machine-readable API.

## Repair

Hotfix 2:

- keeps JSON contract fields authoritative for review, guards, CI hold and authority;
- introduces stable ASCII `NRS-MARKER:*` HTML comments at the required documentation checkpoints;
- validates marker IDs instead of full editorial sentences;
- makes the planning validator ASCII-only;
- adds a self-encoding guard for future non-ASCII drift;
- aggregates all missing documentation markers into one failure report;
- records the general repository rule in `VALIDATOR_AUTHORING_RULES.md`.

## Scope

No engineering review result changes. No REV1 guard changes. No authority changes. No production or historical-test changes. The next authorized activity remains `IMPLEMENT-DIAGNOSTIC3-REV1-TEST-ONLY` after this planning audit passes.
