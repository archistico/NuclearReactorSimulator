# Main Steam Network & Turbine Admission

M4.1 is the first turbine-island milestone. It replaces the temporary M3 steam-export sink as the active downstream path while preserving the validated M3 primary-circuit integration and accounting contracts.

## Scope

The M4.1 steam path is:

```text
Steam drum canonical steam-outlet node
        ↓
M3 SteamExportBoundaryDefinition seam (commanded to zero external export)
        ↓
MainSteamLineDefinition / canonical PipeDefinition
        ↓
main steam header node
        ↓
stop valve
        ↓
control valve
        ↓
admission valve
        ↓
turbine-inlet node
        ↓
replaceable TurbineAdmissionBoundaryDefinition
```

The validated M4.1 scope adds transport and admission only. Turbine expansion, shaft work and rotor inertia are layered by validated M4.2; M4.3 now owns condenser pressure/vacuum and hotwell condensation, while generator electrical load remains a later milestone.

## Canonical topology

`MainSteamNetworkDefinition` composes existing canonical plant components rather than creating a parallel hydraulic graph.

It requires:

- exactly one `MainSteamLineDefinition` for every M3 steam-export seam;
- each main-steam line to use an existing `PipeDefinition` from the canonical steam-outlet node to a canonical header node;
- one or more `TurbineAdmissionTrainDefinition` instances supplied by those headers;
- each admission train to form an exact series chain of existing stop, control and admission `ValveDefinition` components;
- exactly one replaceable `TurbineAdmissionBoundaryDefinition` at each train's canonical turbine-inlet node.

All semantic collections are canonicalized and validated eagerly. Valve and pipe physics remain owned by the validated M1 hydraulic primitives and by `PlantNetworkOrchestrator`.

## M3 replacement seam

The M3 `SteamExportBoundaryDefinition` is intentionally retained because it identifies the canonical drum steam-outlet seam used by M4.

When `MainSteamNetworkInputs` is active, every legacy M3 `SteamExportBoundaryInput` must be exactly zero. This prevents double removal of steam:

```text
legacy M3 external steam export = 0
M4.1 canonical main-steam transport = active
M4.1 turbine-admission boundary = active temporary terminal sink
```

M4.4 now replaces the M3.7 external feedwater mass source with the canonical condensate/feedwater return train; the stable M3 feedwater seam remains as the target identity.

## Single integration boundary

M4.1 does not add another conserved-state integrator.

`MainSteamNetworkSolver`:

1. reads one committed `PlantState`;
2. produces committed-state line and valve diagnostics through `PipeFlowSolver` and `ValveFlowSolver`;
3. evaluates the temporary turbine-admission sink from the same committed turbine-inlet state;
4. passes those terminal source terms into the M3.8 integrated primary-circuit solver through its higher-phase composition seam;
5. combines all staged source terms before the same single `PlantNetworkOrchestrator` integration.

The main-steam pipes and valves are already canonical `PlantDefinition` components, so their equal-and-opposite internal mass/energy transport is solved exactly once by the network orchestrator.

## Turbine-admission boundary

`TurbineAdmissionBoundaryDefinition` is deliberately temporary and replaceable.

For M4.1, a controllable non-negative mass flow removes steam from the canonical turbine-inlet node. The exported specific internal energy is read from that committed node, producing explicit signed external mass and energy source terms.

M4.2 replaces this sink during turbine-expansion operation with mechanical-energy extraction without rewriting the upstream main-steam line/header/valve topology.

## Diagnostics

`MainSteamNetworkSnapshot` exposes:

- per-line pressure difference, mass flow and internal-energy flow;
- effective stop/control/admission valve positions and flow coefficients;
- per-valve flow and pressure diagnostics;
- committed-state continuity residuals between the three valve stages;
- turbine-inlet pressure, temperature and phase;
- turbine-admission mass and energy export;
- inherited integrated-primary-circuit snapshot and global plant-network conservation audit.

Continuity residuals are diagnostics only. They are not corrected by hidden bookkeeping because every component reads the same committed state.

## Deliberately deferred

M4.1 does not add:

- turbine stage expansion or steam-property drop through stages;
- mechanical shaft torque/power extraction;
- rotor inertia or speed dynamics;
- overspeed/trip logic;
- condenser/vacuum/hotwell physics;
- closed condensate/feedwater return;
- automatic pressure, speed or load control.

Those remain M4.2 and later roadmap milestones.

## M4.2 replacement of the terminal boundary

After M4.1 validation, M4.2 uses the existing `TurbineAdmissionBoundaryDefinition` only as the canonical semantic inlet seam. Its temporary external sink input is required to be zero while `TurbineExpansionSolver` is active.

The downstream path becomes:

```text
admission valve
    ↓
turbine inlet node
    ↓
lumped M4.2 turbine stage group
    ├── shaft work → explicit rotor mechanical state
    ↓
canonical exhaust node
```

`MainSteamNetworkSolver` retains its original three-argument API for validated M4.1 behavior and adds a backward-compatible supplemental-source-term overload for M4.2 and later turbine-island phases. All thermofluid balances still compose before one `PlantNetworkOrchestrator` integration.

See `docs/TURBINE_EXPANSION_AND_ROTOR.md` and ADR 0033.


## M10.9.4.1-B.2 source-side ownership — locally validated

The temporary Hotfix 16 current-v2 demand closure in `MainSteamNetworkSolver` was removed in B.2 and the user confirmed compilation/tests passed locally. The main-steam solver no longer calculates drum supply as a function of committed main-steam-line flow.

For explicit current-v2 profiles, steam creation/availability is owned upstream by the steam-drum source closure. `MainSteamNetworkSolver` again owns transport/admission composition only:

```text
drum source physics
    ↓
canonical steam-outlet inventory
    ↓  pressure-driven canonical pipe
main-steam header
    ↓
stop / control / admission valves
    ↓
turbine inlet
```

This restores a one-way dependency: downstream demand may change steam-outlet pressure and therefore indirectly change the source pressure head on later deterministic steps, but downstream flow is not copied back as an invented source term.

Historical/null-source profiles remain explicit compatibility paths. See ADR 0094.


## D.1 turbine-boundary phase semantics

The main-steam/admission train still establishes the hydraulic state at `turbine-inlet`. The current-v2 turbine stage then applies its own explicit `VaporMassFractionLimited` policy at the expansion boundary. This prevents a pressure difference from forcing liquid inventory through the stage merely because a hydraulic request exists. Legacy stages retain their historical unrestricted transfer semantics.
