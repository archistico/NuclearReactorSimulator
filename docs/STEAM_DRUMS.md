# Steam Drums, Separation & Recirculation

M3.6 introduces an aggregated steam-drum layer on top of the validated M3.5 main-circulation system.

## Scope

Each circulation loop owns exactly one semantic `SteamDrumDefinition` in M3.6. The drum does not duplicate fluid inventories: its inventory, steam outlet and liquid recirculation target are canonical `PlantDefinition` fluid nodes.

The loop topology is now allowed to distinguish:

```text
suction header -> MCP -> pressure header -> channel groups -> return collector / drum
                                              ^                         |
                                              |                         |
                                              +---- separated liquid ---+

drum -> separated steam -> steam-outlet node
```

The legacy M3.5 constructor remains backward compatible by using the suction header as the return collector when no dedicated collector is supplied.

## Separation model

`SteamDrumSeparationSolver` is a committed-state, stateless solver. It does not integrate inventories.

For positive committed return flow into the drum:

- subcooled liquid: all separated flow recirculates as liquid;
- saturated mixture: mass split follows the committed vapor quality;
- superheated vapor: all separated flow leaves through the steam outlet;
- unspecified phase: fail fast.

For a saturated mixture, the separated liquid and vapor energy rates use the simplified M1.7 saturation internal energies at the committed drum temperature.

The solver emits `PlantNetworkSourceTerms`:

```text
drum inventory        -(steam + liquid)
steam-outlet node     +steam
suction-header node   +liquid
```

Mass and energy are internal transfers. `ExternalPower` is therefore zero.

## Drum level

`SteamDrumLevelFraction` is a normalized 0..1 diagnostic.

For saturated mixtures, liquid level is derived from:

- committed total drum mass;
- vapor quality;
- saturated-liquid density;
- fixed drum control-volume size.

The model reports a volumetric liquid fraction, not a detailed geometric gauge-height solution. Detailed drum geometry may replace this diagnostic in a later fidelity milestone.

## Deterministic staging

The sequence remains:

```text
committed PlantState
    -> circulation diagnostics from committed state
    -> steam-drum separation source terms
    -> PlantNetworkOrchestrator balance accumulation
    -> one integration per inventory
    -> thermodynamic closure / audit / commit
```

No drum solver mutates `PlantState` directly.

## Intentional deferrals

M3.6 does not yet implement:

- feedwater mass addition;
- exported-steam sink/boundary removal;
- moisture carryover/carryunder correlations;
- separator efficiency maps;
- detailed drum geometry or swell/shrink correlations;
- safety valves;
- turbine coupling.

Feedwater and steam boundaries are M3.7.

## Current closed-cycle inventory closure (M10.9.4 Hotfix 15)

The original M3.6 `LegacyReturnSplit` model treated the liquid outlet as `F_return - F_steam`. Once M4.4 added a real feedwater pump into the same canonical drum inventory, that zero-residence split created a structural closed-cycle ratchet: the physical return pipe and separator return drain cancelled while feedwater remained a one-way mass addition.

Current version-2 sustained-operation profiles therefore use `SteamDrumLiquidRecirculationMode.CirculationDemandBalanced`:

```text
return pipe                 +F_return -> drum
feedwater pump              +F_feedwater -> drum
steam separation            -F_steam -> steam outlet
liquid recirculation        -F_MCP -> suction header
```

so:

```text
dm_drum/dt = F_return + F_feedwater - F_MCP - F_steam
```

The liquid recirculation target remains the canonical loop suction header and its flow is the sum of positive committed MCP flows for that loop. This is still a staged internal transfer: `PlantNetworkOrchestrator` remains the only inventory integrator.

`LegacyReturnSplit` remains available only as an explicit compatibility seam for historical profiles. It is not the preferred current physical closure.


## M10.9.4.1-B.2 current-v2 drum-to-steam source closure — locally validated

B.1 closes the liquid side. B.2, locally user-validated, separately removes the temporary current-v2 rule that replenished the steam-outlet node according to downstream main-steam demand.

Current-v2 sustained-operation profiles now explicitly configure a `SteamDrumSteamSourceDefinition`. The drum source is evaluated from the committed source-side state only:

```text
positive return-flow energy surplus
        +
committed separable vapor inventory
        ↓
energy/inventory-supported steam availability
        ↓  min with
forward drum → steam-outlet pressure capacity
        ↓
actual conservative steam source
```

The source therefore does **not** inspect turbine requested load, main-steam-line demand or valve demand. A forward pressure head is necessary, and pressure capacity alone cannot create steam when neither return energy nor vapor inventory supports it.

The internal transfer remains:

```text
drum inventory  -m_dot_steam, -u_steam*m_dot_steam
steam outlet    +m_dot_steam, +u_steam*m_dot_steam
```

The existing canonical main-steam pipe remains the only transport from the steam-outlet node to the header. Historical profiles with no `SteamDrumSteamSourceDefinition` preserve the earlier return-phase-split behavior.

Read-only diagnostics expose pressure-driven capacity, energy/inventory-supported availability, return-energy-supported production, committed vapor inventory and the active limiting side. These fields are not replay-serialized.

> B.2 remains intentionally within the simulator's current specific-internal-energy transport convention. A future enthalpy/flow-work migration is tracked separately and is not folded into this source-closure step.

## M10.9.4.1-B.3 low-inventory diagnostics and low-low level protection — locally validated

B.3 does **not** change the B.1 liquid-recirculation law or the B.2 steam-source law. It makes low-inventory state explicit and adds current-v2 measured protection semantics only after those source closures have been locally validated.

Read-only, non-serialized diagnostics now expose:

- separable-liquid inventory mass and committed liquid mass fraction;
- committed-liquid depletion;
- all-vapor state with unavailable water/steam separation;
- liquid-recirculation deficit caused by inventory limiting.

For enhanced current-v2 protection profiles, the canonical measured `steam-drum/drum-a/level` channel owns:

- warning alarm below **25%** (`steam-drum-level-low`);
- low-low protection at **10%**, reset eligibility at **20%** (`steam-drum-low-low-level`);
- actions: **ReactorScram + TurbineTrip + GeneratorTrip**.

These are simulator training thresholds, not universal nuclear-plant setpoints. Historical v1/minimal-protection profiles do not receive the new function. The drum-level gauge projects the warning band and low-low protection marker from canonical alarm/protection definitions rather than hard-coding UI thresholds.
