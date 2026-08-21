# Project — current authoritative state

This is the **single current-state and handoff document** for Nuclear Reactor Simulator. Do not duplicate the current checkpoint in README, roadmap, milestone files or candidate-specific notes.

## Current checkpoint

**M10.9.4.1 / Phase I, M10.9.5, M10.9.6, M10.9.7.1 Hotfix 3, M10.9.7.2 REV1, M10.9.7.2 Hotfix 1 REV1 and M10.9.7.2 Hotfix 2 REV1 are VALIDATED.** M10.9.6 remains CLOSED. M10.9.7.2 Hotfix 2 REV1 passed build, complete ordinary tests and `scripts\run-m10972-ten-ms-hot-path-hardening-audit.cmd` on 2026-08-21. The pre-live 10 ms hot-path hardening is therefore qualified: challenge observation version tracking, indexed immutable plant registries reused by `PlantState`, and cached compressible-steam critical ratio are validated. Option A remains frozen: future dedicated `MISSION` / `Mission & Performance`, contextual navigation from COMPUTER, unchanged F1-F8, no F9, no plant-command authority and `UiRouteActivated=False`.

Authoritative desktop production is:

`integrated-operations-desktop-stable@4 | CorrelationConsistentInverseDomain | FourNodeBranchContinuityCorrectedCommitOptIn | 10 ms`

Historical exact-version identities remain immutable; no Phase-I numerical contract is reopened by M10.9.7.

## Active candidate

**M10.9.7.2 Hotfix 3 REV1 — JsonDocument Parse Exception-Type Test Alignment — CANDIDATE.**

The distributed **Docs1 documentation-alignment rebuild** changes documentation only; `src/`, `tests/`, validation scripts and the Hotfix 3 REV1 runtime/test contract remain unchanged. Local validation is therefore still the Hotfix 3 REV1 gate below.

Hotfix 3 REV1 is stacked exclusively on M10.9.7.2 Hotfix 2 REV1 VALIDATED. The original Hotfix 3 package is SUPERSEDED / NOT VALIDATED after one Infrastructure regression assertion required the exact `JsonException` runtime type. REV1 keeps the persistence runtime byte-identical to that package and changes only the malformed-scenario exception assertion to accept the public `JsonException` contract. No local build/test result has yet been reported for REV1.

The candidate closes persistence defects before any live workstation route is activated:

- schema-v1 session archives persist `ControlRoomCommand.NumericValue` in operator actions and recorder events;
- a real turbine-control-valve manual-demand sequence is verified through serialize → deserialize → full replay;
- incomplete manual-demand payloads and undefined persisted command/target/event enum values fail at the archive boundary;
- post-incident JSON owns a private command DTO rather than persisting the Application record directly;
- malformed/structurally invalid scenario, checkpoint, post-incident and session-archive data follow the same `InvalidDataException` boundary contract, while future schema versions remain `NotSupportedException`;
- session archive schema remains v1 and numeric enum ordinals are frozen by executable tests.

String-enum schema migration and stream-based persistence APIs are explicitly deferred. Replay authority, scenario semantics, hot-path optimization, F1-F8, `UiRouteActivated=false`, scoring, challenge definitions, protection, physics and plant command authority remain unchanged.

## Local validation for M10.9.7.2 Hotfix 3 REV1

```bat
dotnet build
dotnet test
scripts\run-m10972-persistence-payload-integrity-audit.cmd
```

Promotion requires all three gates green. After validation, M10.9.7.3 may begin live Mission/Performance wiring with explicit presentation change detection.

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

Phase I, M10.9.5 and M10.9.6 are closed. Continue milestone-by-milestone from the latest validated baseline: M10.9.7.2 Hotfix 2 REV1 VALIDATED → active M10.9.7.2 Hotfix 3 REV1 persistence payload/error-contract closure (validation pending) → M10.9.7.3 live workstation implementation. Do not promote the superseded pre-Hotfix-3 7.2 package or reopen numerical Phase-I/command-consequence work without direct evidence against a validated contract.

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
