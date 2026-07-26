# Reference Plant Scale Contract

## Status

**M10.9.4.1-E.2 HOTFIX 1 — VALIDATED**

The user confirmed compilation and all requested gates passed on 2026-07-26. The current-v2 sustained-generation and synchronization profiles validly implement the accepted 10 MWe educational scale and opt into bidirectional generator/grid coupling. Historical/default profiles preserve their previous 1,000 MWe and generation-only behavior.

## Current-v2 candidate contract

- generator nameplate: **10 MWe**;
- normal sustained request: **5 MWe**, equal to 50% of nameplate;
- requested-load envelope: **0–10 MWe**;
- LOAD RAISE / LOWER increment: **5 MWe**, clamped by the active generator definition;
- rated speed / rotor inertia: **3,000 rpm / 1,000 kg·m²**;
- stored rotor energy at rated speed: approximately **49.348 MJ**;
- inertia constant at 10 MWe: approximately **4.934802 s**;
- full-load governor reference rise: **1.5 rpm**, preserving the already validated 0.75 rpm displacement at 5 MWe;
- maximum synchronizing correction: **0.5 MW**, deliberately retained pending dynamic evidence;
- frequency damping at one hertz slip: **2 MW/Hz**, deliberately retained pending dynamic evidence;
- coupling mode: **Bidirectional** for the two current-v2 sustained profiles;
- signed electrical convention: positive = generation/export, negative = motoring/import;
- signed mechanical convention: positive = shaft power absorbed by generation, negative = power delivered by the grid to the shaft;
- conversion loss remains non-negative in either direction;
- current-v2 electrical scales: **-10..+10 MWe**.

## Compatibility contract

Historical and default definitions remain unchanged unless they opt in explicitly:

- default generator nameplate remains **1,000 MWe**;
- a null coupling retains the historical dispatch-torque-only model;
- `SynchronousGridPowerFlowMode.GenerationOnly` remains the coupling default;
- public/manual `TurbineRotorInput` still rejects negative torque;
- only the generator/grid integration layer may create signed electromagnetic rotor torque;
- historical presentation scales remain non-negative;
- no old initial-condition identity is rewritten.

## Power limits

For a generator with electrical nameplate `Pmax` and efficiency `η`:

- maximum generating shaft absorption is `Pmax / η`;
- maximum motoring shaft delivery is `Pmax × η`;
- resulting electrical exchange is bounded to `-Pmax..+Pmax`;
- conversion loss is `mechanical exchange - electrical exchange` and remains positive in both directions.

In bidirectional mode, power-to-torque conversion uses the current rotor speed with a 10% rated-speed floor. Generation-only and null-coupling paths retain their historical rated-speed conversion behavior.

## Deferred protection

E.2 represents signed states but does not add reverse-power, supervised-underfrequency or loss-of-synchronism protection. E.3.1 records the required trajectories; E.3.2 must derive protection thresholds from their reviewed evidence.
