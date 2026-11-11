# M10 Final VR2 R3 — Diagnostic 3 Deep Review & REV1 Planning 1 — Validator Hotfix 1

**Date:** 2026-09-19  
**Scope:** planning-audit validator only  
**Engineering authority change:** none

## Trigger

The first execution of `run-m10-final-vr2-r3-diagnostic3-deep-review-rev1-planning1.cmd` stopped in the static validator with:

```text
review/plan marker missing: production repair planning/implementation may not begin until hosted `ordinary-ci` is GREEN
```

The planning document already contained the required semantic statement, but its leading clause was Markdown-bolded:

```text
**production repair planning/implementation may not begin** until hosted `ordinary-ci` is GREEN on the deterministic candidate;
```

The validator used a raw `.Contains()` search for the same sentence without the `**` formatting delimiters. The failure was therefore a validator text-format coupling defect, not a missing planning constraint.

## Hotfix

Hotfix 1 adds a Markdown-normalized marker helper for this semantic check. It removes `**` formatting delimiters before evaluating the frozen hosted-CI production-repair hold. All other markers remain exact-string checks.

The hotfix deliberately does **not** weaken the constraint. The planning still requires:

- Diagnostic 3 REV1 test-only evidence may proceed while hosted CI confirmation is pending;
- production repair planning/implementation may not begin until hosted `ordinary-ci` is GREEN;
- `repair-owner=UNSELECTED`;
- production repair, seed retuning, threshold/C4/exact-v9 changes, R3 Requalification 3 and R4 remain unauthorized.

## Baseline / evidence effect

No `src/` file is changed. No test is changed. No guard or adjudication class is changed. No reviewed Diagnostic 3 provenance file is changed. The original Planning 1 engineering conclusions remain authoritative; this record only corrects their validator representation.

## Next activity

Re-run:

```powershell
.\scripts\run-m10-final-vr2-r3-diagnostic3-deep-review-rev1-planning1.cmd
```

Expected result:

```text
M10 Final VR2 R3 Diagnostic 3 Deep Review & REV1 Planning 1: PASS-AS-AUTHORED
Diagnostic 3 Deep Review & REV1 Planning 1 completed.
Next authorized activity: implement Diagnostic 3 REV1 test-only candidate.
```
