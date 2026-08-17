# M10.9.4.1 — Operational Envelope & Numerical Hardening Plan

## Status

**IN PROGRESS — PHASE G COMPLETE; M10.9.4.1-H.13 HOTFIX 2 VALIDATED; H.14 BROADER THERMODYNAMIC BRANCH-CONTINUITY SHADOW QUALIFICATION CANDIDATE**

**Validated prerequisite and continuation:** M10.9.4 plus cumulative M10.9.4.1-H.16; Phase G complete, fixed-step stiffness evidence, method decision, isolated prototype, bounded-work gate, production rollback/extended-shadow evidence, bounded Picard rescue, true-residual/backtracking, safeguarded Anderson, Jacobian-informed damped-Newton, switching diagnosis, thermodynamic-boundary localization, inverse-branch diagnosis, targeted branch continuity, broader 2,000-interval qualification and three-node requalification validated. H.17 is the active long-horizon/cross-profile bounded-hysteresis shadow qualification candidate.

The original extended audit exposed a repeatable long-horizon trip, but follow-up investigation identified and corrected the current-v2 seed energy/hydraulic mismatch. Phases B and C then closed drum/source and condenser ownership, while D.1–D.3.2 closed turbine admission, governor evidence and passive rotor loss. D.4 added typed operator valve authority. On 2026-07-25 the cumulative D.4 source passed 944 ordinary tests and all 17 unique explicit tests. D.4.1, E.2 Hotfix 1, E.3.1 Hotfix 1 and E.3.2 Hotfix 3 were subsequently user-validated on 2026-07-26. F.1–F.3 Hotfix 1 then validated steam-flow capacity, atmospheric relief and internal turbine bypass. G.1 validated the open-control-volume target convention; G.2 Hotfix 2 validated passive current-v2 enthalpy advection and exact pump-work ownership; G.3 validated the remaining non-turbine owners with zero measured ownership residual. G.4 then validated turbine expansion and shaft-work ownership, closing Phase G. H.1 validated fixed-step stiffness evidence; H.2 validated deterministic semi-implicit pressure/flow coupling as the method direction. H.3 Hotfix 1 then validated the isolated frozen-forcing prototype with material chatter reduction, exact conservation/determinism and approximately 15.895x full-time cost. H.4 subsequently validated deterministic selective correction, selecting P060-F040-R015 with only 2/50 corrections, deterministic work ratio 2.14 and `activation-criteria-met=True`. H.5 Hotfix 1 attempted direct free-running activation and failed ordinary validation; H.5 Hotfix 2 restored explicit production and performed extended shadow qualification. User validation recorded 7/500 triggered corrections, 5/7 convergent, deterministic work ratio 1.492000 and `extended-shadow-qualification-passes=False`. H.6 then kept P060/F040 frozen and tested six bounded Picard rescue profiles. User validation selected `R0125-I096`, but it converged only 6/7; the two-tier ladder also remained 6/7 with deterministic work ratio 1.700000 and `refined-envelope-qualification-passes=False`. H.7 then validated a true fixed-point residual plus deterministic backtracking but remained 5/7. H.8 validated safeguarded Anderson and also remained 5/7 with two line-search exhaustions, work ratio 1.212000 and `accelerated-corrector-qualification-passes=False`. H.9 therefore owns the next algorithmic step: a conservative-coordinate finite-difference Jacobian plus damped Newton direction, still shadow-only.

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

- freeze the then-current 1,000 MW generator nameplate, 1,000 kg·m² rotor, 5 MW request, 150 rpm full-load droop and 10 MW coupling values as historical pre-migration evidence rather than implicit constants;
- derive stored rotor energy, inertia constants against 1,000 MW and 10 MW references, droop displacement, synchronizing-authority ratios and constant-power acceleration scales;
- publish the results in `REFERENCE_PLANT_SCALE_EVIDENCE.md` and provisionally favor a reduced-scale educational unit while prohibiting any isolated nameplate change;
- close the current-v2 sustained-generation seed by returning fuel/structure heat conservatively to coolant, matching current-v2 primary hydraulic resistance and aligning current-v2 steam-line initial conditions/control-valve bias;
- preserve historical v1 seeds and all protection thresholds;
- keep the scale evidence observational: no generator-nameplate, inertia, droop or coupling migration is performed in A.3.

### Gate

The historical reference-scale audit froze the pre-migration values. E.1 closed the scale-direction decision; E.2 Hotfix 1 validly replaces that active current-v2 contract with the coordinated 10 MWe/bidirectional model while retaining the old figures as compatibility evidence. The corrected sustained-generation parent source passed the exact long-running and operational-envelope gates.

## Phase B — Drum and Source Inventory Closure — LOCALLY GREEN

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

## Phase C — Condenser Phase-Change Closure — LOCALLY GREEN

### C.1 locally validated — pressure-resolved condensate energy

C.1 and B.3 are locally user-validated. C.1 does not change the existing A.2 condenser capacity values: the current-v2 phase-change control volume assigns condensed mass saturated-liquid specific internal energy at committed condenser pressure, while legacy definitions retain the historical receiving-hotwell energy rule. C.2 is now locally green and formalizes the retained current-v2 ceilings without retuning them: 40 MW becomes definition-owned installed cooling capacity, runtime available cooling remains a separate operating/fault input, `UA·ΔT` remains the independent surface-transfer limit, and 20 kg/s remains the independent maximum condensation-flow ceiling. Maximum-flow, inventory, thermal and cooling-capacity constraints remain separately observable with margins.

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

### D.1 — Admission phase-policy closure — USER VALIDATED LOCALLY

- add an explicit versioned stage admission policy;
- current-v2 admits only the committed vapor mass fraction, so liquid cannot become a zero-work mass bypass;
- wet-steam mass-flow scaling and thermodynamic-work scaling share one policy and do not apply vapor quality twice;
- legacy definitions preserve unrestricted historical transfer semantics.

### D.2 — Valve/stage authority evidence — LOCALLY GREEN / AUDIT-ONLY

- freeze the canonical current-v2 resistance budget and linear control-valve characteristic without changing production physics;
- quantify the analytical authority map from 10–100% valve opening, including the shared 28% sustained seed point and the rejected 30% comparison point;
- collect a deterministic +10 rpm / -10 rpm operational perturbation with control-valve position, admission pressure, raw/effective stage flow and shaft power;
- treat the static resistance map as an indicator only; dynamic plant evidence decides whether correction is needed;
- defer resistance rescaling, effective area or a Stodola/ellipse-style law to a follow-up correction gate only if the evidence demonstrates inadequate authority.

### D.3 — Governor/actuator tracking and admission closure — VALIDATED

- measure controller command versus physical valve position during finite travel;
- add tracking anti-windup only if command/position divergence produces material persistent integral windup;
- review torque-reference continuity separately.

Validated cumulative implementation includes:

- D.3.1 optional 0.5 MW rated-speed passive rotor loss for sustained current-v2 profiles;
- D.3.2 complete stop/control/admission authority over pressure-driven stage flow;
- D.3.2 Hotfix 3 loaded desktop main-steam resistance of 850 Pa·s²/kg² while synchronization remains at 1,000;
- no tracking anti-windup, because the focused tracking evidence remains green without it.

### D.4 — Operator-facing valve authority — VALIDATED

- typed stop/admission valve open/close commands cross the Application boundary;
- the control valve exposes explicit MANUAL/AUTO ownership and a bounded 0–100% manual-demand slider with an explicit APPLY action;
- requested, manual-demand and actual positions are published separately;
- finite actuator travel remains authoritative;
- protection opening inhibits and forced stop-valve closure remain later arbitration and are visible without erasing the operator request;
- the automated D.4 gate is green;
- D.4.1 validates STOP-owned travel plus replay/checkpoint and trip-reset-resume hardening; all user-run ordinary and long-running gates passed on 2026-07-26.

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

## Phase E — Generator/Grid Scale, Signed Coupling and Protection Evidence — COMPLETE / VALIDATED THROUGH E.3.2 HOTFIX 3

### E.1 — Scale target — ACCEPTED DECISION ONLY

The current-v2 educational target is 10 MWe with a 5 MWe normal point, 3,000 rpm rated speed and 1,000 kg·m² inertia. Legacy/default profiles remain on historical definitions. E.2 implements this target only for the two current-v2 sustained profiles.

### E.2 — Coordinated runtime migration — VALIDATED

E.2 validates, as one coordinated runtime contract:

- 10 MWe current-v2 generator nameplate;
- explicitly selected governor normalization, provisionally 1.5 rpm full-load rise to preserve the current 0.75 rpm displacement at 5 MWe;
- `GenerationOnly` compatibility default plus current-v2 `Bidirectional` opt-in;
- signed generation/motoring shaft exchange and electrical output;
- positive conversion loss in both directions;
- internal signed rotor-torque seam owned only by generator/grid integration;
- signed HMI electrical ranges and requested-load clamp 0..10 MWe;
- focused generation, motoring, compatibility, command-clamp and HMI regressions.

The validated runtime uses 10 MWe, 1.5 rpm, retained 0.5 MW synchronizing correction, retained 2 MW/Hz damping and opt-in bidirectional behavior. The reference-scale audit is expanded from 2 to 4 tests and retains explicit legacy 1,000 MWe/generation-only compatibility coverage.

### E.3.1 Hotfix 1 — Signed electrical protection trajectory audit — VALIDATED

The user confirmed compilation and all ordinary/cumulative gates passed and supplied the complete generated bundle. Normal, reverse-power, breaker-open coastdown and phase-offset evidence has been reviewed.

### E.3.2 Hotfix 3 — Protection over signed electrical states — VALIDATED

Canonical M5.5 supports optional measured supervision and committed pickup timing. Both current-v2 sustained profiles opt into validated evidence-derived reverse-power, underfrequency and absolute-slip loss-of-synchronism generator trips. Legacy definitions remain immediate and unsupervised by default. The reviewed implementation bundle confirms the normal 5→0→5 MWe transient does not trip, reverse-power trip occurs after exactly 2.0 s and breaker-open coastdown remains ineligible.

### Required E.2 regressions

- both positive generation and negative motoring are physically representable;
- disconnected/coast-down conditions do not create false electrical trips;
- breaker-closed synchronization has restoring behavior in both slip directions;
- inertia and droop produce the documented scale response;
- replay remains deterministic across signed-power and protection events.

## Detailed execution plan from the current checkpoint

### Gate 0 — Documentation and baseline identity — COMPLETE

1. Treat M10.9.4.1-H.19 as the current validated continuation baseline and use M10.9.4.1-H.20 as the working fail-closed activation/rollback/shadow-telemetry contract candidate; current-v2 production remains explicit and the H.20 supervisor is not wired into `PlantNetworkOrchestrator`.
2. Identify the active source as cumulative D.3.2 Hotfix 3 plus D.4/D.4.1 valve authority hardening, validated E.2 scale/coupling migration, validated E.3 electrical protection, validated F.1–F.3 steam-capacity/relief/bypass work and validated G.1–G.3 energy migration.
3. Record G.4 as the final current-v2 turbine-expansion enthalpy/shaft-work migration before Phase H.
4. Keep legacy/current-v1 defaults and current-v2 opt-in behavior explicit in every test and document.

**Exit:** complete. README, status, handoff, milestone, scale contract/evidence and limitations register describe the same source.

### Gate 1 — Complete automated regression — GREEN 2026-07-25

1. Ordinary suite: 944 passed / 17 explicit skipped / 0 failed.
2. Admission-authority audit: 3/3 passed.
3. Governor-tracking audit: 2/2 passed.
4. Gameplay long-running journeys: 2/2 passed.
5. Operational-envelope audit: 9/9 passed.
6. Reference-scale audit: 2/2 passed.
7. All 17 unique explicit tests passed.

**Exit:** complete. Any later production edit reopens this gate.

### Gate 2 — D.4.1 operator-valve hardening — VALIDATED

1. Replay/checkpoint regressions now cover STOP, ADMISSION, AUTO/MANUAL and manual demand.
2. The checkpoint is captured while requested and actual positions differ during finite travel.
3. The trip → request preserved → canonical reset → travel resumes path is now locked by regression.
4. The stop valve owns an optional travel-rate contract and no longer borrows control-valve configuration; the new public factory parameter is appended for positional source compatibility.
5. Current-v2 sustained profiles explicitly preserve the validated 0.5/s STOP rate, while `null` preserves instantaneous legacy behavior even when other secondary valves are rate-limited.
6. The user confirmed the required ordinary and long-running automated gates; D.4.1 is promoted to the validated baseline.

**Exit:** complete required automated gate and explicit user promotion.

### Gate 3 — E.2 coordinated implementation — COMPLETE

1. Implement the versioned 10 MWe scale and governor normalization.
2. Implement bidirectional coupling and the internal signed rotor-torque seam.
3. Implement signed HMI semantics and compatibility-preserving replay/checkpoint behavior.
4. Add focused generation/motoring and range regressions.

**Exit:** complete.

### Gate 4 — E.2 cumulative promotion — PASSED

1. Re-run the ordinary suite.
2. Re-run admission and governor audits.
3. Re-run both 60-second gameplay journeys.
4. Re-run the complete operational-envelope and reference-scale audits.
5. Re-run replay/checkpoint cases over load step, breaker open, generator trip, turbine trip and signed-power transitions.
6. Manually verify signed GENERATOR presentation and import/export semantics.

**Exit:** user confirmed compilation and all requested ordinary, focused and long-running gates passed on 2026-07-26.

### Gate 5 — E.3.1 evidence collection — PASSED

1. Four explicit signed trajectory audits passed.
2. The complete summaries and CSV files were supplied and reviewed.
3. Pickup/reset/delay/supervision values were derived from the observed separation margins.

**Exit:** user-confirmed all-green E.3.1 Hotfix 1 with reviewed artifact bundle.

### Gate 6 — E.3.2 protection implementation — COMPLETE / VALIDATED

1. Validate generic supervision and committed pickup timing.
2. Validate both current-v2 sustained factories and legacy defaults.
3. Validate normal no-trip, reverse-power trip and disconnected coastdown supervision.
4. Validate in-flight pickup replay/checkpoint restoration.
5. Reproduce E.3.1 reports and rerun all cumulative gates.
6. Manually review HMI markers and protection reset.
7. E.3.2 is promoted. Continue to Phase F, then G, H and I.

## Phase F — Relief and Bypass with Choked Flow

### F.1 — Isolated choked steam-flow capacity law — VALIDATED

F.1 validates the typed ideal-vapor one-way pressure-ratio capacity equation, critical-ratio transition, choked plateau, effective-area scaling and deterministic sizing evidence. The user-confirmed audit reports critical ratio `0.545728` and `0.788008677 kg/s` at `100 mm²`.

### F.2 — Conservative main-steam header relief — VALIDATED

F.2 is the first topology consumer of F.1. It adds one optional current-v2 atmospheric external relief boundary, closed through `6.5 MPa`, fully open at `6.7 MPa`, with a `1,600 mm²` throat and exact source/external mass/internal-energy accounting. The user-supplied audit confirmed 13.531762568 kg/s at 6.80 MPa, monotonicity and conservative external exchange.

### F.3 — Conservative turbine bypass to condenser — VALIDATED

F.3 adds a separate internal current-v2 steam-dump path owned by the condenser system:

- `header` to condenser `condenser`, steam space `exhaust`;
- closed through `6.4 MPa`, full opening at `6.5 MPa`;
- `1,600 mm²` validated F.1 capacity definition;
- capacity resolved against committed condenser backpressure;
- vapor-availability limiting and one-way reverse-flow blocking;
- equal/opposite internal mass and committed-specific-internal-energy terms;
- external mass and power exactly zero;
- F.2 atmospheric relief unchanged and independent.

F.3 deliberately excludes manual authority, actuator dynamics, hysteresis, wet-steam correlations and Phase G enthalpy migration. Its explicit committed-state condenser sequencing becomes an input to the Phase H stiffness audit.

### F.3 required regressions

- current-v2 opt-in and legacy empty default;
- exact source/destination topology and snapshot set;
- pressure-opening law and capacity above the current steam-path scale;
- committed backpressure, choked plateau, subcritical decline and zero flow at equal pressure;
- vapor-quality limiting;
- exact internal mass/energy conservation and zero external exchange;
- unchanged F.2 relief and all cumulative long-running gates.

## Phase G — Flow Work and Enthalpy Transport

### G.1 — Open-control-volume energy convention and gap audit — VALIDATED

G.1 established and validated the target relation `h = u + p/rho` without changing runtime source terms. The user-supplied audit measured up to 192.048450950 kJ/kg and 2.484103126 MW of missing steam-path flow work, with exact identity and internal-transfer closure.

### G.2 — Passive hydraulic enthalpy migration — VALIDATED VIA HOTFIX 2

G.2 migrated current-v2 passive pipes and valve paths to `h*m_dot`, preserved historical defaults and audited pump hydraulic/shaft work. Hotfix 2 aligned the desktop stability contract with signed bidirectional grid exchange without retuning runtime physics. The user-supplied audit confirmed six passive enthalpy-mode components, exact endpoint closure, three pump paths with exact ownership, 2.762103670 MW maximum component flow work and 11.563679870 MW total absolute passive flow work.

### G.3 — Remaining non-turbine enthalpy migration — VALIDATED

G.3 validated the remaining current-v2 non-turbine transport owners in `SpecificEnthalpy`:

- pump paths, with hydraulic work and shaft demand retained as separate single owners;
- steam-drum separation and liquid recirculation;
- feedwater, steam-export and turbine-admission boundaries;
- condenser steam removal and condensate addition, with heat rejection declared once;
- atmospheric relief as an external boundary;
- turbine bypass as an internal equal-and-opposite transfer.

Fluid-node inventories remain mass plus internal energy. Legacy/current-v1 definitions remain on `SpecificInternalEnergy`. Every migrated snapshot publishes `u`, `p/rho`, `h` and the applied selected rate. The user-supplied G.3 audit confirmed 2.817289248 MW maximum flow work, 8.972520763 MW total absolute flow work and zero maximum ownership residual. Turbine expansion remained unchanged for G.4.

Required G.3 evidence:

- definition default/opt-in compatibility;
- exact internal transfer closure;
- explicit feedwater enthalpy for positive external inflow;
- exact pump hydraulic and shaft-work ownership;
- condenser heat-rejection single counting;
- relief external and bypass internal ownership;
- unchanged turbine-expansion mode;
- complete ordinary and cumulative long-running gates.

### G.4 — Turbine expansion and shaft-work ownership — VALIDATED

G.4 is validated and closes Phase G. Current-v2 stage groups apply `h*m_dot` at inlet and `h*m_dot - P_shaft` at exhaust, while shaft work remains one explicit thermofluid-to-rotor transfer. The supplied audit reports 2.506379668 MW maximum flow work, 5.457103652 MW maximum shaft power and zero ownership residual. Legacy stages retain `SpecificInternalEnergy`; turbine work, efficiency, governor, generator/grid coupling and protections were not retuned.

## Phase H — Numerical Stiffness Decision Gate

### H.1 — Fixed-step timestep sensitivity & stiffness evidence — VALIDATED

The 10/5/2.5 ms audit showed approximately doubled cost per halving without monotonic final-state convergence improvement. Raw primary hydraulic chatter and subcooled-liquid pressure excursions remained material while conservation stayed green.

### H.2 — Numerical-method decision — VALIDATED

H.2 selected deterministic semi-implicit pressure/flow coupling and rejected simple bounded explicit substeps as the preferred cure. Production remained explicit at 10 ms.

### H.3 — Isolated semi-implicit hydraulic prototype & audit — HOTFIX 1 VALIDATED

The frozen-forcing prototype converged on all 50 intervals with 16.760 average / 40 maximum iterations. Chatter ratios prototype/explicit were pump 0.432808, channel 0.135868, return 0.031681 and pressure 0.922127. Inventory/conservation/ownership residuals remained zero and deterministic repeat was exact. Full-time cost was approximately 15.894951x, so production activation was correctly deferred.

### H.4 — Deterministic hybrid semi-implicit activation & cost gate — VALIDATED

The validated sweep selected `P060-F040-R015`: pressure trigger 0.060, flow trigger 40 kg/s, relaxation 0.15 and 72 maximum corrector iterations. It corrected 2/50 intervals, converged 2/2, achieved deterministic work ratio 2.14, observational wall-cost ratio 1.662880, retained strong chatter reduction, zero conservation/ownership residuals and exact deterministic repeat. `activation-criteria-met=True`; H.4 itself correctly kept production explicit.

### H.5 — Current-v2 hybrid hydraulic production integration — CANDIDATE

Integrate the exact H.4-selected profile into the canonical `PlantNetworkOrchestrator` only for versioned sustained current-v2 definitions. Legacy/current-v1 and numerical-evidence profiles remain explicitly historical. Freeze non-hydraulic forcing over a triggered correction, rebuild provisional states from the original committed state, integrate conserved inventories once, expose immutable numerical diagnostics and fail explicitly on non-convergence. Keep the external logical timestep at 10 ms and prohibit wall-clock adaptation, filtering and physical retuning.

H.5 closes Phase H only after focused production evidence, ordinary tests, replay/checkpoint determinism, protection/electrical gates, long-running gameplay, operational-envelope and reference-scale audits are all green.

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


### F.3 — Conservative turbine bypass to condenser — VALIDATED

F.3 adds a distinct current-v2 internal steam-dump path owned by the condenser system. It opens from 6.4 to 6.5 MPa, uses a 1,600 mm² F.1 capacity definition, resolves against committed condenser backpressure and transfers mass/internal energy from `header` to `exhaust` with zero external exchange. F.2 atmospheric relief remains independent. The explicit committed-state sequencing is documented and will be included in the Phase H stiffness audit.


### H.5 Hotfix 2 — production activation rollback and extended shadow qualification — VALIDATED

Ordinary H.5 Hotfix 1 validation exposed deterministic non-convergence in free-running current-v2. Hotfix 2 restores explicit 10 ms production and evaluates P060-F040-R015 only as interval-local shadow evidence over 5 s. User validation passed build/tests and recorded 7 triggered corrections with 5/7 convergence, deterministic work ratio 1.492000 and `extended-shadow-qualification-passes=False`. Phase H remains open.


### H.6 — shadow corrector rescue envelope and two-tier qualification — VALIDATED

H.6 froze P060/F040 and the seven H.5 trigger intervals, then swept six bounded Picard profiles. User validation selected `R0125-I096`, but the selected profile converged only 6/7. Maximum selected-profile residuals were 0.291876228 relative pressure and 61.700761261 kg/s flow; maximum explicit-end mass/energy/pressure gaps were 0.000175566/0.000194823/0.291958117. The two-tier H.4-primary/rescue ladder also converged 6/7, deterministic work ratio was 1.700000, exact repeat held and `refined-envelope-qualification-passes=False`. Production stayed explicit and no shadow candidate was committed.


### H.7 Hotfix 1 — residual fixed-point corrector with deterministic backtracking — VALIDATED

The user validated build, ordinary tests and the focused gate. H.7 reproduced H.4 5/7 and H.6 6/7, then converged 5/7 with two line-search exhaustions. Maximum fixed-point pressure/flow residuals were 0.303946536 / 28.424177648 kg/s, deterministic work ratio was 1.308000, accepted merit strictly decreased and exact repeat/conservation passed. `corrector-algorithm-revision-qualification-passes=False`; production remained explicit.

### H.8 — safeguarded Anderson accelerated nonlinear corrector — VALIDATED

The user validated compilation, ordinary tests and the focused audit. H.8 kept the exact H.5-H.7 500-step explicit trajectory and P060/F040 trigger set frozen, reproduced H.4 5/7, H.6 6/7 and H.7 5/7, then safeguarded Anderson also converged 5/7 with two line-search exhaustions. Anderson attempts/acceptances were 30/24, residual fallback 13/11, six least-squares systems were rejected, maximum coefficient L1 norm was 7.188310311, deterministic work ratio was 1.212000, accepted merit strictly decreased and exact repeat/conservation passed. `accelerated-corrector-qualification-passes=False`. Production remained explicit.

### H.9 — conservative-coordinate finite-difference Jacobian + damped Newton — VALIDATED

User validation passed build, ordinary tests and the focused audit. H.9 remained 5/7 with two line-search exhaustions despite 23/21 Jacobian builds/accepted directions, zero Jacobian conditioning rejections, maximum pivot-condition estimate 1.613327388, bounded normalized Newton step 1.276749799, strict merit decrease, exact repeat and deterministic work ratio 2.702000. `jacobian-informed-corrector-qualification-passes=False`; production remained explicit.

### H.10 Hotfix 1 — hydraulic-map switching and non-smoothness diagnosis — VALIDATED

Keep the exact frozen evidence set and diagnose only the two persistent H.9 failures. Use two-scale law-local pressure probes to identify path branch changes, check-valve blocking, square-root derivative scale growth and one-sided slope asymmetry; use conserved mass/internal-energy probes through the existing thermodynamic closure to identify phase/envelope transitions. Compare each stalled H.9 candidate with the committed explicit endpoint. Do not add another nonlinear corrector until this evidence is understood.

### H.11 — thermodynamic switching localization and active-set diagnosis — VALIDATED

Validated H.10 found no hydraulic branch switching/non-smooth paths around the two persistent H.9 failures, but found exactly two thermodynamic phase/envelope switching nodes around the H.9 candidate states and zero at the matching explicit endpoints. H.11 therefore introduces no new corrector. It localizes only the H.10-flagged nodes with conserved energy/mass probes, phase/envelope classification, saturation-relative properties and node-local mapped-minus-applied hydraulic balance residuals. The suggested active-set label is diagnostic evidence only. Production remains explicit at 10 ms.


### H.12 — thermodynamic inverse branch selection audit — VALIDATED

Validated H.11 localized the two persistent phase boundaries to `steam` at interval 200 and `stop-out` at interval 360, both on energy+mass perturbations. Validated H.12 confirmed overlapping saturated/superheated roots at both nodes, coarse saturated detection toggles, five late boundary-aware saturated roots shadowed by earlier coarse-superheated selection, and zero previous-state tie-breaks. Production `Resolve()` ordering remains unchanged and current-v2 remains explicit at 10 ms.

### H.13 Hotfix 2 — thermodynamic branch continuity / hysteresis shadow experiment — VALIDATED

User validation passed build, ordinary tests and the focused H.13 audit. Production H.9 remained 5/7 with two line-search exhaustions; targeted previous-phase continuity and bounded previous-phase hysteresis both reached 7/7 with zero exhaustions, exact repeat, deterministic work ratio 1.886000 and preserved conservation/ownership. The bounded policy used 2% relative pressure / 5 K release limits but exercised zero releases on the frozen seven-event set.

### H.14 — broader thermodynamic branch-continuity shadow qualification — CANDIDATE

H.14 keeps the selected H.13 bounded policy and all production behavior unchanged. It extends the committed shadow horizon to 2,000 intervals, preserves the first 500 as the exact H.13 control window, evaluates every extended P060/F040 event under unchanged H.9, observes `steam`/`stop-out` branch decisions throughout the horizon and adds four deterministic hold/release challenges covering both phase directions. A positive broader qualification requires all extended triggers to satisfy unchanged H.9 gates plus successful deterministic hold and release behavior. H.14 never activates the policy.
