# M10.9.4.1 — Operational Envelope & Numerical Hardening Plan

## Status

**IN PROGRESS — corrected A.3 operating seed, B.1–B.3 drum/inventory/protection closure and C.1 condenser phase-change energy locally validated; C.2 explicit condenser installed-capacity ownership active candidate**

**Validated prerequisite:** M10.9.4 — Subsystem Engineering Schematics.

The original extended audit exposed a repeatable long-horizon trip, but follow-up investigation identified the root cause upstream of the condenser: the current-v2 sustained-generation seed returned only part of fuel/structure power to the coolant, primary circulation was severely under-driven, and the drum/main-steam path slowly exhausted available internal energy. The corrected current-v2 seed now closes conservative solid-to-coolant heat transfer, primary hydraulic circulation and initial steam-line conditions while preserving historical v1 seeds and all protection thresholds. The user locally validated the exact 300-second sustained journey, the explicit 60-second synchronization journey, a zero-warning/zero-error build and 895 ordinary tests with 11 explicit tests filtered out and zero failures. B.1–B.3 have passed local compilation/tests and Phase B is locally green. C.1 also passed locally and is the validated condenser phase-change-energy checkpoint. C.2 is the active candidate for explicit installed-capacity ownership.

## Purpose

Hotfix 13–23 and the corrected current-v2 sustained-generation seed repaired canonical defects discovered by the long-running gameplay/audit gates. The 300-second audit proved valuable by exposing an integrated energy/hydraulic seed imbalance that the shorter journey did not reveal. Remaining work is physical/numerical hardening and must stay separate from HMI/schematic scope.

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

## Phase A — Extended operating-envelope audit — EXECUTED / ROOT CAUSE RESOLVED

Implemented evidence includes:

- 300 simulated seconds at the intended 5 MWe parallel point;
- deterministic load raise/lower;
- breaker-open, generator-trip and turbine-trip load rejection;
- condenser-cooling degradation;
- current-v2 pump non-return behavior;
- mass/energy audit, drum pressure/level, condenser pressure, turbine speed, generator frequency and protection state;
- replay/checkpoint equivalence.

The historical failure and its corrected root-cause analysis are recorded in `M10_9_4_1_A_EXTENDED_AUDIT.md`. The corrected current-v2 seed has passed the exact 300-second sustained journey locally without weakening protection thresholds.

## Phase A.1 — Audit Evidence Completion — IMPLEMENTED / CONTINUING AS CROSS-PHASE EVIDENCE

### Goal

Publish direct one-second protection/limiter/exhaust evidence around any long-horizon protection edge so downstream trip functions can be distinguished from upstream root causes.

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


## Phase A.2 — Condenser Installed-Capacity Headroom — IMPLEMENTED CANDIDATE, NOT ROOT-CAUSE FIX

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

The condenser-capacity change must be evaluated independently during Phase C against the now-corrected operating seed. It must not be credited as the fix for the historical ~70-second failure. Build, ordinary suite, long journeys and condenser-limiter evidence must remain green before any condenser change is promoted.

## Phase A.3 — Reference Plant Scale Evidence / Corrected Operating Seed — LOCALLY GREEN CHECKPOINT

### Scope

- freeze the current 1,000 MW generator nameplate, 1,000 kg·m² rotor, 5 MW request, 150 rpm full-load droop and 10 MW coupling values as explicit evidence rather than implicit constants;
- derive stored rotor energy, inertia constants against 1,000 MW and 10 MW references, droop displacement, synchronizing-authority ratios and constant-power acceleration scales;
- publish the results in `REFERENCE_PLANT_SCALE_EVIDENCE.md` and provisionally favor a reduced-scale educational unit while prohibiting any isolated nameplate change;
- close the current-v2 sustained-generation seed by returning fuel/structure heat conservatively to coolant, matching current-v2 primary hydraulic resistance and aligning current-v2 steam-line initial conditions/control-valve bias;
- preserve historical v1 seeds and all protection thresholds;
- keep the scale evidence observational: no generator-nameplate, inertia, droop or coupling migration is performed in A.3.

### Gate

The explicit `ReferencePlantScaleAudit` must compile and reproduce the documented values. The corrected current-v2 sustained-generation seed must keep the exact 300-second and explicit synchronization journeys green. A final scale decision still requires turbine capability evidence, controlled rotor-response evidence and a coordinated versioned migration plan.

## Phase B — Drum and Source Inventory Closure — IN PROGRESS

### Scope

- replace the current demand-following drum-to-steam supplement with a current-v2 energy/pressure/state/inventory-driven source closure;
- constrain recirculation by physically available liquid inventory;
- define behavior when the drum loses separable liquid;
- publish low-inventory and pressure-outside-design-envelope diagnostics;
- add low-drum-level protection/interlock only after the physical inventory semantics are correct.

### Delivery split

**B.1 — Steam-drum liquid-inventory closure — USER VALIDATED LOCALLY**

- current-v2 `CirculationDemandBalanced` recirculation is limited by physically separable liquid inventory plus same-step incoming liquid;
- a fully vaporized drum cannot fabricate a liquid recirculation source;
- requested/maximum/actual recirculation and separable liquid inventory are explicit diagnostics;
- legacy `LegacyReturnSplit` behavior remains unchanged;
- HMI also exposes committed SPEED and LOAD references, and scoring regression locks the rule that automatic protection trips do not incur manual-command penalties.

**B.2 — Drum-to-main-steam source closure — USER VALIDATED LOCALLY**

- remove the remaining demand-following source from `MainSteamNetworkSolver`;
- current-v2 explicitly enables a drum-owned source whose availability is derived from positive return-flow energy surplus plus committed separable vapor inventory;
- independently cap actual source flow by forward drum-to-steam-outlet pressure head through an explicit source hydraulic resistance;
- keep all source terms internally conservative and integrated exactly once;
- expose non-serialized diagnostics for pressure capacity, energy/inventory availability and active limiting side;
- preserve null-source historical/v1 behavior explicitly.

### Required regressions

- no sustained steam generation without available energy and inventory;
- increasing source heat increases available steam generation monotonically within the supported envelope;
- steam export and recirculation debit the correct mass/energy owners exactly once;
- no liquid recirculation is fabricated from a fully vapor state;
- current and legacy profiles remain explicit.

## Phase C — Condenser Phase-Change Closure

### C.1 locally validated — pressure-resolved condensate energy

C.1 and B.3 are locally user-validated. C.1 does not change the existing A.2 condenser capacity values: the current-v2 phase-change control volume assigns condensed mass saturated-liquid specific internal energy at committed condenser pressure, while legacy definitions retain the historical receiving-hotwell energy rule. C.2 is the active candidate and formalizes the retained current-v2 ceilings without retuning them: 40 MW becomes definition-owned installed cooling capacity, runtime available cooling remains a separate operating/fault input, `UA·ΔT` remains the independent surface-transfer limit, and 20 kg/s remains the independent maximum condensation-flow ceiling. Maximum-flow, inventory, thermal and cooling-capacity constraints remain separately observable with margins.

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

## Phase D — Turbine Admission and Governor Authority — IN PROGRESS

### D.1 — Admission phase-policy closure — CANDIDATE / LOCAL VALIDATION PENDING

- add an explicit versioned stage admission policy;
- current-v2 admits only the committed vapor mass fraction, so liquid cannot become a zero-work mass bypass;
- wet-steam mass-flow scaling and thermodynamic-work scaling share one policy and do not apply vapor quality twice;
- legacy definitions preserve unrestricted historical transfer semantics.

### D.2 — Valve/stage authority evidence — CANDIDATE / AUDIT-ONLY

- freeze the canonical current-v2 resistance budget and linear control-valve characteristic without changing production physics;
- quantify the analytical authority map from 10–100% valve opening, including the current 28% seed point;
- collect a deterministic +10 rpm / -10 rpm operational perturbation with control-valve position, admission pressure, raw/effective stage flow and shaft power;
- treat the static resistance map as an indicator only; dynamic plant evidence decides whether correction is needed;
- defer resistance rescaling, effective area or a Stodola/ellipse-style law to a follow-up correction gate only if the evidence demonstrates inadequate authority.

### D.3 — Governor/actuator tracking — CONDITIONAL

- measure controller command versus physical valve position during finite travel;
- add tracking anti-windup only if command/position divergence produces material persistent integral windup;
- review torque-reference continuity separately.

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


### C.2 — Explicit condenser installed-capacity ownership

C.1 is locally validated. C.2 retains the green 40 MW / 20 kg/s current-v2 values but removes their semantic ambiguity:

- 40 MW becomes an optional plant-definition-owned installed heat-rejection ceiling;
- runtime `AvailableHeatRejectionPower` remains the operating/fault-dependent capacity available now;
- `UA·ΔT` remains the surface-transfer ceiling;
- effective heat rejection uses the minimum of installed, available and surface-transfer capacity;
- 20 kg/s remains the independent maximum condensation-throughput ceiling.

Acceptance requires focused installed-vs-available-vs-UA regressions, unchanged legacy/null-definition behavior, replay-v1 presentation compatibility, ordinary suite, explicit 60-second journeys and the healthy 300-second operational-envelope audit. No capacity retuning is part of C.2.

## C.2 Hotfix 1 observed primary hydraulic chatter

User observation on the locally validated C.2 baseline: operator-facing instantaneous primary flows can alternate strongly at the 10 ms step scale (liquid recirculation roughly 0–20 kg/s, MCP roughly -10–120 kg/s, drum inlet roughly 0–200 kg/s, channel groups roughly -20–120 kg/s, return collector roughly 0–200 kg/s). Long-horizon conservation gates remain green, so this is not presently classified as inventory divergence; it is evidence of explicit/algebraic hydraulic stiffness that must be quantified before any solver-level correction.

C.2 Hotfix 1 therefore adds only deterministic 0.5 s operator-facing flow instrumentation. It does **not** claim to resolve the raw numerical chatter. The later numerical decision gate must measure sign-change frequency, peak-to-peak amplitude, timestep sensitivity and convergence under reduced step/substep trials before choosing among substepping, semi-implicit pressure-flow treatment or explicit hydraulic-inertia state.
