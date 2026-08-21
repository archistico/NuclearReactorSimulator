# Project — current authoritative state

This is the **single current-state and handoff document** for Nuclear Reactor Simulator. Do not duplicate the current checkpoint in README, roadmap, milestone files or candidate-specific notes.

## Current checkpoint

**M10.9.4.1 / Phase I, M10.9.5, M10.9.6, M10.9.7.1 Hotfix 3, M10.9.7.2 REV1, M10.9.7.2 Hotfix 1 REV1, M10.9.7.2 Hotfix 2 REV1 and M10.9.7.2 Hotfix 3 REV1 are VALIDATED.** M10.9.6 remains CLOSED. M10.9.7.2 Hotfix 3 REV1 passed build, complete ordinary tests and `scripts\run-m10972-persistence-payload-integrity-audit.cmd` on 2026-08-21. Schema-v1 command numeric payloads, adapter enum/error boundaries and post-incident DTO ownership are therefore qualified before live MISSION activation.

Authoritative desktop production remains:

`integrated-operations-desktop-stable@4 | CorrelationConsistentInverseDomain | FourNodeBranchContinuityCorrectedCommitOptIn | 10 ms`

Historical exact-version identities remain immutable; no M10.9.7 presentation work reopens Phase-I numerical ownership.

## Active candidate

**M10.9.7.3 Hotfix 1 REV2 — Live Mission / Performance Historical Shell Contract Alignment — CANDIDATE.**

The runtime candidate was originally rebuilt exclusively on M10.9.7.2 Hotfix 3 REV1 VALIDATED plus the pre-7.3 Detailed Forward Execution Plan documentation baseline. The distributed Docs1/Docs2/Docs3/Docs4 alignments change documentation only: Docs1 records the post-7.3 Simulation review disposition, Docs2 records the Application recording/replay review disposition, Docs3 records the App desktop-host/session-integrity review disposition, and Docs4 reorganizes documentation architecture/indexing, normalizes ADR discoverability and aligns current limitation/reference documentation. None changes the REV2 runtime/test/script candidate or its already-green automated evidence. Build, the complete ordinary suite and `scripts\run-m10973-mission-performance-live-workspace-audit.cmd` have now passed for Hotfix 1 REV2; **manual HMI validation is still pending**, so REV2 is not yet promoted. The original M10.9.7.3 package is SUPERSEDED / NOT VALIDATED after two compile-contract defects. The first Hotfix 1 fixed those compile contracts and built, but ordinary tests exposed stale batch-presentation ordering plus an over-broad M10.9.1 shell assertion. Hotfix 1 REV1 fixed the runtime ordering and correctly scoped the historical `GRID DEMAND` absence check; Application.Tests then passed, proving the live-source fix, while App.Tests exposed one remaining stale expectation: the top runtime block publishes current step through `RuntimeProgressText` (`STEP n`), not a direct `LogicalStepText` binding. Hotfix 1 REV2 changes only that historical test contract plus candidate metadata; the REV1 runtime fix and all 7.3 presentation semantics remain unchanged.

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

## Remaining validation for M10.9.7.3 Hotfix 1 REV2

The automated build, ordinary-suite and focused live-workspace gate are green. Promotion now requires only completion of `docs\M10_9_7_3_MANUAL_VALIDATION_CHECKLIST.md`. If source/test/script files change before that review, rerun the automated gates; documentation-only alignment does not invalidate the already reported automated result. Only after the manual HMI gate is green may M10.9.7.3 Hotfix 1 REV2 be promoted. **M10.9.7.4 still must not begin immediately:** the accepted App review requires a separate M10.9.7.3 Hotfix 2 — Desktop Host Failure & Session Save Integrity — stacked only on REV2 VALIDATED and itself validated first.

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
- the reduced quadratic hydraulic map is continuous but not differentiable through some near-zero/reversal transitions, and the generic runtime/numerical-conditioning findings from the post-7.3 Simulation review are explicitly assigned to M11.3/M12 rather than patched into M10 presentation work; see `SIMULATION_NUMERICAL_REGULARITY_AND_RUNTIME_REVIEW.md`;
- the post-7.3 Application review assigns fingerprint-v1 anchoring plus lifecycle-spine/recent-evidence separation to M10.9.7.4, while recorder notification cost, fingerprint cost, collection-copy traps, long-session memory growth and recorder failure policy belong to M11.2/M11.3; see `APPLICATION_RECORDING_REPLAY_REVIEW.md` and ADR-0184;
- the post-7.3 App review identifies desktop numerical-failure containment and non-destructive/atomic session replacement as pre-7.4 integrity work. After REV2 manual validation, M10.9.7.3 Hotfix 2 must close these items before 7.4; UI-thread/projection measurement belongs to M11.3 and stable command-target selection/MainWindowViewModel decomposition to M13. See `DESKTOP_HOST_FAILURE_AND_SESSION_SAVE_INTEGRITY_REVIEW.md` and ADR-0185.

## Continuation rule

Phase I, M10.9.5 and M10.9.6 are closed. Continue milestone-by-milestone from the latest validated baseline: M10.9.7.2 Hotfix 3 REV1 VALIDATED → active M10.9.7.3 Hotfix 1 REV2 live Mission/Performance workspace wiring/manual review → planned M10.9.7.3 Hotfix 2 desktop-host/session-integrity closure → M10.9.7.4 deterministic timeline/drill-down. Do not promote the superseded pre-Hotfix-3 7.2 package or reopen numerical Phase-I/command-consequence work without direct evidence against a validated contract.

M10.9.6 challenge/demand/scoring state is observational Application state. It may consume existing plant evidence but may not issue plant commands, create supervisory authority, change protection or introduce new physics. Missing physical phenomena discovered while authoring challenges remain post-M11 backlog items rather than M10.9.6 scope expansion.

The post-Phase-I execution order remains fixed: M10.9.6 deterministic challenge/demand/scoring → M10.9.7 mission/performance presentation → M10.9.8 integrated M10 validation → M11 release hardening. Detailed contracts live in [`ROADMAP.md`](ROADMAP.md) and the milestone plans from [`M10.9.6.md`](milestones/M10.9.6.md) through [`M11.md`](milestones/M11.md).

## Documentation authority

For current work use only:

- `PROJECT.md` — current checkpoint, handoff and active validation contract;
- `ROADMAP.md` — future work only;
- `KNOWN_MODEL_LIMITATIONS.md` — unresolved model limitations only;
- `ARCHITECTURE.md` — stable architecture and ownership rules;
- `README.md` — documentation navigation.

Historical milestone/hotfix detail belongs under `history/`, ADRs or the changelog and must not be copied back into current-state documents.
