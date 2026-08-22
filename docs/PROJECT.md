# Project — current authoritative state

This is the **single current-state and handoff document** for Nuclear Reactor Simulator. Do not duplicate the current checkpoint in README, roadmap, milestone files or candidate-specific notes.

## Current checkpoint

**M10.9.4.1 / Phase I, M10.9.5, M10.9.6 and M10.9.7 are VALIDATED / CLOSED.** M10.9.8.1 REV1 Docs1, M10.9.8.2 Hotfix 1 REV5, M10.9.8.3 and M10.9.8.4 Hotfix 1 are VALIDATED. M10.9.8.2 REV5 remains the current production/runtime baseline because later M10.9.8.3/8.4 are test/evidence-only; M10.9.8.4 Hotfix 1 is the sole stacking baseline for M10.9.8.5.

The validated M10.9.7 baseline includes the live read-only MISSION workspace, deterministic logical-step timeline, presentation-only drill-down, exact mission/archive binding, replay/checkpoint reconstruction, closure coverage for active/completed/failed mission states, assistance changes and requested/effective authority divergence. F1–F8 remain preserved, F9 remains absent and MISSION has no plant-command authority.

Authoritative desktop production remains:

`integrated-operations-desktop-stable@4 | CorrelationConsistentInverseDomain | FourNodeBranchContinuityCorrectedCommitOptIn | 10 ms`

Historical exact-version identities remain immutable; M10.9.8 validation work does not reopen Phase-I numerical ownership without direct contradictory evidence.

## Active candidate

**M10.9.8.5 — Manual Integrated HMI Acceptance & M10.9.8 Closure — CANDIDATE.**

M10.9.8.4 Hotfix 1 is VALIDATED after build, complete ordinary suite and the replay/checkpoint/same-seed focused audit passed on 2026-08-22. M10.9.8.5 stacks exclusively on that baseline and is manual/docs closure-only: no production runtime, compiled/test surface, Simulation physics, archive schema, fingerprint algorithm, challenge/scoring/protection ownership or plant-command authority change.

The automated preflight `scripts/run-m1098-integrated-human-automation-hmi-audit.cmd` revalidates the frozen M10.9.8.1/8.2/8.3/8.4 contracts and reruns representative HMI/session/authority/list-stability owners. The required manual route is `M10_9_8_5_MANUAL_INTEGRATED_HMI_ACCEPTANCE_CHECKLIST.md` with twelve routes covering startup/minimum window, keyboard navigation, F1–F8, assistance/authority modes, production mission @2, F4 command/dependency stability and ENTER dispatch, target selectors, F8 checkpoint/replay, protection/alarm/first-out, MISSION drill-down/timeline, unavailable/degraded truth, manual takeover and terminal-mission/continuing-plant visibility.

M10.9.8 closes only after explicit `M10.9.8.5 manual integrated HMI acceptance OK`. Even then **M10 remains OPEN**: M11 is blocked until the mandatory cumulative final M10 validation and separate approximately one-hour operational long gate defined in `M10_FINAL_PRE_M11_VALIDATION_PLAN.md` both pass.

## Validation required for active M10.9.8.5

Run:

```bat
dotnet build
dotnet test
scripts\run-m1098-integrated-human-automation-hmi-audit.cmd
```

Then complete `M10_9_8_5_MANUAL_INTEGRATED_HMI_ACCEPTANCE_CHECKLIST.md` and report `M10.9.8.5 manual integrated HMI acceptance OK`. This closes M10.9.8 only; the final M10 cumulative/long gates remain mandatory.

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
- fingerprint-v1 anchoring plus lifecycle-spine/recent-evidence separation are validated through M10.9.7.4/M10.9.8; recorder notification cost, fingerprint cost, collection-copy traps, long-session memory growth and recorder failure policy remain M11.2/M11.3 ownership; see `APPLICATION_RECORDING_REPLAY_REVIEW.md` and ADR-0184;
- UI-thread runtime/projection responsiveness, notification fan-out and archive-export cost remain M11.3 measurement work; stable command-target identity and `MainWindowViewModel` decomposition remain M13 work; see `DESKTOP_HOST_FAILURE_AND_SESSION_SAVE_INTEGRITY_REVIEW.md` and ADR-0185.

## Continuation rule

Phase I, M10.9.5, M10.9.6 and M10.9.7 are closed. Continue milestone-by-milestone from the latest validated baseline: **M10.9.7.5 Hotfix 1 VALIDATED → M10.9.8.1 REV1 Docs1 VALIDATED → M10.9.8.2 Hotfix 1 REV5 VALIDATED → M10.9.8.3 VALIDATED → M10.9.8.4 Hotfix 1 VALIDATED → active M10.9.8.5 manual integrated HMI acceptance → mandatory M10 Final Pre-M11 cumulative + long validation → M11**.

M10.9.6 challenge/demand/scoring state is observational Application state. It may consume existing plant evidence but may not issue plant commands, create supervisory authority, change protection or introduce new physics. Missing physical phenomena discovered while authoring challenges remain post-M11 backlog items rather than M10.9.6 scope expansion.

The post-Phase-I execution order remains fixed: M10.9.7 mission/performance → M10.9.8 integrated M10 validation → mandatory final pre-M11 cumulative/long M10 validation → M11 release hardening → M12–M15 approved post-release epics. The final pre-M11 contract is `M10_FINAL_PRE_M11_VALIDATION_PLAN.md`. Detailed future contracts live in [`ROADMAP.md`](ROADMAP.md) and the milestone plans.

## Documentation authority

For current work use only:

- `PROJECT.md` — current checkpoint, handoff and active validation contract;
- `ROADMAP.md` — future work only;
- `KNOWN_MODEL_LIMITATIONS.md` — unresolved model limitations only;
- `ARCHITECTURE.md` — stable architecture and ownership rules;
- `README.md` — documentation navigation.

Historical milestone/hotfix detail belongs under `history/`, ADRs or the changelog and must not be copied back into current-state documents.
