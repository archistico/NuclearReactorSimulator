# ADR 0094 — Current-v2 drum steam source is pressure-, energy- and inventory-driven

## Status

Accepted and locally user-validated for M10.9.4.1-B.2.

## Context

ADR 0083 introduced a temporary current-v2 closure in `MainSteamNetworkSolver`:

```text
F_supply = max(0, F_main-steam-line - F_return-separated-steam)
```

That hotfix was intentionally conservative and kept the validated low-load turbine path alive, but it made steam production follow downstream main-steam demand. The source therefore did not independently answer the physical questions that must own steam generation: is energy available to form vapor, is vapor inventory present, and is there a forward pressure head from the drum to the steam-outlet node?

M10.9.4.1-B.1 separately closed liquid recirculation against physically separable liquid inventory. B.2 now removes the remaining downstream-demand ownership from the current-v2 drum steam source.

## Decision

`SteamDrumDefinition` may optionally own a `SteamDrumSteamSourceDefinition`. Null preserves historical behavior. Current-v2 sustained-operation seeds explicitly provide the definition; v1 seeds remain null.

For the current source closure, one committed step resolves:

1. a liquid reference specific internal energy at the drum state;
2. source-vapor specific internal energy: committed vapor energy for superheated inventory, otherwise saturated-vapor internal energy at the committed drum temperature;
3. positive return-flow energy above the liquid reference, converted to an energy-supported steam-production rate;
4. committed separable vapor inventory available over the integration interval;
5. forward drum-to-steam-outlet pressure capacity from the configured quadratic source resistance;
6. actual steam source as the minimum of pressure capacity and energy/inventory availability.

Conceptually:

```text
m_dot_energy = return-energy surplus / vaporization-energy interval
m_dot_inventory = separable vapor mass / dt
m_dot_available = m_dot_energy + m_dot_inventory
m_dot_pressure = sqrt(max(p_drum - p_steam_outlet, 0) / R_source)

m_dot_steam = min(m_dot_available, m_dot_pressure)
```

The source remains an internal conservative transfer:

```text
drum inventory  : -m_dot_steam, -u_steam * m_dot_steam
steam outlet    : +m_dot_steam, +u_steam * m_dot_steam
```

The canonical main-steam pipe remains the sole transport from the steam-outlet node to the steam header. The source does not inspect main-steam-line demand, turbine load request or valve demand.

Current-v2 seeds use a dedicated source resistance of 100 Pa·s²/kg². This is an explicit source-side hydraulic parameter and is not inferred from downstream demand.

### Energy-model boundary

B.2 deliberately stays inside the simulator's current **specific-internal-energy transport convention**. It does not attempt the broader enthalpy/flow-work migration reserved for the later hardening phase. The new closure therefore removes downstream-demand ownership and closes source mass/energy consistently with the existing network model, without claiming full steam-table or control-volume enthalpy fidelity.

## Diagnostics

Read-only, non-serialized drum diagnostics expose:

- pressure-driven steam-source capacity;
- total energy/inventory-supported availability;
- same-step return-energy-supported steam rate;
- committed separable vapor inventory;
- pressure-limited versus availability-limited state.

These diagnostics do not create a new state owner and do not change replay serialization.

## Compatibility

- `SteamDrumSteamSourceDefinition == null` preserves the historical return-phase-split steam separation law.
- Historical v1 seeds remain null and retain their original topology/physics choices.
- `LegacyReturnSplit` liquid recirculation remains unchanged.
- No protection threshold, timestep, replay schema or turbine/condenser law changes in B.2.

## Consequences

- current-v2 sustained steam export can no longer be created solely because downstream flow asks for it;
- no steam is produced by the current source when there is neither return-energy surplus nor separable vapor inventory, even if a pressure head exists;
- increasing return energy increases source availability monotonically within the supported envelope;
- pressure head can independently limit actual source flow;
- mass and energy remain staged once and integrated by the existing single `PlantNetworkOrchestrator` owner.

## Superseded decision

ADR 0083 remains historical documentation of Hotfix 16, but its demand-following current-v2 source law is superseded by this ADR for profiles that explicitly own `SteamDrumSteamSourceDefinition`.
