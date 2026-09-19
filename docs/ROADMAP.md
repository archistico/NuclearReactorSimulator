# Roadmap

## Live checkpoint — Diagnostic 3 REV1 Preexecution Amendment 1

<!-- NRS-MARKER:DIAG3-REV1-AMENDMENT1-NEXT -->

1. Audit Diagnostic 3 REV1 Preexecution / Planning Amendment 1.
2. If PASS-AS-AUTHORED, execute the same Diagnostic 3 REV1 test-only scenario with portable hashing and the derived counterfactual component budget.
3. Return artifacts `01`–`07` for independent adjudication.
4. Hosted `ordinary-ci` GREEN remains mandatory before any production repair planning or implementation.

No production repair, seed retuning, model-threshold change, C4/payload change, canonical exact-v9 change, R3 PASS or R4 authority is granted by this amendment.


This file contains **future work only**. The authoritative current checkpoint and active validation gate remain in `PROJECT.md`.

Detailed current execution branches are maintained directly in this roadmap. The earlier cross-milestone execution map is retained only as historical provenance in [`history/project/FORWARD_EXECUTION_PLAN_M10_9_7_TO_M15.md`](history/project/FORWARD_EXECUTION_PLAN_M10_9_7_TO_M15.md).

## Execution discipline

Resolve the active checkpoint from `PROJECT.md`; this roadmap begins only with work that still lies **ahead** of that checkpoint. The forward product sequence is:

```text
remaining M10.9.7 work
        ↓
M10.9.8 Integrated Human-Automation-HMI Validation Gate
        ↓
M11 Release Hardening
        ↓
M12 Extreme Operations Foundations
        ↓
M13 Control-Room Experience
        ↓
M14 Spatial Reactor
        ↓
M15 Accident Progression & Consequence Models
        ↓
future release train / engineering backlog
```

Rules:

1. work on one milestone at a time;
2. each implementation slice starts from the latest explicitly validated baseline only;
3. implementation begins only after its contract, non-scope and gate are documented;
4. promotion requires build + complete ordinary suite + focused gate + any stated manual HMI check;
5. a failing gate authorizes work only on the demonstrated failing contract;
6. remaining M10 work may expose or present existing physics/control/protection truth but may not create new reactor physics, component laws, protection thresholds or control ownership;
7. if operator-experience work reveals missing physics, assign it to the post-M11 engineering milestones instead of expanding M10 scope;
8. no speculative audit, retuning or numerical requalification is added after a green gate merely “for safety”.

<!-- NRS-MARKER:DIAG3-REV1-LIVE-SEQUENCE -->

## M10 Final / VR2 / R3 — live future sequence after Diagnostic 3 deep review

Current-state facts belong in `PROJECT.md`; this section records only work still ahead. The detailed frozen contract is `M10_FINAL_VR2_R3_DIAGNOSTIC3_DEEP_REVIEW_REV1_PLANNING1.md`.

1. **Diagnostic 3 REV1 implementation — COMPLETE.**
2. **Diagnostic 3 REV1 execution — COMPLETE.**
3. **Returned Diagnostic 3 REV1 adjudication — COMPLETE / `CAUSAL-CLOSURE-CONFIRMED`** — the independent IF97 counterfactual closes the suction transport seam to the frozen guards; `repair-owner=UNSELECTED`.
<!-- NRS-MARKER:DIAG3-REV1-HOSTED-CI-CONFIRMATION-NEXT -->
4. **Hosted ordinary-ci confirmation — NEXT** — deterministic ordinary is locally PASS; the same CI candidate must be GREEN on GitHub before production repair planning/implementation may begin.
5. **R3 Energy-Transport Ownership Repair Planning 1** — only when returned REV1 remains `CAUSAL-CLOSURE-CONFIRMED` **and** hosted ordinary-ci is GREEN. Compare multiple ownership families; do not retune the seed or relax thresholds.
6. **Bounded repair implementation + fast gate** — only from an explicitly selected repair plan; canonical exact-v9, C4 payload and existing R3 envelopes remain frozen.
7. **R3 Short Requalification 3** — only after the bounded fast gate is explicitly PASS.
8. **R4 Planning 1** — only after returned/adjudicated R3 PASS.

R3 is still RED and R4 remains blocked.


## Expected validation burden

This is a planning classification, not a duration promise:

| Milestone | Expected validation profile | Relative cost |
| --- | --- | --- |
| M10.9.7 | ordinary + focused presentation/replay tests + manual HMI | low/medium |
| M10.9.8 | integrated 3×3 assistance/authority matrix + degraded/protection/replay + full manual HMI | high |
| M11 | compatibility, long/performance/memory, packaging/clean-machine and release-manual gates | high |
| M12 | directionality + near-zero/conditioning + pump-energy ownership + extreme-state/decay-heat/integrity/replay qualification | high |
| M13 | UI/persistence/procedure/Instructor automated gates + broad manual HMI | medium/high |
| M14 | quasi-spatial aggregate/replay/performance qualification + manual fidelity review | high |
| M15 | consequence-family physics/replay/post-incident/long-horizon matrices | very high |

Remaining M10.9.7 work should not normally require multi-hour numerical requalification. M10.9.8 and M11 are intentionally expensive integration/release checkpoints. M12–M15 are post-release engineering milestones whose expensive validation is justified by new physical/persistent state, spatial computation or consequence progression rather than by routine UI work.

## Remaining M10.9.7 constraints

Current validated/candidate status is intentionally not duplicated here; use `PROJECT.md`. Any remaining M10.9.7 implementation must preserve these fixed ownership/navigation constraints:

- dedicated main-HMI `MISSION` workspace with contextual navigation from COMPUTER;
- Operator Computer F1–F8 unchanged and no F9;
- navigation remains presentation-only with no plant-command authority;
- presentation change detection must be explicit; generated record equality over snapshots containing `IReadOnlyList<>` is not a valid UI change detector;
- demand, requested load and actual output remain distinct;
- live projection continues to use the validated logical-step alignment and bounded recent-event contract.

Persistence follow-ups discovered during the pre-7.3 Infrastructure review have explicit future homes rather than being left as unowned deferred work:

- **M11.2 compatibility/migration hardening:** evaluate an explicit session-archive schema v2 only if string-enum persistence is desired; schema-v1 numeric enum ordinals remain frozen until such a migration exists;
- **M11.3 performance/memory gate:** evaluate stream-based persistence / `Utf8JsonWriter` only against measured save/load allocation and LOH evidence;
- low-risk scenario-definition double-parse and DTO comparer cleanup remain maintenance work and are not prerequisites for M10.9.7.3 unless a gate demonstrates a defect.

Application recording/replay follow-ups that remain future work after the active M10.9.7 checkpoint are explicitly assigned:

- **M11.2 compatibility:** if a snapshot fingerprint v2 is introduced, replay/checkpoint compatibility must select by persisted algorithm id and retain supported v1 verification; any persistent mission-pack identity requires an explicitly versioned archive contract rather than inference from `ScenarioId`;
- **M11.3 measured Application hardening:** measure `LifecycleChanged` notification cost/semantics, fingerprint JSON+SHA+hex cost, recorder read-only collection copies, long-session memory growth and recorder failure policy; never silently decimate recording v1;
- **M11.3 measured host hardening:** measure UI-thread batch/projection/PropertyChanged cost and long-session export responsiveness before any worker/off-thread redesign;
- **M13:** move command-bearing selections toward stable canonical IDs and decompose the mega-ViewModel in staged behavior-preserving slices.

## M10.9.7 — Mission & Performance Workstation

**Status source:** use `PROJECT.md`; this roadmap records forward sequencing only.

**Purpose:** present objectives, demand, progress, score decomposition and deterministic performance history without changing the M10.9.6 evaluation owner.

Detailed plan: [`milestones/M10.9.7.md`](milestones/M10.9.7.md).

Remaining sequence after the active checkpoint:

1. M10.9.7.5 — keyboard/minimum-window/manual closure gate.

M10.9.7.5 consumes the validated deterministic timeline/drill-down/replay-equivalence surface and closes the full M10.9.7 matrix without adding new challenge/scoring/physics ownership.


## M10.9.8 — Integrated Human-Automation-HMI Validation Gate

**Purpose:** validate M10 as one coherent operator system across assistance, control authority, faults, protection, challenge/scoring, replay and manual operation.

Detailed plan: [`milestones/M10.9.8.md`](milestones/M10.9.8.md).

Planned sequence:

1. M10.9.8.1 — freeze the validation matrix and invariants — **VALIDATED**;
2. M10.9.8.2 Hotfix 1 REV5 — automated healthy matrix + production mission/F4/list robustness — **VALIDATED**;
3. M10.9.8.3 — degraded measurement/fault/protection/takeover cases — **ACTIVE CANDIDATE**;
4. M10.9.8.4 — replay/checkpoint/same-seed and scoring integrity;
5. M10.9.8.5 — manual HMI/keyboard acceptance and M10 closure.

M10 closes only after M10.9.8 is explicitly validated.

## M10 Final — replacement-long closure route before M11

The detailed blocking contract is [`M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md`](M10_FINAL_REPLACEMENT_LONG_CLOSURE_PLAN.md). The amended sequence is **P0 → P1 → P2(plan-stop) → P1A → P2R1(plan-stop) → Plan Amendment 2 → Todreas/Kazimi Deep Review Pass 2 → P1B → P2R2(VALIDATED: P3-R owner localization) → Plan Amendment 3 → Physical Reference Model Assessment VR0–VR5 → P3-R1 owner localization → P3-R2 runtime decision → optional bounded repair/requalification → P4 → P5 → P6**:

- P0 freezes Diagnostic 1–6 evidence and planning — **VALIDATED (Hotfix 2)**;
- P1 determines asymptotic first-stage convergence versus stationary bias — **RETURNED EXECUTION PASS / FINAL INCONCLUSIVE**;
- P2 Decision Gate 1 records **PLAN-STOP-INCONCLUSIVE** and selects no P3 branch — **VALIDATED**;
- Plan Amendment 1 inserted **P1A Asymptotic Closure Extension** — **RETURNED EXECUTION PASS / OVERALL INCONCLUSIVE**; exact-v9 5.5 MWe converged and exact-v9 6 MWe demonstrated load reachability but not full stationarity at 3,600 s;
- **P2R1** records `PLAN-STOP-INCONCLUSIVE` and authorizes neither P3 branch; **Plan Amendment 2** defines **P1B Slow-State Closure & Phenomenon-Owner Qualification** at the existing 3,600 s horizon with a 600 s 5 MWe background reference;
- mandatory **Todreas/Kazimi Deep Review Pass 2** validated Plan Amendment 2; **P1B has now returned execution PASS / `COUPLED-MULTI-DOMAIN`** and returns to **P2R2 Decision Re-entry 2**;
- P2R2 is **VALIDATED** and selects **P3-R owner localization**, not P3-W: 6 MWe electrical reachability is demonstrated but represented inventory/hydraulic/steam/controller stationarity is not; production repair is still forbidden;
- **Plan Amendment 3** now inserts a bounded **Physical Reference Model Assessment** (`VR0→VR5`) before P3-R1 so point kinetics, water/steam, iodine/xenon and decay heat obtain independent quantitative reference evidence before any runtime owner is repaired;
- after VR5 `PROCEED-P3R1-EXACTV9`, **P3-R1** localizes the exact canonical runtime owner before any repair semantics are proposed;
- P4 must demonstrate a real stable 5→10→5 manoeuvre;
- P5 freezes and executes Replacement-Long Baseline 2;
- P6 alone closes M10 and releases M11.1.

An inconclusive P1 is a planning stop, not authorization for an ad hoc diagnostic. A second replacement-long freeze is forbidden before P4 PASS. exact-v9 remains immutable if any runtime repair requires exact-v10.

## M11 — Release Hardening

**Purpose:** turn the validated M10 simulator into a release candidate without adding new gameplay or physics features.

Detailed plan: [`milestones/M11.md`](milestones/M11.md). Review-derived assurance contract: [`M11_DIGITAL_IC_RELEASE_ASSURANCE_PLAN.md`](M11_DIGITAL_IC_RELEASE_ASSURANCE_PLAN.md).

Planned sequence:

1. M11.1 — release/support contract and version freeze;
2. M11.2 — save/scenario/session compatibility and migration hardening;
3. M11.3 — performance, memory and long-run release budgets;
4. M11.4 — packaging/publish/deployment verification;
5. M11.5 — documentation/manual/known-limitations release alignment;
6. M11.6 — release-candidate clean-machine and final acceptance gate.

No feature work is accepted inside M11 unless it fixes a release-blocking defect demonstrated by an M11 gate.

Persistence-specific carry-forward: M11.2 owns any deliberate schema-v2/string-enum migration decision and compatibility matrix; M11.3 owns any stream-based persistence change justified by measured allocation/LOH evidence. Neither is required merely because the current schema-v1 adapter remains numeric/string-materialized.

## Post-M11 strategic epics and milestone mapping

The approved long-horizon direction is maintained in [`FUTURE_GAMEPLAY_CONTROL_ROOM_AND_ACCIDENT_DIRECTION.md`](FUTURE_GAMEPLAY_CONTROL_ROOM_AND_ACCIDENT_DIRECTION.md), but it is now mapped to explicit post-release milestones so that the work is visible and dependency-ordered.

Three strategic epics remain authoritative:

- **Epic A — Extreme Operations & Accident Progression** spans **M12 + M15**. M12 builds the physical/extreme-envelope and persistence foundations; M15 adds explicit damage/consequence families only after those prerequisites are validated.
- **Epic B — Spatial Reactor** maps to **M14**. It evolves the reference core toward multiple 2D zones/equivalent channel groups and educational local layers without claiming full-channel neutron transport.
- **Epic C — Control-Room Experience** maps to **M13**. It strengthens operator presentation, maintained-handle semantics, procedures, presets, mimic interaction/layout persistence and Instructor/Fault presentation without moving physics into Avalonia.

The dependency order after M11 is therefore:

```text
M12 Extreme Operations Foundations
    ↓ establishes extreme-envelope, decay-heat, integrity and incident-state prerequisites
M13 Control-Room Experience
    ↓ provides plant-like presentation, procedures, mimic layout and Instructor/Fault shell
M14 Spatial Reactor
    ↓ provides deterministic quasi-spatial zones/groups, rods and local evidence layers
M15 Accident Progression & Consequence Models
    ↓ consumes M12 foundations and may expose M14-localized / M13-instructor evidence
```

This order deliberately prevents visually attractive severe-accident features from outrunning their physical owners. M12–M15 are post-M11 work and must not become prerequisites for M10.9.7, M10.9.8 or M11 unless a current gate demonstrates a release-blocking defect.

### M12 — Extreme Operations Foundations — Epic A, foundation phase

Detailed plan: [`milestones/M12.md`](milestones/M12.md). Operating-point foundation: [`REFERENCE_OPERATING_POINT_EQUILIBRIUM_AND_STABILITY_PLAN.md`](REFERENCE_OPERATING_POINT_EQUILIBRIUM_AND_STABILITY_PLAN.md).

Planned sequence:

0. M12.0 — **Reference Operating-Point Equilibrium & Stability Qualification**: residual taxonomy, closed-loop observational inspector, domain-headroom/trend diagnostics, validation-only plant-hold seam, bounded trimmer only if evidence requires it, and perturbation/long-horizon closure; existing exact identities remain immutable. Reviewed thermal-hydraulic/control sources reinforce that this gate must include requested/effective/control-memory state, steam/feedwater/inventory energy balance, pressure-grade attribution and canonical circulation head/loss evidence; apparent frequency/power calm or a short stored-energy response is not sufficient.
1. M12.1 — flow-owner directionality/support inventory (`BIDIRECTIONAL`, `ONE-WAY BY PHYSICS`, `ONE-WAY BY CHECK/ISOLATION`, `UNSUPPORTED OUTSIDE ENVELOPE`);
2. M12.2 — **near-zero hydraulic constitutive regularity and conditioning audit**: quadratic `sqrt(|Δp|)` behavior, ideal check-valve non-smoothness, valve near-close conditioning, normalized Jacobian/pivot diagnostics and deterministic summation semantics; no production smoothing before evidence/requalification;
3. M12.3 — near-empty inventory and extreme pressure/temperature/inventory validation matrix before expanding fault authority;
4. M12.4 — **pump mechanical/electrical/thermal energy ownership closure**, including explicit treatment of shaft demand and modeled inefficiency/loss-to-heat before stronger full-plant conservation claims;
5. M12.5 — credible post-trip decay-heat ownership integrated through the full-plant runtime and energy accounting;
6. M12.6 — persistent component integrity/stress primitives separated from functional/effective state;
7. M12.7 — physical `IncidentSeverity` contract separated from alarm priority, with deterministic checkpoint/replay/post-incident persistence scaffolding;
8. M12.8 — integrated extreme-foundation closure gate.

No leak, rupture, fire or severe core-damage claim is authorized merely by completing M12.

### M13 — Control-Room Experience — Epic C

Detailed plan: [`milestones/M13.md`](milestones/M13.md).

Planned sequence:

1. M13.1 — IndustrialControls/presentation-boundary integration and stronger retro-industrial visual identity;
2. M13.2 — maintained handle/selector position separated from effective equipment state where the physical control semantics require it;
3. M13.3 — stable canonical-ID selection and command-target safety: disappear/reorder -> clear/disable, never silent retargeting;
4. M13.4 — first-class mimic viewport: zoom, pan, fit/reset, stable selection and drill-down;
5. M13.5 — explicit layout-edit/lock/reset with versioned persistent equipment positions keyed by canonical IDs;
6. M13.6 — presentation-only workspace presets;
7. M13.7 — real operating procedures expressed over canonical commands/interlocks;
8. M13.8 — visually distinct Instructor/Fault mode using only fault/damage authority already modeled at that point;
9. M13.9 — **Digital I&C Degradation & Automation Transparency**: stale/delayed/lost/inconsistent measurement evidence, delayed observed-response, automation intent/reason/result transparency, anti-keyhole persistent critical context and representative human-system part-task evaluation;
10. M13.10 — integrated keyboard/minimum-window/replay/session UX closure plus staged MainWindowViewModel decomposition informed by M11.3 measurements.

Area/subsystem workspaces remain; no giant all-controls screen and no multi-monitor/multi-computer dependency are introduced.

### M14 — Spatial Reactor — Epic B

Detailed plan: [`milestones/M14.md`](milestones/M14.md).

Planned sequence:

1. M14.1 — explicit quasi-spatial fidelity contract and limits;
2. M14.2 — multi-zone/equivalent-channel-group reference-core composition;
3. M14.3 — multiple rods/rod groups with explicit zone mapping and deterministic command/state ownership; any enhanced rod-worth claim must preserve spatial/state provenance rather than treating the global reduced worth curve as a solved local flux response;
4. M14.4 — physically justified local/quasi-spatial power, flow, void, temperature and xenon feedback/evidence; global point kinetics remains explicitly distinct from full-core space-time kinetics, every local layer must retain solved/derived/mapped/interpolated provenance, and any homogenized/coarse reduction must declare the reaction/leakage/power quantities it is intended to preserve against its chosen reference;
5. M14.5 — 2D core map with selectable educational layers for power, flow, void, temperature, xenon and rod influence;
6. M14.6 — local drill-down and deterministic trends without implying unsupported full-channel neutron transport;
7. M14.7 — replay/checkpoint/performance/manual-fidelity closure gate.

Local damage visualization remains deferred until M15 owns an explicit damage mechanism.

### M15 — Accident Progression & Consequence Models — Epic A, consequence phase

Detailed plan: [`milestones/M15.md`](milestones/M15.md).

Planned sequence:

1. M15.1 — pressure-boundary stress, leak initiation/growth and rupture where explicitly supported;
2. M15.2 — rotating-equipment degradation/failure from modeled mechanical/thermal exposure;
3. M15.3 — electrical damage/fire only from explicit electrical/thermal/ignition mechanisms;
4. M15.4 — core-damage prerequisite gate proving decay-heat/cooling/local-thermal causal ownership before any core-damage implementation;
5. M15.5 — bounded core-damage progression only if M15.4 passes; any rapid-reactivity consequence claim remains bounded by the actual M14 spatial/neutronic fidelity and must not reinterpret global point kinetics as licensing-grade space-time analysis;
6. M15.6 — physical incident severity plus persistent replay/checkpoint/session/post-incident integration;
7. M15.7 — Instructor/Fault and local/spatial consequence presentation using M13/M14 surfaces without UI-owned consequence logic;
8. M15.8 — integrated deterministic extreme-operation/accident closure gate.

Every consequence family is introduced one at a time with its own causal owner and focused gate. Scripted `fault → severity` or `threshold → explosion` shortcuts remain prohibited.

## Deferred maintenance

Also non-blocking for M10.9.5 unless new evidence changes the risk:

- physical removal of H.5 `DeterministicHybridSemiImplicit` historical source seams;
- physical removal of H.21 `FourNodeBranchContinuityShadowIntegrated` historical source seams;
- investigation of whether validated long-horizon inventory/energy drifts can be reduced by a separately qualified physical/initialization improvement;
- simplification/removal of branch-continuity machinery only after a separate post-Phase-I proof that current repaired trajectories no longer require it;
- performance optimization of the authoritative corrected path beyond the validated repaired Stage-4 bounds;
- measured Pump/Valve temporary-definition allocation, corrector trigger-path allocation, infeasible-probe exception cost and root-search ceilings in M11.3 only when exact-output evidence supports them;
- generic `SimulationRuntime.Advance(elapsed)` catch-up/API policy in M11.3; desktop cooperative batching is already separately bounded;
- desktop UI-thread batch/projection/PropertyChanged responsiveness measurement in M11.3; any worker runtime must establish single ownership plus immutable snapshot handoff before moving execution off Avalonia;
- long-session archive export responsiveness in M11.3; off-thread serialization requires immutable captured evidence, not direct concurrent access to the live session;
- stable command-target selection/no silent index retargeting in M13.3;
- staged `MainWindowViewModel` decomposition in M13, informed by M11.3 measurements;
- near-zero hydraulic constitutive regularization, check-valve smoothing/leakage, Jacobian conditioning-policy changes and trajectory-changing summation changes only through M12.2 evidence/requalification;
- pump shaft/electrical/inefficiency-to-heat ownership closure in M12.4 before stronger severe-incident/full-plant conservation claims;
- branch-continuity retirement/unification only after dedicated M12.2/post-Phase-I evidence; current bounded previous-phase continuity may conditionally contribute to committed corrected state.


### Todreas/Kazimi thermal-hydraulic deep-review additions

- **M12.0:** distinguish conserved-inventory/root closure, branch identity and bounded perturbation stability; use canonical head/loss and inventory evidence rather than one scalar flow target.
- **M12 extreme hydraulics:** explicitly scope mixed/natural circulation, pump coastdown, stagnation and reverse flow before claiming them.
- **M12/M15 thermal limits:** distinguish DNB/dryout or label any simpler mechanism as an educational surrogate.
- **M14 representative channels:** declare what hydraulic reduction preserves (for example total flow, heat, pressure drop, outlet enthalpy/inventory) and solve shared-boundary flow split when fidelity is increased.
- Any drift-flux, nonequilibrium mixture or two-fluid upgrade is a new physical-model/V&V milestone, not an M10 closure adjustment.

### Todreas/Kazimi Deep Review Pass 2 — Plan Amendment 2 literature hold

P2R1 / Plan Amendment 2 Hotfix 1 and the mandatory post-amendment Todreas/Kazimi review are locally validated. P1B returned execution PASS with label `COUPLED-MULTI-DOMAIN`; P2R2 is now VALIDATED and selects P3-R owner localization only. Plan Amendment 3 inserts the external physical-reference assessment hold before P3-R1. No production repair, P4/P5, second replacement-long or M11 work is authorized.


### P1B — Slow-State Closure / Phenomenon-Owner Qualification

Todreas/Kazimi Deep Review Pass 2 is locally validated `PASS-AS-AUTHORED`; P1B returned execution PASS / `COUPLED-MULTI-DOMAIN`. It confirms 6 MWe electrical reachability while exposing persistent inventory/hydraulic/steam/controller redistribution relative to the stable 5 MWe background. **P2R2 is VALIDATED** and selects P3-R owner localization. **Plan Amendment 3 / Physical Reference Model Assessment** was the historical planning hold that inserted VR0-VR5 before P3-R1; the active VR2 repair branch has since completed R1 and R2, returned R3 Planning 1 `PASS-AS-AUTHORED`, and is now at the bounded R3 shadow-composition execution gate. No default/exact-v9 activation is authorized.


### M10 Final physical-reference hold — current execution state

The external physical-reference route remains a prerequisite before P3-R1. Current execution status is intentionally not duplicated here; use `PROJECT.md`. Future progression remains VR2 repair/requalification closure -> VR3 iodine/xenon -> VR4 decay heat -> VR5 consolidated decision -> P3-R1 only if explicitly authorized. No roadmap item by itself authorizes production repair, exact-version reinterpretation or a second replacement-long baseline.

### VR2 / RP1C forward decision roadmap after Exact-v9 Tail Attribution 1

This subsection is **future-work planning only**. `PROJECT.md` remains authoritative for whether the active Attribution 1 REV1 candidate has actually executed and for the current authority flags. The purpose here is to prevent the remaining VR2 route from becoming an open-ended sequence of diagnostics: every future branch has a defined trigger, evidence goal, stop condition and convergence point.

#### Forward decision tree

```text
Attribution 1 REV1 execution
        |
        v
Returned-evidence adjudication
        |
        +--> A. runtime-mode sensitivity observed
        |        |
        |        +--> A1. confirm the specific runtime factor
        |        |        +--> if PGO is implicated: QJFL=1 comparator planning/confirmation
        |        |        +--> otherwise: narrow single-factor confirmation
        |        |
        |        +--> A2. proposed runtime configuration impact assessment
        |        +--> A3. full-domain confirmation under the intended runtime configuration
        |
        +--> B. tail persists with no useful mode separation
        |        |
        |        +--> B1. Tail Attribution 2 planning: on-CPU/off-CPU/runtime-event correlation
        |        +--> B2a. exogenous/runtime scheduling owner -> performance-contract adjudication
        |        +--> B2b. candidate on-CPU owner -> candidate-specific localization / C5 planning
        |
        +--> C. tail not reproduced or evidence remains inconclusive
        |        |
        |        +--> C1. fixed-sample rare-tail reproducibility extension
        |        +--> C2. adjudicate contradictory historical/current evidence
        |
        +--> D. harness/environment/integrity failure
                 |
                 +--> repair only the failed evidence infrastructure and rerun the same gate

Any branch that produces one selection-ready candidate
        |
        v
RP1C Selection Gate: SELECT-CANDIDATE | SELECT-NONE
        |
        +--> SELECT-NONE -> planning stop / new repair planning only
        |
        +--> SELECT-CANDIDATE
                 |
                 v
R1 -> R2 -> R3 -> R4 -> R5 -> R6 production-repair/requalification route
                 |
                 v
VR2 closed/nonblocking
                 |
                 v
VR3 -> VR4 -> VR5
                 |
                 +--> PROCEED-P3R1-EXACTV9 -> P3-R1 -> P3-R2 -> P4 -> P5 -> P6 -> M11
                 +--> BLOCK-P3R1-MODEL-REPAIR -> bounded model-repair replanning
                 +--> PLAN-STOP-REFERENCE-GAP -> reference-gap planning stop
```

Only **one** evidence branch is active at a time. A returned failure or inconclusive result does not authorize execution of every other branch.

#### Immediate resume checklist when local execution is available

1. Start only from the reviewed Attribution 1 REV1 candidate; do not fall back to the pre-REV1 package.
2. Confirm the caller has no inherited planned `DOTNET_*` compilation variables.
3. Run the existing Attribution 1 runner from the repository root; do not manually split or reorder the 20 child-process runs.
4. If build, PowerShell, harness or integrity fails, preserve whatever evidence exists and stop. Do not edit C4/corpus/thresholds as part of the hotfix.
5. If execution completes, return the entire 86-file artifact tree. Do not launch a follow-up runtime experiment locally even if one mode appears obviously better.
6. Perform returned-evidence adjudication and select exactly one roadmap branch before authoring the next runner.
7. Reconcile `PROJECT.md`, `ROADMAP.md`, the relevant ADR and restart documentation before moving to that branch.

This checklist deliberately ends at returned-evidence adjudication; it prevents a visually compelling timing result from becoming an unauthorized causal or selection decision.

#### Stage 0 — execute and return Attribution 1 REV1

Historical checkpoint — when Attribution 1 REV1 was current, it was the only executable gate in this branch. Its fixed purpose was to compare the four already-planned runtime modes without promoting causality:

- `AMBIENT-UNSET-CONTROL`;
- `TIERING-OFF`;
- `TIERING-ON-PGO-OFF`;
- `TIERING-ON-PGO-ON`.

The REV1 schedule remains counterbalanced and the frozen workload remains 20 fresh processes / 460,800 measured exact-v9 calls. The execution must return the complete evidence tree before any branch below is chosen.

Hard stops:

- inherited `DOTNET_*` compilation configuration -> environment failure, not performance evidence;
- build/test/harness/integrity failure -> infrastructure repair only;
- no result from the runner itself may select C4, alter a threshold or declare a JIT/runtime feature causal.

#### Stage 1 — Attribution 1 returned-evidence adjudication

The first post-run activity is a documentation/evidence gate, not another experiment. It must independently reconstruct:

- all 20 mode/run combinations and the counterbalanced sequence;
- the complete 64 x 360 matrix for every process;
- per-mode and per-run median, p95, max and allocation/GC accounting;
- counts and identities above the diagnostic `100 us` floor;
- counts and identities above the unchanged `409.30666666666673 us` engineering ceiling;
- pass, row, probe, node and `C4` resolution path for each tail event;
- whether any apparent separation is repeated across runs rather than carried by one isolated maximum.

The adjudication must choose one forward branch. The branch labels below are roadmap categories, **not pre-authored statistical verdicts**; any quantitative decision rule needed to distinguish them must be frozen in the planning gate that follows.

#### Branch A — reproducible runtime-mode sensitivity

Use this branch only if the returned evidence shows a repeated directional difference between runtime modes that cannot reasonably be described as one isolated max.

**A1 — Runtime-Factor Confirmation Planning.** Freeze a smaller confirmatory experiment before interpreting the factor. The confirmatory gate must change one runtime dimension at a time where practical, preserve immutable C4/corpus/thresholds and retain counterbalanced fresh-process execution.

Special rule for a possible PGO interpretation: Attribution 1 intentionally preserves `DOTNET_TC_QuickJitForLoops=0`. Because the target `C2-MIXTURE-PREFIX` contains a loop, `PGO-ON ~= PGO-OFF` is not exclusionary evidence. If PGO is still a material hypothesis, create a separately adjudicated **QJFL=1 comparator planning gate** with tiering on and PGO off/on. Do not rewrite Attribution 1 after the fact.

**A2 — Runtime Configuration Impact Assessment.** If a non-default runtime configuration repeatedly removes the strict tail, do not immediately select C4. First qualify the proposed configuration as a project/runtime decision, including at minimum:

- ordinary Release suite under the proposed configuration;
- determinism/replay compatibility checks relevant to M10;
- representative performance regression outside the VR2 micro-path;
- confirmation that the setting does not create a new failure in another validated owner;
- documentation of whether the setting is process-wide, test-only or intended for release/runtime deployment.

A configuration that fixes this microbenchmark but degrades unrelated validated behavior is not an RP1C repair.

**A3 — Full-Domain Performance Confirmation 2.** Only after A2 is green may the immutable candidate be re-run under the exact runtime configuration intended for production qualification. This is a new evidence gate; it does not reinterpret the failed Full-Domain Performance Confirmation 1. Exact-v9 and seam workloads and the engineering ceiling stay frozen unless a separate performance-contract adjudication has explicitly changed them.

Exit from Branch A: one candidate/runtime pair may become `selection-ready` only after the confirmatory evidence and full-domain gate are returned and adjudicated green.

#### Branch B — runtime-mode-insensitive tail persists

Use this branch if rare strict misses remain across modes without a repeatable mode separation, or if the mode comparison is too weak to identify a runtime compilation factor.

**B1 — Exact-v9 Tail Attribution 2 Planning.** Plan a new evidence-only attribution gate whose purpose is to distinguish candidate on-CPU work from off-CPU/runtime/OS interruption. The planning gate must keep the uninstrumented qualification timing separate from any instrumented diagnostic run so tracing overhead cannot become qualification evidence.

Candidate diagnostic families for that planning gate include, subject to an explicit pre-execution review:

- JIT/tier transition correlation;
- managed contention/thread-pool/runtime-event correlation;
- processor migration / scheduling or off-CPU interval evidence;
- process/thread execution context around the rare tail;
- targeted micro-step timing inside `C2-MIXTURE-PREFIX`, but only in a separate diagnostic build/path that is never used as production qualification evidence.

The exact tooling and event set must be frozen by B1; this roadmap does not pre-authorize a profiler, trace provider or source instrumentation.

**B2a — exogenous/runtime scheduling owner.** If B1 shows the strict wall-clock miss is dominated by off-CPU/runtime scheduling rather than candidate work, open a **Performance Contract Adjudication**. Its question is not "how much should the threshold be widened?" but whether a single-call wall-clock maximum on a general-purpose OS is the correct candidate-selection property.

Permitted questions for that adjudication include:

- keep the existing max unchanged under more tightly controlled execution conditions;
- add an on-CPU/CPU-time diagnostic while retaining wall-clock evidence;
- require a reproducibility rule for strict max events rather than treating every isolated preemption identically;
- define process affinity/priority only if separately shown not to distort the product runtime contract.

Any performance-contract change must be versioned, must preserve the old evidence as historical truth, and must replay the relevant C3/C4 comparator evidence before RP1C selection. No contract adjudication may silently convert the current two FDPC1 misses into PASS.

**B2b — candidate on-CPU owner.** If B1 localizes a repeatable on-CPU slow path in C4, do not mutate C4 in place. Continue through Branch E / new-candidate planning below.

#### Branch C — tail not reproduced or evidence remains inconclusive

Use this branch if Attribution 1 does not reproduce the historical tail strongly enough to support either runtime sensitivity or a candidate-specific owner.

**C1 — Rare-Tail Reproducibility Extension Planning.** Freeze the sample size, process count, order/randomization policy and stop rule **before** execution. The extension must not continue sampling until a preferred answer appears.

**C2 — Contradictory-Evidence Adjudication.** Historical Full-Domain Performance Confirmation 1 misses remain authoritative even if the extension is green. The adjudication must decide whether:

- additional evidence is still required;
- the tail is best treated as an environment/performance-contract question;
- a new candidate/localization path is justified;
- RP1C must remain `SELECT-NONE`.

No "tail not seen this time" result erases the frozen run-4/run-5 FDPC1 evidence.

#### Branch D — infrastructure or evidence-integrity failure

Compile errors, Windows PowerShell parser/API issues, malformed evidence, dirty caller runtime variables, missing artifacts or validator defects remain **RED infrastructure**, not engineering evidence.

The only allowed action is a bounded hotfix of the failed infrastructure contract followed by rerun of the same gate or adjudication of already-complete process evidence when that is provably safe. Do not change candidate code, corpus, thresholds or runtime matrix to repair an infrastructure RED.

#### Branch E — candidate-specific repair and C5 route

This branch opens only after returned evidence identifies a repeatable candidate on-CPU owner that cannot be closed by an acceptable runtime configuration or performance-contract adjudication.

**E1 — C4 Exact-v9 Tail Localization 2.** Localize the cost inside the immutable C4 path using test-only diagnostics. The goal is a specific operation/loop/search/branch owner, not another broad timing sweep.

**E2 — C5 Planning.** C4 becomes frozen historical evidence. Any algorithmic repair receives a new candidate identity (provisionally `C5`) and must preserve:

- C4/C3 thermodynamic semantics bit-for-bit on the frozen semantic corpus unless a separately authorized physical correction explicitly changes the target;
- the R1 zero-allocation / zero-fallback closure already demonstrated by C4;
- all RP1A corpora and performance ceilings;
- no production code changes during candidate qualification.

**E3 — C5 qualification sequence.** At minimum:

1. semantic/reference repeat over the complete frozen C4/C3 comparison set;
2. R1 allocation/tail regression gate;
3. exact-v9 + seam full-domain performance gate;
4. any targeted localization regression introduced by E1;
5. returned-evidence adjudication before RP1C.

If C5 is needed, RP1C planning must explicitly amend the selection slate; C4 is not silently replaced.

#### Convergence gate — RP1C Selection

RP1C selection is allowed only after exactly one of the branches above produces a candidate/runtime/performance-contract combination that is fully `selection-ready` under the then-authoritative predicate.

The selection gate must:

- reconstruct the readiness matrix from frozen evidence;
- preserve `SELECT-NONE` as a mandatory option;
- include only candidates whose complete semantic/physical/seam/performance evidence is green;
- preserve D3 as blocked unless separately versioned new evidence changes its seam-max result;
- make no production source change itself.

Expected decision space is therefore one of:

```text
SELECT-C4 | SELECT-NONE
```

or, if Branch E creates and qualifies a successor:

```text
SELECT-C5 | SELECT-NONE
```

A returned selection must itself be adjudicated before production work begins.

#### Post-selection route — R1 through R6

A positive RP1C selection authorizes **R1 planning only**, not immediate runtime activation.

**R1 — new opt-in closure mode implementation.** Implement the selected repair behind a new opt-in production closure mode. Preserve the existing `CorrelationConsistentInverseDomain` behavior and exact-v9 identity. No default switch at this stage.

**R2 — focused thermodynamic/reference/topology qualification.** Re-run the independent VR2 reference points, phase ownership, seam continuity, deterministic-repeat and topology checks against the production-path candidate. Any new wrong-phase or reference-blocking result stops the route.

**R3 — short exact-v9-equivalent shadow/composition requalification.** Demonstrate that composing the production candidate into the exact-v9-equivalent path preserves the required deterministic trajectory/ownership contracts. This remains qualification; it does not reinterpret historical exact-v9.


**R4 — P1B-equivalent long materiality recheck.** Re-run the bounded long materiality scenario with the repaired candidate to prove that the hydraulic/thermodynamic discrepancy is actually removed without creating new inventory, steam, controller or stability regressions.

**R5 — VR2 re-entry against the repaired exact-version candidate.** Only after R1-R4 are green may the next exact-version identity be frozen for the repair candidate (provisionally the next available exact identity; do not rewrite exact-v9). Re-run the independent VR2 assessment and require a nonblocking result.

**R6 — versioned activation decision.** Decide whether the qualified exact-version candidate may become the authorized production path. R6 is a decision gate, not an automatic consequence of R5 PASS.

Any R1-R6 failure keeps the existing production mode authoritative and returns to bounded repair planning. No partial PASS authorizes silent default activation.

#### After VR2 closure — resume Plan Amendment 3

Only after R6/VR2 returns a nonblocking result does the external physical-reference route resume:

1. **VR3 — I-135/Xe-135 reference assessment.** Assess the generic iodine/xenon solver against the already frozen independent reference contract. Its relevance to current exact-v9 remains evidence-based; do not promote a future M14 concern into an M10 blocker without returned materiality evidence.
2. **VR4 — decay-heat reference assessment.** Execute the frozen decay-heat comparison. The absence of a canonical production decay-heat definition in current exact-v9 remains a known scope fact, not an automatic M10 blocker.
3. **VR5 — consolidated physical-reference decision.** Emit exactly one authoritative route decision:
   - `PROCEED-P3R1-EXACTV9`;
   - `BLOCK-P3R1-MODEL-REPAIR`;
   - `PLAN-STOP-REFERENCE-GAP`.
4. **P3-R1** executes only after `PROCEED-P3R1-EXACTV9` and localizes the canonical runtime owner.
5. **P3-R2** makes the runtime decision and may authorize only a bounded repair/requalification route supported by P3-R1 evidence.
6. **P4** must prove a real stable 5 -> 10 -> 5 manoeuvre.
7. **P5** alone freezes and executes Replacement-Long Baseline 2 after P4 PASS.
8. **P6** closes M10 and releases M11.1.

A second replacement-long baseline remains forbidden before P4 PASS.

#### Gate packaging / documentation discipline for all remaining VR2 work

Every future gate in this decision tree must preserve the project pattern already established by VR2:

1. planning/contract first when the experiment or authority changes;
2. pre-execution review before an expensive run;
3. immutable candidate/corpus/reference pins;
4. explicit Windows PowerShell compatibility review for new validators/runners;
5. engineering-negative outcomes recorded as evidence rather than synthetic test failures where the harness is healthy;
6. complete returned artifact folder before the next authority decision;
7. returned-evidence adjudication that independently reconstructs the important metrics from raw evidence;
8. documentation/restart reconciliation before changing chat or opening the next branch;
9. no in-place mutation of historical candidate identities;
10. no threshold, exact-version or production-authority change without a separately named decision gate.

#### Expected remaining validation burden

This table is a cost/risk planning aid, not a schedule promise:

| Future gate family | Typical burden | Reason |
| --- | --- | --- |
| Attribution 1 returned adjudication | low | evidence reconciliation only |
| Runtime-factor/QJFL confirmation | medium/high | multiple fresh-process performance runs |
| Tail Attribution 2 / event correlation | medium/high | diagnostic complexity; qualification and instrumentation must stay separate |
| Performance-contract adjudication | medium | documentation + comparator replay plan; potentially high if contract changes |
| C5 candidate route | high | semantic + R1 + full-domain requalification |
| RP1C selection | low/medium | decision/evidence gate, no production change |
| R1-R3 | medium/high | first production-path implementation and focused qualification |
| R4 | high | P1B-equivalent long materiality recheck |
| R5-R6 | medium/high | independent VR2 re-entry + activation decision |
| VR3-VR5 | medium/high | independent reference assessments and synthesis |
| P4-P6 | very high | stable manoeuvre, second replacement-long baseline and M10 closure |

The roadmap objective is to spend the expensive gates only after the cheaper evidence gates have identified the correct owner and decision branch.

## RP1C post-Attribution 1 branch — runtime single-factor isolation

Attribution 1 REV1 returned evidence establishes strong runtime sensitivity but does not isolate the exact owner of the rare ambient exact-v9 tail. The current planning gate freezes a chained single-factor experiment with PGO OFF and ReadyToRun fixed: `Tiering OFF/QJ OFF/QJFL OFF` -> `Tiering ON/QJ OFF/QJFL OFF` -> `Tiering ON/QJ ON/QJFL OFF` -> `Tiering ON/QJ ON/QJFL ON`. The three adjacent contrasts therefore isolate `TieredCompilation`, `QuickJit`, and `QuickJitForLoops` separately. `TIERING-OFF` remains rejected as a repair direction. If Dynamic PGO remains material after returned factor-isolation adjudication, create a separate QJFL=1 PGO OFF/ON planning gate; do not fold it into the current experiment. C4 and the strict ceiling remain unchanged.

### Runtime Factor Isolation 1 implementation checkpoint — 2026-09-17

The returned `RP1C C4 Runtime Factor Isolation Planning 1` evidence is frozen `PASS-AS-AUTHORED`. The executable candidate `RP1C-C4-EXACT-V9-RUNTIME-FACTOR-ISOLATION1` is now defined as test-only/evidence-only and awaits local execution plus returned-evidence adjudication. It preserves the four-mode single-factor chain, 20 fresh processes, 460,800 measured calls, the `409.30666666666673 us` strict max and the diagnostic-only `100 us` tail floor. Dynamic PGO is not part of this gate. No RP1C selection or production/runtime authority is granted by implementation or local completion alone.


### Runtime Factor Isolation 1 returned adjudication — 2026-09-17

The returned 86-file / 20-process / 460,800-call evidence tree is complete and keeps the project on Branch A. The A↔B single-factor contrast establishes TieredCompilation as materially causal for the gross slowdown in every run block. B↔C shows only a small central QuickJit effect and does not repeat enough strict-tail evidence to promote QuickJit as the rare-tail owner. C↔D is materially neutral for QuickJitForLoops. No candidate allocation or measured-region GC explains the result.

The branch therefore does not open Tail Attribution 2 or C5 planning. Dynamic PGO remains one bounded unresolved factor because the earlier PGO comparator used QJFL disabled. Before Branch A2, plan a QJFL-enabled PGO OFF/ON comparator so the project can determine whether PGO state needs to be part of any proposed process-wide runtime configuration.

### Dynamic PGO Comparator Planning 1 checkpoint — 2026-09-17

At this checkpoint, the active candidate was planning-only `RP1C-C4-DYNAMIC-PGO-COMPARATOR-PLANNING1`. The future evidence gate, if separately authorized after returned planning adjudication, will hold TieredCompilation=1, QuickJit=1, QuickJitForLoops=1 and ReadyToRun=1, and vary only Dynamic PGO 0↔1. It freezes 2 modes × 5 fresh processes, 230,400 measured exact-v9 calls, complete 64×360 identity checking per process, unchanged `409.30666666666673 us` strict maximum and diagnostic-only `100 us` tail floor. Ambient/effective-default equivalence is excluded and deferred to Branch A2. RP1C selection and all production/runtime authority remain false.


### Dynamic PGO Comparator 1 implementation checkpoint — 2026-09-17

Historical checkpoint — Dynamic PGO Comparator Planning 1 returned `PASS-AS-AUTHORED` and its four-file artifact set was adjudicated complete. At that stage, the evidence-only implementation `RP1C-C4-EXACT-V9-DYNAMIC-PGO-COMPARATOR1` became the active candidate. It fixes TieredCompilation=1, QuickJit=1, QuickJitForLoops=1 and ReadyToRun=1, varies only `DOTNET_TieredPGO`, and executes the frozen counterbalanced 10-process schedule for 230,400 exact-v9 calls. The adjudicator must verify complete 64×360 identity/order per process and emit exactly 46 files. A negative, neutral or inconclusive PGO result is engineering evidence rather than infrastructure RED. Ambient/effective-default equivalence remains excluded and deferred to Branch A2. Runtime Configuration Impact Assessment, RP1C selection and all production/runtime authority remain false.


### Runtime Configuration Impact Assessment Planning 1 checkpoint — 2026-09-17

Dynamic PGO Comparator 1 returned a complete 46-file / 10-process / 230,400-call evidence tree and is adjudicated. PGO ON materially improves the central exact-v9 timing distribution in all five counterbalanced blocks and all 360 row medians, while the sparse tail does not justify promoting Dynamic PGO as the rare strict-tail owner. Historical checkpoint — Branch A then advanced to planning-only `RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT-PLANNING1`. The future A2 assessment must compare `AMBIENT-UNSET` with `EXPLICIT-REFERENCE-ALL-ON`, preserve the existing exact-v9 corpus and strict ceiling, run the ordinary Release suite, explicit M10 replay/determinism checks and an unrelated validated hot-path owner under both profiles, and retain the same-boundary two-process reproducibility rule for strict exact-v9 ownership. The explicit profile is a qualification reference only. A2 implementation, RP1C selection, production runtime change/repair and Full-Domain Performance Confirmation 2 remain unauthorized until returned planning adjudication.


### Runtime Configuration Impact Assessment Planning 1 returned / Host-Provenance Amendment 1 — 2026-09-17

Runtime Configuration Impact Assessment Planning 1 returned `PASS-AS-AUTHORED` with the complete four-file planning artifact set. Subsequent execution-workflow information established that project performance gates may run on more than one physical PC and that those PCs have materially different performance. Planning 1 did not freeze physical-host identity, so its PASS remains valid as authored but no longer suffices by itself for A2 implementation.

Historical checkpoint — the planning-only successor at that stage was `RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT-PLANNING1-AMENDMENT1-HOST-PROVENANCE`. It preserves the future A2 profiles, exact-v9 10-process / 230,400-call design, ordinary Release/replay/non-VR2 families, 78-file evidence shape, immutable C4/exact-v9 and unchanged strict maximum. It adds one physical host per complete A2 gate, privacy-preserving stable host fingerprinting, start/end active-power-scheme provenance, no cross-host evidence mixing and host-scoped interpretation of absolute wall-clock observations. Cross-host replication is outside A2 and requires separate planning if later material. A2 implementation, FDPC2, RP1C selection and all production/runtime authority remain false.

### Host-Provenance Amendment 1 returned / Frozen Evidence Ordinary Compaction 1 — 2026-09-17

Host-Provenance Amendment 1 returned `PASS-AS-AUTHORED` and is adjudicated complete. It closes the execution-host contract gap without changing the frozen A2 design. Before A2 implementation, the repository performs one maintenance-only compaction of `eng/frozen-evidence/ordinary`: direct prerequisite files remain <=1 MiB; larger immutable raw performance payloads move to authenticated compressed gate-scoped archives with canonical path/SHA-256/byte-count entries in `eng/frozen-evidence/large-payload-manifest.csv`. Historical evidence remains restorable byte-for-byte. This maintenance checkpoint does not alter C4, exact-v9, thresholds, runtime-profile conclusions or any authority boundary. A2 implementation becomes the next engineering construction only after returned compaction validation.

### Historical Frozen Evidence Ordinary Compaction 1 returned / A2 implementation — 2026-09-17

Frozen Evidence Ordinary Compaction 1 returned `PASS-AS-AUTHORED`: `ordinary` was restored to a compact prerequisite store and the 76 large raw payloads remained byte-identical in authenticated archives. At that checkpoint the next and only executable Branch A gate was `RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT1`. It executes on one physical host, compares `AMBIENT-UNSET` with `EXPLICIT-REFERENCE-ALL-ON`, collects 230,400 exact-v9 calls plus ordinary Release, replay/determinism and three-per-profile M10.9.7.2 hot-path runs, and emits exactly 78 files. Structured xUnit XML is the authoritative functional-test count source, so localized console output cannot produce false RED. Engineering-negative outcomes remain evidence; host/power-plan drift, incomplete exact matrices or missing structured reports remain infrastructure RED. FDPC2 planning, RP1C selection and every production/runtime authority remain false until returned A2 adjudication.

### 2026-09-17 Branch A2 execution checkpoint — supersession note

Earlier paragraphs in this roadmap that name Dynamic PGO Comparator 1, Runtime Configuration Impact Assessment Planning 1, Host-Provenance Amendment 1 or Frozen Evidence Ordinary Compaction 1 as the active successor are historical checkpoints. Their returned evidence is retained, but they are no longer the live gate.

Historical checkpoint — at that stage the only executable Branch A gate was `RP1C-C4-RUNTIME-CONFIGURATION-IMPACT-ASSESSMENT1` (A2), using its hardened evidence candidate. That single-host gate compared `AMBIENT-UNSET` against `EXPLICIT-REFERENCE-ALL-ON`, used structured xUnit XML plus native MTP non-zero-test guards, and required exactly 78 files. This checkpoint is superseded by the returned A2 adjudication and the historical FDPC2 Planning 1 checkpoint below.

### Historical checkpoint override -- A2 returned / FDPC2 Planning 1 (superseded)

Runtime Configuration Impact Assessment 1 returned a complete 78-file same-host evidence tree and is adjudicated PASS. `AMBIENT-UNSET` and `EXPLICIT-REFERENCE-ALL-ON` both preserve ordinary Release, replay/determinism and the validated M10.9.7.2 non-VR2 hot-path owner. Exact-v9 central timing is closely aligned; ambient records 0 strict misses while the explicit reference records one isolated 416.2 us miss with no reproducible strict owner. The project therefore does not require an explicit runtime override for the next qualification step. This is an operational sufficiency conclusion on the A2 host, not proof that internal .NET defaults are identical to the explicit all-ON variables.

At that historical checkpoint the only live successor was planning-only `RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION2-PLANNING1`. The future FDPC2 design preserves immutable C4, exact-v9/seam corpora and all thresholds, uses `AMBIENT-UNSET`, repeats the five-process 217,600-call full-domain protocol, and is scoped to the same A2 physical host and stable power scheme because absolute wall-clock thresholds are host-scoped. FDPC2 implementation, RP1C selection, production runtime change/repair, threshold/exact-v9 changes, VR3, P3-R1 and second replacement-long remain unauthorized pending returned planning adjudication.

### Historical 2026-09-18 -- FDPC2 Planning 1 returned / FDPC2 executable gate (superseded)

`RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION2-PLANNING1` returned `PASS-AS-AUTHORED`. At that historical checkpoint the live Branch A gate was evidence-only `RP1C-C4-FULL-DOMAIN-PERFORMANCE-CONFIRMATION2`, under `AMBIENT-UNSET` on the frozen A2 host and active power scheme. Five fresh processes must produce 217,600 measured calls and exactly 32 evidence files against the unchanged corrected predicate. FDPC1 remains negative historical evidence and is not reinterpreted. RP1C selection, production runtime change/repair, threshold/exact-v9 changes, VR3, P3-R1 and second replacement-long remain unauthorized pending returned FDPC2 adjudication.

### Historical checkpoint -- FDPC2 returned / RP1C Selection Planning 1 (superseded)

FDPC2 returned a complete 32-file / 5-process / 217,600-call evidence tree and is adjudicated `C4-FULL-DOMAIN-PERFORMANCE-CONFIRMED`. All five processes satisfy the corrected predicate under `AMBIENT-UNSET` on the frozen A2 host: zero unresolved, zero exact-v9 strict exceedances, zero seam strict exceedances and zero harness allocation. FDPC1 remains negative historical evidence and is not reinterpreted.

At that checkpoint the only live successor was planning-only `RP1C-ENGINEERING-REPAIR-SELECTION-PLANNING1`. It reconstructed the final readiness matrix: C4 + Ambient is the single selection-ready pair; D3 remains blocked by the frozen 16504.9 us seam maximum. The future decision space is exactly `SELECT-C4 | SELECT-NONE`; `SELECT-NONE` remains mandatory. RP1C selection itself, production repair/runtime changes, threshold/exact-v9 changes, VR3, P3-R1 and second replacement-long remain unauthorized pending returned selection-planning adjudication.

### Historical 2026-09-18 -- Selection Planning 1 returned / RP1C Selection 1 (superseded)

Selection Planning 1 returned `PASS-AS-AUTHORED`. At that historical checkpoint the live gate was `RP1C-ENGINEERING-REPAIR-SELECTION1`, decision-only and measurement-free. The frozen decision space remains `SELECT-C4 | SELECT-NONE`; `SELECT-NONE` is mandatory. The authored decision is `SELECT-C4` because C4 + `AMBIENT-UNSET` is the sole selection-ready pair after green FDPC2 and D3 remains blocked. This does not authorize production repair. A returned/adjudicated `SELECT-C4` may open only `R1-IMPLEMENTATION-PLANNING-ONLY`.

### Historical post-selection checkpoint -- R1 Implementation Planning 1 (superseded)

RP1C Selection 1 returned `SELECT-C4` as an authored engineering decision. This planning checkpoint is now superseded by returned `PASS-AS-AUTHORED` R1 Implementation Planning 1.

### Historical R1 checkpoint -- Selected C4 opt-in closure implementation (closed)

R1 Selected C4 Opt-In Closure Implementation 1 returned complete evidence and is adjudicated PASS. Mode 2 is qualified as explicit opt-in implementation evidence only; this checkpoint is superseded by returned R2 and R3 planning evidence.

### Historical post-R1 checkpoint -- R2 Focused Thermodynamic / Reference / Topology Qualification (closed)

R2 Planning 1 returned `PASS-AS-AUTHORED`, R2 execution returned all nine required artifacts, and returned evidence is adjudicated `PASS-R2-FOCUSED-THERMODYNAMIC-REFERENCE-TOPOLOGY-QUALIFIED`. The independent IF97/reference/topology gate is therefore closed and superseded by R3.

### Historical VR2 R3 repair-branch checkpoint after the first returned R3 RED

R3 Requalification 1 returned `R3-SHADOW-COMPOSITION-BLOCKING`. Diagnostic 1 confirmed `H28.1-E-FUSED-PATH-NOT-MODE2-AWARE`. The then-planned sequence through Diagnostic 2 and causal repair planning is now superseded by the returned Diagnostic 2 evidence and Diagnostic 3 deep review. Use the live future-sequence section at the top of this file for current work.

<!-- NRS-MARKER:DIAG3-REV1-ROADMAP-SUPERSESSION -->

### 2026-09-19 — Historical checkpoint — superseded after deterministic ordinary local PASS and Diagnostic 3 deep review

Diagnostic 2 Adjudicator Hotfix 1 returned PASS and froze `01`–`09`. At this historical checkpoint hosted `ordinary-ci` had reported one failing `Application.Tests` test, so the initial sequence held the entire physics branch behind CI maintenance.

`GitHub Ordinary CI Deterministic Serialization Hotfix 1` subsequently returned local PASS for exact-v9 audit, current-evidence audit and the complete ordinary suite. Diagnostic 3 deep review then clarified the authority split: **test-only causal evidence may proceed while hosted confirmation is pending, but no production repair planning/implementation may begin until hosted ordinary-ci is GREEN**.

The older sequence below is therefore superseded and retained only as provenance:

`Diagnostic 2 returned PASS -> deterministic ordinary CI local + hosted PASS -> causal-seam adjudication/planning -> separately authorized repair candidate -> bounded fast gate -> R3 Short Requalification 3 -> R4 Planning 1`

Current execution order is defined in the live future-sequence section and `M10_FINAL_VR2_R3_DIAGNOSTIC3_DEEP_REVIEW_REV1_PLANNING1.md`.
