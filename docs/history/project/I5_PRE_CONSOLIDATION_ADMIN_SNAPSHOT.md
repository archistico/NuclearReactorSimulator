# I.5 pre-consolidation administrative documentation snapshot

This file preserves the administrative/current-state documents that were merged into `docs/PROJECT.md` during the I.5 documentation consolidation. It is historical provenance only; it is not a current-status source.

## PROJECT_STATUS

# Project status

## Validated production policy

**H.30 Requalification 1 — VALIDATED / ACTIVATE.** Exact v3 `FourNodeBranchContinuityCorrectedCommitOptIn` is the authoritative desktop production default. Exact v2 `ExplicitCommittedState` remains fail-closed rollback/reference. H.28 remains `bounded-but-costly`; fixed step remains 10 ms.

## Validated Phase-I evidence

**I.3 Hotfix 2 — VALIDATED.** Authoritative v3 reference: 300 s / 30,000 steps, zero generation-health violations, zero targeted reverse-flow violations, 3,757/3,757 corrected commits, deterministic repeat, seven final-window slopes and 19 frozen regression budgets.

**I.4 Hotfix 2 — VALIDATED.** Current non-zero I.3 drifts are explicitly registered as known limitations. `DeterministicHybridSemiImplicit` and `FourNodeBranchContinuityShadowIntegrated` have no production, exact-version or current-CI dependency, but still retain four source and four test seams each. Decision: `DEFER-SOURCE-REMOVAL`.

## Current candidate

**M10.9.4.1-I.5 — Cumulative M10.9.4.1 Closure Gate.**

I.5 requires ordinary CI/current evidence plus the scheduled-long gameplay, operational-envelope, reference-scale and I.3 production-reference gates. A green I.5 closes M10.9.4.1 and Phase I and unblocks M10.9.5.

## Evidence/package policy

Candidate ZIPs do not bundle `tests/.../Gameplay/Evidence`, `artifacts`, `bin` or `obj`. Compact immutable prerequisites live under `eng/frozen-evidence/ordinary`; decision/reference manifests live under `eng/evidence-manifests`.

## PROJECT_HANDOFF

# Project handoff

## Validated baseline

- H.30 Requalification 1: `ACTIVATE`; exact v3 corrected-commit is authoritative production, exact v2 explicit is rollback/reference.
- I.3 Hotfix 2: validated 300 s / 30,000-step production reference, seven slopes, 19 regression budgets.
- I.4 Hotfix 2: validated known-limitations / legacy-retirement review; H.5/H.21 source removal deferred.

## Current candidate

`M10.9.4.1-I.5 — Cumulative M10.9.4.1 Closure Gate`.

The I.5 focused script is intentionally cumulative: it runs ordinary CI/current evidence, the scheduled-long matrix, then writes the final closure artifact. It does not modify runtime physics or numerical mathematics.

## Validate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-m10941-cumulative-closure-audit.cmd
```

Expected final flags:

```text
m10941-cumulative-closure-passes=True
i5-audit-passes=True
m10941-closed=True
phase-i-closed=True
m1095-unblocked=True
```

## Packaging rule

Do not bundle `tests\NuclearReactorSimulator.Application.Tests\Scenarios\Gameplay\Evidence`, `artifacts`, `bin` or `obj`. Keep compact immutable prerequisites under `eng\frozen-evidence\ordinary`; large generated traces remain external validation artifacts.

Only a green I.5 unblocks M10.9.5.

## NEW_CHAT_START

# New chat start

Authoritative checkpoint:

- H.30 Requalification 1: VALIDATED, `ACTIVATE`;
- exact v3 corrected-commit: authoritative production default;
- exact v2 explicit: fail-closed rollback/reference;
- I.3 Hotfix 2: VALIDATED authoritative 300 s reference, seven slopes, 19 budgets;
- I.4 Hotfix 2: VALIDATED, legacy source removal deferred;
- current candidate: **M10.9.4.1-I.5 — Cumulative M10.9.4.1 Closure Gate**.

I.5 must run the ordinary/current-evidence and scheduled-long matrices without retuning runtime physics. A green I.5 closes Phase I and unblocks M10.9.5.

Candidate ZIPs exclude `tests/.../Gameplay/Evidence`, `artifacts`, `bin` and `obj`.

## I5_CUMULATIVE

# I.5 — Cumulative M10.9.4.1 Closure Gate

## Purpose

I.5 is the final Phase-I gate for M10.9.4.1 Operational Envelope & Numerical Hardening.

It does not introduce new runtime behaviour. It closes the milestone only if the current production policy, reference baseline, known-limitations review, ordinary CI and scheduled-long regressions are all green together.

## Required closure evidence

- ordinary build/test/current-evidence CI;
- H.30 Requalification 1 `ACTIVATE` production policy;
- authoritative exact-v3 production default with exact-v2 fail-closed rollback/reference;
- validated I.3 300 s / 30,000-step reference;
- zero I.3 generation-health and targeted-train reverse-flow violations;
- seven I.3 final-window slopes and 19 frozen regression budgets;
- 60 s gameplay journeys;
- current operational-envelope protection/replay/load-rejection matrix;
- reference-plant scale contract;
- H.28 `bounded-but-costly` performance classification;
- validated I.4 known-limitations and `DEFER-SOURCE-REMOVAL` legacy decision.

## Execution

The focused closure script is deliberately cumulative:

```bat
scripts\run-m10941-cumulative-closure-audit.cmd
```

It runs `eng\ci-ordinary.cmd`, then `eng\ci-long.cmd`, then writes the final I.5 closure artifact.

## Pass condition

A green I.5 must report:

```text
m10941-cumulative-closure-passes=True
i5-audit-passes=True
m10941-closed=True
phase-i-closed=True
m1095-unblocked=True
```

Only then may M10.9.5 begin.

## I5_CHECKLIST

# I.5 validation checklist

- [ ] `APPLY_UPDATE.cmd`
- [ ] `dotnet build`
- [ ] `dotnet test`
- [ ] `scripts\run-m10941-cumulative-closure-audit.cmd`
- [ ] ordinary CI/current-evidence chain passes inside I.5
- [ ] scheduled-long matrix passes inside I.5
- [ ] `production-policy=ACTIVATE`
- [ ] authoritative default remains exact v3 corrected-commit
- [ ] rollback/reference remains exact v2 explicit
- [ ] I.3 300 s reference remains green with 7 slopes / 19 budgets
- [ ] H.28 remains `bounded-but-costly`
- [ ] I.4 legacy-source decision remains `DEFER-SOURCE-REMOVAL`
- [ ] `m10941-cumulative-closure-passes=True`
- [ ] `i5-audit-passes=True`
- [ ] `m10941-closed=True`
- [ ] `phase-i-closed=True`
- [ ] `m1095-unblocked=True`
- [ ] candidate ZIP contains no `tests/.../Gameplay/Evidence`, `artifacts`, `bin` or `obj`

## M10.9.4.1

# M10.9.4.1 — Operational Envelope & Numerical Hardening

**Status:** IN PROGRESS — I.5 cumulative closure candidate.

## Validated production policy

- H.30 Requalification 1: `ACTIVATE`;
- authoritative desktop default: exact v3 `FourNodeBranchContinuityCorrectedCommitOptIn`;
- rollback/reference: exact v2 `ExplicitCommittedState`;
- H.28 performance class: `bounded-but-costly`;
- fixed step: 10 ms.

## Validated Phase-I reference and review

I.3 Hotfix 2: 300 s / 30,000 production steps, zero generation-health and targeted reverse-flow violations, deterministic repeat, seven final-window slopes, 19 versioned regression budgets.

I.4 Hotfix 2: known drift observations registered; H.5/H.21 are historical-only and source removal is deferred because executable seams remain.

## Current closure gate

I.5 reruns ordinary/current evidence and scheduled-long regressions. A green I.5 closes M10.9.4.1 and Phase I and unblocks M10.9.5.

Detailed chronology is archived under `../history/m10.9.4.1/`. Large generated audit payloads remain outside candidate source ZIPs.

