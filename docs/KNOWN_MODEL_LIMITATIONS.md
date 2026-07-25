# Known Model Limitations Register

## Purpose

This register distinguishes deliberate educational simplifications, unresolved defects/hypotheses and future fidelity work. An entry here is not permission to change production behavior without an isolated milestone, regression and validation gate.

## A. Active investigation in M10.9.4.1

### A1. Sustained-generation seed equilibrium remains a promotion gate

The historical current-v2 seed produced a protection trip near 70 seconds; that root cause was corrected by conservative solid-to-coolant heat return, matched primary circulation and aligned steam-path initial conditions. A corrected 300-second journey has passed previously, but the present cumulative D.3.2/E.2/operator-control candidate must re-run the complete trajectory. It must not be described as a proven steady state until current final-window slopes and full inventory trajectories are within the accepted equilibrium budget.

### A2. Condenser limiter ownership and long-horizon headroom

The condenser uses inventory, heat-transfer and maximum-flow bounds. C.1 is locally green with pressure-resolved saturated-liquid condensate energy. C.2 now separates 40 MW **installed** cooling capacity from runtime **available** capacity and the `UA·ΔT` surface limit; 20 kg/s remains an independent maximum condensation-flow ceiling. These validated values are retained as explicit current-v2 design ceilings rather than operating-point tuning constants. Detailed circulating-water dynamics, non-condensables and the later system-wide enthalpy/flow-work migration remain unresolved.

### A3. Pressure outside the intended design envelope is not an explicit node diagnostic

Compressed-liquid resolution may return finite pressures above the intended plant operating envelope. Mathematical resolvability and design-envelope validity are currently not separate snapshot semantics.

### A4. Turbine admission phase policy — D.1 LOCALLY VALIDATED

The inherited model allowed pressure-driven stage flow to remain positive for liquid or highly wet inlet states while thermodynamic work could fall to zero. M10.9.4.1-D.1 introduced an explicit current-v2 vapor-mass-fraction-limited admission policy: liquid is blocked from stage transfer and wet admission transfers only the vapor fraction without applying quality twice to specific work. D.3.2 additionally closes the discovered admission-train bypass: current-v2 pressure-driven stage flow is bounded by stop/control/admission valve capacity, so any closed valve enforces zero stage transfer. Detailed droplet transport, erosion, moisture separation and calibrated Stodola/effective-area behavior remain out of scope.

### A4b. Breaker-open rotor had no passive deceleration path — D.3.1 CANDIDATE

D.3 evidence showed a disconnected rotor fixed near 3301 rpm after the control valve closed because both generator electromagnetic torque and passive mechanical losses were zero. D.3.1 adds an optional speed-dependent rotor-loss law to sustained current-v2 profiles. The 0.5 MW rated-speed value and its interaction with low-load headroom remain candidate behavior until local long-running validation passes.

### A5. Bidirectional generator/grid coupling is candidate behavior

E.2 current-v2 profiles now represent signed generation and grid motoring through versioned bidirectional coupling and an internal signed rotor-torque seam. The focused migration audit is green, but the long-running signed-power/slip trajectories are not yet promoted. Dedicated reverse-power, supervised underfrequency and loss-of-synchronism protections remain absent and are gated to E.3.

### A6. Drum low-inventory behavior is only partially closed

M10.9.4.1-B.1 inventory-limits current-v2 demand-balanced liquid recirculation and prevents a fully vaporized drum from fabricating a liquid recirculation source. M10.9.4.1-B.2, now locally validated, replaces the temporary demand-following drum-to-main-steam supplement with an explicit current-v2 pressure/energy/inventory-driven source. B.3 adds explicit committed-liquid/separation diagnostics plus measured low-level warning and low-low drum-level protection. General node design-envelope pressure diagnostics remain tracked separately under A3.

### A7. Advective energy uses specific internal energy

Pipe/source transport currently follows the model's specific-internal-energy convention. Flow work/enthalpy transport is not explicit and requires a dedicated whole-network migration to avoid double counting with pumps and turbines.

### A8. Reference-plant scale migration is implemented but not promoted

E.1 accepts a 10 MWe current-v2 educational unit and E.2 coordinates nameplate, 1.5 rpm droop normalization, signed coupling and HMI range while retaining the 1,000 kg·m² rotor and 5 MWe normal point. Historical/default definitions remain unchanged. Focused evidence is green; long-running dynamics, protection supervision and versioned trajectory promotion remain open.

### A9. Legacy/current option combinations are not formally enumerated

Versioned compatibility paths are isolated through optional definitions, but the supported combination matrix and retirement policy remain undocumented.

## B. Deliberate current simplifications

- deterministic fixed external timestep with explicit committed-state integration;
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

The current-v2 sustained seed intentionally uses low primary hydraulic resistances to obtain the required circulation scale. With the current explicit 10 ms network integration and nearly incompressible liquid pressure response, raw algebraic pipe/pump flow diagnostics can alternate strongly from one solver step to the next even while long-horizon mass/energy balances and plant inventories remain bounded. This must not be interpreted as a real 100 Hz plant oscillation.

C.2 Hotfix 1 adds a deterministic 0.5 s instrumentation lag for the operator-facing current-v2 primary flow readouts. This is a presentation/measurement treatment only: the raw solver chatter is still tracked as numerical-hardening debt and requires the later timestep/stiffness/semi-implicit decision gate before it can be considered physically resolved.
