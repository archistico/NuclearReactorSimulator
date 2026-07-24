# ADR 0095 — Current-v2 drum low-inventory diagnostics and low-low-level protection

## Status

Accepted and locally user-validated for M10.9.4.1-B.3.

## Context

B.1 closed current-v2 liquid recirculation against physically available liquid inventory. B.2, now locally user-validated, removed downstream-demand-following steam supplementation and made drum steam export pressure/energy/inventory driven. Only after those two source closures are stable is it meaningful to add low-level protection: otherwise the protection would merely hide an unresolved inventory-ownership defect.

The simulator already has a canonical measured drum-level channel (`steam-drum/drum-a/level`), a level-control loop, latching protection functions and alarm semantics. The missing pieces are explicit read-only low-inventory diagnostics and a current-v2 warning/protection contract that remains absent from historical v1/minimal-protection profiles.

## Decision

B.3 does not change the B.1 liquid-recirculation law or the B.2 drum steam-source law.

### Read-only diagnostics

`SteamDrumSnapshot` exposes non-serialized MODEL diagnostics derived from the committed state:

- separable-liquid inventory mass fraction;
- committed-liquid inventory depleted;
- water/steam separation unavailable when the committed drum is all vapor with no separable liquid;
- liquid-recirculation inventory deficit between requested and physically delivered liquid flow.

These diagnostics do not own control or protection decisions and do not alter replay serialization.

### Measured warning and protection

Enhanced current-v2 protection profiles add:

- warning alarm `steam-drum-level-low`: measured drum level < 25%;
- latching protection `steam-drum-low-low-level`: measured drum level <= 10%;
- reset eligibility threshold: 20%;
- actions: `ReactorScram | TurbineTrip | GeneratorTrip`.

The warning and trip thresholds are **simulator training thresholds**. They are not claimed as universal real-plant setpoints.

The protection uses the existing measured `level` channel. MODEL diagnostics remain explanatory evidence only and never become hidden UI-owned protection inputs.

### HMI projection

The drum-level gauge derives its warning band and low-low marker from the canonical alarm/protection definitions. The primary-circuit panel also shows separable-liquid mass, separable-liquid mass fraction and an inventory/separation status string as MODEL diagnostics.

## Compatibility

- historical v1/minimal-protection profiles remain unchanged;
- no B.1/B.2 source equation changes;
- no existing protection threshold is altered;
- no timestep, replay schema, condenser, turbine or generator physics changes;
- new snapshot diagnostics are `[JsonIgnore]`.

## Consequences

- low inventory becomes visible before it reaches a trip condition;
- a fully vaporized drum is explicitly recognizable as having unavailable water/steam separation;
- the operator can distinguish MEASURED level protection from MODEL liquid-inventory diagnostics;
- low-low inventory now has a deterministic current-v2 protective response only after source/inventory closure is established.
