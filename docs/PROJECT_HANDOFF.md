# Nuclear Reactor Simulator — Project Handoff

> **Authoritative validated baseline:** M10.9.4.1-H.30 — Phase H Closure & Production Qualification Decision — VALIDATED on 2026-08-19.
>
> **Phase H:** CLOSED as `OPT-IN ONLY`.
>
> **Current candidate:** M10.9.4.1-I.1 Hotfix 1 — Profile Compatibility & Legacy Retirement Inventory.

## 1. Authoritative numerical policy

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

H.28 remains `bounded-but-costly`; any future attempt to reach default `ACTIVATE` requires separately scoped performance work and fresh qualification. It is not part of Phase I.

## 2. Closed Phase-H provenance

H.19–H.29 technical evidence is green; H.30 fingerprinted that chain and closed Phase H. The key post-optimization long-horizon regression completed 30,008 runtime steps with 9,626 corrected commits and zero rollback/fallback/unsafe/untargeted disagreement. H.29 then qualified exact-v3 deployment/replay/checkpoint behavior with 400/400 commits and zero violations.

## 3. I.1 purpose

Phase I must complete profile compatibility, legacy retirement, audit consolidation, CI, reference trajectories, known limitations and the cumulative pre-M10.9.5 engineering gate.

I.1 is the first step because retirement is unsafe until exact-version compatibility is explicit. The candidate inventories the 12 factories registered by desktop composition across 9 IDs and assigns lifecycle classifications.

Two older same-ID exact versions are compatibility-retained:

```text
integrated-operations-desktop-stable@1
pre-synchronization-grid-loading@1
```

They are **not** deleted or reinterpreted. Version number `1` alone does not mean legacy; other v1 identities remain supported scenario/training/fault/xenon profiles.

Historical numerical modes:

```text
ExplicitCommittedState                         retain / authoritative production
DeterministicHybridSemiImplicit                audit-only / retirement candidate
FourNodeBranchContinuityShadowIntegrated       audit-only / retirement candidate
FourNodeBranchContinuityCorrectedCommitOptIn  retain / qualified opt-in
```

H.5 hybrid and H.21 shadow-integrated code remains until Phase-I audit consolidation proves it can be retired without losing executable provenance.

## 4. I.1 gate

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-profile-compatibility-legacy-retirement-inventory-audit.cmd
```

Required final output:

```text
profile-compatibility-inventory-passes=True
i1-audit-passes=True
phase-i-compatibility-baseline-established=True
```

The focused gate creates each registered runtime, verifies its actual hydraulic mode and 10 ms fixed step, checks exact registry resolution and writes profile/numerical retirement CSV inventories.

## 5. After a green I.1

Promote I.1 as the Phase-I compatibility baseline. Next consolidate historical audit execution/evidence and CI gating before deleting audit-only numerical seams. After that, establish reference trajectories, known-limitations alignment and the cumulative ordinary/60 s/300 s/protection/replay/conservation/scale/performance gate required before M10.9.5.

Read also:

- `M10_9_4_1_I1_PROFILE_COMPATIBILITY_LEGACY_RETIREMENT_INVENTORY.md`;
- `M10_9_4_1_I1_VALIDATION_CHECKLIST.md`;
- `M10_9_4_1_I1_STATIC_REVIEW.md`;
- `adr/0160-inventory-exact-version-compatibility-before-retiring-legacy-audit-modes.md`;
- `ROADMAP.md`.
