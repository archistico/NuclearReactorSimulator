# Structural Plant-Model Stabilization Plan

## Status

M10.9.3 remains the validated baseline. M10.9.4 Hotfix 16 is the current implementation candidate.

This plan records the structural audit triggered by the long-running gameplay journey and by the discovery that the historical turbine-stage flow law made the admission train a monotonic mass accumulator. The governing rule is now:

> Do not compensate a missing feedback law or conservation closure by repeatedly tuning seed temperatures, resistances, volumes, controller biases or fixed boundary powers.

Each structural correction must be isolated, covered by a short invariant/regression test, and only then re-checked by the explicit long-running gameplay journeys.

## A. Turbine expansion hydraulic closure — CURRENT / Hotfix 13–14

### Confirmed defect

The historical stage flow was derived as the minimum of the three upstream valve flows even though those valves already transferred real mass between the intermediate plenums. The turbine source term drained only `turbine-inlet`, giving the combined admission-train inventory the structural tendency:

```text
d/dt(m_stop-out + m_control-out + m_turbine-inlet) = F_stop - F_stage >= 0
```

### Current correction

Current v2 stage definitions use a pressure-driven `turbine-inlet -> exhaust` expansion resistance. M5.4 and M5.5 share one `TurbineStageMassFlowResolver`; legacy `ExpansionResistance = null` is isolated compatibility behavior, not the current physical law.

### Hotfix 14 test correction

The 200-step invariant gate keeps the physically relevant assertions:

- final combined admission-train inventory remains within ±5% of its initial value;
- the trajectory contains at least one negative inventory increment, directly disproving the historical non-decreasing ratchet invariant;
- admission and stage flows remain finite and positive/in-range.

It deliberately does **not** require instantaneous equality `F_admission == F_stage`: with a compressible `turbine-inlet` plenum, their instantaneous difference is exactly what changes that plenum's inventory during a transient. Equality is only a steady-state consequence, not an invariant.


## B. Steam-drum inventory closure — CURRENT / Hotfix 15

### Confirmed defect

The explicit long gameplay journeys passed the short turbine hydraulic invariant but then drove the canonical `drum` node outside the supported water/steam envelope. The historical separator drained exactly the positive return flow that the canonical return pipe had just added, while canonical M4.4 feedwater remained a one-way addition. Therefore the closed-cycle drum inventory obeyed the structural ratchet:

```text
dm_drum/dt = F_feedwater >= 0
```

### Current correction

Current v2 drums use `CirculationDemandBalanced`: liquid recirculation follows committed positive MCP demand, steam remains a separate separator drain and the canonical return/feedwater transports remain owned by `PlantNetworkOrchestrator`. The balance becomes:

```text
dm_drum/dt = F_return + F_feedwater - F_MCP - F_steam
```

Legacy `ReturnSplit` remains an isolated compatibility mode only. ADR 0082 records the decision.

### Direct regression

A direct M3.6 test verifies that current-mode liquid recirculation equals positive committed MCP demand, drum source drain equals steam plus liquid recirculation and internal source terms remain mass-conservative.

## B2. Main-steam source continuity — CURRENT / Hotfix 16

### Confirmed defect

Hotfix 15 removed the forced feedwater accumulator but did not create source-side continuity. In the v2 operating state the drum was compressed liquid and the historical `positive return * vapor mass fraction` law produced zero steam. The turbine therefore consumed only the preloaded steam-outlet inventory while feedwater compressed the drum.

### Current correction

For `CirculationDemandBalanced` drums, the M4.1 main-steam solver supplements return-separated steam up to positive committed main-steam-line demand. The transfer is internal and conservative:

```text
drum             -= (F_supply, u_steam * F_supply)
steam outlet     += (F_supply, u_steam * F_supply)
main-steam pipe  transports the same committed outlet inventory to the header
```

Legacy drums retain the historical law. Direct regressions verify unchanged outlet inventory under matched current-mode demand, equal drum depletion, zero balance residuals and no replenishment for legacy mode. ADR 0083 records the decision.

## C. Condenser pressure/heat-rejection feedback — VALIDATED / Hotfix 17

### Confirmed defect

The capacity-only M4.3 law used `AvailableHeatRejectionPower / specificEnergyDrop` directly as the thermal condensation limit. Installed cooling capacity therefore remained fully available regardless of condenser steam-space temperature, so pressure/vacuum had no direct negative feedback through the cooling surface.

### Current correction

Current v2 condensers publish a positive overall heat-transfer conductance and cooling-boundary inputs publish effective coolant temperature:

```text
ΔT = max(0, T_steam-space - T_coolant)
Q_surface = UA * ΔT
Q_effective = min(Q_available, Q_surface)
```

Condensation is then bounded independently by maximum condenser mass flow, condensable inventory and `Q_effective / Δu`. The current design point uses `UA = 1.225 MW/K`, 20 °C cooling water and the existing 40 °C exhaust initial state, so `Q_surface = 24.5 MW` at initialization and Hotfix 16's starting operating point is preserved.

Null-UA definitions retain capacity-only behavior only as an isolated legacy seam. Cooling-capacity faults scale `Q_available` but preserve coolant temperature. ADR 0084 records the decision.

### Direct regressions

M4.3 tests verify:

- UA can limit heat rejection below installed cooling capacity;
- condensation weakens as steam temperature approaches coolant temperature;
- non-positive ΔT produces zero surface heat transfer/condensation;
- legacy null-UA definitions retain the historical capacity-only law;
- deterministic mass/energy audit closure remains unchanged.

The user confirmed the ordinary suite and both explicit 60-second journeys are green on Hotfix 17. This item is validated as the current condenser-feedback checkpoint.

## D. Generator-grid synchronous coupling — CURRENT / Hotfix 18

### Confirmed defect

The historical M4.5 generator converted requested electrical power into a fixed opposing torque at rated angular speed. Once paralleled, rotor/grid electrical phase and frequency slip did not feed back into electromagnetic torque, allowing an implausibly slow machine to remain connected while MWe simply scaled down with rotor speed.

### Current correction

Current v2 generators opt into `SynchronousGridCouplingDefinition`. Breaker-closed load torque is derived from dispatched mechanical-power demand plus deterministic infinite-bus corrections:

```text
P_phase = P_sync,max * sin(delta)
P_frequency = P_damp@1Hz * (f_generator - f_grid)
P_load = clamp(P_dispatch + P_phase + P_frequency, 0, P_generator,max / efficiency)
T_e = P_load / omega_rated
```

Positive generator lead / positive frequency slip increase electromagnetic loading; negative slip unloads the shaft. Current v2 uses `P_sync,max = 10 MW` and `P_damp@1Hz = 10 MW`. At 50 Hz and zero phase error both corrections are zero, so the validated Hotfix 17 design point is preserved. Null coupling remains only as an isolated legacy seam.

### Direct regressions

M4.5 tests verify:

- phase lead increases and phase lag reduces electromagnetic load;
- a slow paralleled rotor is unloaded while a fast rotor is loaded more strongly;
- legacy null coupling preserves exact historical dispatch torque;
- sustained-generation and pre-synchronization current-v2 seeds explicitly publish the coupling.

Loss-of-synchronism protection is deliberately not mixed into this physics step; it remains part of the later protection-layer expansion after the coupling itself passes ordinary and long-running gates. ADR 0085 records the decision.

## E. Pump non-return behavior — AFTER GENERATOR COUPLING

### Audit result: confirmed

`PumpFlowSolver` permits negative flow through a stopped/running pump because the pump remains a hydraulic resistance path and no discharge check-valve semantic exists. Negative flow can also create non-intuitive hydraulic-power bookkeeping.

### Planned correction

Add opt-in canonical discharge check-valve semantics to the relevant condensate/feedwater pumps and direct reverse-flow regressions. Preserve pumps without check valves where reverse flow is intentionally modeled.

## F. Protection coverage and actuator dynamics — AFTER CORE POWER PATH IS STABLE

### Audit result: confirmed for the current reference recipe

The reference protection definition currently contains only the very-high-pressure reactor SCRAM function. Turbine/generator trip actions exist in the framework but are not comprehensively instantiated in the reference plant. Valve/pump actuator commands are applied directly to requested positions/speeds without travel-rate dynamics.

The current seed/control setup also includes deliberately simplified loops: pressure control can be manual, drum-level setpoint/control scaling is coarse, and turbine speed control remains simplified.

### Planned correction

Add in small validated increments:

- turbine overspeed trip;
- low condenser vacuum / high backpressure protection;
- generator under/over-frequency, reverse-power and loss-of-synchronism protections as appropriate to the educational model;
- realistic actuator travel/ramp limits;
- coherent parallel-operation load/governor mode rather than relying only on isochronous speed control.

## G. Steam-drum/source-side coupling — AUDIT REQUIRED AFTER B–E

### Audit result: partially confirmed

`SteamDrumSeparationSolver` computes separated steam from positive return inflow times the committed drum vapor mass fraction. That law does not directly use turbine demand or a steam-outlet pressure law. However it is too strong to say steam production is completely independent of reactor power/pressure: return flow and committed thermodynamic state can carry indirect coupling.

After the downstream pressure/torque loops are corrected, add conservation/response tests to determine whether a dedicated pressure-dependent boiling/steam-export closure or steam-dump/safety-valve path is still required.

## H. Turbine thermodynamic work model — LATER FIDELITY HARDENING

### Audit result: confirmed simplification

Stage torque currently derives from fixed `NominalSpecificWork * mass flow * efficiency`; the work is not primarily derived from inlet/exhaust thermodynamic states. The solver throws for impossible energy extraction instead of producing a bounded degraded operating state plus diagnostic evidence.

### Planned correction

After hydraulic, condenser and grid feedback are stable, move to a pressure/enthalpy-dependent educational expansion law and explicit degraded/fault diagnostics.

## I. Integration stability / adaptive substepping — CROSS-CUTTING HARDENING

### Audit result: confirmed

`FluidNodeIntegrator` is explicit Euler with depletion/non-finite guards but no per-node fractional-inventory/CFL-style criterion or adaptive substeps.

### Planned correction

Do not use substepping to hide missing physics. After the primary feedback closures above are correct, add a deterministic integration-budget policy based on fractional mass/energy change per substep and regression tests proving same-seed determinism across supported operating envelopes.

## J. Duplication audit

### Resolved

The duplicate turbine-stage admission-flow law in M5.4/M5.5 is removed by the shared `TurbineStageMassFlowResolver`.

### Still to audit

Some subsystems solve hydraulic components for both snapshots and canonical network integration. This is not automatically a bug when both use the same committed state/definitions, but it is a divergence risk. Audit presentation-vs-integration calculations after the structural power-path corrections and consolidate where a single canonical result can be shared safely.

## Validation order

```text
Hotfix 15: drum inventory closure
    -> ordinary suite
    -> explicit 60 s gameplay journeys

then, one structural change at a time:

1. condenser UA*DeltaT pressure feedback — Hotfix 17 validated
2. synchronous generator-grid coupling — NEXT
3. pump discharge check valves
4. protections + actuator travel/ramp dynamics
5. source-side/steam-dump audit
6. turbine thermodynamic work fidelity
7. deterministic adaptive substepping hardening
```

No item advances merely because the long-run lasts longer. Each item needs a short direct invariant/regression test that would fail under the old structural defect.
