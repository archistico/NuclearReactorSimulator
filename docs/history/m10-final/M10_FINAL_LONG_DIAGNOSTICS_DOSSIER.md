# M10 FINAL LONG DIAGNOSTICS DOSSIER
> Historical consolidation dossier. The source documents below were completed/superseded and had no executable references at consolidation time. Their content is retained here for provenance while the individual top-level files are removed.
## Source manifest
| Original top-level file | Lines | SHA-256 (normalized LF UTF-8) |
| --- | ---: | --- |
| `M10_FINAL_EXACT_V9_PRODUCTION_ACTIVATION_CANDIDATE.md` | 108 | `42E048067D207F710BF19A8DA616C00F7BE632F4F780F869C54817C535672287` |
| `M10_FINAL_EXACT_V9_PRODUCTION_ACTIVATION_DECISION.md` | 119 | `19194E898F25072769F216113B161A19C10344CAA2E33A7A7633EA94480FAA5E` |
| `M10_FINAL_LONG_FAILURE_DIAGNOSTIC1.md` | 65 | `C5E88F6AC6BE1534989205ED99698AC8B61CCDACDF63F3C7EC812654F1F46027` |
| `M10_FINAL_LONG_FAILURE_DIAGNOSTIC2.md` | 153 | `B82FCF062E5037A5A4033CD88627E929D8D756A4C4323770B33038C1AC86D2F7` |
| `M10_FINAL_LONG_FAILURE_DIAGNOSTIC3.md` | 214 | `4A0B873AE6DF1906CB0065892DA3AB811E667F94C1F5FBB28ED191985E53349B` |
| `M10_FINAL_LONG_FAILURE_DIAGNOSTIC4.md` | 185 | `23593E67D8EAF0A120B4A46AA6AC580E9E3714DDAFD7164AC9E4073974E6B0FD` |
| `M10_FINAL_LONG_FAILURE_DIAGNOSTIC5.md` | 66 | `0E6AB5071A5D1EC923263134895075A9B6D77A33AC88E53FFBEB58D0B908DE7A` |
| `M10_FINAL_LONG_FAILURE_DIAGNOSTIC6.md` | 168 | `3C21A6E29B7C761FAFA408E8BC92F340D47CA5D17FDAECC84A73D268B7C0EFCA` |
| `M10_FINAL_LONG_FAILURE_DIAGNOSTIC7.md` | 100 | `BEBBA8DACD949E252CC7DE859AE940FBF36C01AFD9B91EB57B078684EB4075DC` |
| `M10_FINAL_LONG_FAILURE_DIAGNOSTIC8.md` | 105 | `67739230DCCB14EB5D872F7216880C084B10865ADE0651B50F33190C513CFA90` |
| `M10_FINAL_LONG_FAILURE_DIAGNOSTIC9.md` | 91 | `A767F5E9DD9F23AA82433BB6E810D308F831BC974209E95580C8046FDB49E411` |
| `M10_FINAL_LONG_FAILURE_DIAGNOSTIC10.md` | 130 | `0C18324F2027F42D6D46BF8698986DC0FBAF3025EE807DD241FCC9882D347357` |
| `M10_FINAL_LONG_FAILURE_DIAGNOSTIC11.md` | 160 | `C1F08511C4D2C5C965BC7B182D636EB1B18DD74FD0C99039A007057F73CDC9F7` |
| `M10_FINAL_LONG_VALIDATION_EXECUTION.md` | 51 | `2D9BCE0F3DAD36EED583FF378A75BDDE1894FC0F31603BD98615AA1281BFAF26` |

## Retained source snapshots

---

## Source snapshot — `M10_FINAL_EXACT_V9_PRODUCTION_ACTIVATION_CANDIDATE.md`

### M10 Final — Exact-v9 Qualified Production Activation Candidate

**CANDIDATE — exact-v9 is engineering QUALIFIED by returned Diagnostic 11 Hotfix 2 evidence; exact-v4 remains authoritative production; exact-v9 is staged only as an explicit opt-in production policy; replacement long remains unauthorized.**

> **Returned result:** this opt-in activation-candidate gate is now locally validated GREEN. Its returned evidence authorizes only the separate authoritative activation-decision candidate documented in `M10_FINAL_EXACT_V9_PRODUCTION_ACTIVATION_DECISION.md`; this historical note remains the opt-in staging contract and is not rewritten as the authoritative switch.

#### 1. Qualification result entering this gate

The returned Diagnostic 11 Hotfix 2 artifacts complete 600 simulated seconds / 60,000 deterministic 10 ms steps on `integrated-operations-desktop-stable@9` with zero trip steps and zero hydraulic rollbacks.

The returned endpoint and late-window evidence are effectively stationary:

- electrical export: `4.999999982116509 MWe` at 600 s;
- primary pump/channel/return: `100.000000974 / 100.000001357 / 100.000000320 kg/s`;
- drum level: `0.4999999996725085`;
- drum final-60 mass slope: `-1.06214247e-8 kg/s`;
- maximum node pressure slope in the returned final-60 table: below `1e-4 Pa/s`;
- governor integral slope: `+2.22552684e-11 %/s`;
- governor output / control-valve slope: about `-3.8877e-10 %/s`;
- final-60 net external / stored-energy rate: about `9.78e-8 MW` each;
- mean absolute full-energy closure residual: `1.12160695e-5 J`;
- mean absolute turbine-stage ownership residual: `3.05351664e-9 W`;
- turbine admission: `13.339237094 kg/s` total = `13.028001861 kg/s` vapor + `0.311235233 kg/s` moisture drain.

The exact-v9 operating point is therefore **QUALIFIED** for activation staging. This qualification accepts the authored operating point and the already validated governor/moisture-drain semantics; it does not itself switch production.

#### 2. Why this gate does not switch the default

The first failed long campaign exposed two structural defects and one operating-point mismatch. Those are now repaired and exact-v9 is qualified, but deployment selection is a separate contract involving:

- authoritative/default policy resolution;
- exact-version registry availability in the desktop app composition root;
- scenario/training identity;
- fail-closed explicit rollback;
- deterministic equivalence between direct factory construction and policy-path construction;
- preservation of current exact-v4 evidence until the activation decision is explicitly promoted.

Following the existing H.29 -> H.30 activation pattern, this milestone therefore adds an **opt-in qualified policy** but leaves `AuthoritativeDefaultPolicy` on exact-v4.

#### 3. New staged policy

The candidate adds:

`DesktopHydraulicProductionPolicy.M10FinalExactV9QualifiedCandidate`

resolved by:

`DesktopHydraulicProductionPolicySelector.M10FinalQualifiedCandidatePolicy`

Its exact initial condition is:

`integrated-operations-desktop-stable@9`

Its replayable training scenario identity is:

`integrated-normal-operations-training-m10-final-v9-activation-candidate`

The desktop composition root registers the exact-v9 factory so the identity is resolvable by scenario/archive workflows.

The authoritative default remains:

`integrated-operations-desktop-stable@4 | I5RepairedFourNodeCorrectedCommit`

The explicit kill/rollback remains:

`integrated-operations-desktop-stable@2 | ExplicitCommittedState`

#### 4. Candidate gate

Run:

```bat
scripts\run-m10-final-v9-production-activation-candidate.cmd
```

The script performs, in order:

1. restore + Debug build with warnings-as-errors;
2. complete ordinary suite;
3. LR-M1 Hotfix 1 semantic-equivalence regression;
4. current-evidence suite with exact-v4 still authoritative;
5. exact-v9 600 s Diagnostic-11 requalification on the activation-candidate source tree;
6. exact-v9 policy-path activation-candidate audit.

The final focused audit runs 120 simulated seconds through the production-policy selector, requires no trip/breaker-open/rollback/fallback/unsafe/untargeted-disagreement observations, keeps the existing conservation ceilings, verifies the moisture-drain owner, and requires the selector-path deterministic fingerprint to equal direct exact-v9 factory construction.

The frozen activation-candidate contract is `eng/m10-final-v9-production-activation-candidate-contract.json`.

#### 5. Required returned artifacts

Return the complete:

`artifacts\m10-final-v9-production-activation-candidate`

containing:

- `00-progress.txt`;
- `01-v9-production-activation-candidate.summary.txt`;
- `02-selector-matrix.csv`;
- `03-activation-candidate-contract.json`.

The Diagnostic-11 artifact folder produced by step 4 is also retained locally as prerequisite evidence.

#### 6. Decision after the gate

A green candidate gate authorizes **only** a separate production-activation decision candidate that changes the authoritative default from exact-v4 to exact-v9 and deliberately rebinds current production scenario/mission identities where required.

It does not authorize the replacement long directly. The replacement-long contract and workload are created only after the authoritative exact-v9 activation itself is validated.

---

## Source snapshot — `M10_FINAL_EXACT_V9_PRODUCTION_ACTIVATION_DECISION.md`

### M10 Final — Exact-v9 Authoritative Production Activation Decision 1

**CANDIDATE — the preceding exact-v9 opt-in production-policy gate is validated green. This candidate deliberately switches the desktop authoritative default to exact-v9 and advances the current production mission binding to `bounded-demand-following-5-10-5@3`, while preserving exact-v4/exact-v3/exact-v2 and mission-pack `@2`/`@1` as immutable historical identities. Replacement long remains unauthorized.**

> **Hotfix 1 status:** the original candidate was BUILD RED before the ordinary suite because its new focused test declared the canonical nullable decimal mission score as `double` and omitted the `Application.Scenarios.Recording` import required by `ControlRoomSnapshotFingerprint`. Hotfix 1 fixes only those two test-contract compile defects. All activation-decision runtime/source semantics documented below are unchanged.

#### 1. Prerequisite evidence

Diagnostic 11 Hotfix 2 qualified exact-v9 over 600 simulated seconds with effectively stationary whole-cycle behavior around 5 MWe and 100 kg/s, negligible inventory/governor drift, zero trip/rollback and conservative mass/energy ownership.

The returned exact-v9 qualified opt-in production-activation gate then validated the real selector path for 12,000 steps:

- electrical range `4.9999999795104797..4.999999999572232 MWe`;
- primary-pump range `99.999999968963579..100.00000021068621 kg/s`;
- drum-level range `0.49999999993197591..0.50000000007990097`;
- governor-output range `29.281329614794107..29.281329977118531 %`;
- minimum moisture drain `0.31123523307475764 kg/s`;
- maximum commanded-transfer mismatch `0 kg/s`;
- maximum stage energy-ownership residual `1.1175870895385742e-8 W`;
- maximum network mass closure `2.1827872842550278e-11 kg`;
- maximum network energy closure `4.5662359334528412e-5 J`;
- zero corrected trigger/commit, rollback, fallback violation, unsafe commit and untargeted disagreement;
- selector equals direct factory over 128 deterministic steps;
- fingerprint `7880AD580179B936C584EB0055BE663E0A1CFA65C5191B0DB8A7F3C514DB5418`.

That gate explicitly returned `production-activation=False`; its purpose was to prove that the deployment path is ready for this separate decision.

#### 2. Proposed authoritative switch

Within this candidate source tree:

`DesktopHydraulicProductionPolicySelector.AuthoritativeDefaultPolicy`

resolves:

`M10FinalExactV9QualifiedCandidate -> integrated-operations-desktop-stable@9`.

A distinct authoritative production scenario is introduced:

`integrated-normal-operations-training-m10-final-v9-production`.

The earlier activation-candidate scenario:

`integrated-normal-operations-training-m10-final-v9-activation-candidate`

is retained separately and is not reinterpreted.

Historical deployment identities remain explicitly selectable:

- exact-v4 — `I5RepairedProductionPolicy`;
- exact-v3 — `H29ActivationCandidatePolicy`;
- exact-v2 — `ExplicitRollbackPolicy` / fail-closed kill.

No exact-version factory, governor/moisture-drain physics, operating-point constant or first-long frozen manifest is modified by the activation decision.

#### 3. Production mission rebinding

The historical production pack remains:

`bounded-demand-following-5-10-5@2 -> historical exact-v4 production scenario`.

The new current production pack is:

`bounded-demand-following-5-10-5@3 -> exact-v9 authoritative production scenario`.

Version `@3` changes only the exact composed scenario binding. Objective, external demand profile, scoring policy, evaluator, logical-time contract, assistance contract and score-evidence bindings are inherited unchanged from `@2`.

Historical M10.9.8 and failed-long tests that intentionally describe exact-v4 evidence are pinned explicitly to pack `@2`; they no longer follow the symbolic current production pack.

#### 4. Historical evidence preservation

The switch is intentionally accompanied by test/evidence pinning. Tests whose purpose is Phase-I exact-v4, H.29/H.30 exact-v3, or the first failed exact-v4 long now name those exact policies rather than `AuthoritativeDefaultPolicy`.

This prevents a current-default change from silently converting historical validation into exact-v9 evidence.

The frozen first-long source manifest remains provenance only. It must not be reused as the replacement-long baseline because the authoritative source tree has intentionally changed.

#### 5. Validation gate

Run:

```bat
scripts\run-m10-final-v9-production-activation-decision.cmd
```

The script performs:

1. restore + Debug build with warnings-as-errors;
2. complete ordinary suite after the proposed switch;
3. LR-M1 Hotfix 1 semantic-equivalence regression;
4. exact-v9 600 s Diagnostic-11 requalification on the switched source tree;
5. focused authoritative exact-v9 selector/scenario/mission-v3 audit;
6. post-switch cumulative current-evidence routing.

The focused audit additionally executes 12,000 authoritative health steps, checks exact-v2 fail-closed rollback, compares selector construction with the qualified direct exact-v9 fingerprint, and runs 1,200 logical steps of the current production mission `@3`.

The frozen gate contract is:

`eng/m10-final-v9-production-activation-decision-contract.json`.

#### 6. Required returned artifacts

Return the complete:

`artifacts\m10-final-v9-production-activation-decision`

containing:

- `00-progress.txt`;
- `01-v9-production-activation-decision.summary.txt`;
- `02-selector-matrix.csv`;
- `03-mission-pack-matrix.csv`;
- `04-activation-decision-contract.json`.

#### 7. Decision after this gate

Only a complete green result promotes exact-v9 from qualified opt-in to authoritative production.

Even after that promotion, the replacement long is a separate evidence campaign. The next candidate must freeze a **new exact-v9 production baseline manifest** and a redesigned replacement-long contract/workload; the failed exact-v4 long manifest is not reused or rewritten.

---

## Source snapshot — `M10_FINAL_LONG_FAILURE_DIAGNOSTIC1.md`

### M10 Final Long Failure Diagnostic 1

#### Status

The first M10 final long campaign is **FAILED / ABORTED AFTER EVIDENCE COLLECTION**. LR-H1 failed with a production-path `WaterSteamStateOutOfRangeException` at node `outlet`. LR-M1 was manually stopped after reaching logical step 360000 / 440000 because wall cost had become operationally unacceptable.

This diagnostic does **not** modify production runtime, thermodynamic support, I.3 budgets, conservation ceilings, exact-version identities or the failed long evidence.

#### Evidence from the aborted campaign

LR-H1 produced:

- node: `outlet`;
- `v = 0.0026153411609661885 m^3/kg`;
- `u = 1615124.4119888516 J/kg`;
- one unsupported-envelope excursion;
- no preceding unexpected trip/fault in the preserved classifier artifact.

The progress artifact contains only the 300 s LR-H1 checkpoint before failure, therefore the failure occurred after 300 s but before the next 600 s checkpoint. The exact failing logical step was not persisted by the original harness.

LR-M1 reached 3600 / 4400 simulated seconds. Equal 300 s simulated chunks became progressively more expensive, rising from about ten minutes early in the leg to about thirty-six minutes for the 3300->3600 s chunk. This is not a constant per-step cost.

#### Static LR-M1 root-cause finding

The current live MISSION read path contains deterministic full-prefix scans at every `SingleStep` presentation:

1. `ControlRoomRuntimeCoordinator.Dispatch(SingleStep)` publishes both `DeterministicStepCompleted` and `SnapshotChanged` for every logical step.
2. `MissionPerformanceLiveSnapshotSource.OnDeterministicStepCompleted` appends one `ExternalEnergyDemandEvidenceSnapshot` per step to `_demandTimeline`.
3. `OnPresentationSnapshotChanged -> RefreshLocked -> BuildCurrent` runs on every step.
4. `OperationalChallengeScoreEvidenceProjector.ProjectLive` validates strict ordering by iterating the complete timeline, then `Demand(...)` filters/materializes the complete timeline and calculates aggregate averages.
5. `MissionPerformanceTimelineProjector.AddDemandChanges` again iterates the complete demand timeline even though the retained presentation output needs only demand change points.

Therefore step `n` performs O(n) prefix work and a long session performs O(n^2) aggregate projection work. The observed increasing 300 s chunk times are consistent with this code path.

This finding classifies the LR-M1 wall-cost issue as an **Application/MISSION live-projection scalability defect**, not a plant-physics cost increase. A production correction is not yet included in Diagnostic 1; exact live/replay scoring and timeline semantics must be preserved by an incremental replacement.

#### Diagnostic 1 goals

##### A. LR-H1 300 s equilibrium residual census

Replay only the already-validated 300 s exact-v4 domain and collect every second for every canonical fluid node:

- mass;
- internal energy;
- specific internal energy;
- specific volume;
- pressure;
- temperature;
- phase / vapor quality.

For the final 60 s, calculate node-by-node linear slopes. For `outlet`, preserve the distance from the observed LR-H1 failure coordinates. The purpose is to determine whether the pre-failure state already contains a secular drift toward the later unsupported point.

##### B. LR-M1 projector prefix-scaling census

Do not run the plant for thousands of seconds. Feed deterministic synthetic demand histories of increasing lengths through the current score and timeline projectors and record wall cost/allocation. This isolates the already identified prefix-rescan path from plant physics and challenge command behavior.

#### Next decision

After Diagnostic 1 artifacts are available:

- if LR-H1 shows clear secular `outlet`/inventory drift inside 240..300 s, proceed to the minimum equilibrium/owner residual census needed to identify the source term or controller bias before modifying physics;
- if the 300 s state is effectively flat but close to the failure point, investigate thermodynamic support / late branch transition and hydraulic-coupling behavior;
- for LR-M1, design an incremental live evidence accumulator/change-point projection that is exactly equivalent to current score/timeline semantics, then prove equivalence against full-prefix projection before activation.

No new final long campaign is authorized until both blockers are resolved. The replacement final long validation must target 35-45 minutes on the validation workstation and must be operationally capped at 60 minutes; this wall budget is not a physics tolerance.

---

## Source snapshot — `M10_FINAL_LONG_FAILURE_DIAGNOSTIC2.md`

### M10 Final Long Failure Diagnostic 2 / LR-M1 Hotfix 1

#### Status

**PASS / EVIDENCE ACCEPTED on 2026-08-23 — LR-M1 production read-side scalability correction validated locally; LR-H1 diagnostic owner correlation completed.**

This package was stacked on **M10 Final Long Failure Diagnostic 1**. The user reported build, complete ordinary tests and Diagnostic-2 focused execution passing and returned the complete generated artifact folder. Diagnostic 2 is therefore accepted as the validated overlay for LR-M1 and as diagnostic evidence for LR-H1; it is not M10 closure evidence.

M10 remains open and M11 remains blocked.

#### 1. Diagnostic 1 conclusions

##### LR-M1 — confirmed Application scalability defect

The synthetic prefix census measured the unchanged full-prefix projectors at 100,000 demand samples as approximately:

- `OperationalChallengeScoreEvidenceProjector.ProjectLive`: **4,823 us/call**, **802,336 B/call**;
- `MissionPerformanceTimelineProjector.Project`: **3,379 us/call**, **2,592 B/call**;
- combined live projection work: approximately **8.2 ms per presentation step** before the rest of the session/runtime cost.

The output remained bounded (`recent_operational_count=1`, `timeline_count=1`) while the input prefix grew. The live path therefore performed O(n) historical work at step n and O(n^2) aggregate work over a long session.

This is now classified as:

```text
LR-M1 = APPLICATION / MISSION LIVE-PROJECTION SCALABILITY DEFECT
```

##### LR-H1 — real primary inventory redistribution already visible inside 300 s

The exact-v4 300 s census showed:

- outlet mass: `7504.571944 kg -> 4604.960897 kg`;
- final-60 s outlet mass slope: `-7.9140056 kg/s`;
- final-60 s outlet pressure slope: `-3240.1104 Pa/s`;
- final-60 s outlet specific-volume slope: `+3.3805741e-6 m3/kg/s`;
- total final-60 s node mass slopes sum to numerical zero, so this is redistribution rather than global mass loss.

Using the production primary-circulation resistance `25 Pa*s2/kg2` and the sampled 300 s pressures gives approximately:

```text
channel flow ~= 253.23 kg/s
return flow  ~= 261.07 kg/s
residual     ~= -7.85 kg/s
```

That residual closely matches the measured outlet `dm/dt`. The immediate owner is therefore the primary branch / operating-point continuity balance, not an unexplained thermodynamic exception-site loss.

The upstream reason for the moving operating point is not yet frozen: controller bias, authored initial inventory/pressure/thermal distribution, steam-drum recirculation closure or another coupled reference-point mismatch may contribute.

#### 2. LR-M1 Hotfix 1 design

The live MISSION/PERFORMANCE source no longer retains and rescans every deterministic demand sample.

A new internal `MissionPerformanceLiveDemandEvidenceAccumulator` maintains only the information needed by the already-authored semantics:

- current demand sample;
- paired sample count;
- `sum(abs(demand-output error))`;
- `sum(abs(external demand))`;
- at most 100 recent demand **change points**, matching the existing operational timeline retention bound.

The score formula is unchanged. For demand tracking:

```text
mean absolute error = sum(abs(error)) / paired sample count
mean demand         = sum(abs(demand)) / paired sample count
fraction            = 1 - clamp(mean error / mean demand, 0, 1)
```

The replay/offline full-prefix projectors remain unchanged. Only the live read-side adapter uses the incremental aggregate.

Strict logical-order enforcement moves to the incremental `Upsert` boundary, where it is O(1) per incoming sample instead of being rechecked over the complete prefix on every presentation refresh.

Timeline semantics remain unchanged because `MissionPerformanceTimelineProjector` already retains only the latest 100 operational entries. Supplying the latest 100 actual demand change points is exact: any older demand change is necessarily displaced by the 100 newer demand changes before recording/protection/scoring entries are even considered.

No command authority, challenge lifecycle, score policy, scenario identity, archive schema, replay fingerprint, physics or protection semantics change.

#### 3. LR-H1 Diagnostic 2

No production plant correction is applied in this package.

The 300 s exact-v4 route is repeated only to capture, once per simulated second:

- `outlet` mass;
- pressure-header / outlet / drum pressures;
- main-circulation pump flow;
- channel flow;
- return flow;
- channel-minus-return continuity residual;
- drum incoming return flow;
- drum recirculated-liquid flow;
- drum separated-steam flow;
- reactor-primary `flow-control` error / integral / output;
- turbine-secondary `level-control` error / integral / output.

The final 60 s report correlates outlet `dm/dt` directly with the canonical channel-return residual and records controller-integral slopes. This decides whether the next production action is primarily:

```text
A/E. reference operating-point / inventory-flow mismatch
B.   closed-loop bias materially driving that mismatch
```

Only after that evidence is returned should an exact-v5 operating-point repair or a more focused physical-owner correction be designed.

#### 4. Returned validation evidence

The returned artifacts establish:

- LR-M1 incremental semantic equivalence is `True` through 100,000 synthetic samples;
- at 100,000 samples score projection is about `3.350 us/call` / `2,248 B/call` and bounded timeline projection about `3.245 us/call` / `2,600 B/call`, with no prefix-length growth comparable to Diagnostic 1;
- exact-v4 final-60 s `outlet dm/dt = -7.9140055967720722 kg/s`;
- exact-v4 final-60 s mean `channel-return = -7.913791680400684 kg/s`;
- primary `flow-control` integral slope is exactly `0`;
- level-controller integral slope is about `-0.0003838679 /s`.

Therefore LR-M1 Hotfix 1 is accepted, and LR-H1 proceeds as a reference operating-point / authored seed problem rather than a controller-integrator problem. The next gate is `M10_FINAL_LONG_FAILURE_DIAGNOSTIC3.md`.

#### 5. Historical validation route


Run:

```bat
scripts\run-m10-final-long-failure-diagnostic2.cmd
```

It performs:

1. Debug build with warnings-as-errors;
2. complete ordinary suite;
3. focused LR-M1 incremental semantic-equivalence tests;
4. LR-M1 synthetic incremental scaling/equivalence census;
5. LR-H1 exact-v4 300 s primary branch/controller census.

The completed execution returned the complete `artifacts\m10-final-long-diagnostic2` folder. The replacement long campaign remains blocked because LR-H1 still requires a separately qualified repair/reference-point activation.

The historical `eng/m10-final-long-baseline-src.sha256` manifest intentionally remains frozen to the pre-hotfix long baseline. Because LR-M1 Hotfix 1 legitimately changes Application `src/`, the old long-validation route is not the validation route for this candidate and must **not** be rebased yet. A replacement-long contract/source manifest is authorized only after Diagnostic 2 closes LR-H1 and the resulting production candidate passes its focused/ordinary gates.

#### 6. Hard non-scope

This candidate does not:

- widen the water/steam envelope;
- clamp `outlet` state;
- modify the 19 I.3 budgets;
- modify conservation ceilings;
- change hydraulic resistance, pump head, heat-transfer coefficients or controller tuning;
- reinterpret exact-v4;
- rebind an existing exact mission/scenario identity;
- weaken protection or numerical fail-closed behavior.

If LR-H1 ultimately requires a different authored production operating point, it must be introduced as a new exact version rather than editing exact-v4 in place.

---

## Source snapshot — `M10_FINAL_LONG_FAILURE_DIAGNOSTIC3.md`

### M10 Final Long Failure Diagnostic 3 — Exact-v5 Reference Operating-Point Candidate

#### Status

**HOTFIX 1 EXECUTION PASS / EXACT-V5 ENGINEERING NOT QUALIFIED — superseded for active investigation by Diagnostic 4.**

The original Diagnostic 3 candidate failed its Debug build with one test-only CS0103 because `HydraulicNumericalCouplingMode` lacked the `NuclearReactorSimulator.Domain.Plant` import. Hotfix 1 corrected only that namespace import. The user then reported the complete Diagnostic-3 script PASS and returned the full artifact folder.

The returned 600 s run is finite, trip-free and rollback-free, but exact-v5 does **not** satisfy the engineering qualification rule written by this document: drum inventory/level and common-mode pressure/thermal state remain materially monotonic. Exact-v5 therefore stays diagnostic-only; exact-v4 remains the authoritative production default.

M10 remains open. M11 remains blocked. The replacement long campaign is still **not authorized**. See `M10_FINAL_LONG_FAILURE_DIAGNOSTIC4.md` for the next evidence step.

#### 1. Diagnostic 2 conclusions

##### LR-M1 — Hotfix 1 accepted

The incremental live MISSION path preserved semantic equivalence for every synthetic prefix size through 100,000 samples while removing prefix-dependent cost. At 100,000 samples the returned census measured approximately:

- incremental score projection: **3.35 us/call**, about **2,248 B/call**;
- bounded timeline projection: **3.245 us/call**, about **2,600 B/call**;
- recent demand changes retained: **1** for the constant-demand synthetic case;
- `semantic_equivalence=True`.

The same measurements remained in the same microsecond/allocation class from 1,000 through 100,000 samples. LR-M1 is therefore accepted as an Application read-side repair: the live refresh is O(1) with respect to elapsed sample count, while replay/offline full-prefix semantics remain unchanged.

##### LR-H1 — controller bias is not the immediate driver

The exact-v4 300 s controller/branch census confirmed the Diagnostic-1 inventory result with canonical flow telemetry. Over the final 60 s:

- `outlet dm/dt = -7.9140055968 kg/s`;
- mean `channel - return = -7.9137916804 kg/s`;
- residual slope is only about `+0.002029 kg/s2`;
- the reactor-primary `flow-control` controller remains **Manual**, output **100%**, integral slope exactly **0**;
- the steam-drum level-controller integral slope is only about `-0.0003839 /s`; its output falls as the already-existing drum level excess grows.

At 300 s the canonical branch values are approximately:

```text
pump    253.272 kg/s
channel 253.227 kg/s
return  261.076 kg/s
residual -7.849 kg/s
```

The near one-to-one agreement between outlet inventory loss and channel-return residual means the immediate owner is the **authored primary reference operating point / branch continuity balance**. The primary flow controller is not integrating the plant away from equilibrium; the level controller is primarily reacting to the redistribution.

This does **not** prove that 253–260 kg/s is the asymptotic plant equilibrium. The observed exact-v4 flow is still moving at 300 s. Diagnostic 3 therefore introduces an explicitly authored **reference-point probe**, not a claimed solved steady state.

#### 2. Why exact-v5 instead of editing exact-v4

Exact-v4 is already authoritative replay/provenance and must remain immutable. Any materially different initial inventory/pressure/thermal distribution therefore receives a new exact identity:

```text
integrated-operations-desktop-stable@5
```

Diagnostic 3 does not register @5 as the production selector. Authoritative production remains:

```text
integrated-operations-desktop-stable@4
CorrelationConsistentInverseDomain
FourNodeBranchContinuityCorrectedCommitOptIn
10 ms
```

The new factory is instantiated only by the focused diagnostic route.

#### 3. Exact-v5 reference-point construction

The probe chooses **260 kg/s** as a round diagnostic reference inside the late Diagnostic-2 operating region. This is a test point, not a frozen calibration target.

The existing production hydraulic coefficients remain unchanged:

```text
channel resistance = 25 Pa*s2/kg2
return resistance  = 25 Pa*s2/kg2
pump pipe resistance     = 25 Pa*s2/kg2
pump internal resistance = 25 Pa*s2/kg2
rated pump head = 1.0 MPa
```

At 260 kg/s each channel/return leg requires:

```text
DeltaP = R q^2 = 25 * 260^2 = 1.690 MPa
```

With the unchanged 280 °C drum saturation pressure of about `6.416459 MPa`, the authored pressure grade is therefore:

```text
drum     6.416459 MPa
outlet   8.106459 MPa
pressure 9.796459 MPa
suction 12.176459 MPa
```

The pump relation includes both its pipe and internal resistance (`50 Pa*s2/kg2` total), so the suction pressure is selected such that the unchanged 1 MPa active boost also resolves to the same 260 kg/s flow.

The pressure-header and suction nodes remain 280 °C subcooled liquid; only their density/compression seed is changed to represent that pressure grade. The outlet is seeded as a saturated mixture at `8.106459 MPa` with diagnostic quality `0.0358817429` rather than as the old 280 °C compressed-liquid copy.

The existing initial fission power remains **30 MW**. With current-v2 thermal coupling, the unchanged heat split/conductances imply the matching solid-body seed temperatures:

```text
outlet saturation temperature ~= 295.934 °C
fuel     ~= outlet + 21 K = 316.934 °C
structure ~= outlet +  6 K = 301.934 °C
```

No heat-transfer coefficient, fission-power coefficient, hydraulic resistance, pump head, controller tuning, thermodynamic envelope or acceptance tolerance is changed.

#### 4. Additive seed seam

`ColdShutdownInitialConditionFactory.CreateRuntimeEngineForOperationalSeed` receives optional seed-only overrides for:

- suction compression fraction;
- pressure-header compression fraction;
- outlet saturation pressure + vapor quality as a pair;
- fuel temperature;
- structure temperature.

All defaults preserve the pre-existing recipe path. Exact-v1..v4 call sites do not supply the new arguments, so their authored semantics are not intentionally changed. The ordinary suite and the explicit production-selector assertion are required to catch any accidental regression.

#### 5. Diagnostic 3 — 600 s qualification probe

Run:

```bat
scripts\run-m10-final-long-failure-diagnostic3.cmd
```

The route performs:

1. Debug build with warnings-as-errors;
2. complete ordinary suite;
3. focused LR-M1 Hotfix-1 semantic-equivalence regression;
4. exact-v5 600 s reference operating-point census.

The new 600 s route deliberately crosses the time region in which the original exact-v4 LR-H1 failed (after 300 s and before 600 s).

Once per simulated second it records:

- outlet mass, specific volume, specific internal energy, pressure, temperature, phase and quality;
- suction / pressure-header / outlet / drum pressure grade;
- pump / channel / return flows and channel-return residual;
- drum mass and level;
- fuel and structure temperatures;
- drum level-controller error, integral and output;
- corrected-commit / rollback / trip counts.

Artifacts:

```text
40-v5-reference-trajectory.csv
41-v5-final60-summary.txt
42-v5-initial-reference-point.txt
00-progress.txt
```

Diagnostic 3 intentionally freezes **no new drift threshold** before observing the trajectory. The execution must remain finite, trip-free and rollback-free, but promotion requires engineering review of the returned final-60 s slopes. In particular, the candidate must not merely postpone the same monotonic outlet inventory drift beyond 600 s.

Return the complete:

```text
artifacts\m10-final-long-diagnostic3
```

before any production activation step.

#### 6. Returned result and decision

Diagnostic 3 completed, but the exact-v5 candidate is **NOT QUALIFIED**. The 260 kg/s instantaneous pressure-grade seed evolves to a late hydraulic regime near 103 kg/s. Branch continuity improves strongly, yet the full plant remains non-stationary.

Returned final state at 600 s:

```text
outlet mass  1425.894 kg
drum mass    7267.712 kg
drum level   0.956714
pump flow     103.196 kg/s
channel flow  103.023 kg/s
return flow   103.205 kg/s
```

Returned final-60 s slopes include:

```text
outlet mass       -0.17960 kg/s
drum mass         +0.79872 kg/s
drum level        +8.6082e-5 fraction/s
outlet pressure   -1001 Pa/s
drum pressure      -984 Pa/s
fuel temperature  -0.01063 °C/s
structure temp.    -0.01154 °C/s
```

The next step is Diagnostic 4, which leaves exact-v5 unchanged and exposes canonical drum, feedwater/steam-export and coupled energy-balance terms. No production activation is permitted from Diagnostic-3 survival alone.

#### 7. Hard non-scope

Diagnostic 3 does not:

- reinterpret or edit exact-v4;
- switch the production selector;
- widen the water/steam state envelope;
- clamp a conserved thermodynamic state;
- change the 19 frozen I.3 budgets or conservation ceilings;
- change hydraulic resistance, pump head, thermal conductance or controller gains;
- declare 260 kg/s to be a validated equilibrium target;
- authorize the replacement long campaign;
- close M10 or unblock M11.

The executable/frozen intent for this probe is recorded in `eng/m10-final-long-diagnostic3-contract.json`.

The historical `eng/m10-final-long-baseline-src.sha256` remains frozen to the failed first long campaign. It must not be rebased for this diagnostic candidate.

---

## Source snapshot — `M10_FINAL_LONG_FAILURE_DIAGNOSTIC4.md`

### M10 Final Long Failure Diagnostic 4 — Exact-v5 Full-Plant Mass / Energy Balance Census

#### Status

**CANDIDATE — Diagnostic 3 Hotfix 1 completed locally, but exact-v5 is NOT QUALIFIED for production activation.**

This package is stacked directly on the user-validated **M10 Final Long Failure Diagnostic 3 Hotfix 1** package. It does not alter exact-v5, exact-v4, production selection, physics, controller tuning or validation tolerances. It adds only a deeper evidence census over the unchanged exact-v5 600 s trajectory.

M10 remains open. M11 remains blocked. The replacement long campaign remains **not authorized**.

#### 1. Diagnostic 3 execution result versus engineering decision

The Diagnostic-3 script completed successfully: build, complete ordinary suite, LR-M1 Hotfix-1 regression and the explicit exact-v5 600 s run all passed their executable gates. That means exact-v5 is deterministic, finite, trip-free and rollback-free across the historical exact-v4 LR-H1 failure interval.

That executable PASS is **not** the same as operating-point qualification. The returned artifacts show a large monotonic redistribution before the hydraulic branch residual becomes small.

Initial exact-v5 state:

```text
outlet mass       4609.759 kg
drum mass         3918.241 kg
drum level        0.500027
pump flow          259.843 kg/s
channel flow       260.093 kg/s
return flow        259.926 kg/s
```

At 600 s:

```text
outlet mass       1425.894 kg
drum mass         7267.712 kg
drum level        0.956714
pump flow          103.196 kg/s
channel flow       103.023 kg/s
return flow        103.205 kg/s
```

The 260 kg/s pressure-grade probe therefore did what Diagnostic 3 was designed to test: it demonstrated that **instantaneous hydraulic equation matching is not sufficient to author a full-plant equilibrium seed**.

#### 2. What did improve

The primary branch continuity residual becomes small after the long transient. Over the final 60 s of Diagnostic 3:

```text
mean channel-return residual = -0.17836 kg/s
outlet mass slope             = -0.17960 kg/s
pump flow mean                ~= 103.31 kg/s
channel flow mean             ~= 103.14 kg/s
return flow mean              ~= 103.32 kg/s
```

This is a major improvement over exact-v4, where the final-60 s outlet loss and channel-return residual were both about `-7.914 kg/s` at 300 s.

Diagnostic 3 therefore confirms the earlier ownership finding: branch continuity directly explains outlet inventory drift. But exact-v5 reaches near-continuity only after moving to a very different full-plant state.

#### 3. Why exact-v5 is not qualified

The final 60 s remain materially non-stationary:

```text
drum mass slope       +0.79872 kg/s
drum level slope      +8.6082e-5 fraction/s
outlet pressure slope -1001 Pa/s
drum pressure slope    -984 Pa/s
fuel temperature slope -0.01063 °C/s
structure slope        -0.01154 °C/s
```

The final drum level is already `0.956714`. A positive level slope of this order is not a bounded half-full operating-point condition. In parallel, suction / pressure-header / outlet / drum pressures all continue a common-mode decline of roughly 1 kPa/s while the solid thermal bodies continue cooling.

Therefore the next missing evidence is no longer another guessed primary flow target. We need to decompose the remaining state drift into the canonical full-plant balance owners:

1. **drum mass balance** — incoming return + feedwater - separated steam - liquid recirculation;
2. **primary boundary mass balance** — feedwater versus steam export;
3. **secondary-cycle closure** — condensation, condensate-pump and feedwater-pump flows;
4. **coupled energy balance** — nuclear heat, condenser rejection, turbine/electrical export, pump work and stored-energy change.

#### 4. Diagnostic 4 scope

Diagnostic 4 reuses **exact-v5 unchanged** for the same 600 s horizon and records once per simulated second:

##### Primary/drum mass terms

- outlet mass;
- drum mass and level;
- pump / channel / return flows;
- channel-return residual;
- drum incoming return flow;
- separated steam flow;
- requested and actual liquid recirculation;
- recirculation inventory-limit flag;
- feedwater boundary flow;
- steam-export boundary flow;
- derived feedwater-minus-steam-export residual;
- derived drum algebraic net mass rate;
- primary total mass and primary audit mass rates.

##### Secondary mass terms

- feedwater pump flow;
- condensate pump flow;
- condenser condensation flow;
- full-thermofluid expected and accumulated external mass rates.

##### Energy/power terms

- fission, decay and total nuclear heat power;
- primary boundary net external power;
- pump hydraulic power;
- feedwater conditioning power;
- condenser heat rejection;
- turbine shaft power;
- electrical export;
- generator conversion loss;
- passive rotor mechanical loss;
- `NetReactorToGridExternalPower`;
- per-step coupled stored-energy change converted to MW;
- full energy-path closure residual;
- generator requested and actual output.

##### Thermal/common-mode state

- outlet / drum pressure;
- outlet / fuel / structure temperature;
- level-controller output.

#### 5. Diagnostic 4 artifacts

Run:

```bat
scripts\run-m10-final-long-failure-diagnostic4.cmd
```

The focused route performs:

1. Debug build with warnings-as-errors;
2. complete ordinary suite;
3. LR-M1 Hotfix-1 semantic-equivalence regression;
4. exact-v5 600 s full-plant balance census.

Return the complete:

```text
artifacts\m10-final-long-diagnostic4
```

Expected files:

```text
00-progress.txt
50-v5-full-plant-balance-trajectory.csv
51-v5-final60-balance-summary.txt
```

#### 6. Decision rule after Diagnostic 4

No new acceptance threshold is frozen by this diagnostic.

The returned evidence is used to identify the next authored degree of freedom:

- if `feedwater - steam export` accounts for drum accumulation, the next candidate must balance the secondary mass boundary / level-control bias rather than alter primary hydraulics;
- if drum algebraic mass rate is dominated by return/recirculation mismatch, the steam-drum recirculation/reference state remains the owner;
- if coupled stored energy is materially negative while conservation residuals remain small, the next candidate must solve the thermal/full-plant operating point rather than merely match hydraulic pressure drops;
- only after both mass and energy owners are quantitatively identified may a new exact seed be authored.

Exact-v5 must **not** be promoted merely because it survived 600 s without exception.

#### 7. Hard non-scope

Diagnostic 4 does not:

- edit exact-v4 or exact-v5 runtime semantics;
- add exact-v6;
- switch the production selector;
- change primary flow target, resistance or pump head;
- change drum level setpoint, feedwater-pump bias or controller gains;
- change fission power, thermal conductances or heat capacities;
- widen the thermodynamic state envelope;
- change I.3 budgets or conservation ceilings;
- authorize the replacement long campaign;
- close M10 or unblock M11.

The historical first-long manifest remains frozen. Diagnostic 4 is evidence collection only.

---

## Source snapshot — `M10_FINAL_LONG_FAILURE_DIAGNOSTIC5.md`

### M10 Final Long Failure Diagnostic 5 — Exact-v5 Whole-Cycle Authored-State Owner Census

**CANDIDATE — evidence-only; exact-v5 remains NOT QUALIFIED; exact-v4 remains production; replacement long unauthorized.**

#### 1. Why Diagnostic 5 exists

Diagnostic 4 completed successfully and closed the two high-level conservation questions without justifying a new operating-point seed.

The returned 540–600 s evidence shows that the model is conservative, but exact-v5 is not a full-plant equilibrium:

- measured drum inventory slope is about `+0.79872 kg/s`;
- the correct M4.4 closed-cycle drum balance is `return + internal feedwater pump - separated steam - recirculation`, not the legacy M3 primary feedwater boundary;
- `return - recirculation` contributes only about `+0.01270 kg/s`;
- `internal feedwater pump - separated steam` contributes about `+0.78540 kg/s`, approximately 98.4% of the drum accumulation;
- the legacy primary feedwater boundary is zero by design in the M4.4 closed secondary cycle and must not be used as the physical feedwater owner;
- full energy-path closure remains essentially exact: mean late `NetReactorToGridExternalPower` and coupled stored-energy change are both about `-2.477 MW`, while the full closure residual is microscopic.

Therefore the remaining problem is not hidden numerical mass/energy creation. The authored cycle inventories are relaxing toward a different secondary-cycle state while the drum level controller is compensating.

#### 2. Why an exact-v6 seed is still premature

A seed that changes only primary pressure, primary flow, drum level or the level-controller bias is incomplete.

The feedwater pump is a pressure-source pump between `feedwater-inventory` and `drum`. During the exact-v5 transient the feedwater inventory itself becomes pressurized. A new controller bias derived from the 600 s pump output cannot be transplanted into the original low-pressure 40 °C feedwater-inventory seed and still represent the same hydraulic point.

The same applies to the remainder of the closed cycle: `steam`, staged steam-path inventories, `exhaust`, `hotwell` and `feedwater-inventory` all participate in the mass/energy operating point.

Diagnostic 5 therefore records the complete authored thermofluid state before any exact-v6 exists.

#### 3. Scope

Diagnostic 5 reuses exact-v5 unchanged for the same 600 s horizon and records once per simulated second:

- the corrected drum mass balance using the internal M4.4 feedwater-pump flow;
- feedwater/condensate pump flow, effective speed and feedwater active pressure boost;
- level and hotwell controller error, integral and output;
- condenser condensation flow and the existing full energy-path terms;
- node-level mass, specific internal energy, pressure, temperature, phase and vapor quality for `suction`, `pressure`, `outlet`, `drum`, `steam`, `header`, `stop-out`, `control-out`, `turbine-inlet`, `exhaust`, `hotwell`, and `feedwater-inventory`;
- final-60 s node slopes for mass, pressure, temperature and specific internal energy.

No new operating-point value is declared by this diagnostic.

#### 4. Artifacts

The run writes:

```text
artifacts\m10-final-long-diagnostic5
  00-progress.txt
  60-v5-whole-cycle-owner-trajectory.csv
  61-v5-node-state-trajectory.csv
  62-v5-final60-node-slopes.csv
  63-v5-whole-cycle-owner-summary.txt
```

#### 5. Decision rule

After the artifacts are returned, an exact-v6 may be authored only if the evidence permits a mutually coherent initialization of the primary loop, drum, steam path, condenser/hotwell/feedwater train, controller biases and thermal inventories.

The seed must preserve conservation owners and may not reinterpret the zero legacy primary feedwater boundary as zero physical feedwater flow.

If significant node-state drift remains unresolved, the next step remains diagnostic rather than production activation.

#### 6. Hard non-scope

Diagnostic 5 does not modify exact-v4 or exact-v5 runtime semantics, create exact-v6, switch the production selector, change controller gains, change hydraulic resistance or pump head, widen the thermodynamic envelope, alter I.3/conservation budgets, authorize a replacement long, close M10 or unblock M11.

---

## Source snapshot — `M10_FINAL_LONG_FAILURE_DIAGNOSTIC6.md`

### M10 Final Long Failure Diagnostic 6 — Exact-v6 Analytical Whole-Cycle Equilibrium Candidate

**CANDIDATE — exact-v6 is diagnostic/qualification-only; exact-v4 remains production; replacement long unauthorized.**

#### 1. Evidence inherited from Diagnostic 5

Diagnostic 5 completed successfully and returned the full whole-cycle owner census for exact-v5. It confirms that exact-v5 survives 600 s but remains a transient rather than an equilibrium:

- the primary loop moves from the authored ~260 kg/s probe toward ~103 kg/s;
- the drum reaches ~0.9567 level and remains overfilled;
- the liquid secondary side is close to mass closure by 600 s, with condensate-pump and condenser-condensation flows both near 12.97 kg/s;
- feedwater remains slightly above separated steam and the historical drum overfill is largely accumulated transient inventory;
- multiple pressure nodes still drift at about the kPa/s scale, so the 600 s snapshot must not be copied as a seed;
- full mass/energy ownership remains conservative.

Diagnostic 6 therefore authors a new exact-version state from the unchanged model equations rather than from a terminal transient snapshot.

#### 2. Correction to the exact-v5 260 kg/s interpretation

The earlier shorthand that the 260 kg/s probe had “omitted pump internal resistance” is incorrect and is superseded by this document.

Exact-v5 already used the main pump path resistance and internal resistance. Its actual defect was the authored pressure reservoir between `suction` (~12.176 MPa) and `drum` (~6.416 MPa). Because steam-drum liquid recirculation transfers liquid from the drum inventory to the suction header, a stationary authored state cannot rely on that ~5.76 MPa suction/drum separation remaining indefinitely.

For a stationary candidate we set:

```text
P_suction = P_drum
```

and close the unchanged hydraulic equations:

```text
pump path resistance      25 Pa·s²/kg²
pump internal resistance  25 Pa·s²/kg²
channel resistance        25 Pa·s²/kg²
return resistance         25 Pa·s²/kg²
rated pump head             1,000,000 Pa

1,000,000 = (25 + 25 + 25 + 25) q²
q = 100 kg/s
```

This yields the authored primary pressure grade:

```text
suction/drum  6.416459281680372 MPa
pressure      6.916459281680372 MPa
outlet        6.666459281680372 MPa
```

The pressure-header liquid state includes the pump work, and the outlet state includes the unchanged core heat deposition.

#### 3. Secondary mechanical operating point

At synchronous 3000 rpm, 5 MWe requested electrical output and 98% generator efficiency require:

```text
5 / 0.98 = 5.102040816 MW generator mechanical input
+ 0.500000000 MW passive rotor loss
= 5.602040816 MW turbine shaft power
```

The unchanged turbine work model is capped at:

```text
500 kJ/kg nominal × 0.86 efficiency = 430 kJ/kg
```

so the authored secondary throughput is:

```text
q_secondary = 5.602040816 MW / 430 kJ/kg
            = 13.028001898433793 kg/s
```

The unchanged steam-source, main-steam-line, valve and turbine-expansion resistances then determine the pressure grade. The control-valve opening required by that grade is ~27.3123%, close to the historical loaded bias but derived from the current model rather than copied from it.

#### 4. Condenser and feedwater closure

The condenser exhaust temperature is solved from the unchanged UA relation using the same 13.0280 kg/s throughput, 20 °C cooling-water boundary and turbine exhaust enthalpy:

```text
T_exhaust = 42.1258335170 °C
P_exhaust = 8263.444140 Pa
Q_condenser ≈ 27.104146 MW
```

The hotwell is seeded as saturated liquid at the same pressure. The unchanged 42% condensate-pump bias then determines the feedwater-inventory state. The feedwater-pump speed required to deliver the same 13.0280 kg/s into the 6.416459 MPa drum is ~96.8891%.

Thus the initial level and hotwell controller outputs are bumpless at the analytically closed flows instead of being copied from the late exact-v5 transient.

#### 5. Nuclear/thermal closure

With the unchanged component equations and 5 MWe export, 30 MW is not the stationary whole-cycle heat input. Closing steam enthalpy rise, primary-pump work and feedwater-pump work gives:

```text
fission power = 32.48425387176408 MW
neutron population relative = 0.3248425387176408
```

The current 70% fuel / 10% structure / 20% direct-coolant deposition model and unchanged conductances then give:

```text
outlet saturation temperature  282.5453255101 °C
outlet quality                    0.2118679002
fuel temperature                305.2843032203 °C
structure temperature           289.0421762844 °C
```

This is an authored-state change only. No heat-transfer coefficient, turbine efficiency, pump head, valve resistance, condenser UA, control gain, thermodynamic envelope or conservation budget is changed.

#### 6. Exact-version and production rules

Diagnostic 6 adds the distinct exact-version identity:

```text
integrated-operations-desktop-stable@6
```

The following remain immutable:

- exact-v4 authoritative production identity;
- exact-v5 failed diagnostic identity;
- LR-M1 Hotfix 1 semantics;
- fixed 10 ms production timestep;
- corrected-commit hydraulic ownership and rollback behavior;
- CorrelationConsistentInverseDomain thermodynamic closure;
- all physical component coefficients and control gains.

The production selector remains exact-v4. Merely compiling or completing the 600 s Diagnostic-6 run does not activate exact-v6.

#### 7. Diagnostic 6 gate

Run:

```bat
scripts\run-m10-final-long-failure-diagnostic6.cmd
```

The script performs:

1. Debug build with warnings-as-errors;
2. complete ordinary suite;
3. LR-M1 Hotfix 1 semantic-equivalence regression;
4. explicit 600 s exact-v6 whole-cycle equilibrium census.

Artifacts:

```text
artifacts\m10-final-long-diagnostic6
  00-progress.txt
  70-v6-whole-cycle-equilibrium-trajectory.csv
  71-v6-node-state-trajectory.csv
  72-v6-final60-node-slopes.csv
  73-v6-whole-cycle-equilibrium-summary.txt
```

#### 8. Decision rule

The execution test requires finite evidence, zero active-trip steps and zero corrected-commit rollbacks. It deliberately does **not** invent a new numerical drift tolerance before evidence exists.

After the returned artifacts are reviewed, exact-v6 may advance only if the whole cycle is demonstrably bounded: primary and secondary mass flows remain mutually coherent, drum/hotwell inventories remain bounded, pressure/temperature slopes no longer show the material monotonic drift seen in exact-v5, approximately 5 MWe operation is preserved, and the existing full energy-path closure remains conservative.

If those conditions are not met, exact-v6 remains failed diagnostic evidence and the next step remains owner-specific diagnosis. If they are met, the next step is a **separate production-activation/requalification candidate**, not the replacement long itself.

#### 9. Hard non-scope

Diagnostic 6 does not change the production selector, widen the water/steam domain, retune hydraulic resistance, pump head, controller gains, condenser UA, turbine work, I.3 budgets or conservation ceilings. It does not authorize the replacement long, close M10 or unblock M11.

---

## Source snapshot — `M10_FINAL_LONG_FAILURE_DIAGNOSTIC7.md`

### M10 Final Long Failure Diagnostic 7 — Governor-Droop / Steam-Path Owner Census

**CANDIDATE — Diagnostic 6 execution PASS, exact-v6 engineering NOT QUALIFIED; exact-v4 remains production; no exact-v7 exists; replacement long unauthorized.**

#### 1. Returned Diagnostic 6 evidence

Diagnostic 6 completed successfully for 600 simulated seconds with zero trips and zero corrected-commit rollbacks. The full energy path remains conservative, but exact-v6 does not satisfy the engineering qualification rule.

Late evidence remains materially non-stationary:

```text
primary pressure slope          about -0.63 kPa/s
outlet mass slope               about -0.105 kg/s
drum mass slope                 about +0.378 kg/s
steam/header/stop-out mass      still decreasing
control-out/turbine-inlet mass  still increasing
mean net external power         about -1.157 MW
mean stored-energy change       about -1.157 MW
electrical export               4.9986 -> 5.2015 MWe
```

Therefore exact-v6 is retained as diagnostic evidence only and must not be activated.

#### 2. New owner hypothesis from code + returned evidence

The exact-v6 analytical steam-path pressure grade was solved for 13.0280018984 kg/s with the control valve at about 27.3123% and rotor speed authored at 3000 rpm.

The unchanged governor droop contract, however, changes the automatic speed-controller setpoint while the generator breaker is closed:

```text
nominal synchronous speed               3000 rpm
full-load droop reference rise             1.5 rpm
requested load fraction at 5/10 MWe        0.5
------------------------------------------------
effective governor setpoint             3000.75 rpm
```

Thus exact-v6 is not actually bumpless at the governor owner: it starts with a positive speed error of approximately 0.75 rpm even though the hydraulic steam-path state itself is analytically closed at t=0.

A PI/PID speed controller with non-zero integral gain can then move the control valve away from the authored 27.3123% operating point. The Diagnostic-6 inventory signature — upstream steam/header/stop-out depletion with downstream control-out/turbine-inlet accumulation — is consistent with such a change in admission resistance, but the existing artifacts did not record governor diagnostics or individual valve/stage flows.

This is a hypothesis to freeze, not yet a justification for changing the seed.

#### 3. Diagnostic 7 scope

Diagnostic 7 reruns exact-v6 unchanged for only 180 simulated seconds and records every 0.1 s:

- effective governor setpoint, measurement and error;
- P/I/D terms and governor output;
- physical control-valve position;
- rotor rpm;
- generator frequency, phase difference, mechanical input and electrical output;
- main-steam-line flow;
- stop/control/admission valve flows;
- turbine-stage commanded and effective flow;
- turbine shaft power;
- separated steam flow;
- mass and pressure for `steam`, `header`, `stop-out`, `control-out`, `turbine-inlet`.

The shorter workload is deliberate: the suspected owner begins at the first automatic-controller evaluation and is already visible well before 60 s. Another 600 s soak would add cost without improving causal separation.

#### 4. Decision rule

If returned evidence shows all of the following:

1. effective governor setpoint is 3000.75 rpm while the authored initial rotor/measurement is near 3000 rpm;
2. governor error/integral drives output and physical control-valve position away from 27.3123%;
3. the resulting stop/control/admission/stage flow mismatch corresponds in sign and timing to the observed upstream/downstream steam-path inventory transfer;

then the residual exact-v6 drift is classified as a **coupled governor/generator operating-point seed mismatch**. The next candidate may then author a distinct exact-v7 that includes the governor/generator state required for bumpless loaded operation, while preserving the already-validated controller gains and droop law.

If those conditions do not hold, no governor retuning is authorized and owner diagnosis continues.

#### 5. Gate

Run:

```bat
scripts\run-m10-final-long-failure-diagnostic7.cmd
```

The script performs:

1. Debug build with warnings-as-errors;
2. complete ordinary suite;
3. LR-M1 Hotfix 1 semantic-equivalence regression;
4. explicit 180 s exact-v6 governor/droop + steam-path owner census.

Artifacts:

```text
artifacts\m10-final-long-diagnostic7
  00-progress.txt
  80-v6-governor-steam-path-trajectory.csv
  81-v6-governor-steam-path-summary.txt
```

#### 6. Hard non-scope

Diagnostic 7 changes no production source file, initial-condition identity, controller gain, droop law, valve resistance, turbine work law, generator coupling, hydraulic coefficient, thermodynamic envelope, I.3 budget or conservation ceiling. It does not create exact-v7, switch the exact-v4 production selector, authorize the replacement long, close M10 or unblock M11.

---

## Source snapshot — `M10_FINAL_LONG_FAILURE_DIAGNOSTIC8.md`

### M10 Final Long Failure Diagnostic 8 — Exact-v7 Grid-Droop Integral-Reference Requalification

**EXECUTION PASS / ENGINEERING NOT QUALIFIED — returned 2026-08-23. Exact-v7 removes the dominant governor integral windup but remains materially non-stationary; exact-v4 remains production; replacement long unauthorized.**

#### 1. Diagnostic 7 result

Diagnostic 7 confirms the breaker-closed governor owner directly. At a 5 MWe request the effective droop reference remains 3000.75 rpm while the grid holds the rotor close to 3000 rpm. Over 120–180 s the returned evidence shows approximately:

```text
governor mean error                +0.738 rpm
Ki * mean error                    +0.01476 %/s
governor integral slope            +0.01476 %/s
governor output slope              +0.01474 %/s
physical control-valve slope       +0.01474 %/s
```

The measured integral slope therefore matches `Ki * error` essentially exactly. Steam/header/stop-out inventories decrease while control-out/turbine-inlet inventories increase in the same interval.

This cannot be repaired by choosing a different initial integral value. With the historical contract, any finite initial integral continues to accumulate because the intentional droop offset is treated as integral error while the rigid grid holds actual speed near synchronous speed.

#### 2. Versioned control-law repair

Exact-v7 preserves the exact-v6 analytical whole-cycle authored state and component/controller gains. It adds a versioned governor integral-reference mode:

```text
breaker open:
  P/I/D reference = operator speed setpoint               [unchanged]

breaker closed, historical @4/@5/@6:
  P/I/D reference = synchronous speed + load droop offset [unchanged]

breaker closed, exact-v7:
  P/D reference   = synchronous speed + load droop offset
  I reference     = synchronous speed
```

This preserves the intended droop/load characteristic in proportional response while preventing integral action from erasing the droop offset. The generic controller input gains an optional integral setpoint; null preserves historical behavior exactly.

No gain, droop magnitude, valve resistance, turbine work law, pump coefficient, grid stiffness, protection threshold, thermodynamic envelope or hydraulic mode is retuned.

#### 3. Exact-version preservation

The new mode defaults to `EffectiveDroopSetpoint`. Existing factories therefore retain historical semantics. Only the new exact-version

```text
integrated-operations-desktop-stable@7
```

opts into `SynchronousSpeedWhenParalleled`.

The authoritative production selector remains exact-v4. Exact-v5 and exact-v6 remain retained failed diagnostic evidence and are not reinterpreted as qualified operating points.

#### 4. Qualification workload

Diagnostic 8 runs exact-v7 for 600 simulated seconds at the existing 10 ms fixed step. It records:

- complete whole-cycle mass/energy balance terms;
- all primary and secondary owner-node mass, pressure, temperature and specific-energy trajectories;
- feedwater/hotwell controller state;
- governor setpoint, measurement, error, integral, output and physical control-valve position;
- primary pump/channel/return flows;
- electrical export and full energy-path closure.

Artifacts:

```text
artifacts\m10-final-long-diagnostic8
  00-progress.txt
  90-v7-whole-cycle-requalification-trajectory.csv
  91-v7-node-state-trajectory.csv
  92-v7-final60-node-slopes.csv
  93-v7-whole-cycle-requalification-summary.txt
```

#### 5. Decision rule

Exact-v7 is not automatically qualified by test completion. Returned evidence must show that the Diagnostic-7 governor drift is removed and that the whole-cycle state is genuinely bounded: near-zero late governor/control-valve drift, bounded mass/pressure/thermal slopes, stable approximately 5 MWe output, zero trips/rollbacks and conservative energy closure.

No numerical drift threshold is frozen in this candidate. If exact-v7 remains materially non-stationary, owner diagnosis continues instead of widening tolerances or activating production.

#### 6. Gate

Run:

```bat
scripts\run-m10-final-long-failure-diagnostic8.cmd
```

Return the complete `artifacts\m10-final-long-diagnostic8` folder before any production activation or replacement-long authorization.


#### 7. Returned result

Diagnostic 8 completed build, ordinary tests, LR-M1 regression and the 600 s explicit run successfully. The control-law repair is effective but insufficient for operating-point qualification. Returned evidence includes:

```text
governor output/control-valve late slope   ~ +0.000240 %/s
primary pump flow                          100.000 -> 122.698 kg/s
electrical export                           4.9986 -> 4.5644 MWe
late net external / stored-energy rate     ~ +2.402 MW
turbine-inlet late mass slope              +0.22150 kg/s
trip / rollback                            0 / 0
```

The governor drift is roughly sixty times smaller than Diagnostic 7 and is no longer large enough to explain the continuing whole-cycle motion. Exact-v7 is therefore **NOT QUALIFIED**. Diagnostic 9 continues owner diagnosis at the turbine-admission / condenser mass-transfer boundary without changing runtime semantics.

---

## Source snapshot — `M10_FINAL_LONG_FAILURE_DIAGNOSTIC9.md`

### M10 Final Long Failure Diagnostic 9 — Exact-v7 Turbine-Admission / Closed-Cycle Mass-Owner Census

**CANDIDATE — Diagnostic 8 execution PASS / exact-v7 NOT QUALIFIED; evidence-only; exact-v4 remains production; no exact-v8, production activation or replacement-long authorization.**

#### 1. Why Diagnostic 8 does not qualify exact-v7

Diagnostic 8 proves that the versioned synchronous integral reference fixes the dominant breaker-closed governor windup. Late governor/control-valve drift falls from about `+0.01474 %/s` in Diagnostic 7 to about `+0.000240 %/s`.

The whole-cycle state nevertheless remains materially non-stationary at 600 s:

```text
primary pump flow             100.000 -> 122.698 kg/s
electrical export               4.9986 -> 4.5644 MWe
late stored-energy rate        ~+2.402 MW
turbine-inlet dm/dt            +0.22150 kg/s
control-out dm/dt              +0.21362 kg/s
trip / rollback                0 / 0
```

The energy-path closure remains conservative, so widening numerical tolerances is not justified. Production activation remains blocked.

#### 2. New owner hypothesis

The current turbine stage uses `VaporMassFractionLimited` admission. In the canonical solver:

```text
stage effective mass flow = stage commanded mass flow * inlet vapor fraction
```

Only `stage effective mass flow` is removed from `turbine-inlet` and added to `exhaust`. Therefore the inlet control-volume mass balance can contain two distinct residuals:

```text
admission valve - stage commanded     hydraulic / stage-capacity residual
stage commanded - stage effective     vapor-fraction residual
---------------------------------------------------------------
admission valve - stage effective     total turbine-inlet mass residual
```

Diagnostic 7 already showed at the exact-v6 initial point `13.0277 kg/s` commanded but only `12.7107 kg/s` effective. Diagnostic 8 ends with turbine-inlet vapor quality about `0.9820` and measured late inlet mass accumulation `+0.2215 kg/s`. The implied `q*(1-x)` throughput is about `12.31 kg/s`, consistent with the observed secondary-cycle flow scale. This is strong evidence but must be frozen directly under exact-v7 before any semantic repair.

#### 3. Diagnostic 9 workload

Diagnostic 9 changes no runtime source file and reuses exact-v7 unchanged for 180 simulated seconds at the existing 10 ms step. It samples every 0.1 s:

- governor integral/output and physical control-valve position;
- admission-valve, stage-commanded and stage-effective mass flows;
- turbine-inlet vapor fraction and mass;
- `admission-commanded`, `commanded-effective`, `commanded*(1-x)` and `admission-effective`;
- condenser actual/thermal-limited condensation and exhaust mass;
- condensate-pump flow and hotwell mass;
- feedwater-pump flow and feedwater-inventory mass;
- corrected M4.4 drum mass balance and measured drum mass;
- turbine shaft power, condenser heat rejection and electrical export.

Artifacts:

```text
artifacts\m10-final-long-diagnostic9
  00-progress.txt
  100-v7-turbine-admission-mass-owner-trajectory.csv
  101-v7-turbine-admission-mass-owner-summary.txt
```

#### 4. Decision rule

If the returned late-window evidence shows:

```text
measured turbine-inlet dm/dt ~= admission - stage effective
stage commanded - stage effective ~= stage commanded * (1 - vapor fraction)
```

and the downstream exhaust/hotwell/feedwater/drum algebraic balances independently close their measured inventory slopes, the vapor-fraction-limited turbine-stage mass ownership is classified as a structural contributor to the exact-v7 drift.

That result does **not** authorize a seed-only exact-v8. A separate design decision must then choose between:

1. transporting total admitted mass through the turbine while limiting shaft work by vapor fraction;
2. adding an explicit moisture-separation/drain owner for rejected liquid; or
3. using another already-modeled physical path if evidence shows one exists.

If the closures do not match, owner diagnosis continues without changing turbine semantics. No drift tolerance is widened.

#### 5. Gate

Run:

```bat
scripts\run-m10-final-long-failure-diagnostic9.cmd
```

Return the complete `artifacts\m10-final-long-diagnostic9` folder before exact-v8, turbine-admission semantic changes, production activation or replacement-long authorization.

---

## Source snapshot — `M10_FINAL_LONG_FAILURE_DIAGNOSTIC10.md`

### M10 Final Long Failure Diagnostic 10 — Exact-v8 Turbine Moisture-Drain Ownership Requalification

**HOTFIX 1 CANDIDATE — Diagnostic 10 original ordinary suite RED on one new test-only node-balance assertion; exact-v8 runtime not yet requalified; exact-v4 remains production; no production activation or replacement-long authorization.**

#### Hotfix 1 — canonical turbine-inlet net-balance regression alignment

The original Diagnostic 10 candidate compiled, but the complete ordinary suite stopped with one failure out of 1480 tests. The failing moisture-drain regression asserted that `turbine-inlet` mass must decrease by exactly the 1 kg/s turbine transfer over the 1 ms fixture step. That assertion ignored the same-step canonical hydraulic inflow through the admission valve.

The returned value identifies the omitted owner exactly:

```text
admission-valve inflow              +2.236067978 kg/s
turbine total transferred flow       -1.000000000 kg/s
------------------------------------------------------
net turbine-inlet balance            +1.236067978 kg/s
1 ms inventory change                +0.001236067978 kg
observed final mass                   10000.001236067978 kg
```

Hotfix 1 does not alter the solver. The regression now derives the expected final `turbine-inlet` mass from the canonical admission-train snapshot and the stage `TotalTransferredMassFlowRate`. The independent exhaust-vapor and hotwell-moisture owner checks remain unchanged, as do the stage energy ownership and global mass/energy closure tolerances.

Therefore the original Diagnostic 10 result is **test-contract RED**, not evidence that the moisture-drain source-term implementation violates conservation. Exact-v8 still has no 600 s qualification evidence because the gate correctly stopped at the ordinary suite.

#### 1. Diagnostic 9 decision

Diagnostic 9 directly closes the exact-v7 turbine-inlet mass balance over the late 120–180 s window:

```text
stage commanded - stage effective                 0.2688274455 kg/s
stage commanded * (1 - inlet vapor fraction)      0.2688274455 kg/s
measured turbine-inlet dm/dt                      0.2687819297 kg/s
closure difference                               -0.0000455158 kg/s
```

The exhaust independently closes on `stage effective - condensation` to the same order. The non-vapor fraction rejected by `VaporMassFractionLimited` therefore has no downstream owner and remains in `turbine-inlet`. This is a structural contributor to exact-v7 drift and cannot be repaired by a new seed alone.

#### 2. Design choice

Diagnostic 10 does **not** restore total-mixture transport through the work-producing turbine stage. That would reintroduce the D.1 zero-work liquid bypass that `VaporMassFractionLimited` intentionally removed.

Instead a new explicit, versioned policy is introduced:

```text
VaporMassFractionLimitedWithMoistureDrain

admitted vapor      -> turbine stage -> exhaust -> condenser
rejected non-vapor  -> explicit moisture drain -> hotwell
```

The policy requires a canonical `MoistureDrainNodeId`; exact-v8 binds it to `hotwell`. Historical policies cannot name a moisture-drain node.

For saturated-mixture admission, the solver resolves saturated-vapor and saturated-liquid transport properties at the committed inlet pressure. The vapor stream alone produces shaft work; the liquid stream carries its own conservative advected energy to the drain. The stage energy audit becomes:

```text
inlet phase-resolved energy
  - vapor exhaust energy
  - moisture-drain energy
  - shaft power
= ownership residual
```

Subcooled-liquid admission produces zero work-producing stage flow and is diverted only through the explicit moisture owner. Trip blocks both stage and drain transfer.

#### 3. Versioning and preservation

Exact-v8 is:

```text
integrated-operations-desktop-stable@8
```

It preserves the exact-v7 authored whole-cycle seed and the versioned breaker-closed governor integral-reference repair. The only intended runtime semantic difference is the new turbine-admission moisture owner.

Exact-v4 remains the authoritative production selector. Exact-v4, exact-v5, exact-v6 and exact-v7 remain historical/frozen evidence and continue using their previous admission policy.

#### 4. Qualification workload

Diagnostic 10 runs exact-v8 for 600 simulated seconds at the unchanged 10 ms fixed step and records:

- whole-cycle mass/energy trajectory;
- all canonical fluid-node states;
- final-60 s node mass/pressure/temperature/specific-energy slopes;
- governor integral/output/control-valve drift;
- stage commanded flow;
- effective vapor flow;
- explicit moisture-drain flow;
- total transferred admission mass;
- stage energy-ownership residual;
- drum/feedwater/hotwell balances;
- full coupled energy closure;
- trip and rollback counts.

Artifacts:

```text
artifacts\m10-final-long-diagnostic10
  00-progress.txt
  110-v8-whole-cycle-moisture-drain-trajectory.csv
  111-v8-node-state-trajectory.csv
  112-v8-final60-node-slopes.csv
  113-v8-whole-cycle-moisture-drain-summary.txt
```

#### 5. Decision rule

Exact-v8 may be considered for a later production-activation candidate only if returned evidence shows all of the following without widening tolerances:

```text
moisture drain owns the rejected non-vapor mass
stage energy ownership remains conservative
turbine-inlet accumulation is removed rather than displaced
exhaust/hotwell/feedwater/drum inventories remain bounded
governor repair remains effective
primary/secondary pressure and thermal slopes are near stationary
electrical export remains stable near 5 MWe
trip / rollback = 0 / 0
full-cycle energy closure remains conservative
```

If mass merely migrates to `hotwell`, or if the phase-separated energy path exposes another imbalance, exact-v8 is NOT QUALIFIED and owner diagnosis continues. Production activation and replacement long remain unauthorized until a separate qualification/activation gate passes.

#### 6. Gate

Run:

```bat
scripts\run-m10-final-long-failure-diagnostic10.cmd
```

Return the complete `artifacts\m10-final-long-diagnostic10` folder before production activation, further operating-point changes or replacement-long authorization.

---

## Source snapshot — `M10_FINAL_LONG_FAILURE_DIAGNOSTIC11.md`

### M10 Final Long Failure Diagnostic 11 — Exact-v9 Post-Moisture Analytical Whole-Cycle Equilibrium

**VALIDATED — Diagnostic 11 Hotfix 2 build/ordinary suite/600 s exact-v9 requalification PASS; returned artifacts qualify exact-v9; production activation remains separate.**

#### Returned qualification result

User-returned Diagnostic 11 Hotfix 2 artifacts complete 600 s / 60,000 steps with final electrical export `4.999999982116509 MWe`, primary pump/channel/return `100.000000974 / 100.000001357 / 100.000000320 kg/s`, drum level `0.4999999996725085`, final-60 drum mass slope `-1.06214247e-8 kg/s`, governor/control-valve slope about `-3.89e-10 %/s`, late net/stored energy about `9.78e-8 MW`, mean absolute full-energy closure `1.12160695e-5 J`, zero trip steps and zero rollbacks. Exact-v9 is therefore **QUALIFIED** as the post-moisture whole-cycle operating point.


##### Hotfix 2 post-preconditioning regression correction

Hotfix 1 correctly replaced the stale historical integral range, but one assertion still treated the governor proportional contribution as the ideal pre-step value `0.75 %` with a `+/-1e-9` band. The exact-v9 factory deliberately performs 20 ms of deterministic seed preconditioning before the test reads `LatestCanonicalSnapshot`. By then the rotor measurement has changed by only a few nanorpm, but that is enough for the production expression `Setpoint - Measurement` to yield the observed `0.75000000640329745 %`.

Hotfix 2 therefore freezes the controller semantics rather than a mathematically ideal value that no longer corresponds to the sampled instant:

```text
Error = Setpoint - Measurement
P = Kp * Error, with unchanged Kp = 1
Output = UnsaturatedOutput (candidate must not be saturated)
I = UnsaturatedOutput - P - D
```

The authored exact-v9 governor/control root is still checked independently at `29.281329697436618 %` to 6 decimal places. This is not a tolerance relaxation in production physics; it is a correction of the test's temporal contract. No runtime file or authored operating-point value changes.

##### Hotfix 1 regression correction

The original Diagnostic 11 regression inherited the historical `25..28 %` governor integral range from exact-v7/v8. Exact-v9 intentionally raises the authored governor/control output to `29.2813296974 %`. With the unchanged breaker-closed droop proportional contribution of `0.75 %`, the bumpless PI preload is therefore:

```text
29.2813296974 - 0.7500000000 = 28.5313296974 %
```

The runtime produced exactly that value. Hotfix 1 replaces the stale range with a decomposition contract that checks the expected P/output/I region and `I = unsaturated output - P - D` to 9 decimal places. No runtime or authored operating-point value changes.

#### 1. Diagnostic 10 result

Diagnostic 10 Hotfix 1 completed build, ordinary suite, LR-M1 regression and the 600 s exact-v8 requalification. The structural repairs are successful:

- explicit moisture-drain ownership removes the former turbine-inlet accumulation;
- late turbine-inlet mass slope is only about `+8.4e-5 kg/s` instead of the previous `+0.22..0.27 kg/s` class;
- governor/control-valve late drift is about `-3.8e-6 %/s`;
- primary circulation remains near `99.98 kg/s`;
- global mass slope closes to numerical zero and stage/full-cycle energy ownership remain conservative.

Exact-v8 is nevertheless not the final operating point. Its late electrical export is about `4.8682 MWe` and its late net external/stored-energy rate is about `+0.2553 MW`. This is expected because exact-v8 intentionally preserved the pre-drain exact-v7 authored state while changing turbine-admission ownership.

#### 2. Why exact-v8 is off-root

The pre-drain analytical seed treated `13.0280018984 kg/s` as the complete turbine throughput. Under the validated moisture-drain policy, only vapor reaches the work-producing stage. Therefore `13.0280018984 kg/s` must instead be the **vapor** flow required to provide:

```text
5 MWe / 0.98 generator efficiency + 0.5 MW rotor loss
= 5.602040816 MW turbine shaft power

5.602040816 MW / 430 kJ/kg
= 13.028001898433793 kg/s vapor
```

The saturated-vapor drum-source enthalpy, turbine expansion resistance and condenser UA are then solved simultaneously. The resulting post-drain root is:

```text
total admission       13.339237135405003 kg/s
work-producing vapor  13.028001898433793 kg/s
moisture drain         0.311235236971211 kg/s
control valve          29.2813296974 %
```

The steam-path pressure/quality grade is recomputed from unchanged resistances and unchanged enthalpy transport. The condenser root is:

```text
exhaust temperature  42.5253661313 °C
exhaust pressure       8.438344971 kPa
condenser rejection   27.5935735108 MW
```

#### 3. Liquid-loop root

The explicit drain does not pass through the condenser. The hotwell therefore receives two conservative streams:

```text
13.0280018984 kg/s saturated condensate at exhaust pressure
0.3112352370 kg/s saturated-liquid moisture drain at turbine-inlet pressure
```

The mass-weighted enthalpy root gives:

```text
hotwell temperature   47.3356594370 °C
```

Keeping the already-authored feedwater-inventory compression root and solving the unchanged pump equations gives:

```text
condensate pump       42.9665153700 %
feedwater temperature 47.3784886658 °C
feedwater pump        96.9308268016 %
```

These are equation-derived values; the 600 s exact-v8 terminal snapshot is not copied as the new seed.

#### 4. Energy root

At the post-drain mass root the unchanged hydraulic pumps exchange about `0.2244378206 MW` with the fluid. Therefore the external first-law root is:

```text
27.5935735108 MW condenser rejection
+5.0000000000 MW electrical export
+0.1020408163 MW generator conversion loss
+0.5000000000 MW passive rotor loss
-0.2244378206 MW hydraulic pump input
=
32.9711765066 MW fission power
```

The primary remains at the already-derived `100 kg/s` root. The new fission value changes only the authored primary outlet quality and solid temperatures needed to keep the same conservative heat-transfer ownership:

```text
outlet quality       0.2151419126
fuel temperature   305.6251490647 °C
structure           289.1395608114 °C
```

No pump head, hydraulic resistance, valve law, turbine efficiency, condenser UA, controller gain, thermodynamic envelope or conservation tolerance is changed.

#### 5. Exact-version rule

Diagnostic 11 adds only:

```text
integrated-operations-desktop-stable@9
```

Exact-v8 remains frozen Diagnostic-10 evidence. Exact-v4 remains the authoritative production selector. Exact-v9 preserves the exact-v8 governor integral-reference and moisture-drain semantics; only the authored operating point changes.

#### 6. Gate

Run:

```bat
scripts\run-m10-final-long-failure-diagnostic11.cmd
```

The gate performs build with warnings-as-errors, complete ordinary suite, LR-M1 Hotfix-1 semantic-equivalence regression and a 600 s exact-v9 whole-cycle requalification.

Return:

```text
artifacts\m10-final-long-diagnostic11
  00-progress.txt
  120-v9-whole-cycle-equilibrium-trajectory.csv
  121-v9-node-state-trajectory.csv
  122-v9-final60-node-slopes.csv
  123-v9-whole-cycle-equilibrium-summary.txt
```

#### 7. Decision rule

Exact-v9 advances only if the returned evidence shows bounded primary/secondary inventories, negligible governor and turbine-inlet drift, stable operation near 5 MWe, late net-external and stored-energy rates approaching zero together, zero trip/rollback events, and conservative stage/full-cycle energy closure. No new drift tolerance is frozen in advance.

If those conditions pass, the next step is a **separate production-activation and cumulative requalification candidate**. The replacement long is still not authorized by Diagnostic 11 itself.

---

## Source snapshot — `M10_FINAL_LONG_VALIDATION_EXECUTION.md`

### M10 Final Pre-M11 Long Validation — Execution Handoff

#### Baseline

Stacked exclusively on **M10 Final Pre-M11 Cumulative Validation Hotfix 1 VALIDATED**.
The cumulative marker is frozen in `eng/m10-final-cumulative-validation-record.json`.

#### Command

From the repository root:

```bat
scripts\run-m10-final-long-validation.cmd
```

Do not run the five explicit tests individually for promotion evidence. The orchestrator validates the frozen contract, restores/builds the long test surface, executes every leg even if an earlier leg fails, and finalizes the complete artifact set.

#### Expected successful terminal markers

```text
m10-long-workload-completed=True
m10-long-simulated-seconds=14400
m10-long-logical-steps=1440000
m10-long-conservation-ceilings-pass=True
m10-long-healthy-budget-sentinels-pass=True
m10-long-numerical-coupling-safety-pass=True
m10-long-mission-pack-v2-pass=True
m10-long-degraded-recovery-pass=True
m10-long-protection-takeover-pass=True
m10-long-replay-checkpoint-sentinels-pass=True
m10-long-evidence-growth-bounded=True
m10-final-long-validation-passes=True
m10-closure-eligible=True
```

If any leg fails, M10 remains OPEN. Preserve the entire artifact directory and investigate the first physical/evidence divergence; do not widen frozen tolerances to obtain a green run.

#### First acceptance execution — current evidence

The first Hotfix-1 campaign is fail-collect and was still running when the pre-M11 documentation consolidation was prepared. LR-H1 already failed after approximately 48m54s wall-clock with:

```text
WaterSteamStateOutOfRangeException
node=outlet
v=0.0026153411609661885 m^3/kg
u=1615124.4119888516 J/kg
```

The exception originated in the canonical production path (`SimplifiedWaterSteamThermodynamicModel.Resolve` through the integrated full-plant/control runtime), so it is not currently classified as a harness-only failure. The remaining legs must be allowed to finish so the complete diagnostic artifact set is preserved.

M10 is therefore already ineligible for closure on this run. The next engineering action after campaign completion is evidence classification using `M10_LR_H1_EQUILIBRIUM_DIAGNOSTIC_PLAN.md`; no thermodynamic-envelope widening, I.3 budget retuning, conservation-ceiling retuning or workload reduction is authorized merely to obtain a pass.
