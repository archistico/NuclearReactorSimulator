# M10 Final Replacement-Long Closure Plan 1

## Status and authority

**P2R2 DECISION RE-ENTRY 2 is VALIDATED. P1B returned execution PASS with exact P1A checkpoint reproduction, green protection/numerical sentinels and engineering label `COUPLED-MULTI-DOMAIN`. 6 MWe electrical reachability is demonstrated, but the 6 MWe whole-plant state retains material inventory/hydraulic/steam/controller drift while the 5 MWe background is stable. P2R2 selects P3-R for owner localization only. Plan Amendment 3 is now the active planning candidate and temporarily holds P3-R1 behind VR0–VR5 independent physical-reference model assessment. P3-W, production repair and the second replacement-long baseline remain unauthorized. M10 remains OPEN and Replacement-Long Execution 1 remains authoritative RED evidence.**

This document replaces the previous diagnostic-by-diagnostic continuation pattern with a finite closure route from the returned Replacement-Long Failure Diagnostic 1–6 evidence to M10 closure. It is a planning and decision-governance contract only: it changes no production runtime, workload, authority policy, generator-load semantics, protection semantics, exact-v9 state, mission pack or acceptance threshold.

**P0 Hotfix 1 note (23 August 2026):** the original P0 candidate's documentation audit did not start because PowerShell parsed the interpolated error string `"$Path: $Needle"` as an invalid scoped/drive-style variable reference. Hotfix 1 changes only the audit tooling, replacing that interpolation with the parser-safe format expression `-f $Path, $Needle`, and records the hotfix marker in the planning contract. The P0 evidence, roadmap, gates, branch criteria and engineering conclusions are unchanged. The original P0 audit attempt is retained as validator-red provenance, not as a P0 planning failure.

**P0 Hotfix 2 note (23 August 2026):** Hotfix 1 fixed the PowerShell parser error and the validator then executed far enough to expose a second tooling-only mismatch: `docs/PROJECT.md` correctly named `P0 EVIDENCE & PLANNING FREEZE HOTFIX 1 CANDIDATE`, while the validator still required the stale pre-hotfix marker `P0 EVIDENCE & PLANNING FREEZE CANDIDATE`. Hotfix 2 updates the validator/contract identity to `P0-HOTFIX2-PROJECT-MARKER-ALIGNMENT` and requires the actual Hotfix 2 PROJECT marker. No P0 evidence, route, gate, engineering conclusion, runtime, test or authorization boundary changes. The original P0 and Hotfix 1 audit attempts remain validator-red provenance only.


**P0 validation note (23 August 2026):** the returned Hotfix 2 artifact records `m10-final-replacement-long-closure-plan1-p0-passes=True`, `production-src-changed=False`, `production-tests-changed=False`, `second-replacement-long-authorized=False` and `next-authorized-implementation=P1-Asymptotic-First-Stage-Qualification`. P0 is therefore VALIDATED and is the authoritative evidence/planning freeze for the remaining M10 route.


**P1 returned-evidence note (23 August 2026):** the complete returned P1 artifact records a valid stable-reference calibration and `m10-final-replacement-long-closure-plan1-p1-passes=True`, but the primary exact-v9 6 MWe probe remains `INCONCLUSIVE` after its full 1,800 s bounded continuation. Tail output is 5.9312649958700989 MWe, output error 0.068735004129901078 MWe, dispatch adequacy -0.070137744201449845 MW and output slope 3.0659206076729932E-05 MW/s. The trajectory is already near synchronous and inside amplitude tolerances but remains above the frozen stationarity slope band. P2 therefore records `PLAN-STOP-INCONCLUSIVE`: neither P3-W nor P3-R is evidence-authorized.


**P2 validation note (23 August 2026):** the returned Decision Gate 1 artifact records `m10-final-replacement-long-closure-plan1-p2-passes=True`, `p2-decision=PLAN-STOP-INCONCLUSIVE`, both P3 branches unauthorized, Plan Amendment 1 `P1A-ASYMPTOTIC-CLOSURE-EXTENSION`, hard 3,600 s hold ceiling, no production source/test/runtime/workload change and `next-authorized-implementation=P1A-Asymptotic-Closure-Extension`. P2 is therefore VALIDATED.

**P1A executable contract:** P1A reuses the frozen P1 calibration rather than recalibrating thresholds, runs only clean exact-v9 5→5.5 and 5→6 MWe probes, and must reproduce the returned P1 checkpoints before entering extension time. The required checkpoints are 5.5 MWe at 900 s and 6 MWe at 900/1,800 s after load; checkpoint comparisons use predeclared narrow tolerances only to detect baseline drift, not to classify convergence: power/shaft/dispatch ±0.002 MW, thermal ±0.01 MW, frequency ±0.0001 Hz, steam flow ±0.01 kg/s and turbine-inlet pressure ±0.002 MPa. After checkpoint reproduction, the same P1 `CONVERGED` / `BIASED-STATIONARY` criteria remain in force through a hard 3,600 s total hold. If the two probes disagree in final class, P1A overall is `INCONCLUSIVE`. P1A never authorizes P3 directly and always returns to P2R.

**Plan Amendment 1 (P2 VALIDATED):** insert **P1A — Asymptotic Closure Extension** before branch selection. P1A is limited to clean exact-v9 5→5.5 and 5→6 MWe probes, unchanged P1 calibration/tolerances and at most 3,600 s after the load command. It must reproduce P1 checkpoints before consuming the extension. exact-v4 is not rerun. P1A returns to **P2R — Decision Re-entry**; there is no direct jump to P3 and no automatic continuation beyond 3,600 s.
The current production identities remain:

- policy `M10FinalExactV9QualifiedCandidate`;
- initial condition `integrated-operations-desktop-stable@9`;
- production mission `bounded-demand-following-5-10-5@3`;
- exact-v4 retained as historical production evidence;
- Replacement-Long Baseline Freeze 1 retained as immutable provenance for failed Execution 1.

## 1. Evidence freeze: Replacement-Long Failure Diagnostics 1–6

The six diagnostics are now treated as one completed diagnostic campaign. Their execution status is PASS as evidence gates; PASS does **not** mean the failed replacement long or a 10 MWe operating point is qualified.

| Gate | Returned observation | What it rules out / establishes |
| --- | --- | --- |
| D1 | Frozen exact-v9 5→10 MWe path first completes `generator-loss-of-synchronism` at step 636 / 6.36 s; underfrequency triggers earlier but does not complete pickup first. | Protection owner identified. Protection retuning is not justified merely from the aggregate long failure. |
| D2 | Assisted rod motion physically executes but every bounded rod/load probe still latches the same loss-of-synchronism at step 636. | Rod authority/coordination is not the missing transient margin. |
| D3 | Breaker-closed SPEED changes raw but not droop-owned effective reference; simple valve preload does not create material shaft margin; exact-v4 shows the same failure family, slightly earlier. | Direct SPEED, simple valve preload and an exact-v9-only regression are not supported first repairs. |
| D4 | Smaller/slower fixed-time ramps delay failure but do not establish 10 MWe; the nominal 66 MWth pre-power case had reached only ~37.3 MWth at its load step. | Fixed elapsed-time ramping/support is insufficient; the nominal pre-power case was not a true 66 MWth state. |
| D5 | Measured thermal-readiness probes avoid protection but do not settle to the next 6 MWe stage within 20 s; at the exact-v9 6 MWe command the reactor is ~39.32 MWth while shaft/steam flow remain near the preceding 5 MWe mechanical point. | Thermal readiness is not mechanical readiness; 20 s is not a hard capacity boundary. |
| D6 | 180 s holds at exact-v9 5.5 and 6 MWe and exact-v4 6 MWe complete without trip and recover frequency close to 50 Hz, but no strict synchronous operating-point window is reached. | Phase/frequency recovery exists, but qualified operating-point convergence is still unproven. |

### D6 returned tail evidence

D6 is the immediate evidence base for this plan:

| Probe | Tail mean frequency | Tail mean output | Tail mean shaft | Tail dispatch adequacy | Strict sync window |
| --- | ---: | ---: | ---: | ---: | --- |
| exact-v9 → 5.5 MWe | 50.000197 Hz | 5.246229 MWe | 5.853296 MW | −0.258953 MW | none |
| exact-v9 → 6 MWe | 50.000284 Hz | 5.733824 MWe | 6.350836 MW | −0.271619 MW | none |
| exact-v4 → 6 MWe | 50.000249 Hz | 5.653598 MWe | 6.268969 MW | −0.353485 MW | none |

Net rotor acceleration in the late window is approximately zero, so the rotor is no longer undergoing the original fast loss-of-synchronism transient. However, electrical output remains below request and the steam/shaft trajectory remains slowly evolving. D6 therefore does **not** authorize either a workload repair or a generator-grid/runtime repair.

## 2. Engineering conclusions frozen at P0

The following conclusions may be used as prerequisites without rerunning D1–D6 unless a later production change invalidates them:

1. the first replacement-long execution remains RED and immutable;
2. the shared RL-M1/RL-R1 owner is the 5→10 MWe load-raise transient, not replay/checkpoint determinism;
3. protection thresholds are not the first repair target;
4. rod authority, direct breaker-closed SPEED authority and simple control-valve preload are not supported first repairs;
5. exact-v4 and exact-v9 share the broad transient/settling family, so no exact-v9-only defect is established;
6. fixed-time ramps and short support leads are not sufficient evidence for a valid manoeuvre;
7. measured reactor thermal readiness does not imply immediate turbine mechanical readiness;
8. 180 s is enough to demonstrate frequency/rotor recovery on small stages, but not enough to prove a qualified 5.5/6 MWe operating point;
9. no evidence yet justifies changing production workload, authority, generator-load semantics, protection, exact-v9 or mission @3.

## 3. Finite closure route P0–P6

### P0 — Evidence & Planning Freeze

**Purpose:** consolidate D1–D6 and freeze the remaining decision tree before further implementation.

Allowed changes: documentation, planning contract, documentation audit tooling.

Forbidden changes: `src/`, production tests/semantics, replacement workload, authority policy, generator-load semantics, protection, exact-v9, mission @3, second-long freeze.

**Exit:** documentation audit PASS and this plan adopted as the sole replacement-long closure route.

### P1 — Asymptotic First-Stage Qualification

**Question:** do the small 5→5.5/6 MWe stages eventually converge to the requested operating point, or do they approach a biased phase-locked equilibrium?

P1 is the last planned purely exploratory dynamics gate. It must use the D6 seams rather than invent a new owner hypothesis. Planned matrix:

- stable 5 MWe reference/noise control;
- exact-v9 5→5.5 MWe long hold;
- exact-v9 5→6 MWe long hold;
- exact-v4 5→6 MWe historical control.

The primary horizon is up to **900 s after the load command**, with early exit after a qualified convergence window. A bounded continuation inside the same P1 gate may extend the exact-v9 6 MWe probe to at most **1,800 s total** only if the 900 s tail is still monotonically converging above the measured stable-reference noise floor. This continuation is not a new diagnostic milestone.

P1 must derive slope/noise floors from the stable 5 MWe reference rather than hard-coding a post-hoc tolerance. The executable candidate is frozen by `eng/m10-final-replacement-long-closure-plan1-p1-contract.json`: 120 s stable exact-v9 reference, last 60 s calibration tail split into 10 s subwindows, reference-noise multiplier 10× with predeclared fail-closed ceilings, 900 s primary hold, 1,800 s maximum total hold only for exact-v9 6 MWe, 30 s convergence window and 60 s stationary window. The runner executes the ordinary Release CI gate before the explicit P1 test.

The derived stationarity limits cover electrical output, turbine shaft power, steam flow and turbine-inlet pressure. If the stable reference produces a derived limit above its predeclared ceiling, P1 is `calibration-invalid` and fails as an evidence gate rather than widening tolerances.

A convergence result requires a continuous qualified window with at least:

- breaker closed, no trip/protection action;
- `|frequency - 50 Hz| <= 0.01 Hz`;
- electrical output within ±0.10 MWe of requested stage;
- `|net rotor acceleration power| <= 0.05 MW`;
- `|dispatch mechanical adequacy| <= 0.10 MW`;
- tail output/shaft/steam-flow/turbine-inlet-pressure slopes inside the reference-derived stationary band.

P1 may return only one of these classifications:

- `CONVERGED` — requested stage becomes a qualified stationary operating point;
- `BIASED-STATIONARY` — frequency/rotor are locked and tail slopes are stationary, but material electrical-load error remains;
- `STILL-CONVERGING` — 900 s tail remains directional above noise and the bounded continuation rule is invoked;
- `INCONCLUSIVE` — neither convergence nor stationary bias can be established within the fixed P1 contract.

A protection trip, preparation failure or other non-classifiable physical outcome is preserved explicitly in the P1 artifacts and maps to `INCONCLUSIVE` for P2 planning-stop purposes; it does not create a fifth branch class outside the validated P0 plan.

No runtime or workload change is allowed inside P1. The exact-v9 6 MWe result is the primary P1 branch signal; exact-v9 5.5 MWe and exact-v4 6 MWe are supporting controls. P1 itself never authorizes P3: all returned classifications go first to P2.

### P2 — Decision Gate

P2 is a documentation/engineering decision, not another exploratory simulation.

- If P1 returns `CONVERGED`, enter **P3-W — Workload/Procedure path**.
- If P1 returns `BIASED-STATIONARY`, enter **P3-R — Runtime Ownership path**.
- If P1 returns `STILL-CONVERGING`, execute only the bounded P1 continuation already authorized above and then classify.
- If P1 remains `INCONCLUSIVE`, stop. Do not invent Diagnostic 8/9-style probes ad hoc; revise this closure plan explicitly before further implementation.

P2 must record the selected branch and the evidence that makes the other branch unauthorized.

**Returned P1 outcome:** `INCONCLUSIVE`; P2 Decision Gate 1 therefore records `PLAN-STOP-INCONCLUSIVE` and selects neither P3-W nor P3-R. **P2 Decision Gate 1 is VALIDATED.**

### P1A — Asymptotic Closure Extension (Plan Amendment 1)

**Entry condition:** P1 exhausted its authorized continuation without demonstrating either convergence or stationary bias, and P2 recorded the required planning stop.

Run only exact-v9 5→5.5 MWe and exact-v9 5→6 MWe from clean deterministic state, preserving the frozen P1 calibration and all tolerances. Maximum total hold is 3,600 s after each load command. The run must reproduce the 5.5 MWe / 900 s and 6 MWe / 900 s + 1,800 s P1 checkpoints before entering new simulated time. exact-v4 is frozen from P1 and is not rerun. Final classes remain `CONVERGED`, `BIASED-STATIONARY` or `INCONCLUSIVE`; there is no further automatic extension. If the two exact-v9 probes do not agree on the final class, the P1A overall class is `INCONCLUSIVE`.

P1A returns only to **P2R — Decision Re-entry**. P2R maps `CONVERGED` to P3-W, `BIASED-STATIONARY` to P3-R and `INCONCLUSIVE` to another explicit planning stop.

Executable contract: `eng/m10-final-replacement-long-closure-plan1-p1a-contract.json`. Runner: `scripts/run-m10-final-replacement-long-closure-plan1-p1a-asymptotic-closure-extension.cmd`. Preserve and return the complete `artifacts/m10-final-replacement-long-closure-plan1-p1a` directory before P2R.



### Parallel literature-review constraint for P1A / P2R

The five-source plant-dynamics/thermal-hydraulics/reactor-physics review, including detailed-section Review 2, is **interpretation-only** for this closure route. It does not change P1A thresholds, checkpoints, hold ceilings or classes. After P1A returns, P2R should inspect which canonical owner remains dynamic in the late tail: requested/effective control reference and controller memory, actuator state, steam pressure grade and inventories, primary circulation/head-loss state, neutronics/thermal source, shaft balance and grid coupling. The review supports distinguishing fast frequency/governor response and finite stored-energy support from slower sustainable energy-balance/operating-point convergence; it does not preselect P3-W or P3-R.

References: [`PRE_M11_PLANT_DYNAMICS_THERMAL_HYDRAULICS_REACTOR_PHYSICS_REVIEW.md`](PRE_M11_PLANT_DYNAMICS_THERMAL_HYDRAULICS_REACTOR_PHYSICS_REVIEW.md) and [`research/PRE_M11_DEEP_ENGINEERING_SECTION_REVIEW_2.md`](research/PRE_M11_DEEP_ENGINEERING_SECTION_REVIEW_2.md).

### P3-W — Workload / Procedure path

**P2R2 candidate disposition:** P3-W is **not authorized** from returned P1B. Although unchanged exact-v9 reaches 6 MWe electrically, represented inventory/hydraulic/steam/controller states remain materially dynamic. A workload-only route may be reconsidered later only if P3-R1 proves the runtime contracts coherent and establishes bounded readiness/stationarity evidence.

Build a **test-only** staged/readiness-driven 5→10→5 manoeuvre using dwell/readiness criteria derived from P1 rather than arbitrary elapsed times. The procedure must coordinate thermal and electrical evolution without redefining protection or generator-grid physics.

Only after this test-only manoeuvre demonstrates the full route may a revised replacement workload be authored and versioned. exact-v9 and mission @3 remain unchanged unless a separate identity contract explicitly requires a new mission/workload version.

### P3-R — Runtime Ownership path

**P2R2 candidate disposition:** authorize P3-R **for owner localization only**. Returned P1B shows persistent load-specific represented slow-state motion despite 6 MWe electrical reachability and green numerical/conservation sentinels. This is sufficient to investigate runtime ownership before changing workload, but it is not yet sufficient to authorize a production repair.

Before modifying production code, execute **P3-R1 — Primary Inventory / Hydraulic Slow-State Owner Localization & Contract Audit**. P3-R1 starts from the causal evidence ordering established by P1B: canonical node mass/energy derivatives and transfer terms → main-circulation pump/pipe/head-loss diagnostics → channel/return/outlet storage identity → drum return/recirculation/steam/feedwater balance → condensate/feedwater pump flows and inventory transfer → controller memory/actuator state → steam/turbine/grid downstream consequences. The previous generator/governor chain remains a downstream cross-check rather than the first assumed owner.

A production repair is authorized only when one owner violates an existing physical/control contract or the desired command semantics can be stated unambiguously and tested before implementation.

If the repair changes exact-version production semantics, **exact-v9 remains immutable** and the repaired production state receives **exact-v10**. Historical exact-v9 replay/save/scenario identity may never be silently reinterpreted.

Required repair gates: focused owner regression → ordinary Release gate → exact-version/replay compatibility checks → P4.

### P4 — Short 5→10→5 Qualification

P4 is mandatory for both P3 branches and is the first gate allowed to claim a qualified high-load manoeuvre.

It must demonstrate, on the selected production candidate:

- deterministic 5 MWe initial point;
- protected transition to 10 MWe using the branch-approved procedure/semantics;
- a declared stable 10 MWe window, not merely observation of a 10 MWe request;
- breaker closed with no generator/turbine/reactor trip;
- bounded frequency/phase behavior;
- coherent reactor thermal power, steam flow, shaft power and electrical output;
- conservation/numerical sentinels green;
- deterministic return 10→5 MWe and stable recovery;
- replay/checkpoint equivalence for the manoeuvre where applicable.

**No second replacement-long baseline may be frozen until P4 PASS.**

### P5 — Replacement-Long Baseline 2 Freeze and Execution 2

P5 contains two ordered subgates.

**P5A Freeze:** freeze exactly one coherent baseline containing production exact version, replacement workload exact identity, authority policy, protection contract, mission binding, duration/leg definitions, acceptance metrics and evidence schema. Freeze may occur only after P4 PASS.

**P5B Execution 2:** run the full replacement-long campaign without modifying the frozen baseline during execution. Preserve the complete artifact folder regardless of PASS/FAIL.

If Execution 2 fails, do not mutate the baseline and immediately rerun. Classify the failure under the change-impact policy and return to the earliest invalidated gate in P1–P4.

### P6 — M10 Final Closure

M10 may be declared CLOSED only when all of the following are simultaneously true:

- ordinary Release gate PASS;
- GitHub CI green for the selected baseline;
- P4 short 5→10→5 qualification PASS;
- P5 Replacement-Long Execution 2 PASS;
- replay/checkpoint/exact-version invariants preserved;
- `LONG-SOAK-01` promoted from pending to validated with exact artifact provenance;
- final V&V matrix has no blocking pending/failed row;
- documentation/current production identity is consistent;
- final closure audit records `m10-closed=True` and `next=M11.1`.

Only P6 authorizes M11.1.

## 4. Decision tree

```text
D1-D6 RETURNED / EVIDENCE FROZEN
              |
              v
      P0 PLANNING FREEZE (VALIDATED)
              |
              v
   P1 ASYMPTOTIC QUALIFICATION
              |
        INCONCLUSIVE
              |
              v
   P2 PLAN-STOP / AMENDMENT 1
              |
              v
 P1A BOUNDED ASYMPTOTIC EXTENSION
              |
        PASS / INCONCLUSIVE
              |
              v
      P2R1 PLAN-STOP
              |
              v
        PLAN AMENDMENT 2
              |
              v
 TODREAS/KAZIMI DEEP REVIEW PASS 2
              |
              v
 P1B SLOW-STATE / OWNER QUALIFICATION
              |
              v
      P2R2 DECISION RE-ENTRY
          VALIDATED: P3-R
              |
              v
        PLAN AMENDMENT 3
              |
              v
      VR0 -> VR1 -> VR2 -> VR3 -> VR4 -> VR5
              |
       PROCEED-P3R1
              |
              v
        P3-R1 OWNER LOCALIZATION
              |
              v
        P3-R2 RUNTIME DECISION
              |
      +-------+--------+
      |                |
      v                v
 NO REPAIR         REPAIR IF PROVEN
      |                |
      +-------+--------+
              |
              v
       P4 SHORT 5->10->5
              |
            PASS
              |
              v
       P5A BASELINE 2 FREEZE
              |
              v
       P5B REPLACEMENT LONG 2
              |
            PASS
              |
              v
          P6 M10 CLOSE
              |
              v
             M11.1
```

Any `INCONCLUSIVE` result at a decision gate is a hard planning stop. It requires an explicit revision of this plan rather than an improvised new diagnostic.

## 5. Frozen change-authority matrix

| Item | Authority before its gate |
| --- | --- |
| Protection thresholds/semantics | frozen through P4; no current evidence supports retuning |
| Authority policy | frozen through P2R2/P4; D2 eliminated rod authority as first owner |
| exact-v9 | immutable historical/production identity; never reinterpret |
| Mission `bounded-demand-following-5-10-5@3` | frozen unless a later explicit versioning decision requires replacement |
| Generator-load production semantics | P2R2 selected P3-R owner localization, but production change remains forbidden through VR0–VR5 and P3-R1; change only after P3-R2 proves and authorizes a repair |
| Replacement workload | P2R2 did not authorize P3-W; remains frozen through VR0–VR5/P3-R1/P3-R2 unless a later explicit branch reconsideration authorizes it |
| Second replacement-long baseline | forbidden until P4 PASS |
| New exact version | required if P3-R changes production exact semantics |
| M11 work | forbidden until P6 closes M10 |

## 6. Validation cadence from P1 onward

For any candidate that changes code or tests:

1. build/restore as required by the existing CI contract;
2. run `eng\ci-ordinary.cmd` in Release;
3. run the focused gate for the current P-stage;
4. preserve the full generated artifact directory before authoring the next candidate;
5. compare GitHub CI against the same committed candidate before promotion.

Planning-only/documentation-only checkpoints such as P0, P2 and P2R use their dedicated documentation audit and do not substitute for executable validation.

## 7. Current authorization at P2R1 / Plan Amendment 2

P1A returned execution PASS / overall `INCONCLUSIVE`, P2R1 recorded `PLAN-STOP-INCONCLUSIVE`, Plan Amendment 2 defined P1B, and Deep Review Pass 2 returned local PASS / `PASS-AS-AUTHORED`. **P1B has now returned execution PASS / `COUPLED-MULTI-DOMAIN`; P2R2 is the active branch-authority candidate.** No production runtime/workload change or second replacement-long freeze is authorized. If P2R2 passes, only P3-R1 owner localization becomes authorized.


## P1A returned evidence and pre-Plan-Amendment-2 literature checkpoint

P1A has now completed execution PASS with overall classification `INCONCLUSIVE`: exact-v9 5.5 MWe classified `CONVERGED`; exact-v9 6 MWe remained `INCONCLUSIVE` at the frozen 3,600 s hard horizon. The 6 MWe late tail nevertheless demonstrates load reachability essentially at the requested electrical output and with no trip. The unresolved claim is whole-operating-point stationarity, not simple 6 MWe capacity.

Therefore P2R currently remains a **planning stop**:

- `P3-W authorized = False`;
- `P3-R authorized = False`;
- second replacement-long baseline authorization = `False`;
- exact-v9, workload, authority, generator-load semantics, protection and mission binding remain frozen.

Before Plan Amendment 2 is authored, the Todreas/Kazimi Volume I/II Deep Review Pass 1 is recorded as interpretation/planning evidence. It supports a next bounded diagnostic centered on conserved-inventory closure, canonical hydraulic compatibility/head-loss evidence, steam-path state and controller memory rather than a blind longer hold.

**This review is not Plan Amendment 2.** Exact durations, observation windows, acceptance/decision ceilings and executable contract are intentionally not frozen here.

After Plan Amendment 2 is authored, a mandatory Todreas/Kazimi **Deep Review Pass 2** must audit the exact amendment observables and decision rules before its new executable diagnostic is run.

References:

- `research/PRE_M11_TODREAS_KAZIMI_THERMAL_HYDRAULIC_DEEP_REVIEW_PASS1.md`;
- `research/PRE_M11_TODREAS_KAZIMI_DEEP_REVIEW_TRACEABILITY_PASS1.md`.


## Plan Amendment 2 — P1B Slow-State Closure & Phenomenon-Owner Qualification

P1A has returned execution PASS / overall `INCONCLUSIVE`. exact-v9 5.5 MWe is `CONVERGED`; exact-v9 6 MWe reaches essentially the requested electrical output with near-zero dispatch/net-acceleration error and no trip, but the frozen whole-operating-point stationarity slope ceilings remain exceeded. P2R1 therefore records `PLAN-STOP-INCONCLUSIVE` and selects neither P3-W nor P3-R.

Plan Amendment 2 does **not** extend the 6 MWe hold beyond 3,600 s. Instead it defines P1B as an observational replay of the same exact-v9 5→6 MWe path with a 600 s exact-v9 5 MWe background reference, one-second canonical state sampling, exact reproduction of the P1A 900/1,800/3,600 s checkpoints and a final 1,200 s analysis interval split into 4×300 s windows. P1B must expose represented conserved inventory, primary/drum/hydraulic, steam/turbine, controller/actuator and electromechanical state without synthesizing slip, phasic temperatures, ONB/NVG, density-wave margins, new natural-circulation/CHF correlations or prototype RBMK time scales.

The exact amendment is [`M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P2R_DECISION_PLAN_AMENDMENT2.md`](M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P2R_DECISION_PLAN_AMENDMENT2.md). Mandatory **Todreas/Kazimi Deep Review Pass 2** has now audited the actual amendment and returned local PASS / `PASS-AS-AUTHORED`; P1B code may therefore be authored from the frozen amendment. P1B always returns to **P2R2 Decision Re-entry 2**; it cannot select P3 directly.

Current amended route:

`P0 → P1 → P2(plan-stop) → P1A → P2R1(plan-stop) → Plan Amendment 2 → Todreas/Kazimi Deep Review Pass 2 → P1B(returned PASS) → P2R2(decision candidate) → P3-R1 owner localization if P2R2 PASS → later repair/branch decision → P4 → P5 → P6`.

## Todreas/Kazimi Deep Review Pass 2 — post-Plan-Amendment-2 audit

Returned artifact evidence validates Deep Review Pass 1 and P2R1 / Plan Amendment 2 Hotfix 1. The mandatory post-amendment literature/runtime ownership audit now has candidate disposition **`PASS-AS-AUTHORED`**. No Plan Amendment 2 engineering hotfix is required.

The disposition is conditional on binding P1B implementation constraints: canonical/exact-derived evidence only; 1 s as slow-state downsampling with per-step/canonical-event protection/numerical sentinels retained; conserved inventory/transfer evidence before downstream owner inference; no second hydraulic solve; controller memory separate from command/physical actuator state; raw physical units first; and **no single scalar cross-domain owner score in P1B v1**. `NO-MATERIAL-LATE-DRIFT` is not a stationarity promotion.

Deep Review Pass 2 validated locally and P1B has now returned execution PASS. P2R2 is therefore the current branch-authority gate. No P3 repair is authorized until P2R2 validates and P3-R1 subsequently identifies a specific runtime ownership/contract contradiction.

References:

- `research/PRE_M11_TODREAS_KAZIMI_THERMAL_HYDRAULIC_DEEP_REVIEW_PASS2.md`;
- `research/PRE_M11_TODREAS_KAZIMI_DEEP_REVIEW_TRACEABILITY_PASS2.md`.


## P1B executable implementation

Deep Review Pass 2 returned local PASS / `PASS-AS-AUTHORED`, and P1B has now returned execution PASS. The executable P1B contract is [`../eng/m10-final-replacement-long-closure-plan1-p1b-contract.json`](../eng/m10-final-replacement-long-closure-plan1-p1b-contract.json), the implementation note is [`history/m10-final/M10_FINAL_REPLACEMENT_LONG_DIAGNOSTICS_DOSSIER.md`](history/m10-final/M10_FINAL_REPLACEMENT_LONG_DIAGNOSTICS_DOSSIER.md), and the branch decision is now staged in [`M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P2R2_DECISION_REENTRY2.md`](M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P2R2_DECISION_REENTRY2.md).


## P1B returned evidence / P2R2 candidate decision

Returned P1B completed the 600 s 5 MWe background and 3,600 s exact-v9 5→6 MWe probe, reproduced all three P1A checkpoints, and recorded zero trip/nonconverged/nonfinite/rollback/line-search/untargeted/shadow sentinel failures. Its engineering label is `COUPLED-MULTI-DOMAIN`.

The decisive distinction is now **electrical reachability versus whole-plant stationarity**. At 3,600 s output/frequency/dispatch are essentially on request, but the final 300 s still records approximately +1.317 MW thermofluid stored-energy rate, falling drum mass/level, increasing primary channel/return flow, rising turbine-inlet pressure and continuing controller-memory motion. The 5 MWe reference does not show comparable drift. Total plant mass and numerical mass/energy closure remain green, so the evidence is represented internal redistribution rather than a numerical/conservation failure.

P2R2 therefore proposes:

- `P3-W-authorized=False`;
- `P3-R-owner-localization-authorized=True` **only after the P2R2 audit passes**;
- `production-repair-authorized=False`;
- next implementation: `P3-R1-Primary-Inventory-Hydraulic-Slow-State-Owner-Localization`.

P3-R1 remains observation/contract-audit work. It may use existing canonical main-circulation pump snapshots, feedwater/condensate pump snapshots, plant-network balances and controller diagnostics, but may not add constitutive physics, a second hydraulic solve, protection/workload changes or exact-v9 semantic changes.


## Plan Amendment 3 — external physical-reference assessment before P3-R1

P2R2 has now returned PASS and is authoritative for the branch choice: `P3-R-OWNER-LOCALIZATION`. This does **not** authorize a production repair. Before P3-R1 executes, Plan Amendment 3 inserts a bounded external-reference model-assessment sequence because the current V&V matrix intentionally distinguishes software/model verification from independent physical model assessment.

The amendment does not reopen P2R2 and does not select P3-W. It temporarily holds execution of P3-R1 while the following sequence is completed:

`VR0 reference/provenance freeze → VR1 point kinetics → VR2 IAPWS-IF97 water/steam error map → VR3 I-135/Xe-135 shutdown reference → VR4 ANS-5.1 decay-heat assessment → VR5 consolidated impact decision`.

VR5 may emit only:

- `PROCEED-P3R1-EXACTV9`;
- `BLOCK-P3R1-MODEL-REPAIR`;
- `PLAN-STOP-REFERENCE-GAP`.

Only `PROCEED-P3R1-EXACTV9` directly resumes the P3-R1 owner-localization gate. Any production semantic repair keeps exact-v9 immutable and requires a new exact version plus impact-based requalification.

Authoritative detailed planning documents:

- [`M10_FINAL_NEXT_STEPS_DETAILED_EXECUTION_PLAN.md`](M10_FINAL_NEXT_STEPS_DETAILED_EXECUTION_PLAN.md);
- [`M10_FINAL_PHYSICAL_REFERENCE_MODEL_ASSESSMENT_PLAN1.md`](M10_FINAL_PHYSICAL_REFERENCE_MODEL_ASSESSMENT_PLAN1.md);
- [`M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_PLAN_AMENDMENT3_EXTERNAL_MODEL_ASSESSMENT.md`](M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_PLAN_AMENDMENT3_EXTERNAL_MODEL_ASSESSMENT.md);
- [`M10_FINAL_MODEL_ASSESSMENT_CLAIM_POLICY.md`](M10_FINAL_MODEL_ASSESSMENT_CLAIM_POLICY.md);
- [`M10_FINAL_P3R1_TO_P6_DETAILED_GATE_MATRIX.md`](M10_FINAL_P3R1_TO_P6_DETAILED_GATE_MATRIX.md).
