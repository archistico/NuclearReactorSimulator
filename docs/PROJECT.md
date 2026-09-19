# Project — current authoritative state

## Hosted Exact-V9 runtime-alignment decision gate — 2026-09-19

<!-- NRS-MARKER:M10974-EXACT-V9-HOSTED-RUNTIME-ALIGNMENT-CHECKPOINT -->

Cross-host diagnostics 1-4 have reduced the remaining hosted Exact-V9 RED to a single transient IEEE-754 difference at step 126: 127/128 steps are bit-identical, selector/direct are identical on each host, the drift is one ULP at the shared STOP-out / CONTROL-in pressure node, and the trajectories reconverge bit-identically at step 127. The local frozen aggregate is produced on .NET 10.0.5; hosted RED evidence is produced on .NET 10.0.12.

No further ULP-by-ULP production diagnostic is authorized at this point. The next activity is the one-shot `Exact-V9 Hosted Runtime Alignment Diagnostic 1`: execute the unchanged authoritative Exact-V9 method on GitHub with SDK 10.0.105, runtime 10.0.5 and runtime roll-forward disabled. Permanent `global.json`, ordinary CI, source/test code, Fingerprint V1, Exact-V9 golden and VR2/R3 remain unchanged by the probe.

Decision is binary: if the hosted aligned-runtime probe reproduces `7880AD...B5418`, prepare the permanent ordinary-CI runtime pin; if it remains RED while the trace proves .NET 10.0.5, stop numerical seam diagnostics and move to a separately versioned cross-host numerical-canonicalization contract. R3 remains RED and frozen until ordinary CI is actually GREEN.

## Hosted M10.9.7.4 fingerprint-v1 cross-host diagnostic checkpoint — 2026-09-19

<!-- NRS-MARKER:M10974-FINGERPRINT-V1-CROSS-HOST-DIAGNOSTIC1 -->

Stable Contract V2.1.1 is locally GREEN and the hosted workflow now reaches the real ordinary suite. The remaining hosted RED is exactly `M10974FingerprintV1SchemaAnchorTests.FingerprintV1_PopulatedExactVersionFixtureMatchesFrozenGoldenHash`: frozen expected `63643e55...`, hosted actual `3e11375d...`. Every other test assembly is GREEN.

The next activity is test-only `M10.9.7.4 Fingerprint V1 Cross-Host Determinism Diagnostic 1`. It keeps the golden assertion and fingerprint-v1 implementation unchanged and captures the exact normalized JSON plus top-level/node hashes from the same local/hosted fixture execution. R3 remains RED; repair planning remains blocked.

## Hosted ordinary-ci V2.1.1 validator checkpoint — 2026-09-19

<!-- NRS-MARKER:CI-STABLE-CONTRACT-V2_1_1-CHECKPOINT -->

Stable Contract V2 reached the hosted full test suite and exposed the persistent single historical `Application.Tests` failure. V2.1 added single-execution hosted log capture, but its local static validator false-RED because a PowerShell single-quoted literal searched for doubled backslashes while the workflow correctly contained single Windows path separators. V2.1.1 preserves the V2.1 hosted behavior and replaces brittle full-line YAML `.Contains()` checks with ASCII markers, slash normalization, multiline structural regex checks, and exact-one hosted entry-point cardinality.

Next activity: validate V2.1.1 locally, then push the same candidate to capture the exact hosted failing test without rerunning or filtering the suite. R3 remains RED; `repair-owner=UNSELECTED`; repair planning remains blocked pending hosted ordinary-ci GREEN.

## Hosted ordinary-ci contract V2 recovery checkpoint — 2026-09-19

<!-- NRS-MARKER:CI-STABLE-CONTRACT-V2-CHECKPOINT -->

The hosted GitHub `ordinary-ci` workflow is currently RED **before restore/build/test**, because the permanent entry point still invokes the historical `GitHub Ordinary CI Deterministic Serialization Hotfix 1` validator. That validator freezes the Hotfix 1 `src/` and `tests/` snapshots and hashes raw working-tree bytes. Both assumptions are invalid for permanent CI: authorized later milestones have added test-only files, and Windows Git checkouts may materialize text with different EOL representation.

`GitHub Ordinary CI Stable Contract V2` replaces that permanent gate with a semantic harness contract: pinned SDK/test runner, warnings-as-errors, serialized full ordinary suite, no filter/retry/continue-on-error, and current-evidence execution remain fail-closed. It intentionally does not freeze evolving `src/`/`tests/` counts or raw tree hashes. Hotfix 1 remains frozen provenance only.

Current next activity: validate V2 locally with `./scripts/run-github-ordinary-ci-stable-contract-v2.cmd`, then push the same candidate and require hosted `ordinary-ci` GREEN. R3 remains RED; causal closure remains confirmed; `repair-owner=UNSELECTED`; production repair planning remains blocked until hosted GREEN.


## Causal-closure consolidation / hosted-CI hold checkpoint — 2026-09-19

<!-- NRS-MARKER:R3-CAUSAL-CLOSURE-CONSOLIDATION-CHECKPOINT -->

Diagnostic 3 REV1 is complete and its returned `01`-`07` evidence is independently adjudicated `CAUSAL-CLOSURE-CONFIRMED`. The causal seam is `STEAM-DRUM-LIQUID-TRANSPORT-VS-MODE2-SUCTION-TRANSPORT`; `repair-owner=UNSELECTED`; R3 remains RED.

A documentation/architecture inventory has now mapped the current ownership split without opening repair planning. The selected `IFluidThermodynamicModel` owns post-step conserved-inventory resolution, while `SteamDrumSeparationSolver` still constructs an independent default `SimplifiedWaterSteamThermodynamicModel` for saturation/phase-split/stream properties. Purely injecting a mode2 model into the drum would not by itself close the seam because the current public forward saturation provider is bit-identical between mode1 and mode2.

Hosted GitHub `ordinary-ci` remains `PENDING-CONFIRMATION`. The only next authorized activity is `CONFIRM-HOSTED-ORDINARY-CI-GREEN`. Production repair planning/implementation, seed retuning, threshold/envelope changes, C4/payload changes, canonical exact-v9 changes, R3 Requalification 3 and R4 remain blocked.

Architecture inventory: `M10_FINAL_VR2_R3_CAUSAL_CLOSURE_ARCHITECTURE_INVENTORY1.md`.

## Diagnostic 3 REV1 returned-evidence adjudication checkpoint - 2026-09-19

<!-- NRS-MARKER:DIAG3-REV1-RETURNED-ADJUDICATION-CHECKPOINT -->

`R3 Seed Integration Suction Energy-Transport Causal-Seam Diagnostic 3 REV1` completed and returned the full `01`-`07` artifact set. Independent adjudication confirms `CAUSAL-CLOSURE-CONFIRMED`: the production runtime reproduces the approximately `-5.2162718 MW` suction energy discontinuity with closed mass/energy bookkeeping, while the independent IAPWS-IF97 liquid-transport counterfactual differs from mode2 raw suction transport by only `0.00014754291623830795 J/kg` and leaves a net `0.014754295349121094 W`, inside the derived `0.10223668223103162 W` component budget. The causal seam is localized to `STEAM-DRUM-LIQUID-TRANSPORT-VS-MODE2-SUCTION-TRANSPORT`; `repair-owner=UNSELECTED`.

R3 remains RED. The deterministic ordinary CI gate is locally PASS, but hosted GitHub `ordinary-ci` is still `PENDING-CONFIRMATION` in the current evidence. The only next authorized activity is `CONFIRM-HOSTED-ORDINARY-CI-GREEN`. Production repair planning remains blocked until that hosted confirmation is returned GREEN.

## Diagnostic 3 REV1 preexecution amendment checkpoint — 2026-09-19

<!-- NRS-MARKER:DIAG3-REV1-AMENDMENT1-CHECKPOINT -->

The first REV1 execution attempt stopped before build because the validator depended on `Get-FileHash`, which is unavailable in the proven user PowerShell environment. Full preexecution review also found that the frozen `0.01 W` counterfactual-net guard was stricter than the already-frozen `0.001 J/kg` reference-alignment guard at approximately `100 kg/s`; frozen evidence predicts about `0.01475 W` from the drum/suction pressure-work difference alone. Planning Amendment 1 therefore replaces the cmdlet dependency with .NET SHA-256 and replaces the fixed net-rate ceiling with a derived component budget. Production, seeds, model thresholds, historical test semantics and R3/R4 authority remain unchanged.

<!-- NRS-MARKER:DIAG3-REV1-VALIDATOR-HOTFIX1-CHECKPOINT -->
**REV1 validator contract-hygiene hotfix:** the first post-amendment execution stopped before build because the active validator still contained redundant Markdown literal-presence checks after the marker contract. Hotfix 1 removes all active REV1 Markdown prose/file-name presence assertions, moves index/navigation presence to JSON-declared ASCII markers with exact-one cardinality, and leaves physics, guards, scenario and authority unchanged.


## Historical new-chat restart checkpoint — 2026-09-19 (superseded by causal-closure consolidation)

The text in this section is retained as historical provenance. Any wording below that says `active`, `next` or `authoritative` is superseded by the causal-closure checkpoint above and must not be used to select current work.

**Authoritative active engineering state:** M10 Final / VR2 / R3 remains **RED**. R1 and R2 are PASS. No new production repair, seed retuning, tolerance change, C4/payload change or canonical exact-v9 change is authorized.

**Latest completed diagnostic gate:** `R3 Seed Integration Two-Seed-Step Preconditioning Divergence Diagnostic 2 — Adjudicator Hotfix 1` is returned and accepted as `PASS-DIAGNOSTIC-EVIDENCE-COMPLETE`. The complete returned artifact set `01`–`09` is frozen and SHA-256 locked. The first phase divergence is `seed-step1` / `suction`: mode 1 remains `SubcooledLiquid`, mode 2 becomes `SaturatedMixture` at quality `1.5376981274668928E-07`. Governor/controller deltas are still zero at that first step.

**New causal observation from the frozen evidence:** candidate `suction` mass is unchanged across the first 10 ms step, but its conserved internal energy falls by about `52.163 kJ`, equivalent to `-5.21627 MW`. The existing steam-drum liquid source and MCP suction sink reconstruct this rate from an approximately `52.163 kJ/kg` selected-specific-energy difference at approximately `100 kg/s`, to numerical roundoff. Mode 1 remains essentially energy-neutral over the same step. This strongly identifies an energy-transport closure seam but is not yet a repair decision.

**Latest engineering review:** `Diagnostic 3 Deep Review & REV1 Planning 1` records `PASS-WITH-PREEXECUTION-REVISION`. The reviewed Diagnostic 3 design is sound on temporal attribution, suction topology, mass/energy bookkeeping, provenance and authority containment, but its `causal-owner` wording is stronger than the existing evidence and its provider proof should be strengthened with explicit `u + p/rho`, true IEEE-754 bit equality and an independent test-only IAPWS-IF97 counterfactual.
**Planning validator hotfix:** Hotfix 2 replaces prose-coupled document assertions with stable ASCII marker IDs backed by the planning JSON contract, adds validator encoding hygiene and aggregate missing-marker reporting, and preserves review findings, REV1 guards, authority and next activity unchanged.


<!-- NRS-MARKER:DIAG3-REV1-PREEXECUTION-HOLD -->
The pre-execution review/planning checkpoint is closed `PASS-AS-AUTHORED` after Validator Hotfix 2.

<!-- NRS-MARKER:DIAG3-REV1-IMPLEMENTATION-CHECKPOINT -->
**Active engineering candidate:** `R3 Seed Integration Suction Energy-Transport Causal-Seam Diagnostic 3 REV1` is **implemented, not yet executed**. REV1 preserves the same 10 ms one-step scenario, records explicit production `u + p/rho` transport decomposition and true IEEE-754 bit equality, and adds a separate `Simulation.Tests` IAPWS-IF97 counterfactual. The reviewed Diagnostic 3 remains frozen pre-execution provenance and is not the authoritative runtime gate.

**Active execution command:** `.\scripts\run-m10-final-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3-rev1.cmd`. The only next authorized activity is `EXECUTE-DIAGNOSTIC3-REV1-TEST-ONLY`; return the complete `01`–`07` artifact set for independent adjudication before any repair planning.

**Repository/CI state:** `GitHub Ordinary CI Deterministic Serialization Hotfix 1` is **locally PASS**, including exact-v9 authoritative production audit, current-evidence audit and complete ordinary suite. The same candidate must still be confirmed GREEN by the hosted GitHub `ordinary-ci` workflow. Hosted confirmation is a repository-hygiene hold before any eventual production repair is authorized; it does not alter Diagnostic 3 physics evidence.

**Explicitly blocked:** production repair, seed retuning, transport-convention mutation, threshold/envelope changes, C4 resolver/payload changes, canonical exact-v9 changes, R3 Short Requalification 3, R4 Planning 1, VR3, P3-R1 and a second replacement-long baseline.

**Current production baseline for this branch:** Seed Integration Implementation 1 Hotfix 4, including only the authorized `FluidPhase` namespace compile fix. Canonical exact-v9 method body remains frozen.

**Frozen Diagnostic 2 returned evidence:** `eng/frozen-evidence/ordinary/M10FinalVR2_R3_SeedIntegration_TwoSeedStepPreconditioningDivergenceDiagnostic2_ReturnedArtifacts`.

For a new chat, start from this checkpoint. First preserve the reviewed Diagnostic 3 as pre-execution provenance and implement the separately identified Diagnostic 3 REV1 test-only candidate from `M10_FINAL_VR2_R3_DIAGNOSTIC3_DEEP_REVIEW_REV1_PLANNING1.md`. REV1 is planned to return `01`–`07`; separately confirm the hosted `ordinary-ci` workflow on the same deterministic-CI candidate. Do not merge those two evidence streams into an implicit production-repair authorization.

**M10.9.8 is VALIDATED / CLOSED.** **M10 Final Pre-M11 Cumulative Validation Hotfix 1 is VALIDATED.** The exact-v9 Production Activation Decision 1 Hotfix 1 is now also **VALIDATED**: `integrated-operations-desktop-stable@9` is the authoritative desktop production default and `bounded-demand-following-5-10-5@3` is the authoritative production mission binding.

The first M10 Final long campaign remains frozen as **FAILED / ABORTED exact-v4 evidence**. It is not rewritten. Diagnostic 1–11 repaired LR-M1 scalability, the primary/secondary whole-cycle operating point, breaker-closed governor integral ownership and wet-steam turbine-admission ownership. Exact-v9 was qualified at 600 s and then promoted through a separate opt-in staging gate and authoritative activation-decision gate.

The replacement-long baseline freeze was validated and authorized Execution 1. The first exact-v9 replacement campaign then executed all 1,920 authored seconds / 192,000 steps in 35.2527 minutes but remained **RED**. RL-H1, RL-D1, RL-P1, wall budget, MISSION projection scalability and replay/checkpoint equivalence passed; RL-M1 and RL-R1 both failed because the same 5→10 MWe load-raise path entered protection. Replacement-Long Failure Diagnostics 1–6 have now returned execution PASS as diagnostic evidence gates. D1 fixes `generator-loss-of-synchronism` at step 636 as the first completed owner; D2–D5 eliminate rod authority, direct breaker-closed SPEED, simple valve preload, fixed-time ramping and short thermal-readiness lead as supported first repairs. Diagnostic 6 returned execution PASS with no protection during its 180 s 5.5/6 MWe holds and near-50 Hz late frequency, but no strict requested-load operating-point window: exact-v9 6 MWe tails near 50.000284 Hz, 5.733824 MWe output, 6.350836 MW shaft and -0.271619 MW dispatch adequacy. D6 therefore proves frequency/rotor recovery without yet proving requested-load convergence. P0 Hotfix 2 is VALIDATED. P1 returned execution PASS with final classification `INCONCLUSIVE`; P2 Decision Gate 1 / Plan Amendment 1 is now **VALIDATED** from returned artifact evidence. P1A has now returned execution PASS / overall `INCONCLUSIVE`: exact-v9 5.5 MWe is `CONVERGED`, while exact-v9 6 MWe demonstrates load reachability at the 3,600 s boundary but not the frozen whole-operating-point stationarity contract. P2R Decision Re-entry 1 / Plan Amendment 2 Hotfix 1 has now returned local PASS together with prerequisite Deep Review Pass 1. Todreas/Kazimi Deep Review Pass 2 returned local PASS / `PASS-AS-AUTHORED`. P1B has now returned execution PASS with 3/3 P1A checkpoint reproduction, green protection/numerical sentinels and engineering label `COUPLED-MULTI-DOMAIN`. P2R2 Decision Re-entry 2 is now **VALIDATED** and selects the runtime-ownership branch for owner localization only; no production repair is authorized. Plan Amendment 3 — External Physical Reference Model Assessment Hold is now **VALIDATED** from the user-reported local audit PASS. **VR0 REFERENCE / PROVENANCE CONTRACT FREEZE** is now VALIDATED from the user-reported local audit PASS. **VR1 POINT-KINETICS INDEPENDENT BENCHMARK** is now VALIDATED from the user-confirmed review of the returned local artifacts. **VR2 IAPWS-IF97 WATER/STEAM ERROR MAP** has returned `MODEL-DISCREPANCY-BLOCKING` with a qualified reference harness and a localized inverse compressed-liquid pressure defect. **VR2 MATERIALITY DIAGNOSTIC 1 RETURNED-EVIDENCE ADJUDICATION** is now RETURNED PASS and closes the engineering interpretation as `HYDRAULIC-MATERIALITY-CONFIRMED`. **VR2 ENGINEERING REPAIR PLANNING 1** is VALIDATED / CLOSED from the returned `PASS-AS-AUTHORED` audit. **RP1A REFERENCE DOMAIN CORPUS & SEAM MAP FREEZE REV1** is now VALIDATED from the returned complete 7/7 artifact set. It freezes the complete reference corpus, current seam ownership and machine-local pre-candidate performance ceilings. **RP1B TEST-ONLY SHADOW CANDIDATE MATRIX** has now returned `PASS-EVIDENCE-MATRIX-COMPLETE` after Hotfix 2 and is frozen as validated engineering evidence. B1/C1/D1 are all non-selectable in their first versions: B1 preserves exact-v9 phase ownership and performance but misses the <=10% planning target; C1 is fast and highly accurate on resolved states but leaves 12 `feedwater-inventory` exact-v9 rows unresolved; D1 meets the fidelity target but violates the frozen worst-case performance ceiling and remains seam-incomplete. **RP1B REFINEMENT 1 — C2/D2 SHADOW MATRIX** is now validated non-selecting evidence. **RP1B REFINEMENT 2 — C3/D3 VAPOR-SIDE SEAM COMPLETION** is validated evidence. **RP1B REFINEMENT 3 — C3 PERFORMANCE-TAIL ATTRIBUTION** is validated returned evidence: exact-v9 max `150.5 us` and targeted max `162.7 us` are below the frozen ceiling, while one R1-side seam call reached `3667.1 us`. **RP1B REFINEMENT 4 — C3 R1-SEAM WORST-CASE LOCALIZATION & REPRODUCIBILITY** is VALIDATED returned localization evidence. Its two isolated R1-side exceedances occurred on different boundaries and did not justify a C4 code change. **RP1B PERFORMANCE MEASUREMENT REPLANNING 1** is VALIDATED / CLOSED from returned `PASS-AS-AUTHORED` evidence. **RP1B REFINEMENT 5 — C3 CROSS-PROCESS WALL-CLOCK TAIL REPRODUCIBILITY** is now VALIDATED returned evidence. The frozen machine rule returns `C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED` with boundary 3 reproduced in all five processes. Returned engineering review establishes a uniform `416` bytes allocated inside every measured C3 call and exact coincidence of each repeated boundary-3 exceedance with the only Gen0 collection observed inside a candidate-call window in that process. The C4 Planning 1 REV2 review additionally records that the historical sample recorder allocated between candidate calls, so GC correlation is proven while candidate-only control of the exact GC cadence is not. C4 Planning 1 REV2 returned PASS-AS-AUTHORED and its returned artifact set was adjudicated PASS. C4 Test-Only Implementation 1 has now returned a complete 59-file evidence tree and is independently adjudicated PASS as `C4-QUALIFIED-ALLOCATION-TAIL-CLOSED`: 1,967/1,967 semantic comparisons are bit-equivalent to frozen C3, 204,800/204,800 timing calls resolve with 0 B candidate allocation, 0 B whole-region harness allocation, 0 immutable-C2 fallback calls and 0 strict-ceiling exceedances. This closes C4 qualification evidence and authorizes only RP1C planning; RP1C selection and every production/runtime change remain unauthorized. C3 remains byte-for-byte immutable; B1/C1/D1, C2/D2, C3/D3, RP1A corpus/seam coordinates, thresholds and production source remain immutable. It inserts VR0→VR5 independent reference assessment for point kinetics, water/steam, iodine/xenon and decay heat, then returns to P3-R1 if VR5 authorizes `PROCEED-P3R1-EXACTV9`. The original P0 audit attempt is validator-red because of a PowerShell interpolation parser error. Hotfix 1 fixed that parser error but its audit remained validator-red because the validator searched for the stale marker `P0 EVIDENCE & PLANNING FREEZE CANDIDATE` while this document correctly identified the active hotfix as `P0 EVIDENCE & PLANNING FREEZE HOTFIX 1 CANDIDATE`. Hotfix 2 aligns the validator with the actual documented candidate identity. Neither red attempt is a planning/evidence failure. M10 remains OPEN.

This full package still contains the pre-M11 engineering review/planning streams. They remain planning-only and do not weaken the executable validation contract.

M11 is blocked by the amended replacement-long closure route: P0 planning freeze → P1 asymptotic qualification → P2 planning stop → P1A asymptotic closure extension → P2R1 planning stop → Plan Amendment 2 → mandatory Todreas/Kazimi Deep Review Pass 2 → P1B slow-state/owner qualification → P2R2 branch decision (VALIDATED) → Plan Amendment 3 external physical-reference assessment VR0–VR5, with the current VR2 repair-planning/requalification hold before VR3 → P3-R1 owner localization only after the physical-reference route is explicitly cleared → later P3-R repair decision or branch reconsideration → P4 short 5→10→5 qualification → P5 baseline-2 freeze + Replacement-Long Execution 2 → P6 explicit M10 closure.

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

**Active engineering state: M10 FINAL — VR2 R3 REFERENCE-CONSISTENT SEED INTEGRATION FAST-GATE DYNAMIC EQUILIBRIUM DIAGNOSTIC 1 — TEST-ONLY DIAGNOSTIC CANDIDATE.** Implementation 1 reached its focused fast gate but returned RED: raw seed 12/12 remains healthy, while primary flow and governor drift outside the unchanged exact-v9 envelope during the first second. R3 remains RED; Requalification 3 and R4 remain blocked.

P0 Hotfix 2 is **VALIDATED**. Its returned artifact records `m10-final-replacement-long-closure-plan1-p0-passes=True`, no production source/test change, no second-long authorization and `next-authorized-implementation=P1-Asymptotic-First-Stage-Qualification`. D1–D6 are therefore frozen as the completed diagnostic campaign and the P0→P6 route is authoritative.

P1 returned execution PASS with final classification `INCONCLUSIVE`. Its exact-v9 6 MWe probe consumed the full authorized continuation to 1,800 s without trip; frequency and amplitude errors are already near the requested point, but stationarity slopes remain above the frozen P1 bands. P2 Decision Gate 1 is now **VALIDATED** and records `PLAN-STOP-INCONCLUSIVE`, authorizing neither P3-W nor P3-R while explicitly authorizing only Plan Amendment 1.

P1A has returned execution PASS / overall `INCONCLUSIVE`. Its exact-v9 5.5 MWe probe is `CONVERGED`; its exact-v9 6 MWe probe reaches essentially 6 MWe with no trip and near-zero frequency/dispatch/net-acceleration error, but whole-operating-point stationarity is not demonstrated because late output/shaft/steam-flow/turbine-inlet-pressure slopes remain above the frozen P1 ceilings. P2R1 therefore records another planning stop and authorizes neither P3 branch. Plan Amendment 2 defines a bounded P1B observational replay at the existing 3,600 s horizon, preceded by a 600 s 5 MWe background reference. **Todreas/Kazimi Deep Review Pass 2 returned local PASS / `PASS-AS-AUTHORED`, and P1B has now returned execution PASS.** P1B demonstrates a stable 5 MWe background, exact 900/1,800/3,600 s P1A checkpoint reproduction, zero trip/numerical sentinel failures, 6 MWe electrical reachability and persistent load-specific multi-domain slow-state motion. P2R2 is now **VALIDATED** and records `P3-R-OWNER-LOCALIZATION`: P3-W remains unauthorized and production repair remains unauthorized. Plan Amendment 3 is VALIDATED and deliberately holds execution of P3-R1 while a bounded external physical-reference model-assessment sequence (VR0–VR5) quantifies the current point-kinetics, water/steam, iodine/xenon and decay-heat owners against independent references. VR0 is VALIDATED and its frozen contract remains binding. VR1 is now VALIDATED from returned artifact review. VR2 has returned `MODEL-DISCREPANCY-BLOCKING`: its official IF97 self-check passed and the fixed VR2 matrix localized the blocker to inverse compressed-liquid pressure without phase mismatch in that matrix; the later exact-v9 materiality trajectory additionally exposes IF97 Region-4 phase-boundary reinterpretation for hot primary inventories. VR2 Replanning / Materiality Diagnostic 1 Attempt 5 has now executed the complete exact-v9/P1B trajectory and returned all seven evidence artifacts. The artifact summary itself records `execution-pass=True` and `HYDRAULIC-MATERIALITY-CONFIRMED`, with 3/3 P1B checkpoints, 360 resolved node rows, 288 resolved hydraulic-path rows, eight CONFIRMED windows and eight NOT-EXCLUDED windows. The focused test nevertheless returned RED only after artifact generation because its historical `1e-9 kg/s` committed-flow reproduction assertion was stricter than the authoritative exact-v9 H.22 fixed-point absolute-flow residual ceiling `1e-2 kg/s`; the observed maximum residual `0.009952798974779853 kg/s` remains inside that production numerical contract. The returned-evidence adjudication has now completed and confirms `HYDRAULIC-MATERIALITY-CONFIRMED`. Planning 1 is now VALIDATED / CLOSED. RP1A REV1 Reference Domain Corpus & Seam Map Freeze is now VALIDATED from returned review. The first-generation RP1B matrix, Refinement 1 C2/D2 and Refinement 2 C3/D3 are now frozen validated evidence. Refinement 2 closed physical/seam completeness. Refinement 3 subsequently qualified C3 exact-v9/targeted timing but isolated one R1-side seam call at 3667.1 us. Refinement 4 has now returned complete localization evidence: the screen found one 2620.6 us exceedance at boundary 3 with Gen0 activity, while targeted repeats found one 853.5 us exceedance at boundary 191 without GC activity; the same boundary did not reproduce and both boundaries retain ordinary median/p95 timing. Refinement 4 engineering review therefore did not justify C4 and did not erase the strict max evidence. RP1B Performance Measurement Replanning 1 is now VALIDATED from returned `PASS-AS-AUTHORED` audit evidence. RP1B Refinement 5 has completed the frozen five-independent-process protocol without changing C3 or the strict ceiling. Its machine outcome is `C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED`; returned review confirms uniform 416 B/call candidate allocation and repeated coincidence with Gen0 rather than a demonstrated boundary-specific thermodynamic branch. The later C4 pre-execution review also records that the historical sample recorder allocated between calls, so candidate-only causality for the exact Gen0 position is not claimed. Coefficient-only retuning remains insufficient as a standalone repair. Returned C4 evidence now authorizes RP1C planning only; RP1C selection remains unauthorized. Production physics remains unchanged, exact-v9 is immutable, and any later repair requires a new opt-in closure mode plus a new exact-version identity after requalification. P3-R1 resumes only after VR5 returns `PROCEED-P3R1-EXACTV9` or after a separately authorized repair/requalification path. A second replacement-long baseline remains forbidden until P4 short 5→10→5 qualification passes.

**Parallel documentation overlay:** the reviewed pre-M11 planning set remains planning-only and does not supersede executable validation evidence. Reviews 1–2 now record and deeply re-check five additional sources on coordinated plant control/flexibility, nuclear load following, steam-generation circulation/separation and modern reactor physics in `PRE_M11_PLANT_DYNAMICS_THERMAL_HYDRAULICS_REACTOR_PHYSICS_REVIEW.md` and `research/PRE_M11_DEEP_ENGINEERING_SECTION_REVIEW_2.md`. Review 2 revisits only the previously selected high-value sections and adds explicit transferability boundaries. This documentation-only overlay leaves the P1A executable contract byte-for-byte unchanged in `src/` and in all pre-existing tests.

## Validation required for active candidate

The active executable gate is **R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFICATION1**. It is test-only and may not mutate production `src/`, canonical exact-v9, the default closure mode or any exact-version identity.

Run from the repository root in PowerShell:

```powershell
.\scripts\run-m10-final-vr2-r3-short-exact-v9-equivalent-shadow-composition-requalification1.cmd
```

The controlled runner must complete all five stages: static contract audit, ordinary Release suite, forced `--no-incremental` Application.Tests rebuild, focused R3 execution, and evidence adjudication. A successful local run must produce exactly seven artifacts under `artifacts\m10-final-physical-reference-vr2-r3-short-exact-v9-equivalent-shadow-composition-requalification1` and the complete folder must be returned for independent adjudication.

R4 Planning 1 remains blocked until that returned R3 evidence is adjudicated PASS. Default mode 2, canonical exact-v9 mutation, a new exact identity, VR3, P3-R1 and a second replacement-long baseline remain unauthorized.

## Evidence and package policy

Candidate source ZIPs intentionally exclude `tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/Evidence/`, generated `artifacts/`, `bin/` and `obj/`.

`eng/frozen-evidence/ordinary` is a compact prerequisite store: direct payloads are capped at 1 MiB. Larger immutable historical payloads are retained in authenticated compressed packs under `eng/frozen-evidence/archive` with canonical logical-path/SHA-256/byte-count entries in `eng/frozen-evidence/large-payload-manifest.csv`; restore is explicit and temporary for historical reruns.

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

The current forward chain is: **Diagnostic 1 returned/adjudicated PASS -> Two-Seed-Step Preconditioning Divergence Diagnostic 2 -> returned Diagnostic 2 adjudication -> repair planning only if causally supported -> bounded implementation/fast gate -> R3 Short Requalification 3 only after an explicit fast-gate PASS -> R4 Planning 1 only after returned/adjudicated R3 PASS -> R4 long materiality recheck -> R5 VR2 re-entry -> R6 activation decision -> VR3 -> VR4 -> VR5 -> P3-R1 only if the physical-reference route authorizes it -> P3-R2 -> P4 -> P5 Replacement-Long Baseline 2 / Execution 2 -> P6 explicit M10 closure -> M11**. Do not skip returned-evidence adjudication, retune the raw seed without evidence, or reinterpret historical exact-v9 while advancing this chain.

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

Plan Amendment 2 Hotfix 1 and Deep Review Pass 2 are locally VALIDATED / `PASS-AS-AUTHORED`. P1B then executed successfully with the planned slow-state evidence and returned `COUPLED-MULTI-DOMAIN`. P2R2 is VALIDATED and selects P3-R owner localization only. Plan Amendment 3 is VALIDATED and holds P3-R1 behind VR0–VR5 external physical-reference assessment; VR0 Reference/Provenance Contract Freeze is VALIDATED; VR2 Engineering Repair Planning 1 is VALIDATED / CLOSED; RP1A REV1 is VALIDATED; the returned B1/C1/D1 RP1B matrix is VALIDATED engineering evidence; RP1B Refinement 1 C2/D2 is validated non-selecting evidence; RP1B Refinement 2 C3/D3 is validated evidence; RP1B Refinement 3 is validated returned timing evidence; RP1B Refinement 4 is now validated returned localization evidence; RP1B Performance Measurement Replanning 1 is VALIDATED; RP1B Refinement 5 is VALIDATED returned evidence with `C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED`; C4 Planning 1 REV2 is returned/adjudicated PASS, and C4 Test-Only Implementation 1 is now returned/adjudicated PASS as `C4-QUALIFIED-ALLOCATION-TAIL-CLOSED`; RP1C Planning 1 is returned/adjudicated PASS; it freezes the corrected C4/D3 readiness matrix and preserves `NO-SELECTION`. The immutable-C4 Full-Domain Performance Confirmation 1 is returned/adjudicated `C4-FULL-DOMAIN-PERFORMANCE-NOT-CONFIRMED`; Tail Attribution Planning 1 is returned/adjudicated PASS, and the active evidence candidate is now C4 Exact-v9 Wall-Clock Tail Attribution 1 REV1. RP1C selection remains unauthorized pending returned attribution evidence adjudication. The P1B binding clarifications remain in force: 1 s data are slow-state downsampling only; per-step/canonical-event protection and numerical sentinels remain required; no scalar cross-domain owner score is used; existing conservation/energy ledgers are reused; controller memory/command/physical state stay distinct; and `NO-MATERIAL-LATE-DRIFT` is not stationarity qualification.

### P2R1 / Plan Amendment 2 checkpoint

P1A is returned execution PASS / overall `INCONCLUSIVE`; 6 MWe load reachability is demonstrated but full represented stationarity is not. P2R1 selected no P3 branch. Plan Amendment 2 froze the P1B owner-localization scope, Deep Review Pass 2 returned `PASS-AS-AUTHORED`, and P1B has now returned execution PASS / `COUPLED-MULTI-DOMAIN`. P2R2 is validated. Plan Amendment 3 / Physical Reference Model Assessment is VALIDATED; VR0 Reference/Provenance Contract Freeze is VALIDATED; VR2 Engineering Repair Planning 1 is VALIDATED / CLOSED; RP1A is VALIDATED; the first-generation RP1B matrix is complete and non-selecting; RP1B Refinement 5 returned evidence is VALIDATED; C4 Planning 1 REV2 and C4 Test-Only Implementation 1 are returned/adjudicated PASS; C4 is classified `C4-QUALIFIED-ALLOCATION-TAIL-CLOSED`, C3 remains immutable, and RP1C Planning 1 is returned/adjudicated PASS; the immutable-C4 full-domain performance confirmation is closed as returned evidence; Tail Attribution Planning 1 is returned/adjudicated PASS and the evidence-only Attribution 1 implementation is active, while selection and production authority remain blocked.

### P1B implementation checkpoint

Deep Review Pass 2 is locally VALIDATED / `PASS-AS-AUTHORED`. P1B has now completed its 600 s exact-v9 5 MWe background reference and unchanged exact-v9 5→6 MWe replay to 3,600 s, reproducing P1A checkpoints at 900/1,800/3,600 s and emitting canonical inventory, hydraulic, controller/actuator, turbine and generator evidence. P1B selected no P3 branch; its returned evidence was frozen for P2R2, which has now validated the P3-R owner-localization route. P3-R1 remains temporarily held by Plan Amendment 3.

### VR2 Materiality Attempt 5 returned-evidence adjudication note

The returned Attempt-5 artifacts are complete and are now frozen under `eng/frozen-evidence/ordinary/M10FinalPhysicalReferenceVR2MaterialityDiagnostic1_Attempt5_Artifacts/`. The completed adjudication did not rewrite the historical `1e-9 kg/s` assertion or rerun the long trajectory. It confirmed that the returned materiality classification remains valid when the canonical committed-flow versus instantaneous-map difference is bounded by the authoritative exact-v9 H.22 fixed-point residual ceiling of `0.01 kg/s`. The returned evidence also shows that all 72 sampled `pressure` states are production `SubcooledLiquid` but resolve from the same `(v,u)` inventory to IF97 Region 4 `SaturatedMixture`; the same production-subcooled/reference-mixture reinterpretation occurs for 55 sampled `suction` states. This phase-boundary observation must be carried into any later repair planning. No repair, tolerance change, exact-v9 change or VR3 execution is authorized by the returned adjudication; Engineering Repair Planning 1 is validated; RP1A REV1 and the first-generation RP1B matrix are validated evidence, and RP1B Refinement 1 C2/D2 is validated evidence and RP1B Refinement 2 C3/D3 is validated evidence and RP1B Refinement 3 returned complete timing evidence and RP1B Refinement 4 is validated localization evidence and Performance Measurement Replanning 1 and RP1B Refinement 5 returned evidence are VALIDATED; C4 Planning 1 REV2 and C4 Test-Only Implementation 1 are returned/adjudicated PASS; the C4 classification is `C4-QUALIFIED-ALLOCATION-TAIL-CLOSED`; RP1C Planning 1 and Full-Domain Performance Confirmation 1 are now returned/adjudicated, and Tail Attribution Planning 1 is returned/adjudicated PASS; at that historical checkpoint its evidence-only Attribution 1 implementation became the successor.

The historical adjudication contract is documented in `M10_FINAL_VR2_MATERIALITY_DIAGNOSTIC1_RETURNED_EVIDENCE_ADJUDICATION.md`.

### VR2 Engineering Repair Planning 1 / RP1A checkpoint

The returned Planning 1 audit is `PASS-AS-AUTHORED`; Planning 1 is VALIDATED / CLOSED, RP1A REV1 is VALIDATED, the first-generation RP1B matrix is frozen complete, and RP1B Refinement 1 C2/D2 is validated non-selecting evidence; RP1B Refinement 2 C3/D3 is validated evidence; RP1B Refinement 3 is validated returned timing evidence; RP1B Refinement 4 is now validated returned localization evidence; RP1B Performance Measurement Replanning 1 is VALIDATED; RP1B Refinement 5 is VALIDATED returned evidence with `C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED`; C4 Planning 1 REV2 is returned/adjudicated PASS, and C4 Test-Only Implementation 1 is now returned/adjudicated PASS as `C4-QUALIFIED-ALLOCATION-TAIL-CLOSED`; RP1C Planning 1 and Full-Domain Performance Confirmation 1 are returned/adjudicated; C4 Exact-v9 Wall-Clock Tail Attribution Planning 1 is returned/adjudicated PASS; Attribution 1 was the active implementation at that historical checkpoint while RP1C selection remained unauthorized. The returned Materiality Diagnostic 1 adjudication is closed as `HYDRAULIC-MATERIALITY-CONFIRMED`. The validated Planning 1 contract treats the issue as inverse thermodynamic ownership, not only compressed-liquid pressure calibration, because exact-v9 path evidence includes 72/72 `pressure` and 55/72 `suction` production-subcooled inventories that resolve as IF97 Region-4 mixture.

Planning 1 advances three future test-only shadow families: piecewise reference-consistent reduced closure, bounded IF97-derived interpolation surrogate, and bounded production IF97 subset as fidelity/cost comparator. A local coefficient-only retune is explicitly insufficient standalone. No production source change is authorized. Exact-v9 and `CorrelationConsistentInverseDomain` remain immutable; any later repair must use a new opt-in closure mode and a new exact-version identity after requalification.

### VR2 Engineering Repair Planning 1 / RP1B checkpoint

RP1A REV1 has returned a complete `7/7` artifact set and is now VALIDATED. The frozen corpus contains 40 VR2 reference rows, 360 exact-v9 node rows, 288 hydraulic rows and 1,280 seam probes across 320 boundaries. The pre-candidate machine-local ceilings are frozen at 94.8 us median, 158.80666666666667 us p95, 409.30666666666673 us max and 2816 B median allocation. Candidate timing was not inspected before this freeze.

The returned first-generation RP1B matrix is now frozen under `eng/frozen-evidence/ordinary/M10FinalVR2EngineeringRepairPlanning1_RP1B_Artifacts/`. No first-generation candidate is selection-eligible. B1 is fast and preserves 100% exact-v9 phase agreement but misses the <=10% planning target; C1 meets the performance ceiling and resolves all 39 inverse-applicable VR2 rows but leaves 12 exact-v9 `feedwater-inventory` rows unresolved; D1 meets the planning fidelity target with 100% exact-v9 phase agreement but violates the frozen performance ceiling and remains seam-incomplete. Those identities are immutable; no in-place retuning is allowed.

The returned Refinement 1 matrix is validated but non-selecting. Refinement 2 introduced `C3-VAPOR-SEAM-COMPLETE-SURROGATE` and `D3-VAPOR-SEAM-COMPLETE-IF97-COMPARATOR`; both became physically/seam complete, after which Refinements 3–4 isolated the remaining issue to rare R1-side wall-clock tails rather than a stable thermodynamic error. Performance Measurement Replanning 1 then froze the five-process same-boundary reproducibility rule, and Refinement 5 satisfied that rule with returned `C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED` evidence. The RP1A corpus, seam offsets, 10% planning target, 25% VR2 blocking ceiling and machine-local performance ceilings remain unchanged. C4 Planning 1 REV2 and C4 Test-Only Implementation 1 are returned/adjudicated PASS as `C4-QUALIFIED-ALLOCATION-TAIL-CLOSED`. RP1C Planning 1 and Full-Domain Performance Confirmation 1 are returned/adjudicated; the full-domain classification remains `C4-FULL-DOMAIN-PERFORMANCE-NOT-CONFIRMED`. Tail Attribution 1 and Runtime Factor Isolation 1 are now also returned/adjudicated. Runtime Factor Isolation 1 establishes TieredCompilation as materially causal for the gross slowdown, does not promote QuickJit as a rare-tail owner, and finds no material QuickJitForLoops tail benefit. Planning 1 and Host-Provenance Amendment 1 have both returned `PASS-AS-AUTHORED`; the amendment is adjudicated complete. Historical checkpoint — the active candidate at that stage was maintenance-only Frozen Evidence Ordinary Compaction 1 before A2 implementation. A2 implementation remained deferred until that retention cleanup returned and was reviewed; RP1C selection and production repair remain unauthorized.

Performance Measurement Replanning 1 Attempt 1 stopped at static preflight only because the validator required the exact machine-token slow-path rule inside the human planning Markdown. Hotfix 1 aligns the validator to the document's semantic wording while leaving the JSON machine rule, C3, thresholds, Refinement 5 design and all authority flags unchanged.

## New-chat restart / handoff

Historical handoff checkpoint (superseded): the project was then on **Branch A — reproducible runtime-mode sensitivity**. Runtime Factor Isolation 1 established TieredCompilation as materially causal for the gross exact-v9 slowdown; QuickJit was not promoted as a rare-tail owner and QuickJitForLoops had no material tail benefit. Dynamic PGO Comparator 1 returned/adjudicated from a complete 46-file / 230,400-call tree, and Runtime Configuration Impact Assessment Planning 1 / Host-Provenance Amendment 1 / Frozen Evidence Ordinary Compaction 1 later returned `PASS-AS-AUTHORED`. At that historical point the active candidate became Runtime Configuration Impact Assessment 1. This paragraph is retained only as decision trace and is not a current restart instruction.

Current frozen state:

- VR0 and VR1 are VALIDATED; VR2 initial IF97 assessment is `MODEL-DISCREPANCY-BLOCKING` and its materiality is `HYDRAULIC-MATERIALITY-CONFIRMED`.
- Planning 1 and RP1A are VALIDATED. The RP1A corpus remains 40 VR2 rows, 360 exact-v9 node rows, 288 hydraulic rows and 1,280 seam probes; all planning/performance ceilings remain frozen.
- B1/C1/D1, C2/D2 and C3/D3 are immutable historical shadow identities. C3 is the most mature production-oriented shadow candidate: physical/seam qualification is complete, exact-v9 timing is below ceiling, and no production code uses C3.
- Refinements 3–4 preserved isolated R1-side wall-clock exceedances. Performance Measurement Replanning 1 froze the five-process same-boundary rule, and Refinement 5 has now satisfied that rule with boundary 3 in 5/5 processes. All five repeated exceedances coincide with Gen0; every measured call allocates 416 bytes. C4 planning is justified, but the evidence does not support treating boundary 3 itself as an intrinsically slow thermodynamic branch.
- Historical closed-gate checkpoint (superseded): RP1B C4 Test-Only Implementation 1 returned a complete 59-file tree and was adjudicated `C4-QUALIFIED-ALLOCATION-TAIL-CLOSED`; RP1C Planning 1 returned/adjudicated PASS; Full-Domain Performance Confirmation 1 remained `C4-FULL-DOMAIN-PERFORMANCE-NOT-CONFIRMED`; Tail Attribution 1 REV1, Runtime Factor Isolation 1 and Dynamic PGO Comparator 1 returned/adjudicated evidence. Runtime Configuration Impact Assessment Planning 1, Host-Provenance Amendment 1 and maintenance-only Compaction 1 later returned `PASS-AS-AUTHORED`. At that historical point A2 became the active evidence gate. This bullet is retained as chronology only; the authoritative current checkpoint is the R3 execution candidate at the top of this document.

For a new chat, start with this file and the current R3 chain: `M10_FINAL_VR2_R1_SELECTED_C4_OPT_IN_CLOSURE_IMPLEMENTATION1_RETURNED_EVIDENCE_ADJUDICATION.md`, `M10_FINAL_VR2_R2_FOCUSED_THERMODYNAMIC_REFERENCE_TOPOLOGY_QUALIFICATION1_RETURNED_EVIDENCE_ADJUDICATION.md`, `M10_FINAL_VR2_R3_SHORT_EXACT_V9_EQUIVALENT_SHADOW_COMPOSITION_REQUALIFICATION_PLANNING1_RETURNED_EVIDENCE_ADJUDICATION.md`, `M10_FINAL_VR2_R3_SHORT_EXACT_V9_EQUIVALENT_SHADOW_COMPOSITION_REQUALIFICATION1.md`, its prequalification review, and the execution contract under `eng/`. Historical Branch-A performance evidence remains available through the frozen-evidence manifest/archive but is no longer the restart point.

## Returned VR2 RP1B Refinement 5 handoff

Performance Measurement Replanning 1 is VALIDATED / CLOSED. Refinement 5 has now returned complete evidence and is VALIDATED as `C3-SAME-BOUNDARY-SLOW-PATH-CONFIRMED`. The command below is retained as historical execution provenance; it is no longer the active gate:

```powershell
.\scripts\run-m10-final-vr2-engineering-repair-planning1-rp1b-refinement5.cmd
```

The runner executes five independent focused-test processes over the immutable 320-boundary R1 corpus and then aggregates same-boundary reproduction evidence. Return the complete `artifacts/m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-refinement5` folder before C4 planning, performance-contract adjudication or RP1C. C3, the strict `409.30666666666673 us` ceiling, production thermodynamics and exact-v9 remain unchanged.

## 2026-09-17 — RP1C C4 Exact-v9 Tail Attribution 1 returned evidence

Attribution 1 REV1 returned evidence is adjudicated **PASS as comparative evidence**. The 20-process, 460,800-call counterbalanced experiment establishes strong runtime-configuration sensitivity: `TIERING-OFF` is consistently and dramatically slower, while both explicit tiering-on modes have zero strict exceedances. Dynamic PGO causality is not established, ambient-default equivalence is not established, and RP1C selection remains blocked. The next activity is planning-only for a single-factor runtime isolation gate; C4, thresholds, production/runtime configuration, exact-v9, VR3 and P3-R1 remain unchanged and unauthorized.


## 2026-09-17 — Runtime Factor Isolation Planning 1 candidate

Attribution 1 returned evidence confirms strong runtime sensitivity and rejects `TIERING-OFF` as a repair direction, but does not isolate TieredCompilation from QuickJit and does not prove Dynamic PGO causality. At that checkpoint, the candidate was planning-only Runtime Factor Isolation Planning 1. It freezes a four-mode chained single-factor matrix: TieredCompilation, then QuickJit, then QuickJitForLoops, with PGO held OFF and ReadyToRun fixed. A QJFL=1 PGO comparator remains a separate future planning gate only if still material after returned factor-isolation adjudication. RP1C selection and all production/runtime changes remain unauthorized.


## 2026-09-17 — Runtime Factor Isolation 1 returned adjudication / Dynamic PGO Comparator Planning 1

Runtime Factor Isolation 1 returned the complete 86-file evidence tree. The adjudication reconstructs 20 fresh processes and 460,800 calls with zero unresolved calls, zero measured allocation and zero GC. TieredCompilation is materially causal for the gross slowdown under the frozen single-factor A↔B contrast; QuickJit is not promoted as a rare-tail owner; QuickJitForLoops provides no material tail benefit. Dynamic PGO remained materially unresolved with QJFL enabled, so that checkpoint advanced to planning-only `RP1C-C4-DYNAMIC-PGO-COMPARATOR-PLANNING1`. The future comparator is 2 modes × 5 fresh processes = 10 processes / 230,400 calls and changes only `DOTNET_TieredPGO`. Ambient/effective-default equivalence is deferred to Branch A2 Runtime Configuration Impact Assessment. All RP1C/production/runtime authority remains false.


## 2026-09-17 — Dynamic PGO Comparator Planning 1 returned adjudication / Comparator 1 implementation

Dynamic PGO Comparator Planning 1 returned `PASS-AS-AUTHORED` with the complete four-file planning artifact set. The returned matrix freezes exactly two modes, `PGO-OFF-QJFL-ON` and `PGO-ON-QJFL-ON`, with TieredCompilation, QuickJit, QuickJitForLoops and ReadyToRun fixed ON; only `DOTNET_TieredPGO` changes. The planning adjudication therefore authorizes only the evidence-only gate `RP1C-C4-EXACT-V9-DYNAMIC-PGO-COMPARATOR1`: 10 fresh processes, 230,400 measured calls, complete 64×360 identity checking per process, 46 final evidence files, unchanged strict maximum `409.30666666666673 us` and diagnostic-only `100 us` floor. Ambient/effective-default equivalence remains deferred to Branch A2. Returned comparator evidence is required before any PGO causal promotion, Runtime Configuration Impact Assessment planning, RP1C selection or production/runtime change.


## 2026-09-17 — Dynamic PGO Comparator 1 returned adjudication / Runtime Configuration Impact Assessment Planning 1

Dynamic PGO Comparator 1 returned the complete 46-file evidence tree. The adjudication accepts 10 fresh processes and 230,400 measured calls with zero unresolved calls, zero measured allocation and zero measured-region GC. PGO ON reduces the central exact-v9 timing distribution in every counterbalanced run block and across all 360 row medians; the aggregate process-median signal is 4.7→2.1 us and process-p95 signal 5.4→2.5 us. The sparse tail changes 13→8 calls above 100 us and 1→0 strict exceedances, so Dynamic PGO rare strict-tail causality is not promoted. Historical checkpoint — the successor at that stage was planning-only `RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT-PLANNING1`. Its future Branch A2 gate must compare ambient/unset with an explicit all-ON qualification reference across exact-v9, ordinary Release, replay/determinism and representative non-VR2 performance. All RP1C/production/runtime/FDPC2 authority remains false.

## 2026-09-17 — Runtime Configuration Impact Assessment Planning 1 returned adjudication / Host-Provenance Amendment 1

Runtime Configuration Impact Assessment Planning 1 returned the complete four-file planning artifact set and is adjudicated `PASS-AS-AUTHORED`. After return, the execution workflow disclosed that performance gates may be run on multiple physical PCs with materially different performance. The original planning contract did not freeze stable host identity. Its static PASS therefore remains valid, but it is no longer sufficient by itself to authorize A2 implementation.

Host-Provenance Amendment 1 subsequently returned the complete four-file artifact set and is adjudicated `PASS-AS-AUTHORED`. It preserves the two A2 profiles, all run counts, the 78-file future evidence shape, immutable C4/exact-v9 and the unchanged `409.30666666666673 us` strict maximum while adding one-host-per-complete-A2 execution, privacy-preserving SHA-256 host fingerprinting, start/end active-power-scheme capture, no cross-host evidence mixing and host-scoped interpretation of absolute wall-clock observations. Runtime Factor Isolation 1 and Dynamic PGO Comparator 1 recorded consistent minimal runtime context (.NET 10.0.7, Windows 10.0.22631, x64, 20 processors, Stopwatch frequency 10000000), but they do not prove same physical host retroactively because no stable host fingerprint or CPU model was captured. No prior evidence is rewritten. Before A2 implementation, the project executes maintenance-only Frozen Evidence Ordinary Compaction 1.


### A2 second-pass harness hardening

Before local A2 execution, an independent second pass corrected the .NET 10 MTP argument boundary. The first local run then exposed a reporter-accounting mismatch: xUnit v3 3.2.2 schema-3 emitted `total=507`, `passed=487`, `not-run=20`. A2 therefore derives `executed = passed + failed + skipped`, accepts reporter `total` only when it equals either `executed` or `executed + not-run`, records the detected accounting mode, and uses `executed` for non-zero and exact-cardinality checks. `--results-directory` and `--minimum-expected-tests 1` remain SDK/MTP options before `--`; xUnit report/filter options remain after it. These are harness-only corrections; the active A2 engineering protocol, single-host policy, 78-file evidence shape, C4/exact-v9 immutability and all downstream authority boundaries are unchanged.


### A2 Hotfix 4 working-copy identity note
Runtime validation compares canonical `src`/`tests` content and excludes generated `bin`/`obj` subtrees from tree identity. Local generated output from prior builds is permitted; candidate ZIPs remain clean. At that historical checkpoint A2 remained the only live evidence gate; this is superseded by the 2026-09-18 returned A2 / FDPC2 Planning 1 checkpoint below.


A previous failed A2 attempt may leave partial files under the owned A2 artifact root. This is permitted in the working copy because the A2 orchestrator deletes and recreates that entire owned root before new evidence collection, preventing stale/new evidence mixing.

### 2026-09-18 -- A2 returned / FDPC2 Planning 1 historical checkpoint (superseded)

Runtime Configuration Impact Assessment 1 has returned its complete 78-file same-host evidence tree and is adjudicated PASS for Branch A progression. `AMBIENT-UNSET` and `EXPLICIT-REFERENCE-ALL-ON` both preserve ordinary Release, replay/determinism and the validated non-VR2 hot-path owner. Ambient exact-v9 records 0 strict misses; explicit all-ON records one isolated 416.2 us miss with no reproducible strict owner. The project therefore carries `AMBIENT-UNSET` forward as the qualification profile on the frozen A2 host without claiming that internal runtime defaults are identical to explicit all-ON.

At that checkpoint the only live gate was planning-only `RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION2-PLANNING1`. It is superseded by the returned Planning 1 adjudication and FDPC2 executable-gate checkpoint below. RP1C selection, production runtime change/repair, threshold/exact-v9 changes, VR3, P3-R1 and second replacement-long remain unauthorized.

### 2026-09-18 -- Historical FDPC2 Planning 1 returned / FDPC2 executable gate (superseded)

`RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION2-PLANNING1` returned `PASS-AS-AUTHORED`. At that historical checkpoint the Branch A gate was evidence-only `RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION2`, under `AMBIENT-UNSET` on the frozen A2 host and active power scheme. Five fresh processes must produce 217,600 measured calls and exactly 32 evidence files against the unchanged corrected predicate. FDPC1 remains negative historical evidence and is not reinterpreted. RP1C selection, production runtime change/repair, threshold/exact-v9 changes, VR3, P3-R1 and second replacement-long remain unauthorized pending returned FDPC2 adjudication.

### 2026-09-18 -- Historical Selection Planning 1 returned / RP1C Selection 1 executable gate (superseded)

Historical checkpoint (superseded by returned Selection 1): `RP1C-ENGINEERING-REPAIR-SELECTION-PLANNING1` returned `PASS-AS-AUTHORED`; at that point the only live gate was decision-only `RP1C-ENGINEERING-REPAIR-SELECTION1`. Its decision space remains exactly `SELECT-C4 | SELECT-NONE`; `SELECT-NONE` remains mandatory and the authored decision is `SELECT-C4`, based on the frozen readiness evidence rather than automatic selection. The gate performs no measurement and changes no candidate/runtime/threshold/corpus. Returned selection adjudication is required before any `R1-IMPLEMENTATION-PLANNING-ONLY` activity. Production repair/runtime change, threshold/exact-v9 changes, VR3, P3-R1 and second replacement-long remain unauthorized.

### 2026-09-18 -- Historical RP1C Selection 1 / R1 Implementation Planning 1 checkpoint (superseded)

`RP1C-ENGINEERING-REPAIR-SELECTION1` returned complete decision evidence and is adjudicated `SELECT-C4`. C4 + `AMBIENT-UNSET` is the authored selected repair pair; `SELECT-NONE` was preserved and considered, D3 remains blocked, no new measurement or candidate mutation occurred, and no production authority was granted by selection itself.

That planning-only checkpoint is superseded by returned `PASS-AS-AUTHORED` R1 Implementation Planning 1 and the subsequently returned R1 implementation PASS. Its constraints remain historical provenance only.

### 2026-09-18 -- Historical R1 Selected C4 Opt-In Closure Implementation 1 checkpoint (closed)

R1 Implementation Planning 1 returned `PASS-AS-AUTHORED`; R1 Selected C4 Opt-In Closure Implementation 1 subsequently returned complete evidence and is adjudicated PASS. The seven returned R1 artifacts are frozen. Its planning-only R2 successor was subsequently completed and superseded by returned R2 qualification and returned R3 Planning 1 evidence.

### 2026-09-18 -- Historical returned R1 implementation / R2 Planning 1 checkpoint (superseded)

R1 implementation is closed PASS as opt-in implementation evidence. Production mode 2, payload provenance, 1,967 C4-equivalence comparisons, zero resolve-time allocation/I/O/decode, mode 0/default regression, mode 1 legacy-source preservation and ordinary Release are all green. At that historical checkpoint the candidate was **R2 Focused Thermodynamic / Reference / Topology Qualification — Planning 1** only. It freezes independent IF97 requalification of explicit mode 2 over 40 VR2 reference rows, 360 frozen exact-v9 states and 1,280 seam probes. R2 execution, exact-v9 composition, R3, R4, default activation, VR3 and P3-R1 remain blocked until the returned R2 planning artifacts are adjudicated.

### 2026-09-18 -- Historical returned R2 qualification / R3 Planning 1 checkpoint (superseded)

R2 Focused Thermodynamic / Reference / Topology Qualification 1 returned the complete nine-file evidence set and is independently adjudicated `PASS-R2-FOCUSED-THERMODYNAMIC-REFERENCE-TOPOLOGY-QUALIFIED`. Independent IF97 self-check, 39/39 inverse VR2 states, 360/360 frozen exact-v9 states, 1,280/1,280 seam probes, continuity ceilings and deterministic repeat are green. At that historical checkpoint the successor was planning-only `R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFICATION-PLANNING1`: prove a test-local mode-1 shadow is fingerprint-equivalent to canonical exact-v9, then substitute only closure mode 2 for a 120 s short health/ownership requalification. R4 long materiality, mode-2 default activation, exact-v9 mutation, VR3 and P3-R1 remain blocked.

### 2026-09-18 -- Returned R3 Planning 1 / R3 execution checkpoint

R3 Short Exact-v9-Equivalent Shadow / Composition Requalification Planning 1 returned all four required artifacts and is adjudicated `PASS-AS-AUTHORED`. The authorized executable successor is only `R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFICATION1`. It may add one new `Application.Tests` source and no production source: first prove mode-1 shadow equivalence to canonical exact-v9 for 128 deterministic steps, then substitute only closure mode 2 for the inherited 120 s / 12,000-step exact-v9 short envelope and an independent 128-step deterministic repeat. A successful local R3 run is still qualification evidence only and must be returned/adjudicated before R4 planning. Mode-2 default activation, canonical exact-v9 mutation, new exact identity, VR3, P3-R1 and second replacement-long remain unauthorized.


## 2026-09-19 — Current R3 Diagnostic 2 adjudicator hotfix checkpoint

Diagnostic 2 dynamic evidence generation completed through files `01`–`06`. Stage `[4/4] Evidence adjudication` then failed before writing `07`/`08` because the raw checkpoint correctly has zero phase mismatches and Windows PowerShell assigned the zero-output `PhaseNodes` invocation to `$null`; `Set-StrictMode -Version Latest` therefore rejected `$rawPhase.Count` with `PropertyNotFoundStrict`.

The active candidate is now **Diagnostic 2 Adjudicator Hotfix 1**, not a repeat of the dynamic diagnostic. It preserves the original Diagnostic 2 candidate byte-for-byte, SHA-256 locks the six returned CSVs, and adds a separate array-safe adjudicator plus an adjudication-only runner. The returned evidence already shows the first phase mismatch at `seed-step1` on `suction`, while the raw checkpoint has zero mismatches. R3 remains RED; production repair, seed retuning, threshold/C4/exact-v9 changes, R3 Requalification 3 and R4 Planning 1 remain unauthorized.

Current command from repository root in PowerShell:

```powershell
.\scripts\run-m10-final-vr2-r3-seed-integration-two-seed-step-preconditioning-divergence-diagnostic2-hotfix1-adjudication.cmd
```

Return the complete Diagnostic 2 artifact directory containing `01`–`09` before causal-seam selection or repair planning.

## 2026-09-19 - Fingerprint V1 cross-host culture drift isolated; Hotfix 1 candidate

NRS-MARKER:PROJECT-FPV1-CROSS-HOST-HOTFIX1

Cross-host Diagnostic 1 returned local and hosted captures with identical 731-node structure and zero numeric-leaf differences. The only scalar difference is `/primaryCircuit/loops/0/branches/0/voidText`: `Void 0,0%` under local `it-IT` versus `Void 0.0%` under hosted `en-US`. Hotfix 1 REV1 attempted to re-anchor V1, but local `CI CURRENT EVIDENCE` correctly rejected that approach because the Exact-V9 authoritative audit derives a second-level hash from V1 fingerprints. REV2 instead renders the live presentation invariant while preserving the frozen V1 canonical bytes and therefore the historical H29 `63643e...f362` and Exact-V9 `7880AD...B5418` anchors. R3 remains RED and the VR2 physical branch is unchanged; VR2 Repair Planning 1 remains blocked until hosted ordinary CI is GREEN.

Active local qualification command for this checkpoint:

```powershell
.\scripts\run-m10974-fingerprint-v1-cross-host-determinism-hotfix1.cmd
```

Return the complete `artifacts/m10974-fingerprint-v1-cross-host-determinism-hotfix1-rev2` folder. If local qualification is GREEN, push the exact same candidate and require GitHub hosted `ordinary-ci` GREEN before unblocking VR2 Repair Planning 1.

NRS-MARKER:PROJECT-FPV1-HOTFIX1-ACTIVE-COMMAND

## 2026-09-19 - Exact-V9 cross-host determinism contract V2 candidate

NRS-MARKER:PROJECT-EXACT-V9-CROSS-HOST-DETERMINISM-V2

The runtime-alignment experiment is adjudicated: GitHub forced to `.NET 10.0.5` still reproduced the hosted raw V1
aggregate `1E8AAF...80FDD7`, and its 128-step payload trace is byte-identical to ordinary hosted CI. The historical
local raw V1 aggregate `7880AD...B5418` is therefore retained as provenance and same-host exact evidence, not as a
cross-host frozen assertion.

The candidate introduces `sha256-control-room-snapshot-v2-presentation-canonical` with frozen Exact-V9 aggregate
`99B9D27A8F5791A194771D698E8A0740F7024058C172D645E2DF74F1B3C09E73`. V2 preserves the complete presentation
payload except hidden machine-precision `numericValue` number tokens, while V1 selector/direct equality and all
existing physical/conservation gates remain exact and unchanged.

Local qualification command:

```powershell
.\scripts\run-m10974-exact-v9-cross-host-determinism-contract-v2.cmd
```

If local qualification is GREEN, push the exact candidate unchanged and require hosted `ordinary-ci` GREEN before
closing the CI block. R3 remains RED/frozen until that hosted gate passes.


## 2026-09-20 - R3 Energy-Transport Ownership Repair Planning 1 candidate

NRS-MARKER:R3-ENERGY-TRANSPORT-REPAIR-PLANNING1-CURRENT

Diagnostic 3 REV1 remains independently adjudicated `CAUSAL-CLOSURE-CONFIRMED` at
`STEAM-DRUM-LIQUID-TRANSPORT-VS-MODE2-SUCTION-TRANSPORT`. The project-owner process rule is now local-authority:
local qualification is sufficient for advancement and hosted GitHub CI is advisory unless explicitly restored as a gate.

The active planning-only candidate compares the three previously frozen ownership families and authors **Family B - explicit closure-aware transport-property contract** as the selected design for audit. The selected repair owner is
`ACTIVE-CLOSURE-TRANSPORT-PROPERTY-CONTRACT`; no production file is changed by this planning candidate.

A returned local planning PASS may authorize only `R3-ENERGY-TRANSPORT-OWNERSHIP-REPAIR-IMPLEMENTATION1`.
R3 remains RED until a later 120 s / 12,000-step R3 Requalification 3 returns and is adjudicated PASS.
