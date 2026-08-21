# Project — current authoritative state

This is the **single current-state and handoff document** for Nuclear Reactor Simulator. Do not duplicate the current checkpoint in README, roadmap, milestone files or candidate-specific notes.

## Current checkpoint

**M10.9.4.1 / Phase I, M10.9.5, M10.9.6, M10.9.7.1 Hotfix 3, M10.9.7.2 REV1, M10.9.7.2 Hotfix 1 REV1, M10.9.7.2 Hotfix 2 REV1 and M10.9.7.2 Hotfix 3 REV1 are VALIDATED.** M10.9.6 remains CLOSED. M10.9.7.2 Hotfix 3 REV1 passed build, complete ordinary tests and `scripts\run-m10972-persistence-payload-integrity-audit.cmd` on 2026-08-21. Schema-v1 command numeric payloads, adapter enum/error boundaries and post-incident DTO ownership are therefore qualified before live MISSION activation.

Authoritative desktop production remains:

`integrated-operations-desktop-stable@4 | CorrelationConsistentInverseDomain | FourNodeBranchContinuityCorrectedCommitOptIn | 10 ms`

Historical exact-version identities remain immutable; no M10.9.7 presentation work reopens Phase-I numerical ownership.

## Active candidate

**M10.9.7.3 Hotfix 1 REV2 — Live Mission / Performance Historical Shell Contract Alignment — CANDIDATE.**

The candidate is rebuilt exclusively on M10.9.7.2 Hotfix 3 REV1 VALIDATED plus the Docs3 planning alignment. The original M10.9.7.3 package is SUPERSEDED / NOT VALIDATED after two compile-contract defects. The first Hotfix 1 fixed those compile contracts and built, but ordinary tests exposed stale batch-presentation ordering plus an over-broad M10.9.1 shell assertion. Hotfix 1 REV1 fixed the runtime ordering and correctly scoped the historical `GRID DEMAND` absence check; Application.Tests then passed, proving the live-source fix, while App.Tests exposed one remaining stale expectation: the top runtime block publishes current step through `RuntimeProgressText` (`STEP n`), not a direct `LogicalStepText` binding. Hotfix 1 REV2 changes only that historical test contract plus candidate metadata; the REV1 runtime fix and all 7.3 presentation semantics remain unchanged.

The live presentation path:

- accumulates external-demand/scoring evidence on every deterministic step;
- publishes immutable MISSION snapshots at presentation cadence and relevant same-step context changes;
- uses explicit structural change detection instead of generated record equality over `IReadOnlyList<>`;
- keeps `GRID DEMAND`, `REQUESTED LOAD` and `ACTUAL OUTPUT` separate;
- copies score/classification from the existing M10.9.6 owner and gives safety/protection evidence visual priority;
- exposes contextual `OPEN MISSION` navigation from COMPUTER as workspace selection only;
- gives the normal desktop startup a truthful unbound `NO ACTIVE MISSION` state rather than inferring a challenge;
- allows an exact authored pack to be bound explicitly for live/manual validation via `--mission-pack=<exact-id>`;
- adds no challenge definition, scoring arithmetic, protection authority, plant command authority or physics change.

Archive-restored mission binding and deterministic timeline/drill-down equivalence remain explicitly deferred to M10.9.7.4. A user-facing challenge launcher is not part of 7.3.

## Local validation for M10.9.7.3 Hotfix 1 REV2

```bat
dotnet build
dotnet test
scripts\run-m10973-mission-performance-live-workspace-audit.cmd
```

Automated promotion evidence must then be followed by `docs\M10_9_7_3_MANUAL_VALIDATION_CHECKLIST.md`. Only after both automated and manual HMI gates are green may M10.9.7.3 Hotfix 1 REV2 be promoted and M10.9.7.4 begin.

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
- severe-incident, structural-damage and several plant-system models remain reduced-order or incomplete.

## Continuation rule

Phase I, M10.9.5 and M10.9.6 are closed. Continue milestone-by-milestone from the latest validated baseline: M10.9.7.2 Hotfix 3 REV1 VALIDATED → active M10.9.7.3 Hotfix 1 REV2 live Mission/Performance workspace wiring → M10.9.7.4 deterministic timeline/drill-down. Do not promote the superseded pre-Hotfix-3 7.2 package or reopen numerical Phase-I/command-consequence work without direct evidence against a validated contract.

M10.9.6 challenge/demand/scoring state is observational Application state. It may consume existing plant evidence but may not issue plant commands, create supervisory authority, change protection or introduce new physics. Missing physical phenomena discovered while authoring challenges remain post-M11 backlog items rather than M10.9.6 scope expansion.

The post-Phase-I execution order remains fixed: M10.9.6 deterministic challenge/demand/scoring → M10.9.7 mission/performance presentation → M10.9.8 integrated M10 validation → M11 release hardening. Detailed contracts live in `ROADMAP.md` and `docs/milestones/M10.9.6.md` through `M11.md`.

## Documentation authority

For current work use only:

- `PROJECT.md` — current checkpoint, handoff and active validation contract;
- `ROADMAP.md` — future work only;
- `KNOWN_MODEL_LIMITATIONS.md` — unresolved model limitations only;
- `ARCHITECTURE.md` — stable architecture and ownership rules;
- `README.md` — documentation navigation.

Historical milestone/hotfix detail belongs under `history/`, ADRs or the changelog and must not be copied back into current-state documents.
