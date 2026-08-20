# Project — current authoritative state

This is the **single current-state and handoff document** for Nuclear Reactor Simulator. Do not duplicate the current checkpoint in README, roadmap, milestone files or candidate-specific notes.

## Current checkpoint

**M10.9.4.1 / Phase I and M10.9.5 are VALIDATED and CLOSED.** The repaired-v4 Phase-I cumulative chain is green and the completed Contextual Command Consequence Model is the validated operator-experience baseline for M10.9.6.

Authoritative desktop production is:

`integrated-operations-desktop-stable@4 | CorrelationConsistentInverseDomain | FourNodeBranchContinuityCorrectedCommitOptIn | 10 ms`

Historical identities remain immutable:

- exact desktop `@3` = historical corrected-commit + `HistoricalCorrelationTopology` replay/evidence provenance;
- exact desktop `@2` = fail-closed `ExplicitCommittedState` rollback/reference;
- synchronization remains a separate exact-version family; supported current synchronization is `pre-synchronization-grid-loading@3 | FourNodeBranchContinuityCorrectedCommitOptIn`.

Final Phase-I closure evidence is green across ordinary/current evidence, GameplayLong, OperationalEnvelope, ReferencePlantScale, synchronization-v3 and repaired-v4 300 s reference requalification. The repaired-v4 300 s gate completed 30,000 steps with zero health/reverse-flow violations, 0/19 frozen I.3 budget violations, 20/20 corrected trigger/eligible/authorized/commit, zero rollback/fallback/unsafe/untargeted disagreement and deterministic repeat.

## Active candidate

**M10.9.6.1 — Operational Challenge & Energy-Demand Framework / Challenge Lifecycle & Logical-Time Contract — CANDIDATE.**

**Hotfix 1 status:** the first validation attempt was blocked only by two xUnit2013 analyzer errors in the new focused test (`Assert.Equal(1, collection.Count)`). Hotfix 1 replaces them with `Assert.Single`; the M10.9.6.1 Application contract is unchanged.

M10.9.5 is **VALIDATED and CLOSED** after build, complete ordinary tests, the cumulative `run-m1095-command-consequence-closure-audit.cmd` gate and explicit continuation beyond the required manual HMI closure gate. Its authored consequence semantics, bounded dependency chains, F4 COMMANDS/context/mimic integration and logical-step observed-response evidence are frozen as the validated operator-experience baseline.

M10.9.6.1 adds only deterministic Application-layer challenge lifecycle ownership. A versioned challenge references an existing scenario objective, authored activation/observation/completion/failure conditions, logical-step readiness/target/deadline metadata, allowed assistance modes and a future scoring-policy identity. The tracker consumes only immutable `ControlRoomSnapshot` evidence plus accepted operator-action history through a read-only evidence seam. It has no plant command dispatcher, supervisory authority or Simulation mutation path.

Lifecycle is `NOT STARTED -> READY -> ACTIVE -> COMPLETED|FAILED|CANCELLED`. Target completion windows are observational; only an explicitly authored hard logical-step deadline can fail a challenge by time. Declared failure conditions take precedence if completion and failure become true in the same logical step. Cancel/reset are explicit lifecycle operations and are never driven by presentation navigation. No demand profile, score arithmetic or challenge UI is introduced in M10.9.6.1.

## Local validation for M10.9.6.1

Run:

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-m1096-challenge-lifecycle-audit.cmd
```

Promotion requires build, complete ordinary suite and the focused lifecycle/logical-time gate to be green. If a gate fails, fix only the demonstrated challenge-contract defect. Do not introduce external demand, scoring arithmetic, UI or physical/control changes while closing M10.9.6.1. If green, M10.9.6.1 becomes VALIDATED and M10.9.6.2 deterministic external energy-demand profiles is next.

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

Phase I and M10.9.5 are closed. Continue milestone-by-milestone from the latest validated baseline: validated M10.9.5 → active M10.9.6.1 lifecycle/logical-time contract. Do not reopen numerical Phase-I or command-consequence work unless a later gate produces direct evidence that a validated contract is defective.

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
