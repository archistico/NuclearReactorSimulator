# ADR 0096 — Current-v2 condenser condensate energy is pressure-resolved saturated liquid

## Status

Accepted and locally user-validated for M10.9.4.1-C.1 together with B.3.

## Context

The historical condenser law transfers condensed mass from the turbine exhaust steam space into the hotwell while assigning that incoming mass the hotwell's already-committed specific internal energy. This is numerically conservative, but it makes condensation thermally neutral to the hotwell inventory: adding condensate cannot change hotwell specific energy because the incoming mass is defined to have exactly the same specific energy as the receiving inventory.

That historical rule also makes the phase-change energy drop depend directly on the seed value of hotwell specific internal energy. The extended audit identified this as a structural limitation that must be closed before condenser capacity/headroom is judged.

The project deliberately defers the system-wide internal-energy-to-enthalpy/flow-work migration to Phase G. Phase C therefore needs a narrower control-volume correction that improves condenser phase change without changing generic pipe transport.

## Decision

1. `CondenserDefinition` gains an explicit `CondenserCondensateEnergyMode`.
2. The default is `LegacyHotwellSpecificInternalEnergy`, preserving historical fixtures, v1 seeds and replay behavior.
3. The two sustained current-v2 operating seeds explicitly select `SaturatedLiquidAtSteamSpacePressure`.
4. In that mode, condensed mass enters the hotwell with saturated-liquid specific internal energy evaluated at the committed condenser steam-space pressure.
5. Steam-space energy removal remains `u_steam * m_dot`; hotwell energy addition becomes `u_condensate * m_dot`; rejected heat is the difference and remains an explicit external sink. Mass and energy are integrated exactly once by the canonical network boundary.
6. The generic `IFluidThermodynamicModel` contract is not widened. Components needing saturation properties use the optional `IWaterSteamSaturationPropertyProvider` capability, implemented by `SimplifiedWaterSteamThermodynamicModel`.
7. Condensation-limit diagnostics expose maximum-flow, inventory and thermal limits/margins separately. Cooling-boundary diagnostics distinguish installed-capacity and surface-`UA` limits and only call them active when the effective heat-rejection capacity is actually exhausted.
8. The A.2 values (40 MW installed cooling ceiling, 20 kg/s maximum condensation flow, 1.225 MW/K `UA`, 20 °C cooling water) are not retuned in C.1. Their independent necessity remains an evidence question after the energy closure is validated.

## Consequences

- Current-v2 hotwell energy can now respond to the thermodynamic state of incoming condensate instead of being mathematically neutral to condensation.
- The thermal condensation limit no longer divides by a permanently seed-owned hotwell energy state in current-v2.
- Legacy definitions remain exact by default.
- This ADR does not introduce enthalpy transport, flow work, circulating-water-system dynamics, non-condensables or a new condenser pressure law.
- The user subsequently confirmed the cumulative B.3 + C.1 source compiles and tests pass locally; both checkpoints are therefore locally validated.
