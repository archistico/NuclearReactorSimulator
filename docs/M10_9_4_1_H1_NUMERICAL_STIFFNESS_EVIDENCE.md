# M10.9.4.1-H.1 — Fixed-Step Timestep Sensitivity & Stiffness Evidence

> **Status:** VALIDATED. Phase G is complete. H.1 remains evidence-only and activates no new integration method; H.2 owns the evidence-derived method decision.

## Purpose

Phase G closes the open-control-volume energy convention across the current-v2 plant. Phase H can therefore measure numerical behavior without mixing integration error with a known energy-accounting inconsistency.

H.1 answers four questions before any adaptive/substep or semi-implicit implementation is accepted:

1. how sensitive the validated current-v2 operating point is to fixed-step refinement;
2. where the largest per-step fractional mass, internal-energy and compressed-liquid pressure changes occur;
3. how strongly the raw primary hydraulic flows change from one explicit step to the next;
4. what wall-clock cost the same amount of deterministic simulated time has at finer fixed steps.

H.1 did **not** decide the final numerical method. The validated evidence is now consumed by H.2, which selects deterministic semi-implicit pressure/flow coupling for staged prototype and activation gates.

## Fixed-step sweep

The production current-v2 runtime remains exactly:

```text
10.0 ms fixed timestep
```

The H.1 audit-only seam additionally creates the same versioned desktop operating point at:

```text
10.0 ms
 5.0 ms
 2.5 ms
```

All three use the same **20 ms deterministic seed-preconditioning duration**. The number of hidden version-owned seed commits therefore becomes 2, 4 and 8 respectively; public logical STEP 0 remains STEP 0.

The evidence factory is internal to `NuclearReactorSimulator.Application` and visible only to Application tests. No UI or scenario authoring API can change the production timestep.

## Evidence recorded

For each fixed step the audit runs the same five simulated seconds and records:

- final steam-drum pressure and level;
- final condenser pressure;
- final turbine rotor speed;
- final generator frequency and signed grid exchange;
- final effective turbine-stage flow;
- final and min/max total primary pump/channel/return flows;
- maximum absolute one-step change in those three primary-flow totals;
- largest fractional fluid-node mass change per step and owning node id;
- largest fractional fluid-node internal-energy change per step and owning node id;
- largest fractional subcooled-liquid pressure change per step and owning node id;
- existing plant mass/energy closure residuals;
- wall seconds per simulated second and average microseconds per logical step.

The convergence report then compares the 10→5 ms and 5→2.5 ms final-state differences for pressure, level, rotor/frequency and representative mass flows. Where both differences are non-zero it reports the observed refinement order:

```text
p = log2(|x10 - x5| / |x5 - x2.5|)
```

A negative or non-converging result is evidence to act on; H.1 must not hide it by retuning physical coefficients.

## Non-goals

H.1 does not introduce:

- adaptive runtime timestep;
- wall-clock-dependent adaptation;
- automatic stiffness detection that changes physics during a run;
- substep count changes based on UI cadence;
- semi-implicit pressure/flow coupling;
- nonlinear state repair;
- controller/governor retuning;
- protection-threshold changes;
- new physical damping;
- new flow filters.

The current 0.5 s primary operator-display lag remains presentation-only and is not considered a numerical cure.

## H.2 decision

After H.1 validation and evidence review, H.2 must choose one of these outcomes explicitly:

1. retain the production 10 ms explicit step if refinement evidence shows it is adequate;
2. introduce bounded deterministic substeps if refinement converges at acceptable bounded cost;
3. design a semi-implicit pressure/flow treatment if explicit refinement remains stiff or impractically expensive.

Any future substep count must depend only on deterministic simulation state/definitions, never on measured wall-clock performance.
