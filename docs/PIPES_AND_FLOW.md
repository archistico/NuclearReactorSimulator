# Pipes and Flow Resistance

## Scope

M1.3 introduces passive bidirectional hydraulic connections between existing lumped fluid nodes.

A pipe does not own fluid inventory. It reads the two committed endpoint states, solves one instantaneous transfer, and produces equal-and-opposite mass and internal-energy balances for those endpoints.

## Reference direction

Each `PipeDefinition` declares:

```text
FromNodeId  ───── pipe ─────>  ToNodeId
             positive sign
```

This is only a sign convention. A passive pipe is bidirectional:

- positive mass flow means `FromNode -> ToNode`;
- negative mass flow means `ToNode -> FromNode`;
- zero pressure difference produces zero transfer.

Reversed physical flow does not require swapping or recreating the pipe definition.

## M1.3 resistance law

M1.3 uses a lumped quadratic resistance law:

```text
Δp = R · m_dot · |m_dot|
```

where:

```text
Δp     = From pressure - To pressure [Pa]
R      = QuadraticHydraulicResistance [Pa·s²/kg²]
m_dot  = signed mass flow [kg/s]
```

The solver therefore obtains the mass-flow magnitude from the pressure-difference magnitude and applies the pressure-difference sign as the flow direction.

The resistance is intentionally an aggregate model parameter in M1.3. Geometry-dependent correlations, friction-factor models and regime-dependent losses may later derive or replace this coefficient without changing the pipe/network boundary.

## Conservative transport

For a solved signed mass flow, M1.3 advects the **upstream node's specific internal energy**:

```text
positive flow: upstream = FromNode
negative flow: upstream = ToNode
```

The resulting transfer produces endpoint balances:

```text
FromNodeBalance = (-massFlow, -energyFlow)
ToNodeBalance   = (+massFlow, +energyFlow)
```

Therefore, for every isolated pipe transfer:

```text
sum(mass balances)   = 0
sum(energy balances) = 0
```

This is a deliberate conservative M1.3 baseline. A more complete open-system thermal-hydraulic formulation will later use the water/steam property model introduced in M1.7, where transported enthalpy and phase behaviour can be represented correctly.

## Solver ownership

`PipeFlowSolver` is memoryless:

```text
PipeDefinition
+ committed FromNodeState
+ committed ToNodeState
        ↓
PipeFlowSolver
        ↓
PipeFlowResult
        ↓
endpoint FluidNodeBalance values
```

The pipe solver does not mutate node state and does not advance time. Node inventories are integrated separately through `FluidNodeIntegrator` using the same deterministic fixed timestep.

All pipe flows for a future network step must be solved from the same committed pre-step plant state before candidate node states are integrated. This preserves simultaneity and prevents connection ordering from becoming hidden physics.

## Explicit non-goals for M1.3

M1.3 does not yet model:

- valves or variable resistance;
- pumps or active pressure head;
- pipe fluid inventory or transport delay;
- momentum/inertial transients;
- elevation/static head;
- Darcy-Weisbach friction calculations;
- two-phase pressure drop;
- choking/cavitation;
- enthalpy-based phase transport.

Those capabilities arrive only when their milestones require them.
