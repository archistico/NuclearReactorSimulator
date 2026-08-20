# Known model limitations

This register contains **current** limitations only. Resolved investigations and milestone chronology belong under `history/`, milestone records and ADRs.

## Thermodynamics and fluid mechanics

- Water/steam properties are reduced-order educational correlations, not an industrial IAPWS/steam-table implementation across the full operating envelope.
- Fluid nodes and most components are zero-dimensional lumped control volumes.
- Pipe/valve hydraulics use reduced resistance laws; general distributed pressure loss, elevation/static head, acoustic waves and water hammer are not modeled.
- General wet-steam/two-phase critical-flow and choking fidelity remains limited to explicitly implemented reduced-order paths.
- Cavitation/NPSH, detailed non-condensable gases and full circulating-water dynamics are not modeled.
- Drum swell/shrink and detailed separator carryover/carryunder are not modeled at industrial fidelity.

## Validated reference drift / inventory redistribution

Validated I.3 is continuously healthy over 300 s on the authoritative exact-v3 production path, but the final 60 s are **not a claim of asymptotic steady state**. The frozen regression observations include:

- drum inventory slope: **+8.2451672984622224 kg/s**;
- main-steam-header inventory slope: **-0.35293086123580603 kg/s**;
- total fluid internal-energy slope: **-2.061802762164879 MW**.

These values are a regression baseline, not a claim of asymptotic steady state and not calibration targets. They indicate continuing reduced-order inventory/energy redistribution over the reference window. Future work may investigate whether a longer horizon, improved initialization/trim, or a separately validated physical model should reduce those drifts; production physics must not be tuned merely to make the slopes approach zero.

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

- H.30 RQ1 promotes exact-v3 corrected-commit because exact-v2 explicit was shown to suffer targeted steam-train reverse-flow/shaft-drop discontinuities during otherwise healthy operation.
- The corrected path remains materially more expensive than explicit. H.28 classifies it `bounded-but-costly`; H.30 RQ1 accepts that cost because continuity evidence changed the production trade-off.
- `DeterministicHybridSemiImplicit` and `FourNodeBranchContinuityShadowIntegrated` are **source-retained historical modes**. They are not production choices, not exact-version compatibility requirements and not current-CI dependencies. I.4 defers physical deletion because four source files and four test files per mode still preserve executable historical seams.

## Plant completeness

Still simplified or omitted:

- regenerative feed heating/deaeration/moisture-separator-reheater detail;
- complete emergency core cooling/residual heat removal;
- detailed ventilation/fire/suppression systems;
- detailed structural/mechanical failure progression;
- complete severe-accident chemistry and containment behaviour.

## HMI / operator model

- The control room is educational rather than a one-to-one reproduction of a specific historical plant.
- Some operator-facing values are intentionally filtered/presented differently from raw solver diagnostics.
- Numerical diagnostics remain engineering evidence and are not automatically exposed as operator controls or predictions.

## Severe incidents

Faults, leaks/LOCA-class scenarios, blackout-class scenarios, trips and post-incident analysis exist within the currently modeled physics. The simulator must not be described as a general severe-accident, fire or explosion simulator until explicit persistent-damage owners and validated consequence models exist.

## Validation interpretation

A green regression gate means the current reduced-order contract is internally consistent for the tested domain. It does not imply industrial accuracy outside that domain.
