# Nuclear Reactor Simulator — Project Handoff

> **Authoritative validated baseline:** M10.9.4.1-I.1 Hotfix 1 — Profile Compatibility & Legacy Retirement Inventory — VALIDATED on 2026-08-19.
>
> **Phase H:** CLOSED as `OPT-IN ONLY`.
>
> **Current candidate:** M10.9.4.1-I.2 — Audit Consolidation & CI Baseline Hardening.

## 1. Authoritative production policy

```text
exact v2 integrated-operations-desktop-stable
  ExplicitCommittedState
  authoritative default / rollback / reference

exact v3 integrated-operations-desktop-stable
  FourNodeBranchContinuityCorrectedCommitOptIn
  qualified opt-in

explicit deployment kill
  exact v2 ExplicitCommittedState
```

H.28 remains `bounded-but-costly`; Phase I does not reopen the Phase-H activation decision.

## 2. Validated I.1 compatibility baseline

I.1 Hotfix 1 passed build, ordinary tests and focused audit. It established 12 exact-version initial conditions across 9 IDs, retained two older same-ID identities for compatibility, and classified zero exact-version profiles `DELETE-NOW`.

Historical numerical modes remain:

```text
ExplicitCommittedState                         retain / authoritative production
DeterministicHybridSemiImplicit                audit-only / retirement candidate
FourNodeBranchContinuityShadowIntegrated       audit-only / retirement candidate
FourNodeBranchContinuityCorrectedCommitOptIn  retain / qualified opt-in
```

## 3. I.2 purpose

I.2 consolidates validation execution before any retirement. It freezes the user-validated I.1 artifacts and defines four tiers:

```text
ORDINARY
CURRENT-EVIDENCE
SCHEDULED-LONG
HISTORICAL-FROZEN
```

Provider-neutral commands are `eng\ci-ordinary.cmd`, `eng\ci-current-evidence.cmd` and `eng\ci-long.cmd`; GitHub Actions are thin wrappers. Ordinary/current CI does not rerun H.24 post-H.28, H.28 performance or H.5/H.21 historical research gates.

Important: H.5/H.21 no longer being current-CI dependencies does **not** make their source safe to delete. Historical executable tests still reference those numerical modes. I.2 therefore requires `legacy-mode-retirement-authorized=False`.

## 4. I.2 gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-phase-i-audit-consolidation-ci-baseline-audit.cmd
```

Required final output:

```text
phase-i-audit-consolidation-passes=True
i2-audit-passes=True
phase-i-ci-baseline-established=True
```

## 5. After a green I.2

Promote I.2 as the Phase-I audit/CI baseline. Next consolidate current conservation/inventory observations and versioned reference-trajectory/tolerance evidence. Do not delete H.5/H.21 source yet and do not begin M10.9.5 until the full M10.9.4.1 acceptance gate is closed.

Read also:

- `M10_9_4_1_I2_AUDIT_CONSOLIDATION_CI_BASELINE.md`;
- `M10_9_4_1_I2_VALIDATION_CHECKLIST.md`;
- `M10_9_4_1_I2_STATIC_REVIEW.md`;
- `adr/0161-tier-current-validation-and-freeze-historical-audits-before-legacy-retirement.md`;
- `OPERATIONAL_ENVELOPE_NUMERICAL_HARDENING_PLAN.md`;
- `ROADMAP.md`.
