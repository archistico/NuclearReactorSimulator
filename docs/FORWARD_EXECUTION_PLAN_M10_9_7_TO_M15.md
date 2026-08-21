# Forward Execution Plan — M10.9.7 through M15

This document is the **cross-milestone execution map**. It answers what comes next, why the order matters, which detailed plan owns each topic, and where deferred work belongs.

It is deliberately not a second copy of every milestone specification:

- `PROJECT.md` — only authority for current validated/candidate state;
- `ROADMAP.md` — high-level future sequence;
- `milestones/M10.9.7.md` … `milestones/M15.md` — detailed executable plans;
- this file — dependency map, transition rules and deferred-item ownership.

## Execution discipline

Every future implementation slice follows the same rules:

1. start only from the latest explicitly validated baseline;
2. freeze owner, scope, non-scope and evidence before implementation;
3. prefer a pure/immutable contract before UI or persistence wiring;
4. preserve canonical physics, control, protection and recorder owners;
5. build + complete ordinary suite are mandatory for promotion;
6. focused gates must exercise the new contract rather than merely emit a marker;
7. manual HMI gates are required when operator-visible behavior changes;
8. a failing gate authorizes only the smallest evidence-backed repair;
9. long/performance gates are added only where persistent state, hot-path cost or long-horizon behavior actually changes;
10. replay/checkpoint compatibility is mandatory whenever new persistent or reconstructed state is introduced.

## Immediate transition

The M10.9.7.2 persistence closure is validated. The active step is now M10.9.7.3 live MISSION wiring.

```text
M10.9.7.2 Hotfix 3 REV1 VALIDATED
        ↓
M10.9.7.3 Hotfix 1 REV2 live Mission/Performance workspace + manual HMI validation
        ↓
M10.9.7.3 Hotfix 2 desktop-host failure/session-save integrity
        ↓
M10.9.7.4 deterministic timeline + drill-down
        ↓
M10.9.7.5 M10.9.7 closure
        ↓
M10.9.8 integrated M10 human/automation/HMI validation
        ↓
M11 release hardening
        ↓
M12 extreme-operations foundations
        ↓
M13 control-room experience
        ↓
M14 spatial reactor
        ↓
M15 accident progression and consequence models
```

---

## M10.9.7 — Mission & Performance Workstation

Detailed plan: [`milestones/M10.9.7.md`](milestones/M10.9.7.md).

### M10.9.7.3 live workspace — implementation order

The detailed milestone plan splits 7.3 into these implementation slices:

1. **Workspace identity/shell activation** — add one stable MISSION workspace, preserve COMPUTER F1–F8 and add no F9; normal desktop startup remains unbound and manual/live validation binds only an explicit exact pack.
2. **Live projection/publication boundary** — consume the validated M10.9.7.1 projector from canonical live evidence.
3. **Explicit presentation change detection** — never use generated record equality over `IReadOnlyList<>` snapshots as the UI change detector.
4. **Dedicated MissionPerformance ViewModel** — presentation only; no score/control/protection owner.
5. **Primary layout** — objective/lifecycle, distinct GRID DEMAND / REQUESTED LOAD / ACTUAL OUTPUT, score decomposition and bounded recent evidence.
6. **Contextual COMPUTER navigation** — workspace selection only; never a plant command and never F9.
7. **Keyboard/minimum-window contract** — deterministic focus and readable primary mission state.
8. **Focused + manual gate** — shell contract, no-command authority, semantic fidelity, keyboard and visual hierarchy.

### Pre-7.4 host integrity — M10.9.7.3 Hotfix 2

Before 7.4, Hotfix 2 owns desktop numerical-failure containment, start/reset/load/restore boundary consistency, picker-before-export, non-destructive/atomic local-filesystem session replacement and invariant engineering-number formatting. Focused gate: `run-m10973-desktop-host-session-integrity-audit.cmd`; current validation state remains in `PROJECT.md`.

M10.9.7.4 is blocked until this Hotfix 2 is validated. UI-thread worker migration, MainWindowViewModel decomposition, streaming persistence and simulation-speed features remain out of scope.

### M10.9.7.4 timeline/drill-down — implementation order

1. populated `sha256-control-room-snapshot-v1` golden/schema anchor;
2. protected lifecycle-spine retention separated from bounded recent operational evidence;
3. bounded deterministic timeline projection merging those sources;
4. timeline UI using logical-step ordering;
5. presentation-only drill-down targets to subsystem/alarm/COMPUTER evidence;
6. replay/checkpoint/archive-restored exact-pack equivalence and duplicate suppression;
7. focused + manual navigation gate.

### M10.9.7.5 closure

Close against a matrix including no mission, active mission, demand-following, completed/failed mission, required trip evidence, unexpected trip failure, terminal mission with continuing plant time, checkpoint restore, assistance changes and requested/effective authority changes.

M10.9.7 must close with:

- F1–F8 preserved;
- no F9;
- MISSION command authority false;
- demand/request/actual separated;
- score copied from M10.9.6 owner;
- deterministic replay/checkpoint presentation;
- manual HMI acceptance.

---

## M10.9.8 — Integrated Human / Automation / HMI Validation

Detailed plan: [`milestones/M10.9.8.md`](milestones/M10.9.8.md).

This is an **integration gate, not feature work**.

Execution order:

1. freeze machine-readable validation matrix and cross-cutting invariants;
2. run healthy 3×3 assistance × authority matrix;
3. run degraded measurement / fault / protection / takeover matrix;
4. prove replay/checkpoint/same-seed operator-visible equivalence;
5. perform full manual HMI/keyboard acceptance and close M10.

A failed row is routed to its existing owner: presentation, replay/session, challenge/scoring, M5 authority or post-M11 physical-model backlog. Phase H/I numerical tuning is reopened only with direct evidence against a validated contract.

---

## M11 — Release Hardening

Detailed plan: [`milestones/M11.md`](milestones/M11.md).

M11 is feature-frozen and proceeds in this order:

1. **M11.1** release/support contract and version freeze;
2. **M11.2** scenario/save/session/checkpoint compatibility and migration hardening;
3. **M11.3** measured performance, memory, long-run and persistence-allocation budgets;
4. **M11.4** packaging/publish/clean-target verification;
5. **M11.5** Italian manual, README/release notes/known-limitations alignment plus automated documentation-index/ADR/link/architecture-drift checks;
6. **M11.6** clean-state release-candidate final gate.

Key persistence decisions:

- schema-v1 numeric enum ordinals remain frozen unless M11.2 deliberately introduces schema v2;
- string enums, if adopted, require explicit v1 compatibility/migration tests;
- stream/`Utf8JsonWriter` persistence belongs to M11.3 only if measurements demonstrate material allocation/LOH pressure.

Key Simulation decisions from the post-7.3 review:

- M11.3 may measure and optimize temporary Pump/Valve `PipeDefinition` construction, four-node trigger-path allocations, expected infeasible-probe exception cost and thermodynamic search ceilings only when representative outputs remain exact;
- M11.3 inventories the supported `SimulationRuntime.Advance(elapsed)` callers and defines an explicit catch-up/backlog policy if the generic API is retained; the desktop five-step cooperative path is already bounded separately;
- any near-zero hydraulic constitutive smoothing, check-valve leakage/smoothing, Jacobian conditioning-policy change or trajectory-changing summation convention is **not** release optimization and routes to M12.2;
- pump shaft/electrical/loss-to-heat physical ownership routes to M12.4.

Key Application recording/replay decisions from the post-7.3 review:

- M11.2 owns future snapshot-fingerprint multi-version compatibility; a v2, if introduced, must not reinterpret historical `sha256-control-room-snapshot-v1` evidence;
- M11.3 measures full-step fingerprint JSON/SHA/hex cost, recorder collection-copy cost, long-session memory/LOH growth and the cost/meaning of per-step `LifecycleChanged`;
- M11.3 must explicitly decide recorder evidence-failure policy before claiming that recording cannot affect host execution; silently swallowing evidence failures is forbidden;
- M9.1 recording v1 remains one contiguous fingerprinted frame per logical step. Silent truncation/decimation/circular-buffer semantics require a new versioned recording contract, not an optimization.

Key desktop-host/App decisions from the post-7.3 review:

- the pre-7.4 Hotfix 2 owns correctness: numerical step-failure containment, handler policy alignment, picker-before-export and atomic/non-destructive session replacement;
- M11.3 measures UI-thread batch/projection/notification cost, archive-export responsiveness and any worker-ownership proposal; `DispatcherTimer` callbacks are serialized on the UI thread, so the problem is responsiveness coupling rather than concurrent tick overlap;
- background runtime/export work requires an explicit immutable/single-owner handoff; opportunistic `Task.Run` around mutable session state is forbidden;
- user-facing simulation-speed is a separate product decision, not an automatic release optimization.

M11 closes only when source baseline, published artifact, compatibility statement, support statement, manual and known limitations all agree.

---

## M12 — Extreme Operations Foundations — Epic A foundation

Detailed plan: [`milestones/M12.md`](milestones/M12.md).

Dependency purpose: M12 must establish the physical/state foundations before M15 is allowed to claim leaks, ruptures, fire or core damage.

Execution order:

1. **M12.1 flow-owner directionality inventory** — every flow owner classified as bidirectional, one-way by physics, one-way by explicit device or unsupported;
2. **M12.2 near-zero hydraulic regularity / conditioning audit** — measure the quadratic near-zero law, ideal check-valve non-smoothness, valve near-close behavior, normalized Jacobian/pivot diagnostics, summation semantics and branch-continuity interaction before any production smoothing;
3. **M12.3 extreme-state matrix** — near-empty inventories, pressure/temperature/void/flow extremes classified supported/reduced-fidelity/fail-closed;
4. **M12.4 pump mechanical/electrical/thermal energy ownership** — close shaft-demand and modeled inefficiency/loss-to-heat ownership before stronger conservation/consequence claims;
5. **M12.5 full-plant post-trip decay heat** — history-dependent residual heat carried through the integrated energy chain;
6. **M12.6 integrity/stress primitives** — persistent physical integrity separated from functional equipment state;
7. **M12.7 IncidentSeverity** — physical consequence severity separate from alarm priority;
8. **M12.8 closure** — directionality/numerical/extreme/pump-energy/decay/integrity/replay evidence and explicit unsupported-envelope register.

M12 **does not** yet add scripted leaks, rupture, fire or severe core damage.

---

## M13 — Control-Room Experience — Epic C

Detailed plan: [`milestones/M13.md`](milestones/M13.md).

Execution order:

1. **M13.1 industrial control/presentation boundary** — adopt/wrap/retain IndustrialControls deliberately;
2. **M13.2 maintained handle/selector semantics** — requested operator position distinct from effective equipment state where physically appropriate;
3. **M13.3 stable selection identity / command-target safety** — canonical-ID selection, clear/disable on disappearance, never silent retargeting;
4. **M13.4 first-class mimic viewport** — zoom/pan/fit/reset/selection/drill-down;
5. **M13.5 versioned persistent mimic layout** — edit/lock/reset and `EquipmentId → position` overrides;
6. **M13.6 workspace presets** — presentation-only context presets;
7. **M13.7 real operating procedures** — procedures over canonical commands/evidence;
8. **M13.8 Instructor/Fault mode** — visually distinct deterministic fault authority, never normal operator control;
9. **M13.9 integrated UX closure**, including staged `MainWindowViewModel` decomposition informed by M11.3 measurements.

Multi-monitor/multi-computer operation remains explicitly deferred.

---

## M14 — Spatial Reactor — Epic B

Detailed plan: [`milestones/M14.md`](milestones/M14.md).

Execution order:

1. **M14.1 quasi-spatial contract/fidelity limits**;
2. **M14.2 multi-zone/equivalent-channel-group reference core**;
3. **M14.3 multiple rods/rod groups with definition-owned spatial mapping**;
4. **M14.4 local/quasi-spatial power/flow/void/temperature/xenon/rod evidence**;
5. **M14.5 selectable 2D educational layers**;
6. **M14.6 local drill-down and trends**;
7. **M14.7 deterministic/replay/aggregate/performance/fidelity closure**.

The UI must never imply full channel-by-channel neutron transport unless the model actually provides it. Damage overlays remain disabled until M15 owns physical damage.

---

## M15 — Accident Progression & Consequence Models — Epic A consequence phase

Detailed plan: [`milestones/M15.md`](milestones/M15.md).

M15 is blocked until M12, M13 and M14 are closed.

Every consequence must have an initiating physical condition, persistent owner, deterministic transition, physical plant effect, replay/checkpoint/session persistence and post-incident evidence.

Execution order:

1. **M15.1 pressure-boundary degradation/leak/rupture**;
2. **M15.2 rotating-equipment degradation/failure**;
3. **M15.3 electrical damage and physically caused fire**;
4. **M15.4 core-damage prerequisite gate** — prove the complete decay-heat/cooling/local-thermal causal chain before damage code exists;
5. **M15.5 bounded core-damage progression** only if 15.4 passes;
6. **M15.6 physical IncidentSeverity/post-incident integration**;
7. **M15.7 Instructor/spatial consequence presentation** consuming canonical damage state;
8. **M15.8 integrated accident closure**.

No severe consequence is introduced merely because a scenario label says it happened.

---

## Deferred items with explicit owners

These items are intentionally deferred, not forgotten:

| Item | Owner / earliest milestone |
| --- | --- |
| score-dominance classification fail-fast | before any future challenge-pack expansion |
| session archive string enums/schema v2 | M11.2, explicit compatibility migration only |
| stream-based JSON persistence | M11.3, only with measured allocation/LOH evidence |
| scenario serializer double parsing | maintenance or M11.3 if measurable |
| DTO comparer/culture cleanup | maintenance, non-blocking |
| relief/bypass hysteresis/blowdown | post-M11 physical-model work with explicit component semantics |
| signed generator lead/lag phase representation | future synchronization/HMI fidelity work if an operator procedure requires it |
| stricter PI/PID mode-gain consistency | Domain maintenance unless behavioral evidence raises priority |
| near-zero hydraulic constitutive/conditioning change | M12.2 after frozen observational census and H/I requalification plan |
| pump mechanical/electrical/loss-to-heat ownership | M12.4 before severe full-plant energy claims |
| measured Simulation hot-path allocations / generic Advance policy | M11.3, exact-output/explicit-time-policy only |
| branch-continuity semantic retirement/unification | dedicated M12.2 evidence; current conditional corrected role remains |
| UI-thread runtime/projection responsiveness and PropertyChanged fan-out | M11.3 measurement first; worker ownership only if justified |
| stable command-target selection / no silent index retargeting | M13.3 |
| MainWindowViewModel structural decomposition | M13, staged and behavior-preserving |
| user-facing simulation-speed/pacing control | separate product decision; not implicit M11 optimization |
| multi-monitor/multi-computer operation | after M13; explicitly deferred |

## Planned focused-gate naming map

These names are planning conventions, not existing scripts. They should be used unless implementation discovers a better split.

| Work | Planned gate / closure artifact stem |
| --- | --- |
| M10.9.7.3 live MISSION | `run-m10973-mission-performance-live-workspace-audit.cmd` |
| M10.9.7.3 host/session integrity | `run-m10973-desktop-host-session-integrity-audit.cmd` |
| M10.9.7.4 timeline/drill-down | `run-m10974-mission-performance-timeline-audit.cmd` |
| M10.9.7.5 closure | `run-m1097-mission-performance-closure-audit.cmd` + manual checklist |
| M10.9.8 integrated M10 | `run-m1098-integrated-human-automation-hmi-audit.cmd` + manual closure checklist |
| M11 release closure | milestone-specific M11.1–M11.5 gates + `run-m11-release-candidate-closure-audit.cmd` |
| M12 foundations | milestone-specific M12.1–M12.8 gates + `run-m12-extreme-foundations-closure-audit.cmd` |
| M13 control-room experience | milestone-specific gates + `run-m13-control-room-experience-closure-audit.cmd` |
| M14 spatial reactor | milestone-specific gates + `run-m14-spatial-reactor-closure-audit.cmd` |
| M15 accident progression | consequence-family gates + `run-m15-accident-progression-closure-audit.cmd` |

Each closure artifact should state scope, exact owner contracts, replay/checkpoint result where applicable, manual-review requirement and explicit deferred/non-scope items.

## Transition rule

When a milestone closes, the next milestone is not started from this document. Update `PROJECT.md` to the newly validated baseline first, then start the next detailed milestone plan from that baseline.
