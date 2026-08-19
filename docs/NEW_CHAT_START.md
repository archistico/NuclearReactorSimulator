# Nuclear Reactor Simulator — authoritative new-chat start

- **Authoritative validated baseline:** `M10.9.4.1-I.1 Hotfix 1 — Profile Compatibility & Legacy Retirement Inventory` VALIDATED on 2026-08-19.
- **Phase H:** CLOSED as `OPT-IN ONLY`.
- **Authoritative default / rollback / reference:** exact v2 `integrated-operations-desktop-stable` using `ExplicitCommittedState` at 10 ms.
- **Qualified opt-in:** exact v3 using `FourNodeBranchContinuityCorrectedCommitOptIn`.
- **I.1 validated compatibility:** 12 exact versions across 9 IDs; 2 compatibility-retained; 0 delete-now.
- **Current candidate:** `M10.9.4.1-I.2 — Audit Consolidation & CI Baseline Hardening`.
- **I.2 scope:** freeze I.1 evidence, tier audit execution, add ordinary/current/scheduled CI entry points, keep historical H.5/H.21 source until a separate retirement milestone. No numerical/runtime retuning.

## Current gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-phase-i-audit-consolidation-ci-baseline-audit.cmd
```

Required focused flags:

```text
phase-i-audit-consolidation-passes=True
i2-audit-passes=True
phase-i-ci-baseline-established=True
```

After a green I.2, continue with Phase-I conservation/inventory observation consolidation and versioned reference-trajectory/tolerance budgets. Do not begin M10.9.5 until the remaining cumulative M10.9.4.1 gates are complete.

Read `docs/PROJECT_HANDOFF.md`, the I.2 document/checklist/static review, ADR 0161 and `docs/OPERATIONAL_ENVELOPE_NUMERICAL_HARDENING_PLAN.md` before changing code.
