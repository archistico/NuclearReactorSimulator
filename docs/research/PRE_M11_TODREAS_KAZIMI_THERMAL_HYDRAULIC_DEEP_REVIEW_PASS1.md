# Pre-M11 Todreas/Kazimi Thermal-Hydraulic Deep Review — Pass 1 (Pre-Plan-Amendment-2)

## Status and purpose

**RESEARCH / PLANNING REVIEW — documentation-only. Not M10 promotion evidence, not P2R branch selection and not Plan Amendment 2.**

This is the first detailed-section review of the two-volume *Nuclear Systems* reference set by Todreas, Kazimi and, for Volume II, Massoud. It is deliberately performed **after P1A evidence returned but before Plan Amendment 2 is frozen** so that the literature can inform what a next diagnostic should observe without retroactively changing P1A or prematurely defining a new experiment.

A **second Todreas/Kazimi deep review is mandatory after Plan Amendment 2 is authored**. That second pass will not be a generic re-reading: it will audit the exact frozen observables, interpretations, exclusions and decision criteria in the amendment against these source sections and will either confirm them or require the amendment to be revised before execution.

This pass changes no production source, no pre-existing test, no exact-v9 runtime semantics, no replacement workload, no protection/authority semantics, no mission pack and no M10 acceptance threshold. It authorizes neither P3-W nor P3-R and does not authorize a second replacement-long baseline.

The transferability vocabulary remains:

- `DIRECT_CONCEPT` — the engineering principle transfers at the level stated;
- `ANALOGICAL` — the source plant/geometry differs and only the stated systems principle transfers;
- `FUTURE_ONLY` — valuable for a later physical-fidelity milestone but not current M10 execution;
- `NON_TRANSFERABLE_NUMERIC` — numerical values, correlations, geometry, thresholds and tuned coefficients are not imported.

## 1. Current project evidence that motivates this pass

This section is **project evidence, not book-derived evidence**.

P1A completed with execution PASS. The exact-v9 5.5 MWe probe ultimately classified `CONVERGED`, while the 6 MWe probe remained `INCONCLUSIVE` at the frozen 3,600 s hard horizon. The 6 MWe late tail nevertheless demonstrated essentially exact electrical reachability: mean output was about 5.999773 MWe for a 6 MWe request, frequency was about 50.000007 Hz, dispatch error was about -0.000232 MW and no trip occurred. What failed to close were the pre-frozen stationarity-slope ceilings, especially turbine-inlet pressure, with output/shaft/flow also just above their limits.

The distinction to preserve is therefore:

```text
LOAD REACHABILITY AT 6 MWe
    demonstrated by P1A

WHOLE-OPERATING-POINT STATIONARITY AT 6 MWe
    not yet demonstrated
```

The current planning question is no longer “can exact-v9 make 6 MWe?” but “which canonical state owner is still evolving after electrical load and reactor thermal source are nearly settled?” The two volumes are reviewed below specifically for principles that help answer that question without inventing new physics.

---

# Part A — Volume I: *Thermal Hydraulic Fundamentals*

## 2. Chapter 5 — what HEM actually means, and what P1B-style evidence cannot claim

### 2.1 Source-derived finding

Volume I §5.1.3 places HEM at the simplest end of a hierarchy of two-phase models. HEM assumes both:

1. no relative liquid/vapor velocity (mechanical equilibrium, slip ratio one); and
2. liquid/vapor thermodynamic equilibrium.

Under those assumptions the mixture mass, momentum and energy balances can describe the flow. Extensions that allow slip or thermal nonequilibrium require additional constitutive relations and, in richer formulations, more transport equations.

**Transferability:** `DIRECT_CONCEPT`.

### 2.2 Project consequence

The current simulator's homogeneous-equilibrium saturated-mixture mapping is correctly described as **reduced HEM-like physics**. This has a direct diagnostic consequence: a future slow-state diagnostic may observe the state the current model owns, but it must not infer missing degrees of freedom.

For example, the following are legitimate current-model observations when already exposed by canonical state/snapshots:

```text
mass quality
void fraction produced by the current homogeneous mapping
mixture pressure / temperature / internal energy
mixture mass flow
inventory mass and energy
```

The following are **not observable physical facts** in the present model and therefore must not be created by a diagnostic layer:

```text
liquid-vapor slip velocity
interfacial momentum exchange
subcooled-boiling vapor lag
independent phasic temperatures
true flow-regime identity
film/droplet inventory
```

This matters because P1A's slow flow/pressure drift must not be relabeled “density-wave oscillation”, “slip relaxation” or “subcooled-boiling delay” merely because those are real phenomena in higher-fidelity systems. The current runtime cannot identify them.

### 2.3 Deep-review rule for Plan Amendment 2

If the amendment introduces any `owner classification` field, it must distinguish:

- **represented owner/state** — supported by canonical current-model evidence;
- **unrepresented physical mechanism** — source-known but unavailable in the current fidelity;
- **inference** — a project diagnostic interpretation, never a new state variable disguised as measurement.

No drift-flux or two-fluid closure is authorized in M10 by this review.

---

## 3. Chapter 11 — pressure loss is a vector of mechanisms, not one scalar story

### 3.1 Source-derived finding

Volume I §11.6 separates two-phase pressure loss into acceleration, friction and gravity contributions and treats singular/form losses separately. The same chapter emphasizes that void/pressure-loss correlations are model- and domain-dependent; §11.3 explicitly distinguishes HEM, slip/drift-flux and two-fluid models and notes that their appropriateness changes with pressure, flow and transient character.

**Transferability:** `DIRECT_CONCEPT` for decomposition and model-domain discipline; `NON_TRANSFERABLE_NUMERIC` for all correlations and example values.

### 3.2 Project consequence for slow-owner diagnosis

A pressure trend by itself does not tell us whether the owner is:

- changing mass/energy inventory;
- changing mixture density and therefore hydrostatic head;
- changing frictional loss because flow changes;
- changing acceleration loss;
- changing a valve/nozzle/form loss;
- changing an upstream or downstream pressure boundary.

Therefore a future bounded diagnostic should prefer **canonical decomposition already owned by the solver**. If the runtime currently exposes some but not all components, the report should say `UNAVAILABLE` for the missing terms rather than reimplementing textbook correlations in Application/test code.

A desirable evidence shape is:

```text
measured/committed pressure grade
    = upstream pressure - downstream pressure

canonical contributors, where available:
    friction
    gravity / hydrostatic
    acceleration
    valve / form / local loss
    pump available head

unexplained remainder:
    report explicitly, do not tune away
```

This is particularly relevant because P1A's strongest late stationarity miss is turbine-inlet pressure while electrical output is already essentially at target.

### 3.3 Negative rule

The deep review does **not** justify introducing Premoli, Friedel, Lockhart-Martinelli, Thom or any other correlation into exact-v9 solely to make a long gate close. Correlation selection would be a new physical-model milestone requiring its own range/applicability and V&V work.

---

## 4. Chapter 11.8 — root closure is not stability

### 4.1 Source-derived finding: Ledinegg

Volume I §11.8 shows that a heated two-phase channel under an imposed external pressure drop can have more than one flow rate satisfying the pressure-drop intersection. A perturbation criterion then distinguishes stable and unstable intersections. In other words:

```text
hydraulic residual = 0
```

is necessary for a steady solution but does not guarantee that the solution is dynamically stable.

**Transferability:** `DIRECT_CONCEPT`.

### 4.2 Project consequence

This strongly confirms the architecture already being planned for M12.0: **root / stationarity closure** must remain distinct from stability qualification.

```text
EQUILIBRIUM / STATIONARITY
    is not the same claim as
LOCAL DYNAMIC STABILITY
```

For the immediate M10 closure investigation, however, we should keep the questions ordered:

1. identify which represented state remains nonstationary;
2. establish a credible stationary operating point under unchanged semantics;
3. only then use bounded perturbation recovery as a stability qualification where authorized.

Therefore the first post-P1A diagnostic should **not** add arbitrary perturbations merely because the book discusses Ledinegg stability. That would mix owner localization with stability testing.

### 4.3 Branch identity consequence

If a future operating-point trimmer or solver finds a zero residual, the result must retain enough state/continuation provenance to establish that it is the intended physical branch. Optimizer convergence cannot be equated with physical operating-point qualification.

---

## 5. Chapter 11.8 — density-wave oscillations and the physical-vs-numerical distinction

### 5.1 Source-derived finding

The book distinguishes static instabilities from dynamic oscillations. Density-wave behavior reflects transport delay between inlet conditions, heating/phase change, void/density and pressure-drop response. It also states that time-domain analysis contains a special interpretive hazard: numerical instability may coexist with or masquerade as physical instability. Frequency-domain and time-domain approaches therefore answer related but different questions.

The text also shows that predicted stability boundaries depend on the two-phase model: HEM, nonhomogeneous equilibrium and nonequilibrium formulations do not produce identical boundaries.

**Transferability:** `DIRECT_CONCEPT` for the diagnostic distinction; actual BWR stability boundaries are `NON_TRANSFERABLE_NUMERIC`.

### 5.2 Project consequence

If the future diagnostic sees an oscillatory tail, it should record **what the current model actually generated**:

- dominant/observed period if resolvable;
- amplitude or peak-to-peak range;
- whether amplitude decays, persists or grows;
- phase relation among represented pressure, flow, inventory and power variables;
- deterministic-repeat result;
- numerical-coupling/fallback/rollback telemetry already available.

It must not label the oscillation “density-wave instability” unless a later dedicated model/assessment milestone establishes that claim.

The current Phase-H discipline — separating physical branch behavior from numerical convergence and timestep artifacts — is therefore reinforced rather than replaced.

---

## 6. Chapter 13 — boiling onset is not one instantaneous saturation switch

### 6.1 Source-derived finding

Volume I Ch.13 separates onset of nucleate boiling (ONB), net vapor generation (NVG), onset of saturated boiling, later thermal equilibrium and the resulting void profile. It also distinguishes DNB and dryout mechanisms in its critical-condition discussion.

**Transferability:** `DIRECT_CONCEPT` as a fidelity boundary; detailed correlations/thresholds are `NON_TRANSFERABLE_NUMERIC`.

### 6.2 Project consequence

The current simplified water/steam model has no explicit ONB/NVG history owner. Therefore a diagnostic may record current thermodynamic phase/quality/void and branch transitions, but it must not fabricate an ONB or NVG position/time.

Similarly, future M12/M15 thermal-limit work must distinguish whether it is modelling a DNB-like local surface limit, a dryout/history-dependent limit or merely a reduced educational surrogate. The present review does not authorize any such limit in M10.

---

## 7. Chapter 14 — boundary conditions and multiple-flow solutions

### 7.1 Source-derived finding

Volume I Ch.14 shows that for a heated channel the choice of hydraulic boundary conditions matters. Under pressure-pressure boundaries more than one inlet flow rate can satisfy the equations because density changes can make the integrated pressure-drop characteristic nonmonotonic. The chapter also separates forced, natural and mixed convection and emphasizes that buoyancy becomes part of the governing pressure balance when density changes matter.

**Transferability:** `DIRECT_CONCEPT`; LWR geometry/example numbers are `NON_TRANSFERABLE_NUMERIC`.

### 7.2 Project consequence

A future diagnostic must avoid accidentally overconstraining the model by treating both flow and pressure as independent acceptance targets when one is actually an emergent result of the canonical network.

For the P1A successor, the useful question is not:

```text
is flow exactly X AND pressure exactly Y because we chose both?
```

but rather:

```text
under the frozen plant commands/boundaries,
are the canonical pressure and flow owners approaching a mutually compatible stationary root?
```

This reinforces the existing rule that the diagnostic layer observes canonical owners instead of creating a parallel hydraulic solve.

---

# Part B — Volume II: *Elements of Thermal Hydraulic Design*

## 8. Chapter 1 — parallel-channel flow split is part of the solution

### 8.1 Source-derived finding

Volume II Ch.1 formulates multiple heated channels connected only at inlet/outlet plena. When radial pressure gradients in the plena are negligible, all channels share the same pressure drop. The problem then solves channel flow, enthalpy/void and pressure compatibility together; total-flow and specified-pressure formulations are different boundary-value problems.

**Transferability:** `DIRECT_CONCEPT` for an equivalent parallel-channel representation; exact BWR/LMR topology is `ANALOGICAL` to the RBMK-like pressure-channel system.

### 8.2 Project consequence

For the simulator's representative fuel-channel groups, a future higher-fidelity hydraulic closure should not prescribe every group flow independently if the groups share common hydraulic boundaries. The reduced relation to preserve is conceptually:

```text
sum(group flows) = total loop flow
and
shared-boundary-compatible pressure drop across groups
```

For the immediate slow-state diagnosis, no new flow-split solver is authorized. But if current snapshots already expose per-group or per-branch values, the diagnostic can report:

- total-flow closure;
- spread/change of represented group flows;
- common-node pressure consistency already owned by the current network;
- which group/branch contributes most to any represented hydraulic residual.

This helps distinguish “whole loop still moving” from “redistribution among branches while total flow looks calm.”

---

## 9. Chapter 2 — scaling changes how we should talk about time scales

### 9.1 Source-derived finding

Volume II Ch.2 treats two-phase scaling as a problem of deciding which conservation responses, inventories, transfers and dominant phenomena are preserved between model and prototype. The chapter explicitly develops scaling methods around conservation laws, response functions and component mass/energy inventories, and discusses unavoidable distortions.

**Transferability:** `DIRECT_CONCEPT` for claim discipline; specific similarity groups/test-facility results are `NON_TRANSFERABLE_NUMERIC`.

### 9.2 Project consequence

Nuclear Reactor Simulator's 10 MWe reference plant must remain described as a **reduced-order RBMK-like educational plant**, not as a geometrically/dynamically scaled RBMK prototype.

That also means we must not interpret a 3,600 s simulator settling time as “the real RBMK would take 3,600 s.” It is a time scale of this model under this authored operating point and controller/physics configuration.

For P1B-style diagnosis, the correct use of time is therefore:

- compare time scales **within the same exact model**;
- identify which represented variable dominates late fractional change;
- use residence/inventory/change times only as diagnostic descriptors;
- do not infer prototype scaling without a separate scaling study.

### 9.3 Useful idea from Three-Level Scaling

The source's Level-2 emphasis on component mass/energy inventory and inter-component transfer is especially relevant. It supports ranking late owners by conserved inventory and transfer mismatch rather than by whichever displayed pressure happens to have the largest visual slope.

---

## 10. Chapter 3 — model reduction can filter fast dynamics while preserving slower response

### 10.1 Source-derived finding

Volume II Ch.3 compares transient channel formulations with different momentum/compressibility approximations. The detailed sectionalized compressible model can display fast pressure-wave/acoustic behavior that simpler integral/single-velocity models intentionally smooth or neglect; the same examples show that a new long-time mass-flux state can still emerge after the fast transient.

**Transferability:** `DIRECT_CONCEPT` for model-form/time-scale discipline; example BWR values are `NON_TRANSFERABLE_NUMERIC`.

### 10.2 Project consequence

The P1A residual tail should not be diagnosed by assuming that every real hydraulic time scale is represented. Our fixed-step lumped model has its own resolved bandwidth and constitutive assumptions.

A slow-owner diagnostic should therefore focus on:

- represented conserved inventories;
- represented controller memories;
- represented hydraulic/steam flows and pressure grades;
- deterministic long-time trends;

and separately report that high-frequency compressible/acoustic channel physics is outside the current model scope where applicable.

This is another reason not to add a new fast physics model inside M10 merely to explain a slow tail.

---

## 11. Chapter 4 — friction-dominated, gravity-dominated and reverse-flow regimes

### 11.1 Source-derived finding

Volume II Ch.4 treats multiple heated channels across friction-dominated and gravity-dominated regimes. At sufficiently low flow, buoyancy/gravity can dominate and the pressure-drop/flow characteristic can develop qualitatively different branches including upflow/downflow and flow reversal. The chapter explicitly stresses multiple solutions and instability mechanisms.

**Transferability:** `DIRECT_CONCEPT` as a future hydraulic-domain requirement; present P1A high-load state does not by itself establish that this low-flow regime is active.

### 11.2 Project consequence

M12 extreme-hydraulics work should eventually permit stagnation/reversal/natural-circulation behavior when physically supported instead of silently clamping it away.

For current M10, however, this is an exclusion rule: P1A's late normal-load drift is **not evidence that reverse flow or gravity-dominated instability is occurring**. Only canonical runtime evidence can support such a classification.

---

## 12. Chapter 7 — loop analysis gives the right macro-level owner vocabulary

### 12.1 Source-derived finding

Volume II Ch.7 deliberately distinguishes system/loop analysis from detailed local component analysis. It describes simplified loop models as useful for operational/control trends and for generating pressure/flow boundary conditions for detailed component calculations. Natural circulation is treated through the balance between density/buoyancy effects and hydraulic resistance when pump head is absent or reduced.

**Transferability:** `DIRECT_CONCEPT` for system-level balance; the PWR example geometry is `ANALOGICAL` and not imported into the direct-cycle RBMK-like plant.

### 12.2 Project consequence

This is probably the most useful source framing for Plan Amendment 2. A diagnostic can remain deliberately macro-level while still being physically meaningful if it reports the canonical system balances.

The desirable owner vector is:

```text
PUMP / ACTUATOR CONTRIBUTION
    available hydraulic drive as represented

DENSITY / BUOYANCY CONTRIBUTION
    only if represented by canonical model

NETWORK LOSSES
    friction/form/other represented losses

INERTIAL / INVENTORY CHANGE
    time derivative of represented stored mass/energy/momentum proxies

BOUNDARY PRESSURES / FLOWS
    actual canonical boundary state
```

Where a term does not exist in the current model, it must remain explicitly unavailable rather than estimated from a new textbook formula.

---

## 13. Chapter 12 — a fidelity ladder, not an instruction to upgrade M10

### 13.1 Source-derived finding

Volume II Ch.12 explicitly lays out three-, four- and five-equation thermal-hydraulic formulations and richer six-equation single-pressure and seven-equation two-pressure two-fluid models. The two-fluid formulation can represent separate phase motion, compressibility, interfacial exchange, flashing, condensation and other nonequilibrium phenomena at the cost of additional equations and constitutive closures.

**Transferability:** `DIRECT_CONCEPT` for fidelity classification; implementing those models now is `FUTURE_ONLY`.

### 13.2 Project consequence

The current simulator should gain a clear fidelity label, not a last-minute equation-count upgrade. The useful ladder is:

```text
current M10/M11 baseline:
    lumped reduced mixture / HEM-like water-steam physics

possible later fidelity work:
    slip/drift-corrected mixture
    richer nonequilibrium mixture
    two-fluid formulations
```

A move between levels changes what can be claimed and would require separate V&V. P1B must use the current owner and may not “diagnose” missing two-fluid variables.

---

## 14. Appendix L — stationarity should be checked on conserved state, not presentation alone

### 14.1 Source-derived finding

Appendix L derives lumped-parameter control-volume discretizations for mass, energy and momentum for a homogeneous thermally equilibrated water/steam mixture. It explicitly treats junctions/flow paths between control volumes as transfer interfaces.

**Transferability:** `DIRECT_CONCEPT` and especially relevant to the simulator's existing M3 staged balance accumulation and exactly-once inventory integration.

### 14.2 Project consequence

This gives a strong source basis for making **inventory-rate closure** central to the next diagnostic.

A pressure can drift because stored mass or energy is still changing even when a displayed load has converged. Therefore, for every key fluid inventory already owned by the runtime, a useful diagnostic should prefer quantities such as:

```text
mass M
mass rate dM/dt
net canonical mass source/sink rate

internal energy U
energy rate dU/dt
net canonical heat/work/enthalpy transfer rate

level or fill fraction
level slope
```

and only then correlate these with pressure/flow/output trends.

This is not a proposal for a second integrator. The Application/validation layer observes the committed evolution produced by the canonical solver.

---

## 15. Chapter 13 — normalized sensitivity is useful only if physical units remain visible

### 15.1 Source-derived finding

Volume II Ch.13 develops first-order sensitivity/uncertainty propagation using gradients/Jacobians evaluated around nominal inputs. The method is a local linear approximation; correlation among inputs matters through the covariance structure.

**Transferability:** `DIRECT_CONCEPT` for local-ranking discipline; not an authorization to globally linearize the plant.

### 15.2 Project consequence

For slow-owner ranking we may use a normalized score so that kg/s, J/s, Pa/s and MW/s can be compared in one report, but the normalization must never replace the physical-unit evidence.

Preferred report form:

```text
metric                  raw slope / residual        normalized rank
outlet.mass-rate        ... kg/s                    ...
drum.energy-rate        ... MW                      ...
inlet-pressure-slope    ... Pa/s                    ...
controller-I-rate       ... unit/s                  ...
```

The normalized score is a triage aid, not a new acceptance criterion and not proof of global sensitivity.

---

# Part C — Cross-volume consequences for the post-P1A plan

## 16. The most important result of Deep Review Pass 1

The two volumes jointly change the *shape* of the next diagnostic more than they change our physical model.

They strongly support this ordering:

```text
1. CONSERVED INVENTORY CLOSURE
   mass and energy storage rates

2. HYDRAULIC COMPATIBILITY
   boundary pressures, flow, represented head/loss contributions

3. PARALLEL-BRANCH COMPATIBILITY
   total flow plus represented group/branch redistribution

4. STEAM-PATH / TURBINE COUPLING
   pressure grade, steam flow, valve demand/position, shaft balance

5. CONTROL MEMORY
   controller error/integrator/output slope, saturation/headroom

6. ELECTRICAL CLOSURE
   load error, rotor/frequency/phase residuals

7. MODEL-FIDELITY / NUMERICAL SENTINELS
   phase branch, fallback/rollback/repeatability and explicit unavailable physics
```

P1A already suggests that item 6 is nearly closed while one or more earlier items may still be evolving.

## 17. Candidate observable inventory for Plan Amendment 2 — not yet frozen

This list is a **literature-informed candidate**, not the amendment.

### 17.1 Tier A — should be mandatory if canonical state already exposes it

- mass and internal energy of `outlet`, drum and other major water/steam inventories implicated in the 6 MWe path;
- `dm/dt` and `dU/dt` from committed states;
- canonical inflow/outflow or balance residuals already produced by the network;
- steam export and feedwater flow, separately;
- drum level and level slope;
- channel/return/loop flow and their slopes;
- pressure at drum/outlet/steam header/turbine inlet where represented;
- steam flow and shaft power;
- load request, actual output, dispatch residual, frequency/phase residual;
- valve/controller requested, effective and physical state where available;
- controller error, integral/bias state and their slopes where available;
- thermodynamic phase/quality/void from the current canonical HEM-like model;
- trip/protection/authority state;
- deterministic-repeat/numerical-coupling sentinels already available.

### 17.2 Tier B — valuable only if already canonical or cheaply observational

- pressure-loss decomposition by current owner: friction / form / gravity / acceleration;
- pump available head and current system-required head;
- per-channel-group/per-branch flow redistribution and pressure-drop spread;
- normalized fractional inventory-change times such as `|M/(dM/dt)|` or `|U/(dU/dt)|`, clearly marked diagnostic-only and guarded near zero derivatives;
- correlation/lag between inventory change and downstream pressure/steam-flow change;
- phase/branch transition count in the diagnostic window.

### 17.3 Tier C — explicitly unavailable / forbidden to synthesize in M10

- liquid/vapor slip velocity;
- separate phasic temperatures;
- ONB/NVG location/time;
- actual flow-regime classification from new correlations;
- drift-flux/two-fluid interfacial terms;
- real density-wave stability margin;
- prototype/full-scale RBMK time-scale claims;
- a new natural-circulation model;
- a new CHF/dryout model;
- new two-phase pressure-drop correlations.

## 18. What the next diagnostic should classify

The literature suggests that a useful classification should separate at least four questions:

```text
A. LOAD REACHABILITY
   Can requested electrical output be reached?

B. REPRESENTED STATIONARITY
   Are all required canonical state/inventory/control derivatives bounded?

C. OWNER LOCALIZATION
   Which represented owner dominates any remaining drift?

D. STABILITY QUALIFICATION
   Does a stationary point recover from perturbations?
```

P1A has already largely answered A for 6 MWe. The immediate post-P1A work should target B and C. D belongs after stationarity is established and may remain in M12.0 unless the closure plan explicitly requires an earlier bounded proof.

## 19. How to avoid another “just wait longer” experiment

The two volumes give a principled alternative to a blind 7,200 s extension.

A next gate should stop based on **information**, not simply elapsed time. Possible evidence logic for the later amendment to consider is:

- sample a fixed, bounded late window;
- rank raw physical residuals/derivatives by canonical owner;
- determine whether one or more inventory/control residuals remain materially above their numerical/measurement floor;
- identify whether the leading residual is decaying, stationary-biased, oscillatory or changing branch;
- stop when the owner classification is stable enough to decide the next engineering action, even if exact asymptotic zero is not reached;
- never turn a normalized ranking score into a post-hoc acceptance threshold.

The exact durations, windows and thresholds are deliberately **not frozen in this review**. Plan Amendment 2 must define them before execution, and the mandatory second deep review will audit them.

## 20. Numerical-model boundary for the next experiment

The source literature reinforces three non-negotiable rules:

1. **Do not duplicate constitutive physics in the diagnostic layer.** Use canonical runtime state/diagnostics as the source of truth.
2. **Do not infer unmodelled physics.** HEM-like output does not prove slip, ONB/NVG, density-wave or two-fluid behavior.
3. **Do not confuse numerical and physical drift.** If the leading residual approaches a numerical floor or depends strongly on coupling/timestep behavior, classify that explicitly before changing a physical coefficient.

## 21. Future M12/M14 consequences, outside M10 authorization

The deep review also sharpens the longer-term roadmap:

- M12.0 operating-point qualification should explicitly distinguish equilibrium/root closure, branch identity and local perturbation stability;
- M12 extreme hydraulics should own mixed/natural circulation, stagnation/reversal and pump coastdown where supported;
- M12/M15 thermal-limit work should distinguish reduced DNB/dryout claims if implemented;
- M14 representative channel groups should preserve declared quantities such as total heat, total flow, pressure drop and outlet enthalpy rather than merely adding zones;
- M14 hydraulic flow split should emerge from shared boundary compatibility rather than arbitrary per-group flow targets if/when fidelity is increased;
- any later slip/drift/two-fluid upgrade becomes a new physical-model/V&V milestone rather than a hidden M10 correction.

---

## 22. Mandatory second deep review after Plan Amendment 2

Once Plan Amendment 2 exists, repeat this review against the **actual amendment text** and produce a Pass 2 disposition. At minimum, check:

1. every required observable is canonical or explicitly derived from committed canonical state;
2. no unavailable HEM/two-fluid quantity has been invented;
3. flow and pressure are not overconstrained contrary to the runtime's boundary ownership;
4. inventory mass/energy rates are included where the current system exposes them;
5. pressure/head/loss decomposition uses canonical owners only;
6. controller-memory and actuator-state ownership are visible;
7. physical and numerical classifications are separated;
8. the amendment does not silently become a new physical model;
9. no book correlation/number became an M10 threshold without independent project justification;
10. the experiment remains bounded and its stop/decision logic is frozen before execution.

Only after that Pass 2 review should the new executable diagnostic contract be considered literature-audited.

## 23. Final disposition of Pass 1

**Deep Review Pass 1: COMPLETE FOR PRE-AMENDMENT PLANNING.**

The main conclusion is not “upgrade the thermal hydraulics now.” It is the opposite: **use the current model more observably and more rigorously before changing it**. P1A has already demonstrated 6 MWe reachability; the next engineering value comes from conserved-inventory, hydraulic compatibility, steam-path, controller-memory and numerical-owner evidence that identifies why the whole operating point is not yet stationary.
