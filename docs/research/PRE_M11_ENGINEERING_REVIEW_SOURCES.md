# Pre-M11 Engineering Review Sources

## Status

**RESEARCH / PLANNING PROVENANCE — not validation evidence and not a licensing claim.**

This file records the book-length sources reviewed during the final M10 / pre-M11 planning window and the parallel P1A planning period, together with the project decisions retained from each source. The source books themselves are not bundled in project candidate ZIPs.

## 1. Nuclear-code development, V&V and application

**Jun Wang, Xin Li, Chris Allison, Judy Hohorst (eds.), _Nuclear Power Plant Design and Analysis Codes: Development, Validation, and Application_, Woodhead Publishing / Elsevier, 2021.**

Retained principles:

- distinguish verification, model assessment/validation evidence, integral system qualification and user acceptance;
- qualify phenomena and bounded domains explicitly rather than making a blanket “validated code” claim;
- separate single/separate-effect evidence from integral/whole-system evidence;
- treat multiphysics coupling, timestep, convergence and numerical conditioning as qualification subjects;
- use tiered unit/integration/system/long validation and non-regression evidence;
- compare against previously accepted evidence without blindly rerunning obsolete historical experiments;
- state model limitations and qualified ranges explicitly.

Project consequences:

- `PRE_M11_NUCLEAR_CODE_VV_REVIEW.md`;
- 27-row `eng/m10-final-vv-matrix.json`;
- curated final cumulative M10 gate;
- separate long M10 validation gate;
- `CHANGE_IMPACT_REVALIDATION_POLICY.md` and release-evidence planning.

Not imported:

- plant-specific PWR/LWR constants;
- professional/licensing-grade claims;
- wholesale replacement of the simulator’s reduced-order physics with TRACE/ATHLET/SAM/CFD-style models.

## 2. Digital I&C, software safety and human-system integration

**Committee on Application of Digital Instrumentation and Control Systems to Nuclear Power Plant Operations and Safety, _Digital Instrumentation and Control Systems in Nuclear Power Plants: Safety and Reliability Issues_, National Academy Press, 1997.**

Retained principles:

- explicit system-level allocation of control, protection, supervision and operator functions;
- protection/control separation and deterministic manual takeover;
- timing as part of control correctness while preserving the explicit non-claim that the desktop simulator is hard real-time;
- common-mode software failure and the distinction between duplication, design diversity and functional diversity;
- software assurance as more than test execution alone;
- human factors including data overload, keyhole effect, mode errors, workload imbalance, clumsy/opaque automation and situation awareness;
- deterministic treatment of stale/delayed/lost/inconsistent information as a useful future educational direction;
- proportional COTS/dependency assurance rather than nuclear-grade dedication of .NET/Avalonia.

Project consequences:

- `PRE_M11_DIGITAL_IC_HUMAN_SYSTEM_SAFETY_REVIEW.md`;
- `DIGITAL_IC_ARCHITECTURE_INVARIANTS.md`;
- `HUMAN_AUTOMATION_FUNCTION_ALLOCATION.md`;
- `DIGITAL_IC_HAZARD_CATALOG.md`;
- `HMI_CLASSIC_FAILURE_MODES_CHECKLIST.md`;
- M11 release-assurance work;
- M13.9 Digital I&C Degradation & Automation Transparency;
- deferred protection-diversity inventory before any independence/diversity claim.

Not imported:

- 1997 regulatory positions as current requirements;
- a distributed hard-real-time I&C implementation;
- invented quantitative software failure probabilities;
- cosmetic duplicate protection algorithms presented as independent systems.

## 3. Reactor physics, heat removal and operating-point self-consistency

**John R. Lamarsh and Anthony J. Baratta, _Introduction to Nuclear Engineering_, 3rd ed., Prentice Hall, 2001.**

Retained immediate principle:

- in coupled reactor/thermal problems, an operating point is meaningful only when interdependent quantities are mutually self-consistent; an assumed state must reproduce a compatible state after the coupled calculation rather than merely appear quiet for a short observation interval.

Project consequence already planned:

- `REFERENCE_OPERATING_POINT_EQUILIBRIUM_AND_STABILITY_PLAN.md`;
- `M10_LR_H1_EQUILIBRIUM_DIAGNOSTIC_PLAN.md`;
- M12.0 Reference Operating-Point Equilibrium & Stability Qualification;
- observational residual inspector first, bounded trimmer only if evidence requires it;
- exact-version immutability if a repaired production operating-point seed is eventually required.

Additional candidate topics were identified but are **not yet authorized implementation work**; see `LAMARSH_FOLLOW_UP_CANDIDATES.md`.

Not imported:

- reactor-type-specific numerical values or correlations merely because they appear in the textbook;
- PWR/BWR thermal limits as direct RBMK-like simulator limits;
- historical RBMK-specific behavior into the default reference reactor without an explicitly versioned historical model.


## 4. Coordinated plant control, turbine/governor and unit energy balance

**Swapan Basu and Ajay Kumar Debnath, _Power Plant Instrumentation and Control Handbook: A Guide to Thermal Power Plants_, 2nd ed., Academic Press / Elsevier, 2019.**

Retained principles:

- coordinated load control treats heat source/boiler, turbine and generator as one coupled unit;
- the turbine/governor side can respond faster than the heat source and can temporarily consume stored steam/thermal energy;
- a sustained new load requires restoration of the energy balance rather than only a valve or generator command;
- load demand should be rate-managed/coordinated to avoid bumps, overshoot and subsystem mismatch;
- stored-energy change matters under transient conditions even when steady-state energy input/output would balance.

Project consequences:

- `PRE_M11_PLANT_DYNAMICS_THERMAL_HYDRAULICS_REACTOR_PHYSICS_REVIEW.md`;
- P2R owner-chain interpretation and possible later P3-W manoeuvre qualification;
- explicit limitation that the current simulator does not claim an industrial coordinated unit-load/DEB implementation.

Not imported:

- fossil combustion/firing algorithms and constants;
- plant-size-specific ramp gradients;
- vendor-specific DEB controller tuning.

## 5. Power-plant flexibility and response-time decomposition

**Viorel Badescu, George Cristian Lazaroiu and Linda Barelli (eds.), _Power Engineering: Advances and Challenges — Part A: Thermal, Hydro and Nuclear Power_, CRC Press / Taylor & Francis, 2018.**

Retained principles:

- flexibility is multidimensional: ramp rate, minimum stable generation, frequency response, commitment time, idle capability and part-load behavior are distinct;
- turbine/governing valves provide fast response but do not by themselves create indefinitely sustainable thermal power;
- steam-cycle dynamics can limit whole-plant ramping and must be observed at adequate time resolution;
- transient stored-energy mechanisms and sustained heat-source response should not be conflated.

Project consequences:

- future P3-W/P4 qualification separates ramp capability, frequency response, headroom, settling and stable operating range;
- 10 ms logical-time evidence remains valuable for fast transient ownership while long-window evidence establishes final operating points.

Not imported:

- CCGT/coal numerical ramp rates, minimum loads or flexibility economics;
- technology-specific bypass/condensate tricks as nuclear reference-plant controls.

## 6. Nuclear plant control, protection and decay-heat engineering

**Anthony M. Judd, _An Introduction to the Engineering of Fast Nuclear Reactors_, Cambridge University Press, 2014.**

Retained principles:

- load-following can be understood as a fast turbine/frequency response coupled to a slower heat-source response through steam-system state;
- normal control, protection and post-trip heat removal are separate engineering functions;
- shutdown does not eliminate thermal power instantly; decay heat requires continuing heat removal;
- global plant measurements can hide local conditions, reinforcing conservative fidelity claims.

Project consequences:

- P2R examines grid/governor → steam → reactor-heat → shaft/output ownership in sequence;
- M12.5 retains full-plant post-trip decay-heat ownership as a distinct prerequisite;
- protection/trip owners remain separate rather than a single generic plant-trip flag.

Not imported:

- sodium/lead/gas coolant behavior, fast-reactor reactivity coefficients, enrichment or pump-control numbers;
- fast-reactor accident thresholds or steam-generator design values as RBMK-like parameters.

## 7. Nuclear steam generation, two-phase circulation and separation

**Jovica Riznic (ed.), _Steam Generators for Nuclear Power Plants_, Woodhead Publishing / Elsevier, 2017.**

Retained principles:

- sustained steam outflow is coupled to feedwater replacement and stored inventory;
- recirculation is an emergent balance between hydrostatic driving head and total circuit pressure loss;
- circulation/quality/separator loading change with operating state and power;
- carryover, carry-under and separator pressure loss are real phenomena that can affect steam quality and circulation;
- large water/steam inventories provide thermal storage and can contribute slow system dynamics.

Project consequences:

- M12.0 operating-point qualification includes mass/energy/inventory and canonical head/loss evidence, not only apparent quietness;
- `KNOWN_MODEL_LIMITATIONS.md` explicitly retains ideal separator/carryover/carry-under limitations;
- future fidelity work may add separator realism only under a dedicated versioned physical-model milestone.

Not imported:

- PWR/CANDU steam-generator circulation-ratio values, geometry, pressures, heat-transfer areas or separator numeric limits;
- U-tube/once-through plant architecture as if it were the RBMK-like direct steam cycle.

## 8. Modern reactor physics, point kinetics and spatial-fidelity limits

**Alain Hébert, _Applied Reactor Physics_, 3rd ed., Presses internationales Polytechnique, 2020.**

Retained principles:

- point kinetics is a reduced global-amplitude/precursor representation inside a broader transport/lattice/full-core/space-time physics hierarchy;
- delayed-neutron precursor dynamics are essential to normal controllability;
- Doppler broadening and material-specific thermal scattering show that simple temperature coefficients are reduced surrogates for spectral/material physics;
- water and graphite moderator physics require energy/material treatment in high-fidelity calculations;
- spatial reactor behavior, depletion and local histories require explicit spatial ownership rather than presentation-only mapping.

Project consequences:

- current M2 point kinetics remains `VERIFIED — reduced-order educational point kinetics`, not a spatial-transient validation claim;
- M14 must preserve an explicit global-point-kinetics versus quasi-spatial/space-time fidelity boundary;
- current linear feedback coefficients remain declared reduced-order calibration laws;
- future independent analytical/numerical point-kinetics reference cases are useful V&V candidates.

Not imported:

- example cross sections, lattice constants, generic kinetic parameters or benchmark geometries as RBMK calibration;
- a claim that the current quasi-spatial model is full-core diffusion/transport or licensing transient analysis.


## 9. Reactor thermal-hydraulic fundamentals, two-phase models and heated-channel stability

**Neil E. Todreas and Mujid S. Kazimi, _Nuclear Systems, Volume I: Thermal Hydraulic Fundamentals_, 2nd ed., revised printing, CRC Press / Taylor & Francis, 2015.**

Retained principles:

- homogeneous-equilibrium flow assumes equal phasic velocity and thermal equilibrium; slip and nonequilibrium require additional closure;
- two-phase pressure change should be reasoned about through acceleration, friction, gravity and local/form-loss contributions;
- a heated channel can admit multiple flow roots under pressure boundary conditions and a root may be dynamically unstable;
- density-wave and other time-domain behavior must be distinguished from numerical instability;
- ONB/NVG/saturated boiling, quality, void and dryout/DNB are distinct concepts and should not be collapsed into one generic phase-switch claim;
- a single-channel operating point is a coupled mass/momentum/energy/boundary-condition solution.

Project consequences:

- current water/steam/void physics is explicitly described as reduced HEM-like fidelity;
- post-P1A planning gives inventory/balance evidence priority over display-variable quietness;
- M12.0 separates stationarity/root closure from stability and branch qualification;
- M12/M14 own any later slip, nonequilibrium boiling or richer channel-fidelity work.

Not imported:

- BWR/PWR correlation coefficients, stability maps, CHF values, geometry or test-loop numbers;
- a claim that current aggregate oscillations are real density-wave instability;
- drift-flux/two-fluid/ONB/NVG physics inside M10.

## 10. Reactor thermal-hydraulic design, parallel channels, loop dynamics, scaling and model fidelity

**Neil E. Todreas, Mujid S. Kazimi and Mahmoud Massoud, _Nuclear Systems, Volume II: Elements of Thermal Hydraulic Design_, 2nd ed., CRC Press / Taylor & Francis, 2022.**

Retained principles:

- flow split among heated channels connected through common plena is part of the hydraulic solution and must satisfy shared-boundary pressure compatibility;
- scaled/reduced systems preserve selected phenomena and balances and necessarily accept distortions;
- transient model reductions resolve different bandwidths and can suppress fast compressible/acoustic behavior while retaining slower system response;
- loop analysis can use reduced system models for operational/control trends while detailed local analysis remains a different fidelity problem;
- lumped control-volume mass/energy/momentum storage and transfer are first-class transient quantities;
- multi-equation/two-fluid formulations form a fidelity ladder rather than a mandatory endpoint;
- local Jacobian/sensitivity analysis is a local approximation and does not prove global plant behavior.

Project consequences:

- the 10 MWe plant remains a reduced-order RBMK-like educational reference, not a dynamically scaled RBMK prototype;
- Plan Amendment 2 candidate observables emphasize conserved inventories, transfer residuals, canonical hydraulic compatibility and controller memory;
- future M14 equivalent-channel flow split must state which quantities the reduction preserves;
- M12 low-flow/natural-circulation/reversal work remains a separately authorized physical milestone.

Not imported:

- BWR/PWR/LMR geometry, pump curves, scaling ratios, similarity targets or component-specific numbers;
- a new PWR loop architecture in the direct-cycle reference plant;
- 6/7-equation two-fluid physics as an M10 repair.

## Todreas/Kazimi Deep Review Pass 1 coverage (24 August 2026)

Detailed review before Plan Amendment 2 covers Volume I Ch.5, Ch.11, Ch.13 and Ch.14, and Volume II Ch.1, Ch.2, Ch.3, Ch.4, Ch.7, Ch.12, Ch.13 and Appendix L. See [`PRE_M11_TODREAS_KAZIMI_THERMAL_HYDRAULIC_DEEP_REVIEW_PASS1.md`](PRE_M11_TODREAS_KAZIMI_THERMAL_HYDRAULIC_DEEP_REVIEW_PASS1.md) and [`PRE_M11_TODREAS_KAZIMI_DEEP_REVIEW_TRACEABILITY_PASS1.md`](PRE_M11_TODREAS_KAZIMI_DEEP_REVIEW_TRACEABILITY_PASS1.md).

This pass is intentionally **pre-amendment**. A second deep review is mandatory after Plan Amendment 2 is authored and before its new executable diagnostic contract is treated as literature-audited.

## Governing use rule

These sources are engineering inputs. Project code changes still require an explicit owner, scope, acceptance criterion, revalidation impact and compatibility decision. A book-derived idea is never sufficient reason by itself to change a physical coefficient, safety threshold, archive identity or V&V tolerance.



## Detailed-section Review 2 coverage (24 August 2026)

Review 2 does not add a sixth source. It revisits the five sources in §§4–8 above at the exact section level previously marked for deeper study. Detailed conclusions and transferability are recorded in [`PRE_M11_DEEP_ENGINEERING_SECTION_REVIEW_2.md`](PRE_M11_DEEP_ENGINEERING_SECTION_REVIEW_2.md) and [`PRE_M11_DEEP_REVIEW_TRACEABILITY_2.md`](PRE_M11_DEEP_REVIEW_TRACEABILITY_2.md).

### Basu / Debnath — sections actually re-read

- Ch.10 coordinated control, including mode selection, demand limiting/ramping/hold, boiler/turbine balance and heat release;
- Ch.9 §2 EHG speed/load/pressure-control tasks and valve demand;
- Ch.8 steam-pressure/load-index control for process coupling context;
- valve-actuator position demand/feedback behavior;
- supervisory turbine-control layering;
- Ch.14 alarm-management material only for generic HMI/alarm principles.

**Refinement:** industrial coordinated control is not just a common setpoint; it explicitly shapes/limits demand and can hold a direction when process deviations are excessive. This becomes a future project control/transparency requirement, not an M10 controller transplant.

### Riznic — sections actually re-read

- Ch.2 circulation/feedwater design;
- Ch.4 thermal-hydraulics, two-phase circulation and steam-water separation;
- Ch.12 thermal-performance degradation/pressure-drop attribution;
- Ch.1.4/Ch.5 architecture comparisons;
- Ch.13 only as deferred structural-vibration context.

**Refinement:** carry-under and separator pressure loss couple separation performance back into circulation. The source’s PWR/CANDU/WWER geometry and numerical circulation/separator values remain explicitly non-transferable to the RBMK-like direct cycle.

### Badescu / Lazaroiu / Barelli — sections actually re-read

- flexibility dimensions: ramp rate, MSG, frequency response, commitment/idle capability and part-load efficiency;
- spinning headroom and response limits;
- steam/turbine fast-response versus finite stored-energy behavior.

**Refinement:** future load qualification must treat headroom and sustainable support as explicit dimensions; no CCGT/coal percentage is accepted as a reference-plant limit.

### Judd — sections actually re-read

- Ch.4.4 normal/abnormal control;
- Ch.4.3 steam-plant thermal-storage context;
- Ch.5.2 protection/decay-heat-removal principles;
- Ch.1.5–1.6 control rods/reactivity coefficients;
- Ch.3.2 heat-transfer/transport constraints.

**Refinement:** water/metal thermal mass acts as a dynamic filter as well as stored energy; control-rod worth is spatially nonlinear and state-dependent. Fast-reactor-specific coolant and coefficient values remain non-transferable.

### Hébert — sections actually re-read

- §2.6 Doppler broadening, thermal motion and binding effects;
- Ch.4 lattice workflow, self-shielding, homogenization/condensation and SPH equivalence;
- §4.5 isotopic depletion;
- Ch.5 full-core methods;
- §5.3 GPT;
- §5.4 point/space-time kinetics and temporal schemes.

**Refinement:** M14 spatial fidelity is a hierarchy, not a mesh-size claim. Any coarse reduction should state what reference reaction/leakage/power quantities it preserves. GPT is kept as a future reactor-physics sensitivity method and is not equated with existing hydraulic Jacobians.


### Post-Plan-Amendment-2 Deep Review Pass 2

The Todreas/Kazimi Volume I and Todreas/Kazimi/Massoud Volume II sources were revisited after Plan Amendment 2 was locally validated. This pass audits the amendment rather than adding source-derived model equations. Retained sections are chiefly Volume I Chapters 5, 11, 13 and 14 and Volume II Chapters 1–4, 7, 12–13 and Appendix L. The resulting disposition is `PASS-AS-AUTHORED` with explicit claim/fidelity safeguards recorded in `PRE_M11_TODREAS_KAZIMI_THERMAL_HYDRAULIC_DEEP_REVIEW_PASS2.md`. No textbook coefficient, correlation or prototype time constant is imported into M10.
