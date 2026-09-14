# Project — current authoritative state
**M10.9.8 is VALIDATED / CLOSED.** **M10 Final Pre-M11 Cumulative Validation Hotfix 1 is VALIDATED.** The exact-v9 Production Activation Decision 1 Hotfix 1 is now also **VALIDATED**: `integrated-operations-desktop-stable@9` is the authoritative desktop production default and `bounded-demand-following-5-10-5@3` is the authoritative production mission binding.

The first M10 Final long campaign remains frozen as **FAILED / ABORTED exact-v4 evidence**. It is not rewritten. Diagnostic 1–11 repaired LR-M1 scalability, the primary/secondary whole-cycle operating point, breaker-closed governor integral ownership and wet-steam turbine-admission ownership. Exact-v9 was qualified at 600 s and then promoted through a separate opt-in staging gate and authoritative activation-decision gate.

The replacement-long baseline freeze was validated and authorized Execution 1. The first exact-v9 replacement campaign then executed all 1,920 authored seconds / 192,000 steps in 35.2527 minutes but remained **RED**. RL-H1, RL-D1, RL-P1, wall budget, MISSION projection scalability and replay/checkpoint equivalence passed; RL-M1 and RL-R1 both failed because the same 5→10 MWe load-raise path entered protection. Replacement-Long Failure Diagnostics 1–6 have now returned execution PASS as diagnostic evidence gates. D1 fixes `generator-loss-of-synchronism` at step 636 as the first completed owner; D2–D5 eliminate rod authority, direct breaker-closed SPEED, simple valve preload, fixed-time ramping and short thermal-readiness lead as supported first repairs. Diagnostic 6 returned execution PASS with no protection during its 180 s 5.5/6 MWe holds and near-50 Hz late frequency, but no strict requested-load operating-point window: exact-v9 6 MWe tails near 50.000284 Hz, 5.733824 MWe output, 6.350836 MW shaft and -0.271619 MW dispatch adequacy. D6 therefore proves frequency/rotor recovery without yet proving requested-load convergence. P0 Hotfix 2 is VALIDATED. P1 returned execution PASS with final classification `INCONCLUSIVE`; P2 Decision Gate 1 / Plan Amendment 1 is now **VALIDATED** from returned artifact evidence. P1A has now returned execution PASS / overall `INCONCLUSIVE`: exact-v9 5.5 MWe is `CONVERGED`, while exact-v9 6 MWe demonstrates load reachability at the 3,600 s boundary but not the frozen whole-operating-point stationarity contract. P2R Decision Re-entry 1 / Plan Amendment 2 Hotfix 1 has now returned local PASS together with prerequisite Deep Review Pass 1. Todreas/Kazimi Deep Review Pass 2 returned local PASS / `PASS-AS-AUTHORED`. P1B has now returned execution PASS with 3/3 P1A checkpoint reproduction, green protection/numerical sentinels and engineering label `COUPLED-MULTI-DOMAIN`. P2R2 Decision Re-entry 2 is now **VALIDATED** and selects the runtime-ownership branch for owner localization only; no production repair is authorized. Plan Amendment 3 — External Physical Reference Model Assessment Hold is now **VALIDATED** from the user-reported local audit PASS. **VR0 REFERENCE / PROVENANCE CONTRACT FREEZE** is now VALIDATED from the user-reported local audit PASS. **VR1 POINT-KINETICS INDEPENDENT BENCHMARK** is the active candidate and executes the first quantitative external model assessment against the frozen Hébert one-group/six-group reference contract. It inserts VR0→VR5 independent reference assessment for point kinetics, water/steam, iodine/xenon and decay heat, then returns to P3-R1 if VR5 authorizes `PROCEED-P3R1-EXACTV9`. The original P0 audit attempt is validator-red because of a PowerShell interpolation parser error. Hotfix 1 fixed that parser error but its audit remained validator-red because the validator searched for the stale marker `P0 EVIDENCE & PLANNING FREEZE CANDIDATE` while this document correctly identified the active hotfix as `P0 EVIDENCE & PLANNING FREEZE HOTFIX 1 CANDIDATE`. Hotfix 2 aligns the validator with the actual documented candidate identity. Neither red attempt is a planning/evidence failure. M10 remains OPEN.

This full package still contains the pre-M11 engineering review/planning streams. They remain planning-only and do not weaken the executable validation contract.

M11 is blocked by the amended replacement-long closure route: P0 planning freeze → P1 asymptotic qualification → P2 planning stop → P1A asymptotic closure extension → P2R1 planning stop → Plan Amendment 2 → mandatory Todreas/Kazimi Deep Review Pass 2 → P1B slow-state/owner qualification → P2R2 branch decision (VALIDATED) → Plan Amendment 3 external physical-reference assessment VR0–VR5 → P3-R1 owner localization if VR5 returns `PROCEED-P3R1-EXACTV9` → later P3-R repair decision or branch reconsideration → P4 short 5→10→5 qualification → P5 baseline-2 freeze + Replacement-Long Execution 2 → P6 explicit M10 closure.

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

**Active candidate: M10 FINAL — VR1 POINT-KINETICS INDEPENDENT BENCHMARK.**

P0 Hotfix 2 is **VALIDATED**. Its returned artifact records `m10-final-replacement-long-closure-plan1-p0-passes=True`, no production source/test change, no second-long authorization and `next-authorized-implementation=P1-Asymptotic-First-Stage-Qualification`. D1–D6 are therefore frozen as the completed diagnostic campaign and the P0→P6 route is authoritative.

P1 returned execution PASS with final classification `INCONCLUSIVE`. Its exact-v9 6 MWe probe consumed the full authorized continuation to 1,800 s without trip; frequency and amplitude errors are already near the requested point, but stationarity slopes remain above the frozen P1 bands. P2 Decision Gate 1 is now **VALIDATED** and records `PLAN-STOP-INCONCLUSIVE`, authorizing neither P3-W nor P3-R while explicitly authorizing only Plan Amendment 1.

P1A has returned execution PASS / overall `INCONCLUSIVE`. Its exact-v9 5.5 MWe probe is `CONVERGED`; its exact-v9 6 MWe probe reaches essentially 6 MWe with no trip and near-zero frequency/dispatch/net-acceleration error, but whole-operating-point stationarity is not demonstrated because late output/shaft/steam-flow/turbine-inlet-pressure slopes remain above the frozen P1 ceilings. P2R1 therefore records another planning stop and authorizes neither P3 branch. Plan Amendment 2 defines a bounded P1B observational replay at the existing 3,600 s horizon, preceded by a 600 s 5 MWe background reference. **Todreas/Kazimi Deep Review Pass 2 returned local PASS / `PASS-AS-AUTHORED`, and P1B has now returned execution PASS.** P1B demonstrates a stable 5 MWe background, exact 900/1,800/3,600 s P1A checkpoint reproduction, zero trip/numerical sentinel failures, 6 MWe electrical reachability and persistent load-specific multi-domain slow-state motion. P2R2 is now **VALIDATED** and records `P3-R-OWNER-LOCALIZATION`: P3-W remains unauthorized and production repair remains unauthorized. Plan Amendment 3 is VALIDATED and deliberately holds execution of P3-R1 while a bounded external physical-reference model-assessment sequence (VR0–VR5) quantifies the current point-kinetics, water/steam, iodine/xenon and decay-heat owners against independent references. VR0 is now VALIDATED and its frozen contract is binding. VR1 is the active execution candidate and compares the generic production point-kinetics solver against the independent Hébert reference without changing production physics. P3-R1 resumes only after VR5 returns `PROCEED-P3R1-EXACTV9` or after a separately authorized repair/requalification path. A second replacement-long baseline remains forbidden until P4 short 5→10→5 qualification passes.

**Parallel documentation overlay:** the reviewed pre-M11 planning set remains planning-only and does not supersede executable validation evidence. Reviews 1–2 now record and deeply re-check five additional sources on coordinated plant control/flexibility, nuclear load following, steam-generation circulation/separation and modern reactor physics in `PRE_M11_PLANT_DYNAMICS_THERMAL_HYDRAULICS_REACTOR_PHYSICS_REVIEW.md` and `research/PRE_M11_DEEP_ENGINEERING_SECTION_REVIEW_2.md`. Review 2 revisits only the previously selected high-value sections and adds explicit transferability boundaries. This documentation-only overlay leaves the P1A executable contract byte-for-byte unchanged in `src/` and in all pre-existing tests.

## Validation required for active candidate

Run:

```bat
scripts\run-m10-final-physical-reference-vr1-point-kinetics-benchmark.cmd
```

The runner performs the static VR1/VR0 prerequisite audit, the complete ordinary Release gate, then the explicit independent point-kinetics benchmark. A passing VR1 must return the full `artifacts/m10-final-physical-reference-vr1` folder before VR2; a model/reference discrepancy also requires that artifact folder before any repair or tolerance change.

The original DocsPlanning4 Plan Amendment 2 audit attempt is **validator-red only**: the Deep Review Pass 1 validator required the superseded exact `PROJECT.md` heading from the pre-amendment package even though `PROJECT.md` had correctly advanced to the Plan Amendment 2 current state. The console also rendered the UTF-8 em dash as mojibake under Windows PowerShell. **Hotfix 1 changes validator robustness only**: it uses explicit UTF-8 document reads and ASCII-stable/durable marker checks. No engineering conclusion, Plan Amendment 2 content or authorization boundary changes.

The returned local artifacts validate Todreas/Kazimi Deep Review Pass 1, P2R1 / Plan Amendment 2 Hotfix 1, Deep Review Pass 2, P1B and P2R2. P2R2 is therefore authoritative for branch selection and chooses P3-R owner localization. Plan Amendment 3 does not reverse that choice; it adds a physical-reference assessment hold before P3-R1 so that no runtime owner is repaired against an unquantified reference-model error. Production repair remains unauthorized.

## Evidence and package policy

Candidate source ZIPs intentionally exclude `tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/Evidence/`, generated `artifacts/`, `bin/` and `obj/`.

Compact immutable prerequisites required by ordinary/current tests live under `eng/frozen-evidence/ordinary/`; manifests live under `eng/evidence-manifests/`. Generated audit CSV/TXT payloads remain local validation records and are not copied into each subsequent candidate ZIP.

## Current unresolved items

The authoritative limitation register is `KNOWN_MODEL_LIMITATIONS.md`. In particular:

- Phase I is closed; exact-v4 remains validated historical production evidence, exact-v9 is now authoritative production, and the final long gate remains open under Closure Plan 1 pending the validated Plan Amendment 3 VR0-VR5 physical-reference hold, evidence-selected P3-R owner localization, P4 short qualification, P5 baseline-2 freeze/execution and P6 closure; the first exact-v4 LR-H1 failure remains provenance;
- the historical exact-v3 I.3 drift observations remain regression provenance and are not evidence that exact @4 has identical long-horizon means/slopes;
- historical H.28 remains `bounded-but-costly`; repaired Stage 4 separately demonstrated bounded-at-or-below repaired explicit relative wall cost on the validation machine;
- branch overrides disappeared in repaired long-horizon evidence, but previous-phase hysteresis remained materially active and must not be removed without separately scoped post-Phase-I retirement evidence;
- H.5/H.21 historical numerical source seams remain retained for provenance;
- severe-incident, structural-damage and several plant-system models remain reduced-order or incomplete;
- the reduced quadratic hydraulic map is continuous but not differentiable through some near-zero/reversal transitions, and the generic runtime/numerical-conditioning findings from the post-7.3 Simulation review are assigned to M11.3/M12 rather than patched into M10 presentation work; see `SIMULATION_NUMERICAL_REGULARITY_AND_RUNTIME_REVIEW.md`;
- fingerprint-v1 anchoring plus lifecycle-spine/recent-evidence separation are validated through M10.9.7.4/M10.9.8; recorder notification cost, fingerprint cost, collection-copy traps, long-session memory growth and recorder failure policy remain M11.2/M11.3 ownership; see `APPLICATION_RECORDING_REPLAY_REVIEW.md` and ADR-0184;
- UI-thread runtime/projection responsiveness, notification fan-out and archive-export cost remain M11.3 measurement work; stable command-target identity and `MainWindowViewModel` decomposition remain M13 work; see `DESKTOP_HOST_FAILURE_AND_SESSION_SAVE_INTEGRITY_REVIEW.md` and ADR-0185.

## Continuation rule

Phase I, M10.9.5, M10.9.6 and M10.9.7 are closed. Continue milestone-by-milestone from the latest validated chain: **M10.9.8.5 VALIDATED / M10.9.8 CLOSED → M10 Final Pre-M11 Cumulative Hotfix 1 VALIDATED → failed/aborted first long campaign → Diagnostic 1 PASS → Diagnostic 2 / LR-M1 Hotfix 1 PASS → Diagnostic 3 original build RED (test-only CS0103) → Diagnostic 3 Hotfix 1 execution PASS / exact-v5 NOT QUALIFIED → Diagnostic 4 PASS / mass-energy owners identified → Diagnostic 5 PASS / whole-cycle state captured → Diagnostic 6 execution PASS / exact-v6 NOT QUALIFIED → Diagnostic 7 PASS / breaker-closed governor integral-reference defect proven → Diagnostic 8 execution PASS / exact-v7 NOT QUALIFIED → Diagnostic 9 PASS / turbine-admission non-vapor owner proven → Diagnostic 10 original ordinary-suite RED (test-only inlet balance assertion) → Diagnostic 10 Hotfix 1 PASS / exact-v8 NOT QUALIFIED → Diagnostic 11 original ordinary-suite RED (test-only stale governor-integral range) → Diagnostic 11 Hotfix 1 ordinary-suite RED (test-only ideal pre-step P-term assertion) → Diagnostic 11 Hotfix 2 PASS / exact-v9 QUALIFIED → exact-v9 opt-in production activation candidate PASS → authoritative exact-v9 Activation Decision 1 BUILD RED (new-test CS1503 + CS0103 only) → Activation Decision 1 Hotfix 1 PASS / exact-v9 AUTHORITATIVE → Replacement-Long Baseline Freeze 1 PASS → Replacement-Long Execution 1 RED (RL-M1/RL-R1 shared load-raise protection path; other legs/budget/scalability/replay equivalence green) → Replacement-Long Failure Diagnostic 1 PASS / loss-of-synchronism owner → Replacement-Long Failure Diagnostic 2 PASS / rod coordination no margin → Replacement-Long Failure Diagnostic 3 PASS / SPEED-valve-v4 discrimination → Replacement-Long Failure Diagnostic 4 PASS / fixed-time ramp and short support insufficient; nominal 66 MWth not actually attained → Replacement-Long Failure Diagnostic 5 PASS / measured thermal readiness reached but 20 s first-stage settling incomplete without trip → Replacement-Long Failure Diagnostic 6 PASS → P0 Closure Plan 1 Hotfix 2 VALIDATED → P1 asymptotic qualification PASS/INCONCLUSIVE → P2 PLAN-STOP VALIDATED → P1A asymptotic closure extension PASS/INCONCLUSIVE → P2R1 PLAN-STOP → Plan Amendment 2 → Todreas/Kazimi Deep Review Pass 2 → P1B slow-state/owner qualification PASS / COUPLED-MULTI-DOMAIN → P2R2 VALIDATED / P3-R OWNER LOCALIZATION → Plan Amendment 3 physical-reference assessment VR0–VR5 → P3-R1 owner localization if VR5 authorizes `PROCEED-P3R1-EXACTV9` → P3-R2 repair/no-repair decision → P4 short 5→10→5 qualification → P5 replacement-long baseline 2 + Execution 2 → P6 explicit M10 closure → M11**.

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


### Todreas/Kazimi Deep Review Pass 2 checkpoint

Plan Amendment 2 Hotfix 1 and Deep Review Pass 2 are locally VALIDATED / `PASS-AS-AUTHORED`. P1B then executed successfully with the planned slow-state evidence and returned `COUPLED-MULTI-DOMAIN`. P2R2 is VALIDATED and selects P3-R owner localization only. Plan Amendment 3 is VALIDATED and holds P3-R1 behind VR0–VR5 external physical-reference assessment; VR0 Reference/Provenance Contract Freeze is VALIDATED; VR1 Point-Kinetics Independent Benchmark is the active candidate. The P1B binding clarifications remain in force: 1 s data are slow-state downsampling only; per-step/canonical-event protection and numerical sentinels remain required; no scalar cross-domain owner score is used; existing conservation/energy ledgers are reused; controller memory/command/physical state stay distinct; and `NO-MATERIAL-LATE-DRIFT` is not stationarity qualification.

### P2R1 / Plan Amendment 2 checkpoint

P1A is returned execution PASS / overall `INCONCLUSIVE`; 6 MWe load reachability is demonstrated but full represented stationarity is not. P2R1 selected no P3 branch. Plan Amendment 2 froze the P1B owner-localization scope, Deep Review Pass 2 returned `PASS-AS-AUTHORED`, and P1B has now returned execution PASS / `COUPLED-MULTI-DOMAIN`. P2R2 is validated. Plan Amendment 3 / Physical Reference Model Assessment is VALIDATED; VR0 Reference/Provenance Contract Freeze is VALIDATED; VR1 Point-Kinetics Independent Benchmark is the current execution gate.

### P1B implementation checkpoint

Deep Review Pass 2 is locally VALIDATED / `PASS-AS-AUTHORED`. P1B has now completed its 600 s exact-v9 5 MWe background reference and unchanged exact-v9 5→6 MWe replay to 3,600 s, reproducing P1A checkpoints at 900/1,800/3,600 s and emitting canonical inventory, hydraulic, controller/actuator, turbine and generator evidence. P1B selected no P3 branch; its returned evidence was frozen for P2R2, which has now validated the P3-R owner-localization route. P3-R1 remains temporarily held by Plan Amendment 3.
