# Known Model Limitations Register

## Purpose

This register distinguishes deliberate educational simplifications, unresolved defects/hypotheses and future fidelity work. An entry here is not permission to change production behavior without an isolated milestone, regression and validation gate.

## A. Active investigation in M10.9.4.1

### A1. Sustained-generation seed equilibrium is unproved

The current seed is manually parameterized and deterministic, but the 300-second audit has already shown a protection trip near 70 seconds. It must not be described as a proven steady state until final-window slopes and full inventory trajectories are within an accepted equilibrium budget.

### A2. Condenser limiter ownership and long-horizon headroom

The condenser uses inventory, heat-transfer and maximum-flow bounds. C.1 is locally green with pressure-resolved saturated-liquid condensate energy. C.2 now separates 40 MW **installed** cooling capacity from runtime **available** capacity and the `UA·ΔT` surface limit; 20 kg/s remains an independent maximum condensation-flow ceiling. These validated values are retained as explicit current-v2 design ceilings rather than operating-point tuning constants. Detailed circulating-water dynamics, non-condensables and the later system-wide enthalpy/flow-work migration remain unresolved.

### A3. Pressure outside the intended design envelope is not an explicit node diagnostic

Compressed-liquid resolution may return finite pressures above the intended plant operating envelope. Mathematical resolvability and design-envelope validity are currently not separate snapshot semantics.

### A4. Turbine admission phase policy — D.1 CANDIDATE

The inherited model allowed pressure-driven stage flow to remain positive for liquid or highly wet inlet states while thermodynamic work could fall to zero. M10.9.4.1-D.1 introduces an explicit current-v2 vapor-mass-fraction-limited admission policy: liquid is blocked from stage transfer and wet admission transfers only the vapor fraction without applying quality twice to specific work. This remains candidate behavior until local build/journey validation passes. Detailed droplet transport, erosion and moisture-separation physics remain out of scope.

### A5. Generator/grid coupling is one-directional

Negative electromagnetic power/motoring is not represented by the current clamp. Reverse-power and complete synchronous-restoring behavior therefore cannot yet be modeled faithfully.

### A6. Drum low-inventory behavior is only partially closed

M10.9.4.1-B.1 inventory-limits current-v2 demand-balanced liquid recirculation and prevents a fully vaporized drum from fabricating a liquid recirculation source. M10.9.4.1-B.2, now locally validated, replaces the temporary demand-following drum-to-main-steam supplement with an explicit current-v2 pressure/energy/inventory-driven source. B.3 adds explicit committed-liquid/separation diagnostics plus measured low-level warning and low-low drum-level protection. General node design-envelope pressure diagnostics remain tracked separately under A3.

### A7. Advective energy uses specific internal energy

Pipe/source transport currently follows the model's specific-internal-energy convention. Flow work/enthalpy transport is not explicit and requires a dedicated whole-network migration to avoid double counting with pumps and turbines.

### A8. Reference-plant scale is not yet coherent

Generator nameplate, rotor inertia and low-load turbine/condenser capacities require the decision recorded in `REFERENCE_PLANT_SCALE_CONTRACT.md`.

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
