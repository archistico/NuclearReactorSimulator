# Integrated Primary-Circuit Baseline

M3.8 is the validated milestone that closes the M3 integration gate by composing the previously validated primary-circuit layers into one deterministic, headless plant step.

## Canonical composition

`IntegratedPrimaryCircuitDefinition` is a semantic top-level definition only. It owns no duplicate physical inventory and follows one canonical lineage:

```text
IntegratedPrimaryCircuitDefinition
        ↓
PrimaryCircuitBoundarySystemDefinition
        ↓
SteamDrumSystemDefinition
        ↓
MainCirculationSystemDefinition
        ↓
FuelChannelGroupSetDefinition
        ↓
AggregatedCoreDefinition
        ↓
PlantDefinition
```

This makes topology identity explicit while preserving the M3.1 rule that the canonical `PlantDefinition` remains the owner of physical components and state IDs.

## One committed state, one integration

`IntegratedPrimaryCircuitSolver` executes the M3 primary-circuit staging in this order:

```text
same committed PlantState
    ├─ aggregated core power projection
    ├─ fuel-channel nuclear heat source terms
    ├─ main-circulation diagnostics
    ├─ steam-drum separation source terms
    └─ feedwater / steam-export source terms
                 ↓
       PlantNetworkSourceTerms.Combine(...)
                 ↓
       PlantNetworkOrchestrator
                 ↓
       exactly one inventory integration
```

The order above is an orchestration order, not a sequence of state mutations. Every subsystem solver reads the same committed state. No subsystem observes another subsystem's candidate state during the same fixed step.

## Integrated inputs

`IntegratedPrimaryCircuitInputs` groups the complete fixed-step inputs required by the M3 integration layer:

- canonical `AggregatedCoreState`;
- total fission thermal power;
- total decay-heat power;
- complete canonical M3.7 feedwater/steam boundary inputs.

The type validates definition identity eagerly. It does not add point kinetics, decay-heat inventory evolution, controllers, or operator commands; those remain outside this M3 primary-circuit integration boundary and provide their already-calculated committed-step inputs.

## Plant-level snapshot

`IntegratedPrimaryCircuitSnapshot` exposes, in one immutable object:

- aggregated core-zone diagnostics;
- fuel-channel-group power/flow/outlet diagnostics;
- main-circulation loop and pump diagnostics;
- steam-drum inventory, phase, level and separation diagnostics;
- feedwater and steam-export diagnostics;
- candidate canonical `PlantSnapshot`;
- `PlantNetworkAudit`;
- total fission, decay and nuclear heat power;
- total feedwater and steam-export mass flow;
- total modeled water/steam mass inventory;
- total stored plant energy.

This is the first snapshot boundary intended to be directly consumable by later M5 instrumentation and M6 control-room projection without moving physics into the UI.

## Reference operating points and long-run verification

`PrimaryCircuitReferenceOperatingPoint` is a configurable fixed-input initial condition:

```text
initial PlantState
+ IntegratedPrimaryCircuitInputs
+ deterministic fixed step size
```

It contains no hidden regulator and no procedural state correction.

`PrimaryCircuitLongRunRunner` repeatedly executes the integrated solver headlessly and reports:

- simulated duration and step count;
- raw total mass-inventory drift;
- raw total stored-energy drift;
- maximum absolute balance mass-rate residual;
- maximum absolute interval mass-closure residual;
- maximum absolute balance-power residual;
- maximum absolute interval energy-closure residual;
- final integrated snapshot/state.

A zero-net-source equilibrium fixture is used as the deterministic long-run regression baseline. Powered/transient configurations are allowed to evolve physically; the runner reports their drift rather than forcing a steady state.

## Conservation boundary

M3.8 does not introduce correction terms to make audits close.

Expected external exchanges remain explicit:

- nuclear heat from the M3.4 channel-group source terms;
- hydraulic pump work from the validated pump solver;
- any canonical heat sources already present in `PlantDefinition`;
- signed feedwater/steam mass and energy from M3.7.

Internal hydraulic transport, heat transfer and M3.6 drum separation must cancel globally according to their existing contracts.

## Deliberate limits

M3.8 does not add:

- turbine admission/expansion;
- condenser, hotwell or closed condensate/feedwater return;
- automatic drum-level, pressure or flow control;
- protection/interlocks;
- new neutronic fidelity;
- licensing-grade two-phase thermal hydraulics.

Those remain subsequent roadmap phases. M4 may replace the temporary M3.7 external boundaries while preserving the M3.8 integration and accounting contracts.

## Diagnostic ownership boundary

M3.8 is the integration boundary for the primary circuit, not a second reactor-physics orchestrator. Its snapshot therefore owns aggregated core thermal power, channel-group, circulation, steam-drum, boundary, plant-inventory and conservation-audit diagnostics.

Validated M2 point-kinetics, reactivity-breakdown and iodine/xenon snapshots remain upstream inputs/diagnostics. M3.8 does not recompute them and does not fabricate per-zone xenon inventories that the validated M3.3 model intentionally deferred. A later full-simulator/application snapshot can compose those immutable diagnostics with this primary-circuit snapshot.


## Validated M3 gate and M4 composition seam

M3.8 has been locally validated and is the official completed M3 baseline.

The integrated primary-circuit solver remains the owner of the one conserved-inventory integration boundary. M4.1 extends it through a backward-compatible supplemental-source-term overload: later plant phases may contribute explicit staged boundary/source terms before integration, but they may not integrate conserved inventories independently.

The original M3.8 call path is unchanged and supplies no additional terms. M4.1 uses the seam for the temporary turbine-admission boundary while canonical main-steam pipes/valves continue to be solved exactly once by `PlantNetworkOrchestrator`.
