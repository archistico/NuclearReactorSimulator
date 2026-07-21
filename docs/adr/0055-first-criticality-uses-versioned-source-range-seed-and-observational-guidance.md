# ADR 0055 — First criticality uses a versioned source-range seed and observational guidance

- Status: Accepted for M7.3 baseline candidate
- Date: 2026-07-21

## Context

M7.2 establishes a validated cold-shutdown/pre-start recipe with exactly zero modeled neutron population and zero fission power. The validated M2 point-kinetics system is homogeneous and has no explicit external neutron-source term, so an exact zero neutron population cannot leave zero solely because control rods are withdrawn.

M7.3 must support first-criticality training without creating a scenario-local kinetics owner, directly setting thermal power, or adding a hidden stochastic/source model.

## Decision

1. M7.3 introduces a distinct exact-version `pre-criticality-source-range` v1 initial condition.
2. The new factory reuses the M7.2 canonical construction path and changes only explicit initial-condition parameters needed for the M7.2→M7.3 handoff: main circulation is established and the point-kinetics state receives a tiny deterministic non-zero neutron-population seed.
3. The source-range seed is initial-condition data only. It is not represented as an external neutron-source term and must not be described as one.
4. All later neutron-population evolution remains owned by the existing M2 point-kinetics solver; rod motion remains owned by M2/M5.3 command/actuator seams.
5. M7.3 guidance and acceptance checks consume only `ControlRoomSnapshot` and never force rod position, reactivity, power, period, protection or plant state.
6. M7.3 scenario permissions allow controlled rod INSERT/HOLD/WITHDRAW but continue to block generator-breaker closure, turbine-speed control and generator-load control.
7. Quantitative xenon remains explicitly unavailable while M2.8 state is absent from the M5.7 operational envelope. M7.3 records that boundary instead of creating a scenario-local xenon estimate.

## Consequences

- First-criticality progression can occur deterministically through the validated kinetics chain without violating the zero-state property of homogeneous point kinetics.
- M7.2 remains reproducible and unchanged as an exact zero-power cold-shutdown condition.
- M7.3 can teach reactivity/period/low-power control while preserving steam/grid isolation for M7.4+.
- A future explicit external neutron-source model, if desired, requires its own domain/simulation design rather than being hidden in scenario code.
- Quantitative xenon operating guidance remains deferred until canonical xenon state is promoted through the automatic-operation boundary.
