# ADR 0083 — Current main-steam demand closes drum supply

## Status

Accepted for M10.9.4 Hotfix 16 candidate.

## Context

Hotfix 15 corrected the historical steam-drum liquid-recirculation ratchet, but the explicit 60-second journeys still exposed two linked failures:

- the current drum remained compressed liquid, so return-flow phase separation generated zero steam;
- the canonical main-steam line continued to consume the finite preloaded steam-outlet inventory while the feedwater pump compressed the drum.

The first reported failure occurred only 5–6 kPa above the critical isobar because the liquid resolver incorrectly rejected any pressure at or above `pcrit`. Removing that artificial gap exposed the real trajectory: drum pressure crossed the 25 MPa SCRAM threshold, and without that protection the finite steam inventory later depleted below the shaft-power acceptance floor.

The main-steam solver is the first owner that knows actual committed source-line demand. Neither seed tuning nor a protection/thermodynamic clamp can close the missing mass and energy path.

## Decision

When a drum uses `SteamDrumLiquidRecirculationMode.CirculationDemandBalanced`, M4.1 computes:

```text
F_supply = max(0, F_main-steam-line - F_return-separated-steam)
```

It contributes equal and opposite internal source terms between the drum inventory and its steam-outlet node. The transfer carries the committed steam-outlet specific internal energy:

```text
drum         : (-F_supply, -u_outlet * F_supply)
steam outlet : (+F_supply, +u_outlet * F_supply)
```

The canonical main-steam pipe remains the sole transport from steam outlet to header. The transfer adds no external mass or power, does not clamp either inventory and reuses the existing single `PlantNetworkOrchestrator` integration boundary.

`LegacyReturnSplit` profiles receive no demand-balanced supply and preserve their historical evolution.

## Consequences

- current v2 steam-outlet inventory no longer depletes merely because return separation is zero;
- drum inventory and energy respond conservatively to actual main-steam demand;
- the 25 MPa protection threshold and water/steam fail-fast behavior remain active;
- historical replay origins remain isolated;
- this educational demand closure is not a detailed pressure/heat-driven boiling, swell/shrink, safety-valve or steam-dump model;
- future source-side fidelity can replace this opt-in closure without changing the main-steam pipe or conserved-state integrator.
