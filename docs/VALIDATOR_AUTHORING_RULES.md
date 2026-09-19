# Validator Authoring Rules

Date: 2026-09-19  
Status: ACTIVE PROJECT RULE

This document records the repository rule adopted after the Diagnostic 3 Planning 1 validator false REDs.

## 1. Structured state is authoritative

Gate status, authority, guard values, sequencing and machine decisions belong in JSON contracts or returned evidence. Markdown prose is explanatory and must not be used as the machine-readable source of truth.

## 2. Documentation assertions use stable ASCII marker IDs

When a validator must prove that a document exposes a required checkpoint, the document carries an HTML comment marker such as:

```text
<!-- NRS-MARKER:DIAG3-REV1-ROADMAP-SUPERSESSION -->
```

Validators search the marker ID, not the surrounding sentence, Markdown formatting or punctuation. Marker IDs use ASCII letters, digits, hyphen, colon and underscore only.

## 3. No Unicode-dependent validator literals

A PowerShell validator must either be ASCII-only or be stored as UTF-8 with BOM. Non-ASCII punctuation must never be part of a comparison key. This is required for Windows PowerShell 5.1 compatibility.

<!-- NRS-MARKER:VALIDATOR-DOCUMENT-CHECKS-MARKER-ONLY -->

## 4. Identifiers may be checked in structured/code artifacts; Markdown presence is marker-only

Stable technical identifiers such as `CAUSAL-CLOSURE-CONFIRMED`, schema IDs, file names and test method names may be asserted literally **when the authoritative object being checked is JSON, source code, a runner, or returned machine evidence**. Their mere textual presence in Markdown must not be a gate condition.

For Markdown, the only permitted machine-presence contract is an ASCII `NRS-MARKER:*` ID declared in the validator JSON contract. Navigation documents follow the same rule: validators check a navigation marker, not the displayed file name or surrounding prose.

## 5. Report all missing document markers together

Document-marker validation is aggregate. A validator collects every missing marker and emits one failure report instead of stopping at the first missing item.

## 6. Fail closed on semantics, not typography

If a semantic contract field drifts, the validator fails. If a writer changes emphasis, punctuation, line wrapping or heading typography while preserving the required marker and structured contract, the validator remains green.

## 7. Candidate preflight

Before a candidate is handed off, its validator contract must be checked for: structured semantic fields, ASCII marker presence, marker uniqueness where required, source encoding policy, and absence of prose-coupled authority assertions in newly authored validator code.

## 8. Prefer runtime-portable primitives over convenience cmdlets

Do not assume that a convenience cmdlet exists merely because another PowerShell feature is available. In particular, new gate validators/adjudicators must not depend on `Get-FileHash` unless that cmdlet has been proven in the target environment or an explicit fallback is present.

For SHA-256, the repository-safe default is `System.Security.Cryptography.SHA256` over a file stream. This helper works in the already-proven Windows PowerShell subset and avoids version-specific cmdlet availability.

Preflight must inspect **every PowerShell script reached by the runner**, not only the first validator. A compatibility defect in the final adjudicator is still a gate defect even when the initial audit passes.
