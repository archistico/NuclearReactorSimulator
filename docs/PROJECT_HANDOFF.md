# Project handoff

## Authoritative state

Last fully validated baseline: **M10.9.4.1-I.2 — Audit Consolidation & CI Baseline Hardening**.

The following later results are validated **evidence**, but have not yet produced a promoted Phase-I baseline:

1. I.3 Hotfix 4 Classifier Fix 1: exact v2 has 338/338 generation-drop steps coincident with targeted stop/control/admission reverse flow; exact v3 has 0/0 over the same 100 s / 10 ms comparison.
2. I.3 Hotfix 5: exact v3 completes 300 s / 30,000 steps with 0 generation-health violations, 0 targeted reverse-flow violations, 3,757 corrected commits, 0 rollback/fallback/unsafe/untargeted disagreement and deterministic repeat.

Current candidate: **M10.9.4.1-H.30 Requalification 1 — Production Policy Re-review after I.3 Continuity Evidence**.

## Candidate intent

Re-open only the H.30 production-policy decision. The candidate derives `ACTIVATE` if frozen evidence is intact:

- exact v3 becomes the desktop authoritative default;
- exact v2 remains exact-version rollback/reference;
- historical save/replay identities are not reinterpreted;
- numerical mathematics and 10 ms fixed step are unchanged.

The candidate also cleans project documentation by moving detailed M10.9.4.1 chronology under `docs/history/m10.9.4.1/` and rewriting the high-level README/status/roadmap as current-state documents.

## Required local validation

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-h30-rq1-production-policy-rereview-audit.cmd
```

Expected focused flags:

```text
h30-rq1-production-policy-decision=ACTIVATE
h30-rq1-evidence-chain-passes=True
h30-rq1-audit-passes=True
production-corrected-default-activated=True
i3-reference-rerun-unblocked=True
```

Do not mark H.30 RQ1 validated before the user explicitly reports build, complete ordinary suite and focused audit green.

## Frozen evidence used by H.30 RQ1

- original H.30 closure and H.28 performance evidence already stored under test evidence;
- I.3 Hotfix 4 validated 100 s explicit-vs-corrected comparison;
- I.3 Hotfix 5 validated corrected 300 s requalification.

H.30 RQ1 does not rerun H.24, H.28 or the I.3 long diagnostics.

## After a green H.30 RQ1

1. Promote H.30 RQ1 as the authoritative production policy (`ACTIVATE`).
2. Resume I.3 on exact v3 and freeze the versioned reference trajectory/tolerance budgets only after a green authoritative-policy run.
3. Continue Phase-I known-limitations/legacy-retirement/cumulative closure work.
4. Do not begin M10.9.5 until the Phase-I cumulative gate is green.

## Non-negotiable project rules

- milestone-by-milestone validation;
- stack only on a validated baseline or explicitly identified diagnostic lineage;
- warnings-as-errors and xUnit analyzer compliance;
- deterministic fixed-step runtime;
- exact-version save/replay compatibility;
- fail-closed corrected ownership/rollback;
- no UI-side reactor physics;
- no hidden physics retuning to satisfy a regression budget.
