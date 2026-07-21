# Integrated Secondary-Cycle Heat Balance

## Scope

M4.6 adds the first top-level first-law reconciliation across the complete manually commanded reactor-to-grid path already implemented by M3 and M4.1–M4.5.

It deliberately adds **no new component physics, conserved inventory, controller or parallel integrator**. `IntegratedSecondaryCycleSolver` delegates physical evolution to the validated `GeneratorGridSolver` stack and then audits the resulting thermofluid, mechanical and electrical domains together.

## Canonical composition

```text
M3 integrated primary circuit
        ↓
M4.1 main steam / admission
        ↓
M4.2 turbine expansion / rotor
        ↓
M4.3 condenser / hotwell
        ↓
M4.4 condensate / feedwater return
        ↓
M4.5 generator / grid
        ↓
M4.6 integrated heat-balance audit
```

`IntegratedSecondaryCycleDefinition` is a canonical top-level composition wrapper over `GeneratorGridSystemDefinition`. It does not duplicate the underlying topology.

## State ownership remains unchanged

M4.6 owns no mutable physical state.

- thermofluid inventories remain in `PlantState` and are integrated exactly once by `PlantNetworkOrchestrator`;
- turbine rotor kinetic state remains in `TurbineExpansionState` and is integrated exactly once by the M4.2 rotor solver;
- generator/grid phase and breaker state remain in `GeneratorGridState` and advance deterministically through M4.5.

The M4.6 step result simply exposes the same candidate states produced by the wrapped M4.5 solve.

## Heat-balance accounting

`SecondaryCycleHeatBalanceAudit` projects the already-audited energy terms into one explicit reactor-to-grid balance:

- nuclear heat input;
- generic plant heat-source power;
- hydraulic pump power transferred into the modeled fluid network;
- feedwater thermal-conditioning power;
- condenser heat rejection;
- turbine shaft-power transfer;
- generator mechanical input;
- electrical export;
- generator conversion losses;
- thermofluid stored-energy change;
- rotor kinetic-energy change.

Turbine shaft work is an **internal cross-domain transfer**. It is removed from the thermofluid domain by M4.2 and added to the rotor mechanical domain exactly once. M4.6 therefore cancels that transfer once when calculating the complete external reactor-to-grid balance.

For the coupled thermofluid + rotor storage boundary:

```text
Δ(E_thermofluid + E_rotor)
=
(P_thermofluid,external + P_turbine,shaft - P_rotor,load) × Δt
```

For the complete reactor-to-grid path, M4.5 reconciles rotor load with electrical export plus generator conversion loss, yielding:

```text
Δ(E_thermofluid + E_rotor)
=
(P_thermofluid,external
 + P_turbine,shaft
 - P_electrical,export
 - P_generator,loss) × Δt
```

All residuals remain signed raw values. No bookkeeping correction is applied.

## Supplemental-power classification

M4.6 also verifies that the thermofluid `SupplementalExternalPower` is explainable by the currently owned subsystem seams:

```text
nuclear heat
+ primary boundary net power
+ feedwater conditioning
- turbine shaft extraction
- condenser heat rejection
```

The resulting classification residual is exposed directly. Under the closed M4 path, the legacy M3 feedwater/steam external boundaries and M4.1 terminal turbine sink are already required to zero by their owning milestones.

## Closed mass loop

M4.6 does not invent a separate steam-cycle mass balance. The authoritative mass audit remains `PlantNetworkAudit` because every steam, condensate and feedwater inventory lives in the canonical plant network.

The integrated snapshot surfaces:

- expected external mass-flow rate;
- mass-closure residual;
- complete nested steam/condensate/feedwater diagnostics.

For the M4.6 closed-loop configuration, expected external mass flow is zero.

## Stability boundary

M4.6 includes deterministic repeated-step verification that the coupled M3 + M4.1–M4.5 stack remains mass-closed and energy-auditable without hidden corrections.

M4.7 builds on this audit with the configurable full-plant reference operating point, long-run drift gate and plant-performance diagnostics documented in `FULL_PLANT_STEADY_STATE.md`.
