# Plant Network Orchestration

## Scope

M3.2 introduces the first plant-level execution pipeline over the canonical topology/state boundary established by M3.1.

The milestone adds no new hydraulic or thermal equations. It composes the already validated pipe, valve, pump, heat-transfer, heat-source, fluid-node and thermal-body primitives.

## Staged step contract

Every network step follows one deterministic staging rule:

```text
Committed PlantState
        ↓
GATHER canonical topology + state
        ↓
SOLVE all passive/active connections
using only committed endpoint states
        ↓
ACCUMULATE node/domain balances
        ↓
INTEGRATE each conserved inventory once
        ↓
THERMODYNAMIC CLOSURE for each fluid node
        ↓
BUILD candidate PlantState
        ↓
AUDIT global mass/energy accounting
        ↓
return candidate + diagnostics
```

No component is allowed to mutate a shared node before every other component for the same logical step has been solved.

## Component staging order

The implementation uses a fixed canonical category order:

1. passive pipes;
2. valves;
3. pumps;
4. heat-transfer links;
5. external heat sources;
6. fluid-node integration;
7. thermal-body integration;
8. conservation audit.

Within each category, `PlantDefinition` already supplies canonical ordinal-ID order.

The category order is an orchestration implementation detail, not a physical sequential update: every solver reads the same committed state. Only balance accumulation is sequential, and addition order is deterministic.

## Balance ownership

Hydraulic components produce `FluidNodeBalance` values.

Thermal links and heat sources produce `ThermalEnergyBalance` values. When a thermal domain is a fluid node, thermal power is mapped to the fluid node's `NetEnergyRate` with zero mass flow.

The orchestrator never directly edits mass or energy inventories while solving components.

## Single integration rule

After all component balances have been accumulated:

- every `FluidNodeState` is passed to `FluidNodeIntegrator` exactly once;
- every `ThermalBodyState` is passed to `ThermalBodyIntegrator` exactly once;
- valve, pump and heat-source operational states are carried forward unchanged in M3.2.

Future actuator/control milestones may evolve those operational states before network solving, but must preserve the same committed-state staging rule.

## Conservation audit

`PlantNetworkAudit` records:

- initial/final total fluid mass;
- net accumulated mass rate;
- declared/expected external mass flow rate;
- balance mass-rate residual;
- interval mass closure residual;
- initial/final stored energy across fluid and thermal inventories;
- net accumulated energy rate;
- pump hydraulic power exchange;
- enabled external heat-source power;
- signed supplemental external power;
- expected external power;
- balance-power residual;
- final energy closure residual.

Internal passive hydraulic transport, heat-transfer links and conservative staged transfers must cancel globally. Pump hydraulic work and heat sources are explicit external energy crossings. From M3.7 onward, feedwater/steam boundaries additionally declare signed external mass flow and signed external power rather than hiding source/sink exchange inside node balances.

The audit is diagnostic rather than a hidden correction mechanism. M3.2 never edits state to force conservation closure.

## Determinism requirements

M3.2 verifies:

- parallel components read identical committed endpoint state;
- caller registration order cannot alter physical results;
- every fluid thermodynamic closure is evaluated once per fluid node per step;
- runtime pulse segmentation does not alter final state;
- global mass and energy residuals remain inspectable.

## Deferred work

The original M3.2 milestone deliberately did not add core zones, channel groups, main circulation, steam drums or external mass boundaries; those layers were subsequently added through M3.7 while preserving this orchestration contract. M3.8 adds the top-level integrated primary-circuit solver that composes those staged contributions and still delegates the one-and-only conserved-inventory integration to this orchestrator.

Automatic actuator/control logic and plant-specific configuration remain outside the generic M3.2 network orchestrator.

## Higher-phase source-term composition from M4.1

M4.1 preserves the same orchestration boundary. `IntegratedPrimaryCircuitSolver` now has a backward-compatible overload that accepts additional staged `PlantNetworkSourceTerms` from later plant phases before invoking `PlantNetworkOrchestrator`.

The original three-argument M3.8 API still supplies `PlantNetworkSourceTerms.Empty`, so validated M3 behavior is unchanged. `MainSteamNetworkSolver` uses the higher-phase seam only for the temporary turbine-admission boundary. Main-steam pipes and stop/control/admission valves are already canonical plant-network components and therefore must not also be emitted as supplemental balances.

This distinction prevents double counting:

```text
canonical pipes / valves -> solved once by PlantNetworkOrchestrator
explicit external terminal boundary -> staged supplemental source terms
all contributions -> one conserved-inventory integration
```

## Thermofluid-to-mechanical composition from M4.2

M4.2 extends the same staged-source pattern without adding another fluid/thermal inventory integrator.

`MainSteamNetworkSolver` now has a backward-compatible overload accepting higher-phase `PlantNetworkSourceTerms`. `TurbineExpansionSolver` uses it only for the semantic turbine component that is not an ordinary canonical pipe/valve:

```text
turbine inlet fluid node
    -- mass + inlet energy --> lumped turbine expansion
    -- same mass + residual energy --> exhaust fluid node
    -- energy difference --> mechanical shaft domain
```

The inlet/exhaust mass terms cancel exactly. Their energy terms sum to negative shaft power, which is explicitly declared to the thermofluid audit. Rotor state is integrated separately as mechanical state, and `TurbineMechanicalAudit` verifies the corresponding shaft-work / kinetic-energy / external-load balance.

This is not double integration: `PlantNetworkOrchestrator` remains the sole owner of every fluid/thermal conserved inventory, while M4.2 is the sole owner of rotor mechanical state.

## Condenser composition from M4.3

M4.3 adds another staged-source layer without adding another fluid/thermal state integrator.

For each condenser, committed-state calculations produce:

```text
steam-space node:  - condensation mass flow, - steam energy flow
hotwell node:      + condensation mass flow, + condensate energy flow
external boundary: 0 mass, - rejected heat power
```

`CondenserSystemSolver` passes these terms into the backward-compatible `TurbineExpansionSolver` supplemental-source seam. Turbine shaft-work terms, condenser terms, M4.1 terms and all M3 terms are combined before the same single `PlantNetworkOrchestrator.Step(...)` call.

Therefore M4.3 does not integrate exhaust, condenser or hotwell inventories separately. Condenser pressure/vacuum diagnostics are resolved from the candidate exhaust-node state produced by that single integration.

## M4.4 canonical feedwater pumps and supplemental conditioning

M4.4 deliberately reuses the pumps already registered in `PlantDefinition`. Their mass/energy balances and hydraulic work are therefore solved and audited by the existing `PlantNetworkOrchestrator` exactly once.

The M4.4 semantic solver only adds bounded feedwater-conditioning energy as supplemental source terms before the same integration. A backward-compatible overload on `CondenserSystemSolver` carries those terms through the existing M3/M4 composition chain.


## M4.5 electrical coupling

M4.5 adds no new thermofluid inventory integrator. Generator/grid state is separate electrical state, while electromagnetic torque feeds the existing M4.2 rotor integrator. The `GeneratorGridSolver` preserves the higher-phase supplemental `PlantNetworkSourceTerms` seam when it delegates to M4.2, so later M4 composition still reaches one `PlantNetworkOrchestrator` call for all conserved fluid/thermal inventories.
