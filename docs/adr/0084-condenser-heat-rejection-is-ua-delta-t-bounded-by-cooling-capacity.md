# ADR 0084 — Condenser heat rejection is UA·ΔT bounded by cooling capacity

## Status

Accepted for M10.9.4 Hotfix 17 implementation candidate.

## Context

The historical M4.3 condenser converted `AvailableHeatRejectionPower` directly into a condensation-flow ceiling by dividing that constant power by the committed steam-to-hotwell specific-energy drop. That made installed cooling capacity behave as an immediately usable heat sink independent of condenser steam-space temperature. Condenser pressure/vacuum therefore lacked a direct negative feedback through the cooling surface.

The structural stabilization plan requires the condenser to behave as a surface heat exchanger: available cooling-system power is an upper capacity ceiling, while actual transferable heat must also depend on the steam-to-coolant temperature difference.

## Decision

`CondenserDefinition` may publish an optional positive `OverallHeatTransferConductance` (UA). When present, M4.3 computes:

```text
ΔT = max(0, T_steam-space - T_coolant)
Q_surface = UA * ΔT
Q_effective = min(Q_available, Q_surface)
```

The thermal condensation-flow limit is then derived from `Q_effective / Δu`, where `Δu` is the committed steam-space-to-hotwell specific-internal-energy drop. Existing mass-flow and inventory limits remain independent bounds.

`CondenserCoolingBoundaryInput` carries both the external cooling-capacity ceiling and effective coolant temperature. Cooling-capacity faults scale only the power ceiling and preserve coolant temperature.

Current sustained-generation and synchronization v2 seeds use a design point of 24.5 MW at 40 °C steam-space / 20 °C cooling water, therefore `UA = 1.225 MW/K`. This preserves the Hotfix 16 initial operating point while adding negative feedback away from that point.

A null UA remains an isolated legacy capacity-only law for older definitions; it is not the current reference-plant condenser model.

## Consequences

- As condenser steam temperature approaches cooling-water temperature, heat rejection and condensation decrease continuously toward zero.
- Installed cooling capacity no longer implies forced heat removal independent of thermodynamic driving force.
- Condenser backpressure/vacuum can participate in a real feedback loop with the pressure-driven turbine expansion law.
- Cooling faults remain replaceable boundary-capacity effects rather than alternate condenser physics.
- Direct regressions verify UA limitation, monotonic weakening with falling ΔT, zero transfer at non-positive ΔT and legacy isolation.
- The ordinary suite and explicit 60-second gameplay journeys remain required before promotion.
