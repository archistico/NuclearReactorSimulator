# Project — current authoritative state

This is the **single current-state and handoff document** for Nuclear Reactor Simulator. Do not duplicate the current checkpoint in README, roadmap, milestone files or candidate-specific notes.

## Current checkpoint

**M10.9.4.1 / Phase I, M10.9.5, M10.9.6, M10.9.7.1 Hotfix 3, M10.9.7.2 REV1, M10.9.7.2 Hotfix 1 REV1, M10.9.7.2 Hotfix 2 REV1, M10.9.7.2 Hotfix 3 REV1, M10.9.7.3 Hotfix 1 REV2 and M10.9.7.3 Hotfix 2 REV2 are VALIDATED.** M10.9.6 remains CLOSED. M10.9.7.3 Hotfix 2 REV2 passed build, the complete ordinary suite, `scripts\run-m10973-desktop-host-session-integrity-audit.cmd` and its manual desktop-host/session-integrity checklist on 2026-08-21. The validated baseline therefore includes the live read-only `MISSION` workspace plus desktop numerical-failure containment and non-destructive local session-save replacement.

Authoritative desktop production remains:

`integrated-operations-desktop-stable@4 | CorrelationConsistentInverseDomain | FourNodeBranchContinuityCorrectedCommitOptIn | 10 ms`

Historical exact-version identities remain immutable; no M10.9.7 presentation/host work reopens Phase-I numerical ownership.

## Active candidate

**M10.9.7.4 Hotfix 1 — Ordinary Suite Contract Alignment — CANDIDATE.**

The original M10.9.7.4 candidate was stacked exclusively on **M10.9.7.3 Hotfix 2 REV2 VALIDATED** plus Docs4 and compiled, but the complete ordinary suite reported 3 test failures. Hotfix 1 is stacked exclusively on that M10.9.7.4 candidate and changes tests/contracts documentation plus candidate descriptor metadata only: it aligns the historical M10.9.7.3 visual-heading assertion with the intentional timeline UI, makes the no-F9 regression inspect `KeyBinding Gesture="F9"` rather than arbitrary XAML substrings, and recognizes the retained H29 primary-valve presentation as topology-empty while keeping the frozen golden hash unchanged. No production XAML, runtime semantics, physics, fingerprint implementation or replay/archive behavior is changed by Hotfix 1.

The underlying M10.9.7.4 implementation extends presentation/replay evidence only:

- freezes `sha256-control-room-snapshot-v1` with the populated retained H29 exact-version 128-step golden fingerprint `63643e5506a6b99f8106950ecb25a5243e9755b3bc96bf2a60e96c219216f362`;
- separates a protected bounded lifecycle spine from bounded recent operational evidence before deterministic timeline merge;
- projects lifecycle, demand-change, operator-action, alarm/protection/fault and scoring context by logical step/canonical sequence;
- adds presentation-only drill-down to existing ELECTRICAL, ALARMS/EVENTS and COMPUTER evidence surfaces without plant-command authority;
- reconstructs mission lifecycle/demand from an already verified full-replay or checkpoint prefix, then continues on future live deterministic evidence without an opaque challenge-state checkpoint blob;
- preserves archive schema v1: restored MISSION state requires an explicit exact pack binding matching scenario + initial-condition identity; an unbound archive remains unbound and no pack is inferred from `ScenarioId`;
- `START RECORDED SESSION` preserves an already explicit mission binding so the desktop archive/replay round-trip can be exercised without creating a challenge launcher.

M10.9.7.3 `RecentEvents`, challenge/scoring arithmetic, Simulation physics, protection ownership, F1–F8 and the no-F9 contract remain unchanged.

## Validation required for M10.9.7.4 Hotfix 1

Run:

```bat
dotnet build
dotnet test
scripts\run-m10974-mission-performance-timeline-audit.cmd
```

Then complete:

`docs\M10_9_7_4_MANUAL_VALIDATION_CHECKLIST.md`

Only after Hotfix 1 automatic + manual gates are green may M10.9.7.4 be promoted and M10.9.7.5 closure begin.

## Evidence and package policy

Candidate source ZIPs intentionally exclude `tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/Evidence/`, generated `artifacts/`, `bin/` and `obj/`.

Compact immutable prerequisites required by ordinary/current tests live under `eng/frozen-evidence/ordinary/`; manifests live under `eng/evidence-manifests/`. Generated audit CSV/TXT payloads remain local validation records and are not copied into each subsequent candidate ZIP.

## Current unresolved items

The authoritative limitation register is `KNOWN_MODEL_LIMITATIONS.md`. In particular:

- Phase I is closed; repaired exact-v4 production and the final cumulative long/reference chain are validated;
- the historical exact-v3 I.3 drift observations remain regression provenance and are not evidence that exact @4 has identical long-horizon means/slopes;
- historical H.28 remains `bounded-but-costly`; repaired Stage 4 separately demonstrated bounded-at-or-below repaired explicit relative wall cost on the validation machine;
- branch overrides disappeared in repaired long-horizon evidence, but previous-phase hysteresis remained materially active and must not be removed without separately scoped post-Phase-I retirement evidence;
- H.5/H.21 historical numerical source seams remain retained for provenance;
- severe-incident, structural-damage and several plant-system models remain reduced-order or incomplete;
- the reduced quadratic hydraulic map is continuous but not differentiable through some near-zero/reversal transitions, and the generic runtime/numerical-conditioning findings from the post-7.3 Simulation review are assigned to M11.3/M12 rather than patched into M10 presentation work; see `SIMULATION_NUMERICAL_REGULARITY_AND_RUNTIME_REVIEW.md`;
- the post-7.3 Application review assigns fingerprint-v1 anchoring plus lifecycle-spine/recent-evidence separation to M10.9.7.4, while recorder notification cost, fingerprint cost, collection-copy traps, long-session memory growth and recorder failure policy belong to M11.2/M11.3; see `APPLICATION_RECORDING_REPLAY_REVIEW.md` and ADR-0184;
- UI-thread runtime/projection responsiveness, notification fan-out and archive-export cost remain M11.3 measurement work; stable command-target identity and `MainWindowViewModel` decomposition remain M13 work; see `DESKTOP_HOST_FAILURE_AND_SESSION_SAVE_INTEGRITY_REVIEW.md` and ADR-0185.

## Continuation rule

Phase I, M10.9.5 and M10.9.6 are closed. Continue milestone-by-milestone from the latest validated baseline: **M10.9.7.3 Hotfix 2 REV2 VALIDATED → active M10.9.7.4 Hotfix 1 ordinary-suite contract alignment → M10.9.7.5 closure**. Do not begin 7.5 until M10.9.7.4 has passed its own automatic and manual gates.

M10.9.6 challenge/demand/scoring state is observational Application state. It may consume existing plant evidence but may not issue plant commands, create supervisory authority, change protection or introduce new physics. Missing physical phenomena discovered while authoring challenges remain post-M11 backlog items rather than M10.9.6 scope expansion.

The post-Phase-I execution order remains fixed: M10.9.7 mission/performance → M10.9.8 integrated M10 validation → M11 release hardening → M12–M15 approved post-release epics. Detailed future contracts live in [`ROADMAP.md`](ROADMAP.md) and the milestone plans.

## Documentation authority

For current work use only:

- `PROJECT.md` — current checkpoint, handoff and active validation contract;
- `ROADMAP.md` — future work only;
- `KNOWN_MODEL_LIMITATIONS.md` — unresolved model limitations only;
- `ARCHITECTURE.md` — stable architecture and ownership rules;
- `README.md` — documentation navigation.

Historical milestone/hotfix detail belongs under `history/`, ADRs or the changelog and must not be copied back into current-state documents.
