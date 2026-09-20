# M10 Final VR2 R3 - Energy-Transport Ownership Repair Implementation 1

NRS-MARKER:R3-ENERGY-TRANSPORT-REPAIR-IMPLEMENTATION1

Date: 2026-09-20

Status: **CANDIDATE / IMPLEMENTATION FAST-GATE / R3 REMAINS RED**

## Authorized repair

Planning 1 returned `PASS-AS-AUTHORED` with Family B selected:

`repair-owner=ACTIVE-CLOSURE-TRANSPORT-PROPERTY-CONTRACT`

This implementation moves only saturated liquid/vapor **transport-property ownership** to the active water/steam closure. It does not change the public saturation provider, void fraction, drum-level geometry, C4 payload, raw seed, thresholds, default closure mode or canonical Exact-V9 definition.

## Production surface

NRS-MARKER:R3-ENERGY-TRANSPORT-REPAIR-SURFACE

Exactly five production paths are changed, matching Planning 1:

1. add `IWaterSteamPhaseTransportPropertyProvider.cs`;
2. extend `ReferenceConsistentTabulatedInverseResolver` with pressure-keyed dense-saturation transport interpolation;
3. expose the active closure transport capability from `SimplifiedWaterSteamThermodynamicModel`;
4. inject and consume the capability in `SteamDrumSeparationSolver` only for current steam-source transport properties;
5. wire the capability in `IntegratedPrimaryCircuitSolver`.

All other `src/` files remain frozen.

## Mode behavior

NRS-MARKER:R3-ENERGY-TRANSPORT-MODE-BEHAVIOR

Modes 0 and 1 map the new transport capability to the same historical temperature-keyed forward saturation properties used before this repair and must remain IEEE-754 bit-identical.

Mode 2 uses the already-loaded immutable `NRSVR2C4.v1.bin` dense saturation table. Lookup is pressure-keyed binary search plus linear interpolation of saturated liquid/vapor specific volume and internal energy. Density is derived as `1 / specificVolume`. Resolve-time resource I/O and payload decode counts remain zero and the steady-state lookup must allocate zero managed bytes.

At the frozen Diagnostic 3 drum point (`6,416,459.281680372 Pa`, `553.15 K`), the payload interpolation gives a liquid enthalpy transport of approximately `1,236,671.000703454 J/kg`, matching the frozen independent IF97 reference within the required `0.001 J/kg` and the frozen raw mode-2 suction transport within `0.001 J/kg`.

## Consumption boundary

NRS-MARKER:R3-ENERGY-TRANSPORT-CONSUMPTION-BOUNDARY

`SteamDrumSeparationSolver.ResolveSaturatedMixture` remains on the existing historical model for void fraction and level geometry. Only `ResolveSteamSource` obtains saturated liquid/vapor internal energy and density from the injected transport capability before constructing `u`, `p/rho`, enthalpy and advected transport energy.

This closes the proven source/destination transport-ownership discontinuity without copying suction state into the drum or adding a mode-2 conditional at the consumer.

## Fast-gate qualification

NRS-MARKER:R3-ENERGY-TRANSPORT-IMPLEMENTATION-FAST-GATE

The local implementation gate requires:

- static production-surface/frozen-tree audit PASS;
- build PASS;
- focused transport provider tests PASS;
- existing reference-consistent seed-integration 100-step gate PASS with zero envelope violations;
- Exact-V9 Determinism Contract V2 audit PASS;
- complete ordinary local suite PASS.

The 100-step envelope remains frozen at 10 ms per step: electrical `4.99..5.01 MWe`, primary-pump flow `99.9..100.1 kg/s`, drum level `0.49..0.51`, governor `29.27..29.30%`, with zero trip, breaker opening, rollback or non-finite values.

## Authority

A local PASS authorizes only:

`R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFICATION3`

R3 remains RED until that 120 s / 12,000-step qualification returns and is adjudicated PASS. No retuning is authorized if this fast gate is RED.
