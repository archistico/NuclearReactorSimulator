# Project — current authoritative state

This is the **single current-state and handoff document** for Nuclear Reactor Simulator. Do not duplicate the current checkpoint in README, roadmap, milestone files or candidate-specific notes.

## Current checkpoint

**M10.9.4.1 / Phase I, M10.9.5, M10.9.6, M10.9.7.1 Hotfix 3, M10.9.7.2 REV1, M10.9.7.2 Hotfix 1 REV1, M10.9.7.2 Hotfix 2 REV1, M10.9.7.2 Hotfix 3 REV1 and M10.9.7.3 Hotfix 1 REV2 are VALIDATED.** M10.9.6 remains CLOSED. M10.9.7.3 Hotfix 1 REV2 passed build, the complete ordinary suite, `scripts\run-m10973-mission-performance-live-workspace-audit.cmd` and `docs\M10_9_7_3_MANUAL_VALIDATION_CHECKLIST.md` on 2026-08-21. The dedicated read-only `MISSION` / `Mission & Performance` workspace is therefore the current validated presentation baseline; COMPUTER F1–F8 remain fixed, no F9 exists, normal startup remains mission-unbound and explicit exact pack binding remains required for live mission validation.

Authoritative desktop production remains:

`integrated-operations-desktop-stable@4 | CorrelationConsistentInverseDomain | FourNodeBranchContinuityCorrectedCommitOptIn | 10 ms`

Historical exact-version identities remain immutable; no M10.9.7 presentation/host work reopens Phase-I numerical ownership.

## Active candidate

**M10.9.7.3 Hotfix 2 REV2 — Desktop Host Failure & Session Save Integrity — CANDIDATE.**

This candidate is stacked exclusively on **M10.9.7.3 Hotfix 1 REV2 VALIDATED** plus the Docs4 documentation alignment. It closes the two pre-7.4 integrity gaps identified by the App review without changing Mission/Performance semantics:

- `DesktopControlRoomRuntimePump` classifies expected deterministic-step `InvalidOperationException`/`ArithmeticException` failures, including `OverflowException`, and converts them into one PAUSE + diagnostic boundary; unknown/programming exceptions remain unhandled rather than silently swallowed;
- start-recorded-session and reset/recreate-session are protected by the same explicit runtime-construction failure policy; load, restore and save share one archive-operation failure classifier;
- SAVE opens the picker **before** full archive export; cancellation therefore performs no archive serialization;
- safe desktop overwrite requires a local filesystem path from the storage provider, writes a unique temporary sibling, flushes it durably, and only then moves/replaces the destination;
- existing archives are never opened and truncated before replacement is complete; injected write/replace failures preserve the previous destination under the local-filesystem contract and temporary cleanup is best-effort;
- providers that cannot expose a safe local path fail closed rather than falling back to destructive truncate-first write;
- the remaining App gauge-scale and COMPUTER setpoint numbers use the same invariant technical decimal convention as canonical HMI values.

No Simulation physics, fixed timestep, challenge definition, score arithmetic, protection authority, plant-command authority, MISSION navigation contract or archive schema changes in Hotfix 2.

## Validation required for M10.9.7.3 Hotfix 2 REV2

Run:

```bat
dotnet build
dotnet test
scripts\run-m10973-desktop-host-session-integrity-audit.cmd
```

Then complete:

`docs\M10_9_7_3_HOTFIX2_MANUAL_VALIDATION_CHECKLIST.md`

The original Hotfix 2 candidate is SUPERSEDED / NOT VALIDATED because compilation stopped only on eight xUnit1051 violations in the newly added async App tests. Hotfix 2 REV1 fixed those analyzer call sites but is also SUPERSEDED / NOT VALIDATED: the ordinary suite then exposed three contract issues — `InvalidDataException` was missing from the centralized archive failure classifier, the new-file save path attempted an unnecessary backup cleanup, and the historical M10.9.7.1 source regression still expected the superseded inline catch list. Hotfix 2 REV2 fixes those three points without changing the broader host/save design.

Only after the automated gate and manual save/load checks are green may Hotfix 2 REV2 be promoted. M10.9.7.4 remains blocked until that promotion.

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

Phase I, M10.9.5 and M10.9.6 are closed. Continue milestone-by-milestone from the latest validated baseline: **M10.9.7.3 Hotfix 1 REV2 VALIDATED → active M10.9.7.3 Hotfix 2 REV2 desktop-host/session-integrity closure → M10.9.7.4 deterministic timeline/drill-down/replay-equivalence**. Do not begin 7.4 until Hotfix 2 REV2 has passed its own automatic and manual gates.

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
