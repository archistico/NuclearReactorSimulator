# Nuclear Reactor Simulator — Project Handoff

> **Current validated continuation:** M10.9.4.1-F.1. **Working source:** M10.9.4.1-F.2 CANDIDATE — one conservative pressure-actuated main-steam header relief boundary over the validated F.1 choked-flow seam; no turbine bypass or enthalpy migration is active.

## 1. Authoritative status

```text
M7 gate — COMPLETE / VALIDATED
M8 gate — COMPLETE / VALIDATED
M9 gate — COMPLETE / VALIDATED
M10.1–M10.9.4 — VALIDATED
M10.9.4.1 A–D.4.1 — VALIDATED IN THE CUMULATIVE SOURCE
M10.9.4.1-E.2 Hotfix 1 — VALIDATED
M10.9.4.1-E.3.1 Hotfix 1 — VALIDATED
M10.9.4.1-E.3.2 Hotfix 3 — VALIDATED
M10.9.4.1-F.1 — CURRENT VALIDATED CONTINUATION BASELINE
M10.9.4.1-F.2 — WORKING CANDIDATE, VALIDATION PENDING
```

F.1 compilation, ordinary tests, focused tests and the explicit capacity audit passed locally on 2026-07-26. The reviewed evidence confirms:

- analytic critical pressure ratio `0.5457277338`;
- choked capacity `0.788008677 kg/s` at `100 mm²`;
- linear projections `3.940043384 kg/s` at `500 mm²` and `7.880086767 kg/s` at `1,000 mm²`;
- monotonic mass-flow capacity and a stable choked plateau.

See `M10_9_4_1_F1_CHOKED_STEAM_FLOW.md`, its validation checklist and ADR 0115.

## 2. F.2 candidate contract

F.2 is the first topology consumer of the validated F.1 seam. Both current-v2 sustained profiles declare exactly one optional relief boundary:

```text
id                         header-relief
source node                header
receiver boundary          atmospheric-relief-receiver
receiver pressure          0.101325 MPa
set pressure               6.500000 MPa
full-lift pressure         6.700000 MPa
full-open throat area      1,600 mm²
discharge coefficient      0.95
specific gas constant      461.526 J/(kg K)
heat-capacity ratio        1.3
```

`MainSteamReliefBoundarySolver` reads committed header state, derives stateless pressure lift, limits effective area by committed vapor availability, invokes the F.1 capacity solver and publishes one source-node removal plus matching signed external exchange. `MainSteamNetworkSolver` combines those terms before the existing single plant-network integration.

Legacy/current-v1 definitions retain an empty relief-boundary collection.

## 3. Conservation and phase policy

For relief flow `m_dot` and committed header specific internal energy `u`:

```text
header mass balance          -m_dot
header energy balance        -(u * m_dot)
external mass flow           -m_dot
external power               -(u * m_dot)
```

Vapor availability is:

- `1.0` for superheated vapor;
- committed vapor quality for saturated mixture;
- `0.0` for subcooled liquid or unsupported phase.

This is an ideal-vapor relief seam, not a wet-steam safety-valve correlation.

## 4. Explicit F.2 non-scope

F.2 does not add:

- turbine bypass or condenser steam-dump topology;
- receiver inventory or discharge-pipe dynamics;
- manual relief controls, valve travel, hysteresis or reopen/reset memory;
- alarms or protection functions;
- two-phase critical-flow correlations;
- flow-work or enthalpy transport migration;
- HMI changes;
- a second plant-state integration pass.

## 5. Primary implementation files

```text
src/NuclearReactorSimulator.Domain/Physics/TurbineIsland/MainSteam/
    MainSteamReliefBoundaryDefinition.cs
    MainSteamNetworkDefinition.cs

src/NuclearReactorSimulator.Simulation/Physics/TurbineIsland/MainSteam/
    MainSteamReliefBoundarySolver.cs
    MainSteamReliefBoundarySnapshot.cs
    MainSteamReliefBoundaryStepResult.cs
    MainSteamNetworkSolver.cs
    MainSteamNetworkSnapshot.cs

src/NuclearReactorSimulator.Application/Scenarios/
    PreStartup/ColdShutdownInitialConditionFactory.cs
    Training/DesktopSustainedGenerationInitialConditionFactory.cs
    Synchronization/GridSynchronizationSustainedInitialConditionFactory.cs
```

Focused tests and evidence are owned by:

```text
tests/NuclearReactorSimulator.Domain.Tests/.../MainSteamReliefBoundaryDefinitionTests.cs
tests/NuclearReactorSimulator.Domain.Tests/.../MainSteamNetworkDefinitionTests.cs
tests/NuclearReactorSimulator.Simulation.Tests/.../MainSteamReliefBoundarySolverTests.cs
tests/NuclearReactorSimulator.Application.Tests/.../MainSteamReliefImplementationTests.cs
scripts/run-main-steam-relief-tests.cmd
```

## 6. F.2 validation gate

Run:

```bat
dotnet build
scripts\run-main-steam-relief-tests.cmd
dotnet test
```

Then run the cumulative explicit gates listed in `M10_9_4_1_F2_VALIDATION_CHECKLIST.md`.

Expected ordinary discovery if no unrelated test changes occur:

```text
passed:   981
failed:   0
skipped:  28 explicit
total:    1009
```

The focused audit must create and print:

```text
artifacts/f2-main-steam-relief/
    01-current-v2-header-relief-pressure-sweep.csv
    01-current-v2-header-relief-pressure-sweep.summary.txt
```

Promotion requires user-confirmed compilation, focused gate, ordinary suite and cumulative gates.

## 7. Continuation order

1. Validate F.2 and review its pressure-sweep summary/CSV.
2. F.3: introduce a distinct turbine-bypass path only after F.2 conservation is green.
3. Phase G: migrate steam energy transport from internal-energy-only export to the explicitly designed flow-work/enthalpy contract.
4. Phase H: measure numerical stiffness before selecting adaptive substepping or semi-implicit coupling.
5. Phase I: compatibility matrix, audit consolidation, reference trajectories, CI and limitations closure.

Do not combine F.2, F.3 and Phase G into one candidate.

## 8. Working rules

- Keep validated baseline and candidate identity distinct.
- Preserve one canonical owner for each conserved inventory.
- Read committed state and apply candidate source terms once.
- Keep current-v2 opt-ins isolated from legacy/default definitions.
- Do not tune seeds, protections or thresholds to hide a conservation defect.
- Deliver a complete-project ZIP. Include a `.cmd` application script whenever files must be deleted or renamed.
