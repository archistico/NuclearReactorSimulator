# Project — current authoritative state
**M10.9.8 is VALIDATED / CLOSED.** M10.9.8.5 manual integrated HMI acceptance completed on 2026-08-22. **M10 Final Pre-M11 Cumulative Validation Hotfix 1 is VALIDATED** with the complete Release ordinary suite and curated current-authority focused gates green.

The first **M10 Final Pre-M11 Long Validation Hotfix 1** campaign is now **FAILED / ABORTED AFTER DIAGNOSTIC EVIDENCE COLLECTION**. LR-H1 raised `WaterSteamStateOutOfRangeException` at fluid node `outlet` (`v=0.0026153411609661885 m^3/kg`, `u=1615124.4119888516 J/kg`) after the preserved 300 s checkpoint and before 600 s. LR-M1 was manually stopped at logical step 360000 / 440000 after equal 300 s simulated chunks grew from roughly 10 to roughly 36 minutes wall-clock. M10 cannot close on this evidence.

This full package additionally consolidates the three pre-M11 engineering review/planning streams (nuclear-code V&V, Digital I&C/human-system safety, and operating-point equilibrium/stability). Those documents are **planning only** and do not alter the frozen long workload, runtime physics or acceptance criteria.

M10 remains OPEN. M11 is blocked until the long failure is diagnosed, any required owner correction is separately validated, the full long gate passes, and M10 closure is explicitly recorded.

This is the **single current-state and handoff document** for Nuclear Reactor Simulator. Do not duplicate the current checkpoint in README, roadmap, milestone files or candidate-specific notes.

## Current checkpoint

**M10.9.4.1 / Phase I, M10.9.5, M10.9.6 and M10.9.7 are VALIDATED / CLOSED.** M10.9.8.1 REV1 Docs1, M10.9.8.2 Hotfix 1 REV5, M10.9.8.3 and M10.9.8.4 Hotfix 1 are VALIDATED. M10.9.8.2 REV5 remains the last **validated** production/runtime baseline because later M10.9.8.3/8.4 are test/evidence-only; M10.9.8.4 Hotfix 1 is the sole stacking baseline for M10.9.8.5. The active Diagnostic 2 candidate intentionally overlays one unvalidated Application read-side scalability correction for LR-M1; it is not promoted until its ordinary/focused gates pass.

The validated M10.9.7 baseline includes the live read-only MISSION workspace, deterministic logical-step timeline, presentation-only drill-down, exact mission/archive binding, replay/checkpoint reconstruction, closure coverage for active/completed/failed mission states, assistance changes and requested/effective authority divergence. F1–F8 remain preserved, F9 remains absent and MISSION has no plant-command authority.

Authoritative desktop production remains:

`integrated-operations-desktop-stable@4 | CorrelationConsistentInverseDomain | FourNodeBranchContinuityCorrectedCommitOptIn | 10 ms`

Historical exact-version identities remain immutable; M10.9.8 validation work does not reopen Phase-I numerical ownership without direct contradictory evidence.

## Active validation candidate and parallel planning overlay

**Active candidate: M10 Final Long Failure Diagnostic 2 / LR-M1 Hotfix 1 — CANDIDATE.**

The cumulative Hotfix 1 gate is validated. The long candidate changes no production `src/` file and adds only scheduled-long validation/test/contract/documentation surface. `eng/m10-final-long-validation-contract.json` freezes the approximately-one-hour-class workload before the first acceptance run.

The first long execution was stopped after preserving the available artifacts because LR-H1 was already blocking and LR-M1 exhibited severe superlinear wall-cost growth. Do not widen the frozen envelope, I.3 budgets or conservation ceilings. Diagnostic 1 passed locally and established two concrete findings: LR-M1 is a live MISSION prefix-scan scalability defect, while LR-H1 already contains a real outlet inventory/primary-branch drift inside the qualified 300 s window. Diagnostic 2 applies only the LR-M1 Application read-side correction and adds a second 300 s H1 owner/controller census; see `M10_FINAL_LONG_FAILURE_DIAGNOSTIC2.md`.

**Parallel documentation overlay:** this package also includes the reviewed pre-M11 planning set from the three book studies. It does not supersede the executable long baseline and is not promotion evidence.

The campaign comprises LR-H1 7,200 s healthy exact-v4, LR-M1 4,400 s production mission @2, LR-D1 1,800 s unavailable-measurement/recovery, LR-P1 900 s protection/takeover and LR-R1 100 s replay/checkpoint. Total authored exposure is 14,400 simulated seconds / 1,440,000 deterministic 10 ms steps; replay reconstruction adds further deterministic execution.

The 19 I.3 budgets and exact-v4 conservation ceilings are unchanged. M10 closes only after the long artifact reports `m10-final-long-validation-passes=True` and a closure/promotion step records that evidence.

## Validation required for active final long candidate

Diagnostic 1 is complete. Run `scripts\run-m10-final-long-failure-diagnostic2.cmd` on the current candidate. This validates the LR-M1 incremental projection hotfix and captures the remaining LR-H1 primary-flow/controller evidence. Do not start a replacement long campaign until Diagnostic 2 artifacts are reviewed and any LR-H1 production repair is separately validated. M11 remains blocked until a complete replacement long gate passes and M10 closure is recorded.

## Evidence and package policy

Candidate source ZIPs intentionally exclude `tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/Evidence/`, generated `artifacts/`, `bin/` and `obj/`.

Compact immutable prerequisites required by ordinary/current tests live under `eng/frozen-evidence/ordinary/`; manifests live under `eng/evidence-manifests/`. Generated audit CSV/TXT payloads remain local validation records and are not copied into each subsequent candidate ZIP.

## Current unresolved items

The authoritative limitation register is `KNOWN_MODEL_LIMITATIONS.md`. In particular:

- Phase I is closed; repaired exact-v4 production and the final cumulative/reference chain are validated, while the final long gate is still open and its first LR-H1 healthy soak has failed;
- the historical exact-v3 I.3 drift observations remain regression provenance and are not evidence that exact @4 has identical long-horizon means/slopes;
- historical H.28 remains `bounded-but-costly`; repaired Stage 4 separately demonstrated bounded-at-or-below repaired explicit relative wall cost on the validation machine;
- branch overrides disappeared in repaired long-horizon evidence, but previous-phase hysteresis remained materially active and must not be removed without separately scoped post-Phase-I retirement evidence;
- H.5/H.21 historical numerical source seams remain retained for provenance;
- severe-incident, structural-damage and several plant-system models remain reduced-order or incomplete;
- the reduced quadratic hydraulic map is continuous but not differentiable through some near-zero/reversal transitions, and the generic runtime/numerical-conditioning findings from the post-7.3 Simulation review are assigned to M11.3/M12 rather than patched into M10 presentation work; see `SIMULATION_NUMERICAL_REGULARITY_AND_RUNTIME_REVIEW.md`;
- fingerprint-v1 anchoring plus lifecycle-spine/recent-evidence separation are validated through M10.9.7.4/M10.9.8; recorder notification cost, fingerprint cost, collection-copy traps, long-session memory growth and recorder failure policy remain M11.2/M11.3 ownership; see `APPLICATION_RECORDING_REPLAY_REVIEW.md` and ADR-0184;
- UI-thread runtime/projection responsiveness, notification fan-out and archive-export cost remain M11.3 measurement work; stable command-target identity and `MainWindowViewModel` decomposition remain M13 work; see `DESKTOP_HOST_FAILURE_AND_SESSION_SAVE_INTEGRITY_REVIEW.md` and ADR-0185.

## Continuation rule

Phase I, M10.9.5, M10.9.6 and M10.9.7 are closed. Continue milestone-by-milestone from the latest validated baseline: **M10.9.7.5 Hotfix 1 VALIDATED → M10.9.8.1 REV1 Docs1 VALIDATED → M10.9.8.2 Hotfix 1 REV5 VALIDATED → M10.9.8.3 VALIDATED → M10.9.8.4 Hotfix 1 VALIDATED → M10.9.8.5 VALIDATED / M10.9.8 CLOSED → M10 Final Pre-M11 Cumulative Hotfix 1 VALIDATED → failed/aborted M10 Final Pre-M11 Long Validation Hotfix 1 campaign → M10 Final Long Failure Diagnostic 1 PASS → M10 Final Long Failure Diagnostic 2 / LR-M1 Hotfix 1 → LR-H1 repair if required → replacement long <=60 min wall budget → full long PASS → explicit M10 closure → M11**.

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
