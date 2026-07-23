# Operator Experience & HMI Architecture

**Approved direction:** M10.9 operator-experience refactor  
**Validated baseline:** M10.9.2 Hotfix 2 — Advanced Instrument & Gauge System  
**Current candidate:** M10.9.4 — Subsystem Engineering Schematics  
**Validated baseline:** M10.9.1 — HMI Information Architecture & Visual Language

## 1. Product objective

The simulator is educational because the operator must **perform**, diagnose and recover, not because the UI simply displays explanatory text.

The long-term interaction model combines:

- timed operating objectives such as startup, shutdown, testing, power manoeuvring, stabilization and fault recovery;
- deterministic external electrical-demand profiles that the plant must follow;
- scoring for objective time, demand tracking, stability and safety/procedure quality;
- progressive guidance that remains independent from Manual / Assisted / Supervisory Automatic plant-control authority.

Safety/procedure has priority over stability, demand tracking and speed. The scoring model must never reward unsafe operation.

## 2. Four operator questions

Every primary screen should help the operator answer quickly:

1. **Where am I?** — which system/equipment is in context?
2. **How is it going?** — normal, approaching a limit, alarmed, tripped, unavailable, rising or falling?
3. **Why?** — which connected equipment, flows, feedbacks or controls influence the condition?
4. **What happens if I act?** — direct command effect, expected downstream dependencies, permissives/blockers and what to monitor afterward.

## 3. Persistent shell

The HMI is structured around:

- situation strip;
- compact system navigation rail;
- large central engineering workspace;
- contextual inspector;
- alarm/event strip.

The future plant mimic is the primary situation-awareness surface. The M10.8 operator computer remains a validated utility workstation rather than competing with the physical plant as the main mental model.

## 4. Visual hierarchy

Use a restrained futuristic industrial language:

- graphite/dark surfaces;
- cyan/ice information accent;
- white/high-contrast numerical data;
- green = confirmed healthy/available;
- amber = attention/approaching limit;
- red = alarm/trip/protection only;
- subtle motion only when it conveys flow, direction, trend or selection;
- avoid decorative glow, excessive cards and indiscriminate color.

Red must remain rare enough to carry immediate operational significance.

## 5. Engineering schematic grammar

### Full plant

The future plant mimic should make the principal energy/mass path legible:

```text
REACTOR → STEAM DRUMS → TURBINE → GENERATOR → GRID
   ↑            ↓          ↓
   └─ MCPs ← FEEDWATER ← CONDENSER
```

Every equipment visual must make inputs and outputs clear.

### Process lines

Pipes/lines should support:

- flow direction;
- fluid/phase identity;
- pressure;
- temperature;
- flow magnitude/status;
- local alarm/quality state;
- restrained color/animation that does not confuse hot-fluid color with alarm color.

### Instrumentation/control/protection

Signal-flow diagrams use a different visual grammar from process piping:

```text
PLANT → MEASUREMENTS → CONTROLLERS → COMMAND ARBITRATION → PLANT
             ↓                              ↑
         PROTECTION ────────────────────────┘
             ↓
           ALARMS
```

Protection always overrides normal/supervisory control.

## 6. Gauge semantics

The UI must never equate "inside instrument scale" with "acceptable".

```text
instrument range
    ≠ normal operating range
    ≠ scenario target range
    ≠ warning/alarm limits
    ≠ protection/trip limits
```

M10.9.2 advanced gauges consume immutable HMI scale metadata from Application. Threshold ownership stays with the canonical subsystem/protection/scenario/controller owner. Linear gauges are the default bounded engineering instrument; circular gauges are used selectively where dial position materially improves awareness. Provenance, quality, target/setpoint/protection layers, explicit off-scale state and logical-step trends remain independent semantics.

## 7. Command consequence presentation

A focused command should eventually expose:

- **Direct effect** — deterministic command/actuator intent;
- **Expected downstream influence** — model/topology relationship, not fake prediction;
- **Permissives/blockers** — authoritative current availability;
- **What to monitor** — relevant measurements and state;
- **Observed response** — post-command changes actually seen in the simulation.

Expected influence and observed response must remain explicitly separate. The HMI must not claim causal certainty that the simulation evidence does not support.

## 8. Approved M10.9 sequence

1. **M10.9.1 — HMI Information Architecture & Visual Language**
2. **M10.9.2 — Advanced Instrument & Gauge System**
3. **M10.9.3 — Interactive Full-Plant Mimic**
4. **M10.9.4 — Subsystem Engineering Schematics**
5. **M10.9.5 — Contextual Command Consequence Model**
6. **M10.9.6 — Operational Challenge & Energy-Demand Framework**
7. **M10.9.7 — Mission & Performance Workstation**
8. **M10.9.8 — Integrated Human-Automation-HMI Validation Gate**

M10 closes only after M10.9.8 is validated.
