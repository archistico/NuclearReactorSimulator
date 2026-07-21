# Control Rods

M2.2 introduces deterministic control-rod mechanics and maps rod position to explicit M2.1 reactivity contributions. It deliberately does not implement neutron kinetics or direct reactor-power effects.

## Canonical position convention

`ControlRodPosition` stores normalized withdrawal fraction:

```text
0.0  = fully inserted
0.5  = half withdrawn
1.0  = fully withdrawn
```

The convention is explicit in API names (`FractionWithdrawn`, `PercentWithdrawn`, `FractionInserted`) to avoid ambiguous generic percentages.

## Definition and state

A `ControlRodDefinition` owns immutable engineering/configuration data:

- rod id;
- group id;
- normalized full-stroke travel rate;
- fully inserted reactivity endpoint;
- fully withdrawn reactivity endpoint;
- integral worth-curve kind.

A `ControlRodState` owns only operational state:

- rod id;
- current normalized position;
- persistent motion command (`Hold`, `Insert`, `Withdraw`).

This keeps configuration out of mutable simulation state and makes replay/state comparison straightforward.

## Motion

`ControlRodMotionSolver` advances position only from:

```text
committed position
+ persistent motion command
+ configured travel rate
+ fixed timestep
```

Movement is clamped to `[0, 1]`. Reaching either mechanical endpoint automatically changes motion to `Hold`. No wall-clock time or UI cadence participates.

## Groups and command ordering

`ControlRodGroupDefinition` is only a command grouping. Individual rods remain the sole physical state.

`ControlRodSystemSolver` applies commands in caller/FIFO order before advancing the rods. Therefore later commands in the same logical step deterministically override earlier commands for overlapping targets:

```text
1. Withdraw group A
2. Hold rod A-2

result for that step:
A-1 withdraws
A-2 holds
```

Rod advancement then occurs in canonical ordinal rod-id order.

## Integral worth

`ControlRodWorthSolver` maps position to a named `ReactivityContribution` of kind `ControlRods`.

M2.2 includes two deliberately simple integral curves:

- `Linear`;
- `SmoothStep` (`3x² - 2x³`).

Both interpolate between explicit fully-inserted and fully-withdrawn reactivity endpoints. The endpoints are signed `Reactivity` values rather than an assumed always-negative magnitude, keeping the seam open for later plant-specific behavior.

The current curves are educational approximations. A future RBMK-specific model may replace axial worth behavior, including more detailed absorber/displacer effects, behind the same position-to-worth boundary.

## Explicit non-goals

M2.2 does not implement:

- neutron population dynamics;
- delayed neutron groups;
- reactor period;
- automatic regulation systems;
- SCRAM logic or insertion acceleration;
- individual physical rod length/axial core geometry;
- RBMK graphite-displacer transient detail;
- thermal power response.

Those belong to later reactor-physics, control and safety milestones.


## M2.3 integration

Control rods still stop at named `ControlRods` reactivity contributions. M2.3 composes those contributions through `ReactivityModel` and then supplies only the resulting total reactivity to point kinetics. Rod mechanics never writes neutron population directly.
