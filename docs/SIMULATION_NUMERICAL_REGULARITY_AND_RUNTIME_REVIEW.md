# Simulation numerical regularity and runtime review disposition

## Purpose

This document records the post-M10.9.7 static review of `NuclearReactorSimulator.Simulation` and the engineering disposition agreed before M10.9.7.4. It is **not** a new numerical baseline and authorizes no production-physics change by itself.

The governing rule is evidence before modification: presentation/replay work may continue on the validated numerical baseline, while findings that can alter physical trajectories are assigned to explicit M11/M12 gates rather than being patched opportunistically.

## Current disposition at a glance

| Finding | Verified disposition | Owner |
| --- | --- | --- |
| quadratic inverse pipe law has unbounded slope as `Δp → 0` | real numerical-regularity issue; no quick regularization | M12.2 |
| pump discharge check valve | continuous at zero but non-smooth; not a flow discontinuity | M12.1/M12.2 |
| valve effective resistance / quick-opening near closure | reduced-order law has strong near-close conditioning; not a current Jacobian-coordinate singularity by itself | M12.2 |
| H.9 diagonal regularization | acts on a normalized coordinate/residual Jacobian; do not interpret `1e-8` as a raw physical-unit constant | M12.2 conditioning audit |
| `MaximumPivotConditionEstimate` | pivot-spread rejection metric, not a mathematical condition number | M12.2 |
| pump shaft/hydraulic energy difference | motor/electrical draw and inefficiency-to-heat are historical non-scope, not silently conserved owners | M12.4 |
| branch continuity | base resolver is memoryless; corrected four-node path can conditionally use bounded previous-phase continuity and commit it | architecture/docs now; retirement/unification requires dedicated evidence |
| generic `SimulationRuntime.Advance(elapsed)` catch-up | generic API has no per-call cap; desktop production uses bounded cooperative batches instead | M11.3 API/cost audit |
| deterministic signed-value sorting before compensated sum | deterministic, but alternative summation order is unqualified and may alter trajectories | M11.3 measurement / M12.2 if semantics change |
| per-evaluation `PipeDefinition` construction in pump/valve solvers | real hot-path allocation/validation candidate | M11.3 |
| four-node temporary objects | real trigger-path allocation candidate, not an every-step cost | M11.3 |
| exceptions used for infeasible Newton/Anderson probes | semantically valid but potentially expensive expected-path behavior | M11.3 measurement first |
| 512-segment scan / 80 bisection ceiling | possible worst-case excess work; early termination already limits normal cost | M11.3 measurement first |

## 1. Near-zero quadratic hydraulic law

The current passive law in `PipeFlowSolver` is equivalent to:

```text
m_dot = sign(Δp) * sqrt(|Δp| / R)
```

For nonzero `Δp`:

```text
d(m_dot)/d(Δp) = 1 / (2 * sqrt(R * |Δp|))
```

so the slope grows without bound as the pressure difference approaches zero. The exact-zero branch prevents a division/error at precisely zero, but does not make the map differentiable around zero.

This matters because the H.9 corrector estimates a local Jacobian by finite differences. Near a flow reversal, a probe can sample a very different local slope depending on which side of zero it lands.

### Decision

Do **not** insert a laminar segment, epsilon smoothing or any other regularization during M10/M11 UI work. Such a change alters the constitutive law and can change exact trajectories, trigger timing, conservation evidence and corrected-commit behavior.

M12.2 must first:

1. freeze near-zero pressure-difference probes for representative pipe/valve/pump paths;
2. measure current map slope/finite-difference sensitivity and trigger correlation;
3. compare current law against candidate regularizations in an observational harness;
4. bound mass/energy/conservation differences;
5. re-run the relevant H/I reference and replay evidence before any production change is considered.

No regularization is selected by this document.

## 2. Pump discharge check valve semantics

The current check-valve behavior blocks negative raw pump-path flow by returning zero. Because the underlying raw quadratic flow tends continuously to zero as the reverse driving pressure tends to zero, the resulting map is effectively:

```text
max(0, raw_flow)
```

for the blocked direction. It is **continuous at the transition**, but not differentiable there.

### Decision

Do not replace the ideal check with arbitrary reverse leakage merely to make Newton smoother. M12.1 first classifies the check valve as an explicit one-way device. M12.2 may compare ideal blocking against a physically justified finite reverse-leakage model only if the component model provides evidence and ownership for such leakage.

## 3. Valve near-close behavior

For a non-closed valve the current reduced-order model computes effective resistance proportional to `1 / coefficient²`. This creates a large conditioning range near closure. Quick-opening characteristics also contain a square-root shape near zero mechanical position.

These observations do not mean the existing H.9 Jacobian differentiates directly with respect to valve position: its corrected coordinate set is hydraulic state/residual oriented. Therefore this is a conditioning/model-envelope concern, not proof of a current Newton-coordinate singularity.

### M12.2 audit requirements

- classify exact closed behavior separately from very-small-open behavior;
- prove finite values across the supported actuator-position envelope;
- identify any underflow/non-finite boundary and classify it as unreachable or unsupported;
- do not add an arbitrary deadband without a component-level physical/control contract;
- if a new near-close law is proposed, qualify its impact on steam admission, control authority and reference trajectories.

## 4. Jacobian normalization and conditioning terminology

H.9 builds coordinate scales before finite differences and normalizes the coordinate residual:

```text
normalized residual = (mapped - applied) / coordinate scale
```

The diagonal regularization is then added to that normalized Jacobian. Therefore `JacobianDiagonalRegularization = 1e-8` is not a raw Pa/kg/s/W-scale constant and must not be judged as though it were applied directly to an unscaled physical matrix.

However, the current `pivotConditionEstimate = maximumPivot / minimumPivot` is a **pivot-spread heuristic**, not a true matrix condition number. It remains useful as a deterministic rejection signal, but the name and acceptance interpretation must not overstate what it measures.

### M12.2 audit requirements

- publish coordinate/residual scale ranges at representative and near-zero states;
- publish pivot spread separately from any stronger conditioning diagnostic;
- evaluate whether a deterministic norm/condition estimator can add information without destabilizing the gate;
- compare absolute-vs-relative diagonal regularization only in normalized space;
- keep the current H.9 production values unchanged unless the audit demonstrates a reproducible failure and a replacement passes requalification.

## 5. Current branch-continuity semantics

Two related semantics intentionally coexist today:

1. `SimplifiedWaterSteamThermodynamicModel` is the base production inverse map and remains memoryless with respect to branch selection; its `previousState` parameter is not used to choose the ordinary production branch.
2. The corrected four-node path can wrap that same inverse map in `ThermodynamicBranchContinuityModel` with bounded previous-phase hysteresis for targeted nodes. Under the validated corrected-commit authority path, the resulting corrected candidate can become committed plant state.

Therefore historical source wording that calls `ThermodynamicBranchContinuityModel` simply “shadow-only” no longer describes the complete current operational role. The class/name/comment remain historical provenance until a source-touching milestone; current architectural truth is this document plus `ARCHITECTURE.md` and `KNOWN_MODEL_LIMITATIONS.md`.

### Decision

- do not wire previous-state continuity globally into the base resolver during M10.9.7;
- do not remove bounded continuity because repaired exact @4 observed zero branch overrides: previous-phase holds remained materially exercised;
- any future unification, retirement or simplification requires a dedicated post-Phase-I qualification comparing base, corrected and replay/checkpoint trajectories.

## 6. Pump energy ownership

The pump model adds hydraulic power to the receiving fluid and reports positive shaft demand as hydraulic power divided by efficiency. The difference represents motor/pump losses, but current M1.5 scope deliberately did not model motor/electrical dynamics or loss-to-heat deposition.

This is therefore a **known ownership boundary**, not an undiscovered conservation bug. It becomes insufficient once the project makes stronger full-plant accident/long-duration energy claims.

### M12.4 required closure

M12 must decide and implement, with explicit owners:

- where pump shaft demand is charged electrically/mechanically;
- whether/how modeled inefficiency becomes local fluid/component/ambient heat;
- how reverse/blocked/zero-flow states behave;
- how the new ownership appears in full-plant conservation audits;
- replay/checkpoint/session behavior for any new persistent motor/thermal state.

M15 may not rely on pump-loss heating or electrical failure consequences until this ownership is closed.

## 7. Generic runtime catch-up

`SimulationRuntime.Advance(TimeSpan elapsedExternalTime)` can consume all buffered fixed steps in one locked call and does not define a maximum steps-per-call policy.

The desktop production host does **not** use that API as an unbounded wall-clock catch-up loop. `DesktopControlRoomRuntimePump` requests five simulation steps per tick, and `ControlRoomRuntimeCoordinator` enforces a cooperative maximum batch size.

### M11.3 decision point

Inventory all consumers of the generic `Advance(elapsed)` API and choose an explicit supported policy:

- caller-bounded only;
- bounded backlog retained for later work;
- bounded catch-up with explicit dropped-time semantics;
- API restriction/removal if it is not a supported release surface.

A cap must never silently discard deterministic simulation time. The desktop path is not blocked on this review item.

## 8. Deterministic summation

`PlantNetworkSourceTerms.SumDeterministically` imposes deterministic signed-value ordering before compensated summation. Ordering by absolute magnitude is a different numerical convention and may reduce error in some mixed-sign sums, but changing the convention can alter bit-level trajectories and downstream trigger timing.

### Decision

M11.3 may measure cost/error characteristics using frozen vectors. Any production summation change is routed through M12.2 requalification if it changes numerical trajectories. Determinism is mandatory; “more accurate in one synthetic vector” is not sufficient evidence.

## 9. M11.3 measured hot-path candidates

M11.3 owns performance work that can preserve the validated physical law:

### Pump/valve effective resistance

`PumpFlowSolver` and `ValveFlowSolver` currently construct a validated `PipeDefinition` to change only the effective resistance before delegating to `PipeFlowSolver`.

Candidate optimization: an internal `PipeFlowSolver` overload accepting the existing canonical pipe plus an override `QuadraticHydraulicResistance`, while preserving endpoint checks, sign convention and energy-transport mode.

Required gate: before/after allocations and wall cost plus exact output equivalence across forward/reverse/zero/closed representative cases.

### Four-node trigger-path allocations

Measure construction of branch-continuity/corrector helpers and decision collections only on actual trigger paths. Reuse is allowed only if all per-attempt state/logs have explicit reset semantics and deterministic repeat remains exact.

### Infeasible probe exceptions

Measure frequency and cost of exceptions currently used to represent expected infeasible finite-difference/line-search probes. A `TryResolve`/result-path redesign is allowed only if it preserves the exact classification between infeasible probe, unsupported state and genuine programming/contract failure.

### Thermodynamic root-search ceilings

Measure actual coarse-search and bisection iteration distributions, especially near phase boundaries. Reduce ceilings only if all supported roots, branch selection, fingerprints and replay trajectories remain identical.

## 10. Gate ownership and sequencing

This review does **not** block completion of M10.9.7 presentation/replay work.

The approved order is:

```text
finish M10.9.7 manual/automated gates
→ M10.9.7.4 / M10.9.7 closure
→ M10.9.8 integrated validation
→ M11.3 measured non-physical hot-path work
→ M12.1 directionality
→ M12.2 numerical/constitutive regularity and conditioning
→ M12.3 extreme-state envelope
→ M12.4 pump mechanical/electrical/thermal energy ownership
→ M12.5 full-plant decay heat
→ M12.6 integrity/stress
→ M12.7 IncidentSeverity
→ M12.8 closure
```

Any M12.2 production-physics change must explicitly state which historical H/I evidence is rerun and which exact-version identities remain immutable.
