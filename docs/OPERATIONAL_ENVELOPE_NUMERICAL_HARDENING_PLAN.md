# M10.9.4.1 — Operational Envelope & Numerical Hardening Plan

## Status

**IN PROGRESS — Phase A failure attributed; A.2 Hotfix 1 candidate pending validation**

**Validated prerequisite:** M10.9.4 — Subsystem Engineering Schematics.

M10.9.4.1-A Hotfix 1 compiles and the ordinary suite passes, but the explicit extended audit is not green. The repeated 300-second steady/5 MWe journey latched turbine and generator trip near 70 simulated seconds. The action signature and bounded speed/frequency identify `condenser-high-backpressure`. A.2 Hotfix 1 is the current candidate.

## Purpose

Hotfix 13–23 repaired canonical defects discovered by the M10.9.4 long-running gameplay tests. The first 300-second audit now proves that the 60-second validated point does not remain healthy over the extended envelope. Remaining work is physical/numerical hardening and must stay separate from HMI/schematic scope.

## Governing rules

1. Audit before modifying production physics.
2. One structural concern per candidate.
3. Every correction requires a short regression that fails under the old behavior.
4. Long-running success alone is not proof of correctness.
5. Conservation closure alone is not proof of a valid operating point.
6. No seed tuning may compensate for a missing conservation, inventory or feedback law.
7. Protection thresholds and acceptance floors are not weakened to make a journey green.
8. External simulation timestep, replay ordering and canonical state ownership remain unchanged unless explicitly superseded.
9. Legacy compatibility is isolated and may not constrain current-model correctness.
10. Any future steady-state trim is offline/versioned; runtime hidden repair is forbidden.

## Phase A — Extended operating-envelope audit — EXECUTED / FAILURE FOUND

Implemented evidence includes:

- 300 simulated seconds at the intended 5 MWe parallel point;
- deterministic load raise/lower;
- breaker-open, generator-trip and turbine-trip load rejection;
- condenser-cooling degradation;
- current-v2 pump non-return behavior;
- mass/energy audit, drum pressure/level, condenser pressure, turbine speed, generator frequency and protection state;
- replay/checkpoint equivalence.

Observed failure is recorded in `M10_9_4_1_A_EXTENDED_AUDIT.md`. The audit fulfilled its purpose by exposing a long-horizon trip; Phase A cannot be promoted as green.

## Phase A.1 — Audit Evidence Completion — IMPLEMENTED IN A.2 CANDIDATE

### Goal

Publish direct one-second protection/limiter/exhaust evidence around the already attributed condenser-backpressure edge.

### Required diagnostic additions

- per-step protection-function evidence: identifier, measured value, trip/reset thresholds, active/latched state and action;
- per-step extrema around any trip edge, especially condenser pressure, rotor speed and generator frequency;
- condenser actual condensation flow and each independent candidate limit: inventory, thermal/UA, cooling-capacity and maximum-flow;
- active condenser limiter plus absolute/relative margin;
- masses and energy for `exhaust`, `hotwell`, `feedwater-inventory`, drum steam/liquid and admission-train nodes;
- final-window slope and total excursion for each conserved inventory and principal operating variable;
- pump suction pressure, commanded speed, actual speed, discharge check-valve state and transition count;
- controller output, integral state and physical actuator position for speed, level and hotwell loops;
- turbine stage flow, inlet phase/vapor fraction, available/extracted work and shaft power;
- generator requested/actual/mechanical/electromagnetic powers and breaker state.

### Additional gates

- deterministic thermodynamic property sweeps for coverage, continuity and monotonicity;
- deterministic governor-authority map;
- `REFERENCE_PLANT_SCALE_CONTRACT.md` decision evidence;
- supported legacy/current profile matrix draft;
- 300-second reference trajectory with extrema and final-window slopes.

The journey extends to 600 seconds only if the 300-second final-window slope remains ambiguous after the trip cause is removed or isolated. Runtime must not be lengthened merely to obtain a green result.


## Phase A.2 — Condenser Installed-Capacity Headroom — CURRENT CANDIDATE

### Scope

- preserve cooling water at 20 °C and `UA = 1.225 MW/K`;
- preserve the 40 °C initial surface-transfer value of 24.5 MW;
- raise only current-v2 installed cooling-boundary capacity from 24.5 to 40 MW;
- raise only current-v2 maximum condensation flow from 15 to 20 kg/s;
- keep the 30 kPa protection threshold and all solver equations unchanged;
- leave all legacy/v1 seeds byte-for-byte behaviorally unchanged.

### Rationale

The previous boundary ceiling clipped the existing `min(Q_available, UA * ΔT)` feedback exactly at its design point. Any pressure rise could not unlock additional heat rejection, while the 15 kg/s hard flow ceiling had negligible margin over the turbine path. A.2 restores installed-capacity headroom without changing the initial design point or introducing a new condenser law.

### Gate

Build, ordinary suite, both `GameplayLong` journeys and the complete `OperationalEnvelopeAudit` trait must pass. The new one-second evidence must show no unexplained trip and identify the active condenser limiter throughout the healthy reference journey.

## Phase B — Drum and Source Inventory Closure

### Scope

- replace the current demand-following drum-to-steam supplement with a current-v2 energy/pressure/state/inventory-driven source closure;
- constrain recirculation by physically available liquid inventory;
- define behavior when the drum loses separable liquid;
- publish low-inventory and pressure-outside-design-envelope diagnostics;
- add low-drum-level protection/interlock only after the physical inventory semantics are correct.

### Required regressions

- no sustained steam generation without available energy and inventory;
- increasing source heat increases available steam generation monotonically within the supported envelope;
- steam export and recirculation debit the correct mass/energy owners exactly once;
- no liquid recirculation is fabricated from a fully vapor state;
- current and legacy profiles remain explicit.

## Phase C — Condenser Phase-Change Closure

### Scope

- define the energy state of condensate entering the hotwell;
- close steam-space-to-hotwell mass and energy consistently;
- expose every active condensation limit and margin;
- retain, rescale or remove capacity limits only from measured evidence;
- preserve cooling-water temperature and installed-capacity ownership as explicit boundaries.

### Required regressions

- phase-change mass and energy close exactly once;
- hotwell energy responds to condensation according to the accepted control-volume law;
- pressure feedback remains continuous across limiter changes;
- cooling degradation raises backpressure without hidden state repair;
- no over-condensation or inventory depletion is masked by seed retuning.

## Phase D — Turbine Admission and Governor Authority

### Scope

- define a shared current-v2 wet-steam/liquid-admission policy for stage flow and work;
- measure and correct control-valve/stage authority;
- choose resistance rescaling, effective area or a Stodola/ellipse-style law from evidence;
- add tracking anti-windup only if command/position divergence produces material windup;
- review torque-reference continuity separately.

### Required regressions

- liquid admission cannot silently become a zero-work mass bypass;
- valve opening has monotonic, material authority over mass flow and shaft power;
- rate-limited actuator response remains bounded without persistent integral windup;
- load raise/lower is deterministic and returns to the accepted trajectory.

## Phase E — Generator/Grid Scale and Bidirectional Coupling

### Scope

- resolve `REFERENCE_PLANT_SCALE_CONTRACT.md`;
- align machine rating, rotor inertia, load range, droop and synchronizing limits;
- support signed electromagnetic power/torque and motoring;
- add reverse power, supervised underfrequency and loss-of-synchronism only after the physical states exist;
- review power/torque conversion at actual versus rated speed.

### Required regressions

- both positive generation and negative motoring are physically representable;
- disconnected/coast-down conditions do not create false electrical trips;
- breaker-closed synchronization has restoring behavior in both slip directions;
- inertia and droop produce the documented scale response;
- replay remains deterministic across protection events.

## Phase F — Relief and Bypass with Choked Flow

### Scope

- add a canonical compressible-flow primitive with critical/choked-flow behavior;
- add conservative turbine bypass/steam dump and pressure-actuated relief paths;
- make condenser backpressure constrain bypass capacity physically;
- preserve protection priority and explicit destination ownership.

### Required regressions

- load rejection does not require scripted pressure repair;
- relief/bypass mass and energy integrate exactly once;
- choked capacity depends on upstream state in the critical regime;
- downstream pressure regains influence only outside the critical regime.

## Phase G — Flow Work and Enthalpy Transport

This is a dedicated whole-network migration, not a condenser or turbine hotfix.

### Scope

- define the accepted open-control-volume energy convention;
- introduce enthalpy transport or explicit flow-work terms;
- prevent double counting with pump work, turbine work and boundary powers;
- migrate components incrementally with local and global audit equivalence.

### Required regressions

- throttling and advection follow the accepted energy invariant;
- internal transfers preserve global energy to the configured tolerance;
- pump/turbine work appears exactly once;
- new reference trajectories quantify the physical change.

## Phase H — Numerical Stiffness Decision Gate

Before adaptive substepping is implemented, measure:

- timestep sensitivity and observed convergence order;
- dominant fractional mass/energy changes;
- compressed-liquid pressure/flow stiffness;
- runtime cost per simulated second;
- whether explicit substeps converge at an acceptable bounded cost.

Decision:

- use bounded deterministic adaptive substeps if sufficient;
- otherwise use an explicitly designed semi-implicit pressure/flow treatment;
- never use wall-clock adaptation or hidden nonlinear repair.

## Phase I — Compatibility and Engineering Hardening

- document supported current/legacy profile combinations;
- define legacy replay migration/retirement policy;
- consolidate existing conservation audits into one observation contract without creating a second physics owner;
- extend versioned reference trajectories and tolerance budgets;
- add continuous integration for ordinary gates and scheduled/manual long gates;
- maintain `KNOWN_MODEL_LIMITATIONS.md`;
- prototype an offline deterministic steady-state seed compiler only after residuals and scale are defined;
- perform prudent dead-code and documentation cleanup.

## M10.9.4.1 acceptance gate

All of the following are required:

```text
clean restore/build with warnings as errors
complete ordinary suite
existing explicit 60-second journeys
healthy 300-second reference journey with no unexplained trip
load-step, load-rejection and cooling-degradation journeys
per-step protection-trigger evidence
same-seed/replay/checkpoint determinism
mass/energy closure plus inventory-slope evidence
resolved reference-plant scale contract
versioned trajectory/tolerance evidence
performance budget within agreed limits
known-limitations and compatibility records updated
```

Only after this gate passes does work advance to M10.9.5.

## Forward sequence after M10.9.4.1

1. M10.9.5 Contextual Command Consequence Model.
2. M10.9.6 Operational Challenge & Energy-Demand Framework.
3. M10.9.7 Mission & Performance Workstation.
4. M10.9.8 Integrated Human-Automation-HMI Validation Gate.
5. M11 release hardening, packaging and final validation.
