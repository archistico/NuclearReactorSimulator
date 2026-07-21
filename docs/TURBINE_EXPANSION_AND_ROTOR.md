# Turbine Expansion and Rotor Model

## Scope

M4.2 replaces the temporary M4.1 turbine-admission external sink with the first explicit steam-to-mechanical-energy conversion model.

The model is deliberately lumped and educational. It establishes conservation, state ownership and dynamic seams before condenser, generator and automatic-control fidelity are added.

## Topology

```text
M3 steam drums
    ↓
M4.1 main steam lines / headers
    ↓
stop → control → admission valves
    ↓
canonical turbine inlet node
    ↓
M4.2 lumped turbine stage group
    ├── shaft work → rotor mechanical state
    ↓
canonical turbine exhaust node
    ↓
M4.3 condenser boundary (next milestone)
```

`TurbineExpansionSystemDefinition` composes the validated `MainSteamNetworkDefinition` rather than creating a second topology graph.

Every M4.1 `TurbineAdmissionBoundaryDefinition` must feed exactly one M4.2 `TurbineStageGroupDefinition`. Every stage group references:

- the M4.1 admission-boundary id;
- one existing canonical exhaust fluid node;
- one defined turbine rotor;
- nominal specific work at rated speed;
- a simplified constant steam-to-shaft efficiency.

Every rotor must be driven by at least one stage group.

## Seam ownership

M4.1's terminal boundary remains in the definition for backward compatibility, but while M4.2 is active all `TurbineAdmissionBoundaryInput` mass flows must be zero.

This prevents double removal:

```text
WRONG:
inlet → M4.1 external sink
      + M4.2 turbine expansion

M4.2:
inlet → turbine stage group → exhaust
                 ↓
              shaft work
```

The legacy M3 steam-export boundaries must also remain zero, as already required by M4.1.

## Expansion accounting

For each stage group, all values are evaluated from the same committed plant/mechanical state.

The commanded steam flow is blocked only when the explicit rotor trip command is active. Otherwise the stage transfers the requested mass internally.

The stage removes from the inlet:

- the effective steam mass flow;
- the full committed inlet specific-internal-energy flow.

It adds to the exhaust:

- exactly the same mass flow;
- inlet energy flow minus shaft power.

Therefore:

```text
Σ mass source terms = 0
Σ thermofluid energy source terms = -shaft power
```

The solver fails closed if the simplified work model would extract more specific energy than the committed inlet contains.

## Simplified torque characteristic

The M4.2 stage characteristic is intentionally replaceable.

At the defined rated rotor speed, nominal stage power is:

```text
mass flow × nominal specific work × turbine efficiency
```

The corresponding stage torque is the rated power divided by rated angular speed. This produces a deterministic lumped driving torque suitable for the first rotor-dynamics milestone.

Actual shaft work over a step uses the rotor's average angular speed across that deterministic step. This makes the constant-torque rotor integration and mechanical energy audit consistent, including acceleration from zero speed.

Detailed turbine maps, velocity triangles, isentropic property tables, wetness corrections and stage-by-stage pressure ratios remain replaceable future refinements.

## Rotor state and dynamics

Rotor kinetic state is explicit and separate from `PlantState`:

```text
TurbineExpansionState
    └── TurbineRotorState
            └── AngularSpeed
```

This avoids pretending that rotational kinetic energy is thermal energy.

Each rotor definition provides:

- moment of inertia;
- rated angular speed;
- overspeed threshold.

Each step computes:

- total turbine driving torque from attached stage groups;
- commanded external load torque;
- effective external load torque;
- net torque;
- candidate angular speed;
- initial/final kinetic energy;
- shaft power and load power.

External load torque is currently a manual replaceable seam. M4.5 will replace/compose it with generator electromagnetic torque.

A load command that would reverse the rotor within one fixed step is limited exactly at zero speed. Both commanded and effective load remain visible in the snapshot; the limitation is never hidden.

## Mechanical energy audit

`TurbineMechanicalAudit` reports raw:

- initial rotor kinetic energy;
- final rotor kinetic energy;
- total shaft power received from steam;
- total external load power;
- mechanical energy closure residual.

The expected relationship is:

```text
Δ rotor kinetic energy
= (shaft power - external load power) × Δt
```

The thermofluid `PlantNetworkAudit` independently observes the equal shaft-energy removal from fluid inventories. This two-domain accounting is intentional until the later full secondary-cycle/generator energy audit composes all domains at plant level.

## Overspeed and trip seams

M4.2 deliberately separates indication from protection logic.

`OverspeedDetectedAtStart` and `OverspeedDetectedAtEnd` compare rotor speed against the configured threshold. They do **not** automatically latch or issue a trip.

`TripCommand` is an explicit input seam. When asserted, attached stage-group expansion flow is blocked. M4.2 does not secretly mutate stop/control/admission valve states; deterministic protection sequencing belongs to M5.

## Determinism and integration

M4.2 preserves the validated architecture rules:

1. plant and rotor solvers read committed state only;
2. turbine fluid transfers are staged as `PlantNetworkSourceTerms`;
3. M3 + M4.1 + M4.2 fluid/thermal inventories are integrated exactly once by `PlantNetworkOrchestrator`;
4. rotor mechanical state is integrated exactly once by the M4.2 mechanical boundary;
5. no solver edits shared state during component evaluation;
6. conservation residuals remain visible.

## Deferred to M4.3+

- condenser and vacuum dynamics;
- hotwell inventory;
- realistic exhaust-pressure back-coupling;
- multi-cylinder HP/IP/LP detailed staging and moisture behavior;
- generator/grid electromagnetic load;
- automatic speed/load control and overspeed protection.

## M4.3 downstream condenser ownership

After M4.2 validation, M4.3 takes ownership of each canonical turbine exhaust seam without changing turbine stage or rotor definitions.

Every `TurbineStageGroupDefinition.ExhaustNodeId` becomes the steam-space node of exactly one `CondenserDefinition`. Turbine expansion still transfers steam and extracts shaft work exactly as M4.2 defines; M4.3 adds condenser source terms through the new backward-compatible supplemental-source overload on `TurbineExpansionSolver` before the same plant-network integration.

The turbine solver remains the sole owner of rotor mechanical-state integration. The condenser only owns thermofluid exhaust-to-hotwell transfer and external heat rejection.


## M4.5 generator-load ownership

M4.2 intentionally introduced `TurbineRotorInput.ExternalLoadTorque` as a replaceable manual seam. M4.5 now owns that seam when generator/grid physics is active. `GeneratorGridInputs` therefore requires every legacy M4.2 external-load torque command to be zero and injects the generator electromagnetic torque internally before the same M4.2 rotor integration. This prevents double loading while preserving M4.2 backward compatibility for standalone turbine tests and earlier milestones.
