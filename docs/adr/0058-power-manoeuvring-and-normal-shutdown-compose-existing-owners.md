# ADR 0058 — Power manoeuvring and normal shutdown compose existing owners

**Status:** Accepted / M7.6 validated

## Context

M7.6 must exercise broader load changes and a routine shutdown without turning the scenario layer into a second reactor, turbine, generator or protection controller. The current operational snapshot also exposes temperature and void diagnostics but not quantitative M2.8 xenon state.

## Decision

1. M7.6 starts from exact-version `stable-low-load-parallel-operation` v1 with existing canonical state owners already in a breaker-closed low-load condition.
2. Generator load changes use only `GeneratorLoadRaise/Lower`, which modify bounded M4.5 `RequestedElectricalPower`; no scenario/UI code writes electrical output or electromagnetic torque.
3. Reactor-power manoeuvring and shutdown use existing control-rod intents through M5.3/M2; the scenario never writes reactivity, neutron population or thermal MW directly.
4. Turbine speed/rundown uses the existing M5.4 speed-governing command seam; rotor state remains M4 ownership.
5. Fuel/coolant temperature and void are observed from published presentation diagnostics. Quantitative xenon remains explicitly `Unavailable` until promoted through the canonical M5.7 operational envelope.
6. Normal shutdown is ordered guidance over validated seams: reduce load, unload, open breaker, insert rods, reduce turbine speed and retain main circulation for post-shutdown cooling.
7. SCRAM and turbine/generator trips remain available safety/protection actions, but are not redefined as the routine normal-shutdown mechanism.
8. M7.6 checks and guidance are observational/declarative only and cannot force an objective to pass.

## Consequences

M7.6 can train coordinated operation and shutdown while preserving deterministic ownership boundaries. M7.7 can add training objectives/evaluation over the same snapshots and command history without changing physics.
