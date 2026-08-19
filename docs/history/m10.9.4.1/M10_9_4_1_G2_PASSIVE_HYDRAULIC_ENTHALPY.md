# M10.9.4.1-G.2 — Passive Hydraulic Enthalpy Migration & Pump Work Ownership

> **Validated as M10.9.4.1-G.2 Hotfix 2 on 2026-07-26.** This document records the G.2 scope before G.3 migrated pump paths and the remaining non-turbine owners.

## Purpose

G.2 performs the first runtime migration after the validated G.1 convention audit. It changes only passive pipe and valve-path advection in the two current-v2 sustained profiles from specific internal energy to open-control-volume specific enthalpy.

The validated G.1 relation remains authoritative:

```text
specific flow work = p / rho
specific enthalpy  = h = u + p / rho
advective energy   = h * m_dot
```

Fluid-node inventories continue to store mass and internal energy. Internal transfers remain equal and opposite.

## Versioned ownership

`PipeDefinition.EnergyTransportMode` owns the connection-level convention:

```text
SpecificInternalEnergy  historical default
SpecificEnthalpy        Phase G opt-in
```

The default preserves every legacy definition and every constructor call that predates G.2.

The two current-v2 sustained factories opt in all canonical passive pipes and all valve hydraulic paths. Their pump hydraulic paths deliberately remain `SpecificInternalEnergy` until the dedicated pump-path migration decision in G.3.

## Runtime result contract

`PipeFlowResult` now publishes both diagnostic conventions and the selected applied rate:

- `InternalEnergyFlowRate = u * m_dot`;
- `FlowWorkRate = (p/rho) * m_dot`;
- `EnthalpyFlowRate = h * m_dot`;
- `AdvectedEnergyFlowRate`, selected by the definition mode;
- equal-and-opposite endpoint balances built from the selected rate.

Valve results preserve the wrapped pipe mode. Main-steam line and valve snapshots expose the selected mode, flow work and applied advected rate so presentation/diagnostics do not mislabel enthalpy transport as internal-energy transport.

## Pump work boundary

G.2 does not migrate the three current-v2 pump paths. The existing pump contract remains:

```text
passive path advection              u * m_dot
hydraulic fluid work                Delta_p_active * volume_flow
fluid-network net external power    hydraulic fluid work
shaft demand                        hydraulic fluid work / efficiency
```

Hydraulic fluid work is applied exactly once to the actual downstream node. Shaft demand is reported separately and is not injected into the fluid a second time. The generic pump solver is nevertheless verified to preserve single counting if an explicit future definition selects enthalpy mode.

## Explicit exclusions

G.2 does not migrate:

- pump-path advection in current-v2;
- steam-drum separation/source boundaries;
- feedwater and steam-export boundaries;
- condenser phase-change transport;
- F.2 atmospheric relief;
- F.3 turbine bypass;
- turbine expansion or shaft-work accounting;
- heat-transfer or external-boundary power;
- protections, HMI controls, replay schemas or checkpoint formats.

Those component groups remain G.3 and G.4 work.

## Hotfix 1: stability envelope, not runtime retuning

The first complete ordinary run after the intentional passive enthalpy migration produced 2949.3997837402485 rpm at ten simulated seconds, with 5.549828464853323 MW shaft power, finite positive torque, valid electrical generation and no turbine trip or overspeed. The historical 2950 rpm lower bound therefore represented an over-tight pre-migration test edge rather than a failed operating state.

Hotfix 1 changes only the Application-level stability assertion to 2940–3050 rpm and explicitly verifies no trip and no overspeed. The 2940 rpm lower bound is 49.0 Hz, still above the 48.8 Hz underfrequency pickup. No controller, turbine, generator, protection or transport parameter is changed.

## Evidence

The explicit audit writes:

```text
artifacts/g2-passive-hydraulic-enthalpy/
    01-current-v2-passive-enthalpy-transport.csv
    02-current-v2-pump-work-ownership.csv
    01-current-v2-passive-enthalpy-and-pump-work-ownership.summary.txt
```

It records every current-v2 passive pipe and valve path, the selected convention, `u*m_dot`, flow work, `h*m_dot`, applied energy and exact endpoint closure. It separately records all pump paths, hydraulic power, shaft demand and single-count residuals.
