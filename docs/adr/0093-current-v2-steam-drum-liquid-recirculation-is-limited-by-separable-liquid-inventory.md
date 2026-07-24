# ADR 0093 — Current-v2 steam-drum liquid recirculation is limited by separable liquid inventory

## Status

Accepted and locally user-validated for M10.9.4.1-B.1; compilation and requested tests passed.

## Context

`CirculationDemandBalanced` recirculation historically followed positive main-circulation pump outflow demand. That closes the hydraulic demand at the design point, but it can request a liquid recirculation source even when the committed steam-drum state contains no separable liquid.

In a fully vaporized drum, continuing to label pump-demand flow as recirculated liquid fabricates a liquid source and breaks the physical meaning of the separator. Near dryout, an integration step must also not remove more liquid than the committed separable liquid inventory plus same-step incoming separated liquid can support.

The historical `LegacyReturnSplit` profile is a compatibility path and must retain its existing behavior.

## Decision

For current-v2 `CirculationDemandBalanced` operation:

- derive committed separable liquid inventory from drum phase and vapor quality;
- calculate requested recirculation from positive pump outflow demand as before;
- cap the integrated recirculation rate by same-step incoming liquid plus committed separable liquid inventory divided by the integration interval;
- force actual liquid recirculation to zero when no separable liquid exists;
- publish diagnostic requested flow, inventory-supported maximum flow, separable liquid mass and inventory-limited state;
- keep the legacy `LegacyReturnSplit` rule unchanged.

Production integration supplies the deterministic integration interval. Compatibility/diagnostic overloads without an interval retain the historical instantaneous demand result except for the physically impossible fully-vapor case, which always yields zero liquid recirculation.

## Consequences

M10.9.4.1-B.1 closes only the liquid-recirculation inventory boundary. It does not yet replace the current demand-following drum-to-main-steam supplement; that source-law closure remains B.2.

Required regression evidence includes:

- fully vaporized current-v2 drum cannot fabricate liquid recirculation;
- near-dryout current-v2 recirculation cannot exceed same-step available liquid;
- ordinary demand-balanced operation remains unchanged when inventory is sufficient;
- legacy return-split behavior remains unchanged;
- source-term mass and energy closure remains exact.
