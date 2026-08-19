# M10.9.4.1 — External Technical Audit Review and Planning Decision

## Status

**ACCEPTED PLANNING CHECKPOINT — documentation only**

This document evaluates two external LLM technical reviews supplied after M10.9.4.1-A Hotfix 1. The reviews are treated as hypotheses and design input, not as authoritative project truth. Every proposed production change remains subject to direct code inspection, deterministic evidence and an isolated regression.

## Current validated and candidate truth

- M10.9.4 — Subsystem Engineering Schematics: **VALIDATED**.
- M10.9.4.1-A Hotfix 1: compilation fix applied; build and ordinary automated suite passed locally.
- The explicit `OperationalEnvelopeAudit` gate is **NOT GREEN**.
- Therefore M10.9.4.1-A is **AUDIT EXECUTED / FAILURE FOUND / NOT VALIDATED**.
- No production physics change is authorized by this review.

## Review method

Each external claim is classified as one of:

- **confirmed structural concern** — directly supported by the current code;
- **plausible hypothesis requiring instrumentation** — consistent with the code but not proved dynamically;
- **partially correct or overstated** — contains a real concern but an incorrect mechanism, priority or proposed remedy;
- **already covered** — the project already has the relevant capability, though it may need consolidation;
- **future fidelity expansion** — valuable but outside the present hardening milestone.

## Accepted findings

### Immediate audit priorities

1. Expose all condenser condensation limits separately: inventory-limited, thermal/UA-limited and maximum-flow-limited values, plus the active limiter and margin.
2. Capture per-step protection trigger evidence, including function identifier, measured value, threshold, reset threshold and latched action.
3. Record masses and final-window slopes for `exhaust`, `hotwell`, `feedwater-inventory`, steam drum and the turbine admission train.
4. Record pump suction pressure, check-valve state transitions and controller command versus physical actuator position.
5. Measure governor authority through a deterministic valve-position / mass-flow / shaft-power / load-response map.
6. Define the reference-plant nominal scale before changing generator nameplate power, inertia, droop or coupling limits.

### Confirmed model concerns for later isolated phases

- compressed-liquid states can remain mathematically resolvable above the intended operating envelope without an explicit diagnostic;
- current turbine stage flow and current turbine work do not use the same liquid/wet-steam admission policy;
- finite actuator travel has no explicit tracking anti-windup against physical actuator position;
- current generator/grid electromagnetic loading cannot represent negative electrical power or motoring;
- the current nominal electrical scale, rotor inertia and low-load secondary-cycle capacity require an explicit consistency decision;
- drum liquid recirculation and low-inventory behavior require physical closure before low-level protection is added;
- the current advective energy convention uses specific internal energy and does not explicitly model flow work/enthalpy transport;
- the manually parameterized sustained-generation seed has not yet been proved to be an equilibrium point;
- current-v2/legacy option combinations require a supported-profile matrix and eventual retirement policy.

## Findings accepted with qualification

### Governor control-valve authority

The external static estimate correctly indicates that stage resistance may dominate the current control valve at the validated point. It does not by itself prove that a Stodola law is the only correct remedy. The required first step is an observed authority map over valve position, pressure ratio, mass flow and shaft power.

### Condenser capacity ceiling

`min(Q_available, UA * ΔT)` is not intrinsically defective: installed cooling capacity is a legitimate ceiling. The unresolved question is which limiter controls the operating point and how often the active limiter changes. Instrumentation precedes any removal or rescaling of the 24.5 MW ceiling.

### Turbine torque reference speed

The current formula has a derivative discontinuity at rated speed. The external claim that it necessarily creates the stated below-rated positive feedback is not established by the code. This remains a lower-priority continuity issue until measured evidence shows a material effect.

### Runtime conservation ledger

The project already publishes `PlantNetworkAudit`, `SecondaryCycleHeatBalanceAudit` and local residuals. The improvement is to consolidate and expose the existing audits more clearly, not to create a second physical owner.

### Trajectory baselines

The project already has versioned reference-validation cases and tolerance budgets. The required extension is a 300-second trajectory profile with periodic samples, extrema, slopes and integrated error rather than a parallel baseline system.

## Deferred fidelity expansion

The following proposals are valuable but do not belong inside the immediate M10.9.4.1 correction sequence:

- elevation, hydrostatic head and natural circulation;
- density-aware general pipe resistance;
- NPSH, cavitation and loss of pump prime;
- condenser non-condensables and air ejectors;
- circulating-water pumps and cooling-water network;
- regenerative feed heating, deaeration and moisture separation/reheat;
- separate graphite thermal inertia;
- drum swell/shrink;
- residual-heat-removal and emergency cooling systems;
- live T-s/Mollier presentation before entropy/property support exists.

Choked compressible flow is not merely optional presentation fidelity: it is a prerequisite for a credible future relief/bypass phase.

## Process decisions

- Do not change `MaximumElectricalPower` as a one-line hotfix.
- Do not lower protection thresholds or acceptance floors to make the audit green.
- Do not remove condenser capacity bounds before identifying the active limiter.
- Do not add low-drum-level protection before liquid inventory and recirculation semantics are physically closed.
- Do not implement adaptive substepping before measuring timestep convergence and stiffness.
- Do not introduce runtime hidden steady-state trimming. Any future trim tool must be deterministic, offline/versioned and produce an explicit seed artifact.

## Next checkpoint

The next implementation checkpoint is **M10.9.4.1-A.1 — Audit Evidence Completion**. It remains test/diagnostic/documentation work only. Production physics begins only after the A.1 evidence has been reviewed and assigned to the smallest canonical owner.
