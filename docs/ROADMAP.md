# Roadmap

This file contains **future work only**. The authoritative current checkpoint and active validation gate remain in `PROJECT.md`.

Detailed implementation slices, gate expectations and deferred-item ownership are maintained in [`FORWARD_EXECUTION_PLAN_M10_9_7_TO_M15.md`](FORWARD_EXECUTION_PLAN_M10_9_7_TO_M15.md).

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

## M11 — Release Hardening

**Purpose:** turn the validated M10 simulator into a release candidate without adding new gameplay or physics features.

Detailed plan: [`milestones/M11.md`](milestones/M11.md).

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

Detailed plan: [`milestones/M12.md`](milestones/M12.md).

Planned sequence:

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
9. M13.9 — integrated keyboard/minimum-window/replay/session UX closure plus staged MainWindowViewModel decomposition informed by M11.3 measurements.

Area/subsystem workspaces remain; no giant all-controls screen and no multi-monitor/multi-computer dependency are introduced.

### M14 — Spatial Reactor — Epic B

Detailed plan: [`milestones/M14.md`](milestones/M14.md).

Planned sequence:

1. M14.1 — explicit quasi-spatial fidelity contract and limits;
2. M14.2 — multi-zone/equivalent-channel-group reference-core composition;
3. M14.3 — multiple rods/rod groups with explicit zone mapping and deterministic command/state ownership;
4. M14.4 — physically justified local/quasi-spatial power, flow, void, temperature and xenon feedback/evidence;
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
5. M15.5 — bounded core-damage progression only if M15.4 passes;
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
