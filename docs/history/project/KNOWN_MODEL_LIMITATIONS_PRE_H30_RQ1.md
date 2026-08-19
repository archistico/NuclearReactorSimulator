# Known Model Limitations Register

## Current numerical-hardening limitation — nonlinear hydraulic corrector

H.5 Hotfix 2 is validated with production current-v2 on explicit 10 ms coupling. The H.4-selected P060-F040-R015 corrector remains experimental: over 500 committed shadow intervals it triggered 7 corrections and converged on 5/7. H.6 then validated that the best bounded fixed-relaxation Picard rescue profile, R0125-I096, reaches only 6/7 and still leaves a large hard-event residual. Therefore semi-implicit production activation is not currently supported. H.7 revises the shadow corrector algorithm around an unrelaxed fixed-point residual and deterministic backtracking, without changing triggers or physical coefficients.

## Purpose

This register distinguishes deliberate educational simplifications, unresolved defects/hypotheses and future fidelity work. An entry here is not permission to change production behavior without an isolated milestone, regression and validation gate.

## A. Active investigation in M10.9.4.1

### A1. Sustained-generation seed equilibrium remains a promotion gate

The historical current-v2 seed produced a protection trip near 70 seconds; that root cause was corrected by conservative solid-to-coolant heat return, matched primary circulation and aligned steam-path initial conditions. A corrected 300-second journey has passed previously, but the cumulative D.3.2 Hotfix 3 + D.4 source has now re-run and passed the long gameplay and complete operational-envelope gates. The accepted equilibrium budgets remain the controlling evidence for future physics changes.

### A2. Condenser limiter ownership and long-horizon headroom

The condenser uses inventory, heat-transfer and maximum-flow bounds. C.1 is locally green with pressure-resolved saturated-liquid condensate energy. C.2 now separates 40 MW **installed** cooling capacity from runtime **available** capacity and the `UA·ΔT` surface limit; 20 kg/s remains an independent maximum condensation-flow ceiling. These validated values are retained as explicit current-v2 design ceilings rather than operating-point tuning constants. Detailed circulating-water dynamics and non-condensables remain unresolved. The system-wide current-v2 enthalpy/flow-work migration is complete through Phase G.

### A3. Pressure outside the intended design envelope is not an explicit node diagnostic

Compressed-liquid resolution may return finite pressures above the intended plant operating envelope. Mathematical resolvability and design-envelope validity are currently not separate snapshot semantics.

### A4. Turbine admission phase policy — D.1 LOCALLY VALIDATED

The inherited model allowed pressure-driven stage flow to remain positive for liquid or highly wet inlet states while thermodynamic work could fall to zero. M10.9.4.1-D.1 introduced an explicit current-v2 vapor-mass-fraction-limited admission policy: liquid is blocked from stage transfer and wet admission transfers only the vapor fraction without applying quality twice to specific work. D.3.2 additionally closes the discovered admission-train bypass: current-v2 pressure-driven stage flow is bounded by stop/control/admission valve capacity, so any closed valve enforces zero stage transfer. Detailed droplet transport, erosion, moisture separation and calibrated Stodola/effective-area behavior remain out of scope.

### A4b. Breaker-open rotor passive deceleration path — D.3.1 VALIDATED

D.3 evidence showed a disconnected rotor fixed near 3301 rpm after the control valve closed because both generator electromagnetic torque and passive mechanical losses were zero. D.3.1 adds an optional speed-dependent rotor-loss law to sustained current-v2 profiles. The 0.5 MW rated-speed value has passed the cumulative ordinary, long-running and operational-envelope gates; future scale migration must re-audit its interaction with the new nameplate.

### A5. Bidirectional coupling remains an infinite-bus educational model

The validated E.2 Hotfix 1 runtime supports signed generation/motoring exchange and an internal signed rotor-torque seam, but it remains a simplified infinite-bus coupling rather than a detailed synchronous-machine/network transient model. It does not model stator/field transients, reactances, excitation/AVR dynamics, bus topology or multi-machine load flow. E.3.2 now implements reduced-order reverse-power, breaker-supervised underfrequency and absolute-frequency-slip loss-of-synchronism protection as a validated current-v2 contract. It remains educational relay logic over the infinite-bus model, not impedance-based or electromagnetic transient protection.

### A6. Drum low-inventory behavior is only partially closed

M10.9.4.1-B.1 inventory-limits current-v2 demand-balanced liquid recirculation and prevents a fully vaporized drum from fabricating a liquid recirculation source. M10.9.4.1-B.2, now locally validated, replaces the temporary demand-following drum-to-main-steam supplement with an explicit current-v2 pressure/energy/inventory-driven source. B.3 adds explicit committed-liquid/separation diagnostics plus measured low-level warning and low-low drum-level protection. General node design-envelope pressure diagnostics remain tracked separately under A3.

### A7. Open-control-volume energy transport — RESOLVED FOR CURRENT-v2 / PHASE G COMPLETE

Phase G completed the staged current-v2 migration from historical `u*m_dot` advection to explicit `h*m_dot` open-control-volume transport while preserving node inventories as mass/internal energy and keeping pump hydraulic work, condenser heat rejection and turbine shaft work single-owned. Legacy profiles deliberately retain the historical convention. Remaining fidelity limits are property-model/two-phase/component-model limits, not an unresolved energy-ownership migration.

### A8. Reference-plant scale migration is accepted in principle but not implemented

E.2 Hotfix 1 validates the current-v2 10 MWe nameplate, 1.5 rpm governor normalization, signed coupling and HMI range while preserving historical/default definitions. The retained 0.5 MW synchronizing correction and 2 MW/Hz damping remain reduced-order calibrated values. E.3.1 recorded their normal, motoring and phase-offset trajectories; E.3.2 derives supervised delayed thresholds from that evidence.

### A9. Legacy/current profile retirement requires exact-version compatibility discipline

H.30 closes the Phase-H production-policy question, but historical scenario/save/replay identities and audit-only numerical modes must not be conflated. I.1 now provides the candidate executable compatibility/retirement inventory: exact-version profile identities remain loadable, while the old H.5 hybrid and H.21 shadow-integrated numerical modes are retirement candidates only after audit consolidation. No compatibility-retained exact version is safe to delete in I.1.


### A7. Main-steam relief fidelity — F.2 VALIDATED

F.2 adds one conservative current-v2 header-relief path to an atmospheric external boundary. It uses the validated F.1 ideal-vapor capacity equation, stateless pressure lift and committed vapor-quality limiting. It does not model certified safety-valve sizing, wet-steam/two-phase critical flow, valve hysteresis, blowdown, lift dynamics, discharge piping, receiver thermodynamics, acoustic loads or turbine bypass. Current-v2 relief energy export now uses the validated Phase G enthalpy convention; legacy definitions retain their historical convention. The remaining F.2 limitations are valve/critical-flow/discharge-system fidelity rather than unresolved flow-work ownership.

## B. Deliberate current simplifications

- deterministic fixed 10 ms external timestep; legacy/current-v1 remain explicit committed-state, while H.5 Hotfix 2 keeps current-v2 production explicit and H.7 revised deterministic correction remains shadow-only;
- lumped zero-dimensional fluid nodes and components;
- simplified water/steam property model rather than industrial steam tables;
- resistance-based pipe/valve flow without general elevation/static-head geometry;
- no general critical/choked-flow primitive yet;
- no NPSH/cavitation model;
- no non-condensable gas inventory in the condenser;
- cooling water represented as a boundary rather than a complete circulating-water system;
- no regenerative feed heating/deaerator/moisture-separator-reheater chain;
- no explicit drum swell/shrink model;
- no separate graphite thermal mass;
- no residual-heat-removal or emergency-core-cooling system.

## C. Presentation limitations

- a thermodynamic T-s/Mollier diagram is deferred until entropy/property support is authoritative;
- an energy Sankey can be built earlier from existing audited powers, but must label model boundaries and residuals;
- curve plots for pumps, valves and turbine stages require published canonical design/operating-point contracts rather than UI reconstruction.

## Review rule

Update this register whenever a limitation is corrected, superseded, accepted as deliberate scope or found to be an actual defect. The authoritative behavior remains the versioned code, ADRs and validated milestone records.


## Current-v2 primary instantaneous hydraulic chatter

The current-v2 sustained seed uses low primary hydraulic resistances to obtain the required circulation scale. H.1 confirmed that the historical explicit 10 ms integration can produce strong raw algebraic pipe/pump step-to-step chatter even while long-horizon mass/energy balances and plant inventories remain bounded; this must not be interpreted as a real 100 Hz plant oscillation. H.2 selected deterministic semi-implicit pressure/flow coupling, H.3 demonstrated strong numerical improvement, and H.4 validated a bounded selective profile (`P060-F040-R015`) with only 2/50 corrections and `activation-criteria-met=True`. H.5 Hotfix 1 production activation was rejected by ordinary validation; validated H.5 Hotfix 2 restored explicit production and found 7/500 shadow corrections with only 5/7 convergence. Validated H.6 then tested a bounded Picard rescue envelope without trigger changes: R0125-I096 improved the frozen set to 6/7 but did not qualify. H.7 therefore evaluates a separate fixed-point-residual/backtracking corrector while production remains explicit.

C.2 Hotfix 1's deterministic 0.5 s operator-facing flow instrumentation lag remains presentation-only. Do not claim the raw solver limitation closed until a later Phase H production numerical policy is explicitly activated and passes focused, ordinary, replay, long-running and operational-envelope validation gates.

### A8. Turbine-bypass fidelity — F.3 HOTFIX 1 VALIDATED

F.3 is a stateless pressure-actuated ideal-vapor bypass with linear opening, committed backpressure and committed vapor-quality limiting. It does not model actuator dynamics, hysteresis, manual/automatic control modes, desuperheating spray, discharge-pipe pressure loss, wet-steam critical flow, acoustic loads or a tightly coupled condenser solution. F.3 originally shipped on the historical internal-energy advection convention; the now-validated Phase G migration updates current-v2 bypass transport to enthalpy while preserving its internal-transfer ownership. Phase H still owns the timestep-stiffness decision for committed-state condenser sequencing.


### A9. Open-control-volume energy transport — PHASE G COMPLETE / G.4 VALIDATED

G.1 formalized the target enthalpy convention and quantified the gap. G.2 Hotfix 2 validated passive-pipe/valve enthalpy advection plus exact pump hydraulic/shaft ownership. G.3 validated every remaining non-turbine current-v2 owner with zero measured ownership residual. G.4 validated turbine expansion with single-owned shaft work, completing the staged current-v2 enthalpy migration. H.1 measured numerical stiffness without changing that ownership model; H.2–H.4 selected and validated a bounded hybrid numerical method. H.5 Hotfix 1 direct integration was rejected; H.5 Hotfix 2 validated the explicit-production rollback and extended shadow evidence. H.6 validated a negative 6/7 bounded Picard rescue result. H.7 remains shadow-only and does not alter the completed Phase G energy-ownership model.


## Approved future severe-incident direction

The project has approved a future accident-progression backlog, but the current simulator must not be described as a general severe-accident or fire/explosion simulator. Existing deterministic faults, leak/LOCA-class scenarios, blackout-class scenarios, trips and post-incident analysis remain bounded by the currently modeled physics.

Future persistent damage, fire, rupture/explosion mechanisms and severe core-damage progression require explicit physical owners, integrated decay-heat/thermal prerequisites, Phase-H extreme-state numerical evidence and replayable deterministic state. See `FUTURE_GAMEPLAY_CONTROL_ROOM_AND_ACCIDENT_DIRECTION.md` and ADR 0121.


## H.1–H.3 numerical-stiffness decision/prototype checkpoint

H.1 is validated. The production current-v2 runtime still uses a deterministic 10 ms fixed timestep. H.1 shows that 10→5→2.5 ms explicit refinement approximately doubles runtime cost at each halving while maximum final-state relative difference does not improve monotonically (`0.005401937` then `0.006028534`); raw primary hydraulic one-step changes remain large while conservation stays green.

H.2 is validated and selects deterministic semi-implicit pressure/flow coupling for staged implementation. H.3 Hotfix 1 demonstrated material chatter reduction but approximately 15.895x full-time isolated cost. H.4 validated selective correction. H.5 Hotfix 2 validated the production rollback and showed that the selected corrector still fails on 2/7 triggered intervals over 5 s. H.6 validated that bounded fixed-relaxation/iteration retuning recovers only one of those two events. H.7 now revises the corrector algorithm itself in shadow mode, using true fixed-point pressure/flow residuals and deterministic backtracking. Until a later production integration is explicitly validated, raw hydraulic stiffness remains a known model limitation; the existing 0.5 s operator-display lag remains presentation-only and is not considered a solver cure.
