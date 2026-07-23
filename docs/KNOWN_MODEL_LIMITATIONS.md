# Known Model Limitations Register

## Purpose

This register distinguishes deliberate educational simplifications, unresolved defects/hypotheses and future fidelity work. An entry here is not permission to change production behavior without an isolated milestone, regression and validation gate.

## A. Active investigation in M10.9.4.1

### A1. Sustained-generation seed equilibrium is unproved

The current seed is manually parameterized and deterministic, but the 300-second audit has already shown a protection trip near 70 seconds. It must not be described as a proven steady state until final-window slopes and full inventory trajectories are within an accepted equilibrium budget.

### A2. Condenser limiter ownership and long-horizon headroom

The condenser uses inventory, heat-transfer/cooling-capacity and maximum-flow bounds. A.2 adds one-second stage/actual/inventory/thermal/capacity/surface-limit and exhaust-mass evidence. The candidate also raises only current-v2 installed cooling capacity and maximum flow while preserving the initial `UA * ΔT` point. Full active-limiter margins and hotwell phase-change closure remain unresolved.

### A3. Pressure outside the intended design envelope is not an explicit node diagnostic

Compressed-liquid resolution may return finite pressures above the intended plant operating envelope. Mathematical resolvability and design-envelope validity are currently not separate snapshot semantics.

### A4. Turbine admission phase policy is incomplete

Current pressure-driven stage flow can remain positive for liquid or highly wet inlet states while current-v2 thermodynamic work can fall to zero. A shared admission-quality policy is not yet defined.

### A5. Generator/grid coupling is one-directional

Negative electromagnetic power/motoring is not represented by the current clamp. Reverse-power and complete synchronous-restoring behavior therefore cannot yet be modeled faithfully.

### A6. Drum low-inventory behavior is incomplete

Current recirculation demand, phase separation and low-liquid inventory require further closure. Low-drum-level protection is intentionally deferred until the physical owner is corrected.

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
