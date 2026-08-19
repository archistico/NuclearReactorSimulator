# M10.9.4.1-I.1 — Profile Compatibility & Legacy Retirement Inventory

## Status

**VALIDATED via Hotfix 1 on 2026-08-19.** Built directly on user-validated H.30, which closed Phase H as `OPT-IN ONLY` and unblocked Phase I. The initial I.1 candidate had one analyzer-only xUnit2031 build failure; Hotfix 1 corrected the assertion form without changing semantics, after which build, ordinary tests and focused I.1 audit passed. I.1 is a compatibility/engineering-hardening milestone; it does not alter plant physics, numerical mathematics, production selection, save/replay identity or the 10 ms fixed step.

## Purpose

Phase I must eventually complete profile compatibility, legacy retirement, audit consolidation, CI, reference trajectories, known limitations and the final cumulative engineering gate before M10.9.5.

The first safe step is to remove ambiguity about what is actually supported. I.1 therefore creates an executable inventory of:

- every exact-version initial-condition factory registered by the desktop composition;
- which versions are authoritative/current, qualified opt-in, ordinary supported, or compatibility-retained;
- which hydraulic numerical modes are production-supported versus historical audit-only seams;
- which items may be retired later and which must remain loadable for deterministic scenario/save/replay compatibility.

I.1 deliberately performs **no deletion**. Retirement without an inventory would risk silently invalidating exact-version archives.

## Phase-H prerequisite

The user-validated H.30 summary and metrics are promoted as immutable evidence and canonical-fingerprint checked. Required prerequisite state:

```text
phase-h-production-policy-decision = OPT-IN ONLY
phase-h-closed                     = True
phase-i-unblocked                  = True

exact v2 integrated-operations-desktop-stable
  ExplicitCommittedState
  authoritative default / rollback / reference

exact v3 integrated-operations-desktop-stable
  FourNodeBranchContinuityCorrectedCommitOptIn
  qualified opt-in
```

## Exact-version compatibility policy

The desktop composition currently registers **12 exact profile versions across 9 profile IDs**.

Two older identities are explicitly compatibility-retained because newer versions exist under the same profile ID:

```text
integrated-operations-desktop-stable@1
pre-synchronization-grid-loading@1
```

They are not reinterpreted and are not deleted. Existing scenario definitions, recordings, checkpoints and session archives depend on exact-version resolution semantics.

The current production identities are:

```text
integrated-operations-desktop-stable@2
  AUTHORITATIVE-DEFAULT
  ExplicitCommittedState

integrated-operations-desktop-stable@3
  QUALIFIED-OPT-IN
  FourNodeBranchContinuityCorrectedCommitOptIn
```

The remaining exact-version factories remain supported scenario/training/fault/xenon identities and are not legacy merely because their version is `1`.

## Numerical-mode retirement inventory

I.1 distinguishes profile compatibility from historical numerical research seams:

| Numerical mode | Phase-I classification | Production selectable | I.1 action |
| --- | --- | --- | --- |
| `ExplicitCommittedState` | authoritative production | yes | retain |
| `DeterministicHybridSemiImplicit` | historical audit-only | no | retirement candidate after audit consolidation |
| `FourNodeBranchContinuityShadowIntegrated` | historical audit-only | no | retirement candidate after audit consolidation |
| `FourNodeBranchContinuityCorrectedCommitOptIn` | qualified opt-in | yes | retain |

The H.5 hybrid and H.21 shadow-integrated modes are **not removed in I.1** because historical focused audits still compile against those seams. Their safe retirement belongs after Phase-I audit consolidation has preserved the required provenance without depending on executable historical branches.

## Frozen runtime contract

I.1 changes none of the following:

- H.30 `OPT-IN ONLY` production policy;
- exact v2/v3 semantics or deployment selector;
- H.9 mathematics;
- H.20 authority/rollback semantics;
- H.22 corrected commit ownership;
- P060/F040;
- four-node continuity limits;
- physical coefficients;
- protection logic;
- 10 ms fixed step;
- scenario, recording, checkpoint or archive schema.

Under `src/`, the intended delta is metadata-only in `ApplicationDescriptor.cs`.

## Focused gate

Run:

```bat
scripts\run-profile-compatibility-legacy-retirement-inventory-audit.cmd
```

The gate:

1. fingerprint-checks validated H.30 closure evidence;
2. enumerates all 12 exact-version factories and requires 12 unique exact identities across 9 IDs;
3. creates every factory runtime and verifies the actual hydraulic coupling mode and 10 ms fixed step;
4. proves v2 remains default, v3 remains corrected opt-in and explicit kill still resolves to v2;
5. writes the exact profile matrix and numerical-mode retirement inventory;
6. requires zero `DELETE-NOW` exact-version profiles.

Required final flags:

```text
profile-compatibility-inventory-passes=True
i1-audit-passes=True
phase-i-compatibility-baseline-established=True
```

## Next step after validation

I.1 is green and establishes the compatibility baseline. I.2 now consolidates historical audit execution/evidence and CI gating before any code retirement or before M10.9.5 begins.
