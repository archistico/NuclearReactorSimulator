# M10.9.4.1-F.1 Choked Steam-Flow Capacity Law

**Status:** VALIDATED

**Validated parent:** M10.9.4.1-E.3.2 Hotfix 3

**Validation evidence:** user-confirmed compilation, focused tests, ordinary suite and explicit audit passed on 2026-07-26.

## Purpose

F.1 establishes the numerical capacity seam required before conservative main-steam relief and turbine-bypass topology is introduced. It changes no current plant path. The goal is to prove that one typed solver produces:

- zero one-way flow without forward pressure head;
- continuous subcritical capacity as backpressure changes;
- the analytic critical pressure ratio for an ideal vapor;
- a bounded sonic/choked plateau below that ratio;
- exact linear scaling with effective throat area and discharge coefficient.

## Contract

`CompressibleSteamFlowDefinition` owns:

- full-open throat area;
- discharge coefficient in `(0, 1]`;
- specific gas constant;
- heat-capacity ratio in `(1, 2]`;
- the derived critical downstream/upstream pressure ratio.

`CompressibleSteamFlowSolver` consumes:

- upstream absolute pressure;
- upstream absolute temperature;
- downstream absolute pressure;
- effective-area fraction in `[0, 1]`.

It returns effective area, actual pressure ratio, critical ratio, choked state and non-negative one-way mass-flow capacity. Downstream pressure at or above upstream pressure returns zero rather than reverse flow because the seam is intended for later relief/bypass discharge paths with separate topology and check/isolation semantics.

## Current-v2 representative evidence

The explicit audit sweeps downstream/upstream pressure ratio from 1.00 to 0.00 using:

- upstream pressure: 6.2725 MPa;
- upstream temperature: 278.5 °C;
- full-open throat area: 100 mm²;
- discharge coefficient: 0.95;
- water-vapor specific gas constant: 461.526 J/(kg K);
- heat-capacity ratio: 1.3.

The audit writes:

```text
artifacts/f1-choked-steam-flow/
    01-current-v2-representative-pressure-ratio-sweep.csv
    01-current-v2-representative-pressure-ratio-sweep.summary.txt
```

The validated summary reports:

- analytic critical ratio `0.545728`;
- sampled first-choked ratio `0.540000`;
- choked capacity `0.788008677 kg/s` at `100 mm²`;
- projected capacity `3.940043384 kg/s` at `500 mm²`;
- projected capacity `7.880086767 kg/s` at `1,000 mm²`;
- monotonic flow and a stable choked plateau.

These projections are evidence for later topology sizing, not certified valve sizes.

## Explicit model boundary

F.1 does not add:

- a safety/relief valve;
- a turbine bypass valve;
- a discharge receiver, condenser connection or environment boundary;
- a new `PlantNetworkSourceTerms` path;
- valve opening/travel/controller/protection logic;
- two-phase critical-flow behavior;
- enthalpy or flow-work transport migration;
- HMI controls or alarms.

Those are intentionally separated into later Phase-F and Phase-G increments.
