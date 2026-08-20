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

**M10.9.5.3 Hotfix 2 — COMMANDS Context Inspector XAML Contract & Exact Schematic Focus Semantics — CANDIDATE.**

M10.9.5.1 and M10.9.5.2 are explicitly validated post-Phase-I baselines. The initial M10.9.5.3 build exposed a ViewModel API-name mismatch (`Monitors`/`Explanation` vs `MonitorTargets`/`Reason`), fixed by Hotfix 1. Hotfix 1 then compiled, while the ordinary App suite found one stale XAML-contract assertion that expected `SelectedCommandSchematicElementId` without the intentional `Mode=OneWay`. Hotfix 2 corrects that test contract and, during the requested full re-review, also removes a presentation fallback that could highlight an unrelated mimic element when the selected dependency step itself was not graphical. M10.9.5.3 remains the first HMI integration step: F4 COMMANDS consumes the validated authored consequence map and dependency-chain projection without duplicating their semantics in Avalonia.

For the selected command, progressive disclosure shows the existing availability/blocker state, direct effect, qualitative expected influence, authored monitor set and dependency chain. Selecting a dependency step changes only presentation focus on the canonical whole-plant mimic snapshot; it never dispatches a command or mutates plant state. Plant-mimic element steps highlight that element, plant-mimic connection steps use an explicitly labelled source-element proxy, and non-graphical command-target/published-state steps clear the highlight rather than fabricating one. The existing ENTER/EXECUTE typed-command boundary remains unchanged and blocked commands remain inspectable.

No new graph topology, numerical future prediction, plant physics, protection threshold, control authority or exact-version behavior is introduced.

## Local validation for M10.9.5.3

Run:

```bat
APPLY_UPDATE.cmd
dotnet build
dotnet test
scripts\run-m1095-command-context-inspector-schematic-audit.cmd
```

Then perform a manual HMI check on representative reactor/primary/turbine/electrical/alarm commands: keyboard selection must not dispatch, direct effect must remain distinct from expected influence, the monitor list must be readable, dependency selection must only move schematic focus, blocked commands must remain inspectable, and the minimum supported window must remain usable through scrolling.

Promotion requires build, complete ordinary suite, focused gate and manual HMI confirmation to be green. If a gate fails, fix only the demonstrated presentation/integration contract. Do not alter physics or reopen Phase-I numerical qualification.

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

Phase I is closed. Continue milestone-by-milestone from the latest validated baseline: M10.9.5.3 → M10.9.5.4 → M10.9.5.5. Do not reopen numerical Phase-I work unless a post-Phase-I gate produces direct evidence that the validated production baseline is invalid.

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
