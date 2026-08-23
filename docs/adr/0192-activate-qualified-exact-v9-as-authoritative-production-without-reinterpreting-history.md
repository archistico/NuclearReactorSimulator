# ADR 0192 — Activate qualified exact-v9 as authoritative production without reinterpreting historical identities

## Status

Proposed / M10 Final exact-v9 authoritative production activation-decision candidate.

## Context

Diagnostic 11 Hotfix 2 qualified `integrated-operations-desktop-stable@9` as a stationary whole-cycle operating point. The subsequent exact-v9 production-activation candidate was then returned green through the real production selector path: 12,000 healthy steps, ~5 MWe, ~100 kg/s primary flow, stable drum/governor state, explicit moisture-drain ownership, conservative network mass/energy closure, zero rollback/fallback/unsafe/untargeted events, and deterministic selector/direct-factory equality with fingerprint `7880AD580179B936C584EB0055BE663E0A1CFA65C5191B0DB8A7F3C514DB5418`.

That green opt-in gate authorizes a separate activation decision, not silent mutation of existing exact identities. Phase-I exact-v4 evidence, H.29/H.30 exact-v3 evidence, exact-v2 fail-closed rollback, mission-pack `@2`, archives and replay bindings must remain historically resolvable after the production default changes.

## Decision

This candidate proposes to make `DesktopHydraulicProductionPolicy.M10FinalExactV9QualifiedCandidate` the `AuthoritativeDefaultPolicy` and to bind the current desktop production scenario to a new exact scenario identity:

`integrated-normal-operations-training-m10-final-v9-production`

The existing exact-v9 activation-candidate scenario remains a distinct historical/replayable identity and is not renamed or reinterpreted.

Exact-v4 remains explicitly selectable through `I5RepairedProductionPolicy`; exact-v3 remains explicitly selectable through the historical H.29 policy; exact-v2 remains the explicit fail-closed kill/rollback policy.

The current production mission pack advances from `bounded-demand-following-5-10-5@2` to `@3`. Version `@2` remains immutable and bound to the historical exact-v4 production scenario. Version `@3` keeps the same challenge objective, external-demand profile, scoring policy, condition evaluator and score-evidence bindings, but binds the new exact-v9 production scenario. Version `@1` remains the original historical pack.

Historical validation tests that were intended to prove exact-v4 or exact-v3 behavior must pin those exact policies/packs explicitly rather than follow the symbolic current default after this activation.

## Candidate hotfix status

The original Activation Decision 1 package was BUILD RED before validation execution because the newly added focused test had two compile-only contract defects: a canonical `decimal?` mission score was stored in a `double` record field, and the existing `ControlRoomSnapshotFingerprint` helper was referenced without importing `NuclearReactorSimulator.Application.Scenarios.Recording`. Hotfix 1 corrects only those test definitions; the decision, runtime source, exact-v9 state, selector switch and mission `@3` binding are unchanged.

## Validation

The activation-decision gate must run, in order:

1. restore and Debug build with warnings-as-errors;
2. complete ordinary suite after the proposed default switch;
3. LR-M1 Hotfix 1 semantic-equivalence regression;
4. exact-v9 600 s Diagnostic-11 requalification on the switched source tree;
5. focused authoritative selector/scenario/mission-v3 audit;
6. post-switch cumulative current-evidence routing.

The focused audit must retain the qualified exact-v9 fingerprint, healthy ~5 MWe / ~100 kg/s operation, moisture-drain ownership, conservation ceilings, zero trip/breaker-open/rollback/fallback/unsafe/untargeted observations, exact-v4 historical resolution and exact-v2 fail-closed rollback.

## Consequences

A green activation-decision result makes exact-v9 the new authoritative production baseline for subsequent M10 work. It does **not** authorize reusing the failed exact-v4 long manifest. The next step is to freeze a new exact-v9 source/baseline manifest and a redesigned replacement-long workload with the previously established workstation budget, then run that campaign as new evidence.

Until this candidate gate is returned green, exact-v9 authoritative activation remains proposed and the last validated deployment state remains exact-v4 authoritative with exact-v9 qualified opt-in.
