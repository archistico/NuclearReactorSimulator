# M10 Final Replacement-Long Closure Plan 1 — P1B Slow-State Closure & Phenomenon-Owner Qualification

**Status: RETURNED EXECUTION PASS — frozen evidence for P2R2 Decision Re-entry 2.**

P1B is the only implementation authorized after Plan Amendment 2 and Todreas/Kazimi Deep Review Pass 2. It does not repair runtime physics and does not select P3.

## Question

When unchanged exact-v9 repeats the already-demonstrated 5→6 MWe trajectory, which **represented canonical state domain** remains materially dynamic in the final tail relative to a 5 MWe background reference?

## Frozen execution

- exact-v9 5 MWe background reference: **600 s**, no load command;
- exact-v9 5→6 MWe: identical clean initial condition, supervisory authority, thermal preparation and +1 MWe test-only generator-load semantics used by P1A;
- expected load-command logical step: **2785**;
- P1A checkpoint reproduction at **900 / 1800 / 3600 s**, fail-closed with the P1A tolerances;
- maximum hold: **3600 s after load**, no continuation;
- 1 s owner-state output is **slow-state downsampling only**;
- protection, hydraulic convergence/rollback/line-search and other available numerical sentinels are audited at canonical step/event cadence;
- late analysis: final **1200 s**, four contiguous **300 s** windows.

## Diagnostic trend rule

P1B v1 has **no scalar cross-domain owner score**. Raw values and raw slopes are emitted first. For each selected observable a load-specific persistent-trend indication is diagnostic-only and is frozen before execution:

`guard = max(10 × |5 MWe background slope|, 1e-12 × max(1, |5 MWe background mean|))` per second in that observable's own units.

A late trend is tagged persistent only when at least **3 of the 4** 300 s load windows exceed the guard in a common direction. This indication is not a physical acceptance threshold and cannot authorize P3.

The owner vocabulary remains:

`NEUTRONICS-THERMAL-SOURCE`, `PRIMARY-INVENTORY-HYDRAULIC`, `STEAM-PATH-TURBINE`, `CONTROL-ACTUATOR-MEMORY`, `ELECTROMECHANICAL-GRID`, `COUPLED-MULTI-DOMAIN`, `NO-MATERIAL-LATE-DRIFT`, `INCONCLUSIVE`.

If multiple represented domains show persistent load-specific motion, the automatic evidence label is conservatively `COUPLED-MULTI-DOMAIN`; P2R2 performs the causal interpretation using the raw inventory/transfer evidence.

## Evidence ordering

1. conserved inventories;
2. canonical transfers and conservation closure;
3. hydraulic compatibility and represented flow redistribution;
4. steam path;
5. controller memory and actuator demand/physical state;
6. turbine/rotor;
7. generator/grid.

A downstream slope is not root-cause proof while an upstream represented inventory or controller state remains dynamic.

## Fidelity boundary

P1B does not synthesize slip, independent phasic temperatures, ONB/NVG, new flow-regime classification, density-wave margin, natural-circulation/CHF correlations, two-fluid closure or prototype RBMK time constants.

## Exit

Execution PASS requires background/probe completion, all three P1A checkpoints, finite required observables, healthy protection/numerical sentinels and artifact emission. The engineering label may still be `INCONCLUSIVE` or `COUPLED-MULTI-DOMAIN`.

P1B has returned to **P2R2 Decision Re-entry 2** with engineering label `COUPLED-MULTI-DOMAIN`. P1B itself still authorizes neither P3 branch nor a second replacement-long baseline; branch authority belongs only to P2R2.
