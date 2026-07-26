# Nuclear Reactor Simulator — Project Handoff

> **Current validated continuation:** M10.9.4.1-F.2. **Working source:** M10.9.4.1-F.3 Hotfix 1 CANDIDATE — an internal pressure-actuated turbine bypass from `header` to condenser steam space `exhaust`; F.2 atmospheric relief remains separate and Phase G enthalpy migration is not active.

## 1. Authoritative status

```text
M7, M8, M9 gates — COMPLETE / VALIDATED
M10.1–M10.9.4 — VALIDATED
M10.9.4.1 A–E.3.2 Hotfix 3 — VALIDATED
M10.9.4.1-F.1 — VALIDATED
M10.9.4.1-F.2 — CURRENT VALIDATED CONTINUATION BASELINE
M10.9.4.1-F.3 Hotfix 1 — WORKING CANDIDATE, VALIDATION PENDING
```

F.3 Hotfix 1 corrects the Simulation-layer namespace import for `FluidNodeBalance`; no bypass physics or validation expectation changes.

F.2 passed compilation and all requested tests on 2026-07-26. Its supplied audit confirmed first opening at 6.51 MPa, full lift at 6.70 MPa, 13.531762568 kg/s at 6.80 MPa, 33.595745149 MW energy export, monotonic flow and conservative external exchange.

## 2. F.3 Hotfix 1 candidate contract

Both current-v2 sustained profiles declare one optional bypass:

```text
id                         turbine-bypass
source                     header
destination condenser      condenser
destination steam space    exhaust
set / full-open pressure   6.4 / 6.5 MPa
full-open throat area      1,600 mm²
Cd / R / gamma             0.95 / 461.526 / 1.3
```

`TurbineBypassSolver` reads committed header and condenser steam-space states, invokes the validated F.1 capacity law against actual condenser backpressure and stages equal/opposite internal mass and specific-internal-energy terms. External mass and power remain zero. Equal or higher destination pressure produces zero flow.

F.2 relief remains an independent `header` → atmospheric external-boundary path at 6.5/6.7 MPa.

## 3. Solver sequencing

F.3 is composed inside `CondenserSystemSolver` before the inherited single turbine/main-steam plant-network commit. Condensation is calculated from committed condenser inventory and bypass inflow is staged into the same candidate state, so newly dumped steam becomes available to condensation on the next logical step. Phase H owns any later numerical-stiffness decision.

## 4. Primary files

```text
src/NuclearReactorSimulator.Domain/Physics/TurbineIsland/Condenser/
    TurbineBypassDefinition.cs
    CondenserSystemDefinition.cs

src/NuclearReactorSimulator.Simulation/Physics/TurbineIsland/Condenser/
    TurbineBypassSolver.cs
    TurbineBypassSnapshot.cs
    TurbineBypassStepResult.cs
    CondenserSystemSolver.cs
    CondenserSystemSnapshot.cs

tests/.../TurbineBypassDefinitionTests.cs
tests/.../CondenserSystemDefinitionTests.cs
tests/.../CondenserSystemSolverTests.cs
tests/.../TurbineBypassImplementationTests.cs
scripts/run-turbine-bypass-tests.cmd
```

## 5. Validation gate

```bat
dotnet build
scripts\run-turbine-bypass-tests.cmd
dotnet test
```

Expected ordinary discovery:

```text
passed:   994
failed:   0
skipped:  29 explicit
total:    1023
```

Then run every cumulative gate listed in `M10_9_4_1_F3_VALIDATION_CHECKLIST.md` and review all four artifacts under `artifacts/f3-turbine-bypass`.

## 6. Continuation order

1. Validate F.3 and review source-pressure/backpressure evidence.
2. Phase G: define and migrate the whole-network flow-work/enthalpy convention.
3. Phase H: measure stiffness before choosing substepping or semi-implicit coupling.
4. Phase I: compatibility, reference trajectories, CI and engineering limitations closure.

Do not fold Phase G energy migration into an F.3 hotfix. Keep one canonical owner for every inventory and deliver the complete-project ZIP.
