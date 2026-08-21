# ADR 0107 — Generation-ready condenser cooling is capacity, not forced inventory depletion

## Status

Accepted — legacy/foundational decision retained; this ADR predates the explicit status-heading convention.

- Status: Accepted for M10.9.4 Hotfix 11 implementation candidate

## Context

The historical M4.3 condenser interprets available heat rejection as a thermal limit on inventory condensation. That behavior is preserved for existing definitions and replay-compatible v1 seeds. In the new sustained-generation v2 operating points, however, a fixed cooling capacity slightly above instantaneous turbine exhaust supply slowly drained the finite exhaust steam-space. The 10-second gate passed, while 60-second gameplay journeys eventually left the simplified thermodynamic envelope.

## Decision

`CondenserDefinition` gains a backward-compatible, default-off option to limit condensation to incoming effective turbine-stage exhaust mass flow. Generation-ready v2 definitions enable it. Actual condensation is the minimum of canonical condenser maximum flow, condensable inventory, thermal capacity and incoming effective turbine exhaust supply. Unused cooling capacity remains unused.

Historical/v1 definitions keep the prior behavior exactly by leaving the option disabled. No UI layer derives or overrides this physics rule.

## Consequences

- Continuous generation cannot silently consume condenser steam-space inventory merely because installed cooling capacity exceeds instantaneous turbine exhaust flow.
- Cooling-boundary power remains a maximum available sink, not a mandatory draw.
- Historical replay/initial-condition semantics remain unchanged unless the exact versioned definition opts in.
- Long-running gameplay tests remain mandatory before promoting the v2 operating point.
