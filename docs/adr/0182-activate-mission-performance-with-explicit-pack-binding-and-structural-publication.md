# ADR-0182 — Activate Mission/Performance with explicit pack binding and structural publication

## Status

Proposed — implemented in M10.9.7.3 Hotfix 1 REV2. The original M10.9.7.3 package is superseded/not validated after two compile-contract failures. The first Hotfix 1 fixed those contracts but is also superseded/not validated after ordinary tests exposed stale batch-presentation ordering in the live source plus an over-broad historical situation-strip assertion. REV1 fixed both and left Application.Tests green, but its newly scoped historical shell assertion still expected the obsolete direct `LogicalStepText` binding. REV2 retains the REV1 runtime fix unchanged and aligns that test to the actual top runtime-step binding `RuntimeProgressText`. Build, ordinary tests, focused live-workspace audit and manual HMI acceptance are still required before promotion.

## Context

M10.9.7.1 validated an immutable Mission/Performance read model. M10.9.7.2 validated a dedicated peer workspace with contextual COMPUTER navigation, no F9 and no plant-command authority. Pre-live hardening then closed terminal logical-step alignment, presentation/archive robustness, Domain invariants, 10 ms lookup/allocation issues and persistence payload integrity.

Two remaining choices must be explicit before activating the HMI:

1. a live UI must accumulate score/demand evidence at deterministic-step cadence without forcing the UI itself to refresh at 100 Hz;
2. a default desktop scenario cannot safely imply one challenge pack because multiple authored packs may share a scenario identity.

Generated record equality is also unsuitable for update suppression because the snapshot contains `IReadOnlyList<>` members with reference-based equality.

## Decision

1. Activate `MISSION` as a live main-HMI workspace while preserving COMPUTER F1–F8 and adding no F9.
2. Use `MissionPerformanceLiveSnapshotSource` as a read-only Application adapter over canonical session evidence.
3. Accumulate external-demand/scoring evidence on every deterministic step; publish immutable presentation snapshots at presentation cadence and on relevant same-step contextual changes.
4. Use explicit field/sequence comparison for publication suppression; do not use generated record equality as a UI change detector.
5. Keep normal desktop startup mission-unbound. Bind a mission only from an explicit exact `OperationalChallengePackDefinition`.
6. Provide `--mission-pack=<exact-id>` only as an explicit startup/manual-validation binding seam; do not infer by scenario identity and do not introduce a user-facing challenge launcher in 7.3.
7. Keep archive-restored mission binding/timeline reconstruction in M10.9.7.4.
8. Keep navigation and the entire MISSION surface free of plant command authority.

## Consequences

- MISSION can be validated both unbound and with a real live M10.9.6 pack;
- UI refresh cadence cannot alter deterministic demand/scoring evidence;
- recreated collection instances do not cause redundant publications;
- the default desktop session does not silently acquire gameplay semantics;
- pack selection remains explicit and fail-closed;
- checkpoint/archive mission reconstruction remains a clearly scoped next step rather than an implicit partial implementation.
