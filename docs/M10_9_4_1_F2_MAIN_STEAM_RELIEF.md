# M10.9.4.1-F.2 — Conservative Main-Steam Header Relief — VALIDATED

## Purpose

F.2 is the first plant-topology consumer of the F.1 compressible steam-flow capacity seam. It adds one automatic relief path from the current-v2 main-steam header to a named atmospheric boundary and integrates the resulting mass and internal-energy export through the existing single plant-network commit boundary.

F.2 is deliberately narrower than a complete steam-dump system. It does not add turbine bypass, condenser mixing, downstream receiver inventory, manual controls, actuator travel, alarms, protection setpoints, two-phase critical flow or enthalpy transport.

## Canonical ownership

`MainSteamReliefBoundaryDefinition` owns:

- the semantic relief id;
- the source main-steam header node;
- the external receiver-boundary id;
- fixed receiver pressure;
- relief set pressure;
- full-lift pressure;
- the validated F.1 `CompressibleSteamFlowDefinition`.

`MainSteamReliefBoundarySolver` reads only committed source-node state. It calculates relief lift, available vapor fraction, ideal-vapor capacity and the signed source terms for the next canonical plant-network integration.

No Application or Avalonia component computes relief flow.

## Current-v2 contract

Both current-v2 sustained profiles declare exactly one relief path:

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

The 1,600 mm² throat is derived from the validated F.1 capacity evidence. F.1 measured 7.880086767 kg/s at 1,000 mm² for the representative 6.2725 MPa / 278.5 °C source. Scaling to 1,600 mm² gives sufficient full-lift authority to cover the approximately 12 kg/s current-v2 steam path while retaining a modest capacity margin at relief pressure.

Historical and legacy profiles keep an empty relief-boundary collection.

## Lift law

The relief is stateless and pressure actuated:

```text
P <= 6.5 MPa          lift = 0
6.5 < P < 6.7 MPa    lift = (P - 6.5) / 0.2
P >= 6.7 MPa          lift = 1
```

No hysteresis or actuator delay is introduced in F.2. Those would require explicit committed valve state and a separate authority decision.

## Phase policy

F.1 is an ideal-vapor capacity law. F.2 therefore limits effective area by committed vapor availability:

- superheated vapor: `1.0`;
- saturated mixture: committed vapor-quality fraction;
- subcooled liquid or unspecified phase: `0.0`.

This prevents the ideal-vapor seam from silently becoming a liquid relief model. It is not a two-phase safety-valve correlation.

## Conservation contract

For relief mass flow `m_dot` from source node `header` with committed specific internal energy `u`:

```text
header mass balance          -m_dot
header energy balance        -(u * m_dot)
external mass flow           -m_dot
external power               -(u * m_dot)
```

The terms are combined with turbine-admission and downstream source terms before the same `IntegratedPrimaryCircuitSolver` / `PlantNetworkOrchestrator` commit. F.2 adds no second inventory integration pass.

## Snapshot evidence

`MainSteamReliefBoundarySnapshot` publishes:

- source and receiver identity;
- source pressure, temperature, phase and vapor quality;
- lift and vapor-availability fractions;
- effective throat area;
- choked state;
- mass-flow rate;
- exported specific internal energy;
- energy-export rate.

`MainSteamNetworkSnapshot` additionally publishes total relief mass flow and total relief energy export.

## Audit

`scripts/run-main-steam-relief-tests.cmd` runs focused domain, simulation and current-v2 integration tests, then produces:

```text
artifacts/f2-main-steam-relief/
    01-current-v2-header-relief-pressure-sweep.csv
    01-current-v2-header-relief-pressure-sweep.summary.txt
```

The pressure sweep covers 6.20–6.80 MPa and verifies:

- closed behavior through the 6.50 MPa set pressure;
- linear lift between set and full lift;
- full lift from 6.70 MPa;
- monotonic mass-flow capacity;
- choked flow for every active atmospheric-discharge sample;
- exact equality between node removal and declared external exchange;
- full-lift capacity above 12 kg/s;
- no turbine-bypass topology.

## Deferred work

F.2 was validated on 2026-07-26. The supplied sweep confirmed first opening at 6.51 MPa, full lift at 6.70 MPa, 13.531762568 kg/s capacity at 6.80 MPa, 33.595745149 MW energy export, monotonicity and conservative exchange. F.3 now adds the separate internal turbine-bypass path. Phase G remains the dedicated owner for flow work and enthalpy transport.
