# Project Handoff

Use this file as the authoritative continuity reference when starting a new ChatGPT conversation for Nuclear Reactor Simulator.

## How to resume safely in a new chat

Treat this file as the **authoritative continuity checkpoint**. A new conversation should not reconstruct architecture or milestone state from memory when this document, the ADRs and the milestone files already define them.

Read these documents in this order before making changes:

1. `docs/PROJECT_HANDOFF.md` — current truth, ownership rules and restart point.
2. `docs/PROJECT_STATUS.md` — capability map and deliberate omissions.
3. `docs/ROADMAP.md` — milestone sequence and phase gates.
4. `docs/ARCHITECTURE.md` — composition boundaries and state ownership.
5. the latest milestone file in `docs/milestones/` and its linked ADR/domain document.

For a ready-to-paste new-conversation bootstrap, use `docs/NEW_CHAT_START.md`. For a navigable documentation map, use `docs/README.md`.

### Restart checkpoint

- **Last explicitly locally validated baseline:** `M7.5 — Grid Synchronization & Load Increase`.
- **M6 gate:** `COMPLETE / VALIDATED`.
- **Latest implementation package:** `M7.6 — Power Manoeuvring & Normal Shutdown` baseline candidate.
- **M7.5 is explicitly validated.** M7.6 must remain a baseline candidate until the user explicitly confirms local `dotnet build` and full `dotnet test` success.
- **Immediate next action:** validate M7.6. If validation passes, record it as validated, then begin `M7.7 — Training Objectives, Procedure Guidance & Evaluation`.

### Working-package rule

When starting a new chat, use the latest complete project ZIP/source tree as the working baseline. Documentation status does not replace source validation: the source package, local compiler/test result and this handoff must agree before advancing the roadmap.

### Validation rule

A milestone is only `VALIDATED` after the user explicitly reports that the local build and complete automated suite pass. Static inspection, ZIP integrity checks or assistant-side reasoning are not a substitute for local validation. Hotfix ZIPs supersede the original candidate only after their local validation is confirmed.

## Current baseline

- Last locally validated baseline: **M7.5 — Grid Synchronization & Load Increase**.
- M3 gate: **COMPLETE / VALIDATED**.
- M4.1 main-steam/admission topology: **VALIDATED**.
- M4.2 turbine expansion/rotor dynamics: **VALIDATED**.
- M4.3 condenser/vacuum/hotwell: **VALIDATED**.
- M4.4 condensate/feedwater return: **VALIDATED**.
- M4.5 generator/grid/synchronization physics: **VALIDATED**.
- M4.6 integrated secondary-cycle heat balance: **VALIDATED**.
- M4.7 full-plant steady-state baseline / M4 gate: **VALIDATED / COMPLETE**.
- M5.1 instrumentation/signal model: **VALIDATED**.
- M5.2 controller/actuator primitives: **VALIDATED**.
- M5.3 reactor/primary automatic-control loops: **VALIDATED**.
- M5.4 turbine/steam/feedwater automatic-control loops: **VALIDATED**.
- M5.5 interlocks/trips/SCRAM: **VALIDATED**.
- M5.6 alarms/annunciator state: **VALIDATED**.
- M5.7 integrated automatic-operation baseline / M5 gate: **VALIDATED / COMPLETE**.
- M6.1 control-room application shell: **VALIDATED**.
- M6.2 reusable instrument/control component library: **VALIDATED**.
- M6.3 Reactor/Core panel: **VALIDATED**.
- M6.4 Primary-Circuit Mnemonics: **VALIDATED**.
- M6.5 Turbine, Generator & Electrical Panels: **VALIDATED**.
- M6.6 Trends, Alarms & Event Timeline: **VALIDATED**.
- M6.7 Control-Room Integration & Performance Baseline / M6 gate: **VALIDATED / COMPLETE**.
- M7.1 Versioned Initial Conditions & Scenario Framework: **VALIDATED**.
- M7.2 Cold Shutdown & Pre-Startup: **VALIDATED**.
- M7.3 First Criticality & Low-Power Operation: **VALIDATED**.
- M7.4 Heat-Up, Steam Raising & Turbine Startup: **VALIDATED**.
- M7.5 Grid Synchronization & Load Increase: **VALIDATED**.
- Current implementation candidate: **M7.6 — Power Manoeuvring & Normal Shutdown**.
- Next planned milestone after M7.6 validation: **M7.7 — Training Objectives, Procedure Guidance & Evaluation**.

## Current system ownership map

| Concern | Authoritative owner | Important rule |
|---|---|---|
| Reactor kinetics/reactivity/power | M2 | Control never writes thermal MW directly; rods → reactivity → kinetics → fission power. |
| Primary thermofluid inventories | M3 / `PlantNetworkOrchestrator` | Every conserved fluid/thermal inventory is integrated exactly once. |
| Turbine/rotor/condenser/feedwater/generator physics | M4 | Rotor/electrical state stay separate from `PlantState`; cross-domain energy is explicit and audited. |
| Instrumentation | M5.1 | Controllers/protection/UI consume measured signals; no silent true-state fallback. |
| Normal controllers/actuator commands | M5.2–M5.4 | Command seams only; canonical physical owners remain authoritative. |
| Protection/interlocks/SCRAM | M5.5 | Protection overrides normal control through validated canonical seams. |
| Alarm/annunciator memory | M5.6 | ACK/reset never resets physical protection implicitly. |
| Integrated automatic operation | M5.7 | Current-step decisions use the committed measured frame; candidate measurements belong to the next step. |
| Control-room presentation/commands | M6 | Avalonia consumes presentation snapshots and emits typed Application intents only. |
| Initialized sessions/scenarios | M7.1+ | M7.1 owns exact-version reconstruction/session/replay; M7.2 cold-shutdown/pre-start; M7.3 first-criticality/low-power; M7.4 heat-up/turbine startup; M7.5 synchronization/load; M7.6 manoeuvring/normal-shutdown guidance over canonical M2/M4/M5 seams without taking physical ownership. |

### Cross-cutting ownership principles

- Do not create a second state owner because a new UI, controller, scenario or diagnostic needs convenient access.
- Do not bypass validated seams with direct true-state reads or direct physical mutation.
- Distinguish **measured instrumentation** from **model diagnostics** in presentation.
- Keep acceptance criteria, trends, alarms and audit metrics observational; they must never correct physics to force a pass.
- Preserve deterministic logical time. Wall clock may pace a host/UI, but must not determine simulation results or event ordering.

## Non-negotiable architecture rules

1. Avalonia contains no simulation physics.
2. Fixed deterministic timestep; no wall-clock dependency in Simulation.
3. Components read one committed state per step.
4. Solvers produce balances/source terms/diagnostics; they do not mutate shared state mid-step.
5. Every conserved fluid/thermal inventory is integrated exactly once by the plant-network orchestration boundary.
6. Candidate state is checked before commit; faults fail closed.
7. Topology/state IDs are canonical and validated eagerly.
8. Global point kinetics remains the current neutronic dynamics model; spatial fidelity is projected through configurable core zones/channel groups.
9. No plant-specific RBMK constants are hardcoded into generic physics primitives.
10. Mass/energy residuals remain observable; never hide them with corrective bookkeeping.
11. Later plant phases must compose through existing seams rather than bypassing validated subsystem ownership.
12. Mechanical rotor state remains semantically separate from fluid/thermal `PlantState`; cross-domain energy transfer must be explicit and audited.
13. Condenser vacuum/pressure remains a consequence of canonical conserved exhaust inventory and thermodynamic closure, not a parallel synthetic pressure integrator.
14. Condensate/feedwater pumps remain canonical `PlantDefinition` pumps solved by the one plant-network orchestrator; semantic subsystem solvers must not integrate their balances independently.
15. While M4.4 owns condensate return, legacy M3 feedwater mass sources must be zero to prevent double mass addition.
16. Electrical grid/generator state remains separate from both `PlantState` and turbine mechanical state; grid/generator phase advances only from deterministic simulation time.
17. While M4.5 owns generator loading, legacy M4.2 manually commanded external-load torque must be zero to prevent double rotor loading.
18. M4.5 breaker closure is manual and gated by explicit synchronization conditions; automatic synchronizers, governor/AVR logic and protection remain M5 responsibilities.
19. M4.6 is an audit/composition layer only: it must not create duplicate thermofluid, rotor or electrical state or re-integrate any conserved inventory.
20. Turbine shaft work is an internal thermofluid-to-mechanical transfer and must cancel exactly once in full-path heat-balance accounting.
21. M4.7 `FullPlantState` is only a canonical envelope over existing state owners; it must not become a fourth independent physical state or integration boundary.
22. M4.7 steady-state criteria are observational gates only: long-run verification must never trim/reset inventories, rotor speed, electrical phase or commands to force a pass.
23. Plant efficiency/heat-rate diagnostics must derive only from audited power paths; undefined zero-denominator ratios remain undefined rather than using artificial epsilons.
24. M5 controllers/UI must consume measured signals rather than traverse `FullPlantSnapshot` true state directly.
25. Instrumentation/filter state is observational only and must never become a conserved plant inventory or second physical integrator.
26. Sensor faults are explicit deterministic inputs; hidden randomness and scenario scheduling do not belong in M5.1 instrumentation physics.
27. M5.2 controllers consume `MeasuredSignalFrame` only; invalid measurements must never fall back silently to `FullPlantSnapshot` true state.
28. Controller and actuator command memory is non-physical algorithm state; it must not duplicate conserved plant inventories or authoritative valve/pump/rod physical state.
29. Generic M5.2 actuator primitives emit typed command seams only; concrete plant-loop command arbitration belongs to M5.3/M5.4.
30. M5.3 reactor-power control must reuse the validated M2 control-rod, reactivity, point-kinetics and fission-power chain; never map controller output directly to thermal MW.
31. Committed control-rod state determines the current-step rod reactivity; commands generated during the step advance rod state for subsequent committed-state physics.
32. Non-rod reactivity entering M5.3 kinetics remains an explicit input seam; temperature, void, xenon and manual contributions must not be silently recomputed or hidden inside the controller layer.
33. M5.3 main-circulation pump commands replace only canonical `PumpState` operating commands before the single authoritative plant-network step; no second hydraulic integration is allowed.
34. The M5.3 controlled full-plant composition may rewrite only the existing total-fission-power input from validated kinetics; M3 core/channel spatial heat deposition remains authoritative.
35. M5.4 turbine/steam/feedwater controllers consume the same canonical `MeasuredSignalFrame` boundary as M5.3; no secondary controller may traverse full-plant true state directly.
36. Normal M5.4 turbine governing may target only canonical control/admission valves; stop valves remain reserved for M5.5 trip/isolation ownership.
37. M5.4 may project canonical valve-path flow to replace the existing M4.2 stage-group demand seam, but `PlantNetworkOrchestrator` remains the sole hydraulic and inventory integrator.
38. M5.4 feedwater/hotwell loops replace only canonical M4.4 pump operating commands; no duplicate pump or inventory state is allowed.
39. M5.3 and M5.4 automatic-control compositions must share one instrumentation definition and may not command the same physical actuator target.
40. Protection overrides, trip arbitration, SCRAM, permissives and interlocks remain M5.5 responsibilities and must not be hidden inside normal M5.4 process controllers.
41. M5.5 protection consumes the same canonical `MeasuredSignalFrame` as M5.3/M5.4; no trip/interlock may bypass instrumentation to inspect perfect true state.
42. Protection latches and interlock memory are logical state only and must not duplicate authoritative rod, valve, rotor or breaker physical state.
43. Protection arbitration has explicit priority over normal process-control commands and must act only through validated command seams: M2 rods, M4.1 stop valves, M4.2 turbine trip and M4.5 breaker open.
44. SCRAM preserves committed-state ordering: current-step kinetics uses committed rod position while SCRAM insert commands advance candidate rod state.
45. M5.5 reset is explicit and permissive-gated; alarm acknowledgement/presentation must not implicitly reset physical protection.
46. M5.6 owns alarm/annunciator semantics and must observe protection state rather than become a hidden protection trigger owner.
47. Alarm acknowledgement/reset state is operator-presentation memory only and must never reset M5.5 trip latches, clear interlocks or alter physical actuator commands.
48. First-out and alarm-event ordering use deterministic logical sequence numbers only; wall-clock timestamps do not belong in Simulation.
49. M5.6 alarm conditions may observe M5.1 measured channels or M5.5 protection snapshots, but protection remains the sole owner of SCRAM/trip/interlock decisions.
50. M5.7 current-step control/protection/alarm decisions consume the committed measured frame; instrumentation derived from candidate true state is published only for the next logical step.
51. M5.7 verification phases are explicit immutable input sequences only; they must not become a hidden scenario scheduler or force predetermined physical outcomes.
52. M5.7 acceptance criteria are observational gates only and must never trim state, retune controllers, clear protection/alarm memory or substitute true-state values for invalid measurements.
53. M6 Avalonia views/view models consume presentation snapshots only and must not traverse authoritative `FullPlantSnapshot`/`PlantState` or Simulation namespaces directly.
54. M6 operator actions leave Avalonia through application command-dispatch boundaries; UI code must never execute simulation physics or advance deterministic time itself.
55. UI refresh/performance budgets are presentation concerns only and must never influence simulation timestep, control ordering or physical results.
56. M6 reusable components render semantic presentation state supplied by presentation logic; they must not infer warning/trip thresholds from authoritative physical truth.
57. Display instruments are read-only; interactive controls use Application command seams and must not mutate simulation state directly.
58. `Unavailable` interactive controls fail closed at the UI boundary by disabling command acceptance rather than issuing hidden/default commands.
59. M6 domain panels must distinguish measured instrumentation from explicitly labelled model diagnostics; diagnostics must never masquerade as measured protection/control inputs.
60. Reactor/core operator actions leave Avalonia only as typed Application command intents; rod, kinetics and protection state remain owned below the UI boundary.
61. Missing operational data seams are rendered `Unavailable`; UI/application presentation code must not reconstruct or invent hidden physical state (including xenon state absent from M5.7).
62. M6 primary-circuit mnemonics preserve the measured-versus-model-diagnostic distinction; non-instrumented true-state diagnostics must be labelled and never masquerade as M5.1 signals.
63. Primary mnemonic topology derives only from canonical M3 definitions/state; UI presentation must not invent equipment or duplicate hydraulic ownership.
64. MCP operator actions leave Avalonia as typed Application pump intents; M5.3 command arbitration and the single M3 hydraulic integration remain authoritative.
65. M6 turbine/electrical panels preserve M4/M5 ownership: measured channels remain distinct from model diagnostics, and Avalonia must not reproduce turbine, condenser, generator or grid physics.
66. Breaker-close UI enablement may fail closed from the published synchronization permissive, but M4.5 remains the authoritative synchronization/close-check owner.
67. Turbine-speed, generator-load, breaker and trip controls leave Avalonia only as typed Application intents; UI code defines no physical ramp rates, load increments or protection outcomes.
68. M6.6 trend history is presentation state indexed only by `ControlRoomSnapshot.LogicalStep`; UI refresh cadence or wall clock must not create extra time samples.
69. M6.6 annunciator and first-out presentation mirrors validated M5.6 state; Avalonia must not recompute alarm/protection decisions.
70. Alarm ACK/reset actions leave Avalonia as typed Application intents for M5.6 memory only and must never reset M5.5 protection implicitly.
71. Event timeline ordering comes only from M5.6 monotonic logical sequence numbers; bounded UI history is observational and replay-compatible.
72. M6.7 runtime coordination separates deterministic M5.7 stepping from presentation publication; sparse publication must never skip or merge physical steps.
73. One-shot protection, breaker and annunciator commands are consumed for one deterministic step only; persistent controller/setpoint changes remain explicit immutable runtime inputs.
74. Accelerated execution is cooperatively batch-bounded for UI responsiveness, but batch size/publication stride never changes fixed simulation timestep or results.
75. M6.7 does not invent an initialized plant session; versioned initial-condition/scenario creation remains M7.1 ownership.
76. M7.1 initial conditions are immutable exact `(InitialConditionId, Version)` contracts; scenario/session loading must never silently resolve to a newer "latest" version.
77. Initial-condition factories reconstruct fresh canonical lower-layer runtime ownership; scenario/Application/UI code must not deserialize or synthesize individual M1–M5 physical/control/protection state owners piecemeal.
78. Scenario metadata/objectives are declarative only and must never patch physical state or force a predetermined outcome.
79. Scenario operator-action permissions fail closed before commands reach the runtime; run/pause/single-step remain runtime-host controls rather than scenario physical permissions.
80. Scenario schema migrations may reshape metadata but must preserve exact initial-condition identity/version; unknown future schema versions fail closed.
81. M7.1 replay reuses logical-step command traces and explicit fixed stepping only; wall clock, UI cadence and publication cadence must not determine replay ordering or result.
82. General arbitrary full-state checkpoints/seek remain M9.1 ownership; M7.1 must not create a parallel checkpoint state owner.
83. M7.2 concrete initial conditions are deterministic construction recipes composed through canonical M1–M5 definitions/state owners and validated solvers; scenario/Application/UI code must not post-load patch authoritative physical state.
84. M7.2 pre-start readiness checks consume immutable `ControlRoomSnapshot` presentation data only; they are observational and must never command actuators, clear protection or correct physics to become satisfied.
85. Guided preparation steps are declarative. A suggested operator action may be displayed, but guidance must never auto-dispatch it or advance deterministic time.
86. The M7.2 scenario permission set stops before first criticality: rod withdrawal and generator-breaker closure remain unavailable until later operational milestones explicitly own them.
87. The desktop may load the exact `cold-shutdown-pre-start` v1 session through the validated M7.1 registry/session boundary; Avalonia remains presentation-only and does not construct the physical object graph.
88. M7.3 source-range neutron population is explicit versioned initial-condition data for the existing homogeneous M2 kinetics; scenario code must not add a hidden external-source solver or directly set thermal power during runtime.
89. M7.3 rod INSERT/HOLD/WITHDRAW actions use only the validated M5.3 command/actuator seam; guidance and acceptance checks must never patch rod position, reactivity or neutron population directly.
90. M7.3 first-criticality/low-power checks consume immutable `ControlRoomSnapshot` data only and remain observational; steam-path opening, turbine acceleration, breaker closure and generator loading remain outside M7.3 permissions.
91. Quantitative xenon remains explicitly unavailable while canonical M2.8 iodine/xenon state is absent from the M5.7 operational envelope; scenario/UI layers must not synthesize, estimate or privately integrate xenon reactivity.
92. M7.4 startup lineups are exact versioned initial-condition data composed through canonical M1–M5 owners; scenario/UI layers must not create a second stop-valve, steam-pressure, rotor-speed or inventory owner.
93. M7.4 turbine roll/acceleration uses only the validated M5.4 turbine-speed controller seam through typed Application intents; guidance must never set governing-valve position or rotor speed directly.
94. M7.4 heat-up, steam-pressure, drum-inventory and turbine-speed checks consume immutable `ControlRoomSnapshot` data only and remain observational; they must never trim inventories, force pressure/temperature or advance time automatically.
95. M7.4 scenario permissions fail closed on generator-breaker close and generator-load raise/lower; synchronization, breaker closure and initial loading remain M7.5 ownership.
96. Missing generator electrical-output measurements remain unavailable and must not be reconstructed from true state in scenario/UI code; breaker isolation may be observed only from the published operational snapshot.

## Validated progression

- M0: deterministic runtime/engineering foundation.
- M1: quantities, fluid/thermal nodes, pipes, valves, pumps, heat transfer, simplified water/steam.
- M2: reactivity, rods, point kinetics, fission power, decay heat, temperature/void feedback, iodine/xenon.
- M3.1–M3.8: canonical integrated primary circuit, staged orchestration, zones/channels/circulation/drums/boundaries, plant snapshot and deterministic long-run gate verification.
- M4.1: canonical main-steam lines/headers, exact stop/control/admission valve trains, zeroed legacy M3 export seam, replaceable turbine-admission boundary and single-integration M3+M4 composition.
- M4.2: conservative turbine inlet-to-exhaust transfer, explicit shaft-work extraction, separate rotor mechanical state, deterministic torque/speed integration, mechanical audit and explicit trip/overspeed seams.
- M4.3: one-to-one turbine-exhaust condenser ownership, conservative condensation to canonical hotwells, explicit cooling-boundary heat rejection and pressure/vacuum diagnostics derived from conserved state.
- M4.4: canonical hotwell-to-steam-drum condensate/feedwater return using existing pumps, conserved feedwater inventory, zeroed legacy M3 feedwater source and explicit thermal-conditioning energy accounting.
- M4.5: deterministic synchronous-generator/grid state, manual synchronization/breaker gating, electromagnetic rotor loading and shaft-to-electrical conversion/loss audit.
- M4.6: canonical integrated secondary-cycle composition and closed-loop mass / reactor-to-grid first-law reconciliation across thermofluid, mechanical and electrical audits.
- M4.7: canonical full-plant state/snapshot boundary, fixed-input operating-point gate, long-run drift verification and audited performance diagnostics; M4 gate complete.
- M5.1: canonical measured-signal boundary over immutable full-plant truth, deterministic instrument lag/scaling/quality and explicit sensor-fault seams.
- M5.2: deterministic measured-signal-only P/PI/PID primitives, manual/automatic modes, anti-windup/bumpless transfer and typed valve/pump/rod command seams.
- M5.3: measured reactor-power rod regulation through validated M2 kinetics plus canonical main-circulation pump support without duplicate hydraulic integration.
- M5.4: measured turbine/load/steam-pressure admission control plus drum-level/feedwater and hotwell/condensate pump loops over canonical owners.
- M5.5: measured-signal trip/interlock protection with explicit SCRAM/turbine/generator arbitration and permissive-gated reset.
- M5.6: deterministic alarm/annunciator memory with ACK/reset separation, first-out grouping and logical event ordering.
- M5.7: canonical committed-measurement automatic-operation composition, deterministic multi-step verification gate and complete M5 control/protection/alarm integration; M5 gate complete.
- M6.1: scalable control-room shell, snapshot-driven presentation boundary, typed application command dispatch, App/Simulation dependency separation and presentation-only performance budgets.
- M6.2: reusable semantic instrument/control component vocabulary with Normal/Warning/Trip/Unavailable states and fail-closed unavailable interaction.
- M6.3: measured/diagnostic Reactor/Core presentation with canonical rod/protection command seams.
- M6.4: topology-aware primary-circuit mnemonic with measured loop/drum instrumentation and typed MCP intents.
- M6.5: turbine/secondary and electrical operating panels with measured instrumentation, labelled diagnostics and typed speed/load/breaker/trip intents.
- M6.6: bounded logical-step trends, annunciator/first-out presentation and deterministic sequence-ordered event timeline.
- M6.7: live M5.7 runtime adapter/coordinator, complete typed operator-command translation, bounded accelerated execution and rendering-cadence-independent publication; M6 gate complete.
- M7.1: exact-version initial-condition factories/registry, versioned scenario schema, fail-closed action gating, fresh paused sessions and deterministic logical-step replay.
- M7.2: validated concrete cold-shutdown/pre-start v1 recipe, observational readiness checks, declarative guided preparation and initialized paused desktop composition.
- M7.3: validated exact pre-criticality/source-range v1 handoff, controlled rod permissions, observational criticality/low-power guidance and explicit xenon availability boundary.
- M7.4 validated: exact low-power-steam-raising v1 handoff, versioned startup steam lineup, observational heat-up/steam/turbine checks and validated turbine-speed governing path.
- M7.5 validated: exact pre-synchronization-grid-loading v1 handoff, canonical M4.5 close-check/breaker transition, bounded requested electrical-load changes and stable low-load handoff.

## M7.5 validated / M7.6 candidate

M6.1 through M6.7 are locally validated and the M6 gate is complete. M7.1 through M7.5 are locally validated. M7.1 owns exact-version initial-condition/session/scenario/replay boundaries; M7.2 supplies cold-shutdown/pre-start; M7.3 supplies first-criticality and low-power operation; M7.4 supplies heat-up/steam raising/turbine startup; M7.5 supplies canonical synchronization, breaker closure and initial load increase.

M7.6 adds exact `stable-low-load-parallel-operation` v1 with main circulation established, turbine near synchronous speed, generator breaker already closed and a bounded 5 MWe requested-load handoff. Broader load raise/lower remains only a change to canonical M4.5 requested electrical power; reactor response remains M2/M5.3 ownership and turbine governing remains M5.4 ownership. Temperature and void are observed from published core diagnostics. Quantitative xenon remains explicitly unavailable at the M5.7 operational snapshot boundary and is never reconstructed by Application.

Normal shutdown is procedural/orchestrational only: unload generator, open breaker through M4.5, insert rods through M5.3/M2, reduce turbine speed through M5.4 and preserve main circulation for post-shutdown cooling. SCRAM/trips remain available safety actions but are not the routine normal-shutdown mechanism.

## New-chat implementation protocol

When the next conversation begins:

1. Inspect the supplied/latest source package before editing.
2. Read the restart documents listed above.
3. Confirm the latest locally validated milestone and the current candidate.
4. Never re-implement a subsystem already owned by an earlier validated milestone; compose through its seam.
5. For a failed build/test, patch the smallest responsible layer and keep the milestone `BASELINE CANDIDATE` until local validation passes.
6. After validation, update at minimum `PROJECT_HANDOFF.md`, `PROJECT_STATUS.md`, `ROADMAP.md`, the milestone file, relevant ADR status, README/CHANGELOG and application metadata before starting the next milestone.

### Exact continuation point

At the time of this documentation refresh, the correct continuation is:

```text
M6.7 — VALIDATED / M6 GATE COMPLETE
        ↓
M7.1 — VALIDATED
        ↓
M7.2 — VALIDATED
        ↓
M7.3 — VALIDATED
        ↓
M7.4 — VALIDATED
        ↓
M7.5 — VALIDATED
        ↓
M7.6 — BASELINE CANDIDATE, awaiting explicit local validation record
        ↓ after successful validation
M7.7 — Training Objectives, Procedure Guidance & Evaluation
```

M7.1 establishes validated versioned initial-condition/session/scenario ownership. M7.2 supplies cold-shutdown/pre-start. M7.3 supplies validated first-criticality/low-power operation. M7.4 supplies validated heat-up/steam-raising/turbine startup. M7.5 supplies validated grid synchronization and initial load. M7.6 is the current power-manoeuvring/normal-shutdown candidate; M7.7 follows only after explicit M7.6 validation.


### M7.5 ownership additions

96. M7.5 scenario permission to request generator-breaker closure never replaces the canonical M4.5 synchronization close-check; frequency, phase and voltage permissives remain authoritative below Application.
97. M7.5 generator load raise/lower changes only bounded canonical M4.5 requested electrical power; Application/UI must never write electromagnetic torque, rotor speed or electrical output directly.
98. Initial-load coordination remains explicit across existing owners: M4.5 electrical loading, M5.4 turbine-speed governing and M2/M5.3 rod-reactivity-kinetics. Scenario checks are observational only.


### M7.6 ownership additions

99. M7.6 generator-load manoeuvring changes only bounded canonical M4.5 requested electrical power; scenario/UI never writes electrical output, electromagnetic torque or rotor speed directly.
100. M7.6 reactor power changes remain rod-command → M2 reactivity/kinetics/fission-power ownership; normal-shutdown guidance must not directly write thermal MW or reactivity.
101. M7.6 temperature and void response checks are observational projections only. Quantitative xenon remains explicitly unavailable until promoted through the canonical operational snapshot boundary; Application must not reconstruct it privately.
102. Normal shutdown is an ordered use of validated seams: unload, breaker open, controlled rod insertion, turbine rundown and continued main circulation. SCRAM/trips remain protection/safety actions and are not redefined as routine procedural owners.
