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

**M10.9.6.3 Hotfix 1 — Missing Parent Challenge Namespace Test Compile Fix — CANDIDATE.**

M10.9.6.1 Hotfix 1 and M10.9.6.2 Hotfix 1 are **VALIDATED** after build, complete ordinary tests and their focused lifecycle/external-demand audits passed. Lifecycle/logical-time and external-demand semantics are frozen prerequisites.

The first M10.9.6.3 build compiled every production project and failed only in `NuclearReactorSimulator.Application.Tests` with CS0246 because the new scoring test omitted `using NuclearReactorSimulator.Application.Scenarios.Challenges;`. Hotfix 1 is test-only and changes no scoring semantics or production source.

M10.9.6.3 adds only deterministic Application-layer scoring arithmetic. Standard exact v1 policies are `general-operations@1` (SAFETY 45 / PROCEDURE 30 / STABILITY 20 / LOGICAL TIME 5) and `demand-following@1` (SAFETY 40 / PROCEDURE 25 / STABILITY 15 / DEMAND 15 / LOGICAL TIME 5). Grade thresholds are pass 60%, proficient 75%, excellent 90%.

Safety/procedure dominate: authored critical safety failure makes the result non-passing and caps it at 39%; authored critical procedure failure caps it at 59%; safety wins if both exist. Missing required evidence scores zero and makes evaluation incomplete/non-passing. A protection trip is not globally a scoring failure: challenge-owned evidence decides whether an event is failure, protected completion or other observation.

Guidance mode and plant-control authority remain distinct inputs. Standard v1 policies explicitly use neutral 1.00 modifiers for every defined guidance/authority mode, so there is no hidden assistance penalty. Any non-neutral modifier requires an explicit versioned policy. `ChallengeScoreCalculator` owns no command dispatcher, control authority, protection ownership, wall-clock or Simulation mutation path.

Technical reference: `OPERATIONAL_CHALLENGE_SCORING.md`.

## Local validation for M10.9.6.3 Hotfix 1

Run:

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-m1096-multidimensional-scoring-audit.cmd
```

Promotion requires build, complete ordinary suite and the focused multidimensional scoring gate to be green. If a gate fails, fix only the demonstrated scoring-contract defect. Do not add challenge packs, UI, new faults, control retuning or physical changes while closing M10.9.6.3. If green, M10.9.6.3 becomes VALIDATED and M10.9.6.4 initial challenge packs are next.

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

Phase I and M10.9.5 are closed; M10.9.6.1 and M10.9.6.2 are validated. Continue milestone-by-milestone from the latest validated baseline: frozen lifecycle/logical-time + external-demand contracts → active M10.9.6.3 multidimensional scoring. Do not reopen numerical Phase-I or command-consequence work unless a later gate produces direct evidence that a validated contract is defective.

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
