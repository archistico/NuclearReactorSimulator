# ADR index

This index makes the architectural-decision collection navigable. `PROJECT.md` remains the only current validation/candidate source; ADRs explain decisions and provenance, not the active build checkpoint.

## Status vocabulary

The **Normalized status** column uses only `Proposed`, `Accepted`, `Superseded`, or `Superseded by ADR-NNNN`. Historical ADR bodies retain their original narrative status prose; the index normalizes it for navigation without rewriting provenance.

Every ADR file exposes a `## Status` heading. ADRs that predated the explicit status convention are marked accepted as legacy/foundational decisions unless their body records a later supersession.

## Current governing pointers by area

This short list is a discovery aid rather than a substitute for `ARCHITECTURE.md`:

- **Layering / determinism:** ADR 0001, 0002, 0005, 0006, 0008.
- **Plant/network ownership:** ADR 0025, 0027, 0028, 0179, 0180.
- **Hydraulic/thermodynamic correction policy:** ADR 0159, 0183.
- **Scenario / replay / persistence:** ADR 0053, 0068, 0176, 0181, 0184.
- **Mission / operator experience:** ADR 0177, 0178, 0182.
- **Desktop host / save integrity planning:** ADR 0185.
- **Documentation governance:** ADR 0186.

## Full ADR ledger

| ADR | Title | Normalized status | Area |
| --- | --- | --- | --- |
| [0001](0001-clean-layered-architecture.md) | Clean layered architecture | Accepted | General architecture |
| [0002](0002-deterministic-fixed-timestep.md) | Deterministic fixed-timestep simulation | Accepted | General architecture |
| [0003](0003-physics-emergent-gameplay.md) | Physics-emergent gameplay | Accepted | General architecture |
| [0004](0004-avalonia-presentation-boundary.md) | Avalonia presentation boundary | Accepted | HMI / operator experience |
| [0005](0005-runtime-command-and-snapshot-boundaries.md) | Runtime command scheduling and snapshot boundaries | Accepted | General architecture |
| [0006](0006-transactional-step-fault-semantics.md) | Transactional step commit and terminal runtime fault semantics | Accepted | Protection / faults / incidents |
| [0007](0007-logical-command-traces-and-deterministic-replay.md) | Logical command traces and deterministic replay | Accepted | Recording / replay / persistence |
| [0008](0008-strongly-typed-si-physical-quantities.md) | Strongly typed SI physical quantities | Accepted | General architecture |
| [0009](0009-fluid-node-conservation-and-thermodynamic-closure.md) | Separate fluid-node conservation from thermodynamic closure | Accepted | Fluids / thermo / secondary |
| [0010](0010-passive-pipe-flow-and-conservative-transport.md) | Passive pipe flow and conservative transport | Accepted | Fluids / thermo / secondary |
| [0011](0011-valves-modulate-existing-passive-flow.md) | Valves modulate the existing passive-flow model | Accepted | Fluids / thermo / secondary |
| [0012](0012-active-pumps-compose-with-hydraulic-network.md) | Active pumps compose with the existing hydraulic network | Accepted | Fluids / thermo / secondary |
| [0013](0013-conservative-lumped-heat-transfer.md) | Conservative lumped heat transfer with explicit thermal inventories | Accepted | Validation / performance |
| [0014](0014-simplified-water-steam-closure-behind-thermodynamic-seam.md) | Simplified water/steam closure behind the thermodynamic seam | Accepted | Fluids / thermo / secondary |
| [0015](0015-reactivity-is-compositional-input-to-neutron-kinetics.md) | Reactivity is a compositional input to neutron kinetics | Accepted | Reactor physics |
| [0016](0016-control-rod-mechanics-precede-neutron-response.md) | Control-rod mechanics precede neutron response | Accepted | Reactor physics |
| [0017](0017-point-kinetics-is-the-dynamic-reactivity-boundary.md) | Point kinetics is the dynamic reactivity boundary | Accepted | Reactor physics |
| [0018](0018-neutron-population-drives-explicit-fission-thermal-power.md) | Neutron population drives explicit fission thermal power | Accepted | Validation / performance |
| [0019](0019-decay-heat-is-stateful-latent-energy.md) | Decay heat is a stateful latent-energy subsystem | Accepted | Reactor physics |
| [0020](0020-temperature-feedback-uses-committed-state.md) | Temperature feedback uses committed-state measurements | Accepted | Reactor physics |
| [0021](0021-void-quality-and-void-fraction-remain-distinct.md) | Vapor quality and void fraction remain distinct | Accepted | Reactor physics |
| [0022](0022-iodine-xenon-is-stateful-poison-inventory.md) | Iodine/Xenon Is a Stateful Poison Inventory | Accepted | Reactor physics |
| [0023](0023-staged-plant-network-orchestration.md) | Staged plant network orchestration uses a common committed state | Accepted | General architecture |
| [0024](0024-plant-topology-is-canonical-and-complete.md) | Plant topology is canonical and complete before orchestration | Accepted | General architecture |
| [0025](0025-network-components-produce-balances-before-single-integration.md) | ADR 0025: Network components produce balances before single integration | Accepted | Reactor physics |
| [0026](0026-global-kinetics-with-configurable-core-zone-projection.md) | ADR 0026: Keep point kinetics global while core zones provide configurable spatial projection | Accepted | Reactor physics |
| [0027](0027-fuel-channel-groups-compose-canonical-plant-components.md) | Fuel-channel groups compose canonical plant components | Accepted | Reactor physics |
| [0028](0028-main-circulation-composes-canonical-network-components.md) | Main circulation composes canonical network components | Accepted | Validation / performance |
| [0029](0029-steam-drum-separation-is-staged-internal-transfer.md) | Steam-drum separation is a staged internal transfer | Accepted | Fluids / thermo / secondary |
| [0030](0030-primary-circuit-external-boundaries-declare-signed-mass-and-energy.md) | Primary-circuit external boundaries declare signed mass and energy | Accepted | Validation / performance |
| [0031](0031-integrated-primary-circuit-preserves-single-committed-state-integration.md) | Integrated primary circuit preserves single committed-state integration | Accepted | Validation / performance |
| [0032](0032-main-steam-network-reuses-canonical-network-and-replaceable-turbine-boundary.md) | Main steam transport reuses the canonical plant network and terminates at a replaceable turbine boundary | Accepted | Fluids / thermo / secondary |
| [0033](0033-turbine-expansion-uses-explicit-mechanical-state-and-conservative-fluid-transfer.md) | Turbine expansion uses explicit mechanical state and conservative fluid transfer | Accepted | Fluids / thermo / secondary |
| [0034](0034-condenser-condensation-is-a-conservative-internal-transfer-with-explicit-heat-rejection.md) | Condenser condensation is a conservative internal transfer with explicit heat rejection | Accepted | Fluids / thermo / secondary |
| [0035](0035-condensate-feedwater-return-reuses-canonical-pumps-and-zeroes-legacy-feedwater-source.md) | Condensate/feedwater return reuses canonical pumps and zeroes the legacy feedwater source | Accepted | Fluids / thermo / secondary |
| [0036](0036-generator-grid-coupling-uses-explicit-electrical-state-and-rotor-load-seam.md) | Generator/grid coupling uses explicit electrical state and the existing rotor-load seam | Accepted | Electrical / grid |
| [0037](0037-secondary-cycle-heat-balance-is-an-audit-layer-over-existing-state-ownership.md) | Secondary-cycle heat balance is an audit layer over existing state ownership | Accepted | Validation / performance |
| [0038](0038-full-plant-steady-state-gate-wraps-existing-state-owners-without-new-physics.md) | Full-plant steady-state gate wraps existing state owners without new physics | Accepted | General architecture |
| [0039](0039-measured-signals-are-separate-from-full-plant-true-state.md) | Measured signals are separate from full-plant true state | Accepted | General architecture |
| [0040](0040-controllers-consume-measured-signals-and-actuators-remain-command-seams.md) | Controllers consume measured signals and actuators remain command seams | Accepted | Control / automation |
| [0041](0041-reactor-primary-control-reuses-m2-neutronics-and-canonical-pump-state.md) | Reactor/primary control reuses M2 neutronics and canonical pump state | Accepted | Fluids / thermo / secondary |
| [0042](0042-secondary-cycle-controls-use-canonical-valve-pump-owners.md) | Secondary-cycle controls use canonical valve/pump owners and the existing turbine-flow seam | Accepted | Fluids / thermo / secondary |
| [0043](0043-protection-overrides-normal-control-through-canonical-command-seams.md) | Protection overrides normal control through canonical command seams | Accepted | Protection / faults / incidents |
| [0044](0044-alarm-annunciator-memory-is-observational-and-separate-from-protection.md) | Alarm/annunciator memory is observational and separate from protection | Accepted | Protection / faults / incidents |
| [0045](0045-integrated-automatic-operation-preserves-committed-measurement-ordering.md) | Integrated automatic operation preserves committed-measurement ordering | Accepted | General architecture |
| [0046](0046-control-room-ui-consumes-presentation-snapshots-and-dispatches-application-commands.md) | Control-room UI consumes presentation snapshots and dispatches application commands | Accepted | HMI / operator experience |
| [0047](0047-control-room-components-use-semantic-presentation-states.md) | Control-room components use semantic presentation states | Accepted | HMI / operator experience |
| [0048](0048-reactor-core-panel-separates-measured-instruments-from-model-diagnostics.md) | Reactor/Core panel separates measured instruments from model diagnostics | Accepted | Reactor physics |
| [0049](0049-primary-circuit-mnemonics-preserve-measurement-and-topology-ownership.md) | Primary-circuit mnemonics preserve measurement and topology ownership | Accepted | Validation / performance |
| [0050](0050-turbine-electrical-panels-observe-canonical-owners-and-dispatch-typed-intents.md) | Turbine/electrical panels observe canonical owners and dispatch typed intents | Accepted | Electrical / grid |
| [0051](0051-operational-history-uses-logical-step-and-alarm-sequence.md) | Operational history uses logical step and alarm sequence | Accepted | Protection / faults / incidents |
| [0052](0052-runtime-coordinator-separates-simulation-stepping-from-presentation-publication.md) | Runtime coordinator separates simulation stepping from presentation publication | Accepted | HMI / operator experience |
| [0053](0053-versioned-initial-conditions-own-session-reconstruction.md) | Versioned initial conditions own deterministic session reconstruction | Accepted | Recording / replay / persistence |
| [0054](0054-cold-shutdown-recipe-and-prestartup-guidance-remain-layered.md) | Cold-shutdown recipe and pre-start guidance remain layered | Accepted | Scenarios / challenge / training |
| [0055](0055-first-criticality-uses-versioned-source-range-seed-and-observational-guidance.md) | First criticality uses a versioned source-range seed and observational guidance | Accepted | Scenarios / challenge / training |
| [0056](0056-turbine-startup-lineup-remains-versioned-and-governing-control-uses-existing-seam.md) | Turbine startup lineup remains versioned and governing control uses the existing seam | Accepted | Fluids / thermo / secondary |
| [0057](0057-grid-synchronization-and-load-requests-use-canonical-electrical-ownership.md) | Grid synchronization and load requests use canonical electrical ownership | Accepted | Electrical / grid |
| [0058](0058-power-manoeuvring-and-normal-shutdown-compose-existing-owners.md) | Power manoeuvring and normal shutdown compose existing owners | Accepted | General architecture |
| [0059](0059-training-evaluation-is-observational-deterministic-application-state.md) | Training evaluation is deterministic observational Application state | Accepted | Scenarios / challenge / training |
| [0060](0060-fault-injection-is-explicit-logical-step-scenario-state.md) | Fault injection is explicit deterministic scenario state | Accepted | Scenarios / challenge / training |
| [0061](0061-hydraulic-faults-constrain-canonical-components-and-use-network-source-terms.md) | Hydraulic faults constrain canonical components and use the single network source-term boundary | Accepted | Protection / faults / incidents |
| [0062](0062-instrumentation-control-faults-reuse-measured-signal-and-command-input-seams.md) | Instrumentation/control faults reuse measured-signal and canonical command-input seams | Accepted | Protection / faults / incidents |
| [0063](0063-secondary-transients-perturb-canonical-inputs-and-protection-seams.md) | Secondary-system transients perturb canonical inputs and protection seams | Accepted | Protection / faults / incidents |
| [0064](0064-educational-loca-breaks-are-bounded-conservative-source-terms.md) | Educational LOCA-class breaks are bounded conservative source terms | Accepted | Protection / faults / incidents |
| [0065](0065-electrical-loss-constrains-grid-connection-and-composes-explicit-powered-load-losses.md) | Electrical loss constrains the canonical grid connection and composes explicit powered-load losses | Accepted | Electrical / grid |
| [0066](0066-safety-response-evaluation-is-observational-and-reuses-existing-faults.md) | Safety-response evaluation is observational and reuses existing faults | Accepted | Protection / faults / incidents |
| [0067](0067-checkpoints-are-versioned-replay-anchors-not-opaque-state-dumps.md) | Checkpoints are versioned replay anchors, not opaque state dumps | Accepted | Recording / replay / persistence |
| [0068](0068-post-incident-analysis-is-evidence-based-not-causal-inference.md) | Post-incident analysis is evidence-based, not causal inference | Accepted | Recording / replay / persistence |
| [0069](0069-m93-xenon-promotion-is-opt-in-through-versioned-runtime-state.md) | M9.3 Xenon Promotion Is Opt-In Through Versioned Runtime State | Accepted | Reactor physics |
| [0070](0070-operator-computer-aggregates-existing-owners-and-supervisory-automation-remains-m5-owned.md) | Operator computer aggregates existing owners; supervisory automation remains M5-owned | Accepted | HMI / operator experience |
| [0071](0071-m94-quasi-spatial-refinement-preserves-global-point-kinetics.md) | M9.4 Quasi-Spatial Refinement Preserves Global Point Kinetics | Accepted | Reactor physics |
| [0072](0072-historical-inspired-scenarios-require-explicit-provenance-and-fidelity-gates.md) | Historical-inspired scenarios require explicit provenance and fidelity gates | Accepted | Scenarios / challenge / training |
| [0073](0073-m96-reference-validation-is-versioned-provenance-explicit-and-fail-closed.md) | M9.6 reference validation is versioned, provenance-explicit and fail-closed | Accepted | Validation / performance |
| [0074](0074-supervisory-automation-is-m5-owned-and-authority-intents-are-replayed-separately.md) | Supervisory automation is M5-owned and authority intents are replayed separately | Accepted | Recording / replay / persistence |
| [0075](0075-operator-hmi-separates-range-semantics-and-performance-time-is-logical.md) | Operator HMI separates range semantics and performance time is logical | Accepted | HMI / operator experience |
| [0076](0076-advanced-gauges-render-published-semantics-and-trends-use-logical-steps.md) | Advanced gauges render published semantics and trends use logical steps | Accepted | General architecture |
| [0077](0077-whole-plant-mimic-is-application-owned-presentation-topology.md) | Whole-plant mimic is Application-owned presentation topology | Accepted | HMI / operator experience |
| [0078](0078-subsystem-schematics-and-explicit-gameplay-acceptance.md) | Subsystem schematics remain presentation topology; long gameplay acceptance is explicit | Accepted | HMI / operator experience |
| [0079](0079-generation-ready-seeds-are-versioned-and-effective-steam-flow-is-presentation-only.md) | Generation-ready operating seeds are versioned; effective turbine steam flow is presentation-only | Accepted | HMI / operator experience |
| [0080](0080-turbine-expansion-is-a-pressure-driven-hydraulic-element.md) | Turbine expansion is a pressure-driven hydraulic element | Accepted | Fluids / thermo / secondary |
| [0081](0081-legacy-replay-compatibility-does-not-constrain-current-model-correctness.md) | Legacy replay compatibility does not constrain current-model correctness | Accepted | Recording / replay / persistence |
| [0082](0082-steam-drum-liquid-recirculation-closes-on-circulation-demand.md) | Steam-drum liquid recirculation closes on circulation demand | Accepted | Scenarios / challenge / training |
| [0083](0083-current-main-steam-demand-closes-drum-supply.md) | Current main-steam demand closes drum supply | Superseded by ADR-0094 | Scenarios / challenge / training |
| [0084](0084-condenser-heat-rejection-is-ua-delta-t-bounded-by-cooling-capacity.md) | Condenser heat rejection is UA·ΔT bounded by cooling capacity | Accepted | Fluids / thermo / secondary |
| [0085](0085-generator-grid-synchronous-phase-frequency-stiffness.md) | Paralleled generators use explicit phase/frequency grid stiffness | Accepted | Electrical / grid |
| [0086](0086-secondary-pump-discharge-check-valves-are-opt-in-hydraulic-topology.md) | Secondary-pump discharge check valves are opt-in hydraulic topology | Accepted | Fluids / thermo / secondary |
| [0087](0087-current-v2-secondary-protections-are-measured-latching-and-operationally-supervised.md) | Current-v2 secondary protections are measured, latching and operationally supervised | Accepted | Protection / faults / incidents |
| [0088](0088-current-v2-secondary-actuators-have-explicit-travel-ramp-dynamics.md) | Current-v2 secondary actuators have explicit travel/ramp dynamics | Accepted | Control / automation |
| [0089](0089-current-v2-governor-switches-from-speed-reference-to-grid-load-droop.md) | Current-v2 governor switches from speed reference to grid-load droop | Accepted | Electrical / grid |
| [0090](0090-current-v2-turbine-work-is-pressure-temperature-and-vapor-dependent.md) | Current-v2 turbine work is pressure-, temperature- and vapor-dependent | Accepted | Fluids / thermo / secondary |
| [0091](0091-current-v2-condenser-preserves-ua-design-point-with-installed-capacity-headroom.md) | Current-v2 condenser preserves the UA design point with installed-capacity headroom | Accepted | Fluids / thermo / secondary |
| [0092](0092-current-v2-sustained-generation-seed-closes-solid-to-coolant-heat-and-primary-circulation.md) | Current-v2 sustained-generation seed closes solid-to-coolant heat transfer and primary circulation | Accepted | Validation / performance |
| [0093](0093-current-v2-steam-drum-liquid-recirculation-is-limited-by-separable-liquid-inventory.md) | Current-v2 steam-drum liquid recirculation is limited by separable liquid inventory | Accepted | Fluids / thermo / secondary |
| [0094](0094-current-v2-drum-steam-source-is-pressure-energy-and-inventory-driven.md) | Current-v2 drum steam source is pressure-, energy- and inventory-driven | Accepted | Fluids / thermo / secondary |
| [0095](0095-current-v2-drum-low-inventory-diagnostics-and-low-low-level-protection.md) | Current-v2 drum low-inventory diagnostics and low-low-level protection | Accepted | Protection / faults / incidents |
| [0096](0096-current-v2-condenser-condensate-energy-is-pressure-resolved-saturated-liquid.md) | Current-v2 condenser condensate energy is pressure-resolved saturated liquid | Accepted | Fluids / thermo / secondary |
| [0097](0097-current-v2-condenser-installed-capacity-is-definition-owned.md) | Current-v2 condenser installed capacity is definition-owned | Accepted | Fluids / thermo / secondary |
| [0098](0098-current-v2-primary-operational-flow-filtering-and-resynchronization-guidance.md) | Current-v2 primary operational-flow filtering and re-synchronization guidance | Proposed | Scenarios / challenge / training |
| [0099](0099-current-v2-turbine-admission-is-vapor-mass-fraction-limited.md) | Current-v2 turbine admission is vapor-mass-fraction limited | Proposed | HMI / operator experience |
| [0100](0100-turbine-admission-authority-is-measured-before-retuning.md) | Turbine admission authority is measured before retuning | Proposed | HMI / operator experience |
| [0101](0101-governor-effective-setpoint-and-actuator-tracking-are-audited-before-new-anti-windup.md) | Governor effective setpoint and actuator tracking are audited before new anti-windup | Accepted | Control / automation |
| [0102](0102-current-v2-breaker-open-rotor-has-passive-mechanical-losses.md) | Current-v2 breaker-open rotor has passive mechanical losses | Proposed | Electrical / grid |
| [0103](0103-current-v2-pressure-driven-stage-flow-is-bounded-by-the-admission-train.md) | Current-v2 pressure-driven stage flow is bounded by the admission train | Proposed | HMI / operator experience |
| [0104](0104-loaded-desktop-admission-bias-is-realigned-after-train-isolation.md) | Loaded desktop admission bias is realigned after train isolation | Superseded by ADR-0105 | HMI / operator experience |
| [0105](0105-loaded-desktop-stop-valve-pressure-grade-is-rebalanced-after-local-bottleneck-evidence.md) | Loaded desktop stop-valve pressure grade is rebalanced after local bottleneck evidence | Proposed | Protection / faults / incidents |
| [0106](0106-loaded-desktop-main-steam-capacity-is-rebalanced-after-upstream-bottleneck-evidence.md) | Loaded desktop main-steam capacity is rebalanced after upstream-bottleneck evidence | Proposed | Fluids / thermo / secondary |
| [0107](0107-generation-ready-condenser-cooling-is-capacity-not-forced-inventory-depletion.md) | Generation-ready condenser cooling is capacity, not forced inventory depletion | Accepted | Fluids / thermo / secondary |
| [0108](0108-governor-actuator-tracking-is-measured-before-anti-windup-retuning.md) | Governor/actuator tracking is measured before anti-windup retuning | Proposed | Control / automation |
| [0109](0109-reference-plant-scale-target-is-a-10-mwe-educational-unit.md) | Reference plant scale target is a 10 MWe educational unit | Accepted | General architecture |
| [0110](0110-current-v2-reference-plant-is-10-mwe-with-bidirectional-grid-coupling.md) | Current-v2 reference plant is 10 MWe with bidirectional grid coupling | Accepted | Electrical / grid |
| [0111](0111-bidirectional-grid-motoring-uses-an-internal-signed-rotor-torque-seam.md) | Bidirectional grid motoring uses an internal signed rotor-torque seam | Accepted | Electrical / grid |
| [0112](0112-turbine-stop-valve-travel-rate-is-owned-by-the-admission-train.md) | Turbine stop-valve travel rate is owned by the admission train | Accepted | HMI / operator experience |
| [0113](0113-electrical-protection-thresholds-are-derived-from-signed-current-v2-trajectories.md) | Electrical protection thresholds are derived from signed current-v2 trajectories | Accepted | Protection / faults / incidents |
| [0114](0114-evidence-derived-electrical-protection-uses-supervised-delayed-m5-functions.md) | Evidence-derived electrical protection uses supervised delayed M5.5 functions | Accepted | Protection / faults / incidents |
| [0115](0115-choked-steam-flow-is-an-isolated-one-way-capacity-seam-before-relief-bypass-topology.md) | Choked steam flow is an isolated one-way capacity seam before relief/bypass topology | Accepted | Fluids / thermo / secondary |
| [0116](0116-main-steam-header-relief-is-a-pressure-actuated-external-boundary.md) | Main-steam header relief is a pressure-actuated external boundary | Accepted | Fluids / thermo / secondary |
| [0117](0117-turbine-bypass-is-an-internal-header-to-condenser-transfer.md) | Turbine bypass is an internal header-to-condenser transfer | Accepted | Fluids / thermo / secondary |
| [0118](0118-open-control-volume-advection-uses-enthalpy-and-separates-shaft-work.md) | Open-control-volume advection uses enthalpy and keeps shaft work separate | Accepted | General architecture |
| [0119](0119-current-v2-passive-hydraulics-advect-enthalpy-while-pump-paths-remain-versioned.md) | Current-v2 passive hydraulics advect enthalpy while pump paths remain versioned | Accepted | Fluids / thermo / secondary |
| [0120](0120-current-v2-remaining-non-turbine-paths-advect-enthalpy.md) | Current-v2 remaining non-turbine paths advect enthalpy with explicit work and boundary ownership | Accepted | Fluids / thermo / secondary |
| [0121](0121-accident-progression-is-causal-persistent-and-separate-from-alarm-priority.md) | Accident progression is causal, persistent and separate from alarm priority | Accepted | Protection / faults / incidents |
| [0122](0122-reference-core-will-evolve-to-a-reduced-spatial-2d-educational-model.md) | The reference core will evolve to a reduced spatial 2D educational model | Accepted | Reactor physics |
| [0123](0123-control-room-retains-area-workspaces-and-gains-industrial-controls-persistent-mimic-layout-and-instructor-mode.md) | Control room retains area workspaces and gains industrial controls, persistent mimic layout and Instructor mode | Accepted | HMI / operator experience |
| [0124](0124-current-v2-turbine-expansion-advects-enthalpy-and-keeps-shaft-work-single-owned.md) | Current-v2 turbine expansion advects enthalpy and keeps shaft work single-owned | Accepted | Fluids / thermo / secondary |
| [0125](0125-fixed-step-refinement-precedes-numerical-method-change.md) | Fixed-step refinement evidence precedes any numerical-method change | Accepted | General architecture |
| [0126](0126-h1-evidence-selects-deterministic-semi-implicit-pressure-flow-coupling.md) | H.1 evidence selects deterministic semi-implicit pressure/flow coupling | Accepted | Validation / performance |
| [0127](0127-isolate-semi-implicit-pressure-flow-prototype-before-production-activation.md) | Isolate semi-implicit pressure/flow prototype before production activation | Accepted | Reactor physics |
| [0128](0128-hybrid-semi-implicit-production-activation-requires-deterministic-bounded-work-gate.md) | Hybrid semi-implicit production activation requires a deterministic bounded-work gate | Accepted | Reactor physics |
| [0129](0129-activate-h4-selected-hybrid-hydraulics-only-in-versioned-current-v2-production.md) | Activate H.4-selected hybrid hydraulics only in versioned current-v2 production | Superseded | Fluids / thermo / secondary |
| [0130](0130-free-running-hybrid-activation-requires-extended-shadow-qualification.md) | Free-running hybrid activation requires extended shadow qualification | Accepted | Validation / performance |
| [0131](0131-refine-hybrid-corrector-with-bounded-two-tier-shadow-envelope-before-reactivation.md) | Refine the hybrid corrector with a bounded two-tier shadow envelope before reactivation | Accepted | General architecture |
| [0132](0132-revise-shadow-hydraulic-corrector-around-fixed-point-residual-and-deterministic-backtracking.md) | Revise the shadow hydraulic corrector around a fixed-point residual and deterministic backtracking | Accepted | Fluids / thermo / secondary |
| [0133](0133-use-safeguarded-anderson-before-jacobian-informed-hydraulic-root-solving.md) | Use safeguarded Anderson acceleration before Jacobian-informed hydraulic root solving | Accepted | Fluids / thermo / secondary |
| [0134](0134-use-conservative-coordinate-finite-difference-newton-before-diagnosing-map-nonsmoothness.md) | Use conservative-coordinate finite-difference Newton before diagnosing hydraulic-map non-smoothness | Accepted | Fluids / thermo / secondary |
| [0135](0135-diagnose-hydraulic-map-switching-before-further-nonlinear-solver-complexity.md) | Diagnose hydraulic-map switching before further nonlinear-solver complexity | Accepted | Fluids / thermo / secondary |
| [0136](0136-localize-thermodynamic-boundaries-before-active-set-formulation.md) | Localize thermodynamic boundaries before active-set formulation | Accepted | Protection / faults / incidents |
| [0137](0137-audit-inverse-thermodynamic-branch-selection-before-active-set.md) | Audit inverse thermodynamic branch selection before active-set formulation | Accepted | Fluids / thermo / secondary |
| [0138](0138-test-targeted-thermodynamic-branch-continuity-before-active-set.md) | Test targeted thermodynamic branch continuity before active-set reformulation | Accepted | Fluids / thermo / secondary |
| [0139](0139-broaden-bounded-thermodynamic-hysteresis-before-activation.md) | Broaden bounded thermodynamic hysteresis before activation | Accepted | Fluids / thermo / secondary |
| [0140](0140-diagnose-extended-trigger-723-before-changing-hysteresis-or-solver.md) | Diagnose Extended Trigger 723 Before Changing Hysteresis or Solver | Accepted | General architecture |
| [0141](0141-extend-bounded-branch-continuity-to-h15-localized-header-only.md) | Extend bounded branch continuity to the H.15-localized `header` only | Accepted | Protection / faults / incidents |
| [0142](0142-long-horizon-cross-profile-gate-before-production-activation.md) | Require long-horizon and cross-profile branch-continuity qualification before production activation | Accepted | Reactor physics |
| [0143](0143-stratify-long-horizon-trigger-episodes-before-newton-qualification.md) | Stratify long-horizon trigger episodes before Newton qualification | Accepted | Validation / performance |
| [0144](0144-split-turbine-inlet-branch-continuity-from-residual-floor-diagnosis.md) | Split turbine-inlet branch continuity from residual-floor diagnosis | Accepted | Fluids / thermo / secondary |
| [0145](0145-qualify-four-node-continuity-before-any-activation-design.md) | Qualify four-node continuity before any activation design | Accepted | General architecture |
| [0146](0146-define-fail-closed-four-node-activation-contract-before-production-wiring.md) | Define a fail-closed four-node activation contract before production wiring | Accepted | Reactor physics |
| [0147](0147-validate-orchestrator-sidecar-wiring-before-corrected-state-commit.md) | Validate orchestrator sidecar wiring before permitting corrected-state commitment | Accepted | General architecture |
| [0148](0148-introduce-opt-in-corrected-commit-seam-behind-h20-authority.md) | Introduce corrected-state ownership only behind unchanged H.20 authority | Accepted | Reactor physics |
| [0149](0149-qualify-corrected-commit-replay-and-protection-before-long-horizon-activation.md) | Qualify corrected-commit replay and protection before long-horizon activation | Accepted | Recording / replay / persistence |
| [0150](0150-qualify-committed-long-horizon-before-broad-protection-and-activation.md) | Qualify committed long-horizon operation before broader protection and activation work | Accepted | Protection / faults / incidents |
| [0151](0151-target-protection-transient-matrix-without-rerunning-long-horizon.md) | Target protection/transient qualification without rerunning the long-horizon gate | Accepted | Protection / faults / incidents |
| [0152](0152-stress-integrated-fail-closed-fallback-with-internal-only-authority-decision-hook.md) | Stress integrated fail-closed fallback with an internal-only authority-decision hook | Accepted | Validation / performance |
| [0153](0153-map-off-design-corrected-ownership-as-a-bounded-fail-closed-envelope.md) | Map off-design corrected ownership as a bounded fail-closed envelope | Accepted | General architecture |
| [0154](0154-attribute-corrected-path-cost-before-optimizing-h9.md) | Attribute corrected-path cost before optimizing H.9 | Accepted | Validation / performance |
| [0155](0155-remove-h9-probe-object-graph-churn-before-changing-newton-mathematics.md) | Remove H.9 probe object-graph churn before changing Newton mathematics | Accepted | General architecture |
| [0156](0156-reuse-historical-explicit-predictor-before-changing-trigger-contract.md) | Reuse the historical explicit predictor before changing the trigger contract | Accepted | Validation / performance |
| [0157](0157-reuse-exact-probe-state-and-fixed-saturation-grid-before-changing-finite-difference-newton.md) | Reuse exact probe state and fixed saturation grid before changing finite-difference Newton | Accepted | Electrical / grid |
| [0158](0158-version-h29-production-activation-candidate-with-v3-and-explicit-v2-kill.md) | Version the H.29 production activation candidate as v3 and preserve v2 as the explicit kill/rollback reference | Accepted | Reactor physics |
| [0159](0159-close-phase-h-opt-in-only-because-corrected-path-is-qualified-but-bounded-costly.md) | Close Phase H as OPT-IN ONLY because corrected ownership is qualified but bounded-costly | Accepted | Validation / performance |
| [0160](0160-inventory-exact-version-compatibility-before-retiring-legacy-audit-modes.md) | Inventory exact-version compatibility before retiring legacy audit modes | Accepted | Validation / performance |
| [0161](0161-tier-current-validation-and-freeze-historical-audits-before-legacy-retirement.md) | Tier current validation and freeze historical audits before legacy retirement | Accepted | Validation / performance |
| [0162](0162-establish-phase-i-reference-baseline-before-freezing-regression-budgets.md) | Establish the Phase-I 300-second reference baseline before freezing regression budgets | Accepted | Validation / performance |
| [0163](0163-activate-corrected-production-default-after-phase-i-continuity-evidence.md) | Activate corrected production default after Phase-I continuity evidence | Accepted | Protection / faults / incidents |
| [0164](0164-defer-historical-hydraulic-mode-source-removal-through-phase-i-closure.md) | Defer physical deletion of historical hydraulic numerical modes through M10.9.4.1 closure | Proposed | Fluids / thermo / secondary |
| [0165](0165-stage-correlation-consistent-water-steam-inverse-domain-repair-before-production-activation.md) | Stage correlation-consistent water/steam inverse-domain repair before production activation | Accepted | Fluids / thermo / secondary |
| [0166](0166-requalify-repaired-thermodynamic-closure-before-versioned-activation.md) | Requalify the repaired thermodynamic closure before versioned activation | Accepted | Fluids / thermo / secondary |
| [0167](0167-activate-repaired-exact-v4-with-historical-version-preservation.md) | Activate repaired exact-v4 production while preserving historical exact-version semantics | Accepted | Reactor physics |
| [0168](0168-author-contextual-command-consequences-without-predictive-ui-physics.md) | Author contextual command consequences without predictive UI physics | Accepted | General architecture |
| [0169](0169-project-command-dependency-chains-without-automatic-graph-traversal.md) | Project command dependency chains without automatic graph traversal | Accepted | General architecture |
| [0170](0170-integrate-command-context-with-canonical-mimic-without-changing-dispatch.md) | Integrate command context with the canonical mimic without changing dispatch | Accepted | HMI / operator experience |
| [0171](0171-observe-command-response-by-logical-step-without-inferring-causality.md) | Observe command response by logical step without inferring causality | Accepted | General architecture |
| [0172](0172-own-challenge-lifecycle-by-logical-evidence-without-plant-command-authority.md) | Own challenge lifecycle by logical evidence without plant-command authority | Accepted | Scenarios / challenge / training |
| [0173](0173-external-energy-demand-is-versioned-logical-step-evidence-not-generator-control.md) | External energy demand is versioned logical-step evidence, not generator control | Accepted | Scenarios / challenge / training |
| [0174](0174-score-operational-challenges-with-versioned-dominant-multidimensional-policies.md) | Score operational challenges with versioned dominant multidimensional policies | Accepted | Scenarios / challenge / training |
| [0175](0175-compose-initial-operational-challenges-from-existing-validated-evidence-owners.md) | Compose initial operational challenges from existing validated evidence owners | Accepted | Scenarios / challenge / training |
| [0176](0176-reconstruct-challenge-state-from-canonical-recordings-instead-of-persisting-opaque-state.md) | Reconstruct challenge state from canonical recordings instead of persisting opaque state | Accepted | Recording / replay / persistence |
| [0177](0177-project-mission-performance-as-read-only-aggregation-of-validated-owners.md) | Project mission/performance as read-only aggregation of validated owners | Accepted | HMI / operator experience |
| [0178](0178-place-mission-performance-as-dedicated-main-hmi-workspace.md) | Place Mission/Performance as a dedicated main-HMI workspace | Accepted | HMI / operator experience |
| [0179](0179-fail-closed-at-defaultable-domain-definition-boundaries.md) | Fail closed at defaultable Domain definition boundaries | Accepted | Protection / faults / incidents |
| [0180](0180-index-immutable-plant-topology-once-and-version-challenge-observation-changes.md) | Index immutable plant topology once and version challenge observation changes | Accepted | Scenarios / challenge / training |
| [0181](0181-preserve-session-v1-while-restoring-command-payload-integrity.md) | Preserve session schema v1 while restoring command payload integrity | Accepted | Recording / replay / persistence |
| [0182](0182-activate-mission-performance-with-explicit-pack-binding-and-structural-publication.md) | Activate Mission/Performance with explicit pack binding and structural publication | Accepted | HMI / operator experience |
| [0183](0183-defer-hydraulic-regularization-until-post-release-evidence.md) | Defer hydraulic constitutive regularization until a post-release evidence gate | Accepted | Fluids / thermo / secondary |
| [0184](0184-anchor-snapshot-fingerprint-v1-and-separate-mission-timeline-retention.md) | Anchor snapshot fingerprint v1 and separate mission lifecycle retention from recent evidence | Accepted | HMI / operator experience |
| [0185](0185-contain-desktop-runtime-failures-and-replace-session-archives-safely.md) | Contain desktop runtime failures and replace session archives safely before timeline expansion | Accepted | Recording / replay / persistence |
| [0186](0186-separate-stable-architecture-from-chronology-and-index-live-documentation.md) | Separate stable architecture from milestone chronology and index live documentation | Accepted | Documentation / governance |
| [0187](0187-project-mission-timeline-from-canonical-evidence-with-explicit-archive-pack-binding.md) | Project mission timeline from canonical evidence with explicit archive pack binding | Proposed | HMI / operator experience |
| [0188](0188-separate-grid-droop-proportional-and-integral-speed-references.md) | Separate grid-droop proportional and integral speed references | Accepted | Turbine / governor / grid |
| [0189](0189-own-rejected-wet-steam-admission-with-explicit-moisture-drain.md) | Own rejected wet-steam admission with an explicit moisture drain | Proposed | Fluids / thermo / secondary |

## Maintenance rule

- New ADRs use a `## Status` heading.
- Put the normalized state at the start of the status text (`Proposed`, `Accepted`, or `Superseded by ADR-NNNN`); put candidate/history nuance after it.
- When a decision is superseded, update both the ADR body and this index in the same documentation change.
- M11.5 owns automation that verifies ADR numbering, status headings and index coverage before release.
