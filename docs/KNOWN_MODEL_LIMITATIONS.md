# Known model limitations

This register contains **current** limitations only. Historical limitations and resolved milestone investigations are retained under `history/` and milestone/ADR records instead of being repeated here.

## Thermodynamics and fluid mechanics

- Water/steam properties are reduced-order educational correlations, not an industrial IAPWS/steam-table implementation across the full operating envelope.
- Fluid nodes and most components are zero-dimensional lumped control volumes.
- Pipe/valve hydraulics use reduced resistance laws; general distributed pressure loss, elevation/static head, acoustic waves and water hammer are not modeled.
- General wet-steam/two-phase critical-flow and choking fidelity remains limited to explicitly implemented reduced-order paths.
- Cavitation/NPSH, detailed non-condensable gases and full circulating-water dynamics are not modeled.
- Drum swell/shrink and detailed separator carryover/carryunder are not modeled at industrial fidelity.

## Reactor physics

- The core model is reduced-order rather than a full 3D neutronic/thermal-hydraulic solver.
- Point kinetics and quasi-spatial/group behaviour are educational approximations; they are not a licensing transient-analysis model.
- Xenon, feedback and decay-heat behaviour are deterministic reduced-order models with configured coefficients rather than plant-certified data.
- No detailed fuel failure, channel rupture propagation, graphite damage or severe core-damage mechanics are currently authoritative.

## Turbine / electrical system

- Turbine expansion, losses and valve capacity are reduced-order thermodynamic/mechanical models.
- The grid is an educational infinite-bus/reduced coupling model, not a full electromagnetic transient or multi-machine load-flow solver.
- Electrical protection is reduced-order supervised/delayed logic, not impedance/differential/EMT relay simulation.

## Numerical coupling

- H.30 RQ1 candidate promotes the exact-v3 four-node corrected-commit path because exact-v2 explicit was shown to suffer targeted steam-train reverse-flow/shaft-drop discontinuities during otherwise healthy operation.
- The corrected path remains materially more expensive than explicit. H.28 classifies it `bounded-but-costly`; the performance cost is accepted by the H.30 RQ1 candidate because continuity evidence materially changes the production trade-off.
- H.5 hybrid and H.21 shadow-integrated modes remain source-retained historical audit dependencies. They are not current production choices and are not yet safe to delete.

## Plant completeness

The simulator does not yet model every real secondary/support system. Examples still simplified or omitted include:

- regenerative feed heating/deaeration/moisture-separator-reheater detail;
- complete emergency core cooling/residual heat removal;
- detailed ventilation/fire/suppression systems;
- detailed structural/mechanical failure progression;
- complete severe-accident chemistry and containment behaviour.

## HMI / operator model

- The control room is designed for educational usability rather than one-to-one reproduction of a specific historical control room.
- Some operator-facing values are intentionally filtered/presented differently from raw solver diagnostics.
- Numerical diagnostics remain engineering evidence and are not automatically exposed as operator controls or predictions.

## Severe incidents

Faults, leaks/LOCA-class scenarios, blackout-class scenarios, trips and post-incident analysis exist within the physics currently modeled. The simulator must **not** be described as a general severe-accident, fire or explosion simulator until explicit persistent-damage owners and validated consequence models exist.

## Validation interpretation

A green regression gate means the current reduced-order contract is internally consistent for the tested domain. It does not imply industrial accuracy outside that domain.
