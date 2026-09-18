# M10 FINAL REPLACEMENT LONG DIAGNOSTICS DOSSIER
> Historical consolidation dossier. The source documents below were completed/superseded and had no executable references at consolidation time. Their content is retained here for provenance while the individual top-level files are removed.
## Source manifest
| Original top-level file | Lines | SHA-256 (normalized LF UTF-8) |
| --- | ---: | --- |
| `M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC1.md` | 15 | `8686D5B7F9F42C41373E0424D2B96F475083E62E757502D43B621624A9519C20` |
| `M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC2.md` | 54 | `B11126CDA84DBDCF663085073E2139975924ADC06386F34A8707A92FBD91B2C3` |
| `M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC3.md` | 80 | `CB9F044BCCEAC829F1594198254A2C503A48CDF630D95053D2F2DBEDB85113F9` |
| `M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC4.md` | 57 | `ECB99F073226F5A5CFC105CD4C9354B24872F5608A3C494590300E4208D4B2E9` |
| `M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC5.md` | 69 | `EA969C091AB25619C987E7A30E42D5D3D9B3C4A2C8506A640790378BCD7CF61C` |
| `M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P1B_SLOW_STATE_OWNER_QUALIFICATION.md` | 56 | `776310A3B80893979275A331219974C0292A5B95668B7E8DF3B72042B1FBFC6C` |

## Retained source snapshots

---

## Source snapshot — `M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC1.md`

### M10 Final Replacement-Long Failure Diagnostic 1

**Status:** RETURNED / EXECUTION PASS — diagnostic evidence only. Replacement-Long Execution 1 remains RED.

The first authorized exact-v9 replacement campaign executed all five frozen legs in 35.2527 workstation minutes, so the redesigned wall budget is validated. The campaign remains RED because RL-M1 and RL-R1 both entered protection after the same 5 -> 10 MWe generator-load raise.

The failure is narrower than the aggregate summary initially suggests. RL-H1 passed 900 s with exact-v9 operating-point, conservation and numerical-coupling sentinels. RL-D1 and RL-P1 passed. MISSION live-projection scalability passed with late/early ratio 0.969616. RL-R1 produced identical authored, full-replay and checkpoint-continuation fingerprints, exact recording equivalence and bounded archive growth. Its only failing criterion was the same trip seen by RL-M1.

RL-M1 reached the 10 MWe demand at logical step 500, dispatched one `GeneratorLoadRaise`, and the challenge terminated `Failed` at step 637. RL-R1 independently reproduced protection after its load raise, leaving 5,365 of 6,000 authored steps with trip active. Therefore replay/checkpoint determinism is not the blocker; the common transient is.

The frozen replacement workload used only generator-load raise/lower commands. Existing M7.6 guidance is stronger: an on-grid load increase must coordinate generator loading with deliberate rod withdrawal/HOLD and turbine governing, stabilizing after each change. This makes an under-specified validation operator policy plausible, but it is not yet accepted as the diagnosis because the exact relay owner was not captured by Execution 1.

Diagnostic 1 changes no runtime source, protection threshold, exact-v9 seed, challenge pack or replacement workload. It repeats only the first 10 simulated seconds, dispatches the same 5 MWe load raise at step 500, and records every protection function on every 10 ms step together with reactor/turbine/generator/governor state. The returned evidence must identify first trigger, pickup and latch step for each relevant protection function before any repair or workload rewrite is authored.

The failed replacement long remains authoritative RED evidence. M10 remains OPEN. A second replacement campaign may not run until the resulting decision is implemented and a new baseline/workload freeze explicitly authorizes it.

---

## Source snapshot — `M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC2.md`

### M10 Final Replacement-Long Failure Diagnostic 2

#### Status

**RETURNED / EXECUTION PASS — evidence-only authority / coordinated-manoeuvre discrimination.** Replacement-Long Execution 1 remains authoritative **RED** evidence. Diagnostic 1 has now returned PASS and identified `generator-loss-of-synchronism` as the first protection function that completes its pickup/latch chain: trigger begins at step 587 and the 0.5 s pickup latches at step 636 / 6.36 s. Underfrequency begins earlier but does not complete its 1 s pickup before the generator trip.

No protection threshold, exact-v9 state, runtime physics, mission @3 definition or replacement-long workload is changed here.

#### Why Diagnostic 2 is needed

The frozen replacement workload used a deliberately simple operator policy: when the external demand first reaches 10 MWe, dispatch one `GeneratorLoadRaise`; when it later returns to 5 MWe, dispatch one `GeneratorLoadLower`.

That policy is weaker than the already validated M7.6 procedure. `POWER_MANOEUVRING_NORMAL_SHUTDOWN.md` explicitly requires a load raise to be coordinated with rod withdrawal/HOLD and turbine governing, with stabilization after each change.

Diagnostic 1 additionally showed why the distinction matters on exact-v9. The 5 MWe request increment immediately raises electrical loading to almost 10 MWe while turbine shaft power remains near 5.6 MW and reactor thermal power remains at ~32.97 MWth. The governor drives admission open, but the rotor decelerates, frequency falls and phase slip accumulates until loss-of-synchronism trips.

There is also an authority-semantic question. The failed long and Diagnostic 1 run `SupervisoryAutomatic` with `HoldCurrentOperatingPoint`. In that mode `SupervisoryOperationCoordinator` rewrites the reactor-power and turbine-speed loops back to automatic setpoints before each physical step. An operator rod command can therefore be neutralized before it reaches the plant, which would make the M7.6 coordination instruction impossible under the frozen validation policy even though the command seam itself is valid.

#### Probe matrix

Diagnostic 2 therefore runs eight independent 12 s exact-v9 probes. Every probe issues the same `GeneratorLoadRaise` before logical step 500. It compares:

- the exact frozen `SupervisoryAutomatic + HoldCurrentOperatingPoint + load-only` reference;
- the same supervisory path with a rod-withdraw/HOLD pulse spanning the load raise, specifically to test whether supervisory ownership suppresses it;
- an `Assisted` load-only control;
- five bounded `Assisted` rod-withdraw/HOLD timings around the same load raise.

`Assisted` is intentional: it preserves the existing local automatic turbine governor while allowing an operator command to take direct ownership of the selected rod controller. Manual takeover is not used because it would place every local controller, including turbine governing, in manual mode.

The test records every 10 ms sample for requested/actual electrical power, reactor thermal power, generator mechanical input, rotor speed, frequency, phase difference, average rod withdrawal and trip state. Each probe also records first trip, first latched protection owner and final controller/authority state.

#### Decision rule

The diagnostic execution itself is an evidence gate, not a hidden acceptance retune.

Compare the supervisory rod probe directly with the frozen load-only reference rather than assuming suppression. If its command produces no material change in rod/plant trajectory and it reproduces the frozen loss-of-synchronism trip path, while at least one Assisted coordinated probe materially delays or avoids the trip, the replacement workload/authority policy is under-specified relative to M7.6. If a bounded Assisted probe also reaches a late stable 10 MWe window with breaker closed and no trip, that path can become evidence for a **separate** revised replacement operator policy. Such a change still requires a new baseline/workload freeze before any second long.

If no Assisted coordinated probe improves the protection margin, do not weaken protection and do not rewrite the workload. Continue with a production transient/control-granularity diagnostic to determine whether a valid 5 MWe step is too coarse for the exact-v9 generator/turbine dynamics or another production runtime owner is deficient.

#### Validation

Run:

```bat
scripts\run-m10-final-replacement-long-failure-diagnostic2.cmd
```

Return the complete:

```text
artifacts\m10-final-replacement-long-failure-diagnostic2
```

before changing replacement workload, authority policy, protection semantics, exact-v9 runtime, mission pack or freezing a second replacement-long baseline.

---

## Source snapshot — `M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC3.md`

### M10 Final Replacement-Long Failure Diagnostic 3 — Paralleled Governor / Mechanical Preload / Historical Version Discrimination

**Status:** RETURNED / EXECUTION PASS — evidence only. Replacement-Long Execution 1 remains RED; no second replacement long is authorized.

#### Returned prerequisite

Replacement-Long Failure Diagnostic 2 completed PASS. Its returned artifact fixes these observations on the authoritative exact-v9 5→10 MWe path:

- the frozen `SupervisoryAutomatic + HoldCurrentOperatingPoint` reference again trips at logical step 636 with `generator-loss-of-synchronism` first latched;
- a supervisory rod-withdraw pulse produces zero physical rod motion and exactly reproduces the frozen trip;
- under `Assisted`, 2.5 / 5 / 10 percentage-point rod withdrawals do physically execute and increase reactor thermal power;
- nevertheless every Assisted rod/load probe still trips at the same logical step 636 with the same loss-of-synchronism owner;
- none survives two seconds after the load raise and none reaches a late stable 10 MWe window.

Therefore rod authority/coordination is not the missing margin. Protection retuning is not justified.

#### Why another diagnostic is required before runtime repair

M7.6 describes generator-load manoeuvring as coordinated with both reactor power and turbine governing. Diagnostic 2 closed only the reactor/rod branch. Existing current-v2 turbine audits also document an important breaker-closed rule: when the generator is paralleled, the droop adapter derives the effective governor speed reference from requested electrical load and supersedes direct `TurbineSpeedRaise/Lower` setpoint authority.

Diagnostic 1 additionally showed the transient energy mismatch directly. At the 5→10 MWe command the electrical side applies roughly 10.2 MW of mechanical-equivalent electromagnetic loading while turbine shaft power is only about 5.6 MW. The governor saturates and the physical control valve opens at its finite 0.5 fraction/s travel rate, but shaft power does not rise before loss-of-synchronism completes its pickup.

Before changing generator-load semantics or exact-v9, this gate must distinguish three possibilities:

1. the existing breaker-closed turbine-speed command is ineffective by design and the workload lacks mechanical-power prepositioning;
2. physical valve preloading still cannot create enough transient shaft margin, pointing toward generator-load-order/control-granularity semantics;
3. historical exact-v4 behaves materially better than exact-v9, pointing instead toward an exact-v9 steam-path transient-capacity regression.

#### Probe matrix

All probes use the existing 10 ms runtime and the same step-500 `GeneratorLoadRaise`. No production file is changed.

| Probe | Exact | Authority | Additional diagnostic action |
| --- | ---: | --- | --- |
| `exact-v9-frozen-supervisory-load-only` | 9 | SupervisoryAutomatic | frozen load-only reference |
| `exact-v9-assisted-load-only` | 9 | Assisted | D2 control |
| `exact-v9-assisted-speed-raise-1x-preload` | 9 | Assisted | one +10 rpm raw speed-reference command at step 400 |
| `exact-v9-assisted-speed-raise-5x-preload` | 9 | Assisted | five +10 rpm raw speed-reference commands at step 400 |
| `exact-v9-assisted-manual-valve-100-at-load` | 9 | Assisted | manual control-valve demand 100% at load step |
| `exact-v9-assisted-manual-valve-55-preload` | 9 | Assisted | manual control valve to 55% from step 400 |
| `exact-v9-assisted-manual-valve-65-preload` | 9 | Assisted | manual control valve to 65% from step 400 |
| `exact-v4-frozen-supervisory-load-only` | 4 | SupervisoryAutomatic | historical exact-version discrimination control |

The manual-valve probes are diagnostic perturbations only. They do not redefine M7.6 procedure or authorize a replacement workload.

#### Evidence recorded

The gate writes:

- `150-turbine-governing-preload-version-probe-summary.csv` — one-row-per-probe owner/timing, raw/effective governor references, preload shaft/valve state, load-step shaft/electromagnetic loading, frequency margin and captured exception status;
- `151-turbine-governing-preload-version-trajectories.csv` — complete 10 ms trajectories;
- `152-turbine-governing-preload-version-decision-summary.txt` — branch classification inputs and decision rule;
- `00-progress.txt` — probe progress for partial-failure recovery.

Only two physics outcomes are asserted because they are already returned facts: the frozen exact-v9 and Assisted load-only controls must reproduce step-636 loss-of-synchronism. Other probe outcomes are census evidence, not hypotheses disguised as test expectations. Any diagnostic exception in a non-reference probe is captured into the artifact instead of discarding the rest of the matrix.

#### Decision rule

1. If `TurbineSpeedRaise` changes the raw controller reference but not the effective breaker-closed governor setpoint, and trip timing remains unchanged, the direct SPEED seam is confirmed ineffective for paralleled load coordination.
2. If bounded physical valve preloading materially delays/avoids the trip, the frozen replacement operator policy is missing mechanical-power prepositioning. The next candidate must separately define and validate that operator/workload policy before a new baseline freeze. Do not retune protection.
3. If manual-at-load reproduces the automatic trajectory and preload also fails, compare exact-v4 with exact-v9:
   - matching failure → shared generator-load-order/control-granularity semantics; next work is a dedicated request-ramp/torque-coupling diagnostic before any runtime repair;
   - materially healthier exact-v4 → exact-v9-specific steam-path transient-capacity diagnosis.
4. Diagnostic 3 itself never authorizes a production change or second replacement long.

#### Validation

Run:

```bat
scripts\run-m10-final-replacement-long-failure-diagnostic3.cmd
```

Return the complete:

```text
artifacts\m10-final-replacement-long-failure-diagnostic3
```

before changing replacement workload, authority policy, generator-load semantics, protection semantics, exact-v9 runtime, mission pack, or freezing a second replacement-long baseline.

---

## Source snapshot — `M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC4.md`

### M10 Final Replacement-Long Failure Diagnostic 4 — Load Ramp / Torque Coupling / Energy Support Discrimination

**Status:** RETURNED / EXECUTION PASS — evidence only. Replacement-Long Execution 1 remains RED; no second replacement long is authorized.

#### Returned prerequisite

Diagnostic 3 completed PASS. The returned evidence establishes that the exact-v9 frozen 5→10 MWe step trips at logical step 636 with `generator-loss-of-synchronism`; breaker-closed `TurbineSpeedRaise` changes only the raw reference because droop owns the effective governor reference; 55%/65% valve preloading does not create material shaft-power margin; and historical exact-v4 reproduces the same protected failure family, slightly earlier at step 628.

This removes rod authority, direct SPEED authority, simple valve prepositioning, protection retuning and an exact-v9-only regression as supported first repairs.

#### Remaining question

At the frozen command the electrical request changes by the production default 5 MWe increment essentially in one 10 ms step while available shaft power is only about 5.6 MW. Diagnostic 2 showed that short rod withdrawals increase thermal power only slightly before the trip. Diagnostic 3 showed that opening the valve without creating additional upstream energy does not solve that mismatch.

Before changing generator-load semantics or the replacement workload, the remaining discrimination is therefore between:

1. command granularity / instantaneous torque demand — a smaller or slower electrical request ramp is sufficient even at the current operating point;
2. missing slow energy support — load ramp alone is insufficient, but coordinated reactor-power support or deliberate pre-powering establishes enough shaft margin;
3. deeper generator-grid torque coupling / attainable-capacity behaviour — even materially increased thermal/shaft support cannot establish a stable protected 10 MWe window.

#### Probe matrix

All probes are test-only. Production `ControlRoomRuntimeCommandPolicy.Default` remains unchanged. Test-only engine clones use the same solver/state/snapshot with diagnostic load increments where required.

| Probe | Exact | Electrical schedule | Energy support |
| --- | ---: | --- | --- |
| reference | 9 | +5 MWe at step 500 | hold current operating point |
| ramp A | 9 | +1 MWe every 1 s ×5 | hold current |
| ramp B | 9 | +0.5 MWe every 0.5 s ×10 | hold current |
| ramp C | 9 | +1 MWe every 2 s ×5 | hold current |
| supported ramp | 9 | +1 MWe every 2 s ×5 | reactor target scaled to next requested load, 1 s lead |
| pre-power | 9 | +5 MWe at step 2000 | reactor target 66 MWth from step 100 |
| historical supported | 4 | +1 MWe every 2 s ×5 | same proportional reactor support |

Every probe starts from `HoldCurrentOperatingPoint`; exploratory reactor objectives replace it only at their explicit support step.

#### Evidence

The gate writes `160-load-ramp-energy-support-probe-summary.csv`, `161-load-ramp-energy-support-trajectories.csv`, `162-load-ramp-energy-support-decision-summary.txt` and `00-progress.txt`. It records requested/output power, reactor thermal power, generator mechanical input, turbine shaft power, rotor/frequency/phase, commanded/effective electromagnetic torque, protection owner/timing and captured exceptions.

Only the already-returned exact-v9 reference result is asserted: step-636 `generator-loss-of-synchronism`. Exploratory outcomes remain census evidence rather than expected answers.

#### Decision rule

If a smaller/slower load-only ramp reaches a stable protected 10 MWe window, investigate command/workload granularity before production runtime repair. If load-only fails but reactor-supported or pre-powered operation succeeds, classify the frozen replacement manoeuvre as missing slow energy-support coordination. If support materially raises thermal/shaft power yet stable 10 MWe remains unattainable, escalate to generator-grid torque-coupling/attainable-capacity repair analysis. Historical exact-v4 is compared only under the same supported schedule to separate shared behaviour from version-specific steam-path capacity.

Diagnostic 4 authorizes no production change and no second replacement-long freeze.

#### Validation

Run:

```bat
scripts\run-m10-final-replacement-long-failure-diagnostic4.cmd
```

Return the complete `artifacts\m10-final-replacement-long-failure-diagnostic4` folder before changing replacement workload, authority policy, generator-load semantics, protection semantics, exact-v9 runtime, mission pack, or freezing a second replacement-long baseline.

---

## Source snapshot — `M10_FINAL_REPLACEMENT_LONG_FAILURE_DIAGNOSTIC5.md`

### M10 Final Replacement-Long Failure Diagnostic 5 — Measured Readiness-Gated Staged Load / Attainable Capacity

**Status:** RETURNED / EXECUTION PASS — evidence only. Replacement-Long Execution 1 remains RED; no second replacement long is authorized.

#### Returned prerequisite

Diagnostic 4 completed PASS. Its reference exact-v9 probe again reproduced `generator-loss-of-synchronism` at step 636. Smaller/slower fixed-time electrical ramps delayed the protection response but none established a stable 10 MWe window. One-second proportional reactor support did not change the result, and the nominal `66 MWth` pre-power probe still had only about 37.3 MWth at the actual 5→10 MWe step, with shaft power near 5.6 MW.

That last point matters: Diagnostic 4 proved that a **20 s commanded pre-power interval is insufficient**; it did not prove that a thermally prepared high-load state is unattainable.

#### Remaining question

M7.6 requires stabilization after each load change, but neither the failed replacement-long workload nor Diagnostic 4 waited for measured plant readiness before applying the next electrical increment.

Diagnostic 5 therefore removes elapsed-time assumptions. For each staged probe it:

1. starts from the exact production/historical low-load operating point;
2. requests the reactor thermal power proportional to the **next** electrical-load stage;
3. waits until measured thermal power is within 0.25 MW of that target while the generator remains paralleled and near synchronous frequency;
4. only then applies one test-only generator-load increment;
5. requires one continuous second of protected near-target electrical output/frequency before advancing;
6. after reaching 10 MWe, requires a further five continuous seconds of protected stable operation.

This is a diagnostic scheduler only. It does not modify production command semantics or the frozen replacement workload.

#### Probe matrix

| Probe | Exact | Increment | Purpose |
| --- | ---: | ---: | --- |
| frozen reference | 9 | +5 MWe | reproduce the returned step-636 owner |
| readiness staged A | 9 | +1 MWe | test attainable 10 MWe with measured thermal readiness |
| readiness staged B | 9 | +0.5 MWe | separate readiness from electrical-command granularity |
| historical control | 4 | +1 MWe | determine whether any remaining failure is shared or exact-v9-specific |

The +1/+0.5 MWe policies exist only in diagnostic engine clones. `ControlRoomRuntimeCommandPolicy.Default` remains unchanged.

#### Evidence

The gate writes:

- `00-progress.txt`
- `180-readiness-gated-probe-summary.csv`
- `181-readiness-gated-stage-events.csv`
- `182-readiness-gated-trajectories.csv`
- `183-readiness-gated-decision-summary.txt`

Trajectory evidence includes requested/actual electrical power, reactor thermal power, generator mechanical input, turbine shaft power, turbine steam flow, turbine-inlet pressure, effective specific work, control-valve position, relief flow, rotor/frequency/phase, commanded/effective electromagnetic torque and protection state.

#### Decision rule

If either exact-v9 readiness-gated schedule reaches and holds a protected stable 10 MWe window, the reduced-order plant has demonstrated attainable high-load capacity. The failed replacement-long path is then a workload/procedure timing plus command-granularity qualification gap; the next candidate should repair the workload/operator policy and freeze a new replacement baseline without retuning protection or generator-grid physics.

If measured readiness is reached for successive stages but a repeatable electrical stage still trips, use the recorded steam-flow/pressure/specific-work/relief and torque evidence to localize the first true capacity boundary.

If measured thermal readiness itself cannot be reached while holding the preceding stable electrical load, do not treat that as proof that the generator coupling is wrong. It instead shows that simple pre-powering is incompatible with the current reduced-order energy inventory; the next investigation should use an explicitly coupled reactor/load ramp rather than storing energy upstream.

Historical exact-v4 is interpreted only under the same readiness algorithm.

Diagnostic 5 authorizes no production change and no second replacement-long freeze.

#### Validation

The focused script deliberately runs the established ordinary **Release** CI entry point first, then the explicit diagnostic:

```bat
scripts\run-m10-final-replacement-long-failure-diagnostic5.cmd
```

Return the complete `artifacts\m10-final-replacement-long-failure-diagnostic5` folder before changing replacement workload, authority policy, generator-load semantics, protection semantics, exact-v9 runtime, mission pack, or freezing a second replacement-long baseline.

---

## Source snapshot — `M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN1_P1B_SLOW_STATE_OWNER_QUALIFICATION.md`

### M10 Final Replacement-Long Closure Plan 1 — P1B Slow-State Closure & Phenomenon-Owner Qualification

**Status: RETURNED EXECUTION PASS — frozen evidence for P2R2 Decision Re-entry 2.**

P1B is the only implementation authorized after Plan Amendment 2 and Todreas/Kazimi Deep Review Pass 2. It does not repair runtime physics and does not select P3.

#### Question

When unchanged exact-v9 repeats the already-demonstrated 5→6 MWe trajectory, which **represented canonical state domain** remains materially dynamic in the final tail relative to a 5 MWe background reference?

#### Frozen execution

- exact-v9 5 MWe background reference: **600 s**, no load command;
- exact-v9 5→6 MWe: identical clean initial condition, supervisory authority, thermal preparation and +1 MWe test-only generator-load semantics used by P1A;
- expected load-command logical step: **2785**;
- P1A checkpoint reproduction at **900 / 1800 / 3600 s**, fail-closed with the P1A tolerances;
- maximum hold: **3600 s after load**, no continuation;
- 1 s owner-state output is **slow-state downsampling only**;
- protection, hydraulic convergence/rollback/line-search and other available numerical sentinels are audited at canonical step/event cadence;
- late analysis: final **1200 s**, four contiguous **300 s** windows.

#### Diagnostic trend rule

P1B v1 has **no scalar cross-domain owner score**. Raw values and raw slopes are emitted first. For each selected observable a load-specific persistent-trend indication is diagnostic-only and is frozen before execution:

`guard = max(10 × |5 MWe background slope|, 1e-12 × max(1, |5 MWe background mean|))` per second in that observable's own units.

A late trend is tagged persistent only when at least **3 of the 4** 300 s load windows exceed the guard in a common direction. This indication is not a physical acceptance threshold and cannot authorize P3.

The owner vocabulary remains:

`NEUTRONICS-THERMAL-SOURCE`, `PRIMARY-INVENTORY-HYDRAULIC`, `STEAM-PATH-TURBINE`, `CONTROL-ACTUATOR-MEMORY`, `ELECTROMECHANICAL-GRID`, `COUPLED-MULTI-DOMAIN`, `NO-MATERIAL-LATE-DRIFT`, `INCONCLUSIVE`.

If multiple represented domains show persistent load-specific motion, the automatic evidence label is conservatively `COUPLED-MULTI-DOMAIN`; P2R2 performs the causal interpretation using the raw inventory/transfer evidence.

#### Evidence ordering

1. conserved inventories;
2. canonical transfers and conservation closure;
3. hydraulic compatibility and represented flow redistribution;
4. steam path;
5. controller memory and actuator demand/physical state;
6. turbine/rotor;
7. generator/grid.

A downstream slope is not root-cause proof while an upstream represented inventory or controller state remains dynamic.

#### Fidelity boundary

P1B does not synthesize slip, independent phasic temperatures, ONB/NVG, new flow-regime classification, density-wave margin, natural-circulation/CHF correlations, two-fluid closure or prototype RBMK time constants.

#### Exit

Execution PASS requires background/probe completion, all three P1A checkpoints, finite required observables, healthy protection/numerical sentinels and artifact emission. The engineering label may still be `INCONCLUSIVE` or `COUPLED-MULTI-DOMAIN`.

P1B has returned to **P2R2 Decision Re-entry 2** with engineering label `COUPLED-MULTI-DOMAIN`. P1B itself still authorizes neither P3 branch nor a second replacement-long baseline; branch authority belongs only to P2R2.
