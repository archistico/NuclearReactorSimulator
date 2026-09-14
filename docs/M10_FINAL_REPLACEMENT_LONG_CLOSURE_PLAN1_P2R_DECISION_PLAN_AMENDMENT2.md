# M10 Final Replacement-Long Closure Plan 1 — P2R Decision Re-entry 1 / Plan Amendment 2

**Status: HOTFIX 1 VALIDATED — `PLAN-STOP-INCONCLUSIVE` / PLAN AMENDMENT 2. Deep Review Pass 2 subsequently VALIDATED `PASS-AS-AUTHORED`; P1B implementation active.**

P1A returned local execution PASS and preserved the frozen P1/P2 prerequisites, but its overall engineering classification is `INCONCLUSIVE`: exact-v9 5.5 MWe is `CONVERGED`, while exact-v9 6 MWe reaches the requested electrical load without trip yet remains above the predeclared whole-operating-point stationarity slope ceilings at the hard 3,600 s horizon.


> **Validator provenance — Hotfix 1.** The first DocsPlanning4 audit did not reach the Plan Amendment 2 validator. The prerequisite Deep Review Pass 1 validator was red because it required a pre-amendment `PROJECT.md` heading that had correctly been superseded by the current Plan Amendment 2 checkpoint. Windows PowerShell also displayed the UTF-8 em dash as mojibake. Hotfix 1 changes only validator encoding/marker robustness; P1A evidence, P2R1 decision, P1B planning scope and all authorization boundaries are unchanged.

P2R therefore **does not select P3-W or P3-R**. This is the second explicit planning stop required by Closure Plan 1. The purpose of Plan Amendment 2 is not to wait longer for the same four P1 slopes; it is to replay the already-observed 6 MWe horizon with richer observation of the canonical state owners so the residual slow dynamics can be localized before any repair or workload change is authorized.

## 1. Frozen P1A evidence

The returned P1A artifact records:

- gate marker `m10-final-replacement-long-closure-plan1-p1a-passes=True`;
- exact-v9 5.5 MWe final classification `CONVERGED` after 2,798 s hold;
- exact-v9 6 MWe final classification `INCONCLUSIVE` after the full 3,600 s hold;
- no trip/latch/exception in either exact-v9 probe;
- P1 checkpoints reproduced and exact-v4 not rerun;
- no production source/test/runtime/workload/authority/generator-load/protection/exact-v9/mission change;
- no P3 branch and no second replacement-long authorization.

For the 6 MWe probe, the frozen checkpoints are:

| Hold after load | Output MWe | Thermal MWth | Shaft MW | Frequency Hz | Dispatch adequacy MW | Steam flow kg/s | Turbine inlet MPa | Control valve % |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 900 s | 5.8710522993 | 39.5922367906 | 6.4908709368 | 50.0000654230 | -0.1315793512 | 15.0950486903 | 4.9942550339 | 59.4783146530 |
| 1,800 s | 5.9321917176 | 39.5554016825 | 6.5532570735 | 50.0000102832 | -0.0691921117 | 15.2401327291 | 5.0915152264 | 59.4572350324 |
| 3,600 s | 6.0003442329 | 39.5560802355 | 6.6228001721 | 50.0000044832 | +0.0003511028 | 15.4018608655 | 5.2184166817 | 59.4309308356 |

The P1A late-tail summary at 6 MWe has mean output 5.9997726846 MWe, output error 0.0002273154 MWe, mean frequency 50.0000072027 Hz, dispatch adequacy -0.0002321407 MW and effectively zero net rotor acceleration. However, the frozen P1 stationarity ceilings remain exceeded by output, shaft, steam-flow and especially turbine-inlet-pressure slopes.

The evidence status is therefore explicit:

- **`6 MWe LOAD REACHABILITY = DEMONSTRATED`**;
- **`6 MWe WHOLE-OPERATING-POINT STATIONARITY = NOT DEMONSTRATED`**.

The second statement is a non-qualification result, not permission to relax the frozen stationarity ceilings.

## 2. P2R Decision Re-entry 1

P2R records:

- `P2R-DECISION = PLAN-STOP-INCONCLUSIVE`;
- `P3-W-AUTHORIZED = False`;
- `P3-R-AUTHORIZED = False`;
- `SECOND-REPLACEMENT-LONG-AUTHORIZED = False`;
- Replacement-Long Execution 1 remains immutable RED evidence;
- exact-v9 remains the authoritative production exact version and is not reinterpreted;
- production replacement workload, authority policy, generator-load semantics, protection semantics and mission binding remain frozen.

The reason is now narrower than at P2. A hard electrical capacity boundary near 6 MWe is not supported. What remains unresolved is **which represented canonical owner still carries the late state evolution** after electrical output, frequency and reactor thermal power are already near their requested/static values.

## 3. Explicit non-conclusions

P2R does not infer any of the following from P1A:

- that the remaining drift is a generator-grid defect;
- that the remaining drift is a steam-path defect;
- that the remaining drift is natural circulation, density-wave instability, slip, ONB/NVG or any other physics not explicitly represented by the current model;
- that the P1 stationarity ceilings should be relaxed;
- that 7,200 s or any further blind asymptotic extension would resolve the engineering question;
- that the eventual P3 branch is already known.

## 4. Plan Amendment 2 — P1B Slow-State Closure & Phenomenon-Owner Qualification

### 4.1 Question

**P1B question:** when exact-v9 repeats the already-demonstrated 5→6 MWe trajectory, which canonical represented state domain remains materially dynamic in the late tail, and is that residual evolution load-specific, baseline/background, coupled across domains or not localizable with the current model?

P1B is an **observation/owner-localization gate**, not a new physical model, repair, workload experiment or stationarity-threshold relaxation.

### 4.2 Fixed physical scope

Before implementation, Plan Amendment 2 freezes these physical boundaries:

1. Production `src/`, exact-v9, replacement workload, authority policy, generator-load semantics, protection semantics and mission @3 remain unchanged.
2. The only load-change probe is exact-v9 **5→6 MWe**, using the same clean deterministic initial condition, supervisory authority, thermal-preparation method and +1 MWe test-only command semantics already used by P1A.
3. P1B must reproduce the P1A 6 MWe load-command logical step **2785** and the 900 s / 1,800 s / 3,600 s checkpoints before any owner conclusion is accepted.
4. The P1A checkpoint tolerances remain the drift-detection tolerances: power/shaft/dispatch ±0.002 MW, thermal ±0.01 MW, frequency ±0.0001 Hz, steam flow ±0.01 kg/s and turbine-inlet pressure ±0.002 MPa.
5. P1B adds **no hold beyond 3,600 s after the 6 MWe load command**. There is no P1B continuation and no hidden 7,200 s path.
6. A separate exact-v9 **5 MWe background-reference observation of 600 s** is allowed only to measure late background/numerical/control drift for the newly exposed owner variables. It issues no load command and changes no control semantics.
7. Healthy trajectories are sampled at **1 s** resolution for slow-state analysis; exact command/checkpoint events are also captured at their precise logical steps.
8. The fixed late-analysis interval is the final **1,200 s** of the 6 MWe hold, partitioned into four contiguous **300 s** windows. This is used to distinguish persistent drift from a single local fluctuation. The same 300 s late-tail concept is applied to the 5 MWe background reference where possible.
9. Any trip, non-finite state, checkpoint mismatch or missing required owner observable makes owner localization `INCONCLUSIVE`; it does not authorize a branch.

### 4.3 Required canonical observation tiers

P1B must observe existing canonical state or diagnostics. It must not create substitute constitutive physics in the test layer.

**Tier A — required:**

- whole-plant fluid mass, fluid internal energy, thermal-body stored energy and total coupled stored energy where already canonical/derivable by exact summation of committed state;
- every canonical fluid-node id with committed mass and internal energy; pressure/temperature/phase/quality/void only where already supplied by the current canonical snapshot/model;
- total primary mass, feedwater inflow, steam export and per-drum pressure, level, incoming-return flow, steam flow, recirculation flow and represented separable-liquid inventory;
- reactor total/non-rod/rod reactivity, point-kinetics/fission-power state already exposed, reactor thermal power and available canonical thermal-body state;
- turbine main-steam/admission flow and pressure state, requested/effective/physical valve positions where already exposed, turbine shaft/passive-loss/net-acceleration balance;
- generator requested load, actual output, frequency/slip, phase, electromagnetic torque and existing phase/frequency correction terms;
- reactor-primary and turbine-secondary controller diagnostics already exposed by `ControlAndActuatorSnapshot`: controller id/mode, setpoint, measurement/error, P/I/D terms, unsaturated/output value, saturation/anti-windup status and actuator command state;
- protection state and existing numerical/coupling sentinels required to distinguish a physical tail from a numerical failure.

**Tier B — optional only when already canonical or directly derivable without new physics:**

- current-owner pressure-loss/head decomposition;
- existing pump available-head/system-loss evidence;
- per-loop/per-group flow redistribution or pressure-drop spread already represented by the runtime;
- diagnostic fractional inventory time such as `|M/(dM/dt)|` or `|U/(dU/dt)|`, guarded near zero derivatives and labelled diagnostic-only;
- lag/correlation summaries between upstream inventory/pressure changes and downstream steam/shaft/output changes;
- existing phase/branch transition counts.

**Tier C — forbidden to synthesize in P1B:**

- liquid/vapor slip velocity or separate phasic temperatures;
- ONB/NVG location, new flow-regime classifiers or drift-flux/two-fluid closure quantities;
- density-wave stability margins;
- new natural-circulation, CHF/dryout or two-phase pressure-drop correlations;
- any prototype/full-scale RBMK time constant inferred from the reduced 10 MWe model.

### 4.4 Reference-relative late-dynamic assessment

P1B must report raw dimensional values and slopes first. Any normalized owner score is **diagnostic ranking only**, not a new physical acceptance threshold.

The implementation must compare the 6 MWe late windows with the 5 MWe background-reference behavior for the same observable. The final normalization formula, numerical guard and domain-aggregation rule must be frozen in the executable P1B contract **before execution** and must pass the mandatory Todreas/Kazimi Deep Review Pass 2 described below. No score or threshold may be tuned after seeing P1B results.

The owner-domain vocabulary is frozen to:

- `NEUTRONICS-THERMAL-SOURCE`;
- `PRIMARY-INVENTORY-HYDRAULIC`;
- `STEAM-PATH-TURBINE`;
- `CONTROL-ACTUATOR-MEMORY`;
- `ELECTROMECHANICAL-GRID`;
- `COUPLED-MULTI-DOMAIN`;
- `NO-MATERIAL-LATE-DRIFT`;
- `INCONCLUSIVE`.

These are evidence labels, not automatic P3 selections. In particular, downstream shaft/output drift alone cannot be declared the root owner when an upstream conserved inventory or controller state is still evolving.

### 4.5 Planned artifact contract

The later executable P1B gate must emit at least:

1. background-reference calibration/late-drift summary;
2. P1A checkpoint-reproduction table;
3. one-second whole-domain trajectory summary;
4. long-form fluid-node mass/internal-energy evidence by canonical node id;
5. primary/drum/flow evidence;
6. controller/actuator diagnostic evidence;
7. turbine/generator balance evidence;
8. domain late-window trend/ranking summary;
9. events/trips/branch/numerical-sentinel evidence;
10. one P1B engineering summary that explicitly returns to **P2R2 Decision Re-entry 2**.

### 4.6 P1B execution PASS versus engineering result

A future P1B **execution PASS** means that its frozen scope ran, P1A checkpoints reproduced, required canonical evidence was emitted, values remained finite and no harness/contract failure occurred. It does not mean that an owner was necessarily localized.

The engineering evidence may still be `INCONCLUSIVE` or `COUPLED-MULTI-DOMAIN`. P1B is forbidden to write `P3-W-AUTHORIZED=True`, `P3-R-AUTHORIZED=True` or `SECOND-REPLACEMENT-LONG-AUTHORIZED=True`.

## 5. Mandatory Todreas/Kazimi Deep Review Pass 2 hold

Plan Amendment 2 deliberately inserts a literature-audit hold **before P1B implementation**.

After this amendment is validated, the next authorized activity is **Todreas/Kazimi Deep Review Pass 2**, not P1B code. Pass 2 must audit the actual amendment above against Volume I/II and the current runtime ownership model. At minimum it must verify:

1. every Tier A observable is canonical or a transparent exact derivation from committed canonical state;
2. no Tier C quantity has entered the design indirectly;
3. the 600 s background reference, 3,600 s replay horizon, 1 s sampling and 4×300 s late windows are defensible for the stated owner-localization question and are not being presented as real-RBMK time constants;
4. mass/energy inventories and inter-component transfer evidence are sufficient to avoid using the largest downstream slope as a false root-cause proxy;
5. boundary/flow/head observations do not overconstrain the hydraulic problem;
6. controller memory and physical actuator state remain separate owners;
7. physical and numerical drift are distinguishable with the available sentinels;
8. the proposed reference-relative ranking cannot become a post-hoc physical acceptance threshold;
9. no literature correlation or coefficient has been imported into M10;
10. P1B remains bounded, observational and incapable of selecting P3 directly.

Pass 2 dispositions are limited to:

- `PASS-AS-AUTHORED` — P1B implementation may be prepared from this amendment;
- `AMENDMENT-HOTFIX-REQUIRED` — correct Plan Amendment 2 documentation/contract before P1B implementation;
- `PLAN-STOP` — the current model cannot support the intended owner-localization claim without an explicit higher-level planning decision.

## 6. Amended closure route

The route is now:

`P0 VALIDATED → P1 INCONCLUSIVE → P2 PLAN-STOP VALIDATED → P1A PASS/INCONCLUSIVE → P2R1 PLAN-STOP → Plan Amendment 2 → Todreas/Kazimi Deep Review Pass 2 → P1B → P2R2 → P3-W/P3-R/or planning stop → P4 → P5 → P6`.

P2R2, not P1B, is the next branch authority. It must interpret P1B together with P1/P1A and literature evidence and either authorize one P3 branch or record another explicit planning stop.

## 7. Current authorization

If this documentation gate passes:

- P2R1 / Plan Amendment 2 becomes the active validated planning checkpoint;
- **the only next authorized work is Todreas/Kazimi Deep Review Pass 2**;
- this prerequisite has now been satisfied: Deep Review Pass 2 is locally VALIDATED with disposition `PASS-AS-AUTHORED`; P1B is the active implementation;
- P3-W, P3-R, P4, P5, Replacement-Long Execution 2 and M11 remain unauthorized.


## 8. Returned validation and Deep Review Pass 2 disposition

The user-returned local documentation artifacts validate both prerequisite Deep Review Pass 1 and this P2R / Plan Amendment 2 Hotfix 1 gate. The validated summary preserves `P2R-DECISION=PLAN-STOP-INCONCLUSIVE`, both P3 branches false, second-long authorization false, 600 s background, 3,600 s maximum hold and the mandatory Pass 2 hold.

The mandatory post-amendment review is documented in `research/PRE_M11_TODREAS_KAZIMI_THERMAL_HYDRAULIC_DEEP_REVIEW_PASS2.md`. Its candidate disposition is **`PASS-AS-AUTHORED`**. This does not rewrite Sections 1–7 above; it adds binding implementation interpretation: canonical/exact-derived evidence only, 1 s slow-state downsampling plus canonical-step/event sentinels, no second hydraulic solve, no Tier C physics, no scalar cross-domain owner score in P1B v1, and no promotion of `NO-MATERIAL-LATE-DRIFT` to a stationarity claim.

This prerequisite is now satisfied: the Pass 2 documentation audit returned local PASS / `PASS-AS-AUTHORED`, so P1B implementation is authorized while P3 remains blocked pending P2R2.
