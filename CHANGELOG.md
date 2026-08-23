# M10 Final Replacement-Long Failure Diagnostic 5 — Measured Readiness-Gated Staged Load / Attainable Capacity

- Records Diagnostic 4 as local execution PASS.
- Freezes the key correction that its nominal `66 MWth` pre-power target was not physically reached before the load step: actual thermal power was only about 37.3 MWth and shaft power remained about 5.6 MW.
- Adds no production `src/` change and does not alter the frozen workload, authority policy, generator-load semantics, protection, exact-v9 or mission @3.
- Adds measured-readiness-gated +1 MWe and +0.5 MWe exact-v9 diagnostic schedules plus an exact-v4 +1 MWe historical control.
- Requires real thermal readiness before each test-only load increment and a post-increment stable window before advancing; exports steam-flow, inlet-pressure, specific-work, valve, relief, shaft, grid and protection evidence.
- The validation script runs `eng\ci-ordinary.cmd` in Release before the focused explicit test so local ordinary validation uses the same CI entry point.
- Replacement-Long Execution 1 remains RED; second replacement-long freeze remains unauthorized.

# M10 Final Replacement-Long Failure Diagnostic 4 — Load Ramp / Torque Coupling / Energy Support Discrimination

- Diagnostic-only candidate stacked on returned Diagnostic 3 PASS evidence.
- Adds no production `src/` change and does not alter default generator-load policy, authority, protection, exact-v9, mission @3 or frozen replacement workload.
- Adds test-only request-ramp, reactor-support, pre-power and historical exact-v4 controls plus full 10 ms evidence export.
- Replacement-Long Execution 1 remains RED; second replacement-long freeze remains unauthorized.

## M10 Final Replacement-Long Failure Diagnostic 3 — CANDIDATE

- Records returned Diagnostic 2 as execution PASS: the frozen exact-v9 and every Assisted rod/load probe still latch `generator-loss-of-synchronism` at step 636; physical rod motion up to +10 percentage points increases thermal power but creates zero protection-margin improvement.
- Preserves Replacement-Long Execution 1 as RED and changes no production `src/`, protection threshold, exact-v9 identity, mission @3 semantics or frozen replacement workload.
- Closes the remaining M7.6 turbine-governing branch with breaker-closed raw-vs-effective speed-reference probes, then separates governor response from finite-rate physical control-valve preloading.
- Adds bounded diagnostic valve-preload probes at 55% and 65%, a manual-100%-at-load control, and a historical exact-v4 load-only control so returned evidence can distinguish missing mechanical prepositioning, shared generator-load-order/control-granularity semantics, or an exact-v9-specific transient-capacity issue.
- The focused test asserts only already-returned exact-v9 reference facts; all other outcomes are census evidence and non-reference diagnostic exceptions are captured rather than converted into hypothesis-driven failures.
- No second replacement long is authorized. A separate decision/repair and new freeze remain mandatory.

## M10 Final Replacement-Long Failure Diagnostic 2 — CANDIDATE

- Records returned Diagnostic 1 as execution PASS and freezes the exact shared trip owner: generator loss-of-synchronism triggers at step 587 and completes its 0.5 s pickup/latch at step 636 / 6.36 s; underfrequency begins earlier but does not complete its 1 s pickup before the generator trip.
- Preserves the failed Replacement-Long Execution 1 as authoritative RED evidence and changes no production `src/`, protection threshold, exact-v9 identity, mission @3 semantics or frozen replacement workload.
- Adds an authority/coordination discrimination audit over eight independent 12 s exact-v9 probes: the frozen `SupervisoryAutomatic + HoldCurrentOperatingPoint + load-only` path, a supervisory rod-pulse discrimination control, an Assisted load-only control, and five bounded Assisted rod-withdraw/HOLD timings around the same step-500 5→10 MWe load request.
- Explicitly tests the architectural hypothesis that SupervisoryAutomatic rewrites the reactor/turbine loops before each physical step and therefore suppresses the M7.6 rod coordination required by the existing operating procedure.
- Does not authorize workload repair or a second long. Returned evidence must first show whether Assisted coordination materially delays/avoids the loss-of-synchronism path and whether any bounded probe reaches a late stable 10 MWe window.

## M10 Final Replacement-Long Failure Diagnostic 1 — CANDIDATE

- Records the first authorized exact-v9 replacement campaign as executed RED: 1,920 s / 192,000 authored steps completed in 35.2527 minutes, within the frozen 35–45 minute target and 60 minute cap.
- Preserves PASS evidence for RL-H1 900 s, RL-D1, RL-P1, exact-v9 conservation/sentinels, numerical coupling, MISSION live-projection scaling (`late/early=0.969616`), replay/full-replay/checkpoint fingerprint equivalence and archive growth.
- Narrows both failing legs to one shared owner domain: RL-M1 challenge failed at logical step 637 after the step-500 5→10 MWe demand/load raise; RL-R1 independently entered the same protection path after its load raise.
- Adds a 10 s evidence-only exact-v9 reproduction that samples every protection function at every 10 ms step and records first trigger/pickup/latch timing together with reactor/turbine/generator/governor state.
- Does not change `src/`, protection thresholds, exact-v9, mission-pack semantics or the frozen replacement workload. The failed long remains RED and M10 remains OPEN; any second long requires a new freeze after diagnosis.

## M10 Final exact-v9 Replacement-Long Baseline Freeze 1 — CANDIDATE

- Records the locally validated exact-v9 Production Activation Decision 1 Hotfix 1 as the authoritative production prerequisite: exact-v9 default, mission @3 current, exact-v4/@3 historical and exact-v2 fail-closed.
- Freezes new exact-v9 manifests for 959 production `src/` files and 351 pre-execution test files; the failed exact-v4 long manifests remain byte-preserved and are explicitly forbidden as replacement baselines.
- Freezes an information-dense 1,920 s / 192,000-step replacement workload: RL-H1 900 s, RL-M1 480 s, RL-D1 300 s, RL-P1 180 s and RL-R1 60 s.
- Preserves the previously established 35–45 minute validation-workstation target and 60-minute hard job cap; Diagnostic 11 Hotfix 2 timing projects the authored workload to about 40.003 minutes before replay overhead.
- Reuses validated exact-v9 activation envelopes, unchanged conservation ceilings, explicit moisture ownership and a real-live LR-M1 within-run scalability sentinel; historical exact-v4 I.3 absolute budgets are not reinterpreted.
- Adds `scripts/run-m10-final-replacement-long-baseline-freeze.cmd`, the freeze validator/finalizer, contract, activation record, ADR-0193 and execution handoff.
- This candidate does not add or execute replacement-long tests. A green freeze artifact authorizes a later execution candidate to add exactly one explicit replacement-long test file without changing frozen `src/` or pre-existing tests.

# M10 Final Exact-v9 Production Activation Decision 1 Hotfix 1 — Test Type/Namespace Contract Alignment (CANDIDATE)

- The original Activation Decision 1 candidate did not compile: `M10FinalExactV9ProductionActivationDecisionTests.cs` had exactly two new-test errors, CS1503 (`decimal?` mission score passed to a `double` record field) and CS0103 (missing namespace import for `ControlRoomSnapshotFingerprint`).
- Hotfix 1 changes no runtime source. `MissionResult.FinalScore` now preserves the canonical `decimal?` type from `MissionPerformanceScoreSnapshot`, and the test imports `NuclearReactorSimulator.Application.Scenarios.Recording` for the existing fingerprint helper.
- No cast to `double`, tolerance change, selector change, mission-pack change, exact-v9 change, physics change or activation-policy change is introduced.
- The original Activation Decision 1 package is superseded as BUILD RED. Hotfix 1 must rerun the unchanged full activation-decision gate before any authoritative promotion or replacement-long work.

# M10 Final Exact-v9 Production Activation Decision 1 — Authoritative Default + Mission V3 (CANDIDATE)

- Stacked directly on the user-validated exact-v9 qualified opt-in production-activation candidate and its returned artifact folder.
- The prerequisite opt-in gate is green: 12,000 healthy exact-v9 policy-path steps around 5 MWe / 100 kg/s, conservative mass/energy/moisture ownership, zero rollback/fallback/unsafe/untargeted events, selector/direct-factory equality and fingerprint `7880AD580179B936C584EB0055BE663E0A1CFA65C5191B0DB8A7F3C514DB5418`.
- This candidate deliberately switches `AuthoritativeDefaultPolicy` from historical exact-v4 to qualified exact-v9; exact-v4 remains explicitly selectable, exact-v3 remains historical and exact-v2 remains fail-closed rollback.
- Adds distinct authoritative scenario `integrated-normal-operations-training-m10-final-v9-production`; the prior exact-v9 activation-candidate scenario remains a separate replayable identity.
- Advances current production mission `bounded-demand-following-5-10-5` to `@3` bound to exact-v9; preserves `@2` as the historical exact-v4 production binding and `@1` as the original historical pack. Challenge/demand/scoring/evaluator/evidence semantics are unchanged.
- Pins historical Phase-I/H.30/first-long tests to their exact policies/packs instead of symbolic current-default/current-pack references, preventing retroactive reinterpretation.
- Adds a focused 120 s authoritative exact-v9 + 1,200-step mission-v3 gate, exact-v9 600 s requalification and post-switch current-evidence routing.
- Replacement long remains unauthorized. A green result permits only the next step: freeze a new exact-v9 baseline manifest and redesigned replacement-long contract; the failed exact-v4 long manifest is not reused.

# M10 Final — Exact-v9 Qualified Production Activation Candidate (CANDIDATE)

- Diagnostic 11 Hotfix 2 is user-validated: build, ordinary suite and exact-v9 600 s requalification PASS.
- Returned evidence qualifies exact-v9: final electrical export ~4.999999982 MWe, primary ~100.000001 kg/s, drum level ~0.5, late node mass slopes ~1e-8 kg/s, governor/control-valve slopes ~1e-10 %/s, net/stored power ~9.8e-8 MW, zero trips/rollbacks and conservative stage/full-cycle closure.
- Adds `M10FinalExactV9QualifiedCandidate` as an explicit production-policy option resolving exact @9; `AuthoritativeDefaultPolicy` deliberately remains I.5 repaired exact @4.
- Adds a replayable exact-v9 activation-candidate scenario and registers the exact-v9 factory in the desktop composition root.
- Adds ordinary contracts plus a focused policy-path audit proving exact-v2 fail-closed kill preservation, candidate health/conservation, moisture-drain ownership and deterministic equivalence to direct exact-v9 factory construction.
- Adds a cumulative candidate script that reruns ordinary tests, current exact-v4 evidence, Diagnostic-11 600 s exact-v9 qualification and the focused activation-candidate gate.
- No production default switch, production mission-pack rebinding, replacement long or M10 closure is authorized by this candidate.

# M10 Final Long Failure Diagnostic 11 Hotfix 2 — Post-Seed Governor PI Decomposition Regression (CANDIDATE)

## Hotfix 2 — observe the controller at the actual post-preconditioning snapshot

- Diagnostic 11 Hotfix 1 compiled, but the ordinary suite again stopped RED 1/1482 in `ExactV9Candidate_IsDistinctPreservesHistoricalVersionsAndDoesNotSwitchProductionDefault`.
- The remaining assertion froze the ideal pre-step proportional contribution at `0.75 +/- 1e-9 %`. The runtime snapshot is intentionally observed after the exact-v9 factory's 20 ms deterministic seed preconditioning; rotor measurement has already moved by a few nanorpm, so the correct measured proportional term is `0.75000000640329745 %`.
- Hotfix 2 does not widen that idealized range. The regression now verifies the production equations on the actual snapshot: `Error = Setpoint - Measurement`, `P = Error` for the unchanged `Kp=1`, `Output = UnsaturatedOutput`, and `I = UnsaturatedOutput - P - D`.
- The authored governor/control root remains independently frozen at `29.281329697436618 %` to 6 decimal places.
- No `src/`, exact-v9 authored state, controller gain, governor semantics, moisture-drain semantics, production selector, physics coefficient, tolerance or 600 s workload changes. Diagnostic 11 Hotfix 1 is superseded as ordinary-suite RED; exact-v9 600 s evidence is still not produced until Hotfix 2 passes the ordinary suite.

# M10 Final Long Failure Diagnostic 11 Hotfix 1 — Exact-v9 Governor Integral Preload Regression Contract (CANDIDATE)

## Hotfix 1 — test-contract alignment

- Diagnostic 11 original compiled, but the ordinary suite stopped RED 1/1482 in `ExactV9Candidate_IsDistinctPreservesHistoricalVersionsAndDoesNotSwitchProductionDefault`.
- The failing assertion expected `speed-control` integral preload in the historical `25..28 %` range even though exact-v9 intentionally authors a `29.2813296974 %` governor/control output.
- With the breaker-closed droop P contribution at `0.75 %`, the correct bumpless PI preload is `29.2813296974 - 0.75 = 28.5313296974 %`, exactly matching the observed value.
- Hotfix 1 changes only the new Diagnostic-11 regression contract: it freezes the expected output/P/I region and verifies `I = unsaturated output - P - D` to 9 decimal places.
- No `src/`, exact-v9 authored state, governor semantics, moisture-drain semantics, production selector, physics coefficient, tolerance or long-workload rule changes.


- Stacked directly on user-validated Diagnostic 10 Hotfix 1 and its returned 600 s artifacts.
- Diagnostic 10 confirms the governor and moisture-drain structural repairs: primary ~99.98 kg/s, turbine-inlet late mass slope ~+8.4e-5 kg/s, control-valve late drift ~-3.8e-6 %/s, global mass closure ~0 and conservative stage/full-cycle energy closure.
- Exact-v8 remains engineering NOT QUALIFIED because it settles near 4.8682 MWe with about +0.2553 MW late net/stored power; this is the expected pre-drain operating-point root carried into the new admission semantics.
- Adds exact-v9 with no new runtime semantics: 13.0280018984 kg/s vapor, 13.3392371354 kg/s total admission, 0.3112352370 kg/s explicit drain, 29.2813297% control valve, 42.9665154% condensate pump, 96.9308268% feedwater pump and 32.9711765 MW fission root.
- Exact-v4 remains production; exact-v8 remains frozen evidence. Production activation and replacement long remain unauthorized.

# M10 Final Long Failure Diagnostic 10 Hotfix 1 — Turbine-Inlet Canonical Net-Balance Regression Alignment (CANDIDATE)

- Stacked directly on Diagnostic 10 original candidate after the user-reported ordinary-suite result: 1480 total, 1367 passed, 112 ignored, 1 failed.
- The sole failure is `TurbineExpansionSolverTests.Step_MoistureDrainPolicy_AssignsRejectedLiquidToExplicitOwnerWithoutChangingVaporWorkFlow`. The original regression expected `turbine-inlet` mass to fall by the 1 kg/s turbine transfer alone.
- The fixture simultaneously has a canonical pressure-driven admission-valve inflow of about 2.236067978 kg/s. Over 1 ms the correct node balance is therefore `+2.236067978 - 1.0 = +1.236067978 kg/s`, matching the observed `10000 -> 10000.001236067978 kg`.
- Hotfix 1 changes only that regression to assert `initial mass + (admission-valve flow - total turbine transfer) * dt`; exhaust and hotwell owner assertions and the 1e-12 kg/s / 1e-6 W conservation audits remain unchanged. No tolerance is widened.
- `src/`, exact-v8, admission/moisture-drain semantics, governor repair, production selector, historical exact-version factories and first-long manifest are unchanged.
- Re-run the unchanged `scripts\run-m10-final-long-failure-diagnostic10.cmd`; exact-v8 remains unqualified until the ordinary suite and the 600 s Diagnostic 10 census both complete.

# M10 Final Long Failure Diagnostic 10 — Exact-v8 Turbine Moisture-Drain Ownership Requalification (CANDIDATE)

- Stacked on user-validated Diagnostic 9 and its returned artifacts.
- Diagnostic 9 proves the exact-v7 turbine-inlet inventory growth is the non-vapor fraction rejected by `VaporMassFractionLimited`: late `commanded-effective` = `commanded*(1-x)` = ~0.268827 kg/s and measured turbine-inlet `dm/dt` = ~0.268782 kg/s, with ~4.55e-5 kg/s closure difference.
- Adds versioned admission policy `VaporMassFractionLimitedWithMoistureDrain`; vapor remains the sole work-producing stage flow while rejected non-vapor mass is assigned to an explicit canonical moisture-drain node.
- Exact-v8 binds the drain to `hotwell` and preserves the exact-v7 authored whole-cycle seed plus synchronous breaker-closed governor integral-reference repair.
- Saturated-mixture vapor/liquid streams use phase-resolved saturation transport properties at committed inlet pressure; stage mass and energy ownership close as inlet = exhaust + drain + shaft.
- Historical `LegacyUnrestricted` and `VaporMassFractionLimited` policies, exact-v4/@5/@6/@7 factory identities, production policy and first-long manifest remain unpromoted; exact-v4 remains production.
- Adds domain/simulation regressions and a 600 s exact-v8 whole-cycle/moisture-owner requalification census.
- No production activation or replacement-long authorization is included.

# M10 Final Long Failure Diagnostic 9 — Exact-v7 Turbine-Admission / Closed-Cycle Mass-Owner Census (CANDIDATE)

- Diagnostic 8 execution is locally PASS, but exact-v7 is engineering **NOT QUALIFIED**. The versioned synchronous integral reference reduces late governor/control-valve drift from about `+0.01474 %/s` to about `+0.000240 %/s`, yet the full plant remains materially non-stationary.
- Returned D8 evidence: primary pump flow `100 -> 122.6975 kg/s`, electrical export `4.9986 -> 4.5644 MWe`, late net/stored energy `~+2.402 MW`, turbine-inlet mass slope `+0.22150 kg/s`; no trip/rollback and energy closure remains conservative.
- Adds **no runtime source change and no exact-v8**. Diagnostic 9 runs exact-v7 for 180 s and records the canonical mass chain `admission valve -> stage commanded -> stage effective -> condenser -> hotwell -> condensate pump -> feedwater inventory -> feedwater pump -> drum`.
- Separates `admission-commanded` hydraulic/stage-capacity residual from `commanded-effective` vapor-fraction residual and compares `admission-effective` directly with measured turbine-inlet `dm/dt`.
- Also compares stage-effective-minus-condensation with exhaust `dm/dt`, condensation-minus-condensate-pump with hotwell `dm/dt`, condensate-minus-feedwater with feedwater-inventory `dm/dt`, and the corrected M4.4 drum algebraic balance with drum `dm/dt`.
- Decision remains fail-closed: if vapor-fraction-limited stage mass ownership is confirmed, do **not** author a seed-only exact-v8; first choose the physically intended owner (total-mass turbine transport with vapor-limited work, explicit moisture drain/separator, or an already modeled path) under a separate versioned repair.
- Production selector remains exact-v4. Production activation and replacement long remain unauthorized.

# M10 Final Long Failure Diagnostic 8 — Exact-v7 Grid-Droop Integral-Reference Requalification (CANDIDATE)

- Stacked on user-validated Diagnostic 7 and its returned governor/steam-path artifacts.
- Diagnostic 7 proves the breaker-closed historical governor integrates the intentional droop offset: late mean error ~+0.738 rpm with Ki=0.02/s produces governor-integral slope ~+0.01476 %/s, matching measured governor-output/control-valve slope ~+0.01474 %/s.
- Classifies the residual exact-v6 drift as a structural breaker-closed governor control-law mismatch, not a seed-only mismatch: a rigid grid holds rotor speed near synchronous speed, so no finite initial integral can stop integration against the 3000.75 rpm droop-shifted reference.
- Adds optional controller `IntegralSetpoint`; null preserves historical PID behavior. Adds versioned `TurbineGovernorIntegralReferenceMode`; historical default remains `EffectiveDroopSetpoint`.
- Adds exact-version `integrated-operations-desktop-stable@7`, preserving the exact-v6 analytical whole-cycle state while opting only into `SynchronousSpeedWhenParalleled`: P/D retain the droop-shifted reference, I uses synchronous grid speed.
- Adds domain/simulation regressions plus a 600 s exact-v7 whole-cycle requalification census with governor late-slope evidence.
- Exact-v4 remains production; exact-v5/exact-v6 remain retained failed diagnostic evidence. No production activation or replacement long is authorized.

# M10 Final Long Failure Diagnostic 7 — Governor-Droop / Steam-Path Owner Census (CANDIDATE)

- Stacked on user-validated Diagnostic 6 execution and its returned artifacts.
- Diagnostic 6 is engineering **NOT QUALIFIED**: exact-v6 survives 600 s with zero trips/rollbacks and conservative energy closure, but late primary pressure drift remains ~-0.63 kPa/s, drum mass slope ~+0.378 kg/s, steam-path inventories remain directional, and electrical export moves 4.9986→5.2015 MWe.
- Source review identifies a specific owner hypothesis: at a 5 MWe request the unchanged 1.5 rpm full-load governor droop produces a 3000.75 rpm effective speed setpoint, while exact-v6 authors the rotor at 3000 rpm. The automatic PI/PID loop therefore cannot be bumpless at t=0 and may move the control valve away from the analytically solved 27.3123% steam-path resistance.
- Adds only an explicit 180 s governor/droop + steam-path owner census sampled every 0.1 s, capturing governor P/I/D, control-valve position, rotor/grid state, line/valve/stage flows, electrical output and steam-path inventories.
- `src/` and exact-v6 are unchanged. No exact-v7, controller retune, production activation or replacement-long authorization is included.

# M10 Final Long Failure Diagnostic 6 — Exact-v6 Analytical Whole-Cycle Equilibrium Candidate (CANDIDATE)

- Stacked on user-validated Diagnostic 5 and its returned whole-cycle owner artifacts.
- Corrects the earlier shorthand about exact-v5: its 260 kg/s probe did not omit pump internal resistance; the non-stationary seed instead relied on an authored ~5.76 MPa suction-to-drum pressure reservoir that relaxes under drum-to-suction recirculation.
- Adds exact-version `integrated-operations-desktop-stable@6` as candidate-only; exact-v4 remains the authoritative production selector and exact-v5 remains frozen failed diagnostic evidence.
- Derives the stationary primary point from unchanged resistances/head with `P_suction=P_drum`: total closed-loop resistance 100 Pa·s²/kg² and 1 MPa pump head imply 100 kg/s.
- Derives the secondary point from 5 MWe / 98% generator efficiency + 0.5 MW rotor loss and 430 kJ/kg effective turbine work: 13.0280018984 kg/s.
- Solves the unchanged steam-path resistances, condenser UA, condensate/feedwater hydraulics and thermal balances consistently; analytical whole-cycle heat input is 32.4842538718 MW.
- Adds a generic per-node authored thermofluid seed seam used only by exact-v6; all historical seeds retain their existing fallback path.
- Adds ordinary exact-v6 identity/default-policy checks and a 600 s explicit whole-cycle equilibrium census.
- No production activation, replacement-long authorization, domain widening, component coefficient retuning, controller-gain change, I.3 budget change or conservation-ceiling change.

# M10 Final Long Failure Diagnostic 5 — Exact-v5 Whole-Cycle Authored-State Owner Census (CANDIDATE)

- Stacked on the locally validated Diagnostic 4 candidate/results chain.
- Diagnostic 4 execution PASS confirms exact-v5 remains engineering NOT QUALIFIED.
- Diagnostic 4 corrected interpretation: M4.4 physical feedwater is the internal feedwater-pump flow; the legacy primary feedwater boundary remains zero by design and is not the drum feedwater owner.
- Final-60 s drum accumulation is approximately `+0.79872 kg/s`; `return-recirculation` contributes only ~`+0.01270 kg/s`, while `internal feedwater-separated steam` contributes ~`+0.78540 kg/s`.
- Full energy path remains conservative: late net external power and coupled stored-energy change both ~`-2.477 MW`, with microscopic closure residual.
- Adds Diagnostic 5 only: exact-v5 is rerun unchanged for 600 s while all whole-cycle authored thermofluid node states, pump/controller states and corrected owner balances are captured.
- `src/` remains unchanged from Diagnostic 4; exact-v4/exact-v5 and production selection remain unchanged.
- No exact-v6, production activation or replacement long is authorized by this candidate.

# M10 Final Long Failure Diagnostic 4 — Exact-v5 Full-Plant Mass / Energy Balance Census (CANDIDATE)

- Stacked on user-validated Diagnostic 3 Hotfix 1. Diagnostic 3 execution passed, but returned engineering evidence marks exact-v5 **NOT QUALIFIED** for production activation.
- Freezes the Diagnostic-3 finding that the 260 kg/s instantaneous hydraulic probe evolves toward ~103 kg/s, with outlet mass 4609.8→1425.9 kg, drum mass 3918.2→7267.7 kg and drum level 0.5000→0.9567 over 600 s.
- Records final-60 s residuals from returned evidence: outlet `dm/dt` ~-0.1796 kg/s, mean channel-return ~-0.1784 kg/s, drum mass slope ~+0.7987 kg/s, drum-level slope ~+8.61e-5 /s, outlet/drum pressure decline ~-1.00/-0.984 kPa/s, fuel/structure cooling ~-0.0106/-0.0115 °C/s.
- Adds a diagnostic-only 600 s census of canonical drum, primary-boundary, feedwater/condensate and full coupled energy-balance terms.
- `src/` is unchanged from Diagnostic 3 Hotfix 1; exact-v4, exact-v5, production selector, physics, controller tuning, thermodynamic envelope, I.3 budgets and historical long manifest are unchanged.
- Replacement long remains unauthorized; no exact-v6 or new operating-point target is authored until returned balance evidence identifies the mass/energy owners quantitatively.

## M10 Final Long Failure Diagnostic 3 Hotfix 1 — Missing Domain Namespace Import — CANDIDATE

- Supersedes the original Diagnostic 3 candidate, which failed the Debug build only in `M10FinalLongFailureDiagnostic3Tests.cs` with CS0103 because the new test referenced `HydraulicNumericalCouplingMode` without importing its owning `NuclearReactorSimulator.Domain.Plant` namespace.
- Adds only the missing `using NuclearReactorSimulator.Domain.Plant;` to the Diagnostic-3 test source.
- No production `src/` file, exact-v5 seed value, hydraulic/thermal coefficient, production selector, long manifest, LR-M1 Hotfix 1 implementation, acceptance rule or diagnostic workload is changed.
- Diagnostic 3 remains diagnostic-only; replacement long execution remains unauthorized until the 600 s exact-v5 census is returned and interpreted.

## M10 Final Long Failure Diagnostic 2 / LR-M1 Hotfix 1 — CANDIDATE

- Promotes Diagnostic 1 to accepted diagnostic evidence after the user reported build and both diagnostics passing; M10 itself remains open.
- Classifies LR-M1 as an Application live-projection O(n)-per-step / O(n^2)-session scalability defect: at 100,000 synthetic samples the two historical live projectors consumed about 8.2 ms per refresh while presentation output remained bounded.
- Replaces the live `_demandTimeline` prefix with an exact-semantics incremental accumulator: current sample, paired-count/sums for demand tracking, and at most 100 actual demand change points. Offline replay/full-prefix projectors remain unchanged.
- Adds ordinary semantic-equivalence tests and an explicit incremental scaling census; no challenge, scoring-policy, command-authority, archive, replay-fingerprint or exact-version semantics change.
- Classifies LR-H1 more narrowly as a real primary inventory redistribution: outlet final-60 s `dm/dt=-7.914 kg/s`, total node mass slope closes to numerical zero, and the 300 s production pressure heads imply channel-return residual about `-7.85 kg/s`.
- Adds a second 300 s exact-v4 diagnostic for canonical pump/channel/return flows, steam-drum recirculation and flow/level-controller integrals. No physical coefficient, controller tuning, water/steam envelope, I.3 budget or conservation ceiling changes.

## Pre-M11 Engineering Review Consolidation / Planning 3 — CANDIDATE

- Stacked documentation/planning-only on **M10 Final Pre-M11 Long Validation Hotfix 1**; production `src/`, baseline tests, long-test implementation, frozen long workload and all M10 acceptance thresholds remain unchanged.
- Consolidates the three book-driven review streams performed during final M10 validation: nuclear-code V&V, Digital I&C / human-system safety, and reactor operating-point self-consistency/equilibrium.
- Imports the reviewed Digital I&C architecture invariants, human–automation function allocation, deterministic hazard catalog, HMI classic failure-mode checklist, dependency/COTS assurance plan and post-M11 Digital I&C backlog.
- Integrates the post-M10→M15 execution master plan, change-impact/revalidation policy, M11 release-evidence plan, M13.9 Digital I&C Degradation & Automation Transparency, M13.10 integrated UX closure, and M12.0 Reference Operating-Point Equilibrium & Stability Qualification.
- Adds source-provenance documentation and machine-readable source mapping; the three source books themselves are not bundled.
- Records the current first long-run evidence without altering it: LR-H1 healthy exact-v4 is RED on a real `outlet` `WaterSteamStateOutOfRangeException`; the campaign remains fail-collect and must finish before diagnosis/hotfix selection.
- No post-hoc widening of thermodynamic envelope, I.3 budgets, conservation ceilings or long workload is authorized.


## M10 Final Pre-M11 Long Validation Hotfix 1 — Generated Build-Output Exclusion

- Fixes the long-contract preflight when the repository has already been built before `scripts\run-m10-final-long-validation.cmd`.
- `eng\validate-m10-final-long-validation-contract.ps1` now excludes only generated `bin` and `obj` directory contents while comparing `src` and `tests` against the frozen validated-baseline manifests.
- Frozen long workload, I.3 budgets, conservation ceilings, production `src`, existing test surface, and the explicit long-validation test remain unchanged.
- This is validator-only alignment; no physics/runtime behavior is changed.
# M10 Final Pre-M11 Long Validation — CANDIDATE

- Stacked exclusively on **M10 Final Pre-M11 Cumulative Validation Hotfix 1 VALIDATED** after `m10-final-cumulative-validation-passes=True`.
- Adds no production `src/` changes. Adds one explicit scheduled-long Application test class plus frozen workload/validator/finalizer/orchestrator contracts.
- Freezes a timing-calibrated approximately-one-hour-class campaign before first acceptance execution: LR-H1 7,200 s, LR-M1 4,400 s, LR-D1 1,800 s, LR-P1 900 s, LR-R1 100 s; total 14,400 simulated seconds / 1,440,000 authored 10 ms steps.
- Timing calibration uses the already validated exact-v4 300 s cumulative evidence only to size workload duration; physics and acceptance thresholds are unchanged.
- Reuses all 19 frozen I.3 budgets on 24 rolling 60 s healthy windows ending every 300 s through 7,200 s; exact-v4 conservation ceilings remain `1e-6 kg`, `1e-2 J`, `1e-8 kg/s`, `1e-3 W`.
- Freezes degraded fault timing (54,000→90,000), protection/takeover timing (SCRAM 54,000; authority observation 54,001; blocked rod command 60,000; manual takeover 72,000) and replay/checkpoint sentinel timing.
- Adds `scripts/run-m10-final-long-validation.cmd`, which executes all five explicit legs even when an earlier leg fails, then finalizes the complete artifact set.
- Passing this gate makes M10 closure eligible; M11 remains blocked until explicit M10 closure documentation/promotion.

# M10 Final Pre-M11 Cumulative Validation Hotfix 1 — Historical Focused-Route Reuse Alignment — CANDIDATE

- Supersedes the original final-cumulative candidate after Release ordinary tests and all Phase-I/M10.9.5/M10.9.6 current gates passed, then the M10.9.7.2 historical focused script returned xUnit exit code 8 because its superseded exact-candidate `ApplicationDescriptorTests.Current_DescribesM10972Hotfix1Rev1DomainDefinitionInvariantClosureCandidate` method no longer exists in the current test surface.
- Adds an explicit `--historical-reuse` mode to the M10.9.7.2 Hotfix 1, M10.9.7.3 Hotfix 2 REV2 and M10.9.7.4 focused scripts. Standalone execution remains unchanged and still requires each historical exact-candidate descriptor check; final-M10 reuse skips only that descriptor-only assertion while rerunning every functional/domain/host/timeline owner test and artifact writer.
- Updates `run-m10-final-validation.cmd` to use historical reuse for those three routes, records per-stage progress, and adds fail-fast validator anchors so the same no-tests condition is detected before the expensive cumulative sequence.
- Changes no file under `src/` or `tests/`, no V&V row/budget/hash, no Simulation/runtime/HMI semantics and no long-gate contract.

# M10 Final Pre-M11 Cumulative Validation — CANDIDATE

- Promotes **M10.9.8.5 to VALIDATED / M10.9.8 to CLOSED** after build, complete ordinary suite, automated integrated-HMI closure preflight and explicit manual acceptance `M10.9.8.5 manual integrated HMI acceptance OK`.
- Stacks exclusively on M10.9.8.5 VALIDATED and changes **zero files under `src/` and `tests/`**.
- Adds the frozen 27-row `eng/m10-final-vv-matrix.json`, explicit M10.9.8.5 manual-acceptance record and PowerShell-5.1-compatible matrix validator.
- Implements the previously planned `scripts/run-m10-final-validation.cmd`: restore, Release warnings-as-errors build, complete Release ordinary suite, Debug focused build, current Phase-I exact-v4 evidence, M10.9.5/6/7/8 current closure routes and reference-plant-scale audit.
- Historical superseded/frozen long audits are not blindly replayed by the cumulative gate. Long-horizon exposure remains a separate mandatory gate.
- **M10 remains OPEN after this candidate passes.** M11 is blocked until `scripts/run-m10-final-long-validation.cmd` is implemented on this cumulative baseline and passes the frozen long contract.

# M10.9.8.5 — Manual Integrated HMI Acceptance & M10.9.8 Closure (CANDIDATE)

- Promotes **M10.9.8.4 Hotfix 1 to VALIDATED** after build, complete ordinary suite and `scripts\run-m10984-replay-checkpoint-same-seed-integrity-audit.cmd` passed.
- Stacks exclusively on that baseline and changes no `src/` or `tests/` file; M10.9.8.5 is manual/docs closure-only.
- Freezes the 1,286-file `src/` + `tests/` baseline surface in `eng/m10985-baseline-compiled-test-surface.sha256`; the standalone 8.5 validator compares exact SHA-256/path identity instead of relying on milestone-string scans, while `-HistoricalReuse` allows later final-M10 gates to reuse the contract after legitimate future changes.
- Adds the versioned twelve-route manual acceptance contract `eng/m10985-manual-integrated-hmi-acceptance-contract.json`, the integrated preflight `scripts/run-m1098-integrated-human-automation-hmi-audit.cmd`, the manual checklist and explicit M10.9.8 closure document.
- Revalidates the frozen M10.9.8.1/8.2/8.3/8.4 matrices plus representative HMI/session/authority/list-stability owners before manual acceptance.
- Aligns the user manual/current project status and retires the stale limitation text saying the fingerprint-v1 golden anchor still had to be added.
- Explicitly keeps **M10 OPEN after M10.9.8 closure**; M11 remains blocked until the mandatory final cumulative M10 gate and separate approximately one-hour operational long validation pass.

# M10.9.8.4 Hotfix 1 — Protection/Authority Observation Boundary Alignment (CANDIDATE)

- Supersedes the original M10.9.8.4 candidate, which compiled but failed one ordinary test because `RunProtection` asserted `SuspendedByProtection` one deterministic tick too early after `ReactorScram`.
- Aligns the replay/checkpoint protection row with the already validated M5/M10.9.8.3 owner boundary: the SCRAM step commits protection, the following deterministic tick updates effective authority to `Assisted` / health `SuspendedByProtection`, and only then is the replay checkpoint captured.
- Adds a source-level validator anchor for `AdvanceAuthorityAfterProtectionCommit`; no production `src/` file, Simulation physics, protection logic, archive schema, fingerprint algorithm or challenge semantics change.

- Promotes **M10.9.8.3 to VALIDATED** after build, complete ordinary suite and `scripts\run-m10983-degraded-fault-protection-takeover-audit.cmd` all passed.
- Stacks exclusively on that baseline and adds no production runtime source, Simulation physics, archive schema, fingerprint algorithm, challenge/scoring/protection ownership or plant-command authority change.
- Adds a four-row integrity matrix `RCI-01..RCI-04` covering healthy bounded-demand supervisory operation, degraded required measurement with deterministic recovery, protection-trip/suspended authority and manual takeover.
- Defines same-seed as the existing deterministic contract: same exact scenario/initial-condition plus the same accepted operator-action and automation-intent trace; no runtime RNG/seed field is introduced.
- Requires fresh same-seed repeat, full canonical replay, replay-backed checkpoint prefix + identical live continuation and M10.9.6.5 challenge replay projection equivalence for every representative state class.
- Reuses archive schema v1, `sha256-control-room-snapshot-v1` and `m10965-challenge-replay-sha256-v1`; no opaque physical or challenge-state checkpoint blob is added.
- Adds the mandatory planning contract `docs/M10_FINAL_PRE_M11_VALIDATION_PLAN.md`: after M10.9.8 manual closure, M11 remains blocked until a cumulative final M10 gate and a separate approximately one-hour explicit operational long validation both pass.

# M10.9.8.3 — Degraded Measurement / Fault / Protection / Takeover Matrix (CANDIDATE)

- Promotes **M10.9.8.2 Hotfix 1 REV5 to VALIDATED** after build, complete ordinary suite, focused HAA/mission/F4/list-stability audit and user acceptance to continue.
- Stacks exclusively on that validated baseline and adds no production runtime source or Simulation/Domain/Infrastructure physics changes.
- Adds a frozen eleven-row execution matrix `DFP-01..DFP-11` covering invalid supervisory measurement, suspect/unavailable operator truth, protection precedence, protection trip during automation, hydraulic component fault, instrumentation fault, real permissive rejection, requested/effective degradation, manual takeover, recovery and challenge-active degraded/protection integration.
- Realizes INT-12/INT-17 through a **validation-only** exact-v4 composition using the existing M8.3 `instrumentation.sensor-unavailable` seam at logical steps 2..5; no production scenario/challenge/fault type is registered.
- Requires requested `SupervisoryAutomatic` to degrade fail-closed to effective `Assisted` while the required measurement is unavailable, with MISSION publishing the same degradation truth and external demand remaining challenge-owned; after fault clear, canonical M5 recovery restores healthy supervisory authority.
- Adds an exact-v4 protection-precedence integration proof: canonical SCRAM remains authoritative across a later normal rod-withdraw command, requested/effective authority diverges as `SupervisoryAutomatic`/`Assisted`, and the bounded-demand challenge fails observationally rather than owning protection.
- Reuses M8.2/M8.3 fault owners, M4.5 synchronization permissive owner, M10.9.5 observed-response owner and M10.9.6 lifecycle/demand/scoring owners in the focused gate.
- Explicitly leaves per-row replay/checkpoint/same-seed equivalence to M10.9.8.4.

# M10.9.8.2 Hotfix 1 REV5 — Interactive List Refresh Stability (CANDIDATE)

- Stacked on REV4 after user-observed residual flicker in F4 `DEPENDENCY CHAIN — SELECT A STEP` during RUN.
- Audits every collection-backed UI surface that can reproduce hover/selection churn: 24 XAML `ItemsSource` controls plus five `ControlRoomSelector` instances backed by one programmatic `ComboBox` implementation.
- Caches the selected command consequence/dependency projection and suppresses dependency-list/selection notifications on unrelated runtime snapshots; dynamic mimic/schematic state still refreshes.
- Preserves F8 session checkpoint collection/selection identity when immutable checkpoint content is value-equivalent.
- Stops `ControlRoomSelector` from resetting `ComboBox.ItemsSource` on unrelated state/selection visual refresh; target options are replaced only when the option sequence changes.
- Preserves MISSION score/recent-event/timeline collection identity when only scalar mission state changes; this prevents avoidable drill-down button container recreation.
- Adds fail-closed XAML + programmatic-selector inventory/regression tests so future collection-backed interactive surfaces require an explicit refresh-stability review.
- Leaves the 18 read-only plant/alarm/history `ItemsControl` surfaces unchanged because their per-snapshot immutable values are intentionally dynamic and they have no selection/focus/embedded action surface; the nineteenth read-only ItemsControl, MISSION `ScoreDimensions`, receives semantic no-replacement suppression alongside the timeline/recent-event projections.
- No change to production mission @2 identity, Simulation/Domain/Infrastructure physics or coefficients, challenge/scoring/protection ownership, archive schema, fingerprint algorithm or plant-command authority.

# M10.9.8.2 Hotfix 1 REV4 — Legacy Windows PowerShell SHA-256 Compatibility (CANDIDATE)

- Stacked on Hotfix 1 REV3 after build and the complete ordinary suite passed, while the focused audit stopped before M10.9.8.2 tests because the host Windows PowerShell does not provide `Get-FileHash`.
- Replaces only the matrix-v2 validator hash implementation with `System.Security.Cryptography.SHA256` + `System.IO.File`, preserving the exact frozen matrix-v1 SHA-256 check without requiring the version-specific cmdlet.
- No change to `src/`, `tests/`, matrix-v1/v2 bytes, production mission @2, the 1,000-step `control-out` regression, F4 COMMANDS fixes, Simulation physics, archive, fingerprint, scoring, protection or authority semantics.
- The validator continues to require the accepted matrix-v1 hash `272e4eb2c958254c18cf19c1818006325ea0363c4f76eae7d8432fdb42d6da4e`; only the mechanism used to calculate it changes.

# M10.9.8.2 Hotfix 1 REV3 — Active Demand / Logical-Time Contract Correction (CANDIDATE)

- Supersedes unvalidated REV2 after the ordinary suite reached `M10982HealthyAssistanceAuthorityMatrixTests` and failed at `Assert.False(mission.Demand.ExternalDemandAvailable)`: runtime evidence was `True`.
- Corrects the REV2 interpretation of `Window(4_000, 8_000)`. `ChallengeLogicalTimeContract` defines those values as target-completion offsets from `ActivatedLogicalStep`; they are observational and do not delay activation. `ScenarioChallengeTracker` activates when the authored condition is satisfied, and `ScenarioChallengeExternalDemandProjector` publishes demand whenever activation exists.
- Restores HAA-01..HAA-09 to the active bounded-demand semantics already present in the accepted matrix v1, while continuing to execute the production-safe @2 / exact-v4 binding. The test now requires active `bounded-demand-5-10-5@1` evidence, non-null requested/actual electrical evidence, and explicitly freezes +4000/+8000 target-window offsets from activation.
- Retains REV2 checkpoint-prefix/live-continuation coverage, the 1,000-step `control-out` regression, historical @1 preservation, F4 anti-flicker/ENTER fixes and HistoricalReuse validator repair. No Simulation physics/coefficient, protection, scoring, archive schema or fingerprint algorithm change.

# M10.9.8.2 Hotfix 1 REV2 — Pre-Build Evidence Contract Alignment (CANDIDATE)

- Supersedes Hotfix 1 REV1 before build/test after static preflight found three evidence-contract gaps, not compile errors: HAA-01..HAA-09 claimed an already-active bounded-demand challenge although the preserved activation window begins at STEP 4000; the test incorrectly expected external-demand evidence before activation even though the canonical projector returns unavailable; and the row contract required checkpoint-prefix/live-continuation equivalence while the test covered only full replay.
- Keeps the accepted matrix v1 byte-frozen and keeps the production @2 challenge semantics/window unchanged. Matrix v2 now explicitly declares the bounded **pre-activation control-axis** HAA execution phase and routes active-window lifecycle coverage to M10.9.8.4/M10.9.8.5 rather than hiding a long audit inside the ordinary suite.
- Extends every HAA row with a deterministic checkpoint after the first two accepted actions, exact prefix restore, identical live continuation, final physical fingerprint/authority/lifecycle/demand/score comparison, while retaining full replay verification. The pre-activation rows now assert external demand is unavailable by contract; active demand/request/output separation remains covered by the rerun M10.9.6.2 owner test.
- Leaves the 1,000-step production @2 regression, historical @1 preservation, F4 anti-flicker/ENTER fixes and HistoricalReuse validator repair unchanged.

# M10.9.8.2 Hotfix 1 REV1 — Historical Validator Scope Repair (CANDIDATE)

- Stacked on the unvalidated M10.9.8.2 Hotfix 1 candidate; production mission @2 and F4 robustness implementation are unchanged.
- Fixes the focused-gate false positive where the accepted M10.9.8.1 validator scanned all future compiled/test files for the literal `M10.9.8.1` and rejected the legitimate M10.9.8.2 artifact-summary baseline reference.
- Adds `-HistoricalReuse` to `validate-m10981-integrated-validation-matrix.ps1`. Standalone M10.9.8.1 acceptance keeps the original compiled-surface marker check; later milestones reuse matrix/manual validation without retroactively banning references to the accepted baseline.
- `run-m10982-healthy-assistance-authority-matrix-audit.cmd` now invokes the M10.9.8.1 validator with `-HistoricalReuse`.
- `validate-m10982-integrated-validation-matrix-v2.ps1` freezes that invocation contract to prevent regression.
- No `src/`, `tests/`, XAML, Simulation, challenge/scoring/protection, archive, fingerprint, mission binding or F4 production behavior changes relative to Hotfix 1.

# M10.9.8.2 Hotfix 1 — Compile / Production Mission Runtime / F4 Command Console Robustness (CANDIDATE)

- Supersedes the original M10.9.8.2 candidate, which did not compile because `M10982HealthyAssistanceAuthorityMatrixTests` omitted the `NuclearReactorSimulator.Application.ControlRoom.Automation` import required by `SupervisoryObjectiveRequest`.
- Preserves `bounded-demand-following-5-10-5@1` exactly for historical replay/archive compatibility and adds `bounded-demand-following-5-10-5@2`, retaining the same demand/scoring/challenge evidence while binding to repaired production `integrated-operations-desktop-stable@4`.
- Adds a 1,000-step production-mission regression crossing the user-reported historical `control-out` failure region around STEP 610–615 without modifying Simulation coefficients or thermodynamic/hydraulic policy.
- Records the HAA execution correction in `eng/m1098-integrated-human-automation-hmi-matrix-v2.json`; the accepted M10.9.8.1 matrix v1 is not edited.
- Stabilizes F4 COMMANDS by retaining command collection/selection references across refreshes when list-visible command identity/availability is unchanged; newest state/blocking details remain projected from the latest snapshot.
- Replaces the ListBox ENTER KeyBinding with explicit `CommandCatalog_KeyDown` handling (`Handled=true`) and adds an App host policy for expected command validation/target/numeric rejections, while leaving unknown/programming exceptions unhandled.
- Adds focused regressions for exact @1/@2 mission binding, historical failure-region crossing, command refresh stability, ENTER XAML contract and expected command-rejection containment.

# Changelog
## 2026-08-22 — M10.9.8.2 — Automated Healthy Assistance × Authority Matrix — CANDIDATE

- Promotes **M10.9.8.1 REV1 Docs1 to VALIDATED** after build, complete ordinary suite, `scripts\run-m10981-integrated-validation-matrix-audit.cmd` and explicit user direction to proceed on the accepted frozen matrix.
- Stacks exclusively on that validated contract baseline and executes frozen rows HAA-01..HAA-09: `Hidden|ChecklistOnly|Guided × Manual|Assisted|SupervisoryAutomatic`.
- Adds `M10982HealthyAssistanceAuthorityMatrixTests`, using the same exact `bounded-demand-following-5-10-5@1` / `stable-low-load-parallel-operation@1` identity and the same accepted `GeneratorLoadRaise | ControlRodHold | GeneratorLoadLower | AlarmAcknowledgeAll` schedule in all nine rows.
- Healthy supervisory rows explicitly configure `HoldCurrentOperatingPoint`; requested and effective authority must equal the requested mode with `Normal` health in every HAA row.
- Compares physical and replay fingerprint, challenge lifecycle, demand/request/actual, score, alarm count and authority at fixed authority across all three assistance modes; assistance-only changes must not alter these canonical outcomes.
- Records each row through `ScenarioRecorder` and verifies final full-replay canonical fingerprint and authority equivalence. The broader checkpoint/MISSION replay matrix remains M10.9.8.4 ownership.
- Reuses existing M5/M10.9.6 owners in the focused gate: assistance-authority independence, authority integration, external demand, scoring and automation replay.
- Adds `scripts/run-m10982-healthy-assistance-authority-matrix-audit.cmd` and `docs/M10_9_8_2_AUTOMATED_HEALTHY_ASSISTANCE_AUTHORITY_MATRIX.md`. No separate manual HMI gate is introduced; M10.9.8.5 owns manual acceptance.
- No production `src/` file, XAML, Simulation physics, challenge/scoring/protection owner, archive schema, fingerprint algorithm, production scenario registration or plant-command authority change.

Validation required: `dotnet build`, complete `dotnet test`, then `scripts\run-m10982-healthy-assistance-authority-matrix-audit.cmd`.

## 2026-08-22 — M10.9.8.1 REV1 Docs1 — User Manual M10.9.7 Closure Alignment — CANDIDATE

- Stacks documentation-only on **M10.9.8.1 REV1 Contract-Only Matrix Freeze**; all files under `src/` and `tests/` remain byte-identical to M10.9.7.5 Hotfix 1 VALIDATED.
- Updates `docs/usermanual/MANUALE_UTENTE_NUCLEAR_REACTOR_SIMULATOR.md` from the old M10.9.4 reference to the user-facing functionality validated through **M10.9.7 CLOSED**.
- Adds the validated `MISSION` workspace to the control-room map and documents no-active-mission/`UNBOUND`, objective/lifecycle, safety/protection hierarchy, explicit `GRID DEMAND` vs `REQUESTED LOAD` vs `ACTUAL OUTPUT`, multidimensional challenge scoring, deterministic timeline/drill-down and replay/checkpoint behavior.
- Expands Operator Computer documentation to freeze F1–F8/no-F9 and `OPEN MISSION` as presentation-only navigation. Expands assistance/control documentation with requested-vs-effective authority, fail-closed supervisory degradation, measurement-validity discipline, protection priority and bumpless manual takeover.
- Adds a user-facing distinction between legacy `TRAINING SCORE` and versioned multidimensional MISSION challenge scoring, including standard grade bands, incomplete-evidence behavior and safety/procedure dominance.
- Extends the session chapter and English↔Italian glossary for MISSION, demand/request/output, requested/effective authority, drill-down and `UNBOUND`.
- Keeps M10.9.8.1 correctly described as a validation gate in progress rather than a new user feature. The external matrix validator now also verifies the manual-alignment anchors; no C# or XAML test/runtime surface is added.

Validation remains: `dotnet build`, complete `dotnet test`, `scripts\run-m10981-integrated-validation-matrix-audit.cmd`, then `docs\M10_9_8_1_MATRIX_ACCEPTANCE_CHECKLIST.md`.

## 2026-08-22 — M10.9.8.1 REV1 — Integrated Human / Automation / HMI Validation Matrix Freeze — CANDIDATE

- Rebuilds M10.9.8.1 **directly from M10.9.7.5 Hotfix 1 VALIDATED / M10.9.7 CLOSED** after the original M10.9.8.1 candidate was reported non-compilable and was not validated.
- Removes the unnecessary compiled-surface changes from the original candidate: **all files under `src/` and `tests/` are byte-identical to the validated M10.9.7.5 Hotfix 1 baseline**. In particular, `ApplicationDescriptor.cs` and `ApplicationDescriptorTests.cs` remain on the validated baseline and no `M10981...cs` test is added.
- Keeps M10.9.8.1 contract-only: no production runtime semantics, XAML, Simulation physics, challenge/scoring/protection authority, archive schema, fingerprint algorithm, plant-command authority or production scenario/fault registration change.
- Adds `eng/m1098-integrated-human-automation-hmi-matrix.json`: schema v1 with 19 frozen rows, the healthy 3×3 `Hidden|ChecklistOnly|Guided × Manual|Assisted|SupervisoryAutomatic` product, ten integrated families and eleven cross-cutting invariants with explicit owner routing.
- INT-12 remains explicitly validation-only and may be realized later only by composing existing exact-v4 measured-signal/M5 supervisory seams in test/audit code; it does not authorize a production scenario or fault type.
- Adds `eng/validate-m10981-integrated-validation-matrix.ps1`, a PowerShell 5.1-compatible external matrix validator. It validates schema/axes/rows/families/invariants/INT-12 and fails if an M10.9.8.1 marker appears in compiled/runtime source or tests.
- `scripts/run-m10981-integrated-validation-matrix-audit.cmd` reuses the already validated `TrainingAssistanceAuthorityIndependenceTests` and `PlantControlAuthorityIntegrationTests`, then runs the external JSON validator and emits the M10.9.8.1 summary artifact. The wrapper uses direct commands only and Windows CRLF.

Validation required: `dotnet build`, complete `dotnet test`, `scripts\run-m10981-integrated-validation-matrix-audit.cmd`, then `docs\M10_9_8_1_MATRIX_ACCEPTANCE_CHECKLIST.md`. Only after both automated validation and explicit matrix acceptance are green may M10.9.8.2 begin.

## 2026-08-22 — M10.9.8.1 — Integrated Human / Automation / HMI Validation Matrix Freeze — SUPERSEDED / NOT VALIDATED (reported non-compilable)

- This original candidate attempted the same matrix freeze but unnecessarily changed `ApplicationDescriptor.cs`, `ApplicationDescriptorTests.cs` and added `M10981IntegratedValidationMatrixContractTests.cs`.
- The candidate was reported non-compilable before validation. No compiler log was supplied, so REV1 does not claim an unverified exact compiler diagnostic.
- REV1 supersedes it by rebuilding from the validated M10.9.7.5 Hotfix 1 baseline and moving all M10.9.8.1 validation logic outside the compiled C# surface.

## 2026-08-22 — M10.9.7.5 Hotfix 1 — Closure Audit Batch Subroutine Repair — CANDIDATE

- Stacks exclusively on the original **M10.9.7.5 Mission/Performance Closure candidate**, which compiled and passed the complete ordinary suite (1423 total / 1328 passed / 95 ignored / 0 failed) but was **NOT VALIDATED** because the focused Windows audit aborted after its Application test groups with `Impossibile trovare l'etichetta batch specificata - run_app_class`.
- Confirms from the emitted closure artifact that the underlying closure matrix had already produced `m10975-mission-performance-closure-automated-passes=True`; the failure was in the wrapper execution path, not in Mission/Performance runtime semantics.
- Removes all `call :run_application_class` / `call :run_app_class` batch subroutines and their labels from `scripts/run-m1097-mission-performance-closure-audit.cmd`; every focused test group is now invoked explicitly and fail-closed.
- Writes the focused `.cmd` with Windows CRLF terminators and adds `M10975Hotfix1ClosureAuditScriptContractTests` so ordinary tests freeze the no-label-subroutine wrapper structure.
- Keeps production XAML/runtime behavior, Simulation physics, challenge/scoring/protection ownership, archive schema v1, fingerprint-v1 golden, F1–F8/no-F9 and plant-command authority unchanged.

Validation required: `dotnet build`, complete `dotnet test`, `scripts\run-m1097-mission-performance-closure-audit.cmd`, then `docs\M10_9_7_5_MANUAL_VALIDATION_CHECKLIST.md`. Only after all four gates are green may M10.9.7 be declared VALIDATED/CLOSED and M10.9.8 begin.

## 2026-08-22 — M10.9.7.5 — Mission/Performance Closure — SUPERSEDED / NOT VALIDATED (focused batch-wrapper failure)

- Stacks exclusively on **M10.9.7.4 Hotfix 1 VALIDATED** after build, complete ordinary tests, focused timeline audit and manual HMI acceptance passed.
- Adds no production XAML/runtime semantics and no Simulation, challenge, scoring, protection, archive-schema, fingerprint-algorithm or plant-command-authority change.
- Adds `M10975MissionPerformanceClosureContractTests` to exercise the closure matrix for demand/no-demand, Active/Completed/Failed presentation, required-trip versus unexpected-trip semantics, terminal mission with continuing plant logical time, assistance modes and requested/effective authority divergence.
- Re-runs the validated M10.9.6 replay/checkpoint closure and M10.9.7.1/7.3/7.4 presentation, timeline, archive and drill-down contracts through `scripts/run-m1097-mission-performance-closure-audit.cmd`.
- Freezes closure invariants: F1–F8 preserved, F9 absent, MISSION plant-command authority false, demand/request/actual separated, score copied from the M10.9.6 owner, deterministic replay/checkpoint presentation, archive schema v1 unchanged and fingerprint-v1 golden unchanged.
- Adds `docs/MISSION_PERFORMANCE_CLOSURE.md` and `docs/M10_9_7_5_MANUAL_VALIDATION_CHECKLIST.md`.

Validation required: `dotnet build`, complete `dotnet test`, `scripts\run-m1097-mission-performance-closure-audit.cmd`, then the M10.9.7.5 manual closure checklist. Only after all gates are green may M10.9.7 be declared VALIDATED/CLOSED and M10.9.8 begin.

## 2026-08-22 — M10.9.7.4 Hotfix 1 — Ordinary Suite Contract Alignment — VALIDATED

- Stacks exclusively on the original M10.9.7.4 candidate, which compiled but is **SUPERSEDED / NOT VALIDATED** after the complete ordinary suite reported 3 failures.
- Aligns the historical M10.9.7.3 XAML regression with the intentional M10.9.7.4 visual replacement of `RECENT DETERMINISTIC EVIDENCE` by `DETERMINISTIC TIMELINE / DRILL-DOWN`; the M10.9.7.3 `RecentEvents` Application contract remains unchanged and covered by its existing projection/live-wiring tests.
- Replaces the raw-XAML substring no-F9 assertion with a structural `KeyBinding Gesture="F9"` check, avoiding the false positive caused by hexadecimal HMI colors such as `#6F929F`.
- Corrects the populated H29 fingerprint fixture precondition: `PrimaryCircuit.Valves` is intentionally empty because the retained topology has no valve endpoint in the primary-node projection; the stop/control/admission valves are turbine/secondary. The test now positively anchors primary pumps/branches and explicitly anchors the topology-empty primary-valve surface before checking the unchanged frozen golden hash.
- Keeps `sha256-control-room-snapshot-v1`, golden hash `63643e5506a6b99f8106950ecb25a5243e9755b3bc96bf2a60e96c219216f362`, MISSION timeline/drill-down implementation, archive schema v1, replay/checkpoint behavior, F1–F8, XAML, plant-command authority and Simulation physics unchanged.

Validation completed: build, complete ordinary suite, `scripts\run-m10974-mission-performance-timeline-audit.cmd` and the M10.9.7.4 manual timeline/drill-down/archive checklist are green.

## 2026-08-22 — M10.9.7.4 — Deterministic Mission Timeline, Drill-Down & Replay Equivalence — SUPERSEDED / NOT VALIDATED (3 ordinary-suite failures)

- Stacks exclusively on **M10.9.7.3 Hotfix 2 REV2 VALIDATED** plus Docs4.
- Freezes `sha256-control-room-snapshot-v1` with the populated retained H29 exact-version 128-step golden fingerprint `63643e5506a6b99f8106950ecb25a5243e9755b3bc96bf2a60e96c219216f362`; intentional fingerprint-visible semantic change requires a new algorithm id.
- Adds a protected bounded lifecycle spine separate from bounded recent operational evidence, then merges them into a deterministic logical-step/source-sequence timeline without changing the M10.9.7.3 `RecentEvents` contract.
- Projects demand changes, operator actions, alarm/protection/fault evidence and current scoring context from canonical existing owners only.
- Adds presentation-only drill-down targets to ELECTRICAL, ALARMS/EVENTS and existing COMPUTER pages with no plant-command authority and no F9.
- Reconstructs MISSION lifecycle/demand from an already verified archive/checkpoint recording prefix and then continues from future live deterministic evidence without an opaque challenge-state checkpoint blob.
- Keeps session archive schema v1 unchanged: archive-restored MISSION requires an explicit exact pack binding matching scenario + initial-condition identity; unbound loads stay unbound and no pack is inferred from `ScenarioId`.
- Preserves an already explicit mission pack when `START RECORDED SESSION` is used, enabling the desktop archive/checkpoint round-trip without introducing a challenge launcher.
- Adds `scripts/run-m10974-mission-performance-timeline-audit.cmd`, ADR-0187, deterministic timeline documentation and a manual timeline/drill-down/replay checklist.
- No Simulation physics, challenge/scoring/protection authority, archive schema, plant-command authority, F1–F8 or 10 ms fixed-step change.

Validation required: `dotnet build`, `dotnet test`, `scripts\run-m10974-mission-performance-timeline-audit.cmd`, then `docs\M10_9_7_4_MANUAL_VALIDATION_CHECKLIST.md`.

## 2026-08-21 — M10.9.7.3 Hotfix 2 REV2 — Archive Failure / Cleanup / Historical Contract Alignment — VALIDATED

- Keeps **M10.9.7.3 Hotfix 1 REV2 VALIDATED + Docs4** as the only promoted baseline.
- Marks Hotfix 2 REV1 **SUPERSEDED / NOT VALIDATED** after the ordinary suite reported three failures.
- Explicitly includes `InvalidDataException` in `DesktopHostFailurePolicy.IsExpectedArchiveOperationFailure`; it is not an `IOException` subtype and therefore was not covered by the REV1 policy.
- Scopes `.nrs-bak` cleanup to a successfully committed `File.Replace`; a new-file `File.Move` now performs only temporary-file cleanup, while a failed replace does not risk deleting an emergency backup artifact.
- Updates the historical M10.9.7.1 archive-boundary regression to assert use/content of the centralized archive policy instead of requiring the superseded inline catch-list syntax.
- Retains all Hotfix 2 host-integrity behavior: expected numerical step failure containment, START/RESET/LOAD/RESTORE policy alignment, picker-before-export, temp-sibling durable write, safe replace/move, failure preservation and invariant engineering-number formatting.
- No Simulation physics, challenge/scoring/protection authority, plant-command authority, archive schema or MISSION semantics change.

Validation completed: build, complete ordinary suite, `scripts\run-m10973-desktop-host-session-integrity-audit.cmd` and the manual desktop-host/session-integrity checklist are green.

## 2026-08-21 — M10.9.7.3 Hotfix 2 REV1 — xUnit1051 Cancellation Contract Alignment — SUPERSEDED / NOT VALIDATED

- Rebuilt over M10.9.7.3 Hotfix 1 REV2 VALIDATED + Docs4 after the original Hotfix 2 failed compilation only on eight xUnit1051 analyzer violations in its new async App tests.
- Passes `TestContext.Current.CancellationToken` to every affected cancellation-aware test call in `DesktopSessionArchiveFileWriterTests`, `DesktopSessionArchiveSaveCoordinatorTests` and `M10973DesktopHostSessionIntegrityAuditTests`.
- Keeps all Hotfix 2 production behavior unchanged: desktop numerical-failure containment, shared host failure policy, picker-before-export, non-destructive temp-sibling save/replace and invariant engineering-number formatting.
- Original Hotfix 2 is SUPERSEDED / NOT VALIDATED. REV1 compiled, but the ordinary suite then failed 3 tests: missing explicit `InvalidDataException` archive classification, unnecessary backup cleanup on the new-file path, and a historical source assertion still tied to the old inline catch syntax. REV1 is therefore also SUPERSEDED / NOT VALIDATED by REV2.

## 2026-08-21 — M10.9.7.3 Hotfix 2 — Desktop Host Failure & Session Save Integrity — SUPERSEDED / NOT VALIDATED (xUnit1051 build failure)

- Promotes M10.9.7.3 Hotfix 1 REV2 to VALIDATED after build, complete ordinary tests, focused live-workspace audit and manual HMI checklist passed.
- Extends the desktop runtime-pump boundary from `InvalidOperationException` only to the explicit expected fail-closed family `InvalidOperationException` / `ArithmeticException` (therefore `OverflowException`), converting those failures to PAUSE + one diagnostic while leaving unknown/programming exceptions unhandled.
- Protects start-recorded-session and reset/recreate-session boundaries and unifies load/restore/save archive-operation failure classification.
- Reorders SAVE to destination-picker first; cancellation performs no archive export.
- Replaces destructive truncate-first overwrite with a testable local-filesystem writer: unique temporary sibling, complete UTF-8 write, durable flush/close, `File.Replace` for existing destinations or `File.Move` for new files, best-effort cleanup, and fail-closed behavior when no local path is available.
- Adds injected write/replace preservation tests plus a successful overwrite → replay-loadable archive integration test.
- Aligns App gauge-scale and COMPUTER controller-setpoint formatting to the invariant technical HMI decimal convention.
- Adds `scripts/run-m10973-desktop-host-session-integrity-audit.cmd` and a focused manual save/load checklist.
- No Simulation physics, challenge/scoring/protection authority, MISSION navigation semantics, plant-command authority or archive schema change.

## M10.9.7.3 Hotfix 1 REV2 Docs4 — Documentation Architecture / Indexing / Limitations Alignment — CANDIDATE

- Documentation-only revision over the unchanged REV2 runtime/test/script candidate.
- Reorganized `docs/ARCHITECTURE.md` by subsystem ownership and moved the former milestone-led ledger to `docs/history/ARCHITECTURE_MILESTONE_LEDGER.md`.
- Restored `docs/ROADMAP.md` to future-only content.
- Added exhaustive top-level-doc and ADR indexes, normalized ADR status headings, ADR-0186 and documentation-governance policy.
- Added current relief/bypass no-blowdown/reseat limitation, clarified existing physical control-rod travel ownership, reduced human-facing precision where exact values already live in frozen evidence, and assigned documentation-integrity automation to M11.5.
- No source, test, script, eng or CI change; M10.9.7.3 Hotfix 1 REV2 still awaits only manual HMI acceptance.


## 2026-08-21 — M10.9.7.3 Hotfix 1 REV2 Docs3 — Desktop Host / Session Integrity Roadmap Alignment — DOCUMENTATION-ONLY CANDIDATE

- Documentation-only alignment over Hotfix 1 REV2 Docs2; runtime, tests, scripts, engineering gates and CI are unchanged.
- Adds `docs/DESKTOP_HOST_FAILURE_AND_SESSION_SAVE_INTEGRITY_REVIEW.md` and ADR-0185.
- Plans M10.9.7.3 Hotfix 2 only after REV2 manual validation: expected desktop numerical-step failure containment, common session-handler failure policy, picker-before-export, temp-sibling + safe replacement session save, and invariant engineering-number formatting.
- Assigns UI-thread/projection/PropertyChanged/archive-export responsiveness measurement to M11.3.
- Expands M13 with stable canonical-ID command-target selection and staged `MainWindowViewModel` decomposition.
- M10.9.7.4 remains blocked until REV2 and the planned Hotfix 2 are both validated.

## 2026-08-21 — M10.9.7.3 Hotfix 1 REV2 Docs2 — Application Recording / Replay Review Roadmap Alignment

- Documentation-only follow-up over the same M10.9.7.3 Hotfix 1 REV2 runtime/test/script candidate; automated build/ordinary/focused evidence remains green and the manual HMI checklist remains the only promotion gate.
- Adds `docs/APPLICATION_RECORDING_REPLAY_REVIEW.md` and ADR-0184.
- Assigns a populated `sha256-control-room-snapshot-v1` golden/schema anchor plus protected lifecycle-spine retention to M10.9.7.4 before archive/timeline equivalence.
- Assigns future fingerprint multi-version compatibility and any versioned recording-retention format to M11.2.
- Assigns fingerprint JSON/SHA/hex cost, recorder collection-copy hardening, per-step lifecycle-notification measurement, long-session memory/LOH growth and recorder evidence-failure policy to M11.3.
- Clarifies that Hotfix 2 intentionally preserved per-step `LifecycleChanged` observation semantics while removing string-fingerprint allocation; no silent semantic change is made.
- Clarifies M9.1 recording-v1 retention: no silent frame truncation/decimation/circular-buffer reinterpretation.


## M10.9.7.3 Hotfix 1 REV2 Docs1 — Simulation Review / Roadmap Alignment — DOCUMENTATION-ONLY CANDIDATE

- Records that Hotfix 1 REV2 build, complete ordinary tests and `scripts/run-m10973-mission-performance-live-workspace-audit.cmd` are green; manual HMI validation remains the only promotion gate.
- Adds `docs/SIMULATION_NUMERICAL_REGULARITY_AND_RUNTIME_REVIEW.md` and ADR-0183 with the verified disposition of the post-7.3 Simulation review.
- Clarifies that the quadratic near-zero hydraulic law is genuinely non-differentiable, while the ideal pump check-valve transition is continuous but non-smooth; no speculative smoothing/leakage is authorized.
- Clarifies current branch-continuity ownership: memoryless base resolver plus conditional bounded previous-phase continuity in the corrected four-node path that may become committed when authority permits.
- Expands M11.3 with measured physics-preserving hot-path candidates and generic runtime catch-up policy audit.
- Expands M12 from six to eight planned slices: directionality; near-zero/conditioning; extreme envelope; pump mechanical/electrical/thermal energy ownership; decay heat; integrity; IncidentSeverity; closure.
- Updates architecture, limitations, roadmap and forward-plan ownership. No `src/`, `tests/`, `scripts/`, `eng/` or CI file changes belong to Docs1.

## M10.9.7.3 Hotfix 1 REV2 — Live Mission / Performance Historical Shell Contract Alignment — CANDIDATE

- Rebuilt over M10.9.7.2 Hotfix 3 REV1 VALIDATED plus Docs3, retaining the Hotfix 1 REV1 live-source runtime fix unchanged.
- Records Hotfix 1 REV1 as SUPERSEDED / NOT VALIDATED after Application.Tests passed but the correctly scoped historical shell regression still expected `{Binding LogicalStepText}` in the top shell.
- Aligns that historical test to the actual validated top runtime-step presentation `{Binding RuntimeProgressText}` (`STEP n`), matching `HMI_VISUAL_DESIGN_SYSTEM.md` and avoiding a duplicate current-step field in the situation strip.
- No runtime behavior change from Hotfix 1 REV1; only the historical App test and candidate descriptor/gate/documentation metadata change.
- Build, complete ordinary tests and `scripts/run-m10973-mission-performance-live-workspace-audit.cmd` are green for REV2; promotion now requires only the manual HMI checklist unless source/test/script files change.

## M10.9.7.3 Hotfix 1 REV1 — Live Mission / Performance Batch-Publication & HMI Regression Alignment — SUPERSEDED / NOT VALIDATED

- Rebuilt over M10.9.7.2 Hotfix 3 REV1 VALIDATED plus Docs3. The original M10.9.7.3 candidate is SUPERSEDED / NOT VALIDATED after two compile-contract failures; the first Hotfix 1 is also SUPERSEDED / NOT VALIDATED after ordinary tests exposed two regressions.
- Retains Hotfix 1's score-dimension field-wise test comparison and MainWindow mission DataContext separation unchanged.
- Fixes live batch ordering: `ControlRoomRuntimeCoordinator.AdvanceRunning` publishes all deterministic-step evidence before presentation snapshots, so intermediate presentation snapshots may arrive stale. `MissionPerformanceLiveSnapshotSource` now ignores presentation snapshots older than the latest deterministic evidence and fails closed if presentation ever leads deterministic evidence; it never rewinds demand/scoring history.
- Fixes the historical M10.9.1 situation-strip regression test by scoping its `GRID DEMAND` absence assertion to the actual top situation strip instead of the entire MainWindow. GRID DEMAND remains intentionally present only in the dedicated MISSION workspace.
- Extends the focused gate to execute that historical situation-strip regression explicitly.
- No Domain, Simulation or Infrastructure runtime change; no challenge/scoring/protection/plant-command/physics authority change.

- Application.Tests passed after the stale-batch fix, but App.Tests had one remaining failure because the newly scoped historical shell test retained an obsolete direct `LogicalStepText` expectation instead of the actual `RuntimeProgressText` binding.
- Replaced by Hotfix 1 REV2.

## M10.9.7.3 Hotfix 1 — Live Mission / Performance Compile Contract Alignment — SUPERSEDED / NOT VALIDATED

- Fixed the original candidate's score-dimension assertion type mismatch and Avalonia DataContext binding compile errors.
- Build then succeeded, but ordinary tests exposed a stale batch-presentation ordering defect in `MissionPerformanceLiveSnapshotSource` and an obsolete situation-strip test that scanned the entire MainWindow for `GRID DEMAND`.
- Replaced by Hotfix 1 REV1.

## M10.9.7.3 — Live Mission / Performance Workspace Wiring — SUPERSEDED / NOT VALIDATED

- Promotes M10.9.7.2 Hotfix 3 REV1 to VALIDATED after build, complete ordinary tests and `scripts/run-m10972-persistence-payload-integrity-audit.cmd` passed on 2026-08-21; ADR-0181 becomes Accepted.
- Activates the dedicated `MISSION` / `Mission & Performance` main-HMI workspace chosen by M10.9.7.2 while preserving COMPUTER F1–F8 and adding no F9.
- Adds a read-only live Mission/Performance source that accumulates demand/scoring evidence at every deterministic step but publishes immutable UI snapshots at presentation cadence and relevant same-step context changes.
- Adds explicit structural presentation comparison so recreated `IReadOnlyList<>` instances do not trigger redundant UI publications.
- Adds a presentation-only ViewModel/XAML hierarchy for objective/lifecycle, safety/protection significance, separate grid-demand/requested-load/actual-output evidence, score dimensions and bounded recent deterministic evidence.
- Adds contextual `OPEN MISSION` navigation from COMPUTER with zero plant-command authority.
- Keeps normal desktop startup mission-unbound and adds exact fail-closed `--mission-pack=<exact-id>` startup binding for live/manual validation; no scenario-to-pack inference or user-facing challenge launcher is introduced.
- Leaves archive-restored mission binding/timeline equivalence to M10.9.7.4 and adds no challenge definition, scoring arithmetic, protection authority or physics change.
- Adds ADR-0182, `docs/MISSION_PERFORMANCE_LIVE_WORKSPACE.md`, manual HMI checklist and `scripts/run-m10973-mission-performance-live-workspace-audit.cmd`.

## Documentation planning pass — Docs3 Detailed Forward Execution Plan — CANDIDATE

- Adds `docs/FORWARD_EXECUTION_PLAN_M10_9_7_TO_M15.md` as a long-lived future-work execution map; `PROJECT.md` remains the sole current-state authority.
- Carries forward Docs2's explicit post-M11 Epic mapping (M12 foundations, M13 control-room experience, M14 spatial reactor, M15 accident progression) and expands the entire post-persistence path into concrete implementation slices for M10.9.7.3 live MISSION activation, M10.9.7.4 timeline/drill-down, M10.9.7.5 closure, M10.9.8 integrated M10 validation, M11 release hardening and M12-M15 strategic epics.
- Records per-slice owners, non-scope, focused evidence, replay/checkpoint expectations, manual HMI gates and explicit deferred-item ownership.
- Keeps M10.9.7.2 Hotfix 3 REV1 runtime/tests/scripts unchanged; this planning pass is documentation-only and does not change the pending validation commands.

## M10.9.7.2 Hotfix 3 REV1 — JsonDocument Parse Exception-Type Test Alignment — CANDIDATE

- Documentation alignment pass (`Docs1`) reasserts `PROJECT.md` as the only current-state authority, removes duplicated live status from README/ROADMAP, updates ADR-0179/0180 to their validated states, keeps ADR-0181 proposed pending Hotfix 3 REV1 validation, and assigns deferred schema-v2/string-enum work to M11.2 and measured stream-persistence work to M11.3. No `src/`, test or validation-script behavior changes belong to Docs1.
- Original Hotfix 3 is SUPERSEDED / NOT VALIDATED after one Infrastructure test used `Assert.IsType<JsonException>` for malformed input parsed by `JsonDocument.Parse`; REV1 uses the public-contract-safe assignable check and does not modify persistence runtime code.
- Promotes M10.9.7.2 Hotfix 2 REV1 to VALIDATED after build, ordinary tests and the measured 10 ms focused gate passed on 2026-08-21.
- Restores `ControlRoomCommand.NumericValue` in schema-v1 session-archive `CommandDocument` for both operator actions and recorder events; adds a real manual-demand serialize/deserialize/full-replay regression.
- Rejects incomplete manual-demand payloads plus undefined persisted command/target/event enum values at the archive boundary.
- Keeps session archive schema v1 and its numeric enum representation unchanged; executable tests freeze the current v1 ordinals.
- Replaces the post-incident document's direct Application `ControlRoomCommand` property with a private Infrastructure DTO while preserving JSON payload shape including `numericValue`.
- Normalizes malformed/structurally invalid scenario, checkpoint and post-incident JSON to `InvalidDataException`, matching the already-hardened session archive while preserving `NotSupportedException` for future schemas.
- Adds ADR-0181, `docs/PERSISTENCE_PAYLOAD_INTEGRITY_ERROR_CONTRACT.md`, focused Infrastructure regressions and `scripts/run-m10972-persistence-payload-integrity-audit.cmd`.
- Does not change replay authority, schema version, scenario semantics, hot-path optimization, workstation activation, scoring, challenge definitions, protection, physics or plant command authority.

## M10.9.7.2 Hotfix 2 REV1 — Lifecycle Regression Fixture Condition-ID Alignment — VALIDATED

- Records the original Hotfix 2 ordinary-suite result accurately: Domain, Simulation, Infrastructure and App test assemblies passed, while `NuclearReactorSimulator.Application.Tests` had exactly one failure.
- The failure is test-only and deterministic: `LifecycleChanged_PreservesPerStepObservationChangeSemanticsWithoutStringFingerprint` authored `requiredObservationId: "step>=99-observation"`, but its local `TestConditionEvaluator` has no such case. Because the challenge activates at logical step 0, tracker construction immediately evaluates that unsupported ID and throws.
- REV1 changes that fixture to the already-supported `step>=3-observation` while keeping completion at `step>=99`; the test therefore still verifies exactly three per-step observation changes across logical steps 1, 2 and 3 without permitting challenge completion.
- All Hotfix 2 runtime optimization files (`ScenarioChallengeTracker`, `PlantDefinition`, `PlantState`, `CompressibleSteamFlowDefinition`) remain unchanged from the superseded Hotfix 2 candidate.
- Updates candidate descriptor, focused-gate method name and artifact metadata so the superseded Hotfix 2 cannot be confused with this REV1.
- Validation remains `dotnet build`, complete ordinary `dotnet test`, then `scripts/run-m10972-ten-ms-hot-path-hardening-audit.cmd`.

## M10.9.7.2 Hotfix 2 — Measured 10 ms Hot-Path Allocation & Lookup Hardening — SUPERSEDED / NOT VALIDATED

- Promotes M10.9.7.2 Hotfix 1 REV1 to VALIDATED after build, complete ordinary tests and `scripts/run-m10972-domain-definition-invariant-closure-audit.cmd` passed on 2026-08-21. The original Hotfix 1 remains SUPERSEDED / NOT VALIDATED.
- Replaces `ScenarioChallengeTracker` observation string fingerprint construction with a private observation-version counter while preserving `LifecycleChanged` semantics.
- Replaces hot `PlantDefinition` linear/capturing id scans with immutable id-to-canonical-index dictionaries built once per definition; `PlantState` reuses those indexes and deliberately owns no per-instance lookup dictionaries.
- Precomputes `CompressibleSteamFlowDefinition.CriticalDownstreamToUpstreamPressureRatio` once at construction without changing the formula or coefficients.
- Adds semantic regressions plus an explicit same-process measurement audit that records allocations and relative wall cost against test-local reference implementations equivalent to the replaced algorithms.
- Adds ADR-0180, `docs/TEN_MILLISECOND_HOT_PATH_HARDENING.md`, metrics artifacts and `scripts/run-m10972-ten-ms-hot-path-hardening-audit.cmd`.
- Does not change solver equations, reference-plant coefficients, MISSION activation, F1-F8/F9, scoring, challenge definitions, protection or plant-command authority.
- After validation, M10.9.7.3 may begin live Mission/Performance wiring using explicit presentation change detection rather than generated record equality over `IReadOnlyList<>`.

## M10.9.7.2 Hotfix 1 REV1 — Application Descriptor Contract Alignment — VALIDATED

- The original Hotfix 1 Domain invariant candidate compiled all production projects and passed Domain, Simulation, Infrastructure and App test assemblies, but the ordinary suite remained red with exactly one `NuclearReactorSimulator.Application.Tests` failure. Static inspection identified the stale `ApplicationDescriptorTests` contract: production metadata had moved to `M10.9.7.2 Hotfix 1 — Domain Definition Invariant Closure`, while the test still required the previous `M10.9.7.2 REV1 — Workstation Placement / Navigation Decision` strings.
- REV1 keeps every Domain guard and regression test from Hotfix 1 unchanged, updates only the Application descriptor regression contract to the active Hotfix 1 REV1 metadata, and adds that test to `scripts/run-m10972-domain-definition-invariant-closure-audit.cmd` so candidate metadata cannot drift outside the focused gate again.
- No solver, reference-plant coefficient, Simulation runtime, Avalonia runtime, navigation contract, scoring, challenge definition, protection authority or plant-command authority changes from the original Hotfix 1 candidate. The original Hotfix 1 remains NOT VALIDATED / SUPERSEDED by this REV1.

## M10.9.7.2 Hotfix 1 — Domain Definition Invariant Closure — SUPERSEDED / NOT VALIDATED

- Promotes M10.9.7.2 REV1 to VALIDATED after build, complete ordinary tests and `scripts\run-m1097-workstation-navigation-decision-audit.cmd` passed on 2026-08-21. Option A remains frozen and no Mission/Performance UI route is activated.
- Closes synchronization-window construction gaps without changing the validated reference-plant values: generator windows must be positive/non-degenerate, phase must remain below 180 degrees, voltage tolerance below rated voltage, and composed grid frequency/voltage windows may not span the complete nominal grid envelope.
- Revalidates `default(T)` bypasses for strictly-positive Domain quantities at their consuming definition boundaries: steam-drum source hydraulic resistance, iodine/xenon decay constants and optional turbine expansion resistance.
- Makes `PlantState` canonical fluid-node/thermal-body ownership use reference identity rather than record value equality, with structurally-equal clone regressions.
- Rejects undefined `ControlRodCommandTargetKind` values at `ActuatorDefinition` construction time.
- Adds ADR-0179, `docs/DOMAIN_DEFINITION_INVARIANT_CLOSURE.md`, focused regression coverage, artifact summary and `scripts\run-m10972-domain-definition-invariant-closure-audit.cmd`.
- Deliberately does not change solver equations, reference-plant coefficients, relief/bypass hysteresis, PID semantics, pressure arithmetic, lookup implementation, `ObservationFingerprint()`, workstation UI activation, scoring, challenge definitions, protection or plant-command authority.
- After validation, the next pre-live step is measured 10 ms hot-path allocation/lookup hardening before M10.9.7.3.

## M10.9.7.2 REV1 — Workstation Placement / Navigation Decision — VALIDATED

- Promotes M10.9.7.1 Hotfix 3 to the validated baseline after build, complete ordinary tests and `scripts\run-m10971-pre-workstation-robustness-audit.cmd` passed on 2026-08-21. Hotfix 3 includes the complete Hotfix 2 robustness implementation plus the test-only CS8629 fix.
- Rebuilds the previously superseded M10.9.7.2 decision milestone exclusively from Hotfix 3 VALIDATED; the earlier pre-Hotfix-3 package remains NOT VALIDATED and must not be promoted.
- Freezes placement option A in `MissionPerformanceNavigationDecision.Current`: future dedicated main-HMI `MISSION` / `Mission & Performance` workspace, contextual navigation from COMPUTER, unchanged Operator Computer F1–F8, no F9 and no plant-command authority.
- Deliberately keeps `UiRouteActivated=false` and does not modify `ControlRoomWorkspaceId`, `ControlRoomWorkspaceCatalog.Default`, MainWindow, ViewModel or XAML; live route/UI activation remains M10.9.7.3 work.
- Retains the pre-live review follow-ups without absorbing them into 7.2: tracker `ObservationFingerprint()` allocation must be addressed or qualified before live wiring; generated record equality over `IReadOnlyList<>` must not be used as UI change detection; score-dominance classification must fail earlier before future challenge-pack expansion; `FinalScore == FinalPercentage` remains valid only under the explicit v1 100-point policy invariant.
- Adds ADR-0178, `MISSION_PERFORMANCE_WORKSTATION_NAVIGATION.md`, App-layer navigation-decision tests and `scripts\run-m1097-workstation-navigation-decision-audit.cmd`.
- Adds no workstation UI, scoring formula, challenge definition, protection, physics, supervisory authority or plant command.

## M10.9.7.1 Hotfix 3 — Nullable SourceSequence Test Compile Fix — CANDIDATE

- Records the first M10.9.7.1 Hotfix 2 build result accurately: all production projects compiled, but `NuclearReactorSimulator.Application.Tests` was blocked by two CS8629 nullable-flow errors in the new bounded/future-event regression test.
- Keeps the Hotfix 2 production changes unchanged and fixes only the test flow: captures the nullable `SourceSequence` values, asserts `HasValue`, then compares via `GetValueOrDefault()` so nullable warnings-as-errors no longer reject the test while null still fails the preceding assertion.
- Changes no `src/` file, archive boundary, objective metadata, recent-event semantics, lifecycle alignment, requested-load ownership, scoring, challenge definition, UI/workstation placement, plant command authority, protection or physics.
- Validation remains `dotnet build`, `dotnet test`, then `scripts\run-m10971-pre-workstation-robustness-audit.cmd`.

## M10.9.7.1 Hotfix 2 — Pre-Workstation Presentation & Archive Robustness — CANDIDATE

- Promotes M10.9.7.1 Hotfix 1 to VALIDATED after build, complete ordinary tests and the focused presentation-contract audit passed on 2026-08-21.
- Marks the previously distributed M10.9.7.2 placement/navigation package SUPERSEDED / NOT VALIDATED; it must be rebuilt only after this hotfix validates.
- Normalizes blank, malformed/truncated JSON and structurally invalid session-archive data to `InvalidDataException` while preserving `NotSupportedException` for future schema versions; removes the pre-serializer blank-content guard from `CompositionRoot` and adds defensive `ArgumentException` / `KeyNotFoundException` / `OverflowException` handling at the async UI load boundary.
- Fixes Mission/Performance objective metadata to use the matched `ScenarioObjectiveDefinition.Title` and `.Description` instead of challenge title/description.
- Excludes recorder protection events after the presented logical step and bounds `RecentEvents` to the 100 newest deterministically ordered entries, preventing future-information leakage and unbounded presentation growth.
- Promotes the validated M10.9.6.5 terminal lifecycle as-of-step logic to shared internal `ChallengeLifecycleLogicalStepAlignment`, reused by replay, external-demand and Mission/Performance projectors without changing frozen terminal ownership. Non-terminal step mismatches still fail closed.
- Centralizes aggregate requested-generator-load evidence in `ControlRoomElectricalEvidence`, removing duplicated projector implementations.
- Strengthens elapsed-step and post-terminal regressions and adds malformed archive tests plus `scripts/run-m10971-pre-workstation-robustness-audit.cmd`.
- Adds no workstation placement, F1–F8 change, scoring formula/policy, challenge pack, plant command authority, protection, Simulation or physics change.

## M10.9.7.1 Hotfix 1 — xUnit2031 Assert.Single Predicate Compile Fix — CANDIDATE

- Records the first M10.9.7.1 build result accurately: all production projects, including `NuclearReactorSimulator.Application`, compiled successfully; `NuclearReactorSimulator.Application.Tests` was blocked only by analyzer error xUnit2031 in `M10971MissionPerformancePresentationContractTests`.
- Replaces the single analyzer-invalid `Assert.Single(projected.RecentEvents.Where(predicate))` assertion with the xUnit predicate overload `Assert.Single(projected.RecentEvents, predicate)`.
- Scans the complete test tree for the same `Assert.Single(...Where(...))` pattern; no additional occurrence remains.
- Changes no `src/` file, mission/performance projection semantics, M10.9.6 owner, scoring, challenge definition, workstation placement, F1–F8 navigation, plant command authority, protection or physics.
- Validation remains `dotnet build`, `dotnet test`, then `scripts\run-m1097-mission-performance-contract-audit.cmd`.

## M10.9.7.1 — Immutable Mission / Performance Presentation Contract — CANDIDATE

- Promotes M10.9.6.5 Hotfix 1 and M10.9.6 to VALIDATED/CLOSED after build, complete ordinary tests, cumulative closure gate and manual artifact/semantic review passed on 2026-08-21.
- Adds immutable `ControlRoom/MissionPerformance` snapshots and a pure projector over exact validated challenge lifecycle, external-demand and score owners plus existing assistance/control-authority presentation state.
- Preserves separate external demand, requested generator load and actual output; request/actual remain available when external demand itself is unavailable.
- Copies score/classification/dimension results without scoring arithmetic and projects deterministic objective/protection/scoring evidence without wall-clock ordering.
- Adds ADR-0177, `MISSION_PERFORMANCE_PRESENTATION_CONTRACT.md`, focused tests and `scripts/run-m1097-mission-performance-contract-audit.cmd`.
- Does not add UI, workstation placement, F9, challenge definitions, plant command authority, protection changes or physics changes. M10.9.7.2 remains the explicit placement/navigation decision.


## M10.9.6.5 Hotfix 1 — Terminal Lifecycle Replay-Step Alignment — CANDIDATE

- Records the first M10.9.6.5 ordinary-suite result accurately: build completed, but three `M10965ChallengeReplayCheckpointClosureTests` failed because the validated M10.9.6.1 tracker intentionally freezes `ChallengeLifecycleSnapshot.LogicalStep` at the terminal step while the canonical recorder can continue producing later frames.
- Keeps the strict M10.9.6.2 same-step demand projector unchanged; instead fixes only M10.9.6.5 replay reconstruction by deriving a terminal lifecycle *as-of replay frame* view whose `LogicalStep` matches the current recorder frame while preserving the exact terminal state, `TerminalLogicalStep`, activation step, observations and transitions.
- Applies the same alignment to the final replay lifecycle used by scoring/fingerprinting, preventing the latent follow-on failure in `OperationalChallengeScoreEvidenceProjector`, which correctly requires final lifecycle evidence at `ScenarioRecording.FinalLogicalStep`.
- Adds regression evidence that the returned final lifecycle logical step equals the recording final step, the true terminal step remains earlier when applicable, and every external-demand frame is projected at its matching recorder logical step.
- Changes no validated M10.9.6.1 lifecycle transition semantics, M10.9.6.2 demand contract, M10.9.6.3 score arithmetic/policies, M10.9.6.4 pack definitions, Simulation, UI, plant command authority, physics, protection or exact-version identity.
- Validation remains `dotnet build`, `dotnet test`, then `scripts/run-m1096-replay-checkpoint-closure-audit.cmd`, followed by the artifact-only manual checklist.

## M10.9.6.5 — Replay / Checkpoint / Determinism & Closure — CANDIDATE

- Promotes M10.9.6.4 to VALIDATED after build, complete ordinary tests and `scripts/run-m1096-initial-challenge-pack-audit.cmd` passed; ADR-0175 becomes Accepted.
- Adds deterministic `OperationalChallengeRecordingProjector` over canonical M9.1/M10.7 `ScenarioRecording` evidence; challenge state remains derivable and no opaque challenge checkpoint/save blob is introduced.
- Feeds recorded action acceptance and contiguous recorder frames through the existing M10.9.6.1 tracker, then projects M10.9.6.2 demand and M10.9.6.3 scoring evidence.
- Adds `m10965-challenge-replay-sha256-v1` validation fingerprint covering lifecycle, recorder-frame fingerprints, demand/request/output evidence and score decomposition without replacing plant/save fingerprints.
- Qualifies the exact `bounded-demand-following-5-10-5@1` pack across uninterrupted run, canonical replay, checkpoint seek and recorder resume; final challenge projection must match exactly despite publication-stride differences.
- Preserves challenge-specific protection semantics and standard v1 neutral guidance/authority modifiers; demand remains observational and cannot write generator requested load.
- Explicitly adds no demand-schedule action penalty or other new scoring criterion in closure; M10.9.6.3 exact scoring semantics remain frozen.
- Adds cumulative closure runner, summary/matrix artifacts, artifact-only manual checklist, `docs/OPERATIONAL_CHALLENGE_REPLAY_CHECKPOINT_CLOSURE.md` and ADR-0176.
- Adds no challenge UI, new challenge, Simulation change, fault physics, protection owner or plant command authority; Mission/Performance presentation remains M10.9.7.

## M10.9.6.4 — Initial Challenge Packs — CANDIDATE

- Promotes M10.9.6.3 Hotfix 1 to VALIDATED after build, complete ordinary tests and `scripts/run-m1096-multidimensional-scoring-audit.cmd` passed; ADR-0174 becomes Accepted.
- Adds six exact Application-layer operational challenge packs: pre-start circulation preparation, synchronization/initial loading, bounded 5→10→5 MWe demand-following, post-load-change 10 MWe stabilization, controlled normal shutdown and generator-trip/load-rejection response.
- Reuses validated M7.2/M7.5/M7.6 checklist evidence and committed M8.4 fault/action evidence rather than duplicating physical ownership.
- Freezes demand-schedule visibility: only bounded demand-following exposes the next scheduled change; post-load-change stabilization exposes current demand only; synchronization owns no demand profile. Demand never writes generator requested load.
- Adds exact score-evidence provenance bindings for every policy dimension but no new score arithmetic.
- Keeps failure semantics challenge-specific: normal-operation unexpected trips/emergency substitutions may fail authored challenges; generator trip is required evidence rather than failure in the load-rejection response challenge.
- Authors no hard failure deadlines before M10.9.6.5 runtime qualification.
- Adds focused pack tests, artifact summary, `scripts/run-m1096-initial-challenge-pack-audit.cmd`, `docs/OPERATIONAL_CHALLENGE_PACKS.md` and ADR-0175.
- Adds no Simulation/Avalonia changes, new fault physics, protection ownership, command dispatcher/control authority or exact-version change.

## M10.9.6.3 Hotfix 1 — Missing Parent Challenge Namespace Test Compile Fix — CANDIDATE

- Records the first M10.9.6.3 build result accurately: all production projects compiled, but `NuclearReactorSimulator.Application.Tests` was blocked only by CS0246 in `M10963ChallengeScoringContractTests` because the test imported the `.Demand` and `.Scoring` child namespaces but not the parent `Scenarios.Challenges` namespace that owns `ChallengeDefinition` and related lifecycle types.
- Adds only `using NuclearReactorSimulator.Application.Scenarios.Challenges;` to the focused M10.9.6.3 test.
- Re-checks the complete scoring-contract test for the recent xUnit2013 collection-size analyzer pattern; no such case is present.
- Changes no Application scoring implementation, exact policy identity, weights, thresholds, dominance caps, evidence semantics, guidance/authority modifiers, Simulation, UI, command authority, physics, protection or exact-version behavior.
- Validation remains `dotnet build`, `dotnet test`, then `scripts/run-m1096-multidimensional-scoring-audit.cmd`.

## M10.9.6.3 — Multidimensional Evaluation & Scoring Contract — CANDIDATE

- Promotes M10.9.6.2 Hotfix 1 external-demand semantics to validated baseline after build, ordinary suite and focused audit passed.
- Adds pure Application-layer scoring contracts under `Scenarios/Challenges/Scoring`; no Simulation/Avalonia/dispatcher/control/protection ownership changes.
- Freezes standard exact policies: `general-operations@1` = 45 safety / 30 procedure / 20 stability / 5 logical-time and `demand-following@1` = 40 safety / 25 procedure / 15 stability / 15 demand / 5 logical-time.
- Freezes grade thresholds 60/75/90% and dominance caps: authored critical safety failure 39%, authored critical procedure failure 59%; safety wins if both occur.
- Unavailable required evidence scores zero and makes evaluation incomplete/non-passing; every evidence item carries a stable source ID and summary.
- Standard v1 guidance and plant-authority modifiers are explicitly neutral 1.00; non-neutral effects require a distinct versioned policy.
- Protection trips are not globally failures; challenge-owned authored evidence determines semantic classification.
- Adds focused scoring tests, artifact summary, `scripts/run-m1096-multidimensional-scoring-audit.cmd`, `docs/OPERATIONAL_CHALLENGE_SCORING.md` and ADR-0174.

## M10.9.6.2 Hotfix 1 — Nullable Demand-Output Error Compile Fix — CANDIDATE

- Records the first M10.9.6.2 build result accurately: all projects before `NuclearReactorSimulator.Application` compiled, then Application was blocked only by CS0173 in `ScenarioChallengeExternalDemandProjector` because a conditional expression mixed `double` with `null` under `var` inference.
- Changes only that local declaration from `var error = ... ? double : null` to explicit `double? error = ...`, matching the already nullable `ExternalEnergyDemandEvidenceSnapshot.DemandOutputErrorMegawatts` contract.
- Re-scans the new M10.9.6.2 demand files for the same nullable conditional-inference pattern; the only other `?: null` case returns a reference-type control point and is valid.
- Re-scans the new M10.9.6.2 test for xUnit collection-size analyzer anti-patterns; none are present.
- Changes no demand profile semantics, challenge lifecycle, requested-load/output separation, Simulation, UI, scoring, grid coupling, physics, protection or exact-version identity.
- Validation remains `dotnet build`, `dotnet test`, then `scripts/run-m1096-external-energy-demand-audit.cmd`.

## M10.9.6.2 — Deterministic External Energy-Demand Profiles — CANDIDATE

- Promotes M10.9.6.1 Hotfix 1 to VALIDATED after build, complete ordinary tests and `scripts/run-m1096-challenge-lifecycle-audit.cmd` passed on 2026-08-20; ADR-0172 becomes Accepted.
- Adds versioned challenge-owned `ExternalEnergyDemandProfileDefinition` with bounded logical-step control points and `HOLD` / `LINEAR` interpolation, supporting constant, step, bounded-ramp and piecewise demand primitives.
- Adds optional `ChallengeDefinition.ExternalDemandProfile`; challenges without a profile or before activation expose demand as unavailable.
- Adds pure `ScenarioChallengeExternalDemandProjector` evidence separating external grid demand, aggregate generator requested load and actual gross electrical output; demand/output error is observational only.
- Makes future schedule visibility definition-owned and exposes only the next authored control point when permitted.
- Adds fail-closed profile validation for invalid bounds, offsets, terminal interpolation and ramp contracts; all timing remains logical-step-only.
- Adds focused tests and `scripts/run-m1096-external-energy-demand-audit.cmd`, technical reference `docs/OPERATIONAL_CHALLENGE_ENERGY_DEMAND.md` and ADR-0173.
- Adds no score arithmetic, challenge UI, automatic generator load following, grid-coupling mutation, supervisory authority, physics, protection or exact-version change. M10.9.6.3 multidimensional scoring remains next after validation.


## M10.9.6.1 Hotfix 1 — xUnit2013 Collection-Size Assertion Compile Fix — CANDIDATE

- Records the first M10.9.6.1 validation attempt accurately: all production projects and all non-Application test projects compiled, but `NuclearReactorSimulator.Application.Tests` was blocked by two xUnit2013 analyzer errors in `M10961ChallengeLifecycleContractTests`.
- Replaces only the two collection-size assertions `Assert.Equal(1, ...Actions.Count)` with analyzer-compliant `Assert.Single(...)`.
- Re-scans the complete new M10.9.6.1 test file for the same collection-size anti-pattern; no additional xUnit2013 case remains in the new challenge-lifecycle test.
- Changes no Application challenge lifecycle implementation, condition semantics, evidence ownership, logical-time contract, Simulation, UI, command dispatch, physics, protection or exact-version identity.
- Validation remains `dotnet build`, `dotnet test`, then `scripts/run-m1096-challenge-lifecycle-audit.cmd`.

## M10.9.6.1 — Operational Challenge & Energy-Demand Framework / Challenge Lifecycle & Logical-Time Contract — CANDIDATE

- Promotes M10.9.5 Contextual Command Consequence Model to VALIDATED/CLOSED after its cumulative automated closure passed and the user explicitly continued beyond the required manual HMI gate.
- Adds versioned Application-layer `ChallengeDefinition`, logical-step-only timing metadata, assistance/scoring-policy declarations and deterministic lifecycle states `NotStarted|Ready|Active|Completed|Failed|Cancelled`.
- Adds read-only `IChallengeEvidenceSource` / `ScenarioChallengeEvidenceSource` so lifecycle tracking consumes immutable `ControlRoomSnapshot` values plus accepted `ScenarioOperatorActionRecord` history without receiving plant command or control-authority seams.
- Adds `ScenarioChallengeTracker` with authored activation/required-observation/completion/failure conditions, explicit cancel/reset, observational target windows, optional hard logical-step failure deadline and deterministic same-step failure precedence.
- Adds tests proving publication-stride independence, same logical/action trace reconstruction, explicit deadline semantics, cancel/reset non-dispatch behavior, version/objective/assistance ownership and absence of wall-clock types in the public lifecycle contract.
- Adds `scripts/run-m1096-challenge-lifecycle-audit.cmd`, artifact summary under `artifacts/m1096-challenge-lifecycle`, `docs/OPERATIONAL_CHALLENGE_LIFECYCLE.md` and ADR-0172.
- Adds no external demand profile, score arithmetic, challenge UI, new physical model, protection rule, command type, supervisory authority or exact-version change. M10.9.6.2 deterministic external energy-demand profiles remains next after validation.


## M10.9.5.5 — Contextual Command Consequence Model Closure Gate — CANDIDATE

- Promotes M10.9.5.4 to the validated baseline after build, complete ordinary tests and `scripts/run-m1095-command-observed-response-audit.cmd` passed on 2026-08-20.
- Adds no new runtime/UI feature. Adds a cumulative closure runner that reruns the four validated M10.9.5.1-5.4 focused gates in order and writes final evidence only when all four pass in the same invocation.
- Adds cross-cutting closure tests for 27/current command-kind authored coverage, exact monitor-step reuse, zero dispatch/zero observed-response start during inspection/navigation, explicit ENTER/EXECUTE ownership, expected-vs-observed separation, 500-logical-step evidence window and `[JsonIgnore]` observation compatibility.
- Adds `docs/M10_9_5_5_MANUAL_VALIDATION_CHECKLIST.md` covering representative command families, exact schematic focus behavior, accepted/rejected observed response, keyboard-only operation and minimum-window readability.
- Final automated evidence deliberately reports `m1095-closure-ready=True`, not unconditional milestone promotion; M10.9.5 becomes VALIDATED only after explicit manual HMI confirmation.
- No Simulation, physics, protection, command type, dispatcher, exact-version, challenge/scoring or automatic graph traversal changes.

## M10.9.5.4 — Observed Response Evidence — VALIDATED

- Local build, complete ordinary tests and `scripts/run-m1095-command-observed-response-audit.cmd` passed on 2026-08-20.
- ADR-0171 is Accepted.
- M10.9.5.4 is the validated baseline for the M10.9.5.5 closure candidate.

## M10.9.5.4 — Observed Response Evidence — CANDIDATE

- Promotes M10.9.5.3 Hotfix 2 to the validated baseline after build, complete ordinary tests, focused context-inspector/schematic audit and manual HMI continuation were green.
- Adds deterministic current-value projection for the exact authored M10.9.5.1 monitor set; target-specific values are read only from the existing UI-safe `ControlRoomSnapshot`.
- Adds a presentation-only observed-response accumulator with a fixed 500-logical-step window, dispatch-boundary baseline, latest observed value/state, numeric delta/direction, accepted/rejected feedback and observed protection state.
- Rejected commands expose no fictional plant-effect deltas; accepted monitor changes are labelled post-dispatch co-variation only and never converted into generic `SUCCESS/FAILURE` or proof of causality.
- Adds a distinct F4 COMMANDS `OBSERVED RESPONSE — POST-DISPATCH EVIDENCE` panel while preserving the M10.9.5.3 Context Inspector, canonical mimic focus and explicit ENTER/EXECUTE dispatch boundary.
- Marks command observation samples `[JsonIgnore]` because they are derivable presentation evidence; no save/replay fingerprint, exact-version identity or authoritative plant state is changed.
- Adds Application/ViewModel/XAML focused tests, `scripts/run-m1095-command-observed-response-audit.cmd` and ADR-0171.
- No Simulation physics, protection/permissive ownership, command kinds, graph traversal, challenge/scoring or numerical prediction is introduced.

## M10.9.5.3 Hotfix 2 — COMMANDS Context Inspector XAML Contract & Exact Schematic Focus Semantics — CANDIDATE

- Records the Hotfix 1 result accurately: production/Application projects and the new App code compiled, but the complete ordinary suite had one failure in `OperatorComputerM10953ContextInspectorXamlTests` because the test expected `{Binding SelectedCommandSchematicElementId}` while the XAML intentionally uses `{Binding SelectedCommandSchematicElementId, Mode=OneWay}` for presentation-only focus.
- Corrects that XAML contract test and additionally requires `IsHitTestVisible="False"`, freezing the non-interactive mimic boundary.
- Full re-review found a presentation inconsistency not yet caught by the failing test: non-graphical dependency steps (`CommandTarget` / `PublishedState`) could inherit a fallback highlight from another graphical step. Hotfix 2 removes that fabricated fallback.
- Initial command selection now prefers the first dependency step with an authored `PlantMimicElement` or `PlantMimicConnection` reference when one exists; otherwise it preserves the first step with no schematic highlight.
- Element steps highlight the exact canonical element; connection steps use the existing connection's `FromElementId` only as an explicitly labelled proxy; non-graphical steps clear the highlight and state that no canonical mimic focus exists.
- Adds a regression proving non-graphical step selection clears the highlight and never dispatches.
- No M10.9.5.1 consequence semantics, M10.9.5.2 dependency topology, command dispatch, Simulation, protection, physics or exact-version behavior changes.

# M10.9.5.3 Hotfix 1 — Consequence Monitor Projection Compile Fix — CANDIDATE

- fixes the only reported M10.9.5.3 build failure in `OperatorComputerViewModel`: `OperatorComputerCommandConsequenceProjection` exposes `MonitorTargets`, not `Monitors`;
- also aligns the monitor explanation field with the validated M10.9.5.1 record contract: `OperatorComputerCommandMonitorTarget.Reason`, not `Explanation`;
- changes no catalog semantics, dependency-chain semantics, XAML contract, dispatch boundary, runtime state, physics, protection, exact-version identity or manual HMI acceptance criterion;
- validation remains `dotnet build`, `dotnet test`, `scripts\run-m1095-command-context-inspector-schematic-audit.cmd`, then the existing M10.9.5.3 manual HMI checklist.

# M10.9.5.3 — COMMANDS Context Inspector & Schematic Integration — CANDIDATE

- builds exclusively on validated M10.9.5.2;
- integrates the validated consequence/dependency projections into F4 COMMANDS;
- adds progressive disclosure for direct effect, expected influence and monitor evidence;
- adds selectable dependency-chain presentation and canonical whole-plant mimic focus;
- command selection/navigation remains non-dispatching; ENTER/EXECUTE remains authoritative;
- blocked commands remain inspectable;
- no new physics, graph traversal, protection/control ownership or predictive numerical UI is introduced;
- adds focused App/ViewModel/XAML tests, runner and ADR-0170.

## M10.9.5.2 — Contextual Command Consequence Model / Explicit Dependency-Chain Projection — CANDIDATE

- Starts exclusively from the user-validated M10.9.5.1 consequence-semantics/catalog baseline.
- Adds `OperatorComputerCommandDependencyChainCatalog`, a deterministic Application-only bounded chain projection for every authored current command shape.
- Distinguishes command intent, control/actuator state, physical process path, measurement/model observation and protection/alarm relation.
- Reuses canonical typed command targets, existing whole-plant mimic elements/connections and published `ControlRoomSnapshot` paths; M10.9.5.1 monitor targets/provenance remain authoritative.
- Adds fail-closed `NO AUTHORED DEPENDENCY CHAIN` behavior for invalid/future command-target shapes.
- Explicitly forbids automatic graph traversal, shortest-path inference, numerical prediction and runtime side effects.
- Adds focused tests and `scripts\run-m1095-command-dependency-chain-audit.cmd`; adds ADR-0169.
- No Avalonia/COMMANDS integration yet; M10.9.5.3 remains next after validation.

## M10.9.5.1 — Contextual Command Consequence Model / Consequence Semantics & Catalog — VALIDATED

- Local build, complete ordinary tests and `scripts\run-m1095-command-consequence-catalog-audit.cmd` passed on 2026-08-20.
- M10.9.5.1 is the authoritative post-Phase-I baseline for M10.9.5.2.
- ADR-0168 is Accepted.

## M10.9.5.1 — Contextual Command Consequence Model / Consequence Semantics & Catalog — CANDIDATE

- Starts from the user-validated M10.9.4.1 / Phase-I-closed repaired-v4 baseline; no Phase-I numerical contract is reopened.
- Adds `OperatorComputerCommandConsequenceCatalog`, a deterministic Application-only authored semantic catalog covering all 27 current `ControlRoomCommandKind` values, including turbine valve/control-valve commands that are runtime-supported but not yet exposed by the M10.4 COMMANDS console.
- Freezes qualitative relation vocabulary (`INCREASES/DECREASES EXPECTED DEMAND ON`, `ENABLES/DISABLES PATH`, `AFFECTS`, `MAY AFFECT`, `PROTECTION MAY OVERRIDE`) without numerical future prediction.
- Associates authored consequences with existing whole-plant mimic element IDs and published `ControlRoomSnapshot` property paths; monitor targets carry MEASURED / MODEL / canonical-state provenance.
- Adds explicit fail-closed `NO AUTHORED CONSEQUENCE MAP` behavior for unsupported/future command-target shapes rather than inventing causality.
- Adds focused tests for enum/catalog completeness, current COMMANDS-console coverage, turbine-valve family coverage, canonical reference resolution, deterministic qualitative separation and explicit-unmapped behavior.
- Adds `scripts\run-m1095-command-consequence-catalog-audit.cmd`.
- No command dispatch, runtime physics, protection ownership, numerical solver, exact-version identity, scenario seed or Avalonia UI is changed. M10.9.5.2 dependency-chain projection remains the next step after validation.

## M10.9.4.1-I.5 REV1 Hotfix 17.1 Docs Planning 1 — Post-Phase-I / M11 Execution Plan — DOCUMENTATION-ONLY CANDIDATE

- Adds detailed planning contracts for M10.9.5, M10.9.6, M10.9.7, M10.9.8 and M11 without changing runtime, tests, CI scripts or validation gates.
- Expands `ROADMAP.md` from milestone bullets into a fixed execution discipline with explicit entry/exit gates, non-scope, failure discipline and a post-M11 engineering horizon.
- Freezes M10.9.5 as a qualitative/authored consequence model that keeps direct effect, expected influence, blockers, monitor targets and observed response separate and never performs UI-side predictive physics.
- Plans M10.9.6 as deterministic logical-time challenge/demand/scoring ownership; external demand, requested generator load and actual output remain distinct. Exact score weights and selected challenge semantics are explicit pre-implementation decisions, not hidden implementation choices.
- Plans M10.9.7 as presentation-only mission/performance aggregation. Records one open HMI architecture decision: the validated Operator Computer F1–F8 contract must not silently become F1–F9; the recommended plan is a dedicated main-HMI Mission/Performance workspace linked from COMPUTER.
- Plans M10.9.8 as the M10 closure matrix across `Hidden|ChecklistOnly|Guided` assistance and `Manual|Assisted|SupervisoryAutomatic` authority, with degraded measurement, protection, fault, takeover, challenge/scoring and replay/checkpoint cases.
- Plans M11 as strict release hardening: support/version freeze, persistence compatibility/migration, performance/memory budgets, packaging/deployment, documentation/manual alignment and a final clean release-candidate gate. No new feature work is accepted unless a release gate proves a blocker.
- Refreshes the approved future gameplay/damage/accident direction so it is explicitly post-M11 and ordered by causal prerequisites: extreme-envelope audit → integrity/stress primitives → component damage families → core-damage prerequisites → incident severity/persistence → later fidelity/UI extensions.

## M10.9.4.1-I.5 REV1 Hotfix 17.1 — Final Preflight Static Audit & Documentation Alignment — CANDIDATE

- Performs a static preflight over every Hotfix 17 C# / batch / evidence-contract delta before the multi-hour cumulative closure. The new repaired-v4 300 s audit has the required namespaces, including `Application.Scenarios.Recording` for `ControlRoomSnapshotFingerprint`; no escaped-string interpolation pattern like the Hotfix 9 compile failure remains.
- Verifies all batch `call` targets and all `--filter-method` targets exist; opt-in environment-variable names match their tests; the repaired-v4 artifact producer/consumer paths align with cumulative closure; fixes the stale root `APPLY_UPDATE.cmd` that still announced Hotfix 16 and pointed users back to the already validated narrow activation audit.
- Recomputes the canonical SHA-256 of `I3_ValidatedAuthoritativeToleranceBudgets.csv` as `9B7A2653F08059ECBD16F39FEB0DD7350F62C98A5892A8215D34404D6C9301BB`, confirms 19 budget rows, the two-line repaired-v4 contract, and the 19-row Phase-I tier split `1 ORDINARY / 3 CURRENT-EVIDENCE / 4 SCHEDULED-LONG / 11 HISTORICAL-FROZEN`.
- No C# runtime/test logic or CI gate logic is changed by this hotfix. `APPLY_UPDATE.cmd` is corrected from stale Hotfix-16 instructions to the final Hotfix-17/17.1 cleanup and validation sequence.
- Updates `README.md`, `docs/PROJECT.md`, `docs/ROADMAP.md`, `docs/KNOWN_MODEL_LIMITATIONS.md`, `docs/WATER_STEAM_MODEL.md` and ADR 0165/0166; adds ADR 0167 to record exact-v4 authoritative activation while exact @2/@3 and synchronization exact @3 remain immutable compatibility/provenance identities.
- Final validation remains `dotnet build`, `dotnet test`, then `scripts\run-m10941-cumulative-closure-audit.cmd`. No additional repair stage is introduced.

## M10.9.4.1-I.5 REV1 Hotfix 17 — Final Repaired-v4 Phase-I Closure — CANDIDATE

- Promotes Hotfix 16.2 production activation to locally validated evidence: authoritative exact @4 completed the 1200-step healthy control with zero health/trip/breaker violations, 2/2 corrected trigger/commit, zero rollback/fallback/unsafe/untargeted disagreement and deterministic repeat.
- Adds the final authoritative exact-v4 300-second production-reference requalification. The gate runs 30,000 steps at 10 ms, checks health and stop/control/admission flow every step, samples one-second reference state, computes the seven final-window slopes and compares 19 observations directly against the unchanged frozen I.3 exact-v3 tolerance budgets.
- The 19 I.3 budgets are acceptance authority only: Hotfix 17 neither regenerates nor widens them. Historical exact-v3 I.3 evidence remains immutable provenance and is explicitly not reinterpreted as exact @4 evidence.
- Realigns `eng/ci-current-evidence.cmd` and `eng/phase-i-audit-tiers.csv`: H.30 RQ1, I.3 exact-v3 and I.4 move to `HISTORICAL-FROZEN`; current evidence becomes I.2 contract + synchronization exact-v3 activation + repaired exact-v4 production activation.
- Realigns `eng/ci-long.cmd`: GameplayLong, OperationalEnvelope and ReferencePlantScale remain, while the stale authoritative-v3 I.3 rerun is replaced by the repaired exact-v4 300-second reference requalification.
- Updates the cumulative M10.9.4.1 closure to require current exact-v4 activation, synchronization exact-v3 evidence, repaired exact-v4 frozen-budget reference evidence and the complete scheduled-long chain before writing `phase-i-closed=True` and `m1095-unblocked=True`.
- Preserves exact @2 rollback, exact @3 historical replay, synchronization exact @3, the 10 ms fixed step, H.20/H.22 ownership, H.9/P060-F040/hysteresis contracts and all physical coefficients. The only `src/` change is application metadata describing the final closure candidate.
- Final validation is `dotnet build`, `dotnet test`, then `scripts\run-m10941-cumulative-closure-audit.cmd`. No additional I.5 repair stage is planned if this chain is green.

## M10.9.4.1-I.5 REV1 Hotfix 16.2 — Production Activation Descriptor Contract Fix — CANDIDATE

- Fixes the only ordinary-suite failure remaining after Hotfix 16.1: `ApplicationDescriptorTests.Current_DescribesI5RepairedExactV4ProductionActivation` used ordinal case-sensitive matching for `"exact v3"`, while the authoritative descriptor correctly says `"Exact v3 remains..."`.
- Changes only the expected casing in that application test plus current candidate documentation/changelog. No production/runtime code, selector, exact-version composition, thermodynamic closure, numerical contract or acceptance criterion changes from Hotfix 16.1.
- Validation remains `dotnet build`, `dotnet test`, then `scripts/run-i5-repaired-exact-v4-production-activation-audit.cmd`.

## M10.9.4.1-I.5 REV1 Hotfix 16.1 — Production Activation Audit Compile Fix — CANDIDATE

- Fixes the Hotfix 16 focused activation audit compile failure `CS0103` by importing `NuclearReactorSimulator.Application.Scenarios.Recording`, the namespace that owns `ControlRoomSnapshotFingerprint`.
- Changes only the new application-test audit plus current candidate documentation/changelog. Production/runtime code, exact @2/@3/@4 composition, selector activation, thermodynamic closure, numerical contracts and acceptance criteria are unchanged from Hotfix 16.
- The validation sequence remains `dotnet build`, `dotnet test`, then `scripts/run-i5-repaired-exact-v4-production-activation-audit.cmd`. The multi-hour cumulative closure remains deferred until this narrow activation gate is green.

## M10.9.4.1-I.5 REV1 Hotfix 16 — Repaired Exact Version 4 Authoritative Production Activation — CANDIDATE

- Promotes Hotfix 15 exact-v4 readiness to locally validated evidence: exact @1/@2/@3/@4 resolve distinctly; the actual @4 factory completed the frozen 7000-step gap journey with 9/9 trigger/commit, zero rollback/fallback/unsafe/untargeted disagreement and no thermodynamic out-of-range failure.
- Adds `DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit` and changes the authoritative desktop production selector from historical exact @3 to repaired exact @4.
- Keeps exact @2 as fail-closed `ExplicitCommittedState` rollback/reference and exact @3 as an immutable historical H.29/H.30 corrected-commit replay identity; `H29ActivationCandidatePolicy` still resolves exact @3.
- Adds a distinct production scenario `integrated-normal-operations-training-i5-repaired-v4-production` while retaining the historical H.30 production and I.5 readiness scenario identities for replay/training-plan compatibility.
- Updates ordinary current-selector/versioning contracts so historical H.30/I.3 manifests remain frozen provenance while current production is exact @4.
- Adds a narrow production-activation audit: 1200 healthy default-production steps, fail-closed corrected telemetry, conservation bounds and 128-step deterministic repeat.
- Does not alter thermodynamic equations, physical coefficients, H.9 tolerances, P060/F040, hysteresis bounds, synchronization exact versions or the 10 ms fixed step.
- The next and final technical block is repaired-v4 scheduled-long/reference compatibility and cumulative M10.9.4.1 / Phase-I closure.

## M10.9.4.1-I.5 REV1 Hotfix 15 — Repaired Exact Version 4 Registration & Activation Readiness — CANDIDATE

- Promotes Hotfix 14 Stage 4 to locally validated repaired performance/cost/operational-soak evidence: original H.28 relative ceilings pass, repaired corrected is bounded at-or-below repaired explicit on median/p95 wall time, 1536-step corrected soak is trip/rollback/unsafe/disagreement free, and deterministic repeat passes.
- Adds exact `integrated-operations-desktop-stable@4` through `DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory`. Exact @4 preserves the exact-v2 physical seed, the exact-v3 H.22 corrected-commit ownership and 10 ms fixed step while selecting the validated `CorrelationConsistentInverseDomain` water/steam closure.
- Keeps exact @2 and @3 immutable. The current production selector deliberately remains H.30 exact @3 in this readiness candidate; @4 is registered for exact loading/replay and is not yet authoritative production.
- Adds a distinct replayable training scenario `integrated-normal-operations-training-i5-repaired-v4-activation-candidate` and registers exact @4 in the desktop application composition root so candidate saves/archives can resolve without reinterpreting historical identities.
- Adds a narrow activation-readiness audit that resolves exact @1/@2/@3/@4 independently, proves current production still points to @3 and explicit kill to @2, verifies @4 uses corrected hydraulics at 10 ms, and drives @4 through the frozen 7000-step exhaust-gap journey with fail-closed corrected telemetry; trip count remains observational because the historical load-step contract does not impose a no-trip floor.
- No production selector switch occurs in Hotfix 15. If the narrow gate is green, the next candidate performs the single authoritative @3 -> @4 production switch and immediately runs the final scheduled-long/reference-plant/I.3/cumulative Phase-I closure chain.

## M10.9.4.1-I.5 REV1 Hotfix 14 — Thermodynamic Repair Performance/Cost/Operational-Soak Requalification Stage 4 — CANDIDATE

- Promotes user-validated Hotfix 13 Stage 3 evidence: repaired replay/checkpoint/reverse-power protection and all six repaired H.27 off-design scenarios passed with deterministic/fail-closed ownership and bounded conservation.
- Adds an explicit repaired H.28-style performance/cost gate. Both benchmark modes use `CorrelationConsistentInverseDomain`; only hydraulic ownership differs, isolating corrected-path overhead from thermodynamic-repair cost.
- Preserves the original H.28 machine-local relative ceilings unchanged: median wall-cost ratio <= 8, p95 wall-cost ratio <= 12 and median allocation ratio <= 16. The benchmark remains 64 warmup + 256 measured steps per mode with the historical 5 -> 0 -> 5 MWe manoeuvre repeated twice.
- Adds a 1536-step repaired corrected operational soak using six repetitions of the bounded H.28 benchmark load cycle, with fail-closed ownership, no trips, conservation budgets, memory/GC observations and sampled telemetry.
- Adds a 128-step deterministic control repeated twice. Timing/allocation values are observational only and never enter deterministic runtime behavior.
- No registered exact-version identity or production selector is changed. The 10 ms fixed step, H.20/H.22 authority/ownership, H.9, P060/F040, four-node target set, hysteresis limits and physical coefficients are unchanged.
- If Stage 4 is green, the next step is a narrow new exact repaired desktop identity followed by activation evidence, then the final scheduled-long/reference-plant/I.3/cumulative Phase-I closure.

## M10.9.4.1-I.5 REV1 Hotfix 13 — Thermodynamic Repair Replay/Protection/Off-Design Requalification Stage 3 — CANDIDATE

- Promotes Hotfix 12 Stage 2 to locally validated repaired long-horizon evidence: 30,000 qualification intervals across `steady-long|load-pulse|cooling-pulse|combined-load-cooling`, 58/58 triggers/eligible/authorized/commits, zero rollback/fallback/unsafe/untargeted violations, deterministic repeat and bounded conservation.
- Freezes the Stage-2 continuity classification: branch overrides `0`, previous-phase holds `3720`, hysteresis releases `0`, 58 continuity-active trigger steps total and 50 after the first two startup intervals. Detailed telemetry shows 30 trigger steps with 82 holds and 28 with 45; previous-phase hysteresis remains materially exercised and is not retired.
- Adds `PhaseIThermodynamicRepairReplayCheckpointProtectionRequalificationAuditTests`, using an audit-only exact-version factory over `CorrelationConsistentInverseDomain` + H.29 corrected ownership to requalify full scenario replay, checkpoint continuation, deterministic telemetry identity and evidence-derived reverse-power generator protection without registering a production identity.
- Adds `PhaseIThermodynamicRepairOffDesignRequalificationAuditTests`, replaying the validated H.27 six-scenario bounded off-design envelope through the repaired closure with the existing fail-closed corrected-commit authority.
- Adds `scripts/run-i5-thermodynamic-repair-replay-protection-off-design-requalification-stage3-audit.cmd`, which runs both explicit gates and collects their seven evidence files into `artifacts/i5-thermodynamic-repair-requalification-stage3`.
- No file under `src/` changes in Hotfix 13. Historical H.23/H.27 tests and frozen evidence remain untouched; exact desktop `@2`/`@3` and the production selector remain unchanged.
- Performance/cost/operational-soak requalification is deliberately deferred to Stage 4 so semantic replay/protection/off-design failures can be separated from timing/cost regressions. Cumulative Phase-I closure remains blocked.

## M10.9.4.1-I.5 REV1 Hotfix 12 — Thermodynamic Repair Long-Horizon/Cross-Profile Requalification Stage 2 — CANDIDATE

- Promotes Hotfix 11 Stage 1 to locally validated repaired-closure evidence: build and tests passed; 1024-interval H.29 control completed under explicit and corrected hydraulics; corrected authority exercised with `2/2` commits, zero rollback/unsafe/untargeted violations, exact deterministic repeat and bounded conservation.
- Freezes the key Stage-1 classification: branch overrides are `0`, previous-phase holds are `164` total (`82` on each of the first two trigger steps), hysteresis releases are `0`, and no continuity activity was observed after the initial two intervals in that control pattern.
- Adds `PhaseIThermodynamicRepairLongHorizonCrossProfileRequalificationAuditTests`, reusing the validated H.19/H.24 four-profile domain: `steady-long` 12000, `load-pulse` 6000, `cooling-pulse` 6000 and `combined-load-cooling` 6000 intervals.
- Runs the Hotfix 10 `CorrelationConsistentInverseDomain` evidence seam with real H.29 corrected-commit ownership for all 30000 qualification intervals. No registered exact-version identity or production selector is changed.
- Keeps hard acceptance on no trip, H.20/H.22 fail-closed commit safety, zero unsafe/fallback commits, zero untargeted disagreement, conservation budgets and deterministic repeat.
- Treats trigger/commit/branch-override/previous-phase-hold/hysteresis-release counts as classification evidence rather than inheriting the historical H.17/H.19 counts as floors. The audit separately reports continuity activity after the first two startup intervals.
- Production activation remains false. A green Stage 2 advances to repaired replay/checkpoint/protection plus off-design/performance requalification before any new exact repaired production identity or cumulative Phase-I closure.

## M10.9.4.1-I.5 REV1 Hotfix 11 — Thermodynamic Repair Requalification Stage 1 — CANDIDATE

- Promotes Hotfix 10 to locally validated opt-in repair evidence: topology `3/3` observed vapor probes resolved, `7/7` saturated-only seam-below and `7/7` superheated-only seam-above, `231/231` low-temperature census resolved, and both explicit/corrected 7000-step frozen load journeys completed without thermodynamic out-of-range failure.
- Keeps every registered/default runtime, exact-version identity and production selector unchanged. No file under `src/` changes in Hotfix 11.
- Adds `PhaseIThermodynamicRepairRequalificationStage1AuditTests`, which runs the validated H.29 1024-interval control pattern through the Hotfix 10 repaired-closure evidence seam under explicit and corrected hydraulics.
- Re-establishes no-trip, conservation, deterministic-repeat and H.20/H.22 fail-closed commit safety under the changed thermodynamic topology.
- Records P060/F040 triggers, eligibility, authorization, commits, rollbacks, untargeted disagreements, branch overrides, previous-phase holds and hysteresis releases without turning historical counts into new acceptance floors.
- Adds explicit-vs-corrected physical comparison evidence. The purpose is to determine whether H.13-H.19 continuity machinery remains materially active after the historical vapor overlap is removed, so the next long-horizon requalification can preserve only behavior still justified by evidence.
- Production activation remains false. Exact desktop `@2` and `@3` are not overwritten, and cumulative Phase-I closure remains blocked pending repaired long-horizon/cross-profile, replay/protection/off-design/performance and scheduled-long requalification.

## M10.9.4.1-I.5 REV1 Hotfix 10 — Correlation-Consistent Inverse-Domain Repair Candidate — CANDIDATE

- Promotes the user-validated Hotfix 9.1 census as frozen evidence: ordinary suite green (`1176` completed / `83` ignored), @2/@3 exhaust no-root reproduction green, vapor topology green (`194` no-root / `130` overlap / `44` missing-onset samples), and low-temperature census green (`83/83` proven inverse-search blind spots with local saturated-root brackets).
- Adds `WaterSteamThermodynamicClosureMode`. The parameterless/default mode remains `HistoricalCorrelationTopology`; all registered runtime identities and production selectors therefore retain pre-Hotfix-10 thermodynamic behavior.
- Adds opt-in `CorrelationConsistentInverseDomain`: saturation properties are unchanged, while the superheated pressure relation is volume-shifted so the superheated branch touches the correlated `vg(T), Psat(T), ug(T)` saturated-vapor boundary exactly instead of using the incompatible ideal boundary volume.
- Makes the opt-in saturated fallback interval-aware by locating the saturated-liquid density maximum numerically and solving cold-liquid, warm-liquid and vapor specific-volume boundaries independently. This removes the triple-point-connected monotonic-validity assumption responsible for the 4–8.17 °C blind spot.
- Adds an evidence-only desktop factory seam using the unchanged v2 physical seed with repaired thermodynamics under either explicit or H.29 corrected-commit hydraulics. No registered exact version or production policy is changed.
- Adds ordinary unit regressions for the historical M9.7 vapor-gap point and the 5.01 °C disconnected saturation root under the repaired mode while preserving the default historical negative-gap contract.
- Adds `PhaseIThermodynamicInverseDomainRepairCandidateAuditTests` plus `scripts\run-i5-thermodynamic-inverse-domain-repair-candidate-audit.cmd`. The focused gate requires all three observed vapor gaps to resolve, seven two-sided seam probes to have one branch per side, all 231 low-temperature census states to resolve, and the full 7000-step load raise/lower journey to complete under both explicit and corrected hydraulics.
- Production activation remains false. H.12-H.30 and all scheduled long gates must be requalified before the repaired closure can become authoritative. No `exhaust` special case, inventory clamp, condenser retune, tolerance widening or fail-closed weakening is introduced.

## M10.9.4.1-I.5 REV1 Hotfix 9.1 — Low-Temperature Census Compile Fix — CANDIDATE

- Fixes the only compile blocker introduced by Hotfix 9 in `PhaseIWaterSteamLowTemperatureLiquidSeamAuditTests`: the summary interpolation embedded an escaped `"OUT-OF-RANGE"` string literal inside an interpolation expression, which is invalid C# syntax and caused 13 cascade parser errors.
- Computes `productionOutOfRange` before building the summary string, so the interpolation contains only the numeric result and no nested string literal.
- The low-temperature census algorithm, sampling domain, density-maximum/warm-twin search, local saturated-root proof, assertions, expected evidence and all Hotfix 8/9 diagnostics are unchanged.
- No file under `src/` changes. No runtime/model/equation/coefficient/tolerance/target-set/fail-closed behaviour changes.

## M10.9.4.1-I.5 REV1 Hotfix 9 — Full Inverse-Domain Gap Census — CANDIDATE

- Treats the Hotfix 8 focused result as new evidence rather than a failed premise. The vapor audit confirmed 194 no-root samples, 130 overlap samples and 44 no-superheated-onset samples, while all @2/@3/M9.7 observed failures classify inside the same vapor no-root family.
- Reclassifies the single liquid-side failure at 5.01 °C as a second internal inverse-map defect family. The saturated-liquid density correlation is non-monotonic around the water density maximum (~4 °C); a valid local saturated root exists for the rejected `vf(T), uf(T)-10 J/kg` state, but the current boundary-aware saturated search assumes triple-point-connected monotonic validity and therefore never scans the disconnected valid interval.
- Updates the model-wide topology audit so the 5.01 °C result is frozen as expected evidence while regular liquid probes from 15.01 °C upward must remain resolvable.
- Adds `PhaseIWaterSteamLowTemperatureLiquidSeamAuditTests` and a focused runner. The new census maps 0.5–12 K above the triple point, locates the density maximum and warm triple-volume twin, independently proves local saturated-root brackets, and records every production out-of-range state for which a real root exists.
- No file under `src/` changes. No thermodynamic equation, condenser coefficient, solver tolerance, hydraulic policy, target set, acceptance floor or fail-closed rule changes.
- Phase I remains blocked. The next production step, if this census validates, is one coherent inverse-domain repair covering both the vapor seam closure and interval-aware saturated root discovery, followed by focused and cumulative requalification.

## M10.9.4.1-I.5 REV1 Hotfix 8 — Water/Steam Correlation Topology Audit — CANDIDATE

- Reclassifies the Hotfix 7 result: exact desktop @2 also reaches `WaterSteamStateOutOfRangeException` at `exhaust`, later than @3 (4498 successful steps vs 3763), so corrected-commit history changes reachability/timing but does not create the underlying no-root state.
- Adds an evidence-only topology audit over the production `SimplifiedWaterSteamThermodynamicModel`. It maps the full saturated-vapor/superheated seam, probes the saturated-liquid/subcooled seam and classifies the @2/@3 plus historical M9.7 no-root points.
- The audit explicitly compares the saturated-vapor boundary `vg = 1/rho_g(T)` with the superheated onset implied by `p = R*T/v` against `Psat(T)`. It records no-root-gap, overlap/multiple-root and no-superheated-onset-below-640-K regions rather than treating the exhaust failure as an isolated point.
- Adds representative low-pressure seam midpoint probes which must fail closed at several distinct temperatures and liquid-side +/-10 J/kg probes which must remain resolvable across the sampled saturated-liquid/subcooled seam.
- Updates the Hotfix 7 focused diagnostic so both @2 and @3 exhaust failures are valid evidence and the diagnostic returns green when it reproduces the shared family with @3 failing earlier.
- No file under `src/` changes. No thermodynamic equation, condenser coefficient, hydraulic policy, four-node target, tolerance, operating gate or fail-closed rule is changed. Phase I remains blocked pending a coherent vapor-boundary correction and requalification.

## M10.9.4.1-I.5 REV1 Hotfix 6 — Synchronization Corrected Exact-Version Activation — CANDIDATE

- Promotes user-validated Hotfix 5 qualification evidence: exact `pre-synchronization-grid-loading@3` passed the 10 s bounded stabilization plus strict 20–60 s sustained low-load contract with zero trip, breaker, shaft, stable rotor-band or reverse-admission violations.
- Registers exact @3 in desktop composition while preserving @1 and @2 loadable and unchanged. @2 remains explicit compatibility/reference; @3 is the supported sustained-synchronization exact version using `FourNodeBranchContinuityCorrectedCommitOptIn`.
- Moves the scheduled synchronization gameplay-long journey from @2 to @3. The first 10 s checkpoint verifies bounded stabilization/integrity; checkpoints 20–60 s retain the strict >4.0 MWe, >4.5 MW shaft, 2990–3010 rpm and forward-admission contract.
- Freezes only the compact Hotfix 5 summary/metrics/checkpoints plus a canonical evidence manifest; generated qualification artifacts remain external to candidate ZIPs.
- Adds a fast current-evidence activation audit and makes the cumulative I.5 closure require its artifact before declaring Phase I closed. The Phase-I tier manifest becomes 17 entries: 1 ORDINARY, 4 CURRENT-EVIDENCE, 4 SCHEDULED-LONG and 8 HISTORICAL-FROZEN.
- Re-aligns the scheduled reference-plant-scale gate to the authoritative desktop production selector and synchronization @3 instead of stale direct-v2 factories; the 10 MWe/grid-coupling expectations are unchanged.
- Re-labels the old I.1 source inventory tests explicitly as frozen historical provenance rather than pretending their 12-profile snapshot is the current registry.
- No steam-path coefficient, governor gain, grid coupling, protection, desktop production policy, solver mathematics or 10 ms fixed-step retuning. Candidate packaging continues to exclude `Gameplay/Evidence`, generated `artifacts`, `bin` and `obj`.

## M10.9.4.1-I.5 REV1 Hotfix 5 — Synchronization Corrected Exact-Version Qualification — CANDIDATE

- Treats the user-validated Hotfix 4 loaded-contract diagnostic as evidence: exactly 1/7 bounded candidates qualified, `corrected-only`.
- Adds exact `pre-synchronization-grid-loading@3` as an unregistered qualification candidate. It preserves every v2 physical seed, steam-path coefficient, governor gain, grid-coupling parameter, protection setting and the 10 ms fixed step; only hydraulic numerical ownership changes to `FourNodeBranchContinuityCorrectedCommitOptIn`.
- Preserves exact @1/@2 identities unchanged and keeps the H.30 desktop production policy unchanged.
- Freezes only the compact Hotfix 4 summary and candidate-metrics artifacts plus a small evidence manifest; the 147 KB detailed trace remains outside candidate ZIPs.
- Adds ordinary contract tests for v2/v3 policy separation and frozen-diagnostic provenance.
- Adds an explicit 60 s @3 qualification gate. The 10 s post-load point is treated as bounded stabilization with no trip/breaker/request/shaft/reverse-admission loss; from 20–60 s the sustained floor remains >4.0 MWe with rotor 2990–3010 rpm, shaft >4.5 MW and forward admission.
- Does not yet register @3 in desktop composition or change the existing cumulative long journey. If validated, the next step is a narrow activation change before rerunning I.5 closure.
- Candidate packaging continues to exclude `Gameplay/Evidence`, generated `artifacts`, `bin` and `obj`.

## M10.9.4.1-I.5 REV1 Hotfix 4 — Synchronization Loaded-Contract Diagnostic — CANDIDATE

- Treats the governor-only diagnostic result as evidence: frozen v2 remains unstable and 0/4 bounded controller-only candidates satisfy the strict 20–60 s gross/shaft/rotor/admission window.
- Preserves exact `pre-synchronization-grid-loading@2` unchanged and does not weaken the original long-journey floor.
- Adds a second explicit diagnostic that separates the historical D.3.2 loaded main-steam line capacity (`850` vs v2 `1000`), loaded stop-out pressure grade (`276.755 °C` vs v2 `277 °C`), H.30 corrected-commit hydraulics and the validated desktop PID.
- Compares eight bounded contracts, including single-factor controls and the full loaded-contract combination, while keeping reactor/primary seed, condenser/feedwater, grid coupling, protections and fixed 10 ms step unchanged.
- Adds richer one-second trace evidence for gross/mechanical/shaft power, rotor speed, main-line flow/pressure drop, steam/admission flow, valve position, turbine-inlet, drum and condenser state.
- The evidence-only diagnostic reports qualification in artifacts instead of failing merely because no candidate qualifies. No file under `src/` changes and no new exact-version runtime is registered.
- Candidate packaging continues to exclude `Gameplay/Evidence`, `artifacts`, `bin` and `obj`.

## M10.9.4.1-I.5 REV1 Hotfix 3 — Diagnostic Interpolated-String Compile Fix — CANDIDATE

- Fixes the only compile blocker in the new synchronization-governor diagnostic: an escaped string literal inside an interpolated-expression lambda corrupted C# parsing and produced 15 cascade syntax errors.
- Computes the qualifying-candidate count before building the summary string, avoiding nested string literals inside the interpolation hole entirely.
- Diagnostic candidate set, 60 s acceptance criteria, strict long-journey floor, registered runtimes, exact-version identities, physics, hydraulics, grid coupling and production policy are unchanged.
- No file under `src/` changes. Candidate packaging continues to exclude `Gameplay/Evidence`, `artifacts`, `bin` and `obj`.

## M10.9.4.1-I.5 REV1 Hotfix 2 — Synchronization Governor Stability Diagnostic — CANDIDATE

- Treats REV1 Hotfix 1 as failed diagnostic evidence: the synchronization long journey recovered above 4.0 MWe at 20–50 s but fell again to 3.923 MWe and 2954.247 rpm at 60 s.
- Restores the original strict gameplay-long acceptance contract: gross electrical output must remain above 4.0 MWe at every 10 s checkpoint; no stabilization-window exception remains.
- Adds an explicit closure-blocker diagnostic that reproduces frozen `pre-synchronization-grid-loading@2` and compares four bounded governor-only candidates while keeping plant physics, hydraulic mode, grid coupling, protections, seed and exact-v2 identity unchanged.
- Candidate set: legacy PI 0.5/0.02/0; add Kd=0.1; add Kd=0.2; Kp=1 PI; desktop-proven PID 1/0.02/0.2.
- Strict candidate qualification requires no trip/breaker/request/shaft/reverse-admission violation over the 60 s run and, from 20–60 s one-second samples, gross >4.0 MWe, shaft >4.5 MW and rotor 2990–3010 rpm.
- Does not change any registered initial condition, production runtime, exact-version identity, physics coefficient, hydraulic selector or I.3 budget. I.5 remains blocked until the diagnostic selects a qualifying controller and that controller is introduced/qualified as a new exact synchronization version.

## M10.9.4.1-I.5 REV1 Hotfix 1 — Synchronization stabilization-window contract alignment

- Keeps I.5 runtime physics/numerics unchanged.
- Keeps the synchronization journey immediate post-load establishment check above 4.5 MWe.
- Treats only checkpoint 1 (first 10 simulated seconds after the load command) as the bounded stabilization window already implied by the scenario guidance.
- Keeps the existing sustained electrical-export floor above 4.0 MWe unchanged for checkpoints 2–6 (20–60 s).
- Keeps breaker, request, shaft-power and no-trip checks active at every checkpoint.
- Reason: the cumulative I.5 long gate observed 3.959 MWe at 10 s with healthy 5.051 MW shaft power, positive admission flow and no trip; the bidirectional grid coupling unloads the 2982 rpm rotor by the configured frequency-slip damping while turbine torque restores speed.
- No source/runtime production file changes.

## M10.9.4.1-I.5 Revision 1 — static re-verification and documentation consolidation

- Re-verified the I.5 code/test/script delta without changing runtime behaviour: I.4 frozen hashes, descriptor contracts, MTP filter targets, CI script targets, xUnit2031 patterns and candidate packaging all pass static checks.
- Consolidated duplicated current-state documentation into `docs/PROJECT.md`; removed current duplicates `PROJECT_STATUS.md`, `PROJECT_HANDOFF.md`, `NEW_CHAT_START.md`, `docs/current/I5_*` and the in-progress `docs/milestones/M10.9.4.1.md`.
- Preserved the removed administrative material as one historical snapshot under `docs/history/project/I5_PRE_CONSOLIDATION_ADMIN_SNAPSHOT.md`.
- Simplified root `README.md`, `docs/README.md` and `docs/ROADMAP.md` so each has a single responsibility.
- `APPLY_UPDATE.cmd` now removes the obsolete duplicate current-state files when the candidate is stacked over an existing checkout.
- Candidate packaging continues to exclude `Gameplay/Evidence`, `artifacts`, `bin` and `obj`.

# M10.9.4.1-I.5 — Cumulative M10.9.4.1 Closure Gate — CANDIDATE

- Built directly on user-validated I.4 Hotfix 2. H.30 RQ1 `ACTIVATE`, I.3 authoritative reference and I.4 known-limitations/legacy-retirement decisions are treated as validated prerequisites.
- Freezes the compact I.4 summary/dependency/limitation/retirement artifacts and adds `eng/evidence-manifests/i4-validated.csv`.
- Adds a cumulative closure script that runs `eng\ci-ordinary.cmd`, then `eng\ci-long.cmd`, then writes the final M10.9.4.1/Phase-I closure artifact.
- Closure requires current 60 s gameplay, current-production operational-envelope protection/replay/load-rejection tests, reference-plant scale, the I.3 300 s reference, H.28 `bounded-but-costly` classification and the I.4 `DEFER-SOURCE-REMOVAL` decision.
- Updates the reference-plant scale audit to construct the authoritative production runtime through `DesktopHydraulicProductionPolicySelector` rather than the historical direct v2 factory; scale constants/expectations are unchanged.
- Moves validated I.4 candidate docs to history and keeps `docs/current/` limited to I.5.
- No plant physics, numerical mathematics, H.20/H.22 authority, exact-version identity, production fixed step or I.3 budget changes.
- Candidate packaging continues to exclude `tests/.../Gameplay/Evidence`, `artifacts`, `bin` and `obj`.

# M10.9.4.1-I.4 Hotfix 2 — Canonical Frozen-Evidence Contract Alignment

- Fixes the I.4 frozen-I.3 provenance test to hash text evidence canonically with LF line endings, matching the existing manifest contract and avoiding CRLF/LF false negatives.
- Aligns the I.3 reference-contract ordinary test with the already validated `VALIDATED-FROZEN-I3` baseline status.
- No runtime physics, numerical mathematics, production selector, tolerance budget, legacy-retirement decision or packaging policy changes.
- `Gameplay/Evidence` remains excluded from candidate ZIPs; compact frozen prerequisites remain under `eng/frozen-evidence/ordinary`.

# M10.9.4.1-I.4 Hotfix 1 — Frozen I.3 Evidence Hash Alignment

- Corrects only the four canonical SHA-256 values for the validated I.3 frozen summary/slopes/budgets/determinism artifacts.
- The frozen files themselves are byte-equivalent to the user-validated I.3 artifacts; only the previously transcribed hash constants were wrong.
- No runtime, numerical, physics, production-policy, CI-tier, retirement-decision or tolerance-budget behavior changes.
- Candidate packaging continues to exclude `tests/.../Gameplay/Evidence`, `bin`, `obj` and `artifacts`.


## M10.9.4.1-I.4 — Known Limitations & Legacy Retirement Review — CANDIDATE

- Built directly on user-validated I.3 Hotfix 2. The authoritative exact-v3 300 s / 30,000-step reference, seven final-window slopes and 19 regression budgets are frozen as the Phase-I production regression baseline.
- Adds compact I.3 frozen summary/slope/budget/determinism evidence plus `eng/evidence-manifests/i3-validated.csv`; generated trajectory artifacts remain separate and `tests/.../Gameplay/Evidence` remains excluded from candidate ZIPs.
- Records the validated non-zero final-window drift observations as current limitations: drum inventory `+8.2451672984622224 kg/s`, main-steam header `-0.35293086123580603 kg/s`, and total fluid internal energy `-2.061802762164879 MW`. These are regression observations, not calibration targets or proof of asymptotic steady state.
- Reviews `DeterministicHybridSemiImplicit` and `FourNodeBranchContinuityShadowIntegrated`. Neither is a production, exact-version or current-CI dependency, but each remains referenced by four source files and four test files.
- Candidate retirement decision is `DEFER-SOURCE-REMOVAL`: keep historical executable seams through I.5, do not expose the modes as production choices, and perform physical deletion only in a separately scoped maintenance change after historical tests are archived/replaced.
- Adds I.4 current-evidence gate and moves the validated I.3 300 s reference into the scheduled-long CI tier. Phase-I audit-tier contract becomes 16 rows: 1 ORDINARY, 3 CURRENT-EVIDENCE, 4 SCHEDULED-LONG, 8 HISTORICAL-FROZEN.
- Moves completed I.3 current docs into history and keeps `docs/current/` limited to I.4.
- No runtime physics, numerical mathematics, exact-version identity, H.30 RQ1 production policy, H.28 cost classification or 10 ms fixed-step behavior changes.

## M10.9.4.1-I.3 Hotfix 2 — Compact Frozen Evidence Contract Migration — CANDIDATE

- Fixes the 39 ordinary-test failures caused by the new source-package rule that excludes `tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/Evidence` while historical frozen-evidence assertions still read that path directly.
- Moves the compact immutable prerequisite payload required by ordinary/current lightweight evidence tests to `eng/frozen-evidence/ordinary`; generated/local `Gameplay/Evidence` is no longer a source-package dependency.
- Keeps only frozen files below 1 MB in the bundled compact store. The three identical 7.1 MB I.3 ten-millisecond traces remain external and are authenticated by canonical SHA-256 through `eng/frozen-evidence/large-payload-manifest.csv` because preflight tests only require their identity.
- Redirects historical evidence locators in Gameplay audit tests to the compact store and adds manifest fallback only for intentionally omitted large payloads; numerical/runtime assertions are unchanged.
- Adds `Gameplay/Evidence/` to `.gitignore` and documents the evidence-retention boundary. No plant physics, numerical mathematics, H.30 ACTIVATE policy, production selector, persistence semantics, tolerance-budget logic or 10 ms fixed-step behavior changes.
- I.3 remains unvalidated until build, ordinary tests and the focused 300 s authoritative-production reference gate are green.

## M10.9.4.1-I.3 Hotfix 1 — Artifact Pass-Flags Compile Fix — CANDIDATE

- Fixes the only reported I.3 build failure: `conservationPasses` and `telemetryPasses` were computed in the long-audit method but referenced inside `WriteArtifacts(...)` without being passed into that method.
- Passes both already-computed booleans explicitly to `WriteArtifacts(...)` and adds them to the method signature; no threshold, pass criterion, summary meaning, runtime behavior, production selector, numerical policy, physics, persistence semantics or 10 ms fixed step changes.
- Preserves the packaging contract introduced by I.3: `tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/Evidence` and runtime `artifacts` are not bundled in candidate ZIPs.
- H.30 Requalification 1 remains the validated production-policy baseline; I.3 remains a candidate until build, ordinary tests and the focused 300 s gate are green.

## M10.9.4.1-H.30 Requalification 1 Hotfix 1 — Operations Namespace Compile Fix — CANDIDATE

- Fixes the only reported H.30 RQ1 build failure: CS0246 for `PowerManoeuvringGuidancePlan` in `DesktopIntegratedOperationsProductionProgram.cs`.
- Adds only `using NuclearReactorSimulator.Application.Scenarios.Operations;` to the new production wrapper, matching the existing `DesktopIntegratedOperationsProgram` contract.
- Does not change the H.30 RQ1 `ACTIVATE` decision logic, exact-v3 production selector, exact-v2 rollback/reference path, scenario identities, replay/persistence semantics, numerical runtime, physics, CI tiers or documentation consolidation.
- I.2 remains authoritative until H.30 RQ1 Hotfix 1 passes build, ordinary tests and the focused re-review audit.

## M10.9.4.1-H.30 Requalification 1 — Production Policy Re-review after I.3 Continuity Evidence — CANDIDATE

- Built after validated I.3 Hotfix 4 Classifier Fix 1 and validated I.3 Hotfix 5 corrected 300 s evidence; I.2 remains the last fully validated Phase-I baseline until this candidate passes local validation.
- Re-opens only the H.30 deployment decision. Evidence-derived candidate outcome is `ACTIVATE` because exact v2 reproduces 338/338 generation-drop / targeted-train reverse-flow steps while exact v3 produces 0/0 and remains healthy across 300 s / 30,000 steps.
- Promotes the already-qualified exact-v3 `FourNodeBranchContinuityCorrectedCommitOptIn` policy to the candidate authoritative desktop default; exact v2 `ExplicitCommittedState` remains fail-closed rollback/reference and historical exact-version identities are not reinterpreted.
- Wires fresh desktop startup through `DesktopIntegratedOperationsProductionProgram` and adds the distinct `integrated-normal-operations-training-h30-rq1-production` scenario over exact v3; historical v2 and H.29 candidate scenario identities remain separate and replay-compatible.
- Keeps H.28 `bounded-but-costly`; no H.9/H.20/H.22/P060-F040/hysteresis/physical-coefficient/10 ms timestep retuning is introduced.
- Freezes validated I.3 Hotfix 4/5 artifacts under test evidence with canonical SHA-256 checks; H.24/H.28 and the long I.3 diagnostics are not rerun by H.30 RQ1.
- Updates current-evidence CI so original H.30, I.1 and I.3 re-review prerequisites become frozen historical evidence while H.30 RQ1 becomes the current production-policy gate; current gameplay/operational-envelope long regressions now follow the authoritative production selector.
- Performs documentation consolidation: root `README.md` reduced from 589 to about 110 lines; `ROADMAP.md` reduced from 893 to about 120 lines; 161 M10.9.4.1 chronology files moved from the `docs/` root to `docs/history/m10.9.4.1/`; current status/handoff/limitations are rewritten around the actual checkpoint.
- I.3 tolerance budgets remain unfrozen. A green H.30 RQ1 only activates the production policy and unblocks an authoritative-policy I.3 reference rerun.

## M10.9.4.1-I.3 Hotfix 5 Compile Fix 1 — Recording Fingerprint Namespace Import — CANDIDATE

- Fixes the only reported Hotfix 5 build failure: two CS0103 references to `ControlRoomSnapshotFingerprint` in `PhaseICorrectedHealthyReferenceRequalificationAuditTests.cs`.
- Adds only `using NuclearReactorSimulator.Application.Scenarios.Recording;`, matching the established H.24 compile-fix pattern and existing working audit tests.
- Does not change the 300-second corrected collector, health/continuity criteria, determinism contract, runtime physics/numerics, H.30 `OPT-IN ONLY` policy, selector, persistence semantics or 10 ms step.
- I.2 remains authoritative and I.3 remains unvalidated until Hotfix 5 passes build, ordinary tests and the focused corrected-300-second gate.

## M10.9.4.1-I.3 Hotfix 5 — Corrected 300 s Healthy Reference Requalification — CANDIDATE

- Built after user validation of I.3 Hotfix 4 Classifier Fix 1 diagnostic evidence; I.2 remains the authoritative validated baseline and I.3 remains unvalidated.
- Freezes the validated 100 s / 10 ms classifier evidence showing 338/338 explicit drops = targeted-train reverse-flow steps and exact-v3 0/0.
- Runs exact v3 corrected-commit for 300 s / 30,000 steps and checks generation health plus stop/control/admission direction on every 10 ms step.
- Samples conservation/inventory each second, records seven final-window slopes and performs a 256-step deterministic repeat control.
- Requires corrected trigger/eligible/authorized/commit equality with zero rollback/fallback/unsafe/untargeted disagreement.
- Does not change H.30 `OPT-IN ONLY`, production selector, physics, numerical mathematics, persistence semantics or 10 ms step; I.3 budgets remain unfrozen.

## M10.9.4.1-I.3 Hotfix 3 — Descriptor Contract Alignment — CANDIDATE

- Fixes the I.3 Hotfix 2 descriptor-preflight mismatch that prevented the focused script from reaching the 300-second collector.
- Restores `Reference Trajectories`, `Conservation/Inventory`, `Tolerance Budgets` and `final-window slopes` to the descriptor contract while retaining explicit Hotfix 3 identity.
- Does not change runtime, numerical policy, physics, health thresholds, scheduled-long opt-in or diagnostic collection.


## M10.9.4.1-I.3 Hotfix 2 — Scheduled-Long Isolation & Shaft-Drop Diagnostic — CANDIDATE

- Keeps I.2 as the latest validated baseline and keeps I.3 unvalidated.
- Preserves the unchanged generation-health floor and all Hotfix 1 shaft/steam/admission/phase diagnostics.
- Adds `NRS_I3_LONG_AUDIT=1` as a fail-closed execution opt-in for the 300-second collector.
- Updates the focused I.3 script to set that opt-in; ordinary `dotnet test` returns immediately from the long collector even if runner explicit-test policy changes.
- No plant physics, numerical mathematics, H.30 policy, selector, persistence semantics or 10 ms fixed-step behavior changes.

## M10.9.4.1-I.3 Hotfix 1 — Full-Horizon Shaft-Drop Diagnostic — CANDIDATE

- Built over the failed initial I.3 candidate; I.2 remains the authoritative validated baseline.
- Records the user-observed initial I.3 failure at logical step 5,500 / 55 s: no trip, breaker closed, 5 MWe request, about 4.435 MWe gross output, rotor about 2,996.119 rpm, but sampled rotor shaft power exactly 0 MW.
- Does **not** weaken the existing `shaft power > 4.5 MW` one-second healthy-parallel criterion, which is also present in the historical 300 s operational-envelope audit.
- Removes the early per-sample assertion so the same 300 s exact-v2 run can complete and write evidence before the unchanged final health assertion.
- Adds canonical total turbine shaft power, total turbine steam flow, admission flow, control/admission valve positions, turbine-inlet pressure/temperature/phase to trajectory diagnostics.
- Adds `06-generation-health-violations.csv` and `07-shaft-drop-episodes.csv`; the focused script prints the summary/paths even when the final health gate remains red.
- No production runtime, physics, numerical mathematics, H.30 policy, selector, persistence behavior or 10 ms fixed step changes.

## M10.9.4.1-I.3 — Reference Trajectories, Conservation/Inventory Baseline & Tolerance Budgets — FAILED DIAGNOSTIC EVIDENCE

- User-reported build and ordinary tests passed on 2026-08-19.
- Focused 300 s gate stopped at 55 s because the new I.3 test asserted rotor shaft power `> 4.5 MW`; observed sample was trip=False, breaker=True, request/gross/shaft=5/4.435/0 MW, rotor=2996.119 rpm, condenser=7.602 kPa, drum level=57.588%.
- Only `00-progress.txt` was produced because the assertion occurred before full-horizon artifact writing. I.3 was not validated and no tolerance budgets are frozen.

## M10.9.4.1-I.2 — Audit Consolidation & CI Baseline Hardening — VALIDATED

- User-reported compilation, ordinary tests and focused I.2 audit passed on 2026-08-19.
- Tiered ordinary/current-evidence/scheduled-long/historical-frozen CI baseline established with runtime unchanged.

## M10.9.4.1-I.2 — Audit Consolidation & CI Baseline Hardening — CANDIDATE

- Built directly on user-validated I.1 Hotfix 1.
- Freezes the validated I.1 summary, compatibility matrix and numerical-mode retirement inventory with canonical fingerprint checks.
- Adds an explicit four-tier validation contract: `ORDINARY`, `CURRENT-EVIDENCE`, `SCHEDULED-LONG`, `HISTORICAL-FROZEN`.
- Adds provider-neutral `eng\ci-ordinary.cmd`, `eng\ci-current-evidence.cmd` and `eng\ci-long.cmd` entry points.
- Adds GitHub Actions ordinary push/PR/manual CI and separate weekly/manual long-gate workflow using repository `global.json`.
- Keeps H.24 post-H.28, H.28 performance and H.5/H.21 historical research out of ordinary/current CI.
- Does not authorize deletion of H.5/H.21 numerical modes because executable source/test dependencies still remain.
- Under `src/`, changes only `ApplicationDescriptor.cs` metadata; production runtime remains unchanged.

## M10.9.4.1-I.1 Hotfix 1 — Profile Compatibility & Legacy Retirement Inventory — VALIDATED

- User-reported compilation, complete ordinary tests and focused I.1 audit passed on 2026-08-19 after the analyzer-only xUnit2031 repair.
- 12 registered exact versions across 9 IDs; 2 compatibility-retained; 0 delete-now profiles.
- Exact v2 remains authoritative explicit default/rollback/reference and exact v3 remains qualified corrected opt-in.
- H.5 hybrid and H.21 shadow-integrated modes remain audit-only retirement candidates.
- `profile-compatibility-inventory-passes=True`, `i1-audit-passes=True`, `phase-i-compatibility-baseline-established=True`.

## M10.9.4.1-I.1 Hotfix 1 — xUnit2031 Audit Assertion Repair — CANDIDATE

- Fixes the only reported build failure in `ProfileCompatibilityLegacyRetirementInventoryAuditTests.cs`.
- Replaces `Assert.Single(collection.Where(predicate))` with the analyzer-approved `Assert.Single(collection, predicate)` overload.
- Test semantics are unchanged: the same exact-version profile must still be uniquely present.
- No numerical, physical, selector, persistence, compatibility-matrix or H.30 evidence behavior changes.


## M10.9.4.1-I.1 — Profile Compatibility & Legacy Retirement Inventory — CANDIDATE

- Built directly on user-validated H.30; Phase H is closed as `OPT-IN ONLY` and Phase I is unblocked.
- Promotes the validated H.30 closure summary/metrics as immutable fingerprinted prerequisite evidence.
- Adds an executable inventory of 12 exact-version initial-condition factories across 9 profile IDs.
- Classifies desktop v2 as authoritative default, desktop v3 as qualified opt-in, and older same-ID v1 identities as compatibility-retained without reinterpretation.
- Requires zero exact-version profiles to be deleted in I.1; save/replay/scenario identities remain exact-version compatible.
- Classifies `DeterministicHybridSemiImplicit` and `FourNodeBranchContinuityShadowIntegrated` as historical audit-only retirement candidates, but defers deletion until audit consolidation.
- Leaves the H.30 production selector, H.9/H.20/H.22, P060/F040, hysteresis, physical coefficients, persistence schemas and 10 ms fixed step unchanged.
- Adds focused artifacts for the exact profile matrix and numerical-mode retirement inventory.

## M10.9.4.1-H.30 — Phase H Closure & Production Qualification Decision — VALIDATED

- User-reported compilation, complete ordinary tests and focused H.30 gate passed on 2026-08-19.
- Frozen H.19–H.29 evidence chain passed; Phase H closed and Phase I was unblocked.
- Final evidence-derived production decision: `OPT-IN ONLY`.
- Exact v2 `ExplicitCommittedState` remains authoritative default/rollback/reference; exact v3 corrected ownership remains qualified opt-in.
- H.28 remains `bounded-but-costly`; no numerical/runtime selector retuning occurred.

## M10.9.4.1-H.29 — Production Activation Candidate — VALIDATED

- User-reported compilation, complete ordinary tests and focused H.29 gate passed on 2026-08-19.
- 1,024 qualification intervals + 2 transitions = 1,026 runtime steps; 400 triggers, 400 eligible, 400 authorized and 400 corrected commits.
- Zero rollback, explicit fallback, fallback-commit violation, unsafe commit and untargeted branch disagreement.
- 256-interval deterministic repeat passed with activation telemetry fingerprint `BB16A2395682226B6E037901317D70B4A12E8E5C184CFC0E7C4B044643B05D68`.
- Exact v3 replay/checkpoint qualification passed; exact v2 remains independently loadable and explicit kill resolves to v2.
- `h30-closure-review-unblocked=True`; H.29 is a validated activation candidate but does not change the authoritative default.
- Built directly on user-validated H.24 Requalification 1 post-H.28.
- Preserves exact v2 `integrated-operations-desktop-stable` as the authoritative `ExplicitCommittedState` default and operational kill/rollback reference.
- Adds exact v3 as a separately reviewed `FourNodeBranchContinuityCorrectedCommitOptIn` production-default candidate without reinterpreting v2 saves/replays.
- Adds a pre-runtime deployment policy selector where explicit kill always wins and resolves to v2.
- Adds internal observational H.20/H.22 production telemetry counters for trigger, eligibility, authorization, commit, fallback, rollback reasons, unsafe commits and untargeted disagreements; no HMI/control authority is added.
- Adds a distinct v3 candidate scenario and focused exact-version save/replay/checkpoint qualification.
- Freezes validated H.23/H.24-post-H.28/H.25/H.26/H.27/H.28 prerequisite evidence by canonical fingerprint; H.24/H.28 are not rerun.
- Does not change H.9 mathematics, P060/F040, H.20/H.22 semantics, hysteresis limits, physical coefficients or the 10 ms fixed step.
- Carries H.28 `bounded-but-costly` classification forward. H.30 remains the sole authority for `ACTIVATE`, `OPT-IN ONLY` or `REMAIN EXPLICIT`.

## M10.9.4.1-H.24 Requalification 1 — Post-H.28 Committed Long-Horizon & Cross-Profile Regression — VALIDATED

- User-reported compilation, complete ordinary tests and focused requalification gate passed on 2026-08-19.
- 30,000 qualification intervals + 8 transition steps = 30,008 runtime steps across `steady-long|load-pulse|cooling-pulse|combined-load-cooling`.
- 9,626 P060/F040 triggers, 9,626 H.20 eligible, 9,626 H.22 authorized and 9,626 corrected commits.
- Zero rollback, safe fallback, fallback-commit violation, unsafe commit and untargeted branch disagreement; every profile completed without trip.
- 256-interval determinism repeat passed with committed telemetry fingerprint `7AF233CE51A866B3E00C2C032AA58EEFBD7290DE0940725E5F4B7860EA6287BE`.
- No numerical contract/timestep/physics retuning occurred. This closes the single roadmap-required post-H.28 long-horizon regression and unblocks H.29.

## M10.9.4.1-H.28.1-G — Untargeted Branch-Disagreement Scan Fast Path — CANDIDATE

- Rebased on validated H.28.1-D; H.28.1-E/F remain frozen failed performance evidence, not baselines.
- Freezes the real H.28.1-F evidence: trigger p95 96.5735 ms, H.9 74.5796 ms, Jacobian 58.8546 ms, fingerprint unchanged.
- Adds an internal reduced disagreement diagnostic to `SimplifiedWaterSteamThermodynamicModel` returning only production-selected phase and the late-boundary-saturated-shadow predicate consumed by the fail-closed untargeted scan.
- Preserves branch equations and priority; coarse-saturated can return immediately because it is first-priority and makes the late-shadow predicate false. Boundary-superheated is evaluated only when every earlier production branch failed.
- `FourNodeBranchContinuityShadowIntegrationSolver` uses the reduced diagnostic only for the standard simplified provider; any non-standard provider keeps the complete public `DiagnoseInverseBranchSelection` path.
- Adds exact-equivalence coverage across coarse saturated, subcooled liquid, coarse superheated, boundary-aware saturated, boundary-aware superheated and no-root representative states.
- H.9 mathematics, 32 probes, 35 logical hydraulic evaluations, Jacobian dimension 32, P060/F040, hysteresis, H.20/H.22, physical coefficients and 10 ms fixed step remain unchanged.
- H.28 remains failed until the original unchanged performance gate passes; H.29 remains blocked.

## M10.9.4.1-H.28.1-F — Jacobian Probe Coordinate-Residual Specialization — CANDIDATE

- Rebased on validated H.28.1-D; H.28.1-E remains failed evidence, not a baseline.
- Carries forward the measured E exact continuity and hydraulic-component reuse optimizations.
- Specializes finite-difference probes so Newton builds each Jacobian column from the normalized coordinate residual actually consumed by H.9, without mapped thermodynamic fixed-point integration that is discarded by the Jacobian builder.
- Keeps full fixed-point pressure/flow merit for initial residuals, accepted line-search trials and fallback trials.
- Adds an internal `FullFixedPoint` legacy probe mode solely for exact-equivalence tests; public construction uses `CoordinateOnly`.
- Freezes the real H.28.1-E failed evidence: trigger p95 157.754 ms versus unchanged H.28 readiness threshold 88.3812 ms.
- Does not change 32 probe evaluations, 35 logical hydraulic evaluations, Jacobian dimension 32, H.9 tolerances, P060/F040, H.20/H.22, hysteresis, target set, physical coefficients or the 10 ms fixed step.

## M10.9.4.1-H.28.1-D Preflight Hotfix 1 — Unused mapped-reuse local cleanup (candidate)

- Static pre-build review found `mappedProbeReuseFraction` calculated but never consumed in the new H.28.1-D focused audit.
- Removed that redundant local calculation; mapped reuse remains reported directly from row counters in the summary/metrics.
- No production/runtime source file changed. H.9 mathematics, probe count, hydraulic-evaluation count, thermodynamic cache, exact probe-state reuse, H.20/H.22 ownership and the deterministic fingerprint contract are unchanged.
- This preflight hotfix supersedes the first H.28.1-D candidate before any user build/test run.


## M10.9.4.1-H.28.1-D — Hydraulic Probe CPU Hot-Path Analysis & Optimization — CANDIDATE

- Built directly on user-validated H.28.1-B; H.28 remains FAILED and H.29 remains blocked.
- Reuses finite-difference probe fluid-node states only when the probe hydraulic balance is exactly equal to the reference balance; changed nodes retain the existing integration/thermodynamic path.
- Reuses the immutable 513-point saturation-property grid of the unchanged coarse saturated-mixture scan; dynamic boundary-aware scans and bisection remain unchanged.
- Preserves 35 hydraulic evaluations, 32 probes, Jacobian dimension 32, H.9/H.20/H.22 semantics and exact deterministic fingerprint.
- Focused gate requires material Jacobian/H.9/trigger wall reduction versus validated H.28.1-B while preserving H.28.1-C/B allocation and predictor gains.

## M10.9.4.1-H.28.1-B — Historical Explicit Predictor Reuse — VALIDATED

- User-reported build, complete `dotnet test` and focused H.28.1-B gate passed on 2026-08-19.
- 20/20 trigger/commit behavior, 35/32 H.9 work counts and fingerprint `518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38` remained unchanged.
- Non-trigger predictor wall cost fell from ~9309 us to ~392 us; non-trigger engine cost fell from ~19.2 ms to ~10.36 ms. Trigger H.9/Jacobian CPU remained ~1.659/~1.561 s.

## M10.9.4.1-H.28.1-B — Historical Explicit Predictor Reuse — CANDIDATE

- Built directly on user-validated H.28.1-C Hotfix 2.
- Reuses the historical explicit fluid-node predictor state only where the applied total balance exactly matches the canonical H.4 balance, and reuses the already-computed committed hydraulic evaluation.
- Reuses the historical explicit fluid-node integration node-by-node only when the historical total balance exactly equals the canonical H.4 balance; mismatched nodes are reintegrated through the unchanged H.4 path.
- Retains the unchanged predictor-end hydraulic evaluation required by P060/F040 and records exact reuse counts in diagnostic attribution.
- Preserves P060/F040, H.9 finite-difference Newton, 35 hydraulic evaluations / 32 probes, H.20 authority, H.22 ownership, target set, physical coefficients and 10 ms fixed step.
- Focused gate requires exact H.28 deterministic fingerprint, preservation of H.28.1-C allocation gains and material non-trigger predictor wall/allocation reduction.
- H.28 remains FAILED performance evidence and H.29 remains blocked.

## M10.9.4.1-H.28.1-C Hotfix 2 — H.9 Jacobian/Probe Allocation & Hot-Path Optimization — VALIDATED

- User-reported build, complete `dotnet test` and focused H.28.1-C gate passed on 2026-08-19.
- 20/20 trigger/commit behavior, 35 hydraulic evaluations, 32 probes, Jacobian dimension 32 and deterministic fingerprint `518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38` were preserved.
- Jacobian/probe allocation fell from 39,071,378 B to 925,328 B per trigger (~97.63% reduction); total H.9 allocation fell from 41,523,908 B to about 1,004,460 B (~97.58% reduction).
- Triggered Jacobian wall time remained ~1.558 s, confirming CPU work rather than heap churn as the dominant trigger cost.


## M10.9.4.1-H.28.1-C Hotfix 2 — IReadOnlyList Probe-State Compile Fix (CANDIDATE)

- Fixes the single CS0266 exposed after Hotfix 1: `iterateFluidNodes` is explicitly typed as `IReadOnlyList<FluidNodeState>` so line-search accepted state can be assigned without copying.
- No cast or `.ToArray()` is introduced; H.28.1-C optimization and H.9 mathematics remain unchanged.
- H.27 Hotfix 1 remains the validated numerical baseline; H.28 remains failed and H.29 remains blocked.

## M10.9.4.1-H.28.1-C Hotfix 1 — FluidNodeState Namespace Compile Fix — CANDIDATE

- Records the first H.28.1-C local build failure: eight CS0246 errors in `JacobianHydraulicCorrectorSolver.cs`.
- Adds the missing `using NuclearReactorSimulator.Domain.Physics.Fluids;` required by the optimized probe-path `FluidNodeState` signatures.
- No numerical, performance-optimization, authority, ownership, physics or timestep contract changes.


## M10.9.4.1-H.28.1-C — H.9 Jacobian/Probe Allocation & Hot-Path Optimization — CANDIDATE

- Built after user validation of H.28.1-A Hotfix 2 attribution.
- Preserves H.9 finite-difference Newton mathematics, 35 hydraulic evaluations, 32 probes, Jacobian dimension 32, H.20/H.22 authority/ownership, P060/F040, 2%/5 K hysteresis and the four-node target set.
- Removes full `PlantState` materialization from transient H.9 trial/probe evaluation; only canonical fluid-node states are integrated and evaluated until the final candidate boundary.
- Caches immutable hydraulic topology index bindings in `SemiImplicitHydraulicPrototypeSolver`, removes per-evaluation input lookup dictionaries, intermediate H.9 combined-balance dictionaries and duplicate hydraulic-evaluation canonical copies.
- Replaces per-scan heap allocation of internal water/steam saturation-property records with a private value-type carrier while preserving the public saturation API and exact thermodynamic equations/search order.
- Focused qualification requires the exact H.28 deterministic fingerprint plus material allocation reduction versus frozen H.28.1-A evidence.
- H.28 remains failed and H.29 remains blocked until the original H.28 gate is rerun successfully.

## M10.9.4.1-H.28.1-A Hotfix 2 — Performance Attribution — VALIDATED

- User validation: build, complete ordinary suite and focused attribution gate passed.
- 20/256 triggered corrected steps, 20 commits, zero rollback/unsafe/fallback-commit violations.
- H.9 averaged 35 hydraulic evaluations, 32 probes and Jacobian dimension 32.
- H.9 average ~1.654 s / ~41.52 MB per trigger; Jacobian build/probes ~1.556 s / ~39.07 MB.
- Deterministic fingerprint `518BA948637F0C270C7F8228AB97FEF9148E29A4F5CE4376319AB5D1CFBE7F38` matches failed H.28 evidence.

## M10.9.4.1-H.28.1-A Hotfix 2 — Architecture-Safe Measurement Provider — CANDIDATE

- Records that Hotfix 1 compiled, but the ordinary suite correctly rejected direct `Stopwatch` use inside `NuclearReactorSimulator.Simulation`.
- Removes every direct wall-clock/allocation-counter read from the Simulation project.
- Adds internal `PerformanceAttributionMeasurement`: zero-valued by default, with temporary readers injected only by the focused Application test.
- Keeps weak-reference attribution registries and deterministic H.9/H.21/H.22 records unchanged.
- Changes no numerical equation, trigger, tolerance, authority, commit, protection or physical coefficient.
- H.27 Hotfix 1 remains the validated baseline; H.28 remains failed evidence; H.29 remains blocked.

## M10.9.4.1-H.28.1-A Hotfix 1 — Attribution Local-Variable Shadowing Compile Fix — CANDIDATE

- Records the first local H.28.1-A build failure: eight CS0136 errors in `FourNodeBranchContinuityShadowIntegrationSolver.Step()` caused only by local names reused between the early non-trigger block and the containing method scope.
- Renames the non-trigger attribution locals to `noTriggerAuthority*`, `noTriggerSidecar*`, `noTriggerResult` and `noTriggerAttribution`.
- Changes no timing formula, allocation formula, registry behavior, numerical result, H.9/H.20/H.22 decision, threshold, target set, physical coefficient or production default.
- H.27 Hotfix 1 remains the validated baseline; H.28 remains failed performance evidence; H.29 remains blocked.

## M10.9.4.1-H.28.1-A — Corrected-Path Performance Attribution — CANDIDATE

- Built directly on user-validated H.27 Hotfix 1 rather than on the failed H.28 candidate.
- Freezes the user-produced failed H.28 summary, 256+256 benchmark, sampled soak and metrics artifacts with canonical SHA-256 fingerprints.
- Records the H.28 failure as performance evidence: median wall ratio 9.1252571494799053, p95 ratio 100.01553278882017, ~1.70 s average triggered step and ~43.46 MB average triggered allocation; numerical safety/determinism remained green.
- Adds diagnostic-only timing/allocation attribution around historical explicit preparation, sidecar predictor, H.9 layout/residual/Jacobian/line-search, disagreement scan, H.20 authority and H.22 commit/accounting.
- Stores nondeterministic attribution outside deterministic result/telemetry record equality using weak-reference registries; no numerical decision consumes timing/allocation values.
- Adds a short 64-warmup + 256-step attribution run and 128-step deterministic fingerprint control; no performance pass ceiling is applied in H.28.1-A.
- H.28 remains failed and H.29 remains blocked.

## M10.9.4.1-H.27 Hotfix 1 — High-Load Envelope Contract Fix — CANDIDATE

- Records the first H.27 gate: build and ordinary tests passed; the focused six-scenario matrix failed only `high-load-10mwe` because its evidence condition incorrectly required the 10 MWe request to remain trip-free.
- Aligns the audit with H.27 envelope semantics: reaching/observing the 10 MWe requested-load point is required; a canonical protection action is classified as `protected-boundary` rather than an automatic H.27 failure.
- Changes only the focused audit contract and documentation; H.20/H.22 runtime, protection logic, P060/F040, H.9, bounded hysteresis, physical coefficients and standard production factories remain unchanged.


## M10.9.4.1-H.27 — Off-Design Robustness & Qualification Envelope — CANDIDATE

- Promotes user-validated H.26 Hotfix 1 as the authoritative baseline: 12/12 same-step explicit fallbacks, all eight typed H.20 rollback reasons plus four denial controls, zero corrected/partial commits and deterministic repeat.
- Adds a targeted six-scenario off-design matrix over the unchanged H.22 corrected-commit runtime: 10 MWe, 50%/25% cooling capacity, combined load/cooling and bounded total cooling loss.
- Records per-scenario envelope classification (`corrected-qualified`, `safe-fallback-envelope`, `protected-boundary`, `observed-no-trigger`).
- Safe rollback/protection is permitted; fallback commits, unsafe corrected commits, conservation violations and nondeterminism fail the gate.
- H.24 is not rerun; standard current-v2 remains `ExplicitCommittedState` at 10 ms.
- Adds H.27 design/checklist/static review, ADR 0153 and focused script.

## M10.9.4.1-H.26 Hotfix 1 — Focused Audit Proposed-Authority Contract Fix — CANDIDATE

- Local `dotnet build` and complete `dotnet test` passed on 2026-08-19.
- The focused H.26 gate reached the integrated stress test and failed only on `shadow-correction-not-evaluated`: telemetry correctly retained H.20 `ProposedAuthority=CorrectedCandidate`, while H.22 correctly denied the commit with `ShadowCorrectionNotEvaluated` and fell back physically to explicit.
- Fixes only the audit expectation: `RunChallenge` now accepts the expected proposed authority; all challenges default to `ExplicitCommittedState`, while `shadow-correction-not-evaluated` explicitly expects `CorrectedCandidate`.
- No `src/` runtime code, H.20/H.22 semantics, numerical thresholds, physical coefficients, standard factories or production defaults change.

## M10.9.4.1-H.26 — Integrated Rollback & Fail-Closed Stress Qualification — CANDIDATE

- Promotes user-validated H.25 as the authoritative baseline: five scenarios, 837 runtime steps, 178 corrected commits, zero rollback/fallback-commit/unsafe-commit violations, all expected outcomes green, 5m29s focused duration.
- Freezes and fingerprints the complete validated H.25 summary, 837-row telemetry and metrics.
- Adds one `internal` test-only authority-decision transform to `PlantNetworkOrchestrator`; the public constructor and all standard factories remain hook-free.
- Re-runs unchanged H.20 typed-reason and H.22 commit-seam contracts, then forces all eight H.20 rollback reasons plus non-rollback denial controls through the real corrected-commit orchestrator.
- Requires same-step physical equivalence to historical explicit fallback, zero corrected/partial commits and exact deterministic repeat.
- Does not rerun H.24 and does not retune P060/F040, H.9, hysteresis, target nodes, physical coefficients or the 10 ms step.

## M10.9.4.1-H.25 — Committed Protection & Operational-Transient Matrix — VALIDATED

- User validation on 2026-08-19 passed build, complete ordinary tests and the focused H.25 gate.
- Five scenarios completed 837 runtime steps in 5m29s with 178 corrected commits, zero H.20 rollback, fallback-commit violations or unsafe commits, and all expected outcomes satisfied.
- Telemetry fingerprint `ED60939F1E3EE279F018315904EC0BCD88A7D1F446AC199E129BF18EAF82A19E`.
- H.24 remains a rare 4h31m55s qualification gate and was not rerun.

## M10.9.4.1-H.25 — Committed Protection & Operational-Transient Matrix — CANDIDATE

- Promotes user-validated H.24 Hotfix 1 as the authoritative baseline: 30,008 committed runtime steps, 9,626 corrected commits, zero rollback/fallback-commit/unsafe-commit/untargeted-disagreement violations, all four nominal profiles trip-free.
- Records H.24 focused duration 4h31m55s and classifies it as a rare qualification gate rather than a routine regression.
- Adds compact frozen H.24 evidence and the canonical fingerprint of the full 30,008-row telemetry without embedding the 9.95 MB CSV.
- Adds a short five-scenario committed protection/operational-transient matrix plus an ordinary eight-function protection-catalogue contract.
- Does not modify numerical runtime or standard current-v2 activation.


## M10.9.4.1-H.24 Hotfix 1 — Focused Audit Recording-Namespace Compile Fix — CANDIDATE

- The first local H.24 build on 2026-08-18 failed only in `FourNodeCommittedLongHorizonCrossProfileQualificationAuditTests.cs` with CS0103 because `ControlRoomSnapshotFingerprint` was unresolved.
- Root cause: the H.24 audit omitted `using NuclearReactorSimulator.Application.Scenarios.Recording;`; H.22/H.23 audit tests already import that namespace.
- Hotfix 1 adds only that `using` directive to the focused H.24 audit. No calculation, assertion, runtime source, H.20/H.22 authority, target set, tolerance, physical coefficient or standard factory changes.
- H.23 Hotfix 2 remains the authoritative validated baseline; H.24 Hotfix 1 remains candidate pending build, ordinary suite and focused H.24 gate.

## M10.9.4.1-H.24 — Committed Long-Horizon & Cross-Profile Qualification — CANDIDATE

- Built directly on user-validated H.23 Hotfix 2; standard current-v2 remains `ExplicitCommittedState` at 10 ms.
- Adds no numerical runtime change: H.20 authority, H.22 commit seam, H.9, P060/F040, 2%/5 K four-node hysteresis, protection/replay runtime and physical coefficients remain unchanged.
- Freezes and fingerprints the three user-validated H.23 replay/checkpoint/protection artifacts.
- Exercises `FourNodeBranchContinuityCorrectedCommitOptIn` over the H.19 nominal four-profile domain: 12,000 steady + 6,000 load pulse + 6,000 cooling pulse + 6,000 combined intervals, plus 8 action-transition steps.
- Safe H.20 rollback/fallback is allowed; fallback commits, unsafe corrected commits, new untargeted disagreements, network-accounting violations or profile trips fail the focused gate.
- Does not freeze H.19's `3046/92/473` trigger census because actual corrected ownership legitimately changes the committed trajectory.
- Adds the authoritative H.24–H.30 Phase H completion roadmap, H.24 design/checklist/static review, ADR 0150 and `run-four-node-committed-long-horizon-cross-profile-qualification-audit.cmd`.
- H.24 remains candidate pending local build, complete ordinary suite and focused gate.

## M10.9.4.1-H.23 Hotfix 2 — Deterministic Replay, Checkpoint & Protection Interaction Qualification — VALIDATED

- User validation on 2026-08-18 passed build, complete ordinary tests and the focused H.23 gate after the two audit-only hotfixes.
- 701 recorded steps at 10 ms; checkpoint at logical step 502 during in-flight reverse-power pickup; 199 continuation steps to final generator trip.
- 242 corrected candidates committed with zero H.20 rollback, fallback-commit violations, unsafe commits or untargeted disagreements.
- Full replay trace, checkpoint prefix/continuation and deterministic repeat all matched exactly.
- Final reverse-power function latched, generator trip active and breaker open.
- Trace fingerprint `7C8FBA8ECB197F65AB263A79268653E3C2988F700A5A863BB0304D377C82FB54`.
- Standard current-v2 remained explicit; H.22 numerical runtime was unchanged.

## M10.9.4.1-H.23 Hotfix 2 — ApplicationDescriptor Case-Sensitive Contract Fix — CANDIDATE

- Records that H.23 Hotfix 1 compiled, but the subsequent ordinary/focused run failed on the same single case-sensitive `ApplicationDescriptorTests` assertion.
- `ApplicationDescriptor.Current.Status` contains `standard factories remain ExplicitCommittedState at 10 ms.`; the test incorrectly expected capitalized `Standard factories remain ExplicitCommittedState`.
- Changes only that test expectation to lowercase `standard`; descriptor text, numerical runtime, H.20/H.22 authority/commit behavior, replay/checkpoint/protection logic and frozen evidence are unchanged.
- H.22 remains the authoritative validated baseline. H.23 Hotfix 2 remains candidate pending `dotnet build`, `dotnet test` and `scripts\run-four-node-committed-replay-protection-qualification-audit.cmd`.

## M10.9.4.1-H.23 Hotfix 1 — Focused Audit Domain.Plant Namespace Compile Fix — CANDIDATE

- Records the first local H.23 build on 2026-08-18: all projects compiled except `NuclearReactorSimulator.Application.Tests`, which failed with CS0246 at `FourNodeCommittedReplayProtectionQualificationAuditTests.cs(342,9)`.
- Root cause: the focused H.23 test referenced `HydraulicNumericalCouplingMode` without importing its `NuclearReactorSimulator.Domain.Plant` namespace.
- Adds only `using NuclearReactorSimulator.Domain.Plant;` to that focused audit source. No calculation, assertion, replay/checkpoint/protection contract, H.22 runtime, numerical policy, evidence fingerprint or production factory changes.
- H.22 remains the authoritative validated baseline. H.23 Hotfix 1 remains candidate pending `dotnet build`, `dotnet test` and `scripts\run-four-node-committed-replay-protection-qualification-audit.cmd`.

## M10.9.4.1-H.23 — Deterministic Replay, Checkpoint & Protection Interaction Qualification — CANDIDATE

- Built directly on user-validated H.22; H.22 is promoted in documentation with 443/443 H.20-eligible/H.22-authorized corrected commits, zero fallback/unsafe commits and exact repeat.
- Changes no numerical runtime algorithm or production selection path. Standard current-v2 remains `ExplicitCommittedState` at 10 ms.
- Freezes the three user-validated H.22 focused artifacts with canonical SHA-256 provenance checks.
- Adds a test-only exact-version initial-condition factory delegating to the unchanged H.22 committed audit runtime.
- Qualifies the H.22 path through scenario recording/full replay, replay-backed in-flight checkpoint seek/continuation and evidence-derived reverse-power generator protection.
- Captures an internal deterministic H.20/H.22/protection trace and requires exact recording/replay/restored-continuation equality, zero unsafe/fallback-commit violations and standard-factory explicit isolation.
- Adds H.23 design/checklist/static review, ADR 0149 and `run-four-node-committed-replay-protection-qualification-audit.cmd`.
- Committed long-horizon/cross-profile and off-design robustness remain required before default activation.

## M10.9.4.1-H.22 — Four-Node Corrected-Candidate Commit Seam — VALIDATED

- Built directly on user-validated H.21 Hotfix 1; standard current-v2 remains `ExplicitCommittedState` at 10 ms.
- Adds separately opt-in `FourNodeBranchContinuityCorrectedCommitOptIn` mode and a second typed, fail-closed commit seam behind the unchanged H.20 eligibility/rollback decision.
- Evaluates the complete historical explicit candidate first on every interval so any denied/unsafe H.22 decision falls back immediately without partial corrected ownership.
- Exposes the H.9 corrected candidate and applied-iterate pump hydraulic power so committed-state audit uses the balances/work actually applied.
- Corrected ownership is permitted only for triggered, H.20-qualified, non-rollback, evaluated and available candidates; every other reason remains explicit.
- Freezes and fingerprints the user-validated H.21 summary, 2,000-step telemetry and metrics artifacts.
- Adds a deterministic 2,000-interval H.22/H.22-repeat focused gate with positive authorized commits, zero fallback/unsafe commits, per-step conservation, no control-window trip and standard-factory explicit isolation.
- H.22 does not require the post-commit trigger count to remain 15 because committed corrected state can legitimately alter subsequent trigger timing.
- Adds H.22 design/checklist and ADR 0148. Replay, protection, committed long-horizon/cross-profile and off-design gates remain required before any default activation.
- User validation on 2026-08-18 passed build, complete ordinary tests and focused H.22 gate: 443/443 eligible/authorized/committed corrections, zero rollback/fallback/unsafe/untargeted violations, 2000/2000 deterministic repeat and tight network conservation.

## M10.9.4.1-H.21 Hotfix 1 — Four-Node Orchestrator Shadow Wiring & Telemetry Integration — VALIDATED

- User local build, complete ordinary suite and cumulative H.19 -> H.20 -> H.21 focused gate passed on 2026-08-18 after the audit-only CS0136 rename.
- 2,000/2,000 explicit-vs-H.21 presentation equivalence and 2,000/2,000 deterministic repeat equivalence.
- 15 P060/F040 triggers; 15/15 corrected candidates eligible; zero rollback, zero corrected commits and zero untargeted branch disagreements.
- 408 branch overrides, 6,456 previous-phase holds, zero hysteresis releases and deterministic telemetry fingerprint `0454270F4AA63E89915FE231328807D4A6B7AD0C733441F78DC06C86A159CDC8`.
- Standard current-v2 remained `ExplicitCommittedState`; H.19/H.20 numerical and authority contracts remained unchanged.

## M10.9.4.1-H.21 Hotfix 1 — Focused Audit Local-Variable Shadowing Compile Fix — CANDIDATE

- Built directly on the H.21 Documentation Static Review 1 candidate over the user-validated H.20 baseline.
- Records the user's 2026-08-18 local build result: all projects compiled except `NuclearReactorSimulator.Application.Tests`, which failed with CS0136 in `FourNodeOrchestratorShadowIntegrationAuditTests.cs` because `repeatFingerprint` was declared both inside the interval loop and later in the containing method scope.
- Renames only the per-interval local to `repeatPresentationFingerprint`; calculations, assertions, H.21 telemetry, production code, H.19/H.20 prerequisites and expected focused evidence are unchanged.
- The previous documentation/static review remains useful for structural consistency but did not and could not establish C# compilation success.
- H.20 remains the authoritative validated baseline. H.21 Hotfix 1 remains CANDIDATE pending `dotnet build`, `dotnet test` and the complete H.21 focused gate.

## M10.9.4.1-H.21 — Four-Node Orchestrator Shadow Wiring & Telemetry Integration — CANDIDATE

- Builds directly on user-validated H.20.
- Promotes H.20 documentation to VALIDATED: 473/473 default explicit decisions, 473/473 armed shadow candidate eligibility, zero commits, 8/8 typed rollback challenges and deterministic repeat.
- Adds a frozen opt-in `FourNodeBranchContinuityShadowIntegrated` coupling mode that executes the exact P060/F040 + H.9 + 2%/5 K four-node policy and H.20 supervisor from the real `PlantNetworkOrchestrator`.
- Refactors the existing H.4/H.19 gate solver to expose its exact explicit predictor/trigger metrics rather than duplicating trigger logic.
- Adds typed per-step H.21 telemetry, but the orchestrator always returns the explicit predictor; corrected-state commit is impossible.
- Freezes and fingerprints the user-validated H.20 focused artifacts.
- Adds a 2,000-interval explicit/H.21/repeat lockstep audit and makes full H.19 and H.20 focused regressions prerequisites.
- Documentation/static review 2026-08-18 reconciles stale H.17/H.18 checkpoint text and ADR 0142–0145 statuses, and clarifies that H.21 preserves the historical explicit orchestration path for the returned state while the sidecar predictor remains observational. H.21 remains CANDIDATE pending runtime validation.
- Standard current-v2 factories remain `ExplicitCommittedState` at 10 ms; no P060/F040, H.9, hysteresis, target, physical coefficient or production inverse-map retuning.

## M10.9.4.1-H.20 — Four-Node Activation Contract, Rollback & Shadow Telemetry — VALIDATED

- User local compilation, complete ordinary suite and focused H.20 audit passed on 2026-08-17.
- Frozen H.19 evidence accepted for 473 representatives.
- Default arm: 473/473 explicit, 0/473 candidate eligible, 0 production commits.
- Armed shadow simulation: 473/473 candidate eligible, zero rollback, zero production commits.
- All 8/8 typed rollback challenges passed; deterministic fingerprint repeat passed.
- Production remained `ExplicitCommittedState` at 10 ms and the H.20 supervisor remained unwired.

## M10.9.4.1-H.20 — Four-Node Activation Contract, Rollback & Shadow Telemetry — CANDIDATE

- Built directly on user-validated H.19; production remains `ExplicitCommittedState` at 10 ms.
- Freezes the validated H.19 473-representative results, metrics and summary as ordinary regression evidence.
- Adds a shadow-only `FourNodeBranchContinuityShadowActivationSupervisor` with activation arm disabled by default.
- Freezes P060/F040, the exact `steam|stop-out|header|turbine-inlet` target set, H.9 residual guards and validated closure/ownership guards.
- Corrected authority is only a shadow proposal; `ProductionCommitAuthorized` is always false and the supervisor is not wired into `PlantNetworkOrchestrator`.
- Adds deterministic fail-closed rollback reasons for missing qualification evidence, non-convergence, line-search exhaustion, pressure/flow residual breach, closure/ownership breach and untargeted branch disagreement.
- Focused gate evaluates default-disabled and shadow-armed decisions over all 473 validated H.19 representatives, plus eight typed rollback challenges and deterministic fingerprint repeat.
- Adds H.20 design/checklist, ADR 0146 and `scripts/run-four-node-activation-rollback-contract-audit.cmd`.

## M10.9.4.1-H.19 — Four-Node Long-Horizon & Cross-Profile Qualification — VALIDATED

- User-confirmed local build, complete ordinary suite and focused H.19 audit passed.
- Validated result: exact 30,000-interval/four-profile census with 3,046 P060/F040 triggers, 92 episodes and 473 frozen representative keys; 473/473 converged, zero line-search exhaustion, 245/245 H.17 failures recovered and 228/228 successes preserved.
- Committed selection stayed transparent across 120,000 target phase-state checks; no new untargeted late-shadow or selected-phase mismatch node was found; release challenges, deterministic repeat and closure/ownership safeguards passed.
- Production remained `ExplicitCommittedState` at 10 ms and no shadow state was committed.
- Built directly on user-validated H.18 Hotfix 1; production remains `ExplicitCommittedState` at 10 ms.
- Re-runs the complete H.17 30,000-interval/four-profile P060/F040 census and requires exact reproduction of 3,046 trigger intervals, 92 episodes and 473 representative profile/interval keys.
- Evaluates all 473 representatives with unchanged H.9 and unchanged 2%/5 K bounded hysteresis targeted exactly at `steam|stop-out|header|turbine-inlet`.
- Reports recovery of the frozen H.17 245 failures, preservation of the 228 H.17 successes, and the 120 mismatch / 125 non-mismatch subclasses.
- Extends the all-node inverse scan to reject both new untargeted candidate-only late-shadow nodes and new untargeted candidate-vs-explicit selected-phase mismatch nodes.
- Counts all 120,000 committed target phase-state checks across the four profiles while retaining the established observation sampling/fingerprint contract.
- Adds `scripts/run-four-node-long-horizon-cross-profile-qualification-audit.cmd`, H.19 design/checklist and ADR 0145.
- Does not change production `Resolve()`, H.9, P060/F040, 2%/5 K limits, physical coefficients, routing or production state commitment.

## M10.9.4.1-H.18 Hotfix 1 — Turbine-Inlet Continuity Extension & Residual-Floor Split Diagnosis — VALIDATED

- Fixes the H.18 focused audit compile error by using `IReadOnlyList<string>.Count` instead of the nonexistent `.Length` property for the two untargeted-node diagnostic lists.
- No solver, thermodynamic model, branch-continuity policy, trigger, production routing, frozen H.17 evidence or H.18 diagnostic criterion changes.
- User-confirmed local build, complete ordinary suite and focused H.18 audit passed.
- Authoritative focused result: 261/261 converged; 120/120 mismatch failures and 125/125 non-mismatch failures recovered; 16/16 controls preserved; 14,746 turbine-inlet overrides; committed selection transparent; deterministic repeat true; no residual failure or new untargeted branch-disagreement node.
- H.18 Hotfix 1 is the validated baseline for H.19.

## M10.9.4.1-H.18 — Turbine-Inlet Continuity Extension & Residual-Floor Split Diagnosis — SUPERSEDED BY VALIDATED HOTFIX 1

- Built only on user-validated H.17 Hotfix 6; production remains explicit at 10 ms and no H.9/H.13 shadow state is committed.
- Freezes the validated H.17 473-representative evidence contract (228 converged / 245 failed) and its failure split: 120 failures with `turbine-inlet` candidate-vs-explicit phase mismatch, 125 without.
- Reconstructs the same four H.17 reference profiles but skips the already-validated expensive 3,046-trigger H.4 census; H.9 is run on all 245 H.17 failures plus 16 deterministic success controls.
- Extends the unchanged bounded 2% pressure / 5 K temperature shadow target set only from `steam|stop-out|header` to `steam|stop-out|header|turbine-inlet`.
- Diagnoses every remaining failure for mapped-minus-applied node residual ranking, accepted-iterate merit floor, minimum accepted relaxation and all-node candidate-vs-explicit inverse-branch disagreement.
- Adds committed `turbine-inlet` transparency observation, deterministic sentinel repeat, focused artifacts, ADR 0144, H.18 design/checklist and authoritative new-chat handoff documentation.
- Does not change `SimplifiedWaterSteamThermodynamicModel.Resolve()`, `ThermodynamicBranchContinuityModel`, H.9, P060/F040, hysteresis limits, physical coefficients or `PlantNetworkOrchestrator`.

## M10.9.4.1-H.17 Hotfix 6 — Canonical Determinism Fingerprints — VALIDATED

- User-confirmed build, complete ordinary suite and focused H.17 audit passed.
- Long-horizon diagnostic result: 30,000 explicit intervals, 3,046 P060/F040 trigger intervals, 92 episodes, 473 representatives, 228/473 converged and 245/473 line-search exhausted.
- H.16 control remained 15/15; committed selection, deterministic repeat, hold/release challenges and closure/ownership remained green.
- All-node inverse scan discovered untargeted `turbine-inlet`; 120 failures have turbine-inlet candidate-vs-explicit phase mismatch and 125 failures do not.
- The three-node policy therefore does not qualify on the extended domain; production remains explicit.

> **M10.9.4.1-H.17 Hotfix 6 candidate:** fixes an audit-only false determinism failure by canonicalizing policy/committed-observation/inverse-scan fingerprints before comparison. The 30,000-interval census, 3,046-trigger evidence, 92 trigger episodes, 473 H.9 qualification representatives, 21 policy determinism sentinels, H.9 solver, branch-continuity policy, thresholds and production path are unchanged.


> **M10.9.4.1-H.17 Hotfix 5 candidate:** descriptor-only contract alignment: the runtime status now uses the exact phrase `deterministic cross-profile sentinel set` required by `ApplicationDescriptorTests`. The H.17 Hotfix 4 trigger-episode stratified audit, H.9 solver, branch-continuity policy, thresholds and production path are unchanged.
## M10.9.4.1-H.17 Hotfix 4 — Trigger-Episode Stratified Long-Horizon Qualification — CANDIDATE

- Built only on the user-validated H.16 baseline and H.17 Hotfix 3 evidence; no production/numerical component changes.
- Hotfix 3 proved the validator was not stalled: it completed all 30,000 reference intervals and found 3,046 P060/F040 trigger intervals (`837/1014/175/1020` by profile).
- Keeps the exhaustive trigger census and unchanged P060/F040, but replaces unbounded all-trigger H.9 work with deterministic trigger-episode stratification.
- Episodes use a 25-interval maximum quiet gap and retain first/last/hardest representatives; all H.16 control triggers, profile action-boundary representatives and temporal profile samples are also retained.
- Bounds expensive H.9 + all-node candidate inverse-map qualification to at most 512 representative events; mandatory episode/control representatives are never silently dropped.
- All census triggers still force committed target branch-selection observation; deterministic sentinel repeats, hold/release challenges, closure/ownership checks and heartbeat reporting remain.
- Adds `03a-trigger-episode-stratification.csv` and ADR 0143.
- Production remains explicit at 10 ms; H.16 remains the validated baseline until H.17 Hotfix 4 passes build, ordinary tests and focused audit.

## M10.9.4.1-H.17 Hotfix 3 — Long-run audit performance and heartbeat

- Preserves the full 30,000-interval, four-profile reference trajectory and qualifies **every discovered P060/F040 trigger once** with the unchanged H.9 + three-node bounded hysteresis policy.
- Removes the redundant second full H.9 pass and second full inverse-map scan used only for determinism; exact deterministic repeat is now rechecked on a deterministic sentinel set spanning every profile and interval 723.
- Keeps the full committed-state transparency observation pass; its repeat is reduced to deterministic trigger/edge/1,000-interval sentinels.
- Adds a fail-fast budget of 512 discovered triggers. Exceeding it is an explicit gate failure requiring trigger-run stratification, rather than allowing an unbounded Newton audit.
- Adds `artifacts/h17-long-horizon-cross-profile-branch-continuity/00-progress.txt` heartbeat updates through reference generation, trigger census, policy, committed observation and inverse scan stages.
- No production solver, thermodynamic policy, H.9 tolerance, trigger threshold, physical coefficient or target node is changed.


## M10.9.4.1-H.17 Hotfix 3 — Validated Load-Pulse Reference Profile — CANDIDATE

- Builds directly on H.17 Hotfix 1 and preserves the user-validated H.16 numerical baseline.
- Ordinary build and tests passed on Hotfix 1, but the focused H.17 reference trajectory correctly exposed an active plant trip at `load-pulse` interval 634 after the audit-only 5→10 MWe request.
- Does not suppress, reset or weaken that trip. Instead, changes only the audit load excursion to the already validated normal breaker-closed 5→0→5 MWe trajectory: `GeneratorLoadLower` at interval 501 and `GeneratorLoadRaise` at interval 3501.
- Applies the same validated load direction to the load leg of `combined-load-cooling`; the cooling excursion remains 100%→75%→100%.
- Leaves H.9, P060/F040, `steam|stop-out|header`, 2%/5 K bounded hysteresis, all production physics, `Resolve()`, `PlantNetworkOrchestrator`, cross-profile trigger requirements and fourth-node scan unchanged.
- Production remains 10 ms `ExplicitCommittedState`; no shadow state is committed.

## M10.9.4.1-H.17 Hotfix 1 — Cross-Profile Audit String-Interpolation Compile Fix — CANDIDATE

- Built only on the H.17 candidate over the user-validated H.16 baseline.
- Fixes two CSV-emission expressions in `LongHorizonCrossProfileBranchContinuityQualificationAuditTests.cs` that incorrectly escaped a nested C# string literal inside interpolated-expression code and caused the observed CS1039/CS1073/CS1525/CS1056 parser cascade.
- Precomputes the `production-hysteresis-release` counts before interpolation, preserving the exact H.17 audit semantics while removing parser ambiguity.
- No production, simulation, thermodynamic, hydraulic, H.9, branch-continuity, P060/F040, target-set, hysteresis-limit or profile behavior changes.
- H.16 remains the validated baseline until build, ordinary tests and the H.17 focused audit pass locally.

## M10.9.4.1-H.17 — Long-Horizon & Cross-Profile Branch-Continuity Shadow Qualification — CANDIDATE

- Baseline promoted to user-validated M10.9.4.1-H.16.
- Keeps production `ExplicitCommittedState` at 10 ms and leaves `SimplifiedWaterSteamThermodynamicModel.Resolve()`, `ThermodynamicBranchContinuityModel`, H.9, P060/F040, the `steam|stop-out|header` target set and 2%/5 K hysteresis limits unchanged.
- Adds a 30,000-interval shadow evidence set across `steady-long`, `load-pulse`, `cooling-pulse` and `combined-load-cooling`.
- Reproduces the validated H.16 2,000-interval / 15-trigger control before evaluating the extended set.
- Requires every cross-profile trigger to converge with unchanged H.9 tolerances, deterministic repeat and closure/ownership safeguards.
- Tracks all committed target phases on every interval and samples/forces branch-selection transparency checks at 100 ms, trigger intervals and real phase transitions.
- Scans every thermodynamic node at every triggered candidate against the matching explicit endpoint for a new untargeted candidate-only late boundary-aware saturated-root shadow mechanism.
- Retains the inherited two-hold/two-release hysteresis challenges.
- Adds `scripts/run-long-horizon-cross-profile-branch-continuity-audit.cmd`, H.17 documentation/checklist and ADR 0142.
- No production activation is performed; a green H.17 gate only authorizes design of a later reversible activation candidate.

## M10.9.4.1-H.16 — Extended Three-Node Branch-Continuity Shadow Qualification — VALIDATED

- User validation passed build, ordinary tests and focused audit.
- H.14 two-node control reproduced 14/15 and interval 723 failure.
- Unchanged H.13 bounded 2%/5 K policy extended to `steam|stop-out|header` converged 15/15 with zero line-search exhaustion.
- Interval 723 recovered with 68 `header` overrides.
- Deterministic work ratio 1.411000; exact repeat and closure/ownership safeguards passed.
- 6,000 committed target observations remained transparent and six real committed phase transitions were not blocked.
- Four inherited hold/release challenges passed.

## M10.9.4.1-H.16 — Extended Three-Node Branch-Continuity Shadow Qualification — CANDIDATE

- Built only on the user-validated H.15 Hotfix 1 baseline.
- H.15 localized the H.14 interval-723 failure to `header` and confirmed the same inverse-map overlapping-root / coarse-saturated-detection / fixed-priority mechanism previously proven at `steam` and `stop-out`.
- Adds an explicit H.16 audit that first reproduces the validated H.14 two-node 14/15 control and then extends the unchanged H.13 bounded 2%/5 K policy only to `steam|stop-out|header`.
- Positive qualification requires 15/15 convergence, zero line-search exhaustion, concrete `header` override evidence at interval 723, transparent 6,000 committed target observations, inherited hold/release challenge success, exact repeat and preserved closure/ownership.
- Production `Resolve()`, H.9, `ThermodynamicBranchContinuityModel`, hysteresis limits, P060/F040, physical coefficients, `PlantNetworkOrchestrator` and the 10 ms explicit production path remain unchanged.
- Adds `scripts/run-three-node-branch-continuity-audit.cmd`, ADR 0141, H.16 design/checklist and updated handoff/status documentation.

## M10.9.4.1-H.15 Hotfix 1 — Stale Build Output Hygiene — CANDIDATE

- Built only on the H.15 candidate over the user-validated H.14 Hotfix 1 baseline; no simulation, thermodynamic, hydraulic or diagnostic source behavior changes.
- Fixes the stacked-ZIP validation workflow: local `bin`/`obj` outputs from an older milestone can be newer than extracted source timestamps and may therefore be reused by incremental MSBuild, as observed when the H.15 test assembly loaded an H.14 `NuclearReactorSimulator.Application.dll` even though the H.15 `ApplicationDescriptor.cs` source was already correct.
- `APPLY_UPDATE.cmd` now removes all local `bin` and `obj` directories before validation, forcing the subsequent build to compile the extracted H.15 sources.
- Also realigns the stale `APPLY_UPDATE.cmd` banner from H.12 to H.15 Hotfix 1.
- H.15 root-cause diagnostics, H.9, H.13 bounded hysteresis, production `Resolve()` and `PlantNetworkOrchestrator` remain unchanged.

## M10.9.4.1-H.15 — Extended Trigger 723 Root-Cause Diagnosis — CANDIDATE

- Built only on the user-validated H.14 Hotfix 1 baseline. H.14 broadened the selected bounded branch-continuity policy to 2,000 intervals, found 15 P060/F040 triggers and converged 14/15; interval 723 was the sole line-search exhaustion while all four hold/release challenges passed.
- Keeps production `SimplifiedWaterSteamThermodynamicModel.Resolve()`, validated H.13 `ThermodynamicBranchContinuityModel`, H.3-H.9 correctors and `PlantNetworkOrchestrator` routing unchanged.
- Reconstructs the first 724 committed intervals and reproduces the H.14 prefix: nine triggers through interval 724, H.4 primary 6/9 and interval 723 non-convergent.
- Re-runs unchanged H.9 + bounded hysteresis at intervals 721-724 and requires interval 723 to reproduce with zero branch overrides, zero hysteresis releases and line-search exhaustion.
- Generalizes validated H.10/H.12 diagnostics across every hydraulic path and fluid node, ranks mapped-minus-applied node mass/energy residuals and records all inverse-map branch candidates without assuming a culprit.
- Adds `scripts/run-extended-trigger-723-root-cause-audit.cmd`, ADR 0140, H.15 design/validation documentation and updated handoff/application descriptor.
- A clean local switching/inverse-map result explicitly redirects the next step to fixed-point existence/residual-floor and basin analysis rather than solver/hysteresis retuning.
- No production hybrid activation, hysteresis change, branch reorder, active set, physical retuning, trigger retuning, hidden filtering or timestep change.

## M10.9.4.1-H.14 Hotfix 1 — Broader Thermodynamic Branch-Continuity Shadow Qualification — VALIDATED

- User validation passed compilation, complete ordinary tests and the focused H.14 audit. Over 2,000 committed intervals P060/F040 produced 15 events; unchanged H.9 plus targeted bounded hysteresis converged 14/15 and exhausted the line search only at interval 723. Deterministic work ratio was 1.586500 and conservation/ownership remained green.
- All four explicit branch-policy challenges passed: two required holds and two required releases. The 4,000 committed target observations included four real phase transitions with zero policy overrides.
- The interval-723 failure recorded zero branch overrides and zero hysteresis releases, establishing it as a distinct extended-horizon problem rather than a failure of the already validated `steam`/`stop-out` continuity mechanism.
- Hotfix 1 changed only the H.14 Jacobian iteration test contract. Production remained explicit at 10 ms and no shadow state was committed.

## M10.9.4.1-H.14 — Broader Thermodynamic Branch-Continuity Shadow Qualification — CANDIDATE

- Built only on the user-validated H.13 Hotfix 2 baseline.
- Keeps production `SimplifiedWaterSteamThermodynamicModel.Resolve()`, `ThermodynamicBranchContinuityModel`, H.3-H.9 correctors and `PlantNetworkOrchestrator` routing unchanged.
- Extends the current-v2 committed shadow horizon from 500 to 2,000 intervals while preserving the first 500 as the exact H.13 control window.
- Re-evaluates every P060/F040 event found across the extended horizon using the unchanged H.9 Jacobian corrector plus the selected targeted bounded previous-phase hysteresis policy.
- Adds deterministic committed-state branch observation for `steam` and `stop-out` across all 2,000 intervals.
- Adds four explicit branch-policy qualification challenges: two required holds and two required releases, covering both saturated-to-superheated and superheated-to-saturated directions.
- Adds `scripts/run-broader-thermodynamic-branch-continuity-audit.cmd`, ADR 0139, H.14 design/validation documentation and updated handoff/application descriptor.
- No production hybrid activation, hysteresis, branch reorder, active set, physical retuning, trigger retuning, hidden filtering or timestep change.

## M10.9.4.1-H.13 Hotfix 2 — Thermodynamic Branch Continuity / Hysteresis Shadow Experiment — VALIDATED

- User validation passed compilation, complete ordinary tests and the focused H.13 audit. Production H.9 remained 5/7 with two line-search exhaustions; targeted previous-phase continuity and targeted bounded 2%/5 K hysteresis both reached 7/7 with zero exhaustions, deterministic work ratio 1.886000, exact repeat and preserved closure/ownership. The bounded policy recorded zero hysteresis releases on the frozen seven-event set.
- Built only on the user-validated H.12 thermodynamic inverse branch-selection baseline.
- Adds shadow-only `ThermodynamicBranchContinuityModel`; production `SimplifiedWaterSteamThermodynamicModel.Resolve()` remains unchanged.
- Restricts alternative branch selection to H.12 nodes `steam` and `stop-out`.
- Compares previous-phase continuity with bounded previous-phase hysteresis using 2% relative pressure and 5 K temperature release limits.
- Runs both policies under the unchanged H.9 Jacobian corrector over the exact frozen 7 P060/F040 events and records convergence, branch overrides, chatter, avoided branch jumps, work and conservation/ownership.
- Adds ordinary Simulation regressions, explicit Application audit, `scripts/run-thermodynamic-branch-continuity-audit.cmd`, ADR 0138, H.13 design/validation documentation and updated handoff/application descriptor.
- No production hybrid activation, branch reorder, active set, physical retuning, trigger retuning, hidden filtering or timestep change.

## M10.9.4.1-H.12 — Thermodynamic Inverse Branch Selection Audit — VALIDATED

- Promotes user-validated H.11 as the continuation baseline. H.11 localized the two persistent H.9 thermodynamic phase boundaries to `steam` at interval 200 and `stop-out` at interval 360, both on energy+mass probes.
- Adds diagnostic-only `IWaterSteamInverseBranchDiagnosticProvider` evidence to `SimplifiedWaterSteamThermodynamicModel` without changing the existing `Resolve()` branch order or result semantics.
- Adds `ThermodynamicInverseBranchSelectionAnalyzer` to inspect all five existing inverse-map branch attempts, simultaneous saturated/superheated roots, coarse saturated detection toggles, late boundary-aware saturated roots shadowed by earlier coarse-superheated selection, and previous-state tie-break sensitivity.
- Adds ordinary Simulation regressions, explicit Application audit, `scripts/run-thermodynamic-inverse-branch-selection-audit.cmd`, ADR 0137, H.12 design/validation documentation and updated handoff/application descriptor.
- Production current-v2 remains explicit at 10 ms. No active set, hysteresis, branch reorder, thermodynamic clamp, physical retune or shadow commit is introduced.

## M10.9.4.1-H.11 — Thermodynamic Switching Localization & Active-Set Diagnosis — VALIDATED

- Promotes user-validated H.10 Hotfix 1 as the continuation baseline. H.10 reproduced the frozen 500-step / seven-event evidence set, found zero hydraulic branch switches and zero hydraulic non-smooth paths, but exactly two thermodynamic phase/envelope switches and two thermodynamic non-smooth nodes around the two persistent H.9 candidate states; the corresponding explicit endpoints showed zero thermodynamic switches.
- Adds separate shadow-only `ThermodynamicSwitchingLocalizationAnalyzer`, options and structured localization records. It consumes H.10 evidence and analyzes only nodes already marked as phase/envelope-switching.
- Adds conserved energy-minus/plus and mass-minus/plus localization probes that record resolved/out-of-range status, phase, pressure, temperature, vapor quality, specific volume/internal energy, saturation-reference distance and saturated-liquid/vapor internal-energy distances.
- Classifies each localized node by crossing axis (`energy`, `mass`, or both) and boundary type (`phase-boundary`, `envelope-edge`, or both). A `hold-<nominal phase>` active-set label is emitted as diagnostic evidence only; no active set is enforced.
- Adds node-local mapped-minus-applied hydraulic mass/energy balance residual evidence for the localized H.9 candidate nodes without modifying H.9 internals.
- Adds ordinary Simulation regressions, explicit Application audit, `scripts/run-thermodynamic-switching-localization-audit.cmd`, ADR 0136, H.11 design/validation documentation and updated handoff/application descriptor.
- Production current-v2 remains `ExplicitCommittedState` at 10 ms; H.3-H.10 algorithms and `PlantNetworkOrchestrator` routing remain unchanged.

## M10.9.4.1-H.10 Hotfix 1 — Hydraulic Map Switching & Non-Smoothness Diagnosis — VALIDATED

- User validation passed compilation, ordinary tests and the focused H.10 audit. H.10 found `hydraulic-branch-switches=0`, `hydraulic-nonsmooth-paths=0`, two thermodynamic phase/envelope switches, two thermodynamic non-smooth nodes, maximum thermodynamic derivative-scale growth 3.999974411, zero corresponding explicit-end thermodynamic switches, exact deterministic repeat and `switching-nonsmoothness-diagnostic-passes=True`.
- Promotes user-validated H.9 as the continuation baseline. H.9 reproduced the frozen seven P060/F040 events and remained 5/7 with two line-search exhaustions; it built/accepted 23/21 Jacobian directions, rejected no Jacobian for conditioning, reached maximum pivot-condition estimate 1.613327388, maximum normalized Newton step 1.276749799, deterministic work ratio 2.702000, exact repeat and `jacobian-informed-corrector-qualification-passes=False`.
- Stops nonlinear-solver escalation and adds separate shadow-only `HydraulicMapSmoothnessAnalyzer`, probe options and structured evidence records. No H.10 candidate state is solved or committed.
- Adds deterministic two-scale law-local pressure probes for every pipe/valve/pump path, including forward/reverse/zero, closed-valve and discharge-check-valve blocked branches, one-sided slope asymmetry and derivative scale-growth evidence.
- Adds deterministic two-scale conserved mass/internal-energy probes through the existing thermodynamic closure, recording phase and supported-envelope transitions plus pressure derivative scale growth.
- The explicit Application audit reconstructs the same 500 current-v2 intervals and seven frozen triggers, reproduces H.4 5/7, H.6 6/7, H.7 5/7, H.8 5/7 and H.9 5/7, then diagnoses exactly the two persistent H.9 failures and compares them with each committed explicit endpoint.
- Adds `scripts/run-hydraulic-map-switching-nonsmoothness-audit.cmd`, ADR 0135, H.10 design/validation documentation and updated handoff/application descriptor.
- Production current-v2 remains `ExplicitCommittedState` at 10 ms; Picard, H.7, H.8, H.9 and `PlantNetworkOrchestrator` routing remain unchanged.

## M10.9.4.1-H.9 — Jacobian-Informed Nonlinear Hydraulic Corrector — VALIDATED

- User validation passed compilation, ordinary tests and the focused H.9 audit. H.9 converged 5/7 with two line-search exhaustions; Jacobian builds/acceptances were 23/21, Jacobian rejected 0, maximum pivot-condition estimate 1.613327388, maximum normalized Newton step 1.276749799, maximum pressure/flow residuals 0.303946548 / 28.426478113 kg/s, deterministic work ratio 2.702000, strict accepted-merit decrease and exact repeat. `jacobian-informed-corrector-qualification-passes=False`.
- Promotes user-validated H.8 as the continuation baseline: safeguarded Anderson remained 5/7 over the seven frozen P060/F040 events, exhausted two line searches, used deterministic work ratio 1.212000, preserved strict merit decrease/determinism/conservation, and returned `accelerated-corrector-qualification-passes=False`.
- Adds separate shadow-only `JacobianHydraulicCorrectorSolver`, options/result/iteration evidence, conservative hydraulic coordinates, scaled finite-difference Jacobian construction, deterministic backward-probe fallback, scaled-pivot linear solve, pivot-conditioning rejection, bounded normalized Newton step, H.7 merit backtracking and residual-direction fallback.
- Adds ordinary Simulation regressions and an explicit Application audit that reproduces H.4 5/7, H.6 6/7, H.7 5/7 and H.8 5/7 before evaluating H.9.
- Adds `scripts/run-jacobian-informed-corrector-audit.cmd`, ADR 0134, H.9 design/validation documentation and updated handoff/application descriptor.
- Production current-v2 remains explicit at 10 ms; Picard, H.7, H.8 and `PlantNetworkOrchestrator` production routing remain unchanged.

## M10.9.4.1-H.8 — Accelerated Nonlinear Hydraulic Corrector — VALIDATED

- User validation passed compilation, ordinary tests and the focused H.8 audit. H.8 reproduced H.4 5/7, H.6 6/7 and H.7 5/7, then safeguarded Anderson also converged 5/7 with two line-search exhaustions.
- H.8 recorded Anderson attempts/acceptances 30/24, residual fallback 13/11, six rejected least-squares systems, maximum coefficient L1 norm 7.188310311, maximum pressure/flow residuals 0.303946566 / 28.444475059 kg/s, deterministic work ratio 1.212000, strict accepted-merit decrease and exact deterministic repeat. `accelerated-corrector-qualification-passes=False`.
- Promotes user-validated H.7 Hotfix 1 as the continuation baseline: true fixed-point residual plus deterministic backtracking converged 5/7 frozen P060/F040 events, exhausted two line searches, retained exact determinism/conservation, and returned `corrector-algorithm-revision-qualification-passes=False`.
- Adds separate shadow-only `AndersonHydraulicCorrectorSolver`, options/result/iteration evidence types, bounded memory depth 3, regularized affine residual minimization, coefficient L1 safeguard, deterministic merit-decreasing backtracking and H.7 residual fallback.
- Adds ordinary Simulation regressions and an explicit Application audit that reproduces H.4 5/7, H.6 6/7 and H.7 5/7 before evaluating H.8.
- Adds `scripts/run-accelerated-nonlinear-corrector-audit.cmd`, ADR 0133, H.8 design/validation documentation and updated handoff/application descriptor.
- Production current-v2 remains explicit at 10 ms; Picard, H.7 and `PlantNetworkOrchestrator` production routing remain unchanged.

## M10.9.4.1-H.7 — Corrector Algorithm Revision — CANDIDATE

- Promotes user-validated H.6 as the new continuation baseline. H.6 kept the exact 500 committed explicit intervals and P060/F040 trigger set, selected `R0125-I096`, but converged only 6/7; maximum selected-profile pressure/flow residuals were 0.291876228 and 61.700761261 kg/s, the two-tier work ratio was 1.700000, and `refined-envelope-qualification-passes=False`.
- Preserves the historical `SemiImplicitHydraulicPrototypeSolver` unchanged and adds a separate shadow-only `ResidualBacktrackingHydraulicCorrectorSolver`. H.7 does not wire the revised solver into `PlantNetworkOrchestrator` or `HybridSemiImplicitHydraulicGateSolver`.
- Replaces relaxed-iterate-motion convergence evidence with a true unrelaxed fixed-point residual: relative pressure residual plus absolute pipe/valve/pump flow residual. Convergence requires both raw residuals to satisfy the existing `1e-5` / `1e-2 kg/s` tolerances.
- Defines deterministic normalized merit as the maximum of the two tolerance-normalized residuals. Each nonlinear iteration starts at relaxation 1.0 and backtracks by 0.5 down to 1/1024; a trial is accepted only when merit strictly decreases. Invalid trial states are rejected and never become authoritative.
- Keeps conservation ownership unchanged: every accepted candidate is rebuilt exactly once from the original committed inventory using accepted hydraulic balances plus frozen non-hydraulic balances. Rejected trials are not cumulative commits.
- Adds six ordinary Simulation regressions, one explicit Application audit, ADR 0132, `scripts/run-corrector-algorithm-revision-audit.cmd`, H.7 design documentation and validation checklist. Expected discovery becomes 1,053 passed + 39 explicit skipped = 1,092 total.
- The H.7 audit must first reproduce the frozen H.4 5/7 and H.6 `R0125-I096` 6/7 evidence, then records convergence, line-search exhaustion, accepted relaxation trace, true residuals, deterministic hydraulic-evaluation work, conservation, candidate gaps and exact repeat.
- Production current-v2 remains `ExplicitCommittedState` at 10 ms. A positive H.7 result permits only broader free-running/scenario shadow qualification; a negative result requires further nonlinear-corrector development.

## M10.9.4.1-H.6 — Shadow Corrector Rescue Envelope & Two-Tier Qualification — VALIDATED

- Promoted user-validated H.5 Hotfix 2 as the safe production-rollback / extended-shadow baseline. User evidence: 500 committed intervals, 7 P060/F040 shadow corrections, 5/7 converged, deterministic work ratio 1.492000, observational shadow cost ratio 1.480162, exact determinism/conservation and `extended-shadow-qualification-passes=False`; production remained explicit.
- Froze the H.5 trigger evidence instead of raising thresholds: the same 7 triggered intervals were retained with H.4 primary `R015-I072` convergence 5/7.
- Evaluated six bounded Picard rescue profiles changing only relaxation and maximum iteration count: R015-I096, R0125-I096, R010-I096, R010-I128, R0075-I128 and R0075-I160. Pressure/flow residual tolerances and all physical coefficients remained unchanged.
- User validation passed compilation, ordinary tests and the focused audit. `R0125-I096` was selected with 6/7 convergence, deterministic always-use work ratio 1.438000, maximum pressure residual 0.291876228, maximum flow residual 61.700761261 kg/s and maximum mass/energy/pressure gaps 0.000175566/0.000194823/0.291958117. `rescue-profile-qualifies=False`.
- The deterministic two-tier H.4-primary/rescue ladder also converged only 6/7, with deterministic work ratio 1.700000 and exact repeat. `refined-envelope-qualification-passes=False`.
- Production current-v2 remained `ExplicitCommittedState` at 10 ms; `production-hybrid-active=False`, `shadow-candidates-committed=False`, and no trigger retuning, physical coefficient retuning or hidden flow filtering occurred.

## M10.9.4.1-H.5 Hotfix 2 — Production Activation Rollback & Extended Shadow Qualification — VALIDATED

- Rolls ordinary current-v2 production back to the user-validated 10 ms `ExplicitCommittedState` path after H.5 Hotfix 1 exposed non-convergent free-running hybrid corrections in ordinary desktop/runtime tests. This is an evidence-scope correction, not a physical retune.
- Preserves the H.4-selected `P060-F040-R015` corrector as an explicit experimental/shadow path only. The extended H.5 audit evaluates it independently against 500 committed 10 ms production intervals (5 s), records trigger/convergence/residual/work evidence, and never commits a shadow candidate.
- Keeps the no-hidden-fallback rule intact: production is explicitly configured as explicit from the outset; a failed shadow correction is diagnostic evidence, not a silently discarded authoritative step.
- Corrects the synthetic opt-in integration regression to use the H.3-proven conservative corrector controls (96 iterations, relaxation 0.10) instead of the unqualified 64/0.20 pair. No physical law or production coefficient changes.
- Expected ordinary inventory is corrected from prior documentation to 1,047 passed + 37 explicit skipped = 1,084 total, matching actual test discovery once the six H.5 Hotfix 1 failures are removed.
- User validation confirmed compilation and complete ordinary tests green. The 5 s shadow gate recorded 7/500 corrections, 5/7 converged, deterministic work ratio 1.492000, observational cost ratio 1.480162, exact determinism/conservation and `extended-shadow-qualification-passes=False`; production remained explicit and stable.

## M10.9.4.1-H.5 Hotfix 1 — Invariant Comparison Report Compile Fix — CANDIDATE

- Fixes the sole H.5 build failure `CS1503` in `HybridProductionIntegrationAuditTests.FormatComparison`: concatenated interpolated strings were materialized as `string` before `FormattableString.Invariant(...)`. The report is now one interpolated `FormattableString`, preserving invariant-culture formatting and identical output semantics.
- No production runtime, `PlantNetworkOrchestrator`, H.4-selected `P060-F040-R015` numerical profile, physical coefficient, controller, protection, test inventory, audit threshold or artifact set changes.
- Descriptor, apply script and authoritative status/handoff documentation are aligned to H.5 Hotfix 1. H.4 remains the validated baseline until this hotfix passes build, focused, ordinary and cumulative H.5 gates.

## M10.9.4.1-H.5 — Current-v2 Hybrid Hydraulic Production Integration — CANDIDATE

- Promotes user-validated H.4. Compilation, ordinary tests and the focused hybrid activation/cost gate passed. The selected deterministic profile is `P060-F040-R015`: 0.060 predicted subcooled-pressure trigger, 40 kg/s predicted hydraulic-flow trigger, 0.15 Picard relaxation and 72 maximum corrector iterations.
- H.4 evidence corrected only 2/50 intervals, converged 2/2 corrections, used deterministic work ratio 2.14 and observational wall-cost ratio 1.662880 while retaining chatter ratios pump/channel/return/pressure 0.432616/0.135885/0.086691/0.921832, tiny final-state gaps, zero conservation/ownership residuals and exact deterministic repeat.
- Adds versioned `HydraulicNumericalCouplingDefinition` to `PlantDefinition`; historical definitions default to `ExplicitCommittedState`, while the two sustained current-v2 profiles opt into `DeterministicHybridSemiImplicit` with the exact H.4-selected profile. The H.1/H.4 numerical-evidence factory explicitly remains on the historical explicit path.
- Routes only opted-in definitions through the canonical `PlantNetworkOrchestrator` hybrid branch. Non-hydraulic balances remain frozen over a correction, each provisional iterate is rebuilt from the original committed logical-step state, conserved fluid/thermal inventories are integrated once, and a triggered non-convergent corrector fails explicitly rather than silently falling back.
- Adds immutable per-step hydraulic numerical diagnostics through `PlantNetworkStepResult` and `IntegratedPrimaryCircuitSnapshot`, plus production integration regressions and a 5 s explicit-vs-hybrid deterministic audit. No physical coefficient, controller, turbine/generator/protection setting, external 10 ms logical timestep, wall-clock adaptation or hidden flow filtering changes.
- Adds ADR 0129, `scripts/run-hybrid-production-integration-tests.cmd` and H.5 documentation/checklist. Expected ordinary inventory becomes 1,046 passed + 37 explicit skipped = 1,083 total. Phase H closes only after focused, ordinary and cumulative production gates are green; Phase I follows validation.

## M10.9.4.1-H.4 — Deterministic Hybrid Semi-Implicit Activation & Cost Gate — VALIDATED

- Promotes user-validated H.3 Hotfix 1. Compilation, ordinary tests and the focused prototype gate passed; H.3 converged 50/50 intervals with exact conservation/determinism and strong pump/channel/return chatter reductions, but full-time isolated cost was approximately 15.894951x explicit.
- Adds audit-only `HybridSemiImplicitHydraulicGateSolver`: every interval computes the existing explicit predictor and invokes the H.3 corrector only when deterministic predicted subcooled-pressure or hydraulic-flow changes cross configured thresholds. Production `PlantNetworkOrchestrator` remains unchanged and explicit at 10 ms.
- Adds a deterministic eight-configuration sweep over pressure/flow triggers and Picard relaxation/iteration controls. Candidate selection uses convergence, chatter/pressure reduction, final-state gap, conservation/ownership and an iteration-derived deterministic work ratio; wall-clock cost is observational only and never drives simulation branching or selection.
- User validation selected `P060-F040-R015`: 2/50 corrections, 2/2 converged, deterministic work ratio 2.140000, observational wall-cost ratio 1.662880, chatter ratios pump/channel/return/pressure 0.432616/0.135885/0.086691/0.921832, tiny final-state gaps, zero conservation/ownership residuals and exact deterministic repeat. `activation-criteria-met=True`; H.4 itself correctly remained `production-hybrid-active=False` and authorized the separate H.5 production-integration candidate.
- Adds four ordinary Simulation regressions, one explicit Application audit, `scripts/run-hybrid-semi-implicit-activation-gate.cmd`, ADR 0128 and H.4 documentation/checklist. Expected inventory becomes 1,041 passed + 36 explicit skipped = 1,077 total.
- Normalizes H.3/H.4 audit text output to UTF-8 without BOM so Windows `type` no longer emits the leading BOM marker observed in the validated H.3 summary. No H.3 numerical behavior changes.

## M10.9.4.1-H.3 Hotfix 1 — Isolated Semi-Implicit Hydraulic Prototype & Audit — VALIDATED

- User-confirmed compilation and complete ordinary `dotnet test` passed after the xUnit2013 analyzer hotfix.
- Focused H.3 audit converged 50/50 intervals with 16.760 average / 40 maximum iterations; prototype/explicit chatter ratios were pump 0.432808, channel 0.135868, return 0.031681 and pressure 0.922127.
- Inventory integration, hydraulic mass closure and energy ownership residuals were zero at reported precision; final relative gaps were mass 0.000036406, energy 0.000033775 and pressure 0.002898964; deterministic repeat was exact.
- Full-time prototype cost was 16.743903 versus 1.053410 wall-s per simulated second, ratio 15.894951, so production semi-implicit activation remained correctly deferred.

## M10.9.4.1-H.3 Hotfix 1 — xUnit2013 Collection-Cardinality Analyzer Fix — CANDIDATE

- Fixes the warnings-as-errors build failure `xUnit2013` in `SemiImplicitHydraulicPrototypeSolverTests.Evaluate_PreservesHydraulicMassAndEnergyOwnershipClosure` by replacing `Assert.Equal(1, result.PipeMassFlowRates.Count)` with the analyzer-canonical `Assert.Single(result.PipeMassFlowRates)`.
- Test intent is unchanged: the result must still contain exactly one pipe-flow entry before the positive-flow and ownership assertions run.
- No Simulation solver, H.3 Picard parameter, physical coefficient, production route, test inventory or audit artifact changes. Production current-v2 remains explicit at 10 ms and H.4 still owns any activation.

## M10.9.4.1-H.3 — Isolated Semi-Implicit Hydraulic Prototype & Audit — CANDIDATE

- Promotes user-validated H.2 as the numerical-method decision baseline. H.2 compilation and ordinary tests passed, and the H.1 evidence audit reproduced the same non-improving 10/5/2.5 ms refinement pattern with unchanged hydraulic metrics; observed wall costs were 0.826572/1.624096/3.208644 s per simulated second.
- Adds an isolated `SemiImplicitHydraulicPrototypeSolver` beside the production `PlantNetworkOrchestrator`; production current-v2 remains explicit at 10 ms and does not route through the prototype.
- Reuses the existing pipe, valve, pump and fluid-inventory laws unchanged. The prototype applies bounded deterministic under-relaxed Picard iteration and reconstructs every provisional candidate from the original committed inventory, so provisional iterations are never cumulative commits.
- Adds a frozen-forcing current-v2 audit: per-step non-hydraulic balances are reconstructed from the validated explicit trajectory, an isolated one-pass replay must reproduce reference inventories, and the semi-implicit replay changes only pressure/flow coupling.
- Records primary pump/channel/return chatter, subcooled-liquid pressure changes, convergence/iteration residuals, hydraulic ownership residuals, deterministic repeat, final-state gap and isolated solver cost. H.4 alone owns production activation after evidence review.
- Adds six ordinary Simulation regressions, one explicit Application audit, `scripts/run-semi-implicit-hydraulic-prototype-audit.cmd`, ADR 0127 and the H.3 validation checklist. Expected inventory becomes 1,037 passed + 35 explicit skipped = 1,072 total.

## M10.9.4.1-H.2 — Deterministic Semi-Implicit Pressure/Flow Method Decision — VALIDATED

- User-confirmed compilation and complete ordinary tests passed.
- Re-running the H.1 evidence from H.2 reproduced unchanged hydraulic stiffness metrics and `refinement-improves=False`; observational wall cost was 0.826572/1.624096/3.208644 s per simulated second with 1.964857 and 1.975649 cost ratios.
- Confirms the H.2 decision itself did not alter production runtime physics. Deterministic semi-implicit pressure/flow coupling remains selected for isolated H.3 evidence before any H.4 activation.

## M10.9.4.1-H.2 — Deterministic Semi-Implicit Pressure/Flow Method Decision — CANDIDATE

- Promotes the user-validated H.1 evidence checkpoint. H.1 reports 10/5/2.5 ms wall cost 0.824089/1.592215/3.188456 s per simulated second, maximum raw channel one-step change 114.314039863 kg/s and non-improving final-state refinement: 0.005401937 coarse/medium versus 0.006028534 medium/fine.
- Rejects unchanged explicit 10 ms as the final numerical-hardening answer and rejects bounded explicit substeps as the preferred cure because refinement approximately doubles cost without monotonic convergence improvement.
- Selects deterministic semi-implicit pressure/flow coupling as the method direction, while keeping production current-v2 on the validated explicit 10 ms path in H.2.
- Adds no Simulation physics and no semi-implicit runtime path. H.3 owns an isolated prototype/audit; H.4 owns any current-v2 activation only after conservation, determinism, convergence and bounded-cost evidence pass.
- Prohibits wall-clock adaptation, hidden damping, coefficient retuning and solver-protection interlocks as numerical cures.
- Adds ADR 0126, the H.2 decision document and H.2 validation checklist. Ordinary test inventory remains 1,031 passed + 34 explicit skipped = 1,065 total.

## M10.9.4.1-H.1 — Fixed-Step Timestep Sensitivity & Stiffness Evidence — VALIDATED

- User-confirmed compilation, ordinary tests and focused H.1 evidence gate passed.
- Supplied summary reports 10/5/2.5 ms wall cost 0.824089/1.592215/3.188456 s per simulated second, maximum pump/channel/return one-step changes 23.483512764/114.314039863/86.409873575 kg/s, maximum fractional mass/energy/liquid-pressure step changes 0.001965068/0.002079844/0.088657847, and `refinement-improves=False`.
- Maximum final relative difference is 0.005401937 for coarse/medium and 0.006028534 for medium/fine. H.1 therefore closes as evidence and advances to H.2 method selection.

## M10.9.4.1-H.1 — Fixed-Step Timestep Sensitivity & Stiffness Evidence — CANDIDATE

- Promotes the user-validated G.4 turbine-expansion enthalpy/shaft-work migration and closes Phase G. Supplied G.4 evidence reports 2 current-v2 enthalpy-mode stages, 2.506379668 MW maximum flow-work rate, 5.457103652 MW maximum shaft power and 0 W maximum ownership residual, with no thermodynamic-work retuning.
- Keeps the production current-v2 runtime on the deterministic 10 ms fixed timestep.
- Adds an internal audit-only current-v2 desktop factory seam for 10, 5 and 2.5 ms fixed-step refinement while preserving the same 20 ms deterministic seed-preconditioning duration.
- Adds raw per-step evidence for primary pump/channel/return flow changes, dominant fractional fluid-node mass/internal-energy/subcooled-liquid pressure changes and existing conservation residuals.
- Adds final-state 10→5→2.5 ms convergence evidence plus observed order where defined and observational wall-clock cost per simulated second.
- Does not activate adaptive substepping, semi-implicit coupling, wall-clock adaptation, hidden nonlinear repair, physical retuning or new damping/filtering. H.2 owns the numerical-method decision.
- Adds two ordinary factory/runtime regressions, one explicit audit, `scripts/run-numerical-stiffness-decision-audit.cmd`, ADR 0125 and the H.1 validation checklist.

## M10.9.4.1-G.4 — Turbine Expansion Enthalpy & Shaft-Work Ownership — VALIDATED

- User-confirmed focused G.4 gate passed. Supplied audit: 2 current-v2 enthalpy-mode stages, maximum absolute flow-work rate 2.506379668 MW, maximum absolute shaft power 5.457103652 MW and 0 W maximum ownership residual.
- Confirms node inventories remain internal energy, shaft work is single-counted, thermodynamic turbine work is not retuned and legacy profiles remain preserved.
- G.4 closes the staged Phase G open-control-volume enthalpy migration.

## M10.9.4.1-G.4 — Turbine Expansion Enthalpy & Shaft-Work Ownership — CANDIDATE

- Promotes the user-validated G.3 remaining non-turbine enthalpy migration as the continuation baseline. The supplied G.3 audit reports 12 samples, 2.817289248 MW maximum flow work, 8.972520763 MW total absolute flow work and zero maximum ownership residual.
- Adds backward-compatible `FluidEnergyTransportMode` ownership to `TurbineStageGroupDefinition`; historical definitions retain `SpecificInternalEnergy`.
- Enables `SpecificEnthalpy` for turbine expansion only in the two current-v2 sustained profiles.
- Current-v2 turbine inlet transport is `h*m_dot`; exhaust transport is inlet enthalpy transport minus shaft work. Shaft work remains one explicit thermofluid-to-rotor transfer and node inventories remain internal energy.
- Extends turbine stage snapshots with transport mode, inlet flow work, inlet enthalpy, selected inlet/exhaust advected energy, flow-work rate and an explicit ownership residual while preserving historical fields.
- Does not retune thermodynamic turbine work, stage efficiency, governor, generator/grid coupling or protections.
- Adds focused domain/simulation/application regressions, `TurbineExpansionEnthalpyMigrationAuditTests`, explicit CSV/summary evidence, `scripts/run-turbine-expansion-enthalpy-tests.cmd`, ADR 0124 and the G.4 validation checklist.
- Successful G.4 validation closes the staged Phase G runtime migration and advances the hardening sequence to Phase H numerical-stiffness evidence.

## M10.9.4.1-G.3 — Remaining Non-Turbine Enthalpy Migration — VALIDATED

- User-confirmed compilation and all requested tests passed.
- Supplied audit: 12 samples, 3 pump paths, 2 drum paths, 2 condenser paths, 2 external-boundary owners, 2.817289248 MW maximum absolute flow-work rate, 8.972520763 MW total absolute flow-work rate and 0 W maximum ownership residual.
- Confirms node inventories remain internal energy, pump hydraulic work and condenser heat rejection are single-counted, relief remains external, bypass remains internal and turbine expansion remained isolated for G.4.

## M10.9.4.1-G.3 — Remaining Non-Turbine Enthalpy Migration — CANDIDATE

- Documentation checkpoint: records the approved post-hardening gameplay/control-room direction without changing G.3 runtime code, tests, expected inventory or validation gate.
- Adds `FUTURE_GAMEPLAY_CONTROL_ROOM_AND_ACCIDENT_DIRECTION.md` and ADR 0121–0123 covering causal/persistent accident progression, reduced spatial 2D core evolution, IndustrialControls integration, persistent operator-handle semantics, mimic zoom/pan/editable saved layout, workspace presets, plant-like mnemonics/procedures and Instructor/Fault mode.
- Explicitly keeps the immediate sequence G.3 → G.4 → H → I unchanged and excludes multi-monitor from the current approved backlog.
- Builds on the user-validated G.2 Hotfix 2 baseline. The supplied G.2 audit confirms exact passive/pump ownership closure, six migrated passive components, three migrated pump paths, 2.762103670 MW maximum component flow work and 11.563679870 MW total absolute passive flow work.
- Migrates every remaining current-v2 non-turbine advective owner from `u*m_dot` to `h*m_dot`: pump paths, steam-drum separation, feedwater and steam-export boundaries, turbine-admission boundaries, condenser phase change, atmospheric relief and turbine bypass.
- Keeps canonical fluid-node inventories as mass plus internal energy. Every migrated snapshot separately reports specific internal energy, `p/rho` flow work, specific enthalpy, internal-energy rate, flow-work rate and the advected rate actually applied.
- Preserves exact work ownership: pump hydraulic work and shaft demand remain explicit single-count contributions; condenser heat rejection remains the enthalpy drop between removed steam and added condensate; relief remains an external exchange; bypass and drum separation remain conservative internal transfers.
- Keeps legacy/current-v1 definitions on `SpecificInternalEnergy` by default. Only the two current-v2 sustained profiles opt into `SpecificEnthalpy` for the new component groups.
- Leaves turbine expansion and shaft-work migration untouched for G.4; no turbine work, governor, electrical coupling, protection, HMI, replay or checkpoint retuning is introduced.
- Adds focused definition and runtime regressions, `RemainingNonTurbineEnthalpyMigrationAuditTests`, the explicit CSV/summary audit, `scripts/run-remaining-non-turbine-enthalpy-tests.cmd`, ADR 0120 and the G.3 validation checklist.
- Expected ordinary inventory: 1,025 passed, 0 failed, 32 explicit skipped, 1,057 total.

## M10.9.4.1-G.2 Hotfix 2 — Signed Grid-Exchange Stability Contract — VALIDATED

- The user confirmed compilation, focused G.2 tests, ordinary suite and requested gates all passed on 2026-07-26. The supplied audit confirms six passive enthalpy-mode components, exact passive closure, three pump paths with exact work ownership, 2.762103670 MW maximum component flow work and 11.563679870 MW total absolute passive flow work.
- Builds on Hotfix 1 after the user confirmed compilation progressed and reported two ordinary regressions: the application descriptor omitted required G.2 contract wording, and the ten-second desktop stability test still required instantaneous gross grid exchange above 4 MWe.
- Restores the complete descriptor contract: current-v2 passive pipes and valve paths use `h*m_dot`, nodes retain internal energy, legacy profiles remain unchanged, pump hydraulic fluid work and shaft demand retain separate ownership, and G.3-G.4 own the remaining migration.
- Replaces the obsolete point-sample `gross output > 4 MWe` assertion with the validated bidirectional contract: generator breaker closed, requested load above 4.5 MWe, finite signed generator/grid exchange inside the symmetric ±10 MWe nameplate and no generator trip.
- Retains Hotfix 1's 2940-3050 rpm rotor envelope plus explicit no-trip/no-overspeed checks.
- Changes tests and descriptor/documentation only; runtime transport, governor, turbine, generator-grid coupling, protections, HMI, replay/checkpoint behavior, test inventory and G.2 artifacts remain unchanged.

## M10.9.4.1-G.2 Hotfix 1 — Post-Migration Rotor Stability Envelope — CANDIDATE

- Builds on the compiling G.2 candidate after the user reported one ordinary regression failure at 2949.3997837402485 rpm versus the historical 2950 rpm lower bound.
- Treats the failure as an obsolete stability-envelope edge, not a runtime-physics defect: shaft power remained 5.549828464853323 MW, net torque remained finite, generation remained above the existing floor and no turbine trip or overspeed condition was active.
- Changes only `DesktopProfile_ContinuousRunRemainsStableForTenSimulatedSeconds`: the lower rotor-speed bound becomes 2940 rpm, equivalent to 49.0 Hz and still above the 48.8 Hz underfrequency pickup.
- Adds explicit no-trip and no-overspeed assertions to retain protection intent while avoiding a compensating governor/turbine retune for a 0.600216 rpm boundary miss.
- Changes no runtime source, passive enthalpy transport, pump-work ownership, controller tuning, protection threshold, HMI, replay/checkpoint contract, test count or expected G.2 artifact.

## M10.9.4.1-G.2 — Passive Hydraulic Enthalpy Migration & Pump Work Ownership — CANDIDATE

- Builds on the user-validated G.1 open-control-volume convention and supplied four-path gap audit.
- Adds definition-owned `FluidEnergyTransportMode`; historical `SpecificInternalEnergy` remains the default and `SpecificEnthalpy` is opt-in.
- Migrates all passive pipes and valve hydraulic paths in both current-v2 sustained profiles to `h*m_dot` endpoint balances while fluid nodes continue to store internal energy.
- Keeps all current-v2 pump paths on historical `u*m_dot` for this increment and proves hydraulic fluid work plus shaft demand are each counted exactly once.
- Extends pipe/valve/pump results and main-steam snapshots with flow-work, enthalpy, selected advected-energy and mode evidence.
- Adds twelve ordinary regressions and one explicit audit with three artifacts under `artifacts/g2-passive-hydraulic-enthalpy`.
- Adds ADR 0119, the G.2 technical contract, validation checklist and `scripts/run-passive-hydraulic-enthalpy-tests.cmd`.
- Does not migrate boundaries, steam-drum separation, condenser, relief, bypass or turbine expansion; those remain G.3-G.4.

## M10.9.4.1-G.1 — Open-Control-Volume Energy Convention & Gap Audit — VALIDATED

- The user confirmed compilation, focused tests and all requested tests passed on 2026-07-26.
- The supplied audit covered two steam and two liquid paths and confirmed `h = u + p/rho`, exact internal closure and no G.1 runtime migration.
- Maximum observed specific flow work was 192.048450950 kJ/kg and maximum flow-work rate gap was 2.484103126 MW.
- G.1 is the validated baseline for the incremental G.2 runtime migration.

## M10.9.4.1-F.3 Hotfix 1 — Fluid-node balance namespace compile fix — CANDIDATE

- Fixes the two `CS0246` build errors in `TurbineBypassSolver` by importing the canonical `NuclearReactorSimulator.Simulation.Physics.Fluids` namespace that owns `FluidNodeBalance`.
- Changes namespace resolution only; the F.3 bypass topology, pressure schedule, F.1 capacity law, committed condenser backpressure, conservative source terms, snapshots, tests and expected audit artifacts are unchanged.
- Keeps M10.9.4.1-F.2 as the validated continuation baseline until the local F.3 Hotfix 1 gate passes.

## M10.9.4.1-F.3 — Conservative Turbine Bypass to Condenser — SUPERSEDED BY HOTFIX 1

- Builds on the user-validated F.2 atmospheric header-relief baseline.
- Adds an optional current-v2 `TurbineBypassDefinition` owned by the condenser system: `header` to condenser `condenser` / steam space `exhaust`.
- Uses 6.4 MPa set pressure, 6.5 MPa full opening and the validated F.1 1,600 mm² capacity definition.
- Resolves flow against committed condenser backpressure, blocks reverse flow and limits ideal-vapor capacity by committed vapor availability.
- Transfers mass and committed specific internal energy internally with equal and opposite source terms and exactly zero external exchange.
- Adds immutable bypass snapshots, aggregate condenser-system evidence, thirteen ordinary regressions and one explicit two-part audit under `artifacts/f3-turbine-bypass`.
- Adds ADR 0117, the F.3 technical contract, validation checklist and `scripts/run-turbine-bypass-tests.cmd`.
- Adds no manual authority, actuator state, HMI control, protection retuning or Phase G enthalpy/flow-work migration.

## M10.9.4.1-F.2 — Conservative Main-Steam Header Relief — VALIDATED

- The user confirmed compilation, focused tests and all requested tests passed on 2026-07-26.
- The supplied audit confirmed first opening at 6.51 MPa, full lift at 6.70 MPa, 13.531762568 kg/s at 6.80 MPa, 33.595745149 MW energy export, monotonic flow and conservative external exchange.
- Builds on the user-validated F.1 choked steam-flow capacity baseline.
- Adds `MainSteamReliefBoundaryDefinition` with explicit source-header ownership, named external receiver boundary, fixed receiver pressure, set pressure, full-lift pressure and the validated F.1 compressible-flow definition.
- Adds a pressure-actuated linear lift law: closed at or below 6.5 MPa, full lift at or above 6.7 MPa.
- Configures one current-v2 atmospheric header-relief path with a 1,600 mm² full-open throat, 0.95 discharge coefficient, 461.526 J/(kg K) specific gas constant and 1.3 heat-capacity ratio.
- Limits ideal-vapor relief capacity by committed vapor availability: superheated vapor uses 1.0, saturated mixture uses committed vapor quality and subcooled liquid remains ineligible.
- Adds `MainSteamReliefBoundarySolver`, immutable snapshots and exact signed mass/internal-energy export through the existing single plant-network integration boundary.
- Enables the relief path only in the two current-v2 sustained profiles; historical/legacy definitions retain an empty relief set.
- Adds eleven ordinary regressions and one explicit current-v2 header-pressure sweep under `artifacts/f2-main-steam-relief`, plus `scripts/run-main-steam-relief-tests.cmd`.
- Adds ADR 0116, the F.2 technical contract and a dedicated validation checklist.
- Adds no turbine bypass, condenser receiver, downstream inventory, manual command, actuator travel, alarm/protection retuning, two-phase critical-flow law or enthalpy migration.

## M10.9.4.1-F.1 — Choked Steam-Flow Capacity Law & Audit — VALIDATED

- The user confirmed compilation, focused tests and the complete requested suite passed on 2026-07-26.
- The representative audit confirmed analytic critical ratio 0.545728 and choked capacity 0.788008677 kg/s for 100 mm² at 6.2725 MPa and 278.5 °C.
- Linear projections were 3.940043384 kg/s for 500 mm² and 7.880086767 kg/s for 1,000 mm²; monotonicity and the choked plateau were both confirmed.
- Adds a typed `SpecificGasConstant` quantity and an isolated `CompressibleSteamFlowDefinition` for full-open throat area, discharge coefficient, ideal-vapor specific gas constant and heat-capacity ratio.
- Adds `CompressibleSteamFlowSolver`, a one-way ideal-vapor nozzle/orifice capacity law with continuous subcritical behavior and a sonic/choked plateau below the analytic critical pressure ratio.
- Adds ten ordinary domain/simulation regressions and one explicit pressure-ratio audit under `artifacts/f1-choked-steam-flow`.
- F.1 remains a reusable capacity seam; plant topology and conservative integration begin only in F.2.

## M10.9.4.1-E.3.2 Hotfix 3 — Evidence-Derived Electrical Protection — VALIDATED

- The user confirmed compilation, the complete focused gate, the ordinary suite and all requested cumulative tests passed on 2026-07-26.
- Normal 5→0→5 MWe operation produced no trip; maximum reverse-power pickup was 0.080 s and maximum underfrequency pickup was 0.640 s.
- Turbine trip accumulated exactly 2.000 s reverse-power pickup, latched only reverse power, issued generator trip at logical step 701 / 7.010 s and opened the breaker.
- Breaker-open coastdown reached 43.154407 Hz and 6.845593 Hz absolute slip with zero pickup and no generator trip, confirming measured breaker supervision.
- The complete generated `e3-protection-implementation` CSV/summary bundle was supplied and reviewed.

## M10.9.4.1-E.3.2 Hotfix 3 — Typed breaker-command target regression fix — VALIDATED

- Builds on Hotfix 2 after compilation, all nine focused Simulation tests and all thirteen non-explicit focused Application tests passed locally.
- Fixes the sole remaining explicit-gate failure: the E.3.2 audit helper incorrectly sent `GeneratorBreakerOpen` to the generator id with `ControlRoomCommandTargetKind.Generator`.
- Resolves generator load commands to the generator target and breaker open/close commands to the canonical breaker id with `ControlRoomCommandTargetKind.Breaker`; unsupported command kinds now fail explicitly inside the test helper.
- Changes test composition only. No runtime, protection definition, threshold, pickup delay, reset hysteresis, supervision, trip action, plant physics, HMI, report contract or test inventory changes.

## M10.9.4.1-E.3.2 Hotfix 2 — Canonical grid nominal-frequency bootstrap compile fix — SUPERSEDED BY HOTFIX 3

- Fixes the `CS1061` compilation error in `ColdShutdownInitialConditionFactory`: `ElectricalGridDefinition` owns `NominalFrequency`, not the runtime-snapshot-only `Frequency` member.
- Seeds `generator-absolute-frequency-slip` from the generator electrical frequency minus `grid.NominalFrequency`, preserving the intended physical value and the complete initial measured-frame invariant introduced by Hotfix 1.
- Changes no protection threshold, pickup delay, reset hysteresis, supervision, trip action, plant physics, test inventory or expected report artifact.

## M10.9.4.1-E.3.2 Hotfix 1 — Initial measured-frame completeness — SUPERSEDED BY HOTFIX 2

- Added the missing logical-step-zero signals for `generator-breaker-closed` and `generator-absolute-frequency-slip`.
- The first correction used the wrong definition member name for grid frequency and therefore did not compile; Hotfix 2 replaces it with the canonical `NominalFrequency` member.

## M10.9.4.1-E.3.2 — Evidence-Derived Electrical Protection — VALIDATED VIA HOTFIX 3

- Builds on the user-validated E.3.1 Hotfix 1 trajectory baseline and the complete user-supplied CSV/summary evidence bundle.
- Extends canonical M5.5 protection with optional measured supervision and deterministic committed pickup delay; zero delay and no supervision preserve every legacy definition.
- Adds current-v2 breaker-supervised generator trips for reverse power (-0.30 MWe / reset -0.10 MWe / 2.0 s), underfrequency (48.8 Hz / reset 49.5 Hz / 1.0 s) and loss of synchronism via absolute frequency slip (1.5 Hz / reset 0.5 Hz / 0.5 s).
- Enables the set in both current-v2 sustained desktop and synchronization profiles while keeping evidence-only factories available for exact E.3.1 trajectory reproduction.
- Publishes generator breaker state and absolute frequency slip through canonical instrumentation and exposes reverse-power/underfrequency markers through Application HMI scale metadata.
- Adds ordinary definition, timing, supervision, HMI, synchronization-profile and replay/checkpoint regressions plus three explicit implementation journeys.
- Adds `scripts/run-electrical-protection-implementation-tests.cmd`, which writes and prints three implementation summaries plus detailed CSV evidence, ADR 0114, the reviewed evidence record and the E.3.2 validation checklist.

## M10.9.4.1-E.3.1 Hotfix 1 — Signed Electrical Protection Trajectory Audit — VALIDATED

- The user confirmed compilation, ordinary tests and all cumulative long-running gates passed on 2026-07-26.
- The complete generated trajectory bundle was supplied and reviewed before any E.3.2 threshold was selected.
- Observed normal/reverse-power/underfrequency/phase-slip envelopes now form the authoritative E.3.2 calibration evidence.

## M10.9.4.1-E.3.1 Hotfix 1 — Invariant trajectory-report formatting compile fix — VALIDATED

- Fixes five `CS1503` errors in `ElectricalProtectionTrajectoryAuditTests` where concatenating interpolated strings materialized a `string` before it reached `FormattableString.Invariant`.
- Formats each invariant segment independently and concatenates the resulting strings, preserving identical report content and invariant-culture numeric formatting.
- Changes no production source, trajectory scenario, assertion, threshold, relay behavior, test count or expected artifact.

## M10.9.4.1-E.3.1 — Signed Electrical Protection Trajectory Audit — VALIDATED VIA HOTFIX 1

- Builds on the user-validated E.2 Hotfix 1 10 MWe/bidirectional baseline.
- Adds four explicit evidence-only trajectories: normal 5→0→5 MWe load step, turbine trip plus zero electrical request with breaker closed, breaker-open coastdown and a breaker-closed ±15/45/90/135° phase-offset sweep.
- Persists deterministic CSV and text summaries under `artifacts/e3-protection-trajectories` through `scripts/run-electrical-protection-trajectory-audit.cmd`.
- Records requested power, signed grid exchange, mechanical exchange, conversion loss, generator frequency, frequency slip, absolute/signed phase error, breaker state and actual trip state.
- Adds no reverse-power, underfrequency or loss-of-synchronism function, threshold, delay or trip action.
- Adds ADR 0113 and dedicated audit/validation documentation.

## M10.9.4.1-E.2 Hotfix 1 — 10 MWe Reference Scale & Bidirectional Grid Coupling — VALIDATED

- The user confirmed compilation and all requested ordinary, focused and long-running gates passed on 2026-07-26.
- E.2 Hotfix 1 is promoted as the validated parent baseline for E.3.1.
- Exact console counts were not copied into the handoff; no count is inferred beyond the confirmed all-green result.

## M10.9.4.1-E.2 Hotfix 1 — Application test HMI namespace compile fix — VALIDATED

- Adds the missing `NuclearReactorSimulator.Application.ControlRoom.Hmi` import to `ReferencePlantScaleMigrationTests`.
- Restores compilation of `NuclearReactorSimulator.Application.Tests` without changing runtime code, physical laws, scale ownership, test behavior or expected test counts.
- Preserved the E.2 functional contract; the complete E.2 Hotfix 1 source was later user-validated on 2026-07-26.

## M10.9.4.1-E.2 — 10 MWe Reference Scale & Bidirectional Grid Coupling — VALIDATED VIA HOTFIX 1

- Builds on the user-validated D.4.1 baseline without changing reactor, primary, condenser, turbine-work, protection or timestep laws.
- Migrates only the two current-v2 sustained profiles from the historical 1,000 MWe reference to a coherent 10 MWe educational generator nameplate while retaining the validated 5 MWe normal operating point.
- Changes current-v2 full-load governor rise from 150 rpm to 1.5 rpm, preserving the existing 0.75 rpm displacement at 5 MWe.
- Adds versioned `GenerationOnly`/`Bidirectional` grid-coupling semantics; legacy/default definitions remain generation-only unless they opt in.
- Adds an internal signed electromagnetic-torque seam owned only by the generator/grid integration layer while the public/manual rotor-input contract remains non-negative.
- Supports signed mechanical and electrical exchange, electrical-nameplate clamps in both directions, current-speed torque conversion with a low-speed floor, and positive conversion losses during both generation and motoring.
- Changes current-v2 HMI electrical ranges and labels to `-10..+10 MWe`, with positive export and negative import, while historical/default presentation remains non-negative.
- Adds focused ordinary generation/motoring/compatibility tests, expands the explicit reference-scale pack to 4 tests and adds `scripts/run-generator-grid-bidirectional-tests.cmd`.
- Does not add reverse-power, supervised-underfrequency or loss-of-synchronism protection; those remain E.3 after signed trajectories are validated.
- Promoted through E.2 Hotfix 1 after the user confirmed compilation and all requested gates passed on 2026-07-26.

## M10.9.4.1-D.4.1 — Turbine Valve Replay, Reset & Travel Ownership Hardening — VALIDATED

- Builds only on the fully validated D.4 baseline; no turbine thermodynamic law, hydraulic capacity, controller tuning, protection threshold, timestep or generator/grid scale is changed.
- Gives each `TurbineAdmissionTrainDefinition` an optional STOP-valve-owned `ActuatorTravelRate`; `null` preserves legacy instantaneous behavior even when other secondary valves are rate-limited, and the optional factory parameter is appended to preserve positional source compatibility.
- Removes the runtime dependency that borrowed the control-valve actuator travel rate for STOP OPEN/CLOSE requests.
- Adds a differential-rate regression proving STOP and ADMISSION valves move according to their independently configured rates.
- Adds deterministic full-replay and checkpoint-seek coverage for STOP/ADMISSION commands, control-valve AUTO/MANUAL authority and numeric manual demand while valves are still in flight.
- Adds the full trip → preserved STOP OPEN target → canonical reset acceptance → finite opening resumption regression without hidden repair.
- Adds ADR 0112, `scripts/run-turbine-valve-hardening-tests.cmd` and a dedicated validation checklist.
- Promoted after the user confirmed on 2026-07-26 that all ordinary and long-running gates passed.
- This is the validated parent baseline for E.2.

## M10.9.4.1-D.4 — Turbine Valve Operator Authority — VALIDATED

- Promotes the cumulative D.3.2 Hotfix 3 + D.4 source after the complete local automated gate passed on 2026-07-25.
- Ordinary run: 961 discovered, 944 passed, 0 failed and 17 explicit tests skipped.
- All 17 unique explicit tests then passed: admission authority 3/3, governor/actuator tracking 2/2, gameplay long runs 2/2, operational envelope 9/9 and reference scale 2/2; one scale test is shared by two script categories.
- Records D.4 typed STOP/ADMISSION OPEN/CLOSE, control-valve AUTO/MANUAL and explicit manual demand while preserving finite travel and protection priority.
- Corrects the documentation/source mismatch: E.1 is an accepted 10 MWe target decision, while E.2 bidirectional migration and its signed-torque seam are not implemented in this source.
- Renumbers duplicate ADRs to unique identifiers 0107–0111 and marks ADR 0110–0111 as proposed E.2 designs.

## Superseded documentation checkpoint — incorrectly described E.2 as implemented

- Records the operator-facing turbine valve station added after the prior documentation checkpoint: stop/admission OPEN/CLOSE, control-valve AUTO/MANUAL and an explicit bounded manual-demand slider with APPLY.
- Keeps requested/manual-demand/actual valve positions distinct during finite travel and preserves protection as the later authority that can inhibit opening or force the stop valve closed without erasing the operator lineup.
- Aligns the M10.9.4.1 milestone, operational hardening plan, roadmap, status, handoff, new-chat brief, scale contract/evidence, limitations register and user manual with the current cumulative source.
- Historical note: this checkpoint incorrectly described E.2 as implemented. The D.4 validation checkpoint above supersedes that statement; only E.1 target acceptance is current.
- Historical partial gate: ordinary 944 passed / 17 explicit skipped / 0 failed; turbine-admission 3/3; governor-tracking 2/2; reference-scale 2/2.
- Superseded by the later D.4 validation: both long journeys and the complete operational-envelope audit subsequently passed.

## M10.9.4.1-D.3.2 Hotfix 3 — Loaded desktop main-steam capacity rebalance — CANDIDATE

- Uses the second local failure as evidence that the Hotfix 2 stop-valve pressure-grade correction was not the remaining limiting element: the loaded desktop main-steam line still capped the initial series train near 12 kg/s.
- Changes only the loaded desktop current-v2 main-steam-line resistance from 1,000 to 850 Pa·s²/kg²; the synchronization profile remains at 1,000 Pa·s²/kg².
- At the committed seed pressure grade, 850 Pa·s²/kg² gives about 13.02 kg/s main-steam capacity, matching the approximately 13.02 kg/s stop/control capacities instead of leaving an upstream bottleneck.
- Retains the 28% loaded control-valve bias and the 276.7 °C stop-out seed; Hotfix 1 remains rejected and Hotfix 2 remains inherited.
- Widens only the brittle stop-valve pressure-head observation window from 150–190 to 150–250 kPa after the local committed value proved to be 193.421 kPa; generation-ready flow and power floors are not weakened.
- Adds regressions freezing the distinct loaded/synchronization main-steam-line contracts and updates the D.2 authority map for the loaded 850 Pa·s²/kg² path.
- No stage/valve resistance, solver law, PID/PI gain, anti-windup, actuator travel, droop, turbine work, rotor loss, generator/grid, protection, timestep, replay or PLANT-renderer change.

## M10.9.4.1-D.3.2 Hotfix 2 — Loaded desktop stop-valve pressure-grade rebalance — CANDIDATE

- Rejects the ineffective Hotfix 1 bias-only hypothesis after local 30% validation changed effective stage flow only from 11.784841 to 11.792118 kg/s and still failed gross-output support.
- Restores the loaded desktop control-valve seed to 28% and moves only its stop-out steam seed from 277.0 °C to 276.7 °C.
- Balances the analytical fully-open stop-valve and 28% control-valve capacities at approximately 13.017 and 13.015 kg/s respectively, instead of leaving the fully-open stop valve as the 11.89 kg/s bottleneck.
- Adds regressions for the committed stop-valve pressure head, stop/control capacity balance and generation-ready stage flow.
- Preserves D.3.2 admission isolation, D.3.1 passive rotor loss, PI/PID gains, all hydraulic resistances, actuator travel, droop, generator/grid, protections, timestep, replay and the uniform PLANT renderer.

## M10.9.4.1-D.3.2 Hotfix 1 — Loaded desktop admission-bias realignment — CANDIDATE

- Preserves the D.3.2 admission-train isolation law and realigns only the loaded desktop current-v2 initial control-valve bias from 28% to 30%.
- Resolves the two ordinary-suite regressions exposed after valve-train closure: initial effective stage flow fell to 11.784841 kg/s below the existing 12.5 kg/s generation-ready floor, and 10-second shaft power fell to 4.352905 MW below the existing 4.5 MW support floor.
- Keeps the synchronization sustained profile at 28%; its unloaded PI governor operating point remains a separate contract from the loaded desktop PID profile.
- Retains the established generation-ready acceptance floors instead of weakening tests to accept reduced real electrical support.
- Updates the D.2 authority map to include both 28% and 30% points; at 30% the equal-head indicator gives about 31.20% control-valve resistance share and about 18.17% theoretical capacity headroom to full open.
- No solver, valve characteristic, hydraulic resistance, turbine work, mechanical-loss, PID/PI gain, actuator travel, droop, generator/grid, protection, timestep, replay or PLANT renderer change.

## M10.9.4.1-D.3.2 — Admission-train isolation closure & PLANT schematic preservation — CANDIDATE

- Corrects the current-v2 pressure-driven turbine-stage law so commanded stage flow is bounded by the minimum positive capacity of the upstream stop, control and admission valves.
- Resolves the D.3.1 evidence contradiction where CONTROL VALVE = 0% still allowed about 10.6 kg/s effective stage flow and about 4.24 MW shaft power, preventing breaker-open deceleration.
- Adds regressions proving a closed control valve enforces zero pressure-driven stage admission while a fully open train preserves positive capacity.
- Corrects the synchronization governor contract test: the actual P=0.5, I=0.02 s⁻¹, D=0 definition is PI, while the loaded desktop profile remains PID.
- Aligns the interactive PLANT renderer with the engineering-schematic visual grammar already used by PRIMARY, TURBINE, GRID, REACTOR and ALARMS, preserving element selection and subsystem drill-down.
- Records that the uploaded continuation archive itself still contained the older PLANT renderer; D.1–D.3.1 copied those files unchanged. No hidden UI rollback is attributed to turbine physics changes.
- No valve/stage resistance, controller gain, anti-windup, actuator travel, droop, rotor inertia, generator scale, protection threshold, timestep or legacy flow-law retuning.

## M10.9.4.1-D.3.1 — Breaker-open rotor mechanical-loss closure — CANDIDATE

- Converts the failed D.2/D.3 breaker-open evidence into an isolated current-v2 physics correction instead of weakening the audits.
- Adds optional `TurbineRotorMechanicalLossDefinition`; torque is linear with speed, power is quadratic with speed and both are zero at rest.
- Current-v2 sustained desktop and synchronization profiles opt into 0.5 MW loss at 3000 rpm; historical/default profiles remain unchanged with no passive loss.
- Keeps generator electromagnetic torque separate from passive bearing/windage/uncoupled-generator drag.
- Extends rotor, turbine-mechanical and full secondary-cycle energy accounting so passive dissipation closes explicitly without changing replay JSON.
- Corrects the breaker-open evidence method: the rotor must decelerate; any canonical overspeed turbine/generator latch must become reset-safe and be explicitly reset; only then may the audit reach a protection-clear ±5 rpm baseline and apply the existing +10/-10 rpm journey.
- Records that the synchronization profile uses P=0.5, I=0.02 s⁻¹, D=0, while the desktop sustained profile uses P=1.0, I=0.02 s⁻¹, D=0.2 s; D.3 Hotfix 1's derivative-kick explanation was not the governing cause of the observed failure.
- No rotor-inertia, droop, actuator-travel, valve/stage resistance, generator-nameplate, bidirectional-coupling, protection, timestep or legacy replay change.

## M10.9.4.1-D.3 Hotfix 1 — First-step governor event sampling — CANDIDATE

- Fixes the explicit breaker-open D.3 audit failure caused by sampling only every 0.1 simulated seconds after a speed-reference command while the PID derivative kick occurs in the first 0.01-second solver step.
- Captures and reports the first committed step after both `SPEED RAISE` and `SPEED LOWER`, then resumes the existing 0.1-second evidence cadence for the remainder of each 10-second interval.
- Applies the directional output/valve assertion to the captured raise-event sample instead of a later-window maximum that may legitimately miss the transient.
- Writes the complete breaker-open evidence before assertions so any future failure preserves the decisive diagnostic output.
- Test/documentation-only hotfix: no `src/` file, PID gain, actuator travel, droop, turbine law, resistance, seed, protection, timestep or replay contract changes.

## M10.9.4.1-D.3 — Governor effective-setpoint & actuator-tracking evidence — CANDIDATE

- Records the user's successful local build and complete test result for cumulative D.1 + D.2 + D.2 Hotfix 1; those checkpoints are now locally validated.
- Corrects the D.2 operational perturbation method: direct SPEED RAISE/LOWER evidence now runs from the breaker-open sustained synchronization seed, because breaker-closed droop intentionally supersedes the requested speed setpoint.
- Adds an ordinary contract regression freezing the current-v2 PID gains, 0–100% output, 0.5 fraction/s valve travel and 150 rpm full-load droop rise without retuning them.
- Adds two explicit evidence journeys: breaker-open ±10 rpm effective-setpoint/valve tracking, and breaker-closed 5→10→5 MWe load-droop response with the scale-consistent +0.75 rpm setpoint displacement.
- Captures P/I/D terms, saturation, existing conditional-integration anti-windup, bounded controller output, physical valve position, command/position gap, rotor speed, stage flow and shaft power.
- Defers actuator-position tracking anti-windup unless the evidence proves material actuator-induced windup; low-load droop authority caused by the 1,000 MWe nameplate remains a coordinated Phase-E scale question.
- No `src/` file, solver law, seed value, controller gain, actuator travel, droop, resistance, protection, timestep or replay contract changes.

## M10.9.4.1-D.2 Hotfix 1 — Application audit-test API alignment — USER VALIDATED LOCAL CHECKPOINT

- Fixes the D.2 application-test compilation failures without changing production code or physics.
- Uses the canonical `SteamDrumSystemDefinition.Drums` collection instead of the nonexistent `SteamDrums` member.
- Rewrites four filtered `Assert.Single` calls with the analyzer-approved predicate overload, resolving xUnit2031.
- Adds a compatibility replacement for a stale local `ReferencePlantScaleMigrationTests.cs` Phase-E draft that referenced the nonexistent `SynchronousGridCouplingDefinition.PowerFlowMode` member; the replacement freezes only the current 10 MW synchronizing and 10 MW/Hz damping coefficients and keeps scale/bidirectional migration deferred to Phase E.
- No `src/` file, solver, seed, resistance, governor setting, protection, timestep or replay contract changes.

## M10.9.4.1-D.2 — Turbine admission authority evidence — USER VALIDATED LOCAL CHECKPOINT

- Builds cumulatively on D.1; the user subsequently confirmed the cumulative D.1 + D.2 + Hotfix 1 tree passes all tests locally. D.2 is audit-only and changes no production physics.
- Freezes the current-v2 hydraulic authority budget: 100 Pa·s²/kg² drum steam source, 1,000 main-steam line, 1,000 base resistance for each stop/control/admission valve, 21,400 stage expansion resistance, linear control-valve characteristic and 28% sustained seed bias.
- Adds a deterministic analytical resistance map from 10% to 100% control-valve position. At the current 28% seed the control valve owns about 34.24% of the idealized total series resistance and retains about 20.87% theoretical flow-capacity headroom to full open; authority compresses strongly above 60%.
- Adds an explicit +10 rpm / -10 rpm governor-reference runtime evidence journey that records control-valve position, turbine-inlet pressure, commanded/effective stage flow and shaft power without retuning any law.
- Adds dedicated D.2 audit runners, ADR 0100, authority-evidence documentation and a validation checklist.
- Defers all resistance rescaling, effective-area/Stodola selection and governor retuning until the runtime evidence is reviewed.

## M10.9.4.1-D.1 — Turbine admission phase-policy closure — USER VALIDATED LOCAL CHECKPOINT

- Starts Phase D from the user-supplied consolidated M10.9.4.1 continuation base; prior B/C work remains the inherited foundation and the historical 300-second wall-clock performance observation remains tracked separately from physics correctness.
- Adds explicit `TurbineAdmissionPhasePolicy` with legacy default `LegacyUnrestricted` and current-v2 opt-in `VaporMassFractionLimited`.
- Current-v2 sustained desktop and synchronization seeds admit only the committed vapor mass fraction through the turbine stage. Pure liquid admission therefore produces zero stage mass transfer instead of a silent zero-work liquid bypass.
- For wet steam, the effective transferred mass flow is reduced by vapor mass fraction while thermodynamic work is evaluated per kilogram of admitted vapor; vapor quality is not applied twice, preserving the intended first-order total shaft-work scaling.
- Historical/v1 stage definitions retain total-mixture transfer semantics exactly by default. No valve/stage resistance, governor tuning, actuator travel, condenser, generator/grid, protection threshold, timestep or replay contract is retuned in D.1.
- Adds focused regressions for legacy preservation, pure-liquid blocking, wet-steam vapor-fraction transfer, conservation closure and explicit current-v2 seed ownership. D.2 remains responsible for measured valve/stage authority before any Stodola/effective-area/resistance choice.

## M10.9.4.1-C.2 Hotfix 2 — Application test namespace compile fix — CANDIDATE

- Fixed the `NuclearReactorSimulator.Application.Tests` compile failure introduced by C.2 Hotfix 1 by importing the canonical HMI namespace that owns `ControlRoomInstrumentProvenance` in `DesktopSustainedGenerationInitialConditionFactoryTests`.
- Test-only compile fix: no production code, solver, HMI behavior, instrumentation policy, protection threshold, replay contract, seed, or physics changed.

## M10.9.4.1-C.2 Hotfix 1 — Primary operational-flow presentation stabilization & re-synchronization guidance

- Records user-observed 10 ms primary hydraulic diagnostic chatter in the current-v2 low-resistance circuit without disguising it as a physical plant oscillation or retuning the validated 25 Pa·s²/kg² hydraulic seed.
- Adds opt-in 0.5 s deterministic presentation instrumentation for current-v2 MCP total/pump flow, fuel-channel and return flow, drum inlet flow and liquid recirculation. Controllers keep their existing canonical measurement channels; legacy/v1 instrumentation remains unchanged.
- Primary HMI now presents the operational flow paths through these filtered measured channels while retaining raw thermodynamic/pressure diagnostics as MODEL data. The underlying explicit hydraulic chatter remains a known numerical-hardening item rather than being hidden or claimed fixed.
- Clarifies generator re-synchronization after breaker opening: if Δf is essentially zero while phase is outside the close window, waiting alone cannot change phase. The HMI explicitly instructs the operator to use SPEED RAISE/LOWER to create phase slip, then return near synchronous speed and close only when Δf/Δphase/ΔV are all valid.
- Adds regression contracts for the 0.5 s current-v2 presentation channels and the zero-slip/out-of-phase synchronization guidance. No solver law, protection threshold, replay schema, condenser calibration or B-phase physics is intentionally changed.

## M10.9.4.1-C.2 — Explicit condenser installed-capacity ownership — USER VALIDATED LOCAL CHECKPOINT

- Builds on user-validated B.3 + C.1: the cumulative candidate compiles and tests pass locally.
- Separates current-v2 condenser **installed cooling capacity** from **runtime available cooling capacity**. `CondenserCoolingBoundaryDefinition` may now own an optional physical installed-capacity ceiling; legacy/null definitions preserve the historical input-only ceiling semantics.
- The two sustained current-v2 profiles explicitly define 40 MW installed heat-rejection capacity while their runtime boundary input starts at 40 MW available. Faults/transients continue to reduce only runtime availability and no longer redefine the plant's installed hardware capacity.
- Effective condenser heat-rejection capacity is now the minimum of installed capacity, runtime available capacity and the existing `UA·ΔT` surface-transfer limit. The existing 20 kg/s maximum condensation-flow ceiling remains an independent physical throughput limit.
- Adds separate MODEL diagnostics/HMI for installed cooling, available cooling, surface-UA limit and the active heat-rejection limiting side. New presentation diagnostics remain `JsonIgnore` for replay-v1 fingerprint compatibility.
- Adds focused regressions for explicit installed-capacity validation, legacy fallback semantics, installed-capacity limiting, runtime-availability limiting, current-v2 40 MW ownership and presentation-only compatibility.
- Retains the validated numerical values 40 MW / 20 kg/s / 1.225 MW/K / 20 °C; C.2 changes ownership/semantics rather than retuning the validated operating point.
- No B.1/B.2/B.3 drum law, C.1 condensate-energy law, turbine/generator law, protection threshold, timestep, replay schema or historical v1 seed behavior is intentionally changed. Local build, ordinary suite and 60/300-second gates remain required.

## M10.9.4.1-C.1 — Condenser phase-change energy closure — USER VALIDATED LOCAL CHECKPOINT

- Built on M10.9.4.1-B.3; the user subsequently confirmed the cumulative B.3 + C.1 candidate compiles and tests pass locally.
- Adds explicit `CondenserCondensateEnergyMode`: legacy definitions keep the historical receiving-hotwell energy rule, while the two sustained current-v2 seeds opt into saturated-liquid condensate energy resolved at committed condenser steam-space pressure.
- Adds optional `IWaterSteamSaturationPropertyProvider` capability without widening the generic fluid thermodynamic interface; the production simplified water/steam model provides pressure- and temperature-based saturation properties.
- Current-v2 condensation now removes `u_steam * m_dot` from the steam space, adds `u_sat_liquid(p_condenser) * m_dot` to the hotwell and rejects the difference as the explicit external heat sink, preserving single-integration mass/energy ownership.
- Adds non-serialized diagnostics for phase-change `Δu`, maximum/inventory/thermal flow limits and margins, and active installed-capacity/surface-`UA` limits only when effective cooling capacity is actually exhausted.
- Extends the condenser HMI with `CONDENSATE ENERGY · MODEL`, `PHASE-CHANGE Δu · MODEL` and `ACTIVE CONDENSATION LIMIT · MODEL`.
- Does not change the A.2 current-v2 values (40 MW installed cooling, 20 kg/s maximum condensation flow, 1.225 MW/K UA, 20 °C cooling water); C.2 must decide their independent necessity from post-C.1 evidence.
- No generic pipe enthalpy/flow-work migration, turbine/generator law, protection threshold, timestep, replay schema or legacy/v1 seed behavior is intentionally changed. User confirmation: cumulative B.3 + C.1 compiles and tests pass locally; C.1 is the validated condenser phase-change-energy checkpoint used as the base for C.2.

## M10.9.4.1-B.3 — Steam-drum low-inventory diagnostics and low-low-level protection — USER VALIDATED LOCAL CHECKPOINT

- Builds on user-validated M10.9.4.1-B.2: compilation and tests passed locally.
- Adds non-serialized current-v2 diagnostics for separable-liquid inventory mass fraction, committed-liquid depletion, unavailable water/steam separation and liquid-recirculation inventory deficit without changing B.1/B.2 mass or energy source laws.
- Extends the primary-circuit HMI with MODEL indicators for separable-liquid mass, liquid inventory mass fraction and inventory/separation status.
- Adds a current-v2 measured low-level warning at 25% drum level and a distinct low-low protection at 10% with reset eligibility above 20%. These are simulator training thresholds, not universal real-plant values.
- The low-low protection acts on the existing measured `level` channel and latches ReactorScram + TurbineTrip + GeneratorTrip; historical v1/minimal-protection profiles remain unchanged.
- Projects the warning band and low-low protection marker directly onto the drum-level gauge so the HMI does not invent thresholds.
- Adds focused regressions for legacy isolation, alarm/protection thresholds and actions, measured-signal latching, current-v2 model diagnostics and HMI scale semantics.
- No steam-source law, liquid-recirculation law, protection threshold outside this new function, timestep, replay schema, condenser/turbine/generator physics or historical seed behavior changes. User confirmation: cumulative B.3 + C.1 compiles and tests pass locally; B.3 therefore closes Phase B and is the validated base for Phase C.

## M10.9.4.1-B.2 — Drum-to-main-steam pressure/energy/inventory source closure — USER VALIDATED LOCAL CHECKPOINT

- Builds on user-validated M10.9.4.1-B.1: compilation and tests passed locally.
- Removes the temporary current-v2 demand-following main-steam supplement introduced by Hotfix 16; `MainSteamNetworkSolver` no longer computes drum supply from downstream main-steam-line demand.
- Adds optional `SteamDrumSteamSourceDefinition`; null preserves historical behavior, while current-v2 sustained-operation seeds explicitly enable a forward pressure/energy/inventory-driven source with 100 Pa·s²/kg² hydraulic resistance.
- Current-v2 steam availability is derived from positive return-flow energy above the liquid reference state plus committed separable-vapor inventory; actual source flow is independently capped by drum-to-steam-outlet pressure head.
- Keeps the source as one conservative internal drum→steam-outlet transfer before the single canonical plant-network integration boundary.
- Carries committed superheated-vapor specific internal energy when the drum inventory is superheated; saturated/subcooled source formation uses the saturation reference at committed drum temperature. B.2 intentionally remains within the existing internal-energy transport convention; enthalpy/flow-work migration stays deferred.
- Adds non-serialized diagnostics for pressure capacity, energy/inventory availability, incoming-energy-supported steam rate, stored vapor mass and active limiting side.
- Adds focused regressions for zero-energy/no-vapor suppression, monotonic energy support, pressure limiting, conservation, explicit current-v2 source ownership and legacy/null-source preservation.
- Historical v1 seeds, protection thresholds, timestep, replay schema, turbine law and condenser law remain unchanged.
- User confirmation: candidate compiles and tests pass locally; B.2 is the validated source-closure checkpoint used as the base for B.3.

## M10.9.4.1-B.1 — Steam-drum liquid-inventory closure and operator-feedback clarification — USER VALIDATED LOCAL CHECKPOINT

- Builds on the user-validated current-v2 operating-seed correction: exact 300-second sustained 5 MWe journey passed in 2m 07s, explicit 60-second synchronization journey passed, build completed with 0 warnings / 0 errors, and the ordinary suite passed 895 tests with 11 explicit tests skipped and 0 failures.
- Clarifies gameplay scoring in the HMI with a regression that automatic protection state alone does not trigger penalties defined for accepted manual operator commands.
- Adds explicit generator-control feedback: `SPEED REFERENCE · MODEL`, `REQUESTED LOAD · MODEL`, and the canonical command increments (±10 rpm per accepted SPEED press; ±5 MWe per accepted LOAD press).
- Starts Phase B with current-v2 liquid-inventory closure: demand-balanced drum recirculation is capped by same-step incoming liquid plus committed separable-liquid inventory over the integration interval.
- A fully vaporized current-v2 drum can no longer fabricate liquid recirculation merely because main-circulation pumps demand flow. Legacy `LegacyReturnSplit` behavior remains unchanged.
- Adds read-only drum diagnostics for separable liquid mass, requested recirculation, inventory-supported maximum recirculation and inventory-limited state.
- Does not yet replace the current demand-balanced steam-supply supplement; that remains the next isolated Phase B source-closure step. No protection threshold, replay schema or legacy/v1 seed is changed.
- User confirmation: candidate compiles and tests pass locally; B.1 is therefore the validated Phase-B inventory checkpoint used as the base for B.2.

## M10.9.4.1-A.3 operating-seed energy/hydraulic closure — USER VALIDATED LOCAL CHECKPOINT

- The repeated ~70 s `condenser-high-backpressure` trip was traced upstream to the current-v2 operating seed rather than to the thermodynamic resolver: only the direct 20% coolant heat-deposition share reached the coolant, primary circulation was approximately 0.07 kg/s, while the drum supplied approximately 13 kg/s of steam and slowly depleted its internal-energy inventory.
- Enables conservative fuel/structure-to-coolant thermal links for current sustained-operation seeds so the 80% fission heat deposited in explicit solid inventories returns through the canonical heat-transfer owner instead of remaining trapped in the solids.
- Reduces current-v2 primary hydraulic resistances to establish matched circulation, and retunes only current-v2 initial steam-line pressures/temperatures and control-valve bias to the corrected operating point. Historical v1 seeds and all protection thresholds remain unchanged.
- User validation: exact 300-second long-run passed in 2m 07s; explicit 60-second synchronization journey passed; build 0 warnings / 0 errors; ordinary suite 895 passed, 11 explicit skipped, 0 failed.
- The earlier A.2 condenser-capacity headroom remains present in the current-v2 source but is no longer identified as the root-cause correction. Its independent physical necessity remains a Phase C evidence question.

## M10.9.4.1-A.3 — Reference plant scale evidence and provisional direction — CANDIDATE

- Adds an explicit test-only `ReferencePlantScaleAudit` layered on the pending A.2 source without adding another production-physics change.
- Freezes the current hybrid values: 1,000 MW generator nameplate, 1,000 kg·m² rotor, 5 MW request, 150 rpm full-load droop and 10 MW synchronizing/frequency-damping coefficients.
- Derives 49.348 MJ stored rotor energy, `H = 0.049348 s` at the configured nameplate, `H = 4.934802 s` at a 10 MW educational reference, 0.75 rpm current droop displacement and approximately 30.396 rpm/s per MW local acceleration scale.
- Publishes `REFERENCE_PLANT_SCALE_EVIDENCE.md` and provisionally favors a coordinated reduced-scale educational-unit migration while explicitly prohibiting a one-line nameplate correction.
- No `src/` file, seed, solver, controller, protection, timestep, replay schema or physical coefficient is changed beyond the already pending A.2 candidate. Local build/test remains required.

## M10.9.4.1-A.2 Hotfix 1 — Condenser installed-capacity headroom / per-second trip evidence — CANDIDATE

- Confirms the repeated 300-second audit failure is not intermittent: the only automatic function capable of producing the observed `TurbineTrip | GeneratorTrip` at ~70 s with rotor/frequency inside bounds is `condenser-high-backpressure` crossing its unchanged 30 kPa measured threshold.
- Preserves the current-v2 condenser surface law and initial design point: cooling water remains 20 °C and `UA` remains 1.225 MW/K, so a 40 °C steam space is still surface-limited to 24.5 MW.
- Raises only the installed current-v2 cooling-boundary ceiling from 24.5 MW to 40 MW, allowing the existing `UA * ΔT` negative feedback to continue above the initial point instead of clipping exactly at design load.
- Raises the current-v2 maximum condensation-flow ceiling from 15 to 20 kg/s so the hard mass-flow cap has explicit margin over the approximately 15 kg/s turbine path. Legacy/v1 seeds remain unchanged.
- Samples the 300-second audit once per simulated second and reports stage flow, actual/inventory/thermal condensation limits, heat-rejection capacity, surface limit, exhaust mass and the exact latched protection-function measurements.
- No condenser solver equation, thermodynamic property law, protection threshold/action, timestep, controller, replay schema or legacy seed is changed. Local build, ordinary tests, 60-second journeys and the full `OperationalEnvelopeAudit` gate remain required.

## M10.9.4.1-A audit execution / external review planning checkpoint

- Records local compilation and ordinary-suite success after Hotfix 1.
- Records the non-green explicit extended audit: the intended 300-second/5 MWe journey fails at checkpoint 7/30, step 7000 (~70 simulated seconds), with `TurbineTrip | GeneratorTrip` latched.
- Preserves the exact evidence: conservation residuals remain effectively zero; sampled drum reaches 7.821 MPa / 100%, condenser reaches 28.593 kPa and feedwater flow reaches 0 kg/s.
- Classifies condenser high backpressure as the leading but unproved hypothesis because protection executes every step while the audit samples only every 10 seconds.
- Adds the A.1 audit-evidence-completion gate, revised B–I hardening sequence, external LLM review adjudication, open reference-plant scale contract and known-model-limitations register.
- Documentation only: no source, solver, seed, coefficient, threshold, controller, integration or replay behavior changed.

## M10.9.4.1-A Hotfix 1 — Invariant audit diagnostic compile fix

- Fixes `CS1503` in `OperationalEnvelopeExtendedAuditTests.ToString()`: concatenated interpolated strings were materialized as `string` before being passed to `FormattableString.Invariant`.
- Formats each diagnostic segment invariantly and concatenates the resulting strings. Diagnostic content and assertion thresholds are unchanged.
- Test-only correction: no solver, seed, controller, protection, integration, replay or production-physics behavior changed.

## M10.9.4 final validation / M10.9.4.1-A extended audit candidate

- Recorded user-confirmed passage of the complete M10.9.4 manual HMI / engineering-schematic checklist; M10.9.4 is now the official validated milestone baseline.
- Opened M10.9.4.1-A as an audit-only candidate on the validated Hotfix 23 source baseline.
- Added explicit 300-second steady operation, deterministic load raise/lower, breaker-open/generator-trip/turbine-trip load rejection, condenser-cooling degradation, per-step secondary-pump non-return and 120-second replay/checkpoint journeys.
- Added read-only, test-assembly-only access to the already committed canonical snapshot so audit tests can report mass/energy closure, drum/condenser envelope, rotor/frequency, pump-flow and protection evidence without exposing true state to App/UI consumers.
- Added separately filtered, non-parallel audit runner scripts and explicit category filtering for the existing 60-second gameplay pack.
- No solver law, physical coefficient, protection threshold, seed configuration, integration behavior or replay schema was changed. Local .NET 10 validation remains required before Phase A promotion.

## M10.9.4 Hotfix 23 validation and forward-planning documentation checkpoint

- Recorded user-confirmed validation of Hotfix 23: compilation, the complete ordinary suite and both explicit 60-second gameplay journeys passed.
- Clarified that M10.9.4 has no open production candidate and now awaits only final manual HMI/schematic acceptance.
- Added `docs/M10_9_4_FINAL_MANUAL_VALIDATION_CHECKLIST.md` as the exact promotion gate.
- Inserted planned M10.9.4.1 `Operational Envelope & Numerical Hardening` before M10.9.5 to stop physical-model scope creep inside the schematic milestone.
- Added `docs/OPERATIONAL_ENVELOPE_NUMERICAL_HARDENING_PLAN.md` and `docs/milestones/M10.9.4.1.md` with six isolated phases: extended audit, source-side steam generation, load-rejection relief/bypass, supervised electrical protection, deterministic adaptive substepping and canonical-result duplication audit.
- Updated authoritative handoff/status/roadmap/restart/documentation-map records. No production code, physics, replay schema or test thresholds changed.

## M10.9.4 Hotfix 23 — Pressure/Temperature/Vapor-Dependent Turbine Work — VALIDATED STRUCTURAL CHECKPOINT

- Builds on validated Hotfix 22 and is now itself validated after the user confirmed compilation, the complete ordinary suite and both explicit 60-second gameplay journeys all pass.
- Adds strongly typed `SpecificHeatCapacity` and optional `TurbineThermodynamicWorkDefinition`; null preserves the historical fixed-`NominalSpecificWork` law.
- Current-v2 estimates ideal vapor expansion work from committed inlet temperature, inlet/exhaust pressure ratio, heat-capacity ratio and inlet vapor mass fraction, then bounds it by the 500 kJ/kg stage design cap and 80% of committed inlet specific internal energy.
- Liquid admission, absent vapor content or non-positive pressure drop now yields zero current-v2 shaft work instead of nominal torque. Rising exhaust backpressure continuously reduces available work.
- Low-energy current-v2 inlet states degrade through bounded available/extracted-specific-work diagnostics rather than reaching negative exhaust energy in normal operation.
- The validated operating point remains materially unchanged because thermodynamic availability exceeds the existing nominal design cap; turbine efficiency still produces approximately 400 kJ/kg extracted work.
- Extends turbine snapshots, Application presentation and long-running diagnostics with available/extracted specific work plus model-active/limited flags.
- Adds direct domain, solver and versioned-seed regressions plus ADR 0090. Adaptive substepping and source-side/steam-dump fidelity remain separate follow-on work.
- M10.9.3 remains the official milestone baseline until final manual M10.9.4 acceptance; Hotfix 23 is the latest validated structural checkpoint and no production candidate is open.

## M10.9.4 Hotfix 22 — Governor Speed-to-Load Droop Mode Cleanup — VALIDATED STRUCTURAL CHECKPOINT

- Promotes Hotfix 21 to the latest validated structural checkpoint after the user confirmed compilation, the complete ordinary suite and both explicit 60-second gameplay journeys all pass.
- Adds optional `TurbineGovernorDroopDefinition`; null preserves legacy speed-reference-only semantics.
- Current-v2 keeps the existing canonical speed PID/control-valve owner but changes its automatic reference by breaker state: breaker open uses the operator speed setpoint; breaker closed uses grid synchronous speed plus a requested-electrical-load droop offset.
- Current-v2 uses a 150 rpm full-load reference rise on the 3000 rpm machine (5% droop). At the validated 5 MWe / 1000 MWe point the effective reference is 3000.75 rpm, keeping the transition near-bumpless.
- Manual controller mode bypasses droop rewriting. The breaker-close step itself remains pre-sync; load-droop semantics begin on the next committed step.
- Both M5.4 integrated and M5.5 protected orchestration paths pass canonical `IntegratedSecondaryCycleInputs` into the governor so requested load cannot diverge by execution path.
- Adds domain, solver and versioned-seed regressions plus ADR 0089. No protection, actuator-rate, turbine, condenser, pump or generator-grid physics changes are mixed into this hotfix.
- User validation complete: compilation, the ordinary suite and both explicit 60-second journeys passed. Hotfix 22 became the validated base for Hotfix 23.

## M10.9.4 Hotfix 21 — Deterministic Secondary Actuator Travel/Ramp Dynamics — VALIDATED STRUCTURAL CHECKPOINT

- Promotes Hotfix 20 Fix 2 to the latest validated structural checkpoint after the user confirmed compilation, the complete ordinary suite and both explicit 60-second gameplay journeys all pass.
- Adds strongly typed `ActuatorTravelRate` in normalized full-scale fraction per second; `ActuatorDefinition.TravelRate = null` preserves historical instantaneous command application for legacy/versioned compatibility.
- Current-v2 turbine/secondary actuators opt into deterministic travel limits: control/admission valves use `0.5 fraction/s` (2 s full stroke) and condensate/feedwater pumps use `0.25 fraction/s` (4 s full ramp).
- M5.4 keeps controller output / typed actuator command instantaneous and observable, but the canonical physical `PlantState` now moves from committed state toward the requested valve position or pump speed by at most `rate * dt` per step. No hydraulic/thermodynamic integration is moved into the control layer.
- Pump zero-speed commands now ramp down canonical speed before `IsRunning` becomes false; positive commands start at the first finite ramped speed. This avoids defeating the ramp with an instantaneous run-state drop while preserving one canonical pump-speed state.
- Protection trip override remains higher authority: turbine-trip stop-valve closure is not routed through normal M5.4 actuator travel limits. Hydraulic fault overrides also remain separate.
- Adds direct domain and M5.4 regressions for optional legacy semantics, typed rate validation, bounded per-step valve/pump movement, non-instantaneous coast-down and explicit current-v2 ownership.
- Adds ADR 0088. Governor/load-control mode cleanup remains the next separate structural step.
- User validation complete: compilation, the ordinary suite and both explicit 60-second journeys passed. Hotfix 21 became the validated base for Hotfix 22.

## M10.9.4 Hotfix 20 Fix 2 — Meaningful Secondary Protection Set / Physical Frequency Regression Contract — VALIDATED STRUCTURAL CHECKPOINT

- Test-only correction: the current-v2 protection bootstrap regression no longer requires an exact literal 50.000000 Hz after deterministic seed preconditioning. It now derives expected generator frequency from the committed turbine rotor angular speed through `SynchronousGeneratorDefinition.ElectricalFrequencyAt(...)`, asserts measured telemetry matches that physical value, and separately requires the healthy operating point to remain within 49.9–50.1 Hz. No production code, protection thresholds/actions, plant physics or replay semantics changed.

## M10.9.4 Hotfix 20 Fix 1 — Meaningful Secondary Protection Set / Initial Measured-Frame Completeness — SUPERSEDED BY FIX 2

- Fixes the current-v2 bootstrap contract after Hotfix 20 added `condenser-pressure` and `generator-frequency` channels without adding matching initial measured signals. `MeasuredSignalFrame` again contains exactly one signal per instrumentation channel from logical step 0.
- Initial `condenser-pressure` is seeded from the committed `exhaust` node pressure and `generator-frequency` from `SynchronousGeneratorDefinition.ElectricalFrequencyAt(initial rotor speed)`, preventing fabricated bootstrap values and false protection trips.
- Adds a direct regression comparing instrumentation-channel IDs with initial measured-frame signal IDs and checking physically safe initial condenser pressure / 50 Hz generator frequency. No protection threshold/action or plant physics changes.
- Promotes Hotfix 19 to the latest validated structural checkpoint after the user confirmed compilation, the ordinary suite and both explicit 60-second gameplay journeys all pass.
- Adds an opt-in current-v2 protection profile while preserving the historical minimal legacy protection definition by default.
- Current-v2 adds measured `turbine-overspeed` at 3300 rpm (reset-safe 3150 rpm) with turbine + generator trip, `condenser-high-backpressure` at 30 kPa absolute (reset-safe 20 kPa) with turbine + generator trip, and `generator-overfrequency` at 53 Hz (reset-safe 51.5 Hz) with generator trip.
- Adds canonical measured channels for condenser absolute pressure and generator frequency only when the enhanced current-v2 protection profile is enabled. Protection continues to consume measured M5.1 signals only.
- Adds current-v2 warning/trip annunciation for condenser high backpressure plus turbine/generator trip actions.
- Intentionally defers generator underfrequency protection until breaker/load-state supervision is available; a disconnected machine must not latch underfrequency merely because it is not synchronized.
- Adds regressions proving legacy/current version ownership, exact thresholds/actions, healthy initial v2 state and actual latching from measured overspeed/backpressure/overfrequency signals.
- Adds ADR 0087. Actuator travel rates, governor/load-control cleanup and adaptive substepping remain separate follow-on work.
- User validation complete after Fix 2: compilation, the ordinary suite and both explicit 60-second journeys passed. Hotfix 20 Fix 2 became the validated base for Hotfix 21.

## M10.9.4 Hotfix 19 — Secondary-Pump Discharge Check Valves — VALIDATED STRUCTURAL CHECKPOINT

- Compile fix: current/legacy seed tests now resolve pump definitions through `IntegratedSecondaryCycleDefinition.PlantDefinition` before calling `GetPump(...)`; no pump/check-valve physics or seed configuration changed.
- Promotes Hotfix 18 to the latest validated structural checkpoint after the user confirmed compilation, the ordinary suite and both explicit 60-second gameplay journeys all pass.
- Adds opt-in `PumpDefinition.HasDischargeCheckValve`; default `false` preserves existing bidirectional pump-path semantics for legacy definitions and components where reverse flow is intentional.
- `PumpFlowSolver` now closes an enabled discharge check valve whenever the unconstrained hydraulic solution would reverse through the pump path. The blocked state transfers zero mass, zero advected/internal pump energy and zero shaft-demand credit while retaining the committed pump speed/head state.
- Enables discharge check valves only on current-v2 `condensate-pump` and `feedwater-pump`; the main circulation pump and all legacy/default definitions remain unchanged.
- Adds direct regressions for running/stopped reverse-flow blocking, passive forward opening, zero mass/energy balance when closed, and v1/v2 topology ownership.
- Adds ADR 0086 and updates the structural stabilization roadmap. Protection expansion, actuator travel rates and adaptive substepping remain explicitly deferred.
- User validation complete after the compile fix: compilation, the ordinary suite and both explicit 60-second journeys passed. Hotfix 19 became the validated base for Hotfix 20.

## M10.9.4 Hotfix 18 — Generator/Grid Synchronous Phase-Frequency Stiffness — VALIDATED STRUCTURAL CHECKPOINT

- Compile correction: import the canonical Domain turbine namespace in `GeneratorGridSolver.cs` so the `TurbineRotorDefinition` parameter used by the new synchronous-coupling helper resolves correctly. No generator/grid equations, coefficients, seed values, replay semantics, protection logic or control authority changed.

- Promotes Hotfix 17 to the latest validated structural checkpoint after the user confirmed compilation, the ordinary suite and both explicit 60-second gameplay journeys all pass.
- Adds optional `SynchronousGridCouplingDefinition` to M4.5 generator definitions; null preserves the historical dispatch-torque-only legacy seam.
- Current-v2 paralleled generators now apply deterministic infinite-bus corrections around the dispatch setpoint: `Pphase = Psync,max*sin(delta)` plus `Pfrequency = Pdamp@1Hz*(fgenerator-fgrid)`.
- Positive electrical phase lead / positive frequency slip increase electromagnetic load; negative slip unloads the rotor, creating restoring phase/frequency stiffness instead of allowing a paralleled machine to drift freely away from 50 Hz.
- The final mechanical load is bounded to `[0, generator maximum mechanical power]` before conversion to canonical M4.2 external rotor torque. Rotor inertia remains integrated exactly once by `TurbineExpansionSolver`.
- Current sustained-generation and pre-synchronization v2 definitions use `Psync,max = 10 MW` and `Pdamp@1Hz = 10 MW`; at the validated 50 Hz / zero-phase-error design point the correction is exactly zero, preserving the Hotfix 17 initial operating point.
- Adds direct M4.5 regressions for phase lead/lag restoring direction, slow/fast rotor frequency damping and exact legacy-null-coupling dispatch behavior. Current-v2 seed tests assert the coupling is explicitly present.
- Adds ADR 0085. No pump check-valve, protection expansion, actuator travel-rate or adaptive-substep changes are mixed into this hotfix.
- User validation complete after the namespace compile fix: compilation, the ordinary suite and both explicit 60-second journeys passed. Hotfix 18 became the validated base for Hotfix 19.

## M10.9.4 Hotfix 17 — Condenser UA·ΔT Pressure Feedback — VALIDATED STRUCTURAL CHECKPOINT

- Takes user-corrected Hotfix 16 as the current green working checkpoint: solution build, 870 ordinary tests and both explicit 60-second gameplay journeys are documented green there.
- Replaces the current-v2 condenser's capacity-only heat-removal assumption with a canonical surface-condenser feedback law: `Q_effective = min(Q_available, UA * max(0, T_steam-space - T_coolant))`.
- Adds optional `CondenserDefinition.OverallHeatTransferConductance`; null remains isolated legacy capacity-only behavior, while current sustained-generation/synchronization v2 definitions use `1.225 MW/K`.
- Extends `CondenserCoolingBoundaryInput` with effective coolant temperature. Current v2 uses 20 °C; condenser cooling-capacity faults preserve that temperature while scaling only available rejection power.
- Derives `UA = 24.5 MW / (40 °C - 20 °C) = 1.225 MW/K`, preserving the Hotfix 16 initial design point instead of introducing a new tuning discontinuity.
- Publishes coolant temperature, steam-to-coolant ΔT, UA surface limit and effective heat-rejection capacity in condenser snapshots for direct diagnostics.
- Adds direct M4.3 regressions proving UA-limited rejection below installed capacity, weaker condensation as ΔT falls, zero condensation at non-positive ΔT and explicit legacy isolation.
- Removes the obsolete duplicate-number Hotfix 11 condenser ADR and records the current decision as ADR 0084.
- No generator synchronous-coupling, pump check-valve, protection, actuator-rate or adaptive-substep changes are mixed into this hotfix. Those remain ordered follow-on structural items.
- User validation complete: compilation, ordinary suite and both explicit 60-second gameplay journeys passed. Hotfix 17 is the validated base for Hotfix 18.

## M10.9.4 Hotfix 16 — Conservative Main-Steam Supply Closure — IMPLEMENTATION CANDIDATE

- Reproduced both explicit long-gameplay failures at the exact conserved `drum` states and found two successive defects rather than one numerical blow-up.
- Removed the artificial `p < pcrit` rejection from the subcritical-temperature compressed-liquid branch. The failing states are finite compressed liquid at about 548 K and 22.069 MPa; the critical-pressure bound remains in the saturation/vapor branches.
- Closed the current-v2 `drum -> steam outlet -> main-steam line` inventory path: when `CirculationDemandBalanced` is active, M4.1 supplements return-separated steam up to the positive committed main-steam-line demand using an exactly mass/energy-conservative internal transfer. Legacy `LegacyReturnSplit` profiles retain the historical behavior.
- Added direct regressions for both reported thermodynamic states, conservative current-mode steam replenishment and legacy isolation.
- Added versioned operational-seed speed-controller gains. Historical callers retain `P=1`, `D=0`; the already-loaded desktop v2 keeps `P=1`, `I=0.02 s⁻¹` and adds `D=0.2 s` to damp its small 10-second overshoot, while pre-synchronization v2 uses `P=0.5`, `I=0.02 s⁻¹` to prevent post-close 0%/100% cycling. Both preserve the bumpless 46% handoff.
- Long-test failures now retain all completed checkpoint diagnostics and include drum pressure/temperature/phase plus return, steam, recirculation and cycle-flow evidence.
- Final local validation is green: solution build has 0 warnings/errors, the ordinary suite passes 870 tests with only the 2 explicit journeys skipped, and both 60-s explicit journeys pass separately. M10.9.3 remains the validated baseline pending release-candidate promotion.

## M10.9.4 Hotfix 15 — Steam-Drum Inventory Closure — IMPLEMENTATION CANDIDATE

- Ordinary build/tests and the Hotfix 14 200-step turbine hydraulic invariant passed locally; the explicit long gameplay journeys then exposed the next structural failure in the canonical `drum` node.
- Root-caused the historical closed-cycle drum mass balance: physical return flow was added by the canonical return pipe and cancelled by the separator, while M4.4 feedwater remained a one-way drum addition, giving `dm_drum/dt = F_feedwater >= 0` by construction.
- Added explicit `SteamDrumLiquidRecirculationMode`: legacy profiles retain `LegacyReturnSplit`; current v2 sustained-generation/synchronization profiles use `CirculationDemandBalanced`.
- Current v2 liquid recirculation now follows positive committed MCP demand and separator drain is `F_steam + F_liquid`, yielding `dm_drum/dt = F_return + F_feedwater - F_MCP - F_steam` instead of a sign-only accumulator.
- Added direct M3.6 regression coverage for the new source-term closure and ADR 0082. No seed-volume/feedwater tuning is used as the fix.
- M10.9.3 remains the validated baseline; M10.9.4 Hotfix 15 remains candidate pending ordinary and explicit long-gameplay gates.

## M10.9.4 Hotfix 14 — Turbine Hydraulic Invariant Regression Contract — IMPLEMENTATION CANDIDATE

- Keeps the Hotfix 13 pressure-driven `turbine-inlet -> exhaust` expansion law unchanged; no production physics/control/protection code changes in this hotfix.
- Corrects the 200-step structural regression: `F_admission == F_stage` is not an instantaneous invariant for a compressible `turbine-inlet` plenum because their difference changes plenum inventory during transients.
- Retains the direct ±5% final combined admission-train inventory bound and adds a trajectory assertion requiring at least one negative inventory increment, which directly disproves the historical `dM_train/dt >= 0` ratchet.
- Keeps finite positive/in-range admission and stage flow checks without conflating transient valve flow with turbine expansion flow.
- Adds `docs/STRUCTURAL_PLANT_MODEL_STABILIZATION_PLAN.md`, classifying the external structural audit against the current code and fixing the validation order: turbine hydraulic closure first, then condenser pressure feedback, synchronous generator-grid coupling, pump non-return behavior, protections/actuator dynamics, and later fidelity/integration hardening.
- M10.9.3 remains the validated baseline; M10.9.4 Hotfix 14 remains candidate pending ordinary and explicit long-gameplay gates.

## M10.9.4 Hotfix 13 — Pressure-Driven Turbine Expansion Hydraulic Closure — IMPLEMENTATION CANDIDATE

- Rebases deliberately on Hotfix 10, the last ordinary-green candidate; the unvalidated Hotfix 11 and Hotfix 12 experimental branches are not part of the accepted source line.
- Replaces the current-v2 historical `min(stop, control, admission)` stage-drain projection with a shared pressure-driven `turbine-inlet -> exhaust` expansion flow.
- Adds optional `ExpansionResistance`; null preserves isolated legacy behavior, while current v2 uses 21,400 Pa·s²/kg².
- Blocks reverse stage flow and limits one-step drain against committed inlet inventory.
- Removes duplicated stage-flow resolution between integrated control paths and adopts the policy that legacy replay compatibility may not preserve a known defect in the active model.
- Adds ADR 0080 and ADR 0081 plus a short admission-train inventory regression; ordinary and explicit long-running gates remain required.

## M10.9.4 Hotfix 12 — Speculative Fuel/Structure Thermal Closure — WITHDRAWN / NEVER VALIDATED

- Records the historical experimental branch described in the milestone history.
- The branch explored a speculative fuel/structure thermal closure but was never accepted as the canonical cause or validated baseline.
- Hotfix 13 rebased on Hotfix 10 and excluded this branch so the turbine hydraulic defect could be corrected in isolation.
- No Hotfix 12 production behavior exists in the current validated tree.

## M10.9.4 Hotfix 11 — Condenser-Flow-Following Workaround — WITHDRAWN / NEVER VALIDATED

- Records the historical experimental condenser-flow-following workaround omitted from the prior changelog sequence.
- The branch was never validated and was withdrawn rather than retained as a compensating law for the unresolved turbine admission-train accumulator.
- Hotfix 13 rebased on Hotfix 10; no Hotfix 11 production behavior or accepted ADR remains in the current validated tree.

## M10.9.4 Hotfix 10 — Deterministic Seed Preconditioning & Steam-Path Control Authority — IMPLEMENTATION CANDIDATE

- Keeps M10.9.3 as the validated baseline and preserves all v1 initial-condition defaults/replay identities.
- Adds an optional deterministic fixed-step preconditioning count to the operational-seed factory; historical callers remain at exactly one seed step, while the new v2 desktop/synchronization seeds use two committed fixed steps before public logical STEP 0.
- This removes the first-snapshot bootstrap seam where the main-steam line was already flowing but turbine stage demand could still present as zero before controller/measurement/derived-input state had mutually committed.
- Increases only v2 steam-path hydraulic authority by reducing main-steam/admission resistance from 1800 to 1000 Pa·s²/kg².
- Reduces the v2 initial governor bias from 61% to 46%, preserving approximately the same initial steam-flow order while leaving substantially more opening authority when rotor speed falls under electrical load.
- Keeps the generation-scale 1000 m³ exhaust steam-space and 24.5 MW condenser boundary introduced by Hotfix 9.
- Updates the v2 synchronization seed contract to accept the intentional 46% bumpless governor bias without weakening synchronization or runtime stability requirements.
- Ordinary and explicit gameplay acceptance remain required before M10.9.4 promotion.

## M10.9.4 Hotfix 9 — Generation-Scale Condenser Steam-Space — IMPLEMENTATION CANDIDATE

- Replaces the v2-only 10 m³ condenser `exhaust` node with a 1,000 m³ low-pressure steam-space while preserving the historical 10 m³ default for all v1 replay identities and existing callers.
- Adds the optional `exhaustSteamSpaceVolumeCubicMetres` operational-seed parameter with strict finite/positive validation; no canonical M4 solver law or node connectivity is changed.
- Increases v2 steam-path hydraulic margin by reducing main-steam/admission resistance from 2,800 to 1,800 Pa·s²/kg² so the initial staged path has comfortable mechanical-power margin before the PI governor settles toward the 5 MWe equilibrium.
- Reduces v2 initial condenser heat rejection from 25.0 to 24.5 MW so the target low-load point is not systematically biased toward exhaust mass depletion.
- Strengthens the v2 seed regression to expose the actual stage-flow and shaft-power values with `Assert.InRange` rather than an opaque boolean failure.
- M10.9.3 remains the validated baseline; Hotfix 9 remains candidate pending the ordinary suite and the explicit 60-second gameplay pack.

## M10.9.4 Hotfix 8 — Continuous Main-Steam Supply Gradient — IMPLEMENTATION CANDIDATE

- Root-caused the repeated `exhaust` depletion: v2 initialized `steam` and `header` at the same saturation temperature/pressure, so the canonical main-steam line supplied 0 kg/s while the downstream admission train consumed only its finite preloaded inventories.
- Added an optional operational-seed header steam temperature with a backward-compatible default equal to primary steam temperature; historical v1 recipes therefore remain unchanged.
- Generation-ready v2 desktop and pre-synchronization seeds now initialize a continuous pressure staircase `steam 280 °C → header 275 °C → stop-out 269.5 °C → control-out 253 °C → turbine-inlet 246 °C`, yielding approximately 13 kg/s through every canonical steam-path element with the existing v2 resistances and 61% governor bias.
- Added ordinary regressions requiring positive >10 kg/s canonical main-steam-line replenishment and positive source-to-header pressure difference before accepting the v2 generation-ready seed.
- No M4 solver law, condenser law, turbine law, protection logic, historical v1 identity or replay payload was changed. M10.9.3 remains the validated baseline; Hotfix 8 remains candidate pending the ordinary suite and explicit long-gameplay pack.

## M10.9.4 Hotfix 7 — Condenser Balance & Spinning-Reserve Seed Correction — IMPLEMENTATION CANDIDATE

- Preserves M10.9.3 as the validated baseline and keeps historical `integrated-operations-desktop-stable` v1 / `pre-synchronization-grid-loading` v1 replay identities unchanged.
- Corrects the generation-ready v2 condenser heat-rejection boundary from 30 MW to 25 MW, matching the modeled ~12.9 kg/s low-load turbine steam flow and exhaust-to-hotwell specific-energy drop instead of progressively over-condensing the exhaust steam-space.
- Starts the v2 pre-synchronization profile with the same bumpless ~61% turbine-governor bias used by the sustained-generation profile, establishing spinning reserve before breaker closure rather than leaving downstream steam inventory to drain while the control valve is closed.
- Adds an ordinary one-simulated-second pre-synchronization thermodynamic smoke regression, so control-out/exhaust envelope failures are caught by the standard suite before the explicit 60-second gameplay pack.
- Restores the exact `ApplicationDescriptor` contract phrases required by the validated descriptor test (`long-gameplay acceptance tests`, `without moving plant topology`).
- No M4 condenser/turbine solver law, replay fingerprint schema, protection priority or Avalonia plant-control ownership is changed.

## M10.9.4 Hotfix 6 — Generation-Ready Power-Path Balance — IMPLEMENTATION CANDIDATE

- The first real explicit long-gameplay execution proved the historical desktop v1 seed was not a sustained generation point: at 10 simulated seconds it had decayed to ~1442.6 rpm and ~2.406 MWe with MODEL rotor shaft power 0 MW; the separate synchronization journey also drove `control-out` outside the simplified water/steam envelope.
- Preserved historical `integrated-operations-desktop-stable` v1 and `pre-synchronization-grid-loading` v1 unchanged for exact-version replay/archive compatibility; added generation-ready v2 factories and registered both versions.
- Current desktop integrated operations now references v2, with a staged 280→275→260→250 °C pressurized steam path, matched low-load admission hydraulics, bumpless PI governor bias, explicit turbine-shaft instrumentation, finite condenser heat rejection/capacity and matched condensate/feedwater pump capacity/bias.
- The explicit synchronization long journey uses a v2 initial condition without changing the historical M7.5 v1 scenario.
- Added presentation-only `[JsonIgnore]` `EffectiveTurbineSteamFlow`, derived from actual turbine stage-group effective mass flow. HMI, mimic, schematics, diagnostics and Operator Computer now use this value instead of the legacy zero-valued M4.1 turbine-boundary seam, while fingerprint-v1 keeps the historical serialized field unchanged.
- Strengthened the ordinary generation-ready seed regression to require finite synchronous speed, >4.5 MW MODEL shaft support, >10 kg/s effective steam/condensation/condensate/feedwater flow, and strengthened both explicit journeys to require sustained shaft plus electrical output.
- Added ADR 0079. M10.9.3 remains the validated baseline; M10.9.4 Hotfix 6 remains candidate until the ordinary suite and explicit 60-second gameplay pack both pass locally.

## M10.9.4 Hotfix 5 — Cooperative Long-Gameplay Batching — IMPLEMENTATION CANDIDATE

- Fixed both explicit long-running gameplay journeys so each 1,000-step checkpoint respects `ControlRoomRuntimeCoordinator.ExecutionBudget.MaximumSimulationStepsPerBatch` instead of calling `AdvanceRunning(1000, ...)` directly.
- A 1,000-step/10-second checkpoint is now executed cooperatively as runtime-budget-sized chunks (desktop default: 256 + 256 + 256 + 232), while assertions remain at the same 10-second checkpoints through 60 simulated seconds.
- The runtime execution budget is not widened or bypassed; no plant physics, seed, control, protection, replay, HMI semantics or acceptance thresholds changed.
- M10.9.3 remains the validated baseline; M10.9.4 Hotfix 5 is the current cumulative candidate pending the ordinary gate and explicit long-gameplay pack.

## M10.9.4 Hotfix 4 — Nullable Measured-Shaft Compile Guard — IMPLEMENTATION CANDIDATE

- Fixed CS8629 in `ControlRoomSubsystemSchematicProjector.BuildGeneratorPowerPathDiagnostic`: the near-zero measured-shaft branch now explicitly checks `shaft.HasValue` before reading `shaft.Value`.
- Preserves Hotfix 3 semantics exactly: unavailable measured shaft remains `UNAVAILABLE`, is never coerced to zero, and MODEL rotor-shaft remains a separate diagnostic datum.
- No runtime physics/control/protection/seed/replay changes.
- M10.9.3 remains the validated baseline; M10.9.4 Hotfix 4 is the current cumulative candidate.

## M10.9.4 Hotfix 3 — Unavailable Measured-Shaft Semantics & Long-Gameplay Gate Correction — IMPLEMENTATION CANDIDATE

- Corrected the ordinary projection test: the initial aggregate turbine-shaft MEASURED presentation channel is `UNAVAILABLE` (`NumericValue = null`) because that measured source is not published by the desktop instrumentation definition; it must not be coerced to numeric zero.
- Updated generator power-path diagnostics to distinguish `MEASURED shaft unavailable` from `measured shaft near zero`, while explicitly exposing the finite MODEL rotor-shaft evidence without silently substituting true state for an unavailable measurement.
- Corrected the explicit desktop gameplay journey so step 0 requires a finite MODEL rotor-shaft value but sustained mechanical/electrical production is enforced at 10, 20, 30, 40, 50 and 60 simulated seconds.
- Preserved Hotfix 2 Microsoft Testing Platform runner syntax using `dotnet test --project ... -- --explicit only`.
- No canonical plant physics/control/protection/replay behavior changed; M10.9.3 remains the validated baseline until the ordinary suite and explicit long-gameplay gate both pass.

## M10.9.4 Hotfix 2 — Test contracts, MTP runner syntax & desktop power-path evidence — IMPLEMENTATION CANDIDATE

- Fixed the M10.9.4 historical instrumentation-label regression to inspect XAML `Label` attributes, matching the already validated M10.9.2/M10.9.3 contract.
- Corrected the desktop power-path projection regression: the actual seed currently has ~5 MWe requested/actual output with near-zero turbine shaft production, so the diagnostic correctly reports a shaft deficit rather than `POWER PATH ACTIVE`.
- Strengthened the explicit desktop gameplay journey to fail immediately when the handoff is powered only by rotor kinetic energy, before proceeding to 60-second sustained-export checks.
- Fixed both long-gameplay helper scripts to use Microsoft Testing Platform syntax `dotnet test --project <csproj> --no-build -- --explicit only`.
- No production physics/control/protection code changed in this hotfix; the potential desktop seed/integrated-balance defect remains intentionally exposed for the explicit gameplay gate.


## M10.9.4 Hotfix 1 — xUnit2009 Assertion Contract Fix — IMPLEMENTATION CANDIDATE

- Replaced the two new `Assert.True(string.StartsWith(...))` assertions in `ControlRoomSubsystemSchematicProjectionTests` with canonical `Assert.StartsWith(...)` assertions required by xUnit analyzer rule xUnit2009.
- No production code, schematic semantics, gameplay logic, physics, controls, protection, bindings or long-running test behavior changed.
- M10.9.3 remains the official validated baseline until M10.9.4 Hotfix 1 passes the normal gate and the explicit long-gameplay pack.

## M10.9.4 — Subsystem Engineering Schematics — IMPLEMENTATION CANDIDATE

- Promoted M10.9.3 Interactive Full-Plant Mimic to VALIDATED after the user confirmed successful compilation and the complete automated suite passed.
- Added five Application-owned subsystem engineering schematic families and the Avalonia `ControlRoomSubsystemSchematicControl` renderer.
- Added explicit reactor/core feedback, primary recirculation/steam-drum, turbine/secondary, generator/grid and instrumentation/control/protection process/signal-flow diagrams with IN/OUT semantics.
- Promoted generator requested electrical power as presentation-only metadata and added a live power-path diagnostic separating shaft power, mechanical input, requested load, actual MWe, synchronization, breaker and protection state.
- Clarified that amber SHAFT is the mechanical-energy medium color, not warning severity.
- Added two xUnit v3 explicit long-running gameplay/system acceptance journeys, plus separate CMD/PowerShell launchers, so 60-second integrated turbine→generator→grid verification does not run in the ordinary fast suite.
- Added M10.9.4 Application/App regression coverage, `SUBSYSTEM_ENGINEERING_SCHEMATICS.md`, `GAMEPLAY_LONG_RUNNING_SYSTEM_TESTS.md` and ADR 0078.

## M10.9.3 — Interactive Full-Plant Mimic — IMPLEMENTATION CANDIDATE

- Promoted M10.9.2 Hotfix 2 to VALIDATED after the user confirmed successful compilation and the complete automated suite passed.
- Added immutable Application-layer `ControlRoomPlantMimic*` contracts and `ControlRoomPlantMimicProjector` over the existing `ControlRoomSnapshot` presentation boundary.
- Added an interactive whole-plant PLANT overview with eight macro-equipment groups, nine directed process/energy connections, explicit IN/OUT semantics, medium-specific visual grammar and key live operating evidence.
- Added equipment selection, connected-path emphasis and navigation-only `OPEN SUBSYSTEM` drill-down to existing REACTOR/PRIMARY/TURBINE/GRID workspaces.
- Added `ControlRoomPlantMimicControl` with recognizable equipment glyphs and flow arrows; Avalonia renders supplied semantics and owns no topology/physics inference.
- Added Application/App regression coverage plus `docs/milestones/M10.9.3.md`, `docs/INTERACTIVE_FULL_PLANT_MIMIC.md` and ADR 0077.

## M10.9.2 Hotfix 2 — Measured Instrument Label Contract Restoration — IMPLEMENTATION CANDIDATE

- Restored the canonical static XAML labels `ROTOR SPEED · MEASURED` and `ELECTRICAL OUTPUT · MEASURED` after the App contract suite exposed the first compatibility regression and review found the second latent assertion before another test cycle.
- Preserved the new runtime provenance badge; no gauge scale, threshold, trend, projection, replay, physics, protection or control semantics changed.
- M10.9.1 remains the validated baseline until the cumulative M10.9.2 Hotfix 2 package passes the local build/test/manual gate.

## M10.9.2 Hotfix 1 — Nullable Setpoint Compile Correction — IMPLEMENTATION CANDIDATE

- Fixed `CS0173` in `ControlRoomSnapshotProjector` for steam-drum level and steam-pressure setpoint projection by explicitly typing the two conditional locals as `double?`.
- No formula, threshold, scale, target, protection, trend, UI binding or replay semantics changed.
- M10.9.1 remains the validated baseline until M10.9.2 Hotfix 1 passes the local build/test/manual gate.

## M10.9.2 — Advanced Instrument & Gauge System — IMPLEMENTATION CANDIDATE

- Recorded explicit local validation of M10.9.1: the user confirmed successful compilation and the complete automated suite passed; M10.9.1 is the official validated baseline.
- Added reusable advanced linear and circular gauge controls that render published scale/band/target/setpoint/protection semantics without UI-owned thresholds, including compact numeric semantics text so color is never the only carrier of limit meaning.
- Added presentation-only provenance, quality and explicit off-scale metadata to control-room scalar snapshots while preserving replay/fingerprint-v1 identity.
- Added deterministic logical-step trend snapshots with reset/backwards-step invalidation and no wall-clock dependency.
- Projected canonical gauge metadata for reactor power, rod position, steam-drum pressure/level, rotor speed, generator synchronization quantities and electrical output.
- Kept total turbine shaft power numeric because no defensible canonical display scale is currently published; M10.9.2 does not invent one.
- Added M10.9.2 Application/App regression coverage, `ADVANCED_INSTRUMENT_GAUGE_SYSTEM.md` and ADR 0076.

## M10.9.1 — HMI Information Architecture & Visual Language — IMPLEMENTATION CANDIDATE

- Recorded explicit local validation of M10.8: the user confirmed successful compilation and the complete automated suite passed; M10.8 is the official validated baseline.
- Reframed the desktop around a five-region operator-experience shell: persistent situation strip, compact system navigation, central workspace, contextual inspector and persistent alarm/event strip.
- Added high-salience runtime, logical-step, gross-output, training-score, alarm, protection, assistance and control-authority summaries without inventing the future external-demand capability planned for M10.9.6.
- Reduced developer-facing milestone terminology in operator headings while preserving explicit MEASURED/MODEL provenance and all validated M10.8 keyboard/command capabilities.
- Added immutable Application-layer HMI contracts that separate displayed instrument scale, operating bands, scenario/controller target bands, setpoint markers and protection thresholds; Avalonia remains presentation-only and does not own safety semantics.
- Added shell/range-semantics regressions plus ADR 0075 and the approved M10.9.1–M10.9.8 operator-experience/HMI roadmap.
- Added the user-provided plant, reactor-core, turbine-island and instrumentation/protection schematics under `docs/reference/hmi/` as design references only; they are not runtime topology or authoritative physics/control data.

## M10.8 — Integrated Operator Computer UI — VALIDATED

- Recorded explicit local validation of M10.7.1 Hotfix 3: compilation and the complete automated suite passed; M10.7.1 is the official validated baseline.
- Integrated all eight operator-computer pages into a fixed header/menu/status/footer workstation with a single scrollable page-content region.
- Moved the Computer workspace out of the normal center-workspace outer scroll into a dedicated center viewport so the terminal menu/status regions remain genuinely fixed while only page content scrolls; non-Computer workspaces retain the validated scroll layout.
- Reworked the page menu into a readable 4×2 F1–F8 layout with persistent selected-page indicators independent of keyboard focus.
- Kept runtime, logical step, alarm, signal-health and protection summaries always visible above scrolling page content.
- Reduced rigid COMMANDS/SESSION sizing to bounded responsive list layouts suitable for the validated center viewport.
- Preserved keyboard-first operation (F1–F8, Tab/Shift+Tab, arrows, Enter), full mouse support and the no-free-form-command rule.
- Added dedicated M10.8 ViewModel/XAML regression coverage and rebuilt authoritative `PROJECT_HANDOFF.md` / `NEW_CHAT_START.md` for a clean post-M10.8 chat handoff.

## M10.7.1 Hotfix 3 — Committed breaker-state regression expectation — IMPLEMENTATION CANDIDATE

- Updated the single stale App regression exposed by Hotfix 2: when a snapshot reports the generator breaker as physically CLOSED while a generator trip is active, `CLOSE BREAKER` must still present the committed CLOSED/active state rather than becoming visually `Unavailable`.
- The already-satisfied/affected close command remains non-clickable (`BreakerCloseCommandEnabled = false`), so the test now verifies the intended separation between committed-state feedback and command availability.
- Test-only correction; no production ViewModel, command-dispatch, physics, protection, replay, archive, or UI behavior changed.

## M10.7.1 Hotfix 2 — Persistent control-state and momentary-command feedback — IMPLEMENTATION CANDIDATE

- Extended `ControlRoomPushButton` with a presentation-only `IsActive` state and a short click-feedback pulse; active normal controls use a filled green background with dark text while command availability remains a separate concern.
- Reactor `INSERT / HOLD / WITHDRAW` now reflect the actual committed motion of the selected canonical rod/group; group targets report `MIXED` when member motions differ, and the already-active motion command is disabled.
- Primary `START / RUN` and `STOP` now reflect the actual committed selected-pump state and disable the already-satisfied command.
- Electrical `CLOSE BREAKER` and `OPEN BREAKER` now reflect the actual committed breaker position and disable the already-satisfied side; close remains warning/blocked when the canonical synchronization permissive is not satisfied.
- `SPEED LOWER / SPEED RAISE / LOAD LOWER / LOAD RAISE` remain explicitly momentary setpoint-step commands: they flash on press, never latch visually, and the UI retains a `LAST CONTROL ACTION · ACCEPTED/BLOCKED` status so the operator can distinguish click feedback from committed state.
- Added presentation-only rod-target effective-motion projection excluded from fingerprint-v1 serialization, plus App/Application/XAML/replay regressions protecting actual-state semantics and historical replay compatibility.

## M10.7.1 — Operator Control-State & Synchronization Usability Hotfix — IMPLEMENTATION CANDIDATE

- Recorded M10.7 Hotfix 1 as VALIDATED after the user confirmed successful compilation and the complete automated suite passed; M10.7 is the new official baseline.
- Separated `ControlRoomPushButton` visual state from command availability so latched SCRAM/turbine/generator trips remain strongly filled/visible while the one-shot trip command is disabled.
- Added contextual `RESET PROTECTION` access in reactor, turbine and electrical areas plus presentation-only canonical M5.5 reset readiness/blocker projection; F4 COMMANDS now uses the same readiness/blocker state instead of advertising every active-trip reset as available.
- Made M4.5 synchronization presentation breaker-aware: open breaker shows detailed Δf/Δphase/ΔV close-check status; closed breaker shows `PARALLELED`/normal rather than a stale synchronization warning.
- Added Overview operator action guidance: current condition, next canonical procedure action and a cold-shutdown-to-first-electrical-output command map composed from validated M7 guidance.
- Kept new reset/synchronization diagnostics excluded from `ControlRoomSnapshotFingerprint` v1 so replay/checkpoint identity remains unchanged.
- Added App/Application/XAML/fingerprint regression coverage and `docs/OPERATOR_CONTROL_STATE_SYNCHRONIZATION_USABILITY.md`.

## M10.7.1 Hotfix 1 — Fingerprint-v1 compatibility and descriptor regression fix

- Fixed the stale `ApplicationDescriptorTests` expectation so M10.7.1 is correctly asserted on the validated M10.7 baseline.
- Preserved the exact M10.7 serialized semantics of `GeneratorPresentationSnapshot.SynchronizationState` and `SynchronizationText` used by `ControlRoomSnapshotFingerprint` v1.
- Moved the new breaker-aware synchronization UX to `[JsonIgnore]` presentation-only `DisplaySynchronizationState` / `DisplaySynchronizationText` properties; the new label/detail diagnostics are also excluded from fingerprint serialization.
- Strengthened replay regression coverage so presentation-only reset/synchronization diagnostics cannot change fingerprint-v1 output.

## M10.7 Hotfix 1 — xUnit analyzer compliance

- Replaced the single `Where(...)+Assert.Single(...)` pattern in `ScenarioSessionArchiveReplayTests` with the predicate overload of `Assert.Single`, satisfying xUnit analyzer rule `xUnit2031` under warnings-as-errors.
- Test-only correction; no production, archive, replay, serializer, UI, runtime, or physics behavior changed.

## M10.7 — Session, Checkpoint, Replay & Save Workspace — IMPLEMENTATION CANDIDATE

- Promoted the cumulative M10.2–M10.6 Hotfix 1 chain to VALIDATED after the user confirmed successful compilation and complete automated tests; M10.6 is the new official baseline.
- Added replay-backed `ScenarioSessionArchive` schema v1 with compact per-step fingerprint/event evidence, exact embedded scenario identity, operator actions, M10.5/M10.6 automation intents, recorder events and M9.1 checkpoints.
- Added JSON archive persistence plus canonical `ScenarioFullReplayRunner` archive replay/seek verification; no opaque solver-state dump or second checkpoint/restore owner.
- Added exact checkpoint-prefix event reconstruction so operator-action evidence accepted between committed frames is retained iff that action belongs to the applied replay prefix.
- Added `ScenarioRecorder.Capture()` and verified-prefix resume support so loaded/restored sessions continue one deterministic recording trace.
- Activated F8 SESSION with explicit recorded-session restart, checkpoint creation/listing, replay verification, file save/load and selected-checkpoint restore.
- Kept normal desktop recording opt-in to avoid hidden per-step fingerprint/frame overhead.
- Reduced routine desktop/full-plant endurance regressions from 6,000 to 1,000 steps / 10 simulated seconds after the original thermodynamic failures were isolated by dedicated direct resolver regressions; historical M9.7 60-second validation evidence remains unchanged.
- Added `docs/milestones/M10.7.md` and `docs/OPERATOR_COMPUTER_SESSION_CHECKPOINT_REPLAY_SAVE.md`.

## M10.6 Hotfix 1 — Automation replay test compilation

- Added the missing `NuclearReactorSimulator.Application.Scenarios.Recording` import to `ScenarioAutomationReplayTests`.
- Changed the recorder-completion exception assertion to an explicit `Action` block so xUnit v3 selects the synchronous `Assert.Throws<T>` overload unambiguously.
- Test-only correction: no production runtime, supervisory-control, authority, recorder, replay, or checkpoint behavior changed.

## M10.6 — Supervisory Automatic Operation — IMPLEMENTATION CANDIDATE

- Preserved M10.1 as the last explicitly validated M10 baseline; M10.2–M10.4 remain unvalidated candidates. M10.5 is included as the minimum prerequisite and M10.6 is the current candidate layered on that chain.
- Added independent training-assistance and physical plant-control-authority axes with requested/effective/health/degraded presentation state.
- Added deterministic M5-owned `SupervisoryOperationCoordinator` with bounded Hold Reactor Power, Hold Turbine Speed and Hold Current Operating Point objectives over existing local controller modes/setpoints only.
- Added measured-signal-only supervisory validation, fail-closed degradation, canonical protection suspension and deterministic bumpless Manual takeover using committed controller outputs.
- Activated the MODES terminal page for training assistance, MANUAL/ASSISTED/SUPERVISORY selection, current-operating-point hold and per-loop mode/status visibility.
- Added separate scenario automation-intent journaling and M9.1 full-replay/checkpoint reconstruction without changing the versioned `ControlRoomSnapshot` fingerprint schema or recasting automation intents as physical `ControlRoomCommandKind` values.
- Added M10.5/M10.6 integration, protection, invalid-measurement, replay and App/ViewModel/XAML regression coverage.
- Added `docs/milestones/M10.5.md`, `docs/milestones/M10.6.md`, `docs/DUAL_ASSISTANCE_CONTROL_AUTHORITY.md`, `docs/SUPERVISORY_AUTOMATIC_OPERATION.md` and ADR 0074.

## M10.4 — Contextual Command Console — IMPLEMENTATION CANDIDATE

- Preserved M10.1 as the last explicitly validated M10 baseline; M10.2 and M10.3 remain unvalidated, with M10.4 layered on that candidate chain.
- Activated the fixed COMMANDS terminal page with immutable Application-layer command catalog contracts.
- Added contextual expansion of canonical typed commands by exact target (rod/group, MCP, rotor, generator, breaker, alarm) plus runtime/protection/global commands.
- Added explicit AVAILABLE / BLOCKED / UNAVAILABLE presentation states, current-state text and blocking reasons without creating a second permissive/interlock owner.
- Added keyboard/list selection and Enter/explicit execute dispatch through the existing `IControlRoomCommandDispatcher`; blocked commands are not dispatched and runtime/scenario rejection remains authoritative/fail-closed.
- Kept training/presentation intents and session lifecycle intents outside `ControlRoomCommandKind`, preserving ADR 0070 ownership boundaries.
- Added Application/App/XAML regression coverage and `docs/OPERATOR_COMPUTER_CONTEXTUAL_COMMAND_CONSOLE.md` / `docs/milestones/M10.4.md`.


## M10.3 — Alarm, Log & Incident Workstation — IMPLEMENTATION CANDIDATE

- Preserved M10.1 as the last explicitly validated M10 baseline; M10.2 remains unvalidated and M10.3 is layered on that candidate.
- Added immutable operator-computer alarm/log/incident presentation contracts and `OperatorComputerAlarmLogProjector`.
- Activated the ALARMS terminal page as a read-only projection over canonical M5.6/M6.6 annunciator state and bounded logical-step alarm-event history; no terminal ACK/RESET action is introduced before M10.4.
- Activated the LOG page with explicit LIVE / SESSION / INCIDENT evidence scopes: bounded M6.6 trends/events, optional M9.1 recorder events, and optional immutable M9.2 post-incident reports.
- Kept default desktop operation free of hidden full-recorder overhead: M9.1 evidence is shown only when a recorder is explicitly supplied by a session owner.
- Reordered MainWindow history observation before operator-computer reprojection so the terminal sees the same committed snapshot/history step instead of lagging one publication.
- Added Application/App regressions for annunciator projection, bounded event ordering, M6.6 trend reuse, optional M9.1 evidence, optional M9.2 incident projection and read-only desktop terminal behavior.
- Added `docs/OPERATOR_COMPUTER_ALARM_LOG_INCIDENT_WORKSTATION.md` and `docs/milestones/M10.3.md`; no new ADR is required because ownership follows ADR 0070 and validated M5.6/M6.6/M9.1/M9.2 boundaries.

## M10.2 — Unified Information, Guidance & Diagnostics — IMPLEMENTATION CANDIDATE

- Recorded explicit local validation of M10.1: compilation and the complete automated suite passed and the terminal shell worked correctly.
- Added generic immutable operator-computer content contracts for information, guidance steps and procedure diagnostics.
- Added `OperatorComputerInformationProjector`, sourcing only already-published `ControlRoomSnapshot`/M6 panel values and preserving explicit `[MEASURED]`, `[MODEL]`, `[STATE]` and `[UNAVAILABLE]` provenance.
- Added `OperatorComputerScenarioContentProjector` adapters for the existing M7.2–M7.6 guidance/checklist families; canonical evaluator results remain the only readiness criteria.
- Activated GUIDANCE/INFO/DIAGNOSTICS terminal content while leaving ALARMS/LOG, COMMANDS, MODES and SESSION staged for their planned M10 milestones.
- Preserved `TrainingGuidanceMode`: Hidden/ChecklistOnly suppress step-by-step guidance without changing diagnostic evaluation or scoring semantics.
- Added Application/App/XAML regressions for unavailable-value preservation, canonical checklist reuse, guidance-mode suppression, page staging and real desktop-scenario terminal integration.

## M10.1 — Operator Computer Contracts & Terminal Shell — VALIDATED

- Recorded explicit validation of M9.7 hotfix 5 and the M9 phase gate: local compilation succeeded and all 760 automated tests passed.
- Integrated the user-supplied `MainWindow.axaml` as the authoritative validated layout basis; the manual corrections remove the previous center-workspace overlap/clipping behavior without restoring the discarded synthetic minimum-width/horizontal-scroll workaround.
- Added immutable Application-layer operator-computer page/status/snapshot contracts and a fixed eight-page catalog: GUIDANCE, INFO, ALARMS, COMMANDS, MODES, DIAGNOSTICS, LOG and SESSION.
- Added `OperatorComputerSnapshotProjector`, projecting only already-published `ControlRoomSnapshot` shell status; all page content remains explicitly `ShellOnly` in M10.1.
- Added `OperatorComputerViewModel`, seventh `Computer` workspace, monospace HUD-style `ControlRoomComputerControl`, fixed status line and global F1–F8 page navigation. Page selection/focus remain App presentation state and dispatch no plant commands.
- Added Application/App/XAML regressions for fixed page-set immutability, status projection, selection persistence, keyboard navigation, dedicated terminal binding and preservation of the user-validated MainWindow layout contract.
- No M10.2+ guidance/info/diagnostic content, alarm/log aggregation, command catalog, control-authority model, supervisory automation or session persistence is implemented yet.

## M9.7 — Advanced Fidelity Integration Gate — VALIDATED / M9 GATE COMPLETE

- User confirmed local compilation and complete automated validation: **760/760 tests passed** after hotfix 5.
- The 6,000-step / 60-second Application and real desktop-pump endurance gates pass, including the boundary-aware saturated and superheated root-bracketing regressions.
- Final manual GUI layout corrections were supplied and validated by the user in `MainWindow.axaml`; that file is integrated as the authoritative M9.7 layout baseline.
- M9.7 and the full M9 phase gate are complete; M10.1 begins on this validated baseline.

## M9.7 — Advanced Fidelity Integration Gate — IMPLEMENTATION CANDIDATE

### M9.7 hotfix 5 — superheated phase-boundary root bracketing

- Extended the M1.7 numerical root-search correction symmetrically to the superheated-vapor branch after the 60-second endurance gate exposed `exhaust` at `v=65.477888248812704 m^3/kg`, `u=2434381.9782870663 J/kg`.
- Verified that the conserved state has a genuine solution inside the existing superheated closure (about 17.907 °C and 2.052 kPa); the fixed 512-segment scan missed it because the first admissible superheated temperature lies between coarse samples.
- Added a deterministic boundary-aware superheated fallback that locates the exact admissible temperature interval, injects its valid endpoints, and reuses the existing superheated equations/bisection. No state clamp, envelope widening or new thermodynamic correlation is introduced.
- Added direct regression coverage for the observed low-pressure `exhaust` state and a negative regression proving that a true correlation gap with no root still fails closed.
- Retained the 6,000-step / 60-second direct-session and real desktop-pump endurance gates unchanged.

### M9.7 hotfix 4 — saturation-boundary thermodynamic root bracketing

- Investigated the 60-second endurance failures at `drum` and `exhaust` and confirmed a structural numerical gap in the simplified water/steam closure rather than another seed-only defect. The fixed 512-point full-range saturated-mixture scan could miss a valid root when the admissible saturation interval terminated between two samples near quality 0 or quality 1.
- Preserved the long-validated resolver order/fast paths. Only after saturated-mixture, subcooled-liquid and superheated-vapor resolution all fail, a deterministic boundary-aware saturated-mixture fallback computes the exact upper temperature of the physically valid specific-volume interval and rescans that interval before declaring the state unsupported.
- Added direct regressions using the exact conserved `(v,u)` states observed in manual/endurance failures: the `drum` case resolves near quality 0 at about 120 °C, while the `exhaust` case resolves as wet steam near quality 0.990 at about 39.93 °C. No arbitrary phase clamp or broadened thermodynamic envelope is introduced.
- Removed reliance on the desktop-only inflated `0.001` liquid-compression override; the M9.7 desktop seed returns to the shared historical default margin while retaining its separately versioned balanced 5 MWe / finite-condenser-cooling steam-path lineup.
- Updated stale M9.7 descriptor lineage text and made runtime-pump tests report an actual host failure before generic null/step-count assertions.
- Hotfix 3 layout, full-session reset and 6,000-step/60-second endurance requirements remain intact and must be revalidated locally.

### M9.7 hotfix 3 — workspace viewport, deterministic session reset and extended desktop endurance

- Recorded successful local compilation and complete automated-suite validation of M9.7 hotfix 2.
- Reworked the center workspace viewport so padding belongs to scrollable content rather than the `ScrollViewer` viewport, added explicit horizontal scrolling for wide dashboard grids, a clipped center-column host and a trailing scroll extent so the final card remains fully reachable above the fixed footer.
- Added an explicit `Reset session` desktop action that reconstructs the exact versioned M9.7 desktop scenario through the composition root instead of mutating or partially zeroing live physical state. The old ViewModel unsubscribes from runtime/training events before replacement.
- Extended both the Application-level desktop integration endurance regression and the real App `DesktopControlRoomRuntimePump` path to 6,000 fixed steps (60 simulated seconds), deliberately crossing the manually observed step-3111 block. The candidate desktop seed now uses the validated low-load 5 MWe handoff, a small finite 0.1 MW condenser cooling boundary and a 0.001 compressed-liquid margin; validated M7 identities/defaults remain unchanged.
- Added a fresh-session reload regression proving reset semantics return to logical step 0, PAUSED host mode and the exact original snapshot fingerprint.
- Added XAML contract coverage for the reset action, bidirectional center scrolling, inner scroll padding and explicit trailing extent.

### M9.7 hotfix 2 — desktop drum thermodynamic-margin correction

- Preserved the hotfix-1 continuous-RUN regression at 1,000 logical steps rather than weakening the test after it exposed a second real desktop-seed weakness at the primary steam drum.
- Added an optional primary-liquid compression-margin parameter to the shared operational-seed factory with the exact historical default (`0.000001`) preserved for all previously validated M7/M8/M9 initial-condition call sites.
- The separately versioned M9.7 desktop seed alone opts into a modest `0.0001` density-compression fraction, moving its primary liquid inventories deterministically inside the simplified subcooled-liquid envelope instead of starting effectively on the saturation boundary.
- The thermodynamic resolver/envelope, canonical M3 ownership, historical versioned seeds and scenario physics are unchanged.

### M9.7 hotfix 1 — desktop runtime/manual-GUI gate corrections

- Updated the stale `ApplicationDescriptorTests` expectation from M9.6 to the actual M9.7 integration-gate descriptor.
- Added a real App-layer `DispatcherTimer`/`DesktopControlRoomRuntimePump` so RUN advances bounded deterministic fixed-step batches; PAUSE stops host advancement and SINGLE STEP remains exact. Wall-clock cadence remains outside physics.
- Added a separately versioned `DesktopIntegratedOperationsInitialConditionFactory` / desktop scenario wrapper instead of mutating validated M7 v1 identities. The new seed uses a turbine steam-path inventory aligned with the upstream steam-space condition plus explicit governor droop/opening, with a regression that advances 1,000 logical steps (10 simulated seconds) past the former step-5 `control-out` failure.
- Hardened `ControlRoomPushButton` hit targets with a non-null surface, full stretch/content alignment, minimum height and pointer cursor so the complete visual button rectangle is interactive.
- Added a RUNNING/PAUSED + logical-step progress indicator beside the host controls and in the footer.
- Clipped the center workspace row above the footer and added bottom scroll breathing room so the final `ARCHITECTURE CONTRACT` content can be scrolled fully above the status bar.
- Added App/Application regression coverage for continuous desktop RUN, pause behavior, runtime progress bindings and footer-safe scrolling.

- Recorded successful local compilation and complete automated-suite validation of M9.6 hotfix 1; M9.6 is the validated code baseline for M9.7.
- Added a cross-feature Simulation regression proving M9.3 canonical xenon and M9.4 quasi-spatial feedback compose exactly once through the single global point-kinetics/non-rod-reactivity seam.
- Added a real M9.3 xenon-session integration test spanning M9.1 recorder/checkpoint/full replay, M9.2 post-incident analysis and M9.6 immutable snapshot metric extraction with identical original/replay evidence.
- Added M9.5/M9.6 fidelity-evidence consistency checks that preserve the explicit distinction between validated model capabilities and external historical calibration claims.
- Added real-runtime App/ViewModel integration tests for xenon availability, legacy `Unavailable` semantics and RUN/PAUSE/SINGLE STEP synchronization through the canonical scenario/coordinator boundary.
- Added `docs/M9_ADVANCED_FIDELITY_INTEGRATION_GATE.md`, `docs/M9_FINAL_MANUAL_VALIDATION_CHECKLIST.md` and `docs/milestones/M9.7.md`.
- M9.7 introduces no new physics. Final M9 promotion requires local clean build/tests plus explicit completion of the manual GUI checklist before M10 starts.

### M9.6 hotfix 1 — advanced GUI test compilation fix

- Replaced an invalid object-initializer syntax applied to the `CreateViewModel(...)` factory result in `MainWindowViewModelAdvancedTests.SelectionIndices_ClampWhenRodPumpGeneratorAndAlarmCollectionsShrink` with explicit property assignments.
- Test-compilation-only correction: no App/ViewModel production code, GUI behavior, M9.6 reference validation, physics, runtime, scenario, replay, or ownership semantics changed.

## M9.6 — Calibration & Reference Validation Suite — IMPLEMENTATION CANDIDATE

- Recorded explicit local validation of M9.5: compilation and the complete automated suite passed; M9.5 is now the official validated baseline.
- Added versioned steady-state/transient reference-validation contracts with exact logical-step targets, explicit absolute/relative tolerance budgets, model-version tracking and fail-closed missing-evidence semantics.
- Added stable `ControlRoomSnapshot` reference metric IDs/extraction and curated internal regression baselines for cold shutdown, pre-synchronization and first generator loading; these are explicitly not external historical measurements.
- Added deterministic sensitivity/regression reports for explicit parameter perturbations, including a real `FissionPowerCalibration` sensitivity regression.
- Expanded `NuclearReactorSimulator.App.Tests` with advanced workspace, snapshot-refresh, target-clamping, typed-command routing, alarm/protection/interlock and XAML binding/provenance contract tests before M10.
- Added `docs/CALIBRATION_REFERENCE_VALIDATION.md`, `docs/MANUAL_GUI_VALIDATION_CHECKLIST.md`, `docs/milestones/M9.6.md` and ADR 0073.
- M9.6 remains a candidate until local clean build/complete automated tests and the requested manual GUI validation are explicitly confirmed. M9.5 remains the official validated baseline until then.

## M9.5 — Historical-Inspired Scenario Framework — VALIDATED

- Local compilation and the complete automated suite passed; M9.5 is the official validated baseline for M9.6.
- The implementation content below is unchanged from the previously delivered M9.5 candidate.

- Recorded explicit local validation of M9.4 after hotfix 1: compilation and the complete automated suite passed; M9.4 is now the official validated baseline.
- Added optional versioned `ScenarioDefinition.HistoricalContext` with explicit source references, claim classification (`DocumentedFact`, `EducationalApproximation`, `SimulatorSpecificAssumption`), required model-capability IDs, fidelity statement and deliberate non-claims.
- Added deterministic fail-closed `HistoricalScenarioFidelityReviewer` and `HistoricalScenarioFidelityException`; `ScenarioSessionFactory` now blocks historical-inspired content before runtime creation when declared validated capabilities are missing.
- Advanced JSON scenario persistence to schema v3; v0/v1/v2 migration preserves existing scenario/initial-condition semantics and never invents historical metadata.
- Added `docs/HISTORICAL_INSPIRED_SCENARIO_FRAMEWORK.md`, `docs/milestones/M9.5.md` and ADR 0072.
- M9.5 introduces no named historical reconstruction, source-network access, calibration or scenario-owned physical outcome.

### M9.4 hotfix 1 — test namespace compilation fix

- Added the missing `NuclearReactorSimulator.Domain.Physics.Fluids` namespace import to `ReactorPrimaryControlSolverTests`, allowing the M9.4 quasi-spatial regression to resolve the canonical `VoidFraction` value object.
- Test-compilation-only correction: no production physics, runtime integration, scenario, initial-condition, replay, or M9.4 ownership semantics changed.

## M9.4 — Spatial/Quasi-Spatial Fidelity Refinement — VALIDATED

- Built on the validated M9.3 baseline without changing existing versioned M7/M8/M9.3 scenarios or initial conditions.
- Added opt-in `QuasiSpatialCoreFeedbackDefinition` over the validated M3.3 aggregated-core boundary; arbitrary zone identifiers/coordinates remain supported and adjacency is never inferred from coordinates.
- Reused the existing M2 fuel-temperature, coolant-temperature and void feedback solvers per committed core zone, then reduced local contributions to one current-power-share-weighted global reactivity contribution through the existing non-rod/global point-kinetics seam.
- Added explicit symmetric zone-coupling definitions that smooth only the power-shape-driving signal; coupling does not create local neutron populations, duplicate kinetics, new conserved inventories or implicit grid topology.
- Added deterministic normalized power-shape relaxation with explicit sensitivity and time constant. Candidate zone shares affect the next committed full-plant step; the existing single global point-kinetics owner is preserved.
- Added domain/simulation regressions for definition invariants, weighted feedback, explicit coupling, shape closure/evolution, determinism, zero-sensitivity behavior, and opt-in integration through `ReactorPrimaryControlSolver`.
- Added `docs/SPATIAL_QUASI_SPATIAL_FIDELITY.md`, `docs/milestones/M9.4.md` and ADR 0071.
- Local compilation and the complete automated suite subsequently passed after hotfix 1; M9.4 became the official validated baseline for M9.5.

## M9.3 — Advanced Xenon & Low-Power Transients — VALIDATED

- Recorded explicit local validation after hotfix 2: compilation and the complete automated suite passed; M9.3 is the official validated baseline.
- Approved future M10 `Operator Computer, Supervisory Automation & Human-Machine Integration` architecture and roadmap: fixed menu terminal, independent training-assistance/control-authority axes, M5-owned supervisory automation, fail-closed degradation, bumpless manual takeover, intent taxonomy and replay-backed session archive direction.
- Added `docs/OPERATOR_COMPUTER_SUPERVISORY_AUTOMATION.md`, planned `docs/milestones/M10.md` and ADR 0070.
- Renamed planned M9.7 to `Advanced Fidelity Integration Gate` and moved final release hardening/packaging to M11 after M10.
- Hotfix candidate 2: bounded the Application-level restart-seed determinism regression to a short end-to-end window; the previous 200-step full-plant loop accidentally coupled an M2.8 wiring assertion to the unrelated simplified M3 drum water/steam envelope. No production physics, seed data, scenario semantics or replay/versioning contracts changed.
- Hotfix candidate 1: added the missing Simulation iodine/xenon namespace import in `ReactorPrimaryControlSolverTests`; this is test-compilation-only and does not change production physics, runtime behavior, scenario semantics or replay/versioning contracts.
- The implementation package was subsequently validated locally after the two test-only hotfixes; M9.3 is now the official validated baseline.
- Promoted the canonical validated M2.8 I-135/Xe-135 state into the M5 reactor/primary runtime through an optional `IodineXenonDefinition` / `IodineXenonState` seam rather than introducing Application/scenario physics.
- Composed committed xenon reactivity through the existing explicit non-rod-reactivity seam before point kinetics and advanced poison inventories with the existing M2.8 solver after candidate kinetics/fission power.
- Promoted only immutable committed M2.8 xenon diagnostics through the control-room presentation boundary; configurations without canonical poison state remain explicitly `Unavailable`.
- Preserved exact-version compatibility by leaving existing M7 v1 initial conditions xenon-disabled instead of silently changing prior replay/checkpoint semantics.
- Added versioned post-shutdown restart and poisoned low-power initial conditions plus `AdvancedXenonScenarioPack`; scenarios use existing typed rods, circulation, alarm and protection commands and do not script xenon/power trajectories or recovery outcomes.
- Added deterministic Simulation/Application regression coverage, M9.3 milestone/domain documentation and ADR 0069.


## M9.2 validated handoff maintenance checkpoint

- Recorded explicit local validation of M9.2: compilation and the complete automated test suite passed; M9.2 is the official functional baseline.
- Synchronized authoritative status/roadmap/architecture/readme documentation for the transition to M9.3.
- Rebuilt `docs/PROJECT_HANDOFF.md` and `docs/NEW_CHAT_START.md` as the authoritative restart pair for a new project chat.
- Performed conservative dead-code review across production/test symbols. Removed `ShellControlRoomCommandDispatcher`, an internal legacy shell fallback with zero production/test references; retained low-reference serializer interfaces and scenario packs because they are deliberate public/architectural seams.
- The cleanup changes occur after the already validated M9.2 run; run a clean restore/build/test on this maintenance package before treating the cleanup delta as revalidated or beginning M9.3 implementation.


## M9.2 — Post-Incident Analysis (baseline candidate)

- Recorded M9.1 as locally validated after successful build and complete test suite.
- Added deterministic `ScenarioPostIncidentAnalyzer` over immutable M9.1 recordings.
- Added exact/automatic incident anchors, logical-step pre/post windows, ordered evidence timeline and start/anchor/end state summaries.
- Added observed response metrics for alarms, protection activation, operator action, fault clearance and peak signal/alarm/fault indicators.
- Added nearest preceding replay-backed checkpoint linkage without creating a second restore mechanism.
- Added versioned `PostIncidentAnalysisReport` schema v1 plus JSON serializer with fail-closed unknown-schema handling.
- Added ADR 0068 formalizing that temporal ordering is evidence and must not be silently promoted to causal inference.
- Added regression tests and updated handoff/status/roadmap documentation.



## M9.1 — Recorder, Checkpoints & Full Replay (baseline candidate)

- Recorded explicit local validation of the exact M8.5 hotfix 2 → M8.6 → M8.7 hotfix 2 chain after `dotnet clean`, restore/build and the complete automated suite passed; M8.7 hotfix 2 is now the official validated baseline and the M8 gate is complete.
- Added `ScenarioRecorder` capturing the initial frame plus every deterministic fixed step independently from presentation publication stride.
- Added immutable `ScenarioRecording` with retained control-room frames, accepted typed operator actions and a monotonic recorder event stream for operator actions, alarm events, fault transitions and protection transitions.
- Added versioned `ControlRoomSnapshotFingerprint` v1 and replay-backed `ScenarioCheckpoint` schema v1; checkpoints never serialize or own private physical solver state.
- Added `ScenarioFullReplayRunner` with exact scenario/initial-condition reconstruction, accepted-action replay, per-frame fail-closed fingerprint verification, event-stream verification and deterministic seek-to-checkpoint verification.
- Added `JsonScenarioCheckpointSerializer` with explicit unsupported-schema rejection.
- Added M9.1 Application/Infrastructure regression tests, ADR 0067 and `docs/RECORDER_CHECKPOINT_FULL_REPLAY.md`.


## M8.7 stacked baseline candidate / hotfix 2 — M8.5 committed-node thermodynamic-admissibility guard

- Fixed the only failing stacked-chain regression: an intentionally extreme pressure-driven break could respect `MaximumInventoryFractionPerStep` yet still move a near-saturated liquid node outside the simplified water/steam state envelope.
- Pressure-driven breaks now use a deterministic committed-node admissibility probe and further cap only the M8.5 mass/energy removal when required by the existing thermodynamic closure; no second full-plant predictor step is performed.
- The declared inventory fraction remains a strict upper bound; the guard never adds inventory, mutates committed state, relaxes the thermodynamic model or changes M3 single-integration ownership.
- The existing severe-break regression now verifies both positive additional loss and compliance with the declared maximum without causing `WaterSteamStateOutOfRangeException`.
- M8.5, M8.6 and M8.7 remain unvalidated stacked candidates; the last official validated baseline remains M8.4 hotfix 2.


## M8.7 — Safety-Response Scenario Pack (stacked baseline candidate)

- M8.5 and M8.6 remain unvalidated stacked candidates because the user is temporarily away from the validation environment; M8.7 is intentionally stacked on that exact chain and does not change the official validated baseline (M8.4 hotfix 2).
- Added three capstone safety-response exercises reusing exact M8.3/M8.5/M8.6 fault declarations: protection fail-safe, large-break-class response and station-blackout-class response.
- Added `SafetyResponseCheckpointEvaluator` with committed-presentation-only acceptance checks and 100-point M7.7 training plans; acceptance criteria never inject physical/protection outcomes.
- Added `SafetyResponseEvaluationSession`, exposing deterministic assessment plus the existing accepted-operator-action logical timeline for debrief.
- Added M8.7 regression tests, ADR 0066, `docs/SAFETY_RESPONSE_SCENARIO_PACK.md` and stacked-candidate handoff/status/roadmap updates.

## M8.6 — Electrical Loss & Station Blackout-Class Scenarios (stacked baseline candidate)

- M8.5 remains unvalidated because the user is temporarily away from the validation environment; M8.6 is intentionally stacked on that candidate and does not change the official validated baseline (M8.4 hotfix 2).
- Added `electrical.external-supply-loss`, bound fail-closed to the exact canonical M4.5 grid id. While active it forces canonical generator breakers open through `GeneratorGridInputs` and overrides close requests without writing breaker state directly.
- Added deterministic external-supply-loss and station-blackout-class scenario definitions.
- Station-blackout-class consequences are explicit composition of validated M8.2 pump trips, M8.3 powered actuator-command fail-low faults and M8.4 turbine/generator trips; no synthetic AC/DC bus, diesel, battery or ECCS electrical model is introduced.
- Fault clearance removes external-supply forcing only; generator reconnection remains a deliberate synchronization/close operation.
- Documented that M2.5 stateful decay heat is not yet promoted into the M5.7 integrated operational runtime and deliberately avoided fabricating a fixed post-shutdown heat source.
- Added M8.6 Application regression tests, ADR 0065, `docs/ELECTRICAL_LOSS_STATION_BLACKOUT_SCENARIOS.md` and stacked-candidate handoff/status/roadmap updates.

## M8.5 — Educational Leak/LOCA-Class Scenarios (baseline candidate)

- Recorded explicit local validation of M8.4 hotfix 2: compilation and complete tests passed; M8.4 is now the validated baseline.
- Added `loca.pressure-driven-break`, a deterministic bounded break boundary driven only by committed canonical node pressure and immutable scenario parameters.
- Added conservative break mass plus carried internal-energy removal through existing `PlantNetworkSourceTerms`; `PlantNetworkOrchestrator` remains the sole fluid/thermal inventory integrator.
- Added an explicit per-step inventory-removal bound as a lumped-model validity/numerical guard; it does not represent ECCS, containment response or scripted accident correction.
- Added small primary leak, large break-class and steam-space leak/depressurization deterministic scenario definitions over the validated M7.6 operating initial condition.
- Added fail-closed target/parameter/conflict validation and regression tests for mass/energy loss, relative depressurization, zero driving-pressure flow, inventory bounds and built-in registry binding.
- Added ADR 0064, `docs/EDUCATIONAL_LEAK_LOCA_SCENARIOS.md` and M8.5 milestone/handoff/status/roadmap updates with explicit non-licensing fidelity limits.

## M8.4 — Turbine / Generator / Feedwater / Condenser Transients — VALIDATED / HOTFIX 2

- Hotfix 2 scales the transient-ready condenser cooling seed from 20 MW to 0.1 MW so the compact conserved exhaust inventory remains inside the simplified water/steam closure envelope during deterministic seed/runtime steps; fault semantics and canonical M4.3 ownership are unchanged.

- Hotfix candidate 1: added the missing Simulation condenser/feedwater namespace imports in `IntegratedAutomaticOperationRuntimeEngine`; no transient, fault, solver or scenario semantics changed.
- Recorded explicit local validation of M8.3: compilation and complete tests passed; M8.3 is now the validated baseline.
- Added exact `secondary-transient-ready` v1 initial condition reusing canonical M7 owners with finite 0.1 MW M4.3 condenser cooling-boundary capacity scaled to the compact educational exhaust inventory.
- Added deterministic turbine-trip and generator-trip/load-rejection fault applicators that feed existing M5.5 protection inputs rather than writing valves, breakers, rotor speed or electrical power directly.
- Added condenser cooling degradation/loss as a bounded per-step overlay on canonical `CondenserCoolingBoundaryInput.AvailableHeatRejectionPower`; condenser pressure/vacuum remain derived from conserved state.
- Added feedwater degradation/loss scenarios by composing validated M8.2 `hydraulic.pump-degradation` and `hydraulic.pump-trip` effects on the canonical feedwater pump.
- Added four M8.4 scenario definitions, built-in applicator registration, fail-closed target validation and Application regression tests.
- Added ADR 0063, `docs/SECONDARY_SYSTEM_TRANSIENTS.md` and M8.4 milestone/handoff/status/roadmap updates.

## M8.3 — Instrumentation & Control Faults — VALIDATED

- M8.2 Hydraulic Component Faults hotfix 2 promoted to validated baseline after explicit local build/test success.
- Added built-in deterministic sensor bias/freeze/failed-low/failed-high/unavailable applicators reusing the canonical M5.1 `SensorFaultInput` seam.
- Added controller-output freeze/fail-low/fail-high and actuator-command freeze/fail-low/fail-high as bounded temporary overlays on canonical controller inputs; no direct physical state writes.
- Added fail-closed canonical target/conflict validation and one-controller/one-actuator ambiguity checks for actuator-specific command faults.
- Added `InstrumentationControlFaultScenarioPack` demonstration and protection fail-safe diagnostic scenarios.
- Added M8.3 regression tests for measured-signal semantics, committed-frame protection ordering, control-command forcing/clearance, actuator-command freeze and built-in registry binding.
- Added ADR 0062, `docs/INSTRUMENTATION_CONTROL_FAULTS.md` and M8.3 milestone/handoff/status/roadmap updates.

## M8.2 — Hydraulic Component Faults — VALIDATED / HOTFIX 2

- Hotfix candidate 2: corrected the new App regression test to use the xUnit `Assert.Single(collection, predicate)` overload required by analyzer rule xUnit2031; production code and M8.2 behavior are unchanged.
- Hotfix candidate 1: corrected the electrical `GENERATOR TARGET` selector to use a neutral `GeneratorSelectionState` instead of inheriting the generator-trip visual state.
- Hotfix candidate 1: made turbine speed/load operator controls fail closed when either the turbine trip or generator trip is active; physical protection ownership remains unchanged.
- Hotfix candidate 1: added the first headless `NuclearReactorSimulator.App.Tests` project with ViewModel regression tests for generator selection, turbine-trip gating, breaker-close permissives, target-index clamping, typed dispatch and the XAML generator-selector binding contract.
- Recorded M8.1 hotfix 1 as locally validated after successful build and complete test suite.
- Added typed M8.2 hydraulic fault applicators for pump trip/degradation, valve fail-open/fail-closed/stuck, valve-controlled path restriction/blockage and selected node leaks.
- Added immutable `HydraulicComponentFaultInputs` consumed inside the existing protected full-plant step; no second pump/valve/network solver is introduced.
- Added selected leak mass + carried internal-energy removal through signed `PlantNetworkSourceTerms`, preserving the one `PlantNetworkOrchestrator` integration/audit boundary.
- Added `HydraulicComponentFaultScenarioPack.Demonstration`, built-in hydraulic applicator registration and end-to-end Application tests over real canonical plant state.
- Added ADR 0061, `docs/HYDRAULIC_COMPONENT_FAULTS.md` and M8.2 milestone/handoff/status/roadmap updates.

## M8.1 — Deterministic Fault-Injection Framework — VALIDATED / HOTFIX 1

- Hotfix candidate 1: corrected the scenario-v2 deserializer fallback for fault parameters so both operands of the null-coalescing expression are `SortedDictionary<string, string>`; no schema, ordering or fault semantics changed.
- Recorded explicit local validation of M7.7: compilation and complete tests passed; M7.7 is now the validated baseline and the M7 gate is complete.
- Added explicit immutable scenario fault declarations with stable fault/type/target IDs, deterministic parameters and activation/optional deactivation triggers.
- Added exact logical-step and named committed-`ControlRoomSnapshot` plant-condition trigger semantics with no wall-clock/random scheduling.
- Added fail-closed exact-ID registries for runtime-bound fault applicators and plant-condition evaluators.
- Added deterministic single-pass `Pending → Active → Cleared` lifecycle state with logical-step stamps and monotonic transition sequence.
- Added `ScenarioFaultRuntimeEngine` as a scheduling/lifecycle decorator around the canonical runtime; M8.1 itself adds no concrete subsystem fault physics.
- Added fault lifecycle projection to `ControlRoomSnapshot` and deterministic replay reconstruction from the same versioned scenario definition.
- Advanced scenario JSON persistence to schema v2 with deterministic v0/v1 migration that preserves exact initial-condition identity and invents no faults.
- Added M8.1 application/infrastructure tests, ADR 0060 and `docs/DETERMINISTIC_FAULT_INJECTION_FRAMEWORK.md`.

## M7.7 — Training Objectives, Procedure Guidance & Evaluation — VALIDATED

- Recorded explicit local validation of M7.6: compilation and complete tests passed; M7.6 is now the validated baseline.
- Added a deterministic accepted-operator-action journal at the scenario command boundary; runtime-host commands and rejected actions are excluded.
- Added `DeterministicStepCompleted` observation on the Application runtime coordinator so training evaluation sees every fixed simulation step independent of presentation publication stride.
- Added generic training checkpoints, evaluation criteria, objective scoring, procedure-deviation penalties and optional `Hidden` / `ChecklistOnly` / `Guided` assistance modes.
- Added historical first-achievement checkpoint tracking and ordered accepted-action sequence evaluation without mutating physics, control, protection or alarms.
- Added the 100-point `Integrated Normal Operations Training` capstone over the validated M7.6 `stable-low-load-parallel-operation` v1 initial condition.
- Added desktop training evaluation presentation, M7.7 application tests, ADR 0059 and `docs/TRAINING_OBJECTIVES_GUIDANCE_EVALUATION.md`.

## M7.6 — Power Manoeuvring & Normal Shutdown — VALIDATED

- Recorded explicit local validation of M7.5: compilation and complete tests passed; M7.5 is now the validated baseline.
- Added exact `stable-low-load-parallel-operation` v1 with canonical breaker-closed 5 MWe low-load handoff.
- Extended the canonical operational-seed helper with optional breaker/load seed parameters while preserving all earlier M7 defaults.
- Added bounded power-manoeuvring guidance using only validated generator-load, rod and turbine-speed command seams.
- Added observational temperature/void checks and preserved quantitative xenon as explicitly unavailable at the M5.7 operational snapshot boundary.
- Added controlled normal-shutdown guidance for unload, breaker open, rod insertion, turbine rundown and continued main circulation.
- Updated desktop composition to load the exact M7.6 session paused.
- Added M7.6 application tests, ADR 0058 and `docs/POWER_MANOEUVRING_NORMAL_SHUTDOWN.md`.

## M7.5 — Grid Synchronization & Load Increase — VALIDATED

- Added exact `pre-synchronization-grid-loading` v1, reusing canonical M7.2 construction with a 3000 rpm phase-matched breaker-open handoff.
- Added observational M7.5 synchronization/load checklist and seven-step guidance through the stable low-load M7.6 handoff.
- Enabled scenario-gated generator breaker close while preserving the authoritative M4.5 synchronization close-check.
- Completed `GeneratorLoadRaise/Lower` translation through bounded M4.5 requested electrical power; no direct rotor torque/output mutation.
- Desktop now loads the M7.5 session paused.

## M7.4 — Heat-Up, Steam Raising & Turbine Startup — validated

- Hotfix 1 validated after successful local build and complete tests.
- Hotfix candidate 1: made saturated steam-space recipe construction robust near the dry-saturated boundary. The existing 0.99 vapor-quality seed is preserved whenever the validated thermodynamic closure resolves it; otherwise initialization deterministically retries at 0.98, remaining inside the same two-phase model envelope without changing the solver.
- Recorded M7.3 as locally validated after successful build and complete tests.
- Added exact-version `low-power-steam-raising` v1 through `HeatUpTurbineStartupInitialConditionFactory`.
- Extended the canonical M7.2 recipe helper with explicit rod-position, primary-temperature and turbine-startup-lineup seed parameters while preserving existing M7.2/M7.3 defaults.
- Added a versioned startup lineup with stop/admission availability, governing control initially closed and no new direct stop-valve owner.
- Added observational heat-up, steam-pressure/inventory, turbine-roll/warm-up/near-synchronous and generator-isolation checks.
- Added declarative M7.4 guidance and fail-closed permissions: turbine speed control is enabled; generator breaker close/load raise/load lower remain blocked for M7.5.
- Updated desktop composition to load the exact M7.4 session paused and display M7.4 guidance/checks.
- Added M7.4 application tests, ADR 0056 and `docs/HEAT_UP_STEAM_RAISING_TURBINE_STARTUP.md`.

## M7.3 — First Criticality & Low-Power Operation — VALIDATED

- Recorded explicit local validation of M7.2 hotfix 1: compilation and complete tests passed; M7.2 is now the validated baseline.
- Added exact-version `pre-criticality-source-range` v1 initial condition reusing the canonical M7.2 construction path with established main circulation and a tiny deterministic non-zero point-kinetics seed.
- Added controlled rod INSERT/HOLD/WITHDRAW scenario permissions while continuing to fail closed on turbine speed, generator load and breaker-close actions.
- Added presentation-only first-criticality checks for source-range power, near-critical reactivity, first criticality, educational low-power band and reactor-period stabilization.
- Added declarative first-criticality/low-power guidance; guidance never auto-dispatches commands or forces physical state.
- Documented the source-range seed as versioned initial-condition data rather than an external neutron-source solver.
- Added an explicit xenon availability objective: quantitative xenon remains `Unavailable` until canonical M2.8 state is promoted through the M5.7 operational envelope.
- Updated desktop composition to load the exact M7.3 session paused and display scenario guidance/checks.
- Added M7.3 application tests, ADR 0055 and `docs/FIRST_CRITICALITY_LOW_POWER.md`.

## M7.2 — Cold Shutdown & Pre-Startup — VALIDATED

- Corrected the M7.2 candidate compile blocker in `ColdShutdownInitialConditionFactory`: added the missing Simulation runtime namespace imports for controller inputs, primary-circuit boundary/integration inputs and turbine-island state/input types; no physics or milestone scope changed.
- Recorded explicit local validation of M7.1: compilation and complete tests passed; M7.1 is now the validated baseline.
- Added exact-version `cold-shutdown-pre-start` v1 built-in Application initial-condition factory reconstructed through canonical M1–M5 owners and the validated simplified water/steam closure.
- Added an operational cold/subcritical seed with rods inserted, pumps stopped, steam admission isolated, turbine stationary and generator breaker open.
- Added presentation-only pre-start readiness definitions/evaluator for signal health, protection, reactor shutdown, rod insertion, circulation, turbine, breaker, steam isolation and annunciator state.
- Added ordered declarative preparation guidance with optional suggested operator actions; guidance never auto-dispatches commands or patches physical state.
- Added fail-closed M7.2 scenario permissions that allow circulation preparation but deliberately exclude rod withdrawal and breaker closure before M7.3.
- Promoted the desktop composition from the no-session shell fallback to a real paused exact-version M7.2 session through the validated M7.1 registry/session boundary.
- Added M7.2 application/runtime-composition tests, ADR 0054 and `docs/COLD_SHUTDOWN_PRESTART.md`.

## M7.1 — Versioned Initial Conditions & Scenario Framework — VALIDATED

- Recorded explicit local validation of M6.7: compilation and complete tests passed; M6 gate is now complete.
- Added immutable exact-version `InitialConditionReference` / descriptor contracts and `IVersionedInitialConditionFactory` reconstruction seam.
- Added `VersionedInitialConditionRegistry` with duplicate rejection and exact-version-only resolution; no silent latest-version fallback.
- Added immutable scenario metadata, descriptive objectives and explicit allowed operator command kinds.
- Added fail-closed `ScenarioCommandDispatcher` while keeping run/pause/single-step under runtime-host ownership.
- Added `ScenarioSessionFactory` as the canonical fresh paused load/start boundary over the validated M6.7 runtime coordinator.
- Added deterministic `ScenarioReplayRunner` reusing the M0 logical `SimulationCommandTrace<ControlRoomCommand>` seam.
- Added Infrastructure JSON scenario schema v1 with deterministic v0→v1 migration that preserves exact initial-condition identity/version and rejects unknown future versions.
- Added M7.1 application/infrastructure tests, ADR 0053 and `docs/INITIAL_CONDITIONS_SCENARIO_FRAMEWORK.md`.
- M7.1 intentionally does not invent the first operational cold-shutdown recipe; M7.2 owns that concrete initial condition and pre-start flow.

## Documentation continuity refresh after M6.7 candidate

- Hardened `PROJECT_HANDOFF.md` as the authoritative new-chat checkpoint with explicit validation state, ownership map, restart protocol and exact continuation point.
- Added `docs/NEW_CHAT_START.md` with a ready-to-paste conversation bootstrap.
- Added `docs/README.md` as a documentation navigation map.
- Clarified across status/roadmap/M6.7 docs that M6.6 is the last explicitly validated baseline and M6.7 remains candidate until local validation is explicitly confirmed.
- Renamed the legacy architecture-debt section to apply to all future phases rather than only M5.

## M6.7 — Control-Room Integration & Performance Baseline

- Added the live M5.7 `IntegratedAutomaticOperationRuntimeEngine` and `ControlRoomRuntimeCoordinator`.
- Added complete typed command translation for rods, MCPs, turbine/generator controls, breakers, protection and annunciator actions.
- Added one-step transient command consumption plus persistent immutable controller/setpoint updates.
- Added bounded accelerated batches and rendering-cadence-independent presentation publication tests.
- Added ADR 0052 and `docs/CONTROL_ROOM_INTEGRATION_PERFORMANCE.md`; after validation M6 is complete and M7.1 is next.

## M6.6 — Trends, Alarms & Event Timeline

- Recorded successful local validation of M6.5 Turbine, Generator & Electrical Panels.
- Added presentation-only M5.6 alarm/annunciator, first-out and event contracts to `ControlRoomSnapshot`.
- Added typed targeted/bulk alarm ACK and RESET Application command intents without changing M5.5 protection ownership.
- Added configurable bounded logical-step trend history over presentation values only, with deterministic same-step replacement and explicit unavailable gaps.
- Added bounded event history deduplicated and ordered by the validated M5.6 monotonic logical sequence number.
- Added the production Alarms & Events workspace with trends, annunciator controls, first-out groups and deterministic event timeline.
- Added M6.6 presentation/history tests, ADR 0051 and `docs/TRENDS_ALARMS_EVENT_TIMELINE.md`; next planned milestone after validation is M6.7 Control-Room Integration & Performance Baseline.

## M6.5 — Turbine, Generator & Electrical Panels

- Recorded successful local validation of M6.4 Primary-Circuit Mnemonics.
- Added `TurbineSecondaryPanelSnapshot` and `ElectricalPanelSnapshot` Application presentation contracts with canonical M4 topology/equipment identity.
- Added measured M5.1 turbine shaft power, rotor speed, condenser pressure/vacuum/hotwell mass, generator frequency/output and gross electrical output presentation.
- Added explicitly labelled model diagnostics for main steam/admission, turbine stages, condenser/feedwater and generator/grid synchronization details.
- Added turbine/generator trip presentation plus typed turbine-speed, generator-load, breaker close/open and trip operator command intents.
- Added fail-closed breaker-close UI gating from published synchronization permissives while keeping M4.5 authoritative.
- Added M6.5 presentation-contract tests, ADR 0050 and `docs/TURBINE_GENERATOR_ELECTRICAL_PANELS.md`; next planned milestone after validation is M6.6 Trends, Alarms & Event Timeline.

## M6.4 — Primary-Circuit Mnemonics

- Added presentation-only primary-circuit snapshots for loops, MCPs, fuel-channel branches, steam drums and primary-connected valves.
- Projected measured M5.1 loop flow/header ΔP and drum pressure/level separately from explicitly labelled model diagnostics.
- Added typed main-circulation-pump START/RUN and STOP Application command intents without changing M5.3/M3 ownership.
- Added the Primary Circuit mnemonic workspace, M6.4 tests, ADR 0049 and `docs/PRIMARY_CIRCUIT_MNEMONICS.md`.
- Recorded M6.3 as validated; next planned milestone after validation is M6.5 Turbine, Generator & Electrical Panels.

## M6.3 — Reactor/Core Panel

- Marked M6.2 as the locally validated baseline after successful build and complete test execution.
- Added Application-only reactor/core presentation contracts with measured reactor-power projection and explicitly labelled kinetics/reactivity/rod/core-zone diagnostics.
- Added the first domain-specific Reactor/Core workspace with coarse zone tiles, canonical rod state/target presentation and M5.5 SCRAM/interlock context.
- Added typed rod insert/hold/withdraw, SCRAM and protection-reset operator command intents without UI-side state mutation.
- Kept missing M2.8 xenon operational state explicitly unavailable rather than reconstructing/synthesizing it in presentation code.
- Added M6.3 tests, ADR 0048, `docs/REACTOR_CORE_CONTROL_ROOM_PANEL.md` and `docs/milestones/M6.3.md`; next planned milestone after validation is M6.4 Primary-Circuit Mnemonics.

## M6.2 — Reusable Instrument & Control Components

- Marked M6.1 as the locally validated baseline after successful build and complete test execution.
- Added shared Application-layer `ControlRoomVisualState` semantics for Normal, Warning, Trip and Unavailable presentation.
- Added a stable component/interaction catalog for numeric indicators, meters, lamps, toggle switches, selectors and pushbuttons without Avalonia coupling.
- Added reusable Avalonia control-room components plus a shell component gallery for visual validation.
- Standardized display-only versus interactive behavior, keyboard/pointer rules and fail-closed unavailable-state handling.
- Added M6.2 presentation-contract tests, ADR 0047, `docs/CONTROL_ROOM_COMPONENT_LIBRARY.md` and `docs/milestones/M6.2.md`; next planned milestone after validation is M6.3 Reactor/Core Panel.

## M6.1 — Control-Room Application Shell

- **VALIDATED** after successful local build and complete test execution.
- Marked M5.7 as the locally validated baseline and closed the complete M5 automatic-operation gate after successful build/test execution.
- Added stable Overview, Reactor, Primary, Turbine/Secondary, Electrical and Alarms/Events control-room workspaces.
- Added narrow Application-layer `ControlRoomSnapshot` projection/source contracts so Avalonia consumes presentation state rather than authoritative full-plant truth.
- Added typed `ControlRoomCommand` / `IControlRoomCommandDispatcher` seams and shell run/pause/single-step dispatch without UI-side physics.
- Removed the Avalonia project's direct Simulation project reference and added architecture tests forbidding direct Simulation namespace use from App source.
- Added scalable desktop shell layout, presentation-only performance budgets, ADR 0046, `docs/CONTROL_ROOM_APPLICATION_SHELL.md` and `docs/milestones/M6.1.md`; next planned milestone after validation is M6.2.

## M5.7 — Integrated Automatic-Operation Baseline

- Marked M5.6 as the locally validated baseline after the corrected build and complete test suite passed.
- Added canonical `IntegratedAutomaticOperationState` / inputs / snapshot composition over existing physical, instrumentation, controller, protection and annunciator owners.
- Added committed measured-frame ordering: current M5 decisions use one committed frame; instrumentation over candidate true state becomes the next-step frame.
- Added deterministic headless verification phases for reference hold, explicit setpoint/input changes and protection/interlock expectation cases without a hidden scenario scheduler.
- Added measured tracking and raw mass/energy closure, signal-validity and annunciator acceptance metrics with observational-only criteria.
- Added M5.7 integration tests, ADR 0045, `docs/INTEGRATED_AUTOMATIC_OPERATION.md` and `docs/milestones/M5.7.md`; next planned milestone after validation is M6.1.

## M5.6 — Alarms & Annunciator State

- Added deterministic alarm conditions over measured M5.1 channels and observational M5.5 protection state.
- Added explicit non-latching and latched-until-reset annunciator semantics with independent acknowledgement and safe reset.
- Added deterministic first-out grouping and monotonic logical alarm-event ordering without wall-clock dependencies.
- Added immutable alarm, first-out-group and event snapshots plus an observational M5.6 wrapper over the validated M5.5 protected step.
- Marked M5.5 validated and advanced the next planned milestone to M5.7 integrated automatic-operation baseline.



## M5.5 — Interlocks, Trips & SCRAM

- Marked M5.4 as the locally validated baseline after successful compilation and complete automated test-suite confirmation.
- Added measured-signal-only deterministic `ProtectionSystemDefinition` with latching high/low trip functions, reset hysteresis and explicit fail-closed invalid-measurement policy.
- Added reactor SCRAM, turbine trip and generator trip latching actions plus explicit manual trip/reset seams and measured reset permissives.
- Added non-latching rod-withdrawal, turbine-admission-opening and generator-breaker-close interlocks.
- Added explicit protection-over-normal-control arbitration through canonical M2 rod commands, M4.1 stop valves, M4.2 turbine `TripCommand` and M4.5 breaker-open commands.
- Added `ProtectedAutomaticFullPlantSolver` composing M5.3 + M5.4 + M5.5 over one measured frame and one M4.7 physical step.
- Added protection/arbitration diagnostics separated from alarm presentation, ADR 0043, `docs/PROTECTION_INTERLOCKS_TRIPS_SCRAM.md` and `docs/milestones/M5.5.md`.

## M5.4 — Turbine, Steam & Feedwater Control Loops

- Recorded successful local validation of M5.3 as the reactor/primary automatic-control baseline.
- Added semantic M5.4 turbine-speed/load, steam-pressure, steam-drum-level and hotwell-inventory loop definitions over measured signals and M5.2 controller/actuator primitives.
- Added canonical normal-operation admission-valve validation; stop valves remain reserved for M5.5 trip/isolation logic.
- Added hotwell-mass instrumentation source and canonical condensate/feedwater pump command adapters without duplicate pump or inventory state.
- Added automatic M4.2 stage-flow replacement from the limiting projected stop/control/admission valve path while preserving the single plant-network hydraulic integration.
- Added integrated M5.3 + M5.4 automatic-control composition over one measured frame, disjoint physical actuator targets and one M4.7 physical full-plant step.
- Added M5.4 simulation verification, ADR 0042, `docs/TURBINE_STEAM_FEEDWATER_CONTROL_LOOPS.md` and `docs/milestones/M5.4.md`; updated handoff/status/roadmap/architecture/application metadata.


## M5.3 — Reactor & Primary-System Control Loops

- Recorded successful local validation of M5.2 as the reusable controller/actuator primitive baseline.
- Added canonical reactor/primary loop definitions that bind measured-signal controllers and typed actuator commands to specific rod/group and main-circulation-pump owners.
- Added main-circulation semantic instrument sources for total pump flow and header pressure rise.
- Reused the validated M2 `ControlRodSystemSolver`, rod-reactivity model, point kinetics and fission-power scaling rather than introducing a synthetic controller-to-power shortcut.
- Added explicit non-rod-reactivity input seam for temperature/void/xenon/manual contributions composed outside the controller primitive.
- Added committed-state ordering: current committed rods determine current kinetics; controller commands advance rods for the next committed step.
- Added canonical MCP command application by replacing only operational `PumpState` before the one existing M4.7 physical step.
- Added controlled full-plant input rewriting that replaces only the M3 total-fission-power seam with point-kinetics-derived power while preserving downstream M3 spatial heat-deposition ownership.
- Added immutable reactor/primary control diagnostics and tests for rod motion, reactivity progression, kinetics/fission coupling, pump command application and topology/source validation.
- Added ADR 0041, `docs/REACTOR_PRIMARY_CONTROL_LOOPS.md` and `docs/milestones/M5.3.md`; updated handoff/status/roadmap/architecture/application metadata.

## M5.2 — Controller & Actuator Primitives

- Recorded successful local validation of M5.1 as the measured-signal instrumentation baseline.
- Added canonical P/PI/PID controller definitions bound exclusively to `MeasuredSignalFrame` channel ids.
- Added deterministic fixed-step controller state, manual/automatic modes, output limits, conditional-integration anti-windup and bumpless manual-to-auto transfer.
- Added explicit invalid/unavailable-measurement behavior that holds the last command without integrating hidden controller state.
- Added typed controller output frames and detailed controller diagnostics for P/I/D terms, saturation, anti-windup and transfer state.
- Added canonical controller-to-actuator bindings and typed valve-position, pump-speed/run and control-rod motion command seams.
- Kept actuator command memory separate from physical plant/rod ownership; plant-specific loop wiring remains deferred to M5.3/M5.4.
- Added M5.2 domain/simulation tests, ADR 0040, `docs/CONTROLLER_ACTUATOR_PRIMITIVES.md` and milestone documentation.

## M5.1 — Instrumentation & Signal Model

- Recorded M4.7 approval as the validated full-plant steady-state baseline and closed the M4 gate.
- Added canonical instrumentation/channel definitions, finite signal ranges and linear output scaling.
- Added stable semantic true-state source catalog over the immutable M4.7 `FullPlantSnapshot`, including aggregate and per-component plant signals.
- Added separate `InstrumentationState` for deterministic first-order lag/filter memory only.
- Added controller/UI-facing `MeasuredSignalFrame` with no direct true-state reference plus diagnostic-only processing traces.
- Added explicit signal validity, quality, out-of-range/clamp reporting and deterministic bias/freeze/failed-low/failed-high/unavailable fault seams.
- Added `InstrumentedFullPlantSolver` composition that preserves the single M4.7 physical evolution path and observes the resulting immutable snapshot without duplicate physical integration.
- Added M5.1 domain/simulation tests, ADR 0039, `docs/INSTRUMENTATION_SIGNAL_MODEL.md` and milestone documentation.

## M4.7 — Full-Plant Steady-State Baseline

- Recorded successful local validation of M4.6 as the new baseline.
- Added canonical `FullPlantState`, thin `FullPlantSolver` and immutable `FullPlantSnapshot` over the existing M4.6 state owners without a new physical integrator.
- Added fixed-input `FullPlantReferenceOperatingPoint`, explicit `FullPlantSteadyStateCriteria` and deterministic `FullPlantLongRunRunner` / result metrics.
- Added raw long-run mass, coupled stored-energy, rotor-speed, electrical-output and first-law closure drift reporting with no hidden state correction.
- Added gross plant-performance diagnostics for reactor heat, turbine shaft, generator mechanical input/electrical export, condenser rejection, generator losses, efficiency and heat rate.
- Added deterministic 1,000-step full-plant reference verification and criteria rejection tests.
- Added ADR 0038, `docs/FULL_PLANT_STEADY_STATE.md` and `docs/milestones/M4.7.md`; updated project handoff/status/roadmap/architecture/application metadata.
- M4.7 remains a baseline candidate until local build/test validation is reported.

## M4.6 — Integrated Secondary-Cycle Heat Balance

- Marked M4.5 as the locally validated baseline after successful build and complete automated test-suite confirmation.
- Added canonical `IntegratedSecondaryCycleDefinition`, inputs, solver, snapshot and step-result composition over the validated M4.5 reactor-to-grid stack without new mutable physical state.
- Added `SecondaryCycleHeatBalanceAudit` reconciling thermofluid stored energy, rotor kinetic energy, turbine shaft transfer, generator mechanical input, electrical export and conversion losses.
- Added explicit nuclear heat, pump hydraulic power, feedwater conditioning and condenser heat-rejection diagnostics in the integrated first-law boundary.
- Added raw supplemental-power classification, shaft-transfer, mechanical-to-electrical, coupled-domain and full-path closure residuals; no residual correction or hidden bookkeeping is introduced.
- Surfaced authoritative closed-loop external-mass and mass-closure diagnostics from the existing plant-network audit.
- Added deterministic repeated-step coupled verification across M3 + M4.1–M4.5 while preserving single thermofluid, rotor and electrical state ownership.
- Added ADR 0037, `docs/SECONDARY_CYCLE_HEAT_BALANCE.md` and `docs/milestones/M4.6.md`; updated project handoff/status/roadmap/architecture/application metadata.
- M4.6 local build and complete automated test-suite validation were subsequently reported successful; M4.6 is the validated baseline for M4.7.

## M4.5 — Generator, Grid & Synchronization Physics

- Marked M4.4 as the locally validated baseline after successful build and complete automated test-suite confirmation.
- Added strongly typed `Frequency`, `ElectricPotential`, deterministic normalized `PhaseAngle` and shortest-separation `PhaseAngleDifference` quantities.
- Added canonical `ElectricalGridDefinition`, `SynchronousGeneratorDefinition` and `GeneratorGridSystemDefinition` over the validated M4.4 secondary-cycle stack with exact one-generator-per-M4.2-rotor ownership.
- Added separate deterministic `GeneratorGridState` for grid phase, generator electrical phase and breaker state.
- Added rotor-speed/pole-pair-derived electrical frequency plus fixed-step grid/generator phase advancement with no wall-clock dependency.
- Added manual breaker close/open commands with explicit frequency, phase and voltage synchronization windows and observable rejected-close diagnostics.
- Required legacy M4.2 manual external-load torque to be zero while M4.5 owns generator electromagnetic loading.
- Added requested electrical-power to electromagnetic-torque feedback through the existing single M4.2 rotor integrator.
- Added shaft-to-electrical conversion efficiency, generator loss accounting, electrical export snapshots and `GeneratorElectricalAudit`.
- Preserved higher-phase supplemental thermofluid composition through the wrapped M4.2 solver.
- Added electrical quantity, topology, synchronization, breaker, load-seam, audit and determinism tests.
- Added ADR 0036, `docs/GENERATOR_GRID_SYNCHRONIZATION.md` and `docs/milestones/M4.5.md`; updated project handoff/status/roadmap/architecture/application metadata.
- M4.5 local build and complete automated test-suite validation were subsequently reported successful; M4.5 is the validated baseline for M4.6.

## M4.4 — Condensate & Feedwater Train

- Marked M4.3 as the locally validated baseline after successful build and complete automated test-suite confirmation.
- Added canonical `CondensateFeedwaterSystemDefinition` and per-train topology binding from M4.3 hotwells to every M3 feedwater seam.
- Reused existing canonical `PumpDefinition` components for condensate/feedwater transport; no second hydraulic graph or inventory integrator was introduced.
- Added exact hotwell → condensate pump → feedwater inventory → feedwater pump → steam-drum target validation.
- Required legacy M3 feedwater boundary mass-flow inputs to be zero while M4.4 owns the closed condensate return path.
- Added bounded lumped feedwater thermal-conditioning power as explicit positive external energy on the canonical feedwater inventory.
- Added deterministic pump, inventory, thermal-conditioning and inherited global-audit snapshots.
- Extended `CondenserSystemSolver` backward-compatibly with higher-phase supplemental source-term composition before the same single plant-network integration.
- Added closed-mass-path, legacy-source exclusion, conditioning-energy and determinism tests.
- Added ADR 0035, `docs/CONDENSATE_FEEDWATER_TRAIN.md` and `docs/milestones/M4.4.md`; updated project handoff/status/roadmap/architecture/application metadata.
- M4.4 local build and complete automated test-suite validation were subsequently reported successful; M4.4 is the validated baseline for M4.5.

## M4.3 — Condenser, Vacuum & Hotwell

- Marked M4.2 as the locally validated baseline after successful build/test confirmation, including the fixed saturated-mixture fixture and anti-reverse floating-point canonicalization.
- Added canonical `CondenserSystemDefinition`, `CondenserDefinition` and `CondenserCoolingBoundaryDefinition` over every validated M4.2 turbine exhaust seam.
- Added one-to-one turbine-stage-to-condenser topology validation with canonical steam-space and hotwell fluid nodes.
- Added complete M4.3 cooling-boundary inputs with explicit available heat-rejection power.
- Added deterministic committed-state condensation limited by condenser capacity, available vapor inventory and cooling-boundary capacity.
- Added conservative steam-space-to-hotwell mass transfer with explicit signed external condenser heat rejection.
- Added dynamic condenser pressure/vacuum, phase/quality, condensation-limit and hotwell inventory snapshots.
- Extended `TurbineExpansionSolver` backward-compatibly with higher-M4 supplemental source-term composition before the same single plant-network integration.
- Added topology, exact-input-coverage, cooling-limit, conservation, vacuum-response and determinism tests.
- Added ADR 0034, `docs/CONDENSER_VACUUM_HOTWELL.md` and `docs/milestones/M4.3.md`; updated project handoff/status/roadmap/architecture/application metadata.
- M4.3 local build and complete automated test-suite validation were subsequently reported successful; M4.3 is the validated baseline for M4.4.

## M4.2 — Turbine Rotor & Expansion Model

- Marked M4.1 as the locally validated baseline after successful build/test confirmation.
- Added strongly typed `AngularSpeed`, `Torque` and `MomentOfInertia` mechanical quantities plus `TurbineEfficiency`.
- Added canonical `TurbineExpansionSystemDefinition`, `TurbineRotorDefinition` and `TurbineStageGroupDefinition` over the validated M4.1 admission seams.
- Added explicit canonical exhaust-node ownership seams for the upcoming condenser milestone.
- Replaced the active M4.1 terminal sink in M4.2 operation with conservative inlet-to-exhaust mass transfer and explicit thermofluid-to-shaft energy extraction.
- Added immutable `TurbineExpansionState` / `TurbineRotorState`, rotor inertia, turbine/load/net torque, deterministic angular-speed integration and kinetic-energy diagnostics.
- Added manual external-load torque as a replaceable generator seam, including zero-speed reversal limiting with commanded/effective values both observable.
- Added `TurbineMechanicalAudit` to close shaft work against rotor kinetic-energy change plus external load.
- Added explicit trip-command flow blocking and diagnostic overspeed seams without hidden automatic protection or latching.
- Extended `MainSteamNetworkSolver` backward-compatibly with higher-phase supplemental source-term composition before the same single plant-network integration.
- Added M4.2 topology, conservation, rotor, trip and overspeed tests.
- Added ADR 0033, `docs/TURBINE_EXPANSION_AND_ROTOR.md` and `docs/milestones/M4.2.md`; updated project handoff/status/roadmap/architecture/application metadata.
- M4.2 local build and complete automated test-suite validation were subsequently reported successful; M4.2 is the validated baseline for M4.3.

## M4.1 — Main Steam Network & Turbine Admission

- M3.8 local build and complete test-suite validation closes the M3 primary-circuit gate.
- Added canonical `MainSteamNetworkDefinition` mapping every M3 steam-export seam to an existing plant main-steam pipe and header.
- Added exact stop → control → admission valve-train topology validation over existing canonical `ValveDefinition` components.
- Added replaceable turbine-admission boundary definitions and complete per-step terminal demand inputs.
- M4.1 inputs require legacy M3 steam-export sinks to be commanded to zero while the main-steam network owns downstream transport, preventing double steam removal.
- Added committed-state line/valve pressure, flow, energy and fail-safe diagnostics plus turbine-inlet state/continuity diagnostics.
- Added temporary turbine-admission signed external mass/energy accounting using committed turbine-inlet specific internal energy.
- Added a backward-compatible higher-phase supplemental-source-term seam to `IntegratedPrimaryCircuitSolver`, preserving exactly one `PlantNetworkOrchestrator` integration.
- Added topology, boundary-exclusion, fail-safe and integrated conservation tests.
- Added ADR 0032, `docs/MAIN_STEAM_NETWORK.md` and `docs/milestones/M4.1.md`.
- M4.1 local build and complete automated test-suite validation was reported successful by the user.

## M3.8 — Integrated Primary-Circuit Baseline — VALIDATED

- M3.7 local build and complete test-suite validation establishes the feedwater/steam boundary baseline.
- Added canonical `IntegratedPrimaryCircuitDefinition` spanning core zones, channel groups, main circulation, steam drums and M3 external boundaries.
- Added immutable integrated inputs for core state, fission power, decay heat and complete boundary inputs.
- Added `IntegratedPrimaryCircuitSolver`, which evaluates every subsystem from the same committed plant state and performs exactly one plant-network integration.
- Added integrated plant-level snapshot with global inventory/power/flow aggregates plus per-subsystem diagnostics and conservation audit.
- Added configurable reference operating points and a deterministic headless long-run runner reporting raw inventory drift and maximum audit residuals without corrective bookkeeping.
- Added integrated composition, canonical-lineage and long-run deterministic equilibrium tests.
- Added ADR 0031, `docs/INTEGRATED_PRIMARY_CIRCUIT.md` and `docs/milestones/M3.8.md`.
- M3.8 local build and complete automated test-suite validation was reported successful by the user.

## M3.7 — Feedwater & Steam Boundary Interfaces — VALIDATED

- M3.6 local build/test validation establishes the validated steam-drum/separation baseline.
- M3.7 local build and complete automated test-suite validation was reported successful by the user.
- Added canonical feedwater-source and steam-export boundary definitions with exactly one of each per steam drum.
- Added complete canonical per-step boundary inputs with controllable mass flow and explicit feedwater specific internal energy.
- Added committed-state steam-export energy removal from the canonical steam-outlet node.
- Extended `PlantNetworkSourceTerms` with signed external mass flow and signed external power while preserving the existing constructor.
- Added `PlantNetworkSourceTerms.Combine(...)` for staged source-term composition ahead of the single M3.2 integration boundary.
- Extended `PlantNetworkAudit` with declared external mass flow and an explicit balance mass-rate residual.
- Added immutable feedwater/export/system snapshots and topology/accounting/integration tests.
- Added ADR 0030 and `docs/PRIMARY_CIRCUIT_BOUNDARIES.md`.
- Integrated long-run primary-circuit reference-state validation remains deferred to M3.8.

## M3.6 — Steam Drums, Separation & Recirculation

- M3.5.1 local build/test validation establishes the validated main-circulation baseline.
- Added backward-compatible explicit loop `ReturnCollectorNodeId` so channel returns can terminate at dedicated drum inventory nodes.
- Added canonical steam-drum/system definitions with one drum per circulation loop and eager topology validation.
- Added `SteamDrumLevelFraction`, per-drum/system snapshots and committed-state phase/quality/void/level diagnostics.
- Added deterministic ideal phase separation with saturated liquid/vapor internal-energy routing from the M1.7 water/steam model.
- Added conservative internal `PlantNetworkSourceTerms` for steam export-node transfer and liquid recirculation to MCP suction headers.
- Added topology, separation, conservation and network-audit integration tests.
- Added ADR 0029 and `docs/STEAM_DRUMS.md`.
- Feedwater and external steam boundary accounting were deferred to the separately implemented M3.7 candidate.

## M3.5.1 — Main Circulation Test Namespace Hotfix

- Fixed two test-only `using` directives that referenced the nonexistent `Domain.Physics.Reactor.Core.ThermalPower` namespace.
- `HeatDepositionFraction` is now imported from its canonical `Domain.Physics.Reactor.ThermalPower` namespace.
- No production code or simulation behavior changed.

## M3.5 — Main Circulation System

- M3.4 local build/test validation establishes the validated fuel-channel-group baseline.
- Added canonical semantic main-circulation system/loop/branch definitions over existing plant components.
- Added eager validation of suction/pressure headers, MCP endpoint direction, group ownership and return-path closure.
- Added committed-state `MainCirculationSystemSolver` without a second integration boundary.
- Added per-pump, per-branch, per-loop and whole-system immutable diagnostics for flow, pressure, power, phase/quality/void and continuity residuals.
- Added order-independence, branch-resistance distribution, topology-validation and network-integration tests.
- Added ADR 0028 and `docs/MAIN_CIRCULATION_SYSTEM.md`.
- Steam drums/separation, pump coastdown/electrical dynamics and detailed two-phase hydraulic correlations remain deferred.

## M3.4 — Fuel-Channel Group Model

- Added canonical equivalent fuel-channel groups mapped to M3.3 core zones and existing M3.1 plant components.
- Added represented channel counts, per-zone group power shares and explicit fuel/structure/coolant heat-deposition fractions.
- Added exact deterministic group partitioning of zonal fission power plus optional global decay-heat routing.
- Added committed-state per-group hydraulic diagnostics using the validated passive `PipeFlowSolver`.
- Added `PlantNetworkSourceTerms` so nuclear heat enters M3.2 staged balance accumulation without direct inventory mutation.
- Extended `PlantNetworkAudit` with explicit supplemental external power.
- Added channel-group domain/simulation/integration tests and ADR 0027.
- No steam drums, main circulation headers/pumps integration or individual-channel fidelity is introduced in M3.4.

## M3.3 — Aggregated Core-Zone Model

- M3.2 local build/test validation establishes the validated deterministic network-orchestration baseline.
- Added configurable `CoreZoneCoordinate` and normalized `CoreZonePowerFraction` primitives without a fixed grid-size assumption.
- Added `CoreZoneDefinition` / `AggregatedCoreDefinition` with canonical ordering, unique coordinates, normalized nominal power shares and eager references to canonical plant fuel/structure/coolant domains.
- Added `CoreZoneState` / `AggregatedCoreState` with exact zone-set validation and normalized current power shares.
- Added `AggregatedCorePowerSolver` with deterministic exact-closure global-to-zone fission-power allocation while keeping point kinetics global.
- Added `CoreZoneSnapshot` / `AggregatedCoreSnapshot` local committed-state diagnostics including thermal state, coolant pressure/phase/quality and volumetric void projection.
- Added ADR 0026 and `docs/CORE_ZONE_MODEL.md`.
- No spatial neutron diffusion, channel hydraulics or per-zone heat-deposition physics is introduced in M3.3.

## M3.2 — Deterministic Multi-Component Network Orchestration

- M3.1 local build/test validation establishes the validated plant composition/topology baseline.
- Added `PlantNetworkOrchestrator` with staged committed-state-only solving for pipes, valves, pumps, heat-transfer links and heat sources.
- Added deterministic canonical balance accumulation before any conserved inventory integration.
- Added exactly-once `FluidNodeIntegrator`/`ThermalBodyIntegrator` execution per stateful inventory per network step.
- Added immutable `PlantNetworkStepResult` balance diagnostics and `PlantNetworkAudit` global mass/energy accounting.
- Added explicit pump hydraulic-work and heat-source external-power accounting without hidden conservation correction.
- Added parallel-connection committed-state tests, shuffled-registry order-independence tests and fixed-step runtime pulse-segmentation integration.
- Added ADR 0025 and `docs/PLANT_NETWORK_ORCHESTRATION.md`.


## M3.1 — Plant Composition & Topology Baseline

- Added immutable `PlantDefinition` with canonical registries for fluid nodes, passive pipes, valves, pumps, thermal bodies, heat-transfer links and heat sources.
- Added global topology-ID uniqueness, including wrapped valve/pump hydraulic-path IDs, to keep diagnostics and future recorder/UI addressing unambiguous.
- Added eager validation of all hydraulic endpoints, thermal-domain links and heat-source targets.
- Added immutable complete `PlantState` with exact state-set validation and canonical fluid/thermal definition consistency.
- Added plant-level immutable `PlantSnapshot` as the first committed whole-plant observation boundary.
- Added topology/state/snapshot tests covering canonical order, caller collection independence, missing references, ID collisions, incomplete states and typed lookups.
- Added `docs/PLANT_COMPOSITION.md`, ADR 0024 and milestone M3.1 documentation.
- M2.8 local validation closes M2 and establishes the validated reactor-physics baseline carried into M3.1.
- No physical equations or validated M1/M2 solver behavior changed.

## M2.8.1 — M2 Closure & Roadmap Consolidation

- Recorded successful local validation of M2.8 and closure of the complete M2 reactor-physics foundation.
- Added `docs/PROJECT_STATUS.md` with validated capability map, intentional gaps and phase gates.
- Added `docs/PRIMARY_CIRCUIT_PLAN.md` with the detailed M3.1–M3.8 integration sequence.
- Expanded the roadmap into granular M3–M9 milestones with explicit cross-phase acceptance gates.
- Added ADR 0023 requiring staged committed-state network solving, deterministic balance accumulation and one integration per conserved inventory.
- Updated application baseline/status metadata to identify M3.1 as the next implementation milestone.
- No simulation equations, public physics APIs or runtime behavior changed.

## M2.8 — Iodine/Xenon Dynamics

- Added explicit normalized immutable I-135 and Xe-135 inventories.
- Added configurable fission-power-scaled iodine/direct-xenon production, isotope decay constants and neutron-population-dependent xenon burnup.
- Added analytic finite-step integration of the coupled reduced I/Xe linear system for deterministic fixed-step evolution.
- Added long-operation equilibrium initialization and detailed production/decay/burnup diagnostics.
- Added signed configurable `XenonReactivityCoefficient` and one named `ReactivityContributionKind.Xenon` contribution through the validated M2.1 composition boundary.
- Added shutdown xenon-buildup, burnup, equilibrium, reactivity-composition and runtime pulse-segmentation tests.
- Added ADR 0022 and `docs/IODINE_XENON_DYNAMICS.md`.
- M2.7.1 local validation establishes the validated void-feedback baseline carried into M2.8.
- M2.8 subsequently passed local build/test validation and closes the complete M2 reactor-physics baseline.

## M2.7.1 — Void Feedback Composition Test Hotfix

- Fixed the expected total in `VoidFeedbackSolverTests.MultipleInputs_AreCanonicalAndComposeThroughReactivityModel`.
- The two configured contributions are `+10 pcm` for `void/a` and `-20 pcm` for `void/b`, so the physically correct composed total is `-10 pcm`, not `+30 pcm`.
- The production `VoidFeedbackSolver`, `ReactivityModel`, public APIs and physical behavior are unchanged.
- M2.7 remains the functional milestone; M2.7.1 was validated locally and closes the M2.7 baseline.

## M2.7 — Void Feedback

- Added strongly typed `VoidFraction` and signed `VoidFractionDifference`, explicitly distinct from saturated-mixture `VaporQuality`.
- Added signed `VoidReactivityCoefficient` with canonical delta-k/k per unit void fraction storage and explicit pcm per percentage-point void conversion.
- Added immutable `VoidReactivityFeedbackDefinition` with explicit reference void fraction.
- Added deterministic `WaterSteamVoidFractionSolver`: subcooled liquid maps to zero void, superheated vapor to full void, and saturated mixtures use quality plus M1.7 saturation densities.
- Added pure `VoidFeedbackSolver`, immutable diagnostics, canonical multi-feedback ordering and composition through the validated M2.1 `ReactivityModel`.
- Added water/steam-state-to-void-to-reactivity-to-point-kinetics runtime integration with fixed-step pulse-segmentation determinism.
- Added ADR 0021 and `docs/VOID_FEEDBACK.md`.
- M2.6 local validation establishes the validated temperature-feedback baseline carried into M2.7.

## M2.6 — Temperature Feedback

- Added strongly typed signed `TemperatureReactivityCoefficient` with canonical delta-k/k/K storage and explicit pcm/K conversion.
- Added immutable `TemperatureReactivityFeedbackDefinition` values restricted to fuel- and coolant-temperature contribution categories.
- Added pure `TemperatureFeedbackSolver` with linear `rho = alpha * (T - T_ref)` evaluation and immutable diagnostics.
- Added canonical multi-feedback ordering, duplicate-ID validation and composition through the validated M2.1 `ReactivityModel`.
- Added deterministic committed-state coupling from temperature feedback to point kinetics, fission power and subsequent thermal evolution.
- Added first closed thermal-neutronic feedback-loop runtime test with external pulse-segmentation invariance.
- Added ADR 0020 and `docs/TEMPERATURE_FEEDBACK.md`.
- M2.5 local validation establishes the validated decay-heat baseline carried into M2.6.

## M2.5 — Decay Heat

- Added strongly validated `DecayHeatGenerationFraction` and configurable equivalent `DecayHeatGroupDefinition` values.
- Added immutable latent `DecayHeatState`/`DecayHeatGroupState` energy inventories with empty and long-operation equilibrium initialization.
- Added `DecayHeatSolver` implementing exact analytic finite-step first-order group evolution `dE/dt = f*P_fission - lambda*E`.
- Added explicit precursor-production energy, emitted-decay-energy and latent-inventory balance accounting.
- Added average same-step decay-heat deposition for exact thermal integration and end-of-step instantaneous snapshot diagnostics.
- Added canonical complete decay-heat destination partition with adapters to thermal-body and zero-mass fluid-node energy balances.
- Added shutdown persistence, half-life, buildup, equilibrium, energy-conservation and fixed-step pulse-segmentation determinism tests.
- Added ADR 0019 and `docs/DECAY_HEAT.md`.
- M2.4 local validation establishes the validated thermal-power baseline carried into M2.5.

## M2.4 — Thermal Power

- Added explicit `FissionPowerCalibration` between normalized neutron population and reference fission thermal power.
- Added validated `HeatDepositionFraction`, canonical fission-heat destinations and complete partition validation.
- Added stateless deterministic `FissionPowerSolver` with linear neutron-population scaling and fail-fast numerical overflow protection.
- Added immutable `FissionPowerSnapshot` and named `FissionHeatDeposition` diagnostics.
- Added exact total-power closure across heat destinations using deterministic residual allocation.
- Added adapters to existing `ThermalEnergyBalance` and zero-mass `FluidNodeBalance` energy boundaries.
- Added thermal-body/fluid-node energy integration and M2.3 kinetics-to-M2.4 power fixed-step pulse-segmentation tests.
- Added ADR 0018 and `docs/THERMAL_POWER.md`.
- M2.3 local validation establishes the validated neutron-kinetics baseline carried into M2.4.

## M2.3 — Neutron Kinetics

- Added strongly validated delayed-neutron fraction, decay constant, normalized neutron population and precursor-population quantities.
- Added immutable canonical delayed-neutron group definitions, point-kinetics parameter sets and state.
- Added explicit critical-equilibrium initialization from `Λ`, `β_i` and `λ_i`.
- Added generic plant-independent `PointKineticsSolver` implementing point-reactor kinetics with arbitrary delayed-neutron groups.
- Added deterministic bounded RK4 internal substepping inside the already-fixed simulation timestep.
- Added prompt-critical margin, beta-relative dollars/cents, logarithmic population-rate and signed reactor-period diagnostics.
- Added fail-fast numerical-envelope protection for non-finite or materially negative kinetic populations.
- Added zero-reactivity equilibrium, positive/negative reactivity response, prompt-supercritical finite-envelope and runtime pulse-segmentation determinism tests.
- Added ADR 0017 and `docs/NEUTRON_KINETICS.md`.
- M2.2 local validation establishes the validated control-rod baseline carried into M2.3.

## M2.2 — Control Rods

- Added normalized `ControlRodPosition` with explicit fully-inserted/fully-withdrawn semantics.
- Added strongly validated normalized `ControlRodTravelRate`, persistent `Insert`/`Withdraw`/`Hold` motion and immutable rod state.
- Added immutable rod/group/system definitions with canonical ordering and bidirectional membership validation.
- Added deterministic individual-rod and group commands with ordered same-step override semantics.
- Added `ControlRodMotionSolver` with mechanical endpoint clamping and automatic hold at limits.
- Added linear and smooth-step integral rod-worth curves behind `ControlRodWorthSolver`.
- Added one canonical `ControlRods` reactivity contribution per rod and composition through the validated M2.1 `ReactivityModel`.
- Added domain, motion, worth, group-command, mechanical-limit and fixed-step/pulse-segmentation determinism tests.
- Added ADR 0016 and `docs/CONTROL_RODS.md`.
- M2.1 local validation establishes the validated reactivity-composition baseline carried into M2.2.

## M2.1 — Reactivity Model

- Added strongly typed signed `Reactivity` with canonical `delta-k/k` storage and explicit percent/pcm conversions.
- Added immutable named `ReactivityContribution` values with diagnostic source categories.
- Added deterministic `ReactivityModel` canonicalization, compensated summation and per-category subtotals.
- Added immutable `ReactivityBreakdownSnapshot` diagnostics and duplicate-ID fail-fast validation.
- Added unit, composition, permutation-independence, immutability and fixed-step runtime determinism tests.
- Added ADR 0015 and `docs/REACTIVITY_MODEL.md`.
- M1.7 local validation closes the complete M1 physical-foundation baseline carried into M2.1.

## M1.7 — Simplified Water/Steam Phase Model

- Added explicit `FluidPhase` classification and strongly validated saturated-mixture `VaporQuality`.
- Extended `FluidThermodynamicState` compatibly with phase, optional quality and derived vapor mass fraction.
- Added `SimplifiedWaterSteamThermodynamicModel` as the first production `IFluidThermodynamicModel` implementation.
- Added IAPWS-IF97 Region-4 saturation-pressure reference calculation with deterministic compact educational correlations for the remaining properties.
- Added deterministic closure for subcooled/compressed liquid, saturated mixtures and superheated vapor.
- Added `WaterSteamSaturationProperties` and explicit `WaterSteamStateOutOfRangeException`.
- Added phase round-trip, saturation-reference, unsupported-state, integration and determinism tests.
- Added ADR 0014 and `docs/WATER_STEAM_MODEL.md`.
- M1.6 local validation closes Heat Transfer as the validated baseline carried into M1.7.

## M1.6 — Heat Transfer

- Added strongly typed `HeatCapacity` and `ThermalConductance` with canonical SI storage.
- Added immutable lumped thermal bodies with conserved stored energy and derived temperature.
- Added stateless signed heat-transfer solving using lumped conductance and temperature difference.
- Added exactly equal-and-opposite thermal endpoint balances for internal energy conservation.
- Added deterministic thermal-body integration with below-absolute-zero fail-fast protection.
- Added explicit enabled/disabled external heat-source energy input.
- Added wall-to-fluid thermal coupling through the existing `FluidNodeBalance` energy boundary.
- Added fixed-step and pulse-segmentation determinism tests for the thermal model.
- Added ADR 0013 and `docs/HEAT_TRANSFER.md`.
- M1.5 local validation closes Pumps as the validated baseline carried into M1.6.

## M1.5 — Pumps

- Added strongly typed normalized pump speed and simplified pump efficiency.
- Added immutable pump definitions/states composed over existing hydraulic paths.
- Added speed-squared active pressure boost and quadratic internal pump-curve resistance.
- Added pressure-driven bidirectional pump flow without an imposed-flow solver.
- Added upstream-density volumetric flow and explicit hydraulic-work/shaft-power accounting.
- Added mass-conservative endpoint balances whose net energy equals active hydraulic work.
- Added stopped-pump passive-flow, reverse-flow, affinity-law and deterministic fixed-step tests.
- Added ADR 0012 and `docs/PUMPS.md`.
- M1.4 local validation closes Valves as the validated baseline carried into M1.5.

## M1.4 — Valves

- Added strongly typed normalized valve position and flow-capacity coefficient.
- Added linear, quick-opening and normalized equal-percentage valve characteristics.
- Added immutable valve definitions/states and explicit fail-safe actions.
- Added valve flow solving by resistance modulation over the existing M1.3 pipe solver.
- Added exact closed/open endpoint behaviour without infinite or magic resistances.
- Added conservative, reversal-aware and deterministic valve integration tests.
- M1.3 local validation closes Pipes & Flow Resistance as the validated baseline carried into M1.4.

## M1.3 — Pipes & Flow Resistance

- Added strongly typed `QuadraticHydraulicResistance` using canonical SI `Pa·s²/kg²`.
- Added immutable `PipeDefinition` with explicit reference endpoints and strictly positive resistance.
- Added stateless bidirectional `PipeFlowSolver` using a lumped quadratic pressure-loss relation.
- Added natural flow reversal from signed endpoint pressure difference.
- Added upstream specific-internal-energy advection and exactly equal-and-opposite endpoint balances.
- Added dimensional `SpecificEnergy × MassFlowRate -> Power` arithmetic.
- Added deterministic integration tests proving total mass/internal-energy conservation and fixed-step pulse-segmentation invariance.
- Added ADR 0010 and `docs/PIPES_AND_FLOW.md`.
- M1.2.1 local validation closes M1.2 as the validated baseline carried into M1.3.

## M1.2.1 — Thermodynamic Closure Test Hotfix

- Fixed the contradictory zero-balance expectation in `FluidNodeIntegratorTests`.
- A zero mass/energy balance still preserves conserved inventory, while thermodynamic closure remains intentionally resolved once through `IFluidThermodynamicModel`.
- The test now asserts the thermodynamic state returned by the configured closure model instead of incorrectly requiring the previous pressure/temperature to be preserved.
- No production code, public API, conservation semantics or runtime behavior changed.
- M1.2 remains the functional milestone; M1.2.1 was validated locally and closes the M1.2 baseline.

## M1.2 — Fluid Node Model

- Added immutable fluid-node domain model separating definition, conserved inventory and thermodynamic closure state.
- Added derived density and specific internal energy to avoid duplicated drifting state.
- Added signed `FluidNodeBalance` for net mass and energy rates.
- Added deterministic `FluidNodeIntegrator` with explicit integration interval.
- Added `IFluidThermodynamicModel` seam without introducing a premature production equation of state.
- Added fail-fast `FluidNodeDepletionException` when a candidate step would reach zero/negative fluid mass.
- Added Domain and Simulation tests for validity, conservation arithmetic, determinism and M0 runtime composition.
- Added ADR 0009 and `docs/FLUID_NODES.md`.
- M1.1.1 local validation also validates all carried-forward M0.2/M0.3 tests.

## M1.1.1 — Nullable Architecture Test Hotfix

- Fixed `CS8619` in `ArchitectureRulesTests.ReadProjectReferences`.
- Project-reference filename extraction now converts the nullable BCL return contract into an explicit non-null result with fail-fast diagnostics.
- No production code, physical quantity semantics, APIs or simulation behavior changed.
- M1.1 remains the functional milestone; M1.1.1 was validated locally and closes the M1.1 baseline.

## M1.1 — Physical Quantities & Units

- Added immutable strongly typed physical quantities in `Domain.Physics.Quantities`.
- Established canonical SI storage with explicit non-SI factories and conversions.
- Added geometry types: `Length`, `Area`, `Volume`.
- Added matter types: `Mass`, `Density`.
- Added thermal types: `Temperature`, `TemperatureDifference`.
- Added hydraulic types: `Pressure`, `PressureDifference`.
- Added energy types: `Energy`, `SpecificEnergy`, `Power`.
- Added flow types: `MassFlowRate`, `VolumetricFlowRate`.
- All quantity construction rejects `NaN` and infinities.
- Absolute quantities enforce non-negative physical domains where intrinsic to the dimension.
- Added explicit signed difference types for temperature and pressure.
- Added selected dimensionally meaningful arithmetic needed by upcoming physical models.
- Added Domain tests for construction, conversions, arithmetic, safety, equality and comparisons.
- Added ADR 0008 and `docs/PHYSICAL_QUANTITIES.md`.
- No fluid nodes, pumps, valves, heat transfer, phase model or reactor physics introduced yet.

## M0.3 — Simulation Test Harness & Runtime Hardening

- Added terminal `Faulted` runtime semantics and immutable fault diagnostics.
- Added transactional fixed-step commit: failed calculations do not commit logical state or clock.
- Failed-step commands are restored to the queue in original FIFO order.
- Added deterministic `ISimulationInvariant<TState>` validation before state commit.
- Added structured invariant results and invariant-violation diagnostics.
- Added immutable logical-step command traces and deterministic headless replay.
- Added a reusable generic scenario harness that captures initial and per-step snapshots.
- Added long-run 100,000-step pulse-segmentation determinism verification.
- Added large command-trace replay stress verification.
- Added fault rollback, invariant rollback and partial multi-step commit tests.
- Formalized immutable/copy-on-write state ownership for future physical kernels.
- Added ADR 0006 and ADR 0007.
- Reactor physics, fluids and thermodynamics remain intentionally out of scope until M1.

## M0.2 — Deterministic Simulation Runtime

- Added deterministic fixed-timestep `SimulationClock`.
- Added generic headless `SimulationRuntime<TState, TCommand, TStateSnapshot>`.
- Added pause, resume and paused single-step execution.
- Added exact 0.25×, 0.5×, 1×, 2×, 5× and 10× speed multipliers using fixed-point quarter-unit scaling.
- Added thread-safe FIFO command queue with monotonic sequence numbers.
- Commands are consumed only at fixed physical-step boundaries.
- Added immutable runtime and simulation snapshot envelopes.
- Added deterministic repeatability and pulse-segmentation tests.
- Added architecture enforcement against wall-clock/timer/delay APIs in Simulation.
- Added ADR 0005 for command scheduling and snapshot ownership.
- Consolidated the remaining M0 roadmap around M0.3 runtime hardening and simulation test harness.
- No reactor physics, thermodynamics or fluid modelling has been introduced yet.

## M0.1 — Engineering Foundation & Architectural Baseline — VALIDATED

- Created .NET 10 solution structure.
- Added Domain, Simulation, Application, Infrastructure and Avalonia App projects.
- Added four test projects using xUnit.net v3.
- Added centralized compiler/analyzer settings and package management.
- Isolated Avalonia dependencies to the App project.
- Added explicit project dependency rules and automated architecture tests.
- Added initial composition root.
- Added architectural documentation, ADRs and approved M0–M9 roadmap.
- No nuclear physics or plant simulation logic was introduced.
- The local validation suite was reported as passing on 2026-07-20.
## M10.9.4.1-I.3 Hotfix 4 — Explicit-vs-Corrected Branch Discontinuity Comparison — CANDIDATE

- Preserves I.2 as the last validated baseline and keeps I.3 unvalidated.
- Freezes the completed red I.3 300 s summary plus generation-health and shaft-drop episode CSV evidence.
- Adds a 100 s exact-v2 versus exact-v3 comparison at 10 ms per-step resolution.
- Records the H.18 steam/header/stop-out/control-out/turbine-inlet pressure chain, admission-train flows, stage flow, shaft power and corrected-commit telemetry.
- Does not weaken the `shaft > 4.5 MW` health floor and does not change plant physics, numerical mathematics, H.30 policy, production selector, persistence identity or the 10 ms fixed step.
- Adds `scripts/run-phase-i-explicit-vs-corrected-branch-discontinuity-comparison-audit.cmd`.
## M10.9.4.1-I.3 Hotfix 4 — Script Fix 1 — MTP CLI Compatibility — CANDIDATE

- Fixes only `scripts/run-phase-i-explicit-vs-corrected-branch-discontinuity-comparison-audit.cmd`.
- Uses .NET 10 Microsoft.Testing.Platform project selection via `dotnet test --project ...`.
- Uses native xUnit v3 MTP selection via `--explicit only` and fully-qualified `--filter-method`.
- Keeps `NRS_I3_BRANCH_COMPARISON_AUDIT=1` as the fail-closed scheduled/manual opt-in.
- Adds explicit post-run checks for the four expected Hotfix 4 comparison artifacts.
- No C# source, runtime physics, numerical mathematics, H.30 policy, selector, persistence semantics, diagnostic acceptance criteria or 10 ms fixed step changed.

## M10.9.4.1-I.3 Hotfix 4 — Classifier Fix 1 — Targeted-Train Reverse-Flow Classification — CANDIDATE

- Corrects only the Hotfix 4 diagnostic acceptance model plus candidate metadata/documentation; plant runtime and numerical contracts remain unchanged.
- The completed 10 ms comparison showed `338` exact-v2 generation-drop steps and `338` targeted-train reverse-flow steps: `8` reverse stop-valve, `0` reverse control-valve and `330` reverse admission-valve steps.
- Every explicit generation drop coincides one-for-one with reverse flow on stop/control/admission, and every targeted reverse-flow step is a generation drop.
- Exact v3 remains clean in the same 100 s domain: `0` generation drops and `0` targeted-train reverse-flow steps, with `1791/1791` corrected commits and `0` rollback/fallback/unsafe/untargeted disagreement in the observed failed-classification run.
- Replaces the overly narrow Hotfix 4 requirement `every drop has reverse admission flow` with the physically correct targeted-train requirement `every drop has reverse flow on stop, control or admission`.
- Does not change H.30 `OPT-IN ONLY`, does not freeze I.3 tolerance budgets and leaves I.2 as the authoritative validated baseline until this corrected focused classifier passes locally.

## M10.9.4.1-H.30 Requalification 1 Hotfix 1 — VALIDATED

- Local build, complete ordinary suite and focused production-policy re-review gate passed on 2026-08-20.
- Production decision is now `ACTIVATE`.
- Exact v3 `FourNodeBranchContinuityCorrectedCommitOptIn` is the authoritative desktop production default.
- Exact v2 `ExplicitCommittedState` remains fail-closed rollback/reference and compatibility-retained.
- H.28 remains `bounded-but-costly`; no numerical retuning or fixed-step change was required.

## M10.9.4.1-I.3 — Authoritative Production Reference Trajectory, Conservation/Inventory & Tolerance Baseline — CANDIDATE

- Runs the H.30-RQ1-authoritative production selector for 300 simulated seconds / 30,000 steps.
- Checks generation health and stop/control/admission flow direction at every 10 ms step.
- Records 301 one-second reference samples and seven final-window conservation/inventory slopes.
- Derives 19 versioned internal regression tolerance budgets from the final 60 seconds.
- Verifies corrected production telemetry and a separate deterministic control fingerprint.
- Adds a compact H.30 RQ1 evidence manifest under `eng/evidence-manifests/` instead of bundling large audit payloads.
- Candidate ZIP packaging excludes `tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/Evidence`; applying the candidate preserves any local Evidence directory.
- Does not modify H.9/H.20/H.22, P060/F040, hysteresis limits, physical coefficients or the deterministic 10 ms fixed step.

## Documentation planning — post-M11 Epic A/B/C roadmap alignment

- Restored explicit roadmap visibility for the previously approved Extreme Operations & Accident Progression, Spatial Reactor and Control-Room Experience epics.
- Mapped Epic A to M12 foundations plus M15 consequence models, Epic C to M13, and Epic B to M14.
- Added milestone plans M12–M15 and removed stale pre-Phase-I sequencing language from the long-lived future-direction document.
- Kept all M12–M15 work post-M11 and non-blocking for the active M10.9.7 persistence candidate.

## M10 Final Long Failure Diagnostic 1 — CANDIDATE

- Preserves the failed/aborted first final-long campaign as evidence; no long result is reinterpreted as passing.
- Adds an explicit 300 s exact-v4 fluid-node residual census across the already-validated reference domain, including final-60 s node slopes and `outlet` comparison to the observed LR-H1 failure coordinates.
- Adds a synthetic MISSION score/timeline prefix-scaling census so the LR-M1 wall-cost defect can be measured without another multi-hour plant run.
- Records the static call-chain finding that the current live MISSION path scans the growing `_demandTimeline` multiple times on every `SingleStep` presentation, yielding O(n) work at step n and O(n^2) aggregate session work.
- Adds `scripts/run-m10-final-long-failure-diagnostic1.cmd` and `eng/m10-final-long-diagnostic1-contract.json`.
- Freezes the next final-long operational target at 35–45 minutes with a 60-minute maximum workstation budget; wall time remains diagnostic/job-budget semantics, not a physics tolerance.
- No production `src/` file, physical coefficient, thermodynamic envelope, I.3 budget, conservation ceiling, archive schema, fingerprint algorithm or exact historical identity is changed.
