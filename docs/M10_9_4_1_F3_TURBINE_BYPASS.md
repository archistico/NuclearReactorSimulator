# M10.9.4.1-F.3 — Conservative Turbine Bypass to Condenser — VALIDATED

## Purpose

F.3 adds a distinct automatic steam-dump path from the canonical main-steam header to the canonical condenser steam space. It uses the validated F.1 compressible-flow capacity seam, respects committed condenser backpressure and transfers mass plus committed specific internal energy internally before the inherited single plant-network commit.

The F.2 atmospheric header relief remains a separate external boundary. F.3 does not replace, merge with or retune that relief.

## Canonical ownership

The bypass belongs to `CondenserSystemDefinition`, because that subsystem owns both the condenser identity and its steam-space node while retaining access to the upstream main-steam definition.

`TurbineBypassDefinition` owns:

- semantic bypass id;
- source main-steam header node;
- destination condenser id;
- pressure setpoint and full-open pressure;
- validated F.1 `CompressibleSteamFlowDefinition`.

`TurbineBypassSolver` reads only committed plant state. It resolves the destination from the condenser definition, uses the actual committed condenser steam-space pressure as backpressure and stages source terms for the next canonical integration. No Application or Avalonia component computes bypass flow.

## Current-v2 contract

Both current-v2 sustained profiles declare exactly one bypass:

```text
id                         turbine-bypass
source header              header
destination condenser      condenser
destination steam space    exhaust
set pressure               6.400000 MPa
full-open pressure          6.500000 MPa
full-open throat area      1,600 mm²
discharge coefficient      0.95
specific gas constant      461.526 J/(kg K)
heat-capacity ratio        1.3
```

Historical/current-v1 definitions retain an empty bypass collection.

## Opening and capacity law

```text
P <= 6.4 MPa          opening = 0
6.4 < P < 6.5 MPa    opening = (P - 6.4) / 0.1
P >= 6.5 MPa          opening = 1
```

The effective throat fraction is the product of pressure-derived opening and committed vapor availability:

- superheated vapor: `1.0`;
- saturated mixture: committed vapor quality;
- subcooled liquid or unsupported phase: `0.0`.

The validated F.1 solver then resolves subcritical or choked capacity against the committed condenser steam-space pressure. Equal or higher destination pressure yields zero flow; no reverse-flow path exists.

## Conservation contract

For bypass flow `m_dot` and committed header specific internal energy `u`:

```text
header mass balance          -m_dot
header energy balance        -(u * m_dot)
exhaust mass balance         +m_dot
exhaust energy balance       +(u * m_dot)
external mass flow            0
external power                0
```

These terms are combined with condenser phase change, turbine expansion and downstream supplemental source terms before the same single plant-network integration. No second inventory pass or synthetic receiver inventory is introduced.

## Explicit sequencing

F.3 uses committed-state explicit composition. Condensation for a step is resolved from the committed condenser inventory, while bypass inflow is staged into the same candidate-state commit. Newly bypassed steam therefore becomes available to the condensation calculation on the next logical step. This is deliberate and deterministic; Phase H remains responsible for measuring any timestep stiffness before selecting substepping or tighter coupling.

## Snapshot evidence

`TurbineBypassSnapshot` publishes source/destination identity, source thermodynamic state, committed destination pressure, opening, vapor availability, effective area, choked state, mass flow, transferred specific internal energy and internal-energy transfer rate.

`CondenserSystemSnapshot` publishes the exact bypass set and aggregate bypass mass/internal-energy transfer rates.

## Audit

`scripts/run-turbine-bypass-tests.cmd` produces:

```text
artifacts/f3-turbine-bypass/
    01-current-v2-turbine-bypass-source-pressure-sweep.csv
    01-current-v2-turbine-bypass-source-pressure-sweep.summary.txt
    02-current-v2-turbine-bypass-condenser-backpressure-sweep.csv
    02-current-v2-turbine-bypass-condenser-backpressure-sweep.summary.txt
```

The source-pressure sweep covers 6.20–6.60 MPa. The backpressure sweep covers positive pressure ratios 0.01–1.00 and verifies the choked plateau, subcritical decline, zero flow at equal pressure, reverse-flow blocking and exact internal conservation.

## Non-scope

F.3 adds no manual command, actuator travel, hysteresis, bypass controller, HMI control, alarm/protection retuning, wet-steam critical-flow correlation, flow-work term or enthalpy migration. Phase G remains the owner of the whole-network energy-transport convention.


## Validation result

User validation on 2026-07-26 confirmed compilation and all requested tests. The supplied audit reported first opening at 6.41 MPa, full opening at 6.50 MPa, 13.133769551 kg/s at 6.60 MPa, 12.934773043 kg/s choked plateau at 6.50 MPa, first sampled unchoked ratio 0.55, zero flow at equal pressure, exact internal mass/energy closure and zero external exchange. F.3 Hotfix 1 is the validated baseline for Phase G.
