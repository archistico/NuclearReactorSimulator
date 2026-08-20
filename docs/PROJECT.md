# Project — current authoritative state

This is the **single current-state and handoff document** for Nuclear Reactor Simulator. Do not duplicate the current checkpoint in README, roadmap, milestone files or candidate-specific notes.

## Current checkpoint

**M10.9.4.1 / Phase I and M10.9.5 are VALIDATED and CLOSED; M10.9.6.1, M10.9.6.2 Hotfix 1 and M10.9.6.3 Hotfix 1 are VALIDATED.** The repaired-v4 Phase-I cumulative chain and completed Contextual Command Consequence Model remain frozen prerequisites; deterministic challenge lifecycle, external-demand semantics and multidimensional scoring are now the validated baseline for M10.9.6.4.

Authoritative desktop production is:

`integrated-operations-desktop-stable@4 | CorrelationConsistentInverseDomain | FourNodeBranchContinuityCorrectedCommitOptIn | 10 ms`

Historical identities remain immutable:

- exact desktop `@3` = historical corrected-commit + `HistoricalCorrelationTopology` replay/evidence provenance;
- exact desktop `@2` = fail-closed `ExplicitCommittedState` rollback/reference;
- synchronization remains a separate exact-version family; supported current synchronization is `pre-synchronization-grid-loading@3 | FourNodeBranchContinuityCorrectedCommitOptIn`.

Final Phase-I closure evidence is green across ordinary/current evidence, GameplayLong, OperationalEnvelope, ReferencePlantScale, synchronization-v3 and repaired-v4 300 s reference requalification. The repaired-v4 300 s gate completed 30,000 steps with zero health/reverse-flow violations, 0/19 frozen I.3 budget violations, 20/20 corrected trigger/eligible/authorized/commit, zero rollback/fallback/unsafe/untargeted disagreement and deterministic repeat.

## Active candidate

**M10.9.6.4 — Initial Challenge Packs — CANDIDATE.**

M10.9.6.1 Hotfix 1, M10.9.6.2 Hotfix 1 and M10.9.6.3 Hotfix 1 are **VALIDATED** after build, complete ordinary tests and their focused lifecycle, external-demand and multidimensional-scoring audits passed. Lifecycle/logical-time, demand semantics and exact scoring policies are frozen prerequisites.

M10.9.6.4 composes six versioned challenge packs from existing validated M7.2/M7.5/M7.6 scenario/check owners and the existing M8.4 generator-trip/load-rejection fault owner. The pack layer introduces no new fault, plant physics, command authority, protection ownership or UI.

The initial catalog covers pre-start circulation preparation, synchronization/initial loading, bounded 5→10→5 MWe demand-following, post-load-change 10 MWe stabilization, controlled normal shutdown and generator-trip/load-rejection response. Only the bounded demand-following challenge exposes the next scheduled demand change; post-load-change stabilization exposes current demand only; synchronization owns no demand profile. External demand never writes generator requested load.

Every pack binds one exact scoring policy and one documented evidence source for each policy dimension, but M10.9.6.4 performs no score arithmetic. Challenge failure semantics remain local: unexpected trips are failures only in authored normal-operation challenges, while the generator trip is required evidence in the load-rejection response challenge. No hard failure deadlines are introduced before M10.9.6.5 runtime qualification.

Technical reference: `OPERATIONAL_CHALLENGE_PACKS.md`.

## Local validation for M10.9.6.4

Run:

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-m1096-initial-challenge-pack-audit.cmd
```

Promotion requires build, complete ordinary suite and the focused initial challenge-pack gate to be green. If a gate fails, fix only the demonstrated pack/evidence-composition defect. Do not add UI, new faults, control retuning or physical changes while closing M10.9.6.4. If green, M10.9.6.4 becomes VALIDATED and M10.9.6.5 replay/checkpoint/determinism closure is next.

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

Phase I and M10.9.5 are closed; M10.9.6.1, M10.9.6.2 Hotfix 1 and M10.9.6.3 Hotfix 1 are validated. Continue milestone-by-milestone from the latest validated baseline: frozen lifecycle/logical-time + external-demand + scoring contracts → active M10.9.6.4 initial challenge packs. Do not reopen numerical Phase-I or command-consequence work unless a later gate produces direct evidence that a validated contract is defective.

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
