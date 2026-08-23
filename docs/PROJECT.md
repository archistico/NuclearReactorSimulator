# Project — current authoritative state
**M10.9.8 is VALIDATED / CLOSED.** **M10 Final Pre-M11 Cumulative Validation Hotfix 1 is VALIDATED.** The exact-v9 Production Activation Decision 1 Hotfix 1 is now also **VALIDATED**: `integrated-operations-desktop-stable@9` is the authoritative desktop production default and `bounded-demand-following-5-10-5@3` is the authoritative production mission binding.

The first M10 Final long campaign remains frozen as **FAILED / ABORTED exact-v4 evidence**. It is not rewritten. Diagnostic 1–11 repaired LR-M1 scalability, the primary/secondary whole-cycle operating point, breaker-closed governor integral ownership and wet-steam turbine-admission ownership. Exact-v9 was qualified at 600 s and then promoted through a separate opt-in staging gate and authoritative activation-decision gate.

The replacement-long baseline freeze was validated and authorized Execution 1. The first exact-v9 replacement campaign then executed all 1,920 authored seconds / 192,000 steps in 35.2527 minutes but remained **RED**. RL-H1, RL-D1, RL-P1, wall budget, MISSION projection scalability and replay/checkpoint equivalence passed; RL-M1 and RL-R1 both failed because the same 5→10 MWe load-raise path entered protection. Replacement-Long Failure Diagnostics 1–6 have now returned execution PASS as diagnostic evidence gates. D1 fixes `generator-loss-of-synchronism` at step 636 as the first completed owner; D2–D5 eliminate rod authority, direct breaker-closed SPEED, simple valve preload, fixed-time ramping and short thermal-readiness lead as supported first repairs. Diagnostic 6 returned execution PASS with no protection during its 180 s 5.5/6 MWe holds and near-50 Hz late frequency, but no strict requested-load operating-point window: exact-v9 6 MWe tails near 50.000284 Hz, 5.733824 MWe output, 6.350836 MW shaft and -0.271619 MW dispatch adequacy. D6 therefore proves frequency/rotor recovery without yet proving requested-load convergence. P0 Hotfix 2 is VALIDATED. P1 has now returned execution PASS with final classification `INCONCLUSIVE`; the active candidate is **M10 Final Replacement-Long Closure Plan 1 — P2 Decision Gate 1 / Plan Amendment 1**. The original P0 audit attempt is validator-red because of a PowerShell interpolation parser error. Hotfix 1 fixed that parser error but its audit remained validator-red because the validator searched for the stale marker `P0 EVIDENCE & PLANNING FREEZE CANDIDATE` while this document correctly identified the active hotfix as `P0 EVIDENCE & PLANNING FREEZE HOTFIX 1 CANDIDATE`. Hotfix 2 aligns the validator with the actual documented candidate identity. Neither red attempt is a planning/evidence failure. M10 remains OPEN.

This full package still contains the pre-M11 engineering review/planning streams. They remain planning-only and do not weaken the executable validation contract.

M11 is blocked by the amended replacement-long closure route: P0 planning freeze → P1 asymptotic qualification → P2 planning stop → P1A asymptotic closure extension → P2R branch decision → P3-W/P3-R → P4 short 5→10→5 qualification → P5 baseline-2 freeze + Replacement-Long Execution 2 → P6 explicit M10 closure.

This is the **single current-state and handoff document** for Nuclear Reactor Simulator. Do not duplicate the current checkpoint in README, roadmap, milestone files or candidate-specific notes.

## Current checkpoint

The authoritative production state is now:

- policy `M10FinalExactV9QualifiedCandidate`;
- initial condition `integrated-operations-desktop-stable@9`;
- production scenario `integrated-normal-operations-training-m10-final-v9-production`;
- production mission `bounded-demand-following-5-10-5@3`;
- deterministic selector/direct-factory fingerprint `7880AD580179B936C584EB0055BE663E0A1CFA65C5191B0DB8A7F3C514DB5418`;
- exact-v4 retained explicitly as historical I.5 production;
- exact-v3 retained explicitly as historical H.30 production;
- exact-v2 retained as fail-closed rollback/reference.

The returned activation-decision artifact recorded 12,000 healthy authoritative steps, zero trip/rollback/fallback/unsafe/untargeted disagreement, ~5 MWe, ~100 kg/s primary flow, stable drum/governor state, explicit moisture ownership and conservative mass/energy closure. `production-activation=True` and `replacement-long-authorized=False` were intentionally separate decisions.

The failed first-long manifests remain immutable provenance:
`eng/m10-final-long-baseline-src.sha256` and `eng/m10-final-long-baseline-tests.sha256`.

## Active validation candidate and parallel planning overlay

**Active candidate: M10 Final Replacement-Long Closure Plan 1 — P2 DECISION GATE 1 — PLAN-STOP-INCONCLUSIVE CANDIDATE.**

P0 Hotfix 2 is **VALIDATED**. Its returned artifact records `m10-final-replacement-long-closure-plan1-p0-passes=True`, no production source/test change, no second-long authorization and `next-authorized-implementation=P1-Asymptotic-First-Stage-Qualification`. D1–D6 are therefore frozen as the completed diagnostic campaign and the P0→P6 route is authoritative.

P1 has now returned execution PASS with final classification `INCONCLUSIVE`. Its exact-v9 6 MWe probe consumed the full authorized continuation to 1,800 s without trip; frequency and amplitude errors are already near the requested point, but stationarity slopes remain above the frozen P1 bands. P2 therefore records `PLAN-STOP-INCONCLUSIVE` and authorizes neither P3-W nor P3-R.

Plan Amendment 1 inserts P1A Asymptotic Closure Extension: exact-v9 5.5/6 MWe only, unchanged P1 criteria, clean deterministic reruns, P1 checkpoint reproduction and a hard 3,600 s total hold ceiling. P1A returns to P2R before any P3 selection. A second replacement-long baseline remains forbidden until P4 short 5→10→5 qualification passes.

**Parallel documentation overlay:** the reviewed pre-M11 planning set remains planning-only and does not supersede executable validation evidence.

## Validation required for active candidate

Run:

```bat
scripts\run-m10-final-replacement-long-closure-plan1-p2-decision-audit.cmd
```

P2 is documentation/planning-only. Its audit verifies the returned P1 evidence, records the mandatory `PLAN-STOP-INCONCLUSIVE`, keeps both P3 branches unauthorized and freezes Plan Amendment 1. It does not execute dynamics or substitute for the ordinary Release gate.

## Evidence and package policy

Candidate source ZIPs intentionally exclude `tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/Evidence/`, generated `artifacts/`, `bin/` and `obj/`.

Compact immutable prerequisites required by ordinary/current tests live under `eng/frozen-evidence/ordinary/`; manifests live under `eng/evidence-manifests/`. Generated audit CSV/TXT payloads remain local validation records and are not copied into each subsequent candidate ZIP.

## Current unresolved items

The authoritative limitation register is `KNOWN_MODEL_LIMITATIONS.md`. In particular:

- Phase I is closed; exact-v4 remains validated historical production evidence, exact-v9 is now authoritative production, and the final long gate remains open under Closure Plan 1 pending P2 planning-stop validation, P1A/P2R classification, the evidence-selected P3 branch, P4 short qualification, P5 baseline-2 freeze/execution and P6 closure; the first exact-v4 LR-H1 failure remains provenance;
- the historical exact-v3 I.3 drift observations remain regression provenance and are not evidence that exact @4 has identical long-horizon means/slopes;
- historical H.28 remains `bounded-but-costly`; repaired Stage 4 separately demonstrated bounded-at-or-below repaired explicit relative wall cost on the validation machine;
- branch overrides disappeared in repaired long-horizon evidence, but previous-phase hysteresis remained materially active and must not be removed without separately scoped post-Phase-I retirement evidence;
- H.5/H.21 historical numerical source seams remain retained for provenance;
- severe-incident, structural-damage and several plant-system models remain reduced-order or incomplete;
- the reduced quadratic hydraulic map is continuous but not differentiable through some near-zero/reversal transitions, and the generic runtime/numerical-conditioning findings from the post-7.3 Simulation review are assigned to M11.3/M12 rather than patched into M10 presentation work; see `SIMULATION_NUMERICAL_REGULARITY_AND_RUNTIME_REVIEW.md`;
- fingerprint-v1 anchoring plus lifecycle-spine/recent-evidence separation are validated through M10.9.7.4/M10.9.8; recorder notification cost, fingerprint cost, collection-copy traps, long-session memory growth and recorder failure policy remain M11.2/M11.3 ownership; see `APPLICATION_RECORDING_REPLAY_REVIEW.md` and ADR-0184;
- UI-thread runtime/projection responsiveness, notification fan-out and archive-export cost remain M11.3 measurement work; stable command-target identity and `MainWindowViewModel` decomposition remain M13 work; see `DESKTOP_HOST_FAILURE_AND_SESSION_SAVE_INTEGRITY_REVIEW.md` and ADR-0185.

## Continuation rule

Phase I, M10.9.5, M10.9.6 and M10.9.7 are closed. Continue milestone-by-milestone from the latest validated chain: **M10.9.8.5 VALIDATED / M10.9.8 CLOSED → M10 Final Pre-M11 Cumulative Hotfix 1 VALIDATED → failed/aborted first long campaign → Diagnostic 1 PASS → Diagnostic 2 / LR-M1 Hotfix 1 PASS → Diagnostic 3 original build RED (test-only CS0103) → Diagnostic 3 Hotfix 1 execution PASS / exact-v5 NOT QUALIFIED → Diagnostic 4 PASS / mass-energy owners identified → Diagnostic 5 PASS / whole-cycle state captured → Diagnostic 6 execution PASS / exact-v6 NOT QUALIFIED → Diagnostic 7 PASS / breaker-closed governor integral-reference defect proven → Diagnostic 8 execution PASS / exact-v7 NOT QUALIFIED → Diagnostic 9 PASS / turbine-admission non-vapor owner proven → Diagnostic 10 original ordinary-suite RED (test-only inlet balance assertion) → Diagnostic 10 Hotfix 1 PASS / exact-v8 NOT QUALIFIED → Diagnostic 11 original ordinary-suite RED (test-only stale governor-integral range) → Diagnostic 11 Hotfix 1 ordinary-suite RED (test-only ideal pre-step P-term assertion) → Diagnostic 11 Hotfix 2 PASS / exact-v9 QUALIFIED → exact-v9 opt-in production activation candidate PASS → authoritative exact-v9 Activation Decision 1 BUILD RED (new-test CS1503 + CS0103 only) → Activation Decision 1 Hotfix 1 PASS / exact-v9 AUTHORITATIVE → Replacement-Long Baseline Freeze 1 PASS → Replacement-Long Execution 1 RED (RL-M1/RL-R1 shared load-raise protection path; other legs/budget/scalability/replay equivalence green) → Replacement-Long Failure Diagnostic 1 PASS / loss-of-synchronism owner → Replacement-Long Failure Diagnostic 2 PASS / rod coordination no margin → Replacement-Long Failure Diagnostic 3 PASS / SPEED-valve-v4 discrimination → Replacement-Long Failure Diagnostic 4 PASS / fixed-time ramp and short support insufficient; nominal 66 MWth not actually attained → Replacement-Long Failure Diagnostic 5 PASS / measured thermal readiness reached but 20 s first-stage settling incomplete without trip → Replacement-Long Failure Diagnostic 6 PASS → P0 Closure Plan 1 Hotfix 2 VALIDATED → P1 asymptotic qualification PASS/INCONCLUSIVE → P2 PLAN-STOP → P1A asymptotic closure extension → P2R branch decision → P3-W/P3-R → P4 short 5→10→5 qualification → P5 replacement-long baseline 2 + Execution 2 → P6 explicit M10 closure → M11**.

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
