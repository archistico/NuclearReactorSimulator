# ADR 0030 — Primary-circuit external boundaries declare signed mass and energy

## Status

Accepted for M3.7.

## Context

M3.6 closes drum separation internally, but the pre-M4 primary circuit still needs feedwater entering the plant and steam leaving it. Treating those exchanges as ordinary unlabelled node balances would make inventory changes possible without an explicit declaration of what crossed the modeled system boundary.

The M3.2 audit already distinguishes explicit external power from conservative internal energy transfers, but it did not yet carry an equivalent declared external mass flow and previously constrained supplemental external power to non-negative source behavior.

## Decision

M3.7 models feedwater and steam export as committed-state staged source terms over canonical plant fluid nodes.

- Feedwater adds a controllable non-negative mass flow and explicit specific internal energy to the associated drum inventory node.
- Steam export removes a controllable non-negative mass flow from the associated canonical steam-outlet node.
- Exported specific internal energy is read from the committed outlet state.
- `PlantNetworkSourceTerms` carries signed external mass flow and signed external power.
- Positive sign means net transfer into the modeled plant; negative sign means net export.
- `PlantNetworkAudit` compares accumulated node mass rate against declared external mass flow, in addition to the existing external-power comparison.
- `PlantNetworkSourceTerms.Combine(...)` composes independently solved staged contributions before the single M3.2 integration boundary.

## Consequences

- feedwater/steam exchange is explicit and inspectable rather than hidden in bookkeeping;
- internal M3.6 separation remains distinguishable from true plant-boundary exchange;
- mass imbalance in supposedly conservative source terms becomes observable through a balance-rate residual;
- steam export can correctly declare negative external power;
- M4 can replace the simplified source/sink boundaries with turbine/feedwater components without changing the plant-wide conservation contract;
- boundary flow controls remain external inputs for now, not automatic control logic.
