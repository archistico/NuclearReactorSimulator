# Known model limitations

This register contains **current** limitations only. Resolved investigations and milestone chronology belong under `history/`, milestone records and ADRs.

## Thermodynamics and fluid mechanics

- Water/steam properties remain reduced-order educational correlations, not an industrial IAPWS/steam-table implementation across the full operating envelope.
- Authoritative desktop exact `@4` uses `CorrelationConsistentInverseDomain`. Its superheated branch is anchored to the same correlated saturated-vapor boundary used by the saturated branch, and its saturated inverse search is interval-aware around the water-density maximum. The validated repair census resolved the three observed historical no-root states, 7/7 two-sided seam probes and 231/231 low-temperature probes; the repaired 7000-step load journey, 30,000-interval cross-profile run, replay/protection/off-design matrix and performance/soak gates are green. The former vapor seam gap/overlap and 4–8 °C inverse-search blind spot are therefore **resolved for authoritative exact @4 within the validated domain**, not current production blockers.
- Historical exact desktop `@2` and `@3` deliberately retain `HistoricalCorrelationTopology` for exact-version replay compatibility. Their old gap/overlap and low-temperature inverse-search behavior is historical compatibility semantics, not the authoritative production thermodynamic closure. Old saves/scenarios must not be silently reinterpreted through @4.
- `CorrelationConsistentInverseDomain` is still a simplified closure. Matching the phase boundary removes an internal inverse-map inconsistency; it does not make the model a complete high-fidelity steam-table implementation, nor does it validate arbitrary metastable/supercritical states outside the declared envelope. Genuinely unsupported states continue to fail closed.
- Fluid nodes and most components are zero-dimensional lumped control volumes.
- Pipe/valve hydraulics use reduced resistance laws; general distributed pressure loss, elevation/static head, acoustic waves and water hammer are not modeled.
- General wet-steam/two-phase critical-flow and choking fidelity remains limited to explicitly implemented reduced-order paths.
- Cavitation/NPSH, detailed non-condensable gases and full circulating-water dynamics are not modeled.
- Drum swell/shrink and detailed separator carryover/carryunder are not modeled at industrial fidelity.

## Validated reference drift / inventory redistribution

Historical validated I.3 was continuously healthy over 300 s on the then-authoritative exact-v3 production path, but the final 60 s are **not a claim of asymptotic steady state**. Exact @3 is now frozen provenance; exact @4 is current authoritative production and has separately passed the final repaired-v4 300 s reference requalification against all 19 unchanged I.3 budgets rather than inheriting exact-v3 evidence. The frozen regression observations include:

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

- Historical H.30 RQ1 promoted exact desktop `@3` corrected-commit because exact `@2` explicit showed targeted steam-train reverse-flow/shaft-drop discontinuities. Exact `@3` remains immutable replay provenance. Authoritative desktop production is now exact `@4`, which preserves corrected-commit ownership while using the repaired thermodynamic closure.
- `pre-synchronization-grid-loading@2` is retained as an exact explicit compatibility/reference identity, but it is not the supported sustained low-load synchronization journey: the I.5 long diagnostic reproduced late rotor/export instability and reverse-admission steps. Synchronization exact `@3` preserves the @2 physical/control/grid seed and uses corrected-commit for the supported sustained journey. This synchronization version family is independent from desktop exact `@4`.
- Historical H.28 remains `bounded-but-costly` provenance for the old closure. Repaired Stage 4 compares repaired explicit vs repaired corrected on the same thermodynamic closure and passed the original relative ceilings with median wall ratio `0.97677554038185532`, p95 ratio `0.83442338580147768` and allocation ratio `1.1163446388589435`; the validated repaired classification is `bounded-at-or-below-explicit` on that machine-local benchmark.
- Repaired Stage 2 observed zero branch overrides but 3,720 previous-phase holds across 58 trigger steps, including 50 post-startup continuity-active steps. Branch override machinery may now be topologically obsolete, but previous-phase hysteresis remains materially exercised and must not be removed merely because the vapor seam became single-root. Any retirement requires separately scoped evidence after Phase-I closure.
- `DeterministicHybridSemiImplicit` and `FourNodeBranchContinuityShadowIntegrated` are **source-retained historical modes**. They are not production choices, not exact-version compatibility requirements and not current-CI dependencies. I.4 defers physical deletion because historical executable seams still preserve provenance.

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
