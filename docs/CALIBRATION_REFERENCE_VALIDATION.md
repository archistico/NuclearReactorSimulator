# Calibration & Reference Validation

## Purpose

M9.6 adds a deterministic, versioned validation layer for comparing observable simulator behavior against explicit reference targets without creating a second physics owner or silently turning regression expectations into historical truth claims.

The framework separates three concerns:

1. **reference cases** — steady-state or transient observable targets at exact logical steps;
2. **tolerance budgets** — explicit absolute and/or relative error allowances attached to each target;
3. **sensitivity/regression probes** — explicit expected response direction/magnitude for one configurable parameter perturbation.

## Ownership boundary

M9.6 is observational.

```text
canonical M2–M5 physics/control
        ↓
immutable ControlRoomSnapshot / canonical solver result
        ↓
M9.6 metric extraction + explicit reference definitions
        ↓
comparison / sensitivity report
```

M9.6 never writes reactor state, controller state, actuator state, fault state or scenario outcomes.

`ControlRoomReferenceMetricExtractor` reads only values already present in `ControlRoomSnapshot`. Missing/unavailable presentation values remain missing evidence and fail closed; M9.6 does not reach into private Simulation state to fabricate a value.

## Versioned reference cases

`ReferenceValidationCaseDefinition` contains:

- stable `CaseId`;
- title/description;
- `SteadyState` or `Transient` classification;
- explicit `ModelVersion`;
- explicit `ReferenceSource` provenance text;
- one or more exact logical-step `ReferenceValidationTarget` entries.

Each target contains a stable metric ID, exact logical step, reference value and explicit `ReferenceValidationToleranceBudget`.

A suite may combine multiple cases only when all cases declare the same model version.

## Curated v1 baseline cases

M9.6 ships three deliberately conservative internal reference/regression cases:

- `cold-shutdown-steady-state-v1` — M7.2 cold-shutdown boundary at logical step 0;
- `grid-synchronization-steady-state-v1` — M7.5 pre-synchronization handoff at logical step 0;
- `initial-grid-load-transient-v1` — canonical breaker-close then first load-raise sequence through logical step 2.

These cases are anchored to behavior already validated in earlier milestones. Their provenance explicitly states that they are **internal validated regression baselines, not external historical measurements**.

External engineering/historical reference datasets may be added later only as new versioned cases with explicit provenance and justified tolerances. M9.6 does not invent or silently import external numbers.

## Tolerance semantics

For one target:

```text
allowed error = max(absolute tolerance,
                    abs(reference value) × relative tolerance fraction)
```

Observed evidence is:

- `Passed` when a finite observed value exists and error is within budget;
- `Failed` when a finite value exists but exceeds budget;
- `Missing` when the exact step/metric is absent or unavailable.

`Missing` is never treated as success.

## Stable presentation metric IDs

M9.6 defines stable IDs for reference evidence including:

- reactor thermal power, average rod withdrawal and xenon reactivity;
- primary total mass and running-pump count;
- turbine shaft power and maximum rotor speed;
- gross/total generator output, breaker count and synchronization-ready count;
- invalid measured-signal count and unacknowledged alarm count;
- reactor/turbine/generator trip state.

The ID catalog is a validation/presentation seam, not a new physical state model.

## Sensitivity/regression reports

`ReferenceSensitivityProbeDefinition` declares:

- stable probe ID;
- configurable parameter ID;
- baseline and perturbed parameter values;
- observed metric ID;
- expected response direction;
- minimum/maximum absolute response budget.

`ReferenceSensitivityAnalyzer` compares two results produced by canonical runs and emits:

- metric delta;
- parameter delta;
- normalized sensitivity;
- pass/fail assessment.

The analyzer does not mutate parameters or run hidden simulations. Test/orchestration code remains responsible for constructing explicit baseline and perturbed canonical configurations.

M9.6 includes a regression demonstrating a real `FissionPowerCalibration` perturbation and its expected thermal-power response through the existing M2.4 solver.

## GUI/App hardening before M10

At the user's request, M9.6 also strengthens automated App/UI coverage before the M10 operator-computer expansion.

The new tests verify:

- workspace selection exposes exactly one matching presentation workspace state;
- published snapshots update headline state, protection summary, signal health and dependent property notifications;
- rod/pump/generator/alarm selection indices clamp safely when published collections shrink;
- targeted UI commands preserve canonical target IDs and `ControlRoomCommandTargetKind` across the dispatcher boundary;
- host/protection/global alarm commands remain untargeted typed intents;
- missing canonical targets fail closed and do not dispatch;
- scenario dispatcher rejection is surfaced as a blocked status rather than hidden UI mutation;
- alarm ACK/RESET availability follows canonical annunciator semantics;
- trip/interlock states disable affected normal controls without converting presentation into a second protection owner;
- real validated M7.2/M7.5 seeds expose expected command surfaces;
- XAML command/state bindings are checked for the operational push buttons;
- target selectors remain two-way bound to dedicated selection/availability state;
- key instrumentation labels preserve explicit `MEASURED` versus `MODEL` provenance;
- workspace navigation remains bound to the canonical workspace catalog.

These are logic/binding regression tests, not screenshot pixel-diff tests. Visual layout/readability still requires the short manual validation checklist in `docs/MANUAL_GUI_VALIDATION_CHECKLIST.md`.

## Deliberate non-goals

M9.6 does not claim:

- licensing-grade validation;
- exact historical reconstruction;
- automatic parameter fitting/optimization;
- hidden calibration that changes canonical model parameters;
- external-source authority ranking;
- private Simulation-state access from Application;
- pixel-perfect visual regression infrastructure.

M9.7 remains the advanced-fidelity integration gate that must combine replay, analysis, xenon, quasi-spatial behavior and these reference baselines before M10 begins.
