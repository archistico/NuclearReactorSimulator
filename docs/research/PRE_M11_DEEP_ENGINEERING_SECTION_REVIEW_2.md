# Pre-M11 Deep Engineering Section Review — Review 2

## Status and purpose

**RESEARCH / PLANNING REVIEW — documentation-only. Not M10 promotion evidence and not implementation authorization.**

Review 2 is a deliberate second pass over the exact parts of the five additional books that were previously identified as the sections worth studying in depth. It does not broaden the literature search indiscriminately: it re-reads the selected sections, checks the earlier Review 1 conclusions against the source text, records corrections or sharper boundaries, and translates only defensible engineering consequences into project planning.

The active executable candidate remains **M10 Final Replacement-Long Closure Plan 1 — P1A Asymptotic Closure Extension**. Review 2 does not change P1A source, pre-existing tests, exact-v9 semantics, thresholds, checkpoints, maximum 3,600 s hold, mission binding, protection behavior, P2R decision rules or second replacement-long authorization.

The review uses four transferability classes:

- `DIRECT_CONCEPT` — the physical/control principle transfers at the level of architecture or diagnostic reasoning;
- `ANALOGICAL` — the source is a different plant/reactor technology, so only the stated systems principle transfers;
- `FUTURE_ONLY` — useful for a later milestone but not for current M10 execution;
- `NON_TRANSFERABLE_NUMERIC` — numerical values, correlations, geometry, tuning and performance limits must not be copied.

## 1. Basu & Debnath — plant coordination, turbine governing and load-demand shaping

### 1.1 Previously selected material re-read

The deep-study set is:

- Chapter 10, **Coordinated Control System**, especially pp. 797–815;
- Chapter 9 §2, **Electro Hydraulic Governor Control System**, pp. 745–753;
- Chapter 8 §2, **Steam Pressure Control With Load Index**, pp. 634–637;
- Chapter 10 §6.2–6.3, **Boiler Turbine Balance / Concept of Heat Release**, pp. 812–815;
- Chapter 13 turbine-control discussion, especially the supervisory/EHC layering;
- Chapter 6 control valves and actuators;
- Chapter 14 alarm/safety-lifecycle material only where it provides generic alarm-engineering principles.

### 1.2 What Review 1 got right

Review 1 correctly retained the central idea that a power unit cannot be reduced to a single electrical-load command. In coordinated operation the heat source, steam system, turbine and generator form a coupled energy-conversion chain. The turbine/governor can move rapidly, while the heat source and stored thermal/steam inventories respond on different time scales. A sustained new load therefore requires a new whole-unit energy balance, not merely a changed valve position or MW request.

It was also correct to treat turbine inlet steam pressure as a key coupling variable. In the source, coordinated, turbine-follow and boiler-follow modes are distinguished by which subsystem follows which demand/pressure signal; that is a much stronger statement than “the turbine controls power.” The steam system carries the mismatch information between turbine admission and heat release.

**Disposition:** `DIRECT_CONCEPT` for coupled-owner reasoning; `ANALOGICAL` for the fossil boiler/reactor heat-source substitution.

### 1.3 Refined finding: load demand is a processed trajectory, not an unconstrained setpoint

The detailed re-read sharpens an important point that Review 1 mentioned only briefly. The source does not treat unit load demand as a raw operator/LDC number passed directly to final actuators. It is conditioned through:

- upper/lower unit-load limits;
- a tracking/ramp integrator;
- ramp-rate limits that may come from a turbine thermal-stress evaluator;
- directional block/hold logic when plant variables indicate that further increase or decrease is not supportable;
- mode-dependent transfer logic designed to avoid bumps.

For this project the transferable principle is not “implement the textbook controller.” It is that a future P3-W workload/procedure, if P2R authorizes it, should distinguish at least:

```text
requested demand
  -> qualified/rate-shaped effective demand
  -> controller output / actuator demand
  -> physical actuator position
  -> steam/shaft response
  -> actual electrical output
```

This is more precise than the earlier two-level distinction between request and response. It also explains why an apparently reasonable requested MW trajectory can still be physically premature.

**Disposition:** `DIRECT_CONCEPT` for state/rate gating and requested/effective/physical separation; `NON_TRANSFERABLE_NUMERIC` for ramp rates, limits and thermal-stress thresholds.

### 1.4 Refined finding: governor ownership is layered and mode-dependent

The EHG material reinforces several existing simulator architecture choices:

- pre-synchronization control is fundamentally a speed/frequency problem;
- after grid connection, load/frequency/droop semantics become relevant;
- pressure-control/turbine-follow behavior is a distinct mode, not just another PID setpoint;
- supervisory logic can constrain or direct a lower-level governor without becoming the physical actuator owner;
- final valve motion depends on position demand versus position feedback and on actuator dynamics.

For Nuclear Reactor Simulator this validates the *ownership layering* already used in M5: supervisory intent must not become a second physical valve owner. It also reinforces the need to keep requested actuator position distinct from committed physical position, especially when travel limits are active.

**Disposition:** `DIRECT_CONCEPT`; machine-specific valve sequencing, control percentages and hydraulic implementation remain `NON_TRANSFERABLE_NUMERIC`.

### 1.5 Refined finding: coordinated control should inhibit impossible demand rather than hide the mismatch

The source includes directional demand blocking/holding based on excessive deviations in steam pressure, feedwater and other process variables. The key project lesson is not to add these exact fossil signals. It is to require that any future automatic load coordinator expose why it is holding or limiting a demand and use canonical plant evidence rather than silently forcing an output.

This aligns with the project’s existing automation-transparency requirement: `requested`, `effective`, `inhibited/degraded`, `reason`, and `observed result` should remain distinct concepts.

**Disposition:** `FUTURE_ONLY` for an industrial-style unit-load master; immediately useful as planning vocabulary.

### 1.6 Alarm material: useful but subordinate

The Chapter 14 alarm discussion reinforces generic properties already adopted elsewhere in the project: alarms should be relevant, unique, timely, prioritized, diagnosable and tied to operator action; alarm floods, chatter, stale/standing alarms and redundant alarms are undesirable. This is supportive evidence for M13/HMI planning, but this fossil-plant handbook is not treated as the project’s regulatory authority and does not supersede the dedicated Digital I&C/human-system review.

### 1.7 Documentation consequences

Review 2 therefore strengthens these requirements without changing runtime:

- P2R owner analysis must include demand shaping/controller memory as well as physical plant state;
- future P3-W should use evidence-based stage/ramp readiness rather than fixed timing copied from another technology;
- a future coordinated-load feature must be transparent about limit/hold state;
- M13 automation surfaces should preserve requested/effective/physical distinctions;
- no fossil DEB equations, boiler-firing logic, ramp percentages or vendor tuning are imported.

---

## 2. Riznic — two-phase circulation, separation and steam-system performance

### 2.1 Previously selected material re-read

The deep-study set is:

- Chapter 2 §§2.2–2.7, especially circulation and feedwater design;
- Chapter 4, **Thermalhydraulics, circulation, and steam-water separation in nuclear steam generators**, pp. 81–104;
- Chapter 12, **Thermal performance degradation and heat-transfer fouling**, pp. 365–398;
- Chapter 1.4 steam-generator types for architectural context;
- Chapter 5 WWER material for comparison only;
- Chapter 13 flow-induced vibration as future integrity context only.

The book is primarily about PWR/CANDU/WWER steam generators. The simulator’s RBMK-like direct boiling/steam path is architecturally different. That difference is now elevated from a footnote to an explicit transferability boundary.

### 2.2 What Review 1 got right

Review 1 correctly retained the circulation principle: in a recirculating two-phase system, circulation is an emergent balance between a driving head and total circuit losses. The source describes natural circulation specifically as the hydrostatic-head difference between a denser saturated-water downcomer/annulus and a lower-density two-phase riser/bundle region, balanced against friction, entrance/exit, support and separator losses.

For the simulator, the direct transfer is the *equilibrium logic*, not the PWR steam-generator geometry or a target circulation ratio.

**Disposition:** `DIRECT_CONCEPT` for head/loss balance; `ANALOGICAL` for applying that reasoning to the RBMK-like primary loop; all quoted circulation ratios are `NON_TRANSFERABLE_NUMERIC`.

### 2.3 Refined finding: quality, void fraction and circulation ratio are related but not interchangeable

The source makes a useful distinction that matters for our diagnostics. The average outlet quality of a recirculating bundle is related to the circulation ratio under its idealized definition, yet the same mass-quality state can correspond to a very large volumetric void fraction because liquid and vapor densities differ strongly. Local qualities also vary spatially.

For this project, that reinforces three model/diagnostic rules:

1. mass quality, volumetric void and loop flow must remain separate quantities;
2. a single aggregate flow value cannot stand in for local two-phase state;
3. future spatial fidelity must not infer detailed channel/bundle distributions from a global mean without explicit provenance.

This directly supports the existing separation between `VaporQuality` and `VoidFraction` in M1/M2.

### 2.4 Refined finding: separator behavior participates in the hydraulic loop

The detailed separator discussion is more consequential than Review 1’s simple “ideal separator limitation” statement. The source identifies two distinct failure/imperfection directions:

- **carryover** — liquid moisture leaves with nominal steam;
- **carry-under** — steam remains entrained in liquid returning to the circulation path.

Carry-under matters hydraulically because even a small vapor content can lower the density of the returning fluid and therefore reduce hydrostatic driving head. Separator pressure loss also belongs to total loop loss. The separator is thus not merely an output-quality filter; it participates in the circulation equilibrium.

**Project consequence:** the current ideal deterministic separator is a stronger simplification than “100% quality at outlet.” It omits a feedback path between separation quality, return density and circulation. This remains a known limitation and is **not** to be patched into M10 by tuning a flow coefficient.

**Disposition:** `DIRECT_CONCEPT`; separator efficiencies, moisture limits and vendor designs are `NON_TRANSFERABLE_NUMERIC`.

### 2.5 Refined finding: measured steam pressure can include downstream loss effects

Chapter 12’s performance analysis discusses how separator, outlet-nozzle and downstream piping pressure drops can affect measured steam pressure and inferred thermal performance. The project lesson is diagnostic: a pressure shortfall at a measurement point is not automatically proof that the heat source is insufficient. The pressure-grade owner chain must be decomposed.

This supports the existing M10 practice of separating main-steam line, stop/control valve and turbine-stage capacity instead of treating “steam pressure” as a single owner.

**Disposition:** `DIRECT_CONCEPT` for pressure-grade decomposition; plant-specific fouling interpretations are `ANALOGICAL`/`FUTURE_ONLY`.

### 2.6 Thermal-performance degradation: retain the concept, not the mechanism

The fouling chapter demonstrates that a heat-transfer degradation can appear as reduced steam pressure and/or thermal performance and that interpretation requires separating tube-bundle heat transfer from other pressure-loss effects. The simulator currently does not model nuclear steam-generator fouling chemistry/deposits and should not acquire such a mechanism merely because it appears in this source.

The useful future principle is a general one: component degradation should alter the physical owner that represents the degraded mechanism, while diagnostic inference must not confuse heat-transfer loss with hydraulic pressure loss.

### 2.7 Flow-induced vibration: explicitly deferred

Chapter 13 is relevant to future structural integrity because two-phase velocities and flow distributions can excite tube vibration. The current simulator has no resolved steam-generator tube bundle and no structural vibration model. Therefore no vibration threshold, tube-support rule or fatigue number is imported. The subject is only a future M12/M15 integrity research input if a corresponding physical structure ever exists in the model.

### 2.8 Documentation consequences

Review 2 adds or strengthens:

- M12.0 equilibrium evidence should report head/loss compatibility, not a preferred scalar flow target;
- current ideal separation must explicitly mention the omitted carry-under → return-density → circulation feedback;
- pressure-grade diagnostics should distinguish source pressure from downstream loss;
- M14 local layers must not conflate quality and void or infer spatial distributions from global averages;
- PWR/CANDU/WWER geometry, pressures, circulation ratios, separator efficiencies and fouling numbers remain non-importable.

---

## 3. Badescu, Lazaroiu & Barelli — flexibility as a vector of capabilities

### 3.1 Previously selected material re-read

The deep-study focus is the power-plant flexibility material covering:

- ramp rate;
- minimum stable generation (MSG);
- frequency-response capability;
- spinning headroom;
- commitment/start time and idle capability;
- part-load efficiency;
- steam-cycle/turbine flexibility and short-lived stored-energy mechanisms.

The detailed examples are largely CCGT/coal oriented. They are retained only where they define generally useful capability dimensions.

### 3.2 What Review 1 got right

Review 1 correctly concluded that “flexibility” is not one scalar. A plant can be good at one of these and poor at another. In particular, rapid frequency response is not the same property as a sustainable ramp to a new steady load.

The source also emphasizes that temporal resolution matters: a model sampled too coarsely can miss ramp-rate constraints. This is directly compatible with our use of deterministic 10 ms logical stepping for fast ownership and separate long windows for stationarity.

### 3.3 Refined finding: headroom is a state variable in the qualification argument

Frequency response requires available spinning headroom and is limited by physical governor/plant capability. This sharpens the project’s future load-qualification vocabulary: it is insufficient to report only requested MW and actual MW. A manoeuvre should establish whether the plant had *upward or downward margin available at the moment the response was requested*.

For Nuclear Reactor Simulator, “headroom” should not become one invented percentage. It should be derived from the existing owners that actually limit additional support: steam admission capacity, thermal/steam production, shaft balance, protection margins and controller/actuator saturation.

**Disposition:** `DIRECT_CONCEPT`; source percentages are `NON_TRANSFERABLE_NUMERIC`.

### 3.4 Refined finding: transient stored-energy support can create a false impression of sustainable flexibility

Steam-valve action and other stored-energy mechanisms can release energy quickly but only for finite time. This cross-checks the Basu/Judd conclusion and is directly relevant to interpreting long M10 trajectories: an early rise in shaft/electrical output is not proof of a new operating point if steam/thermal inventories continue to drain or controller integrals continue to move.

The inverse is also important: a slower initial response is not automatically a defect if the plant is deliberately respecting a sustainable thermal trajectory. Qualification must separate *response speed* from *final supportability*.

### 3.5 Refined finding: minimum stable generation is not a universal percentage

MSG is a useful concept—there is a lowest load at which a given thermal unit can continuously operate within its constraints—but the values in the source belong to particular CCGT technologies. The project may later define a qualified operating envelope for its own 10 MWe reference plant, but that envelope must come from simulator-specific evidence and model ownership, not imported percentages.

### 3.6 Part-load efficiency is a different axis from stability

Part-load efficiency affects how much input energy is needed per electrical output, but “less efficient at part load” is not the same as “unstable at part load.” The simulator should avoid using one metric as a proxy for the other. Any future efficiency/heat-rate diagnostic must derive from audited power paths, while stability remains a residual/trend qualification.

### 3.7 Documentation consequences

Future P3-W/P4 language should separate:

```text
response speed
available headroom
sustained thermal support
stable operating range
settling / inventory recovery
part-load efficiency
```

No CCGT ramp percentage, MSG percentage, startup time or flexibility-economics result is adopted as a simulator acceptance limit.

---

## 4. Judd — nuclear load following, stored thermal mass, protection and rod worth

### 4.1 Previously selected material re-read

The deep-study set is:

- Chapter 4.4 **Control Systems**, pp. 232–237;
- Chapter 4.3 steam plant / available-energy context;
- Chapter 5.2 protective systems and decay-heat removal;
- Chapter 1.5–1.6 control rods and reactivity coefficients;
- Chapter 3.2 heat transfer and transport.

The reactor is a fast reactor with liquid-metal cooling. Technology-specific coefficients, pump strategies, coolant properties and accident numbers do not transfer to the RBMK-like reference plant.

### 4.2 What Review 1 got right: the load-following causal chain

Judd’s load-following description is one of the clearest source checks for our P2R reasoning. A load increase first perturbs alternator speed/frequency; turbine admission is changed to restore the electrical condition; this perturbs steam pressure; the heat source then changes to restore steam pressure. It explicitly describes a hierarchy of time scales rather than a single simultaneous command.

For our project the exact control law differs, but the diagnostic chain transfers well:

```text
grid/load disturbance
  -> turbine/governor response
  -> steam-pressure/inventory disturbance
  -> reactor/thermal-source response
  -> new steam/shaft/electrical balance
```

**Disposition:** `ANALOGICAL` but high-value.

### 4.3 Refined finding: thermal mass intentionally filters short disturbances

Judd points out that water in steam generators/drums and metal in steam-system structures can smooth steam-demand changes, so steam pressure can respond more slowly than turbine-valve position. This gives a stronger interpretation of stored energy than Review 1 had: thermal storage is not just an “extra energy source”; it is also a dynamic filter that can delay or reshape the coupling signal.

**Project consequence:** P2R should not expect all owner slopes to change sign or settle simultaneously. Inventory and thermal-body trends can lag a near-settled governor/frequency state. The important question is whether the complete residual vector is converging without a secular hidden drain/fill.

### 4.4 Refined finding: protection and decay-heat removal are separate from normal control

The abnormal-condition discussion reinforces that turbine/grid trips, auxiliary failures, reactor shutdown and decay-heat removal have different owners and different purposes. A fail-safe protective action should not be implemented as a side effect of normal load control, and shutdown does not erase residual heat-removal obligations.

This is consistent with existing M5 protection priority and M12.5 post-trip decay-heat planning. No fast-reactor protection thresholds are imported.

### 4.5 Refined finding: control-rod worth is spatially nonlinear

Judd’s control-rod discussion makes the spatial limitation of the current reduced model concrete. Rod worth depends on neutron flux/importance over the inserted region; insertion can reshape the flux itself, and the resulting integral worth-versus-position curve is generally nonlinear. A simple smooth-step or linear mapping is therefore a reduced control law, not physical validation of a real core.

**Project consequence for M14:** when multiple rods/rod groups acquire explicit zone mapping, rod influence should eventually depend on spatial state/provenance rather than merely painting a global worth onto a 2D map. This is a future fidelity objective, not an M10 change.

### 4.6 Reactivity coefficients: design dependence is the key lesson

The fast-reactor coefficient sections demonstrate that feedback coefficients arise from specific material/spectral/geometric mechanisms and depend on reactor design. The transferable lesson is therefore *not* the sign or magnitude of Judd’s fast-reactor coefficients. It is the requirement that our RBMK-like coefficients remain explicitly configured/reduced and that any future high-fidelity coefficient field be sourced from an appropriate model/reference for that reactor family.

### 4.7 Heat-transfer limit: power capability ultimately depends on heat removal

The core heat-transfer material states the general engineering constraint that fission heat must be removed without exceeding material/thermal limits. For the simulator this supports treating “electrically requested power” as subordinate to physical heat-removal capability. It does not supply our fuel/channel thermal limits.

### 4.8 Documentation consequences

Review 2 strengthens:

- P2R interpretation of steam pressure/inventory as a coupling state between fast turbine response and slower heat-source response;
- M12.5 separation of post-trip decay heat from normal shutdown command semantics;
- M14 future spatial rod-worth/provenance requirements;
- explicit non-import of sodium/fast-reactor numbers and coefficient signs.

---

## 5. Hébert — nuclear-data hierarchy, homogenization, depletion, sensitivities and time-dependent kinetics

### 5.1 Previously selected material re-read

The deep-study set is:

- delayed-neutron data and precursor treatment;
- §2.6 Doppler broadening and thermal-motion/binding effects;
- Chapter 4 lattice calculation, resonance self-shielding, homogenization/condensation and SPH equivalence;
- §4.5 isotopic depletion;
- Chapter 5 full-core calculation;
- §5.3 generalized perturbation theory (GPT);
- §5.4 point kinetics and space-time kinetics;
- temporal numerical schemes used for time-dependent neutron equations.

This is the most directly useful source for the future M14 physics *architecture*, but it does not provide a ready-made RBMK parameter set for the current reduced simulator.

### 5.2 What Review 1 got right: point kinetics has a precise spatial assumption

The detailed derivation makes the limitation sharper: point kinetics assumes that the shape of the neutron flux does not change during the transient (or, equivalently in the ideal homogeneous case, that there is no spatial dependence to resolve). The global amplitude and precursor populations evolve, but spatial redistribution is not independently solved.

This justifies the current project claim:

`VERIFIED — reduced-order educational point kinetics`

and explicitly rejects a stronger claim such as “full-core transient neutronics validated.”

### 5.3 Refined finding: effective kinetic parameters need not be immutable in a spatial transient

In the more general derivation, effective reactivity, generation time and delayed-neutron parameters are weighted quantities associated with a flux/importance distribution. If that distribution changes materially, the effective parameters can also change. The current simulator’s configured global parameter sets are therefore a model boundary, not merely a convenient constant table.

**Project consequence for M14:** adding spatial zones must not silently retain the claim that one fixed global kinetics parameter set is physically equivalent to a solved space-time core. Either M14 remains explicitly quasi-spatial, or a later step owns the additional physics required for state-dependent effective parameters.

### 5.4 Refined finding: Doppler broadening supports the phenomenon, not our linear coefficient

Hébert shows that increasing material temperature broadens resonance cross sections while reducing peak height, with the resonance-area relationship preserved by the convolution treatment. This is a microscopic nuclear-data effect. The current `rho = alpha * (T - T_ref)` feedback is therefore properly understood as a reduced macroscopic surrogate calibrated outside the nuclear-data calculation, not an implementation of Doppler broadening itself.

The text also emphasizes that thermal motion changes scattering behavior and that molecular/metallic binding matters for moderators at thermal energies. This supports the same boundary for water/graphite: high-fidelity moderator treatment requires material/energy-dependent scattering data; our current coolant/moderator coefficient is not such a calculation.

**Disposition:** `DIRECT_CONCEPT`; all example cross sections/data are `NON_TRANSFERABLE_NUMERIC`.

### 5.5 Refined finding: a credible spatial model is a hierarchy, not just a finer mesh

The lattice-calculation workflow is particularly useful for M14 planning. It separates:

1. evaluated/isotopic nuclear-data access and temperature treatment;
2. resonance self-shielding;
3. multigroup flux/transport calculation with leakage treatment;
4. homogenization and energy condensation;
5. equivalence corrections such as SPH;
6. full-core calculation using the reduced data.

This shows why “add 2D zones” is not by itself a high-fidelity neutronics upgrade. Spatial discretization without an explicit source for equivalent material/cross-section behavior remains quasi-spatial.

### 5.6 Refined finding: homogenization should preserve selected physics, not merely produce plausible maps

Hébert notes that direct flux-volume homogenization generally does not preserve all reaction and leakage rates. SPH factors are introduced so a coarse macro calculation can preserve selected reaction/leakage balances of a finer reference calculation.

This yields a concrete future M14 V&V principle:

> a coarse spatial model should be qualified against conservation/preservation targets (reaction rates, leakage and power integrals as appropriate), not merely against visual similarity or one scalar `k_eff`.

The project need not implement SPH specifically. The requirement is to declare what quantities any reduction is designed to preserve and test those quantities against an independent finer/reference model.

### 5.7 Refined finding: depletion creates history-dependent nuclear properties

The depletion section connects isotopic evolution to neutron flux and reaction rates; burnup is fundamentally time-integrated exposure/power per initial mass, and the evolving material composition changes macroscopic cross sections and thus flux. This has two project consequences:

- current lumped iodine/xenon is a deliberately narrow history-dependent model, not general depletion;
- future spatial xenon/burnup cannot be a presentation-only layer; each zone needs a well-defined local history owner if the model claims local depletion effects.

### 5.8 Refined finding: GPT is a specific adjoint sensitivity method, not a generic “Jacobian feature”

Generalized perturbation theory is presented for reactor characteristics formulated as functionals of a steady-state flux/eigenvalue problem. It can compute gradients useful for optimization/sensitivity studies. This should correct an overly broad possible interpretation from Review 1: GPT does **not** justify adding numerical gradients everywhere in the current simulator.

**Project consequence:** GPT is `FUTURE_ONLY` research for M14+ once there is a spatial neutronics model and a clearly defined reactor characteristic. Existing hydraulic Jacobian work in Phase H is a separate numerical method with no implied equivalence to reactor-physics GPT.

### 5.9 Refined finding: temporal schemes are benchmark options, not an instruction to replace RK4

Hébert discusses explicit, fully implicit and Crank–Nicolson-type time discretizations for time-dependent neutron equations. Their relative behavior is useful as a future verification/reference topic. It does **not** authorize replacing the current deterministic point-kinetics RK4 implementation during M10.

The useful V&V lesson is to compare temporal discretizations or refinement against an independent reference when spatial kinetics is introduced, with stability/accuracy assessed for the actual stiff system and coupling method.

### 5.10 Documentation consequences

Review 2 strengthens M14 planning around a staged fidelity hierarchy:

```text
nuclear data / material state
  -> self-shielded lattice/reference calculation
  -> homogenization / declared preserved quantities
  -> coarse full-core spatial model
  -> local feedback/depletion history
  -> time-dependent spatial kinetics (only if actually implemented)
```

It also freezes the non-claim that the current global point-kinetics + mapped zones are equivalent to this hierarchy.

---

## 6. Cross-source synthesis after the detailed re-read

The five sources now support a more precise common model of plant response:

```text
external electrical demand / disturbance
  -> requested demand
  -> rate/limit/mode-conditioned effective demand
  -> governor/controller output
  -> physical valve/admission motion
  -> immediate shaft/grid response using available steam and stored energy
  -> steam pressure + inventory disturbance
  -> reactor/thermal-source response
  -> boiling, circulation, separation and feedwater response
  -> restored steam/shaft/electrical balance
  -> controller-memory and slow-inventory stationarity
```

The principal correction to a simplistic interpretation is that **successful fast response and successful steady-state qualification are different tests**. A plant can have near-perfect frequency while drawing down an inventory, or can approach requested MW while a controller integral or thermal body still has a persistent signed slope.

For M10, this synthesis changes only *how returned evidence is interpreted at P2R*. It does not change P1A.

## 7. P2R evidence reading — refined checklist

After P1A returns, inspect late-window evidence in the following order:

1. **Demand/control state** — requested load, effective reference, controller error/integral/output, saturation/hold evidence where available.
2. **Actuator state** — demanded versus committed valve/pump state and remaining travel/capacity.
3. **Steam path** — pressure grade, stage/line/valve flow, steam-header/drum/outlet inventories and their slopes.
4. **Primary circulation** — pump head, loop/head losses, channel/return flows, void/quality and inventory slopes.
5. **Thermal source** — fission/decay power and fuel/structure/coolant thermal trends.
6. **Mechanical/electrical conversion** — shaft power, passive loss, net rotor acceleration, electrical output, frequency/phase behavior.
7. **Slow-state closure** — feedwater/drum/hotwell balance, thermal-body storage and all controller memories.

P2R remains bound to the existing classes and branch rule. Literature cannot relabel an `INCONCLUSIVE` trajectory as converged.

## 8. Roadmap integration

### M12.0 — equilibrium/stability

Add explicit qualification language for:

- stored-energy masking of imbalance;
- demand/controller-memory stationarity;
- circulation head/loss compatibility;
- steam pressure-grade decomposition;
- quality/void separation;
- ideal separator omission as a model-limit flag.

### M12.5 / M12.6 — decay heat and integrity

Retain separation of post-trip decay heat from normal control. Treat flow-induced structural vibration only as future integrity research if a physical structure exists to own it.

### M13 — automation transparency

When future supervisory/load-coordination features are visible, preserve `requested -> effective/limited -> actuator demand -> physical state -> observed result` and expose inhibit/hold reason. Do not create a UI-owned control policy.

### M14 — spatial reactor

Strengthen the milestone from “more zones” toward an explicit reduction hierarchy and preservation targets. M14 may still intentionally close as **quasi-spatial educational fidelity**; the documentation should say so if it does not implement lattice/full-core/space-time neutronics.

Recommended future qualification topics include:

- rod-worth spatial dependence/provenance;
- local power and feedback conservation across aggregation;
- reaction/leakage/power preservation targets for any homogenization;
- local xenon/depletion history ownership;
- point-kinetics versus space-time comparison cases if/when a spatial solver exists.

### M15 — consequence progression

Any rapid-reactivity or local-damage claim remains bounded by the M14 neutronic/spatial fidelity actually achieved. A coarse map must not be presented as a solved local neutron-transport accident model.

## 9. Explicit corrections / refinements to Review 1

Review 1 remains directionally correct. Review 2 refines it as follows:

1. **Basu:** “rate-managed demand” is expanded into requested/effective/ramped/limited/physical layers, including directional hold/block concepts.
2. **Riznic:** separator idealization is now recognized as a circulation-feedback omission through carry-under density and separator pressure loss, not only an outlet steam-quality simplification.
3. **Badescu:** headroom is made an explicit dimension of future manoeuvre qualification and must be derived from simulator owners rather than assigned a generic percentage.
4. **Judd:** stored thermal mass is treated as a dynamic filter as well as an energy reservoir; rod worth is explicitly identified as spatial/state-dependent future fidelity.
5. **Hébert:** GPT is narrowed to its actual adjoint-functional sensitivity role; M14 spatial fidelity is framed as a nuclear-data/lattice/homogenization/full-core hierarchy; homogenization must declare and test preserved quantities.
6. **All sources:** no source-specific numeric limit or technology-specific controller is promoted into M10 or the default RBMK-like model by literature alone.

## 10. Governance rule

A Review 2 item can become implementation work only after it has a project owner, explicit milestone/branch home, versioning decision, predeclared acceptance evidence, revalidation scope and a clear statement of what source-specific content is *not* being imported.

Until that happens, this document remains planning evidence. **P1A remains frozen; P2R remains the next decision gate after P1A.**
