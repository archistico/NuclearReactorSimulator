# Project — current authoritative state

This is the **single current-state and handoff document** for Nuclear Reactor Simulator. Do not duplicate the current checkpoint in README, roadmap, milestone files or candidate-specific notes.

## Current checkpoint

**M10.9.4.1 / Phase I is VALIDATED and CLOSED.** The final repaired-v4 cumulative chain is green and `m1095-unblocked=True`.

Authoritative desktop production is:

`integrated-operations-desktop-stable@4 | CorrelationConsistentInverseDomain | FourNodeBranchContinuityCorrectedCommitOptIn | 10 ms`

Historical identities remain immutable:

- exact desktop `@3` = historical corrected-commit + `HistoricalCorrelationTopology` replay/evidence provenance;
- exact desktop `@2` = fail-closed `ExplicitCommittedState` rollback/reference;
- synchronization remains a separate exact-version family; supported current synchronization is `pre-synchronization-grid-loading@3 | FourNodeBranchContinuityCorrectedCommitOptIn`.

Final Phase-I closure evidence is green across ordinary/current evidence, GameplayLong, OperationalEnvelope, ReferencePlantScale, synchronization-v3 and repaired-v4 300 s reference requalification. The repaired-v4 300 s gate completed 30,000 steps with zero health/reverse-flow violations, 0/19 frozen I.3 budget violations, 20/20 corrected trigger/eligible/authorized/commit, zero rollback/fallback/unsafe/untargeted disagreement and deterministic repeat.

## Active candidate

**M10.9.5.4 — Observed Response Evidence — CANDIDATE.**

M10.9.5.1 consequence semantics, M10.9.5.2 explicit dependency-chain projection and M10.9.5.3 Hotfix 2 COMMANDS/context-inspector/schematic integration are explicitly validated post-Phase-I baselines. M10.9.5.4 adds the missing post-dispatch evidence layer without reopening their contracts.

For each F4 COMMANDS attempt, the presentation distinguishes accepted/rejected dispatch feedback from plant response. Accepted dispatch captures the command's authored M10.9.5.1 monitor set at the dispatch logical step and compares it with later UI-safe snapshots through a bounded **500 logical-step** window. The presentation reports baseline/latest value or state, actual delta/direction when meaningful, and observed protection state. It explicitly labels the result as post-dispatch co-variation: it does not infer that every change was caused by the command and it does not invent generic `SUCCESS/FAILURE` from numeric deltas.

Advisory-blocked commands, missing-dispatcher cases and canonical runtime/scenario rejection record rejection feedback but expose **no fictional plant-effect deltas**. Observation samples are derivable `[JsonIgnore]` presentation evidence on the existing command snapshot and therefore do not change save/replay fingerprints or authoritative physical state.

No physics, Simulation state, command type, dispatch owner, protection/permissive owner, exact-version identity, fixed timestep, challenge/scoring semantics or automatic graph traversal is introduced.

## Local validation for M10.9.5.4

Run:

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-m1095-command-observed-response-audit.cmd
```

Promotion requires build, complete ordinary suite and the focused M10.9.5.4 gate to be green. If a gate fails, fix only the demonstrated observed-response/presentation contract. Do not alter plant physics or reopen Phase-I numerical qualification. If green, M10.9.5.5 closure is next.

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

Phase I is closed. Continue milestone-by-milestone from the latest validated baseline: M10.9.5.4 → M10.9.5.5. Do not reopen numerical Phase-I work unless a post-Phase-I gate produces direct evidence that the validated production baseline is invalid.

M10.9.5 may present existing command/control/protection truth but must not add new plant physics, protection thresholds or control ownership. Missing physics discovered while authoring consequence explanations is a post-M11 backlog item rather than an M10.9.5 scope expansion.

The post-Phase-I execution order is now pre-planned rather than open-ended: M10.9.5 consequence semantics → M10.9.6 deterministic challenge/demand/scoring → M10.9.7 mission/performance presentation → M10.9.8 integrated M10 validation → M11 release hardening. Detailed contracts live in `ROADMAP.md` and `docs/milestones/M10.9.5.md` through `M11.md`. Phase I is green; this plan now governs the active post-Phase-I execution sequence.

## Documentation authority

For current work use only:

- `PROJECT.md` — current checkpoint, handoff and active validation contract;
- `ROADMAP.md` — future work only;
- `KNOWN_MODEL_LIMITATIONS.md` — unresolved model limitations only;
- `ARCHITECTURE.md` — stable architecture and ownership rules;
- `README.md` — documentation navigation.

Historical milestone/hotfix detail belongs under `history/`, ADRs or the changelog and must not be copied back into current-state documents.
