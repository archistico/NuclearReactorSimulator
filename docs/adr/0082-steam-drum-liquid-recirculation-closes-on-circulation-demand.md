# ADR 0082 — Steam-drum liquid recirculation closes on circulation demand

## Status

Accepted for M10.9.4 Hotfix 15 candidate.

## Context

The long-running gameplay gate reached a repeatable thermodynamic failure in the canonical `drum` node after the pressure-driven turbine-expansion fix had already passed the ordinary suite and its 200-step admission-train invariant regression.

The historical M3.6 ideal separator used positive channel-return inflow `F_return` and committed drum vapor fraction `x` to emit:

```text
F_steam  = x * F_return
F_liquid = F_return - F_steam
```

The physical return pipe is already integrated by `PlantNetworkOrchestrator` and adds `+F_return` to the drum. The separator source term then removed exactly `F_return` again. When M4.4 later added the canonical feedwater pump directly into the same drum inventory, the drum mass balance became structurally:

```text
dm_drum/dt = F_return + F_feedwater - (F_steam + F_liquid)
            = F_feedwater >= 0
```

Therefore the current closed-cycle operating profile could only accumulate feedwater in the drum. Tuning feedwater-pump speed, drum volume, controller gains or seed temperature could change the time to failure but could not remove the monotonic-accumulator invariant.

## Decision

`SteamDrumDefinition` gains an explicit `SteamDrumLiquidRecirculationMode`.

- `LegacyReturnSplit` preserves the historical M3.6 zero-residence split for isolated legacy compatibility.
- `CirculationDemandBalanced` is the current v2 operating law.

For `CirculationDemandBalanced`:

```text
F_steam  = positive return flow * committed vapor fraction
F_liquid = sum of positive committed MCP flows for the drum's circulation loop
```

The separator drains the drum by `F_steam + F_liquid`, sends `F_steam` to the canonical steam-outlet node and `F_liquid` to the canonical suction header. The physical return pipe and feedwater pump remain canonical `PlantNetworkOrchestrator` transports.

The resulting inventory balance is:

```text
dm_drum/dt = F_return + F_feedwater - F_MCP - F_steam
```

and in normal circulation, where `F_return` approximately follows `F_MCP`:

```text
dm_drum/dt ~= F_feedwater - F_steam
```

This restores the intended level/inventory degree of freedom: feedwater makeup can increase inventory, steam production/export can decrease it, and no sign-only ratchet is imposed by construction.

The new mode is opt-in for the current version-2 sustained-generation and synchronization profiles. Historical profiles remain explicitly legacy rather than constraining the active physical model.

## Consequences

- the drum is no longer mathematically forced to accumulate every kilogram of feedwater;
- main-circulation demand is the canonical liquid recirculation sink rather than an algebraic remainder of return-flow phase split;
- `PlantNetworkOrchestrator` remains the only conserved-inventory integrator;
- separator source terms remain globally mass/energy conservative;
- legacy replay compatibility remains isolated behind an explicit mode;
- further source-side fidelity work is still required: this decision does not by itself create a pressure/heat-driven steam-generation law, safety-valve path, steam dump or detailed swell/shrink model.
