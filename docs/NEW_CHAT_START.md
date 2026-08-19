# Nuclear Reactor Simulator — authoritative new-chat start

- **Authoritative validated baseline:** `M10.9.4.1-H.30 — Phase H Closure & Production Qualification Decision` VALIDATED on 2026-08-19.
- **Phase H:** CLOSED.
- **Production decision:** `OPT-IN ONLY`.
- **Authoritative default / rollback / reference:** exact v2 `integrated-operations-desktop-stable` using `ExplicitCommittedState` at 10 ms.
- **Qualified opt-in:** exact v3 using `FourNodeBranchContinuityCorrectedCommitOptIn`.
- **Current candidate:** `M10.9.4.1-I.1 Hotfix 1 — Profile Compatibility & Legacy Retirement Inventory`.
- **I.1 scope:** inventory all registered exact-version profiles; preserve replay/save identities; classify historical numerical audit modes before any retirement. No numerical/runtime retuning.

## Current gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-profile-compatibility-legacy-retirement-inventory-audit.cmd
```

Required focused flags:

```text
profile-compatibility-inventory-passes=True
i1-audit-passes=True
phase-i-compatibility-baseline-established=True
```

Expected inventory baseline: 12 registered exact versions across 9 profile IDs, zero `DELETE-NOW` exact-version profiles, desktop v2 authoritative, desktop v3 qualified opt-in. H.5 hybrid and H.21 shadow-integrated numerical modes are retirement candidates only after audit consolidation.

After a green I.1, promote it as the Phase-I compatibility baseline and continue with audit consolidation/CI hardening. Do not begin M10.9.5 until the remaining Phase-I cumulative gates are complete.

Read `docs/PROJECT_HANDOFF.md`, `docs/M10_9_4_1_I1_PROFILE_COMPATIBILITY_LEGACY_RETIREMENT_INVENTORY.md`, its validation checklist/static review, ADR 0160 and `docs/ROADMAP.md` before changing code.
