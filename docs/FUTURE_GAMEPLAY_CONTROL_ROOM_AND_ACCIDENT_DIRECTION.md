# Future Gameplay, Control-Room and Accident-Progression Direction

> **Status:** APPROVED FUTURE BACKLOG / ARCHITECTURE DIRECTION — decision checkpoint 2026-08-16. This document does not change the active hardening scope. G.4 closed Phase G; H.1–H.8 are validated. H.6 proved that bounded fixed-relaxation Picard rescue reaches only 6/7 frozen trigger events; H.7 proved that true-residual deterministic backtracking still reaches only 5/7 and exhausts two line searches; H.8 safeguarded Anderson also remained 5/7 with two line-search exhaustions. H.9 is the active shadow-only Jacobian-informed corrector candidate. Production remains explicit while the numerical method is still being qualified. These backlog epics remain deferred until the numerical method is settled and Phase I hardening is complete.

## 1. Purpose

The simulator must continue to gain physical and numerical rigor while also becoming a more convincing operating/training game. The accepted product direction is not to replace the existing deterministic plant model with scripted events. Instead, gameplay consequences should emerge from plant state, component limits, protection response, operator actions and persistent equipment damage.

The project therefore retains these priorities:

1. physics and causal ownership remain in Domain/Simulation, never in Avalonia presentation code;
2. interlocks exist for modeled plant reasons, not to protect a fragile solver from unsupported user actions;
3. abnormal and accident gameplay is introduced only where the engine can remain deterministic, bounded and replayable;
4. the control room remains divided into subsystem workspaces/tabs rather than becoming one giant all-controls screen;
5. future visual improvements may use `archistico/IndustrialControls`, but visual controls remain presentation-only adapters over canonical application snapshots and commands;
6. multi-monitor operation is explicitly deferred and is not part of the approved near-term backlog.

## 2. Extreme operation and out-of-envelope robustness

The simulator should eventually allow the operator to make poor or extreme decisions such as:

- closing or opening the wrong valve;
- starving or overfeeding a subsystem;
- creating reverse flow where the component contract permits it;
- operating with pumps degraded, stopped or incorrectly aligned;
- entering very low or very high pressure/temperature/inventory states;
- creating conflicting control demands;
- running through trips, loss of grid, blackout-class events, leaks and LOCA-class scenarios;
- deliberately exploring configurations far from the normal operating envelope in Instructor/Fault mode.

The desired behavior is:

```text
operator action / injected fault
        ↓
physical state changes through canonical component laws
        ↓
measurements and diagnostics change
        ↓
alarms / interlocks / protection respond
        ↓
operator recovers the plant
        OR
stress accumulates and physical integrity degrades
        ↓
persistent damage / secondary consequences
```

The undesired behavior is:

```text
unsupported operator action
        ↓
solver failure
        ↓
artificial interlock added only to protect software
```

Phase H remains a prerequisite for this direction because numerical stiffness and extreme-state handling must be measured before the simulator is deliberately exposed to more aggressive off-design states.

## 3. Component directionality and reverse flow

No global rule that “all flows may reverse” will be introduced. Directionality remains definition-owned by each physical component.

Examples of intended semantics:

- passive pipe paths may be bidirectional when their pressure/flow law supports a signed solution;
- pump paths may permit or block reverse flow depending on pump/check-valve topology;
- check valves and explicitly one-way devices remain one-way;
- atmospheric relief remains an external one-way discharge;
- turbine bypass remains a one-way header-to-condenser path;
- turbine admission/expansion remains directionally constrained unless a future explicit reverse-flow model is introduced.

A future extreme-envelope audit should classify every flow-owning component as:

```text
BIDIRECTIONAL
ONE-WAY BY PHYSICS
ONE-WAY BY CHECK/ISOLATION DEVICE
NOT YET SUPPORTED OUTSIDE CURRENT ENVELOPE
```

This classification must be explicit and testable.

## 4. Accident progression must be causal, not scripted

Existing scenarios already cover faults, trips, leaks/LOCA-class behavior, station-blackout-class composition and post-incident analysis. Future work may extend these into persistent physical consequences, but consequences must be produced by modeled mechanisms rather than arbitrary event flags.

A generic rule such as:

```text
temperature > threshold → explosion
```

is rejected.

Instead, each consequence must have an explicit causal owner. Example progressions include:

### 4.1 Pressure-boundary damage

```text
overpressure / thermal stress
        ↓
stress exposure accumulation
        ↓
integrity degradation
        ↓
leak initiation / leak growth
        ↓
rupture if the modeled failure criterion is reached
```

### 4.2 Rotating-equipment damage

```text
overspeed / adverse torque / thermal condition
        ↓
mechanical stress exposure
        ↓
degradation
        ↓
mechanical failure when supported by the model
```

### 4.3 Electrical damage and fire

```text
electrical fault / sustained overload / thermal stress
        ↓
equipment heating or arc/fault mechanism
        ↓
equipment damage
        ↓
fire only when a modeled ignition mechanism exists
```

### 4.4 Core-damage progression

A future core-damage model must be downstream of validated residual heat and thermal/hydraulic state. A plausible causal chain is:

```text
loss of cooling / loss of inventory
        +
decay heat
        ↓
fuel/channel temperature rise
        ↓
uncovery / dryout if represented
        ↓
fuel or channel damage
        ↓
progressive core damage
```

No severe core-damage claim should be added before the integrated full-plant runtime owns a credible post-trip decay-heat contribution and the necessary thermal states.

## 5. Persistent equipment integrity

Functional state and physical integrity should become separate concepts.

Candidate future integrity states:

```text
HEALTHY
STRESSED
DEGRADED
DAMAGED
FAILED
DESTROYED
```

This is independent from current operational state. For example:

```text
PUMP
command/selector: RUN
effective state:  STOPPED
integrity:        DAMAGED
availability:     NO
```

Damage accumulation should preferably depend on exposure, not only instantaneous threshold crossing:

```text
damage rate = f(severity beyond limit, duration, component/model-specific stress)
```

Damage persists for the session and is not cleared by alarm acknowledgement or ordinary protection reset. Instructor mode may later own explicit repair/replacement actions where useful for training.

## 6. Alarm priority and incident severity are different axes

Alarm/annunciator priority describes operator attention. Incident severity describes physical consequence.

Future incident reporting should therefore keep a separate severity model, for example:

```text
NONE
MINOR
MAJOR
SEVERE
CATASTROPHIC
```

This does not replace existing alarm priority. It allows states such as:

```text
ALARM PRIORITY:   CRITICAL
INCIDENT SEVERITY: MINOR
PROTECTION:       SUCCESSFUL
PHYSICAL DAMAGE:  NONE
```

or:

```text
ALARM PRIORITY:   CRITICAL
INCIDENT SEVERITY: SEVERE
PHYSICAL DAMAGE:  CONFIRMED
```

Incident severity must be evidence-derived from modeled consequences, not merely copied from alarm class.

## 7. Post-incident persistence and replay

Future damage and incident progression must remain compatible with the existing deterministic recorder/checkpoint/replay architecture.

Replay must be able to reconstruct:

- initiating action/fault;
- first abnormal measurement;
- alarm/protection chronology;
- operator actions;
- accumulated stress/damage transitions;
- resulting leaks, ruptures, fires or failures when implemented;
- final incident severity and affected equipment.

A reset may clear a latched protection only when its existing permissives allow it. It must not silently repair physical damage.

## 8. Spatial 2D reactor-core direction

The validated quasi-spatial architecture remains the starting point, but the reference plant should evolve beyond a single aggregated visual core.

The accepted direction is a **2D educational spatial core**, not a full high-fidelity 3D neutron-transport model.

The core should eventually expose multiple zones / equivalent channel groups with stable logical coordinates and visible local state. Candidate selectable layers include:

- local/relative power;
- coolant flow;
- void fraction;
- fuel temperature;
- coolant temperature;
- xenon/poison indication;
- local reactivity contribution where meaningful;
- control-rod/group position and influence;
- local warning/damage state when future integrity models exist.

The operator should be able to select a zone/channel group and inspect its values and trends.

The reference plant should also evolve from one representative rod toward multiple rods or rod groups with an explicit mapping to core zones. The model should remain reduced and educational: equivalent groups are preferred over pretending to simulate every real RBMK channel when the underlying physics does not support that claim.

## 9. Control-room workspace direction

The existing area/tab split is retained. The simulator should not converge toward one enormous dashboard containing every control.

Preferred structure remains subsystem-oriented, for example:

```text
OVERVIEW / PLANT
REACTOR
PRIMARY
TURBINE
GENERATOR
ELECTRICAL
ALARMS / INCIDENTS
OPERATOR COMPUTER
```

The whole-plant mimic is a situation-awareness and navigation surface. Detailed operation remains inside dedicated subsystem workspaces.

## 10. IndustrialControls integration

The project `https://github.com/archistico/IndustrialControls` is the preferred source of reusable Avalonia industrial controls for a future control-room visual refresh, subject to normal compatibility review at implementation time.

Expected integration areas include industrial panels, lamps, illuminated pushbuttons, gauges, selector/toggle/spring-return controls, interlock indicators, alarm indicators and trend/recorder displays.

Architecture rule:

```text
Domain / Simulation state
        ↓
Application presentation snapshots + typed commands
        ↓
Avalonia adapters using IndustrialControls
```

`IndustrialControls` must not become an owner of plant physics, protection semantics, deterministic time or canonical equipment state.

The visual target is stronger retro-industrial identity while preserving legibility, provenance, quality state, alarm semantics and modern educational usability.

## 11. Persistent operator-handle state

A future control-room realism pass should distinguish the physical equipment state from the maintained position of the operator control.

Example:

```text
before trip:
selector = RUN
equipment = RUNNING

trip occurs:
selector = RUN          ← physical handle remains where the operator left it
equipment = STOPPED
trip = ACTIVE
```

Where the real control semantics require it, restart should require an explicit operator sequence such as returning the control to STOP/RESET and then issuing RUN again after permissives recover.

This rule must not be applied blindly. The UI/control model should distinguish at least:

- maintained selector;
- maintained toggle;
- momentary pushbutton;
- spring-return switch;
- breaker CLOSE/TRIP style command;
- dedicated reset action.

The command/handle model must remain deterministic and replay-visible.

## 12. Mimic diagram as a first-class operator surface

Mimic diagrams are considered fundamental for whole-plant comprehension and should gain first-class viewport/layout behavior.

Required future capabilities:

- zoom around pointer/focus;
- pan;
- fit-to-plant / reset-view action;
- element selection and subsystem drill-down;
- explicit `EDIT LAYOUT` mode;
- drag equipment symbols in layout mode;
- optional snap/grid/alignment aids if useful;
- `LOCK LAYOUT`;
- `RESET TO DEFAULT`;
- persistent user-selected element positions;
- stable behavior across application restart.

The following separation is mandatory:

```text
CANONICAL PLANT TOPOLOGY
IDs + connectivity + process semantics
        ↓
CANONICAL DEFAULT MIMIC LAYOUT
        ↓
OPTIONAL USER LAYOUT OVERRIDES
EquipmentId → visual position
        ↓
VIEWPORT STATE
zoom + pan
```

Dragging an element must never alter plant topology, hydraulic connectivity, electrical connectivity or any simulation definition.

Layout persistence should be versioned and keyed by stable canonical equipment IDs. Missing/removed IDs are ignored; newly introduced equipment falls back to the canonical default position.

## 13. Workspace presets

Future presets should configure **presentation/workspace state**, not plant state.

Candidate presets:

- STARTUP;
- SYNCHRONIZATION;
- NORMAL GENERATION;
- REACTOR TRANSIENT;
- TURBINE TRIP;
- ELECTRICAL EVENT;
- LOCA RESPONSE.

A preset may choose initial tab/workspace, mimic viewport, visible layers, selected trend groups and expanded panels. It must not silently move valves, set controllers, alter power, clear trips or change the physical scenario.

## 14. More plant-like mnemonic displays

The existing mimic/schematic architecture should evolve toward more recognizably industrial mnemonic displays while retaining current semantic strengths:

- explicit flow direction and medium;
- equipment state;
- measured versus model provenance;
- quality/unavailable state;
- alarm/protection state;
- readable values and units;
- subsystem navigation.

Visual authenticity must not reduce educational clarity.

## 15. Real operating procedures

Future gameplay should increasingly require meaningful sequences rather than isolated button actions.

Examples may include:

- pump alignment before start;
- valve lineup before admitting steam;
- synchronization sequence;
- protection reset prerequisites;
- controlled recovery after trip;
- emergency response steps.

Procedures remain guidance/evaluation contracts over canonical commands. They must not bypass interlocks or directly write physical outcomes.

## 16. Instructor / Fault mode

Instructor/Fault mode is approved as a distinct future mode.

Normal Training mode should expose only normal operator authority. Instructor mode may inject or manipulate faults through the existing deterministic fault framework and future extensions, including component degradation, stuck valves, leaks, sensor faults, supply loss and severe-event initiators when supported.

The mode must be visually unmistakable and must not masquerade instructor-only actions as real plant controls.

Potential future Instructor authority may include explicit repair/reset of persistent damage for scenario control, but this must be separately modeled and recorded.

## 17. Multi-monitor decision

Multi-monitor / multi-computer distributed control-room operation is **not** part of the current approved backlog. It may be reconsidered later, but no current architecture or UI milestone should depend on it.

## 18. Approved sequencing

These decisions do not supersede the current hardening sequence.

Immediate continuation remains:

```text
G.3  remaining non-turbine enthalpy migration
G.4  turbine expansion / shaft-work ownership
H    numerical stiffness evidence, method selection, bounded hybrid gate and current-v2 integration
I    compatibility and engineering hardening
```

After the hardening sequence, organize the approved future product work as three large epics.

### Epic A — Extreme Operations & Accident Progression

1. extreme-envelope matrix;
2. component directionality/reverse-flow audit;
3. near-empty and extreme pressure/temperature/inventory robustness;
4. integrated decay-heat ownership after trip;
5. equipment integrity state;
6. exposure/damage accumulation;
7. evolving leaks/ruptures where supported;
8. electrical/mechanical fire/failure mechanisms where supported;
9. core-damage progression prerequisites and implementation;
10. separate incident severity;
11. persistent damage and replay evidence;
12. incident visualization and post-incident reporting.

### Epic B — Spatial Reactor

1. reference core with multiple zones/equivalent channel groups;
2. multiple rods/rod groups and zone mapping;
3. local/quasi-spatial feedback extension where physically justified;
4. 2D core map;
5. selectable power/void/temperature/xenon/flow/rod layers;
6. local drill-down and trends;
7. local alarm/damage visualization when supported.

### Epic C — Control-Room Experience

1. IndustrialControls integration;
2. stronger retro-industrial visual identity;
3. persistent operator-handle semantics;
4. real operating procedures;
5. workspace presets;
6. more plant-like mnemonic displays;
7. mimic zoom/pan;
8. mimic layout-edit mode and persistent positions;
9. Instructor/Fault mode;
10. integrated UX validation across all subsystem tabs.

## 19. Acceptance principles for future work

Every future implementation increment remains subject to the existing project discipline:

- warnings-as-errors clean build;
- deterministic fixed-step behavior;
- explicit physical/conservation ownership;
- immutable snapshots;
- no UI-owned physics;
- typed command boundaries;
- replay/checkpoint compatibility;
- regression tests and explicit audits for new dynamics;
- local validation before baseline promotion;
- known limitations stated explicitly when fidelity is educational rather than licensing-grade.
