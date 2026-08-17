# M10.9.4.1-H.10 — Hydraulic Map Switching & Non-Smoothness Diagnosis

## Status

VALIDATED as M10.9.4.1-H.10 Hotfix 1. User validation passed build, ordinary tests and the focused audit. Production current-v2 remains `ExplicitCommittedState` at the validated 10 ms fixed step.

Validated focused result:

```text
hydraulic-branch-switches=0
hydraulic-nonsmooth-paths=0
thermodynamic-phase-or-envelope-switches=2
thermodynamic-nonsmooth-nodes=2
max-thermodynamic-derivative-scale-growth=3.999974411
explicit-end thermodynamic switches=0
switching-evidence-found=True
non-smooth-evidence-found=True
deterministic-repeat=True
```

## Why H.10 exists

H.4 through H.9 progressively changed the nonlinear-corrector family while preserving the same frozen P060/F040 evidence set. The validated convergence counts are:

- H.4 Picard primary: 5/7;
- H.6 bounded Picard rescue: 6/7;
- H.7 true fixed-point residual + deterministic backtracking: 5/7;
- H.8 safeguarded Anderson: 5/7;
- H.9 finite-difference Jacobian + damped Newton: 5/7.

H.9 is especially diagnostic because the Jacobians were well conditioned, Newton steps were bounded and merit decreased strictly, yet two events still exhausted line search at almost the same residual barrier as H.7/H.8. Increasing solver complexity again is therefore not justified before examining the map itself.

H.10 asks a narrower question: **do the two persistent H.9 failures sit near branch changes, kinks, phase/envelope boundaries or other locally non-smooth structure?**

## Diagnostic design

H.10 introduces `HydraulicMapSmoothnessAnalyzer`. It is observational only and is not consumed by `PlantNetworkOrchestrator`.

### Hydraulic law-local pressure probes

For every pipe, valve and pump path, H.10 perturbs the from-node pressure symmetrically at two scales while preserving the remainder of the state. These probes deliberately isolate the already-existing hydraulic law from thermodynamic closure.

The audit records:

- base, minus and plus flow;
- pressure-sign branch (`forward`, `reverse`, `zero`);
- closed-valve branch;
- pump discharge-check-valve blocked branch;
- one-sided slope asymmetry;
- central derivative at coarse and fine probe scales;
- derivative scale growth as the probe shrinks;
- explicit branch-switch evidence.

Two scales matter because the current passive law `m_dot = sign(Δp) * sqrt(|Δp|/R)` is continuous but not differentiable at zero pressure difference. Near that point the apparent slope grows as the probe shrinks even when a simple left/right sign check is insufficient.

### Conserved thermodynamic inventory probes

For every fluid node, H.10 perturbs conserved internal energy and mass at two scales and re-runs the existing `IFluidThermodynamicModel` closure.

The audit records:

- phase on each side of the base state;
- whether each probe remains inside the supported thermodynamic envelope;
- pressure derivative scale growth for energy probes;
- pressure derivative scale growth for mass probes;
- phase/envelope switching evidence.

These probes do not clamp or modify conserved state. An out-of-range probe is evidence about proximity to the supported closure boundary, not permission to widen that boundary.

## Frozen evidence set

The focused H.10 audit reconstructs the exact 500-step current-v2 explicit trajectory and frozen P060/F040 trigger set used by H.5-H.9. It must reproduce:

- 7 triggered events;
- H.4 convergence 5/7;
- H.6 convergence 6/7;
- H.7 convergence 5/7;
- H.8 convergence 5/7;
- H.9 convergence 5/7 and two persistent failures.

Only the two non-converged H.9 candidate states are diagnosed in detail. The committed explicit endpoint of the same interval is analyzed as a local control.

## Interpretation

A positive switching/non-smoothness finding does **not** activate a different solver. It localizes the path/node and supports a later formulation tailored to that structure, such as an active-set or semi-smooth treatment.

If the selected probes find no switching or scale-sensitive non-smoothness, H.10 instead directs the next work toward fixed-point existence and basin structure: the relevant question becomes whether a nearby fixed point exists at all, or whether the requested frozen map has a residual floor or a remote branch.

## Production isolation

H.10 does not:

- change `PlantNetworkOrchestrator` routing;
- replace Picard, H.7, H.8 or H.9;
- retune P060/F040;
- retune any hydraulic or thermodynamic coefficient;
- change the 10 ms fixed step;
- introduce hidden filtering;
- commit any diagnostic candidate.

Production therefore remains exactly on the validated explicit path.

## Focused gate

Run:

```bat
scripts\run-hydraulic-map-switching-nonsmoothness-audit.cmd
```

Artifacts are written to:

```text
artifacts\h10-hydraulic-map-switching-nonsmoothness\
```

The summary is diagnostic. `switching-evidence-found=False` is not a milestone failure if the frozen baseline, deterministic repeat and production isolation remain valid; it simply changes the next scientific question.
