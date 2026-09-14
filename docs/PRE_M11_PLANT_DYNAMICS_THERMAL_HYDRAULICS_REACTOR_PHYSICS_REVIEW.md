# Pre-M11 Plant Dynamics, Thermal-Hydraulics & Reactor Physics Reference Review — Reviews 1–2

## Status and purpose

**RESEARCH / PLANNING REVIEW — documentation-only. Not M10 promotion evidence and not implementation authorization.**

Review 1 records the engineering information initially extracted from five additional book-length sources; Review 2 re-reads the previously selected high-value sections in detail and checks/refines those conclusions while **M10 Final Replacement-Long Closure Plan 1 — P1A** remains the executable active candidate. The books inform interpretation, model-limit statements and post-M10 planning. They do **not** change the frozen P1A contract, thresholds, checkpoints, maximum 3,600 s hold, exact-v9 runtime, replacement workload, protection semantics, mission pack or P2R branch rules.

The retained information is separated into:

1. **source-derived principles** — concepts directly supported by the reviewed sources;
2. **project-specific consequences** — conservative implications for Nuclear Reactor Simulator;
3. **non-imports** — numerical constants, reactor-specific correlations or industrial claims that are explicitly not copied into the simulator.

The simulator remains an educational reduced-order RBMK-like plant model. None of these reviews upgrades it to a licensing, safety-analysis or plant-design code.

## 1. Sources reviewed in this pass

1. Swapan Basu and Ajay Kumar Debnath, *Power Plant Instrumentation and Control Handbook: A Guide to Thermal Power Plants*, 2nd ed., Academic Press / Elsevier, 2019. Primary material used: Chapter 10, **Coordinated Control System**, especially demand processing, boiler-turbine balance and heat-release/energy-balance discussion.
2. Viorel Badescu, George Cristian Lazaroiu and Linda Barelli (eds.), *Power Engineering: Advances and Challenges — Part A: Thermal, Hydro and Nuclear Power*, CRC Press / Taylor & Francis, 2018. Primary material used: plant-flexibility dimensions, ramp rate, minimum stable generation, frequency response and steam-turbine flexibility mechanisms.
3. Anthony M. Judd, *An Introduction to the Engineering of Fast Nuclear Reactors*, Cambridge University Press, 2014. Primary material used: Chapter 4 §4.4 **Control Systems**, plus selected protection/decay-heat engineering principles. Fast-reactor-specific physics is not imported.
4. Jovica Riznic (ed.), *Steam Generators for Nuclear Power Plants*, Woodhead Publishing / Elsevier, 2017. Primary material used: Chapter 2 circulation/feedwater design and Chapter 4 thermalhydraulics, circulation and steam-water separation.
5. Alain Hébert, *Applied Reactor Physics*, 3rd ed., Presses internationales Polytechnique, 2020. Primary material used: thermal scattering/Doppler treatment, lattice/full-core calculation workflow, depletion, generalized perturbation concepts and Chapter 5 space-time/point kinetics.

Exact bibliographic provenance and retained/non-retained consequences are also recorded in [`research/PRE_M11_ENGINEERING_REVIEW_SOURCES.md`](research/PRE_M11_ENGINEERING_REVIEW_SOURCES.md).

## 2. Consolidated source-derived findings

### 2.1 A sustained load change is an energy-balance problem for the whole unit

Basu/Debnath describe coordinated control as a common-demand problem in which boiler/heat-source, turbine and generator must behave as one coordinated unit. Their direct-energy-balance discussion distinguishes the faster turbine/governor response from the slower replenishment of energy by the heat source and explicitly accounts for stored steam/thermal energy during load changes.

Judd gives a nuclear load-following formulation with the same high-level structure: grid/frequency disturbance changes turbine admission first; the resulting steam-pressure disturbance then drives the reactor heat source toward the new sustained condition.

**Project consequence:** the project must not equate `requested generator MW` with instant availability of sustained reactor-to-steam-to-shaft energy. A future P3-W load manoeuvre, if authorized, should be qualified as a coordinated trajectory across existing canonical owners rather than as a blind sequence of electrical MW commands.

### 2.2 Fast response and sustained response are different capabilities

The flexibility discussion in Badescu et al. separates ramp rate, minimum stable generation, frequency-response capability, commitment time, idle capability and part-load efficiency. It also describes turbine valves as fast actuators while thermal-source response and other stored-energy mechanisms operate on different time scales and/or cannot sustain an increase indefinitely.

Basu/Debnath similarly treat stored energy as a transient resource that the faster turbine side may draw before the heat source restores the energy balance.

**Project consequence:** future qualification must distinguish at least:

- transient frequency support;
- requested-load ramp capability;
- thermal/steam capacity build-up;
- settling time;
- final stable operating point;
- available headroom during the manoeuvre.

A successful frequency recovery is therefore not by itself proof that requested electrical output has reached a qualified operating point.

### 2.3 A reference operating point is a vector equilibrium, not a single MW value

Riznic's steam-generator treatment reinforces that steam production, feedwater replacement, internal recirculation, pressure losses, quality and separation are mutually coupled. Steam leaving the system must be replaced by feedwater in sustained operation, and the recirculation state is determined by the hydraulic balance of driving head and circuit losses rather than by an arbitrary standalone flow target.

Basu/Debnath add the dynamic energy-balance perspective: in transient operation, energy output is not equal to instantaneous heat-source input unless changes in stored energy are accounted for.

**Project consequence:** M12.0 operating-point qualification must continue to use a vector residual/state view. A candidate point should include, as applicable, reactor thermal power, steam pressure/flow, feedwater flow, fluid and thermal inventories, circulation, turbine shaft power, generator output, controller state and relevant slow inventories. `looks quiet`, `frequency ~= 50 Hz` or `requested MW approximately met` are not standalone equilibrium proofs.

### 2.4 Recirculation is an emergent hydraulic equilibrium

Riznic states that natural-circulation ratio is found by balancing hydrostatic pumping head between the denser downcomer/annulus region and the two-phase riser region against total loop pressure losses. The circulation ratio changes with quality, geometry, separator losses and operating power.

**Project consequence:** for the RBMK-like primary loop, the general engineering principle retained is that a stable recirculation flow must remain explainable by the canonical pressure/head/loss equations. PWR/CANDU steam-generator circulation-ratio numbers are not targets for this simulator.

### 2.5 Steam-water separation is a real dynamic subsystem; the current simulator is intentionally idealized

Riznic describes carryover (liquid moisture leaving with steam) and carry-under (steam retained in recirculated water), separator loading and separator pressure loss. Carry-under can materially alter recirculation by reducing liquid density.

**Project consequence:** the current deterministic ideal phase-separation model remains a declared reduced-order approximation. Carryover, carry-under and detailed separator pressure-loss/efficiency behavior are candidate future fidelity work, not an M10 repair and not hidden tuning parameters for P1A.

### 2.6 Global point kinetics is a valid reduced model but not spatial reactor kinetics

Hébert presents point kinetics inside a broader reactor-physics workflow that includes transport/lattice calculation, full-core calculation and space-time kinetics. Point kinetics collapses spatial and spectral behavior into a global amplitude/precursor representation; space-time treatment is required when spatial redistribution matters.

Hébert also treats delayed-neutron precursors explicitly and shows why their time scales make controlled reactor operation possible.

**Project consequence:** the existing M2 point-kinetics owner remains a defensible reduced-order educational model for global neutron dynamics, but it must not be described as a full-core spatial transient solution. M14 must keep the distinction between global kinetics and quasi-spatial/local presentation explicit.

### 2.7 Current temperature/void feedback coefficients are reduced surrogates for spectral/material physics

Hébert's Doppler and thermal-scattering treatment shows that temperature affects neutron interaction data through resonance broadening and moderator scattering physics; water and graphite have material-specific thermal scattering behavior.

**Project consequence:** configured linear fuel/moderator/coolant/void feedback coefficients in the current simulator remain reduced-order response laws. The book supports the underlying phenomena, but it does not provide plant-specific RBMK coefficient fields that can be copied into the simulator.

### 2.8 A coupled plant contains multiple characteristic time scales

Across the five sources, different owners respond on different time scales:

```text
grid / electrical disturbance
        ↓ fast
governor / turbine admission
        ↓
stored steam / thermal inventory
        ↓
heat-source / reactor-power response
        ↓
boiling / circulation / separation
        ↓
new steam and shaft-power balance
        ↓
full operating-point settling
```

Hébert additionally separates neutron/precursor kinetics from slower plant thermal-hydraulic inventories.

**Project consequence:** at P2R, the useful question is not merely whether 6 MWe was eventually approached. The returned P1A trajectory should be decomposed by owner to determine **which state still has a significant signed derivative** at long times.

## 3. Immediate M10 consequence: P1A remains frozen

The literature review does **not** authorize a P1A change.

The following remain exactly as already frozen by validated P2 / Plan Amendment 1:

- exact-v9 5→5.5 MWe and 5→6 MWe probes only;
- P1 checkpoint reproduction requirements;
- P1-derived stationarity limits;
- hard 3,600 s total hold ceiling;
- no exact-v4 rerun;
- final classes `CONVERGED`, `BIASED-STATIONARY`, `INCONCLUSIVE`;
- mandatory return to P2R;
- no direct P3 authorization.

Changing P1A after reading these books would contaminate the evidence-selection gate. Source-derived hypotheses are therefore used only **after** the P1A artifact is returned.

## 4. P2R interpretation checklist — project-specific synthesis

The following checklist is a project-specific synthesis informed by the reviewed sources. It is **not** a textbook-prescribed test and does not alter P1A acceptance criteria.

For the returned P1A 5.5/6 MWe trajectories, inspect the late-window derivative/sign and amplitude of each owner in sequence:

1. **Neutronics:** neutron population/fission power, total reactivity, delayed-neutron precursor state where available.
2. **Thermal source:** fuel/structure temperatures and reactor thermal power.
3. **Primary/steam production:** channel return/outlet inventories, drum inventories/level, pressure, steam generation/export and recirculation flow.
4. **Steam admission:** turbine-inlet pressure/flow, control-valve effective position and saturation/headroom.
5. **Mechanical conversion:** turbine shaft power, passive loss and net rotor-acceleration power.
6. **Grid coupling:** requested output, actual electrical output, frequency slip, phase correction and electromagnetic/mechanical dispatch balance.
7. **Control memory:** reactor-power/governor/feedwater integral/output slopes and saturation state.

Decision interpretation remains governed by the already frozen P2R rule:

- P1A `CONVERGED` → P2R may authorize **P3-W**;
- P1A `BIASED-STATIONARY` → P2R may authorize **P3-R**;
- P1A `INCONCLUSIVE` → another explicit **planning stop**.

The books may help identify the likely owner inside P3-R or shape the manoeuvre in P3-W, but they do not override this branch contract.

## 5. Consequences for P3-W if and only if P2R authorizes the workload/procedure branch

A P3-W candidate should not be framed as merely “choose a smaller MW step and a longer fixed dwell.” The reviewed sources support a more rigorous future qualification vocabulary:

- `requested-load trajectory`;
- `thermal/steam capacity trajectory`;
- `frequency-response/headroom evidence`;
- `steam/shaft readiness`;
- `intermediate-state qualification`;
- `final operating-point qualification`.

A future production manoeuvre may still be simple, but its dwell/ramp policy should be justified by measured plant state and the existing control architecture rather than copied from fossil/CCGT values.

## 6. Consequences for P3-R if and only if P2R authorizes the runtime branch

If P1A demonstrates a stationary bias, inspect ownership before changing any coefficient. The preferred causal chain is:

```text
requested electrical load
  → generator/grid dispatch semantics
  → governor / turbine demand
  → valve / steam admission
  → steam-pressure / plant-energy response
  → reactor-power control
  → boiling / steam production
  → shaft power
  → electrical output
```

A P3-R change is justified only by a demonstrable contradiction or missing ownership in this chain. If production behavior changes, exact-v9 remains immutable and a new exact version is required by the existing closure plan.

## 7. Post-M10 roadmap consequences

### M12.0 — Reference Operating-Point Equilibrium & Stability Qualification

Retain/add these evidence goals:

- distinguish accounting closure from physical inventory accumulation;
- include steam/feedwater mass-flow balance and full energy/stored-energy trends;
- characterize recirculation through canonical pressure/head/loss evidence rather than a standalone flow target;
- classify separator idealization explicitly;
- require multi-variable stationarity before freezing a reference operating point.

### M12.5 — Post-trip decay-heat ownership

Judd reinforces the general requirement that shutdown does not eliminate thermal power instantly and that decay heat must remain connected to a credible heat-removal path. Numerical fast-reactor decay-heat values are not imported.

### M14 — Spatial Reactor

Hébert provides a strong theoretical boundary for the M14 claim:

- a quasi-spatial model is not a space-time transport/diffusion calculation;
- local power/flow/void/xenon layers must be labeled as solved/derived/mapped/interpolated according to their real owner;
- local feedback and rod influence must not imply unsupported full-channel neutron transport;
- future spatial xenon requires local history ownership, not merely global xenon copied to a map.

### M15 — Accident Progression & Consequence Models

Global point kinetics may support bounded educational accident progression, but it does not justify a high-fidelity rapid-reactivity-accident claim. Any M15 claim must remain bounded by the spatial/neutronic fidelity actually implemented by M14.

## 8. Known-model-limit statements reinforced by this review

The following limitations are now explicitly source-informed:

- no industrial coordinated unit-load / direct-energy-balance controller is currently claimed;
- no qualified 5→10 MWe ramp envelope exists until P4 passes;
- ideal steam-drum phase separation omits detailed carryover/carry-under/separator performance;
- current reference operating points are not automatically solved full-plant fixed points;
- global point kinetics is not space-time/full-core kinetics;
- configured feedback coefficients are reduced-order surrogates, not plant-certified RBMK spectral coefficient fields;
- current grid model is an educational infinite-bus/reduced coupling model, not a complete power-system stability/EMT model.

## 9. Explicit non-imports

This review does **not** authorize copying:

- CCGT/coal ramp-rate percentages or minimum stable load values into the 10 MWe reference plant;
- fossil-boiler DEB equations or firing-control constants directly into the nuclear plant;
- PWR/CANDU steam-generator circulation-ratio, pressure, separator or heat-transfer values into the RBMK-like loop;
- fast-reactor sodium/lead/gas coolant coefficients, pump strategies, enrichments or accident thresholds;
- Hébert example cross sections, lattice constants or generic kinetic data as RBMK calibration;
- industrial protection/reliability claims;
- full-space-time or licensing-grade neutron-transport claims.

## 10. Governance rule

A source-derived idea becomes implementation work only after it has:

1. a canonical project owner;
2. an explicit milestone/branch home;
3. a versioning/compatibility decision;
4. predeclared acceptance evidence;
5. change-impact/revalidation scope.

Until then it remains engineering review input. P1A stays executable and unchanged, and P2R remains the next decision point after P1A evidence is returned.


## 11. Detailed-section re-review — Review 2 disposition

The previously selected “study deeply” sections have now been re-read and traced individually. The full source-by-source analysis is in [`research/PRE_M11_DEEP_ENGINEERING_SECTION_REVIEW_2.md`](research/PRE_M11_DEEP_ENGINEERING_SECTION_REVIEW_2.md); the compact requirement map is [`research/PRE_M11_DEEP_REVIEW_TRACEABILITY_2.md`](research/PRE_M11_DEEP_REVIEW_TRACEABILITY_2.md).

Review 2 leaves the main Review 1 conclusions intact but sharpens five areas:

- **coordinated demand is multi-stage:** requested load, rate/limit-conditioned effective load, controller/actuator demand, physical actuator position and observed plant response must not be collapsed into one value;
- **separator idealization affects circulation as well as steam quality:** omitted carry-under can change return density/driving head, while separator pressure loss belongs to total loop loss;
- **flexibility requires headroom and sustainability evidence:** fast frequency support, stable load range, ramp capability, inventory recovery and part-load efficiency are distinct claims;
- **thermal storage is also a dynamic filter:** a nearly settled governor/frequency state can coexist with slower steam/thermal inventory motion; rod worth is likewise a spatially dependent future-fidelity issue;
- **M14 needs a declared reduction hierarchy:** point kinetics remains global, lattice/homogenization/full-core steps have different owners, and any coarse spatial reduction should declare and verify the reaction/leakage/power quantities it is intended to preserve.

Two interpretive corrections are explicitly frozen. First, generalized perturbation theory in Hébert is a specific adjoint/functional sensitivity framework and must not be conflated with the hydraulic Jacobian machinery used elsewhere in this project. Second, the PWR/CANDU steam-generator literature is analogical for the RBMK-like direct cycle: its head/loss/separation principles are useful, while its geometry, circulation ratios, pressures and separator numerical limits are not project targets.

### Review 2 impact on active execution

None. **P1A remains frozen** and the next decision remains P2R. Review 2 only makes the P2R evidence reading more disciplined by adding demand/effective-reference/actuator state, steam pressure-grade decomposition, headroom and stored-energy recovery to the existing owner-chain interpretation.


## Todreas/Kazimi two-volume extension — Deep Review Pass 1 before Plan Amendment 2

The engineering review set now includes two additional thermal-hydraulic references: Todreas/Kazimi Volume I and Todreas/Kazimi/Massoud Volume II. Their first detailed pass is intentionally sequenced after P1A but before any Plan Amendment 2 freeze.

The strongest immediate consequence is diagnostic rather than implementational: after P1A demonstrated 6 MWe load reachability but not complete stationarity, the next planning step should prioritize canonical conserved-inventory rates, hydraulic boundary/head-loss compatibility, branch/group redistribution, steam-path state and controller memory. Current HEM-like fidelity must remain explicit; missing slip, nonequilibrium-boiling, density-wave, ONB/NVG and two-fluid physics may not be inferred from aggregate variables.

The detailed source/traceability record is in:

- `research/PRE_M11_TODREAS_KAZIMI_THERMAL_HYDRAULIC_DEEP_REVIEW_PASS1.md`;
- `research/PRE_M11_TODREAS_KAZIMI_DEEP_REVIEW_TRACEABILITY_PASS1.md`.

No P3 branch is selected by this review. A second Todreas/Kazimi deep pass is required after Plan Amendment 2 is authored.
