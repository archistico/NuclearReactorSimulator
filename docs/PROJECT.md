# Project — current authoritative state

This is the **single current-state and handoff document** for Nuclear Reactor Simulator. Do not duplicate the current checkpoint in README, roadmap, milestone files or candidate-specific notes.

## Current checkpoint

**M10.9.4.1 / Phase I, M10.9.5, M10.9.6, M10.9.7.1 Hotfix 3, M10.9.7.2 REV1, M10.9.7.2 Hotfix 1 REV1, M10.9.7.2 Hotfix 2 REV1, M10.9.7.2 Hotfix 3 REV1, M10.9.7.3 Hotfix 1 REV2, M10.9.7.3 Hotfix 2 REV2 and M10.9.7.4 Hotfix 1 are VALIDATED.** M10.9.6 remains CLOSED. M10.9.7.4 Hotfix 1 passed build, the complete ordinary suite, `scripts\run-m10974-mission-performance-timeline-audit.cmd` and its manual timeline/drill-down/archive checklist on 2026-08-22. The validated baseline therefore includes the live read-only `MISSION` workspace, deterministic logical-step timeline, presentation-only drill-down and verified archive/checkpoint mission reconstruction.

Authoritative desktop production remains:

`integrated-operations-desktop-stable@4 | CorrelationConsistentInverseDomain | FourNodeBranchContinuityCorrectedCommitOptIn | 10 ms`

Historical exact-version identities remain immutable; no M10.9.7 presentation/closure work reopens Phase-I numerical ownership.

## Active candidate

**M10.9.7.5 Hotfix 1 — Mission/Performance Closure Audit Wrapper Repair — CANDIDATE.**

M10.9.7.5 Hotfix 1 is stacked **exclusively on the original M10.9.7.5 closure candidate**, itself stacked on M10.9.7.4 Hotfix 1 VALIDATED. The original candidate compiled and passed the complete ordinary suite, but its focused Windows `.cmd` aborted on the `run_app_class` batch subroutine lookup and is therefore SUPERSEDED / NOT VALIDATED. Hotfix 1 repairs only that audit wrapper and adds source-level regression coverage. It remains a closure gate, not feature work. Production XAML/runtime semantics, Simulation physics, challenge/scoring/protection ownership, archive schema, fingerprint algorithm and plant-command authority remain unchanged.

The closure candidate adds cumulative executable evidence for the frozen M10.9.7 matrix:

- no active mission remains explicit/unbound and never fabricates mission state;
- active missions with no external-demand profile keep requested and actual output visible while external demand/error remain unavailable;
- bounded demand-following keeps GRID DEMAND, REQUESTED LOAD and ACTUAL OUTPUT semantically distinct;
- Active, Completed and Failed lifecycle states remain presentable;
- terminal lifecycle boundaries remain frozen while plant logical time may continue;
- generator trip remains required evidence for the dedicated load-rejection challenge but an explicit authored failure for normal-operation challenges where unexpected;
- assistance-mode changes and requested/effective control-authority divergence remain observational presentation state;
- checkpoint/full-archive replay and continuation reuse the already validated deterministic M10.9.6/M10.9.7.4 evidence path;
- F1–F8 remain preserved, F9 remains absent, MISSION plant-command authority remains false and score remains copied from the M10.9.6 owner.

See `MISSION_PERFORMANCE_CLOSURE.md`.

## Validation required for M10.9.7.5 Hotfix 1

Run:

```bat
dotnet build
dotnet test
scripts\run-m1097-mission-performance-closure-audit.cmd
```

Then complete:

`docs\M10_9_7_5_MANUAL_VALIDATION_CHECKLIST.md`

Only after automatic + manual closure gates are green may **M10.9.7 be declared VALIDATED/CLOSED** and M10.9.8 begin.

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

Phase I, M10.9.5 and M10.9.6 are closed. Continue milestone-by-milestone from the latest validated baseline: **M10.9.7.4 Hotfix 1 VALIDATED → active M10.9.7.5 Hotfix 1 closure → M10.9.8 integrated validation**. Do not begin M10.9.8 until M10.9.7.5 Hotfix 1 has passed build, ordinary tests, the focused closure gate and explicit manual closure acceptance.

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
