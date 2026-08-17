# M10.9.4.1-G.3 — Remaining Non-Turbine Enthalpy Migration

> **Status: VALIDATED.** User-confirmed focused, ordinary and cumulative gates passed; supplied audit maximum ownership residual: 0 W.

## Purpose

G.3 completes the non-turbine portion of the open-control-volume energy migration established by validated G.1 and begun at runtime by validated G.2 Hotfix 2.

The accepted relation is unchanged:

```text
specific flow work = p / rho
specific enthalpy  = h = u + p / rho
advected power     = h * m_dot
```

Canonical fluid nodes continue to conserve mass and internal energy. G.3 changes only the energy carried across selected current-v2 open-control-volume paths.

## Versioned scope

Every new transport-mode argument defaults to `SpecificInternalEnergy`. This preserves historical constructor behavior and legacy/current-v1 trajectories.

The two current-v2 sustained profiles opt these owners into `SpecificEnthalpy`:

- all pump hydraulic paths;
- steam-drum steam separation and liquid recirculation;
- external feedwater and steam-export boundaries;
- temporary turbine-admission boundaries;
- condenser steam removal and condensate addition;
- F.2 atmospheric main-steam relief;
- F.3 internal header-to-condenser turbine bypass.

Passive pipes and valve paths were already migrated by G.2.

## Shared energy selector

`FluidEnergyTransport` centralizes deterministic calculation of:

```text
p/rho
h = u + p/rho
selected specific energy
selected energy rate
```

It rejects non-positive density and undefined transport modes. Solvers retain separate diagnostics for internal energy, flow work, enthalpy and the selected applied rate.

## Pump ownership

G.3 migrates current-v2 pump-path advection to `h*m_dot`. It does not fold pump work into enthalpy transport.

The existing pump contract remains:

```text
path advection                 selected upstream energy * m_dot
hydraulic fluid work           active pressure rise * volume flow
fluid-network net work         hydraulic fluid work exactly once
shaft demand                   hydraulic fluid work / efficiency
```

The G.2 audit becomes cumulative and now verifies that all current-v2 pipes, valves and pump paths use enthalpy while hydraulic work and shaft demand remain separate.

## Steam-drum separation

The drum definition owns its transport mode. For current-v2:

- incoming positive return energy is evaluated with each return pipe's selected mode;
- steam and liquid reference states expose `u`, `p/rho` and `h`;
- steam production support uses the selected vaporization-energy interval;
- the drum inventory loses the sum of selected steam and liquid advected rates;
- steam outlet and suction header receive equal selected rates;
- separation mass and energy residuals remain exactly zero.

The drum inventory itself remains an internal-energy inventory.

## External primary and admission boundaries

Steam export and turbine-admission sinks derive enthalpy from the committed source state. Their signed external power equals the selected energy removed.

A positive current-v2 external feedwater input must provide explicit specific enthalpy because the upstream external pressure/density state does not exist inside the plant graph. Zero-flow and historical inputs remain source-compatible.

## Condenser phase change

The condenser uses the selected convention on both sides of condensation:

```text
steam energy removed       = h_steam * m_dot
condensate energy added    = h_condensate * m_dot
heat rejection             = removed - added
```

Current-v2 condensate continues to use the validated saturated-liquid-at-steam-space-pressure state. Heat rejection remains one explicit external sink and is not injected a second time into either node.

## Relief and bypass

F.2 relief remains an external boundary:

```text
header mass/energy balance   negative
external mass/power          matching negative values
```

F.3 bypass remains an internal transfer:

```text
header                       -m_dot, -h*m_dot
condenser steam space        +m_dot, +h*m_dot
external exchange             zero
```

Both snapshots preserve `u*m_dot`, flow work and selected advected-energy evidence.

## Explicit exclusions

G.3 does not migrate or retune:

- turbine expansion;
- turbine shaft-work extraction;
- rotor/generator coupling;
- governor or valve-controller tuning;
- protections and thresholds;
- HMI commands or presentation semantics;
- replay/checkpoint contracts;
- numerical integration order or timestep.

Turbine expansion and exact shaft-work ownership remain the isolated G.4 task.

## Audit artifacts

The explicit audit writes:

```text
artifacts/g3-remaining-non-turbine-enthalpy/
    01-current-v2-remaining-non-turbine-enthalpy.csv
    01-current-v2-remaining-non-turbine-enthalpy.summary.txt
```

Rows cover pump paths, drum steam/liquid paths, active external boundaries, condenser steam/hotwell paths, forced-open relief and forced-open bypass. Each row records selected mode, mass flow, internal-energy rate, flow-work rate, applied advected rate, ownership residual and declared external power.
