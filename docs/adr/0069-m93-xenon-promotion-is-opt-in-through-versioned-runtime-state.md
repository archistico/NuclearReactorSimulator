# ADR 0069 — M9.3 Xenon Promotion Is Opt-In Through Versioned Runtime State

## Status

Accepted. M9.3 was subsequently locally validated with the complete automated suite.

## Context

M2.8 validated a deterministic history-dependent I-135/Xe-135 state model, but M5.3/M5.7 did not carry that state. M6/M7 therefore correctly exposed quantitative xenon as unavailable.

M9.3 needs restart-after-shutdown and low-power poisoning scenarios. Reconstructing xenon in Application/scenario/UI would create a second physics owner. Enabling new poison dynamics inside existing version-1 initial conditions would also silently change the semantics of exact-version M7/M9.1 replay identities.

## Decision

1. `ReactorPrimaryControlSystemDefinition` gains an optional canonical `IodineXenonDefinition`.
2. `ReactorPrimaryControlState` carries `IodineXenonState`; non-empty poison state requires the definition to be configured.
3. `ReactorPrimaryControlSolver` invokes the existing M2.8 solver and composes committed xenon reactivity through the explicit non-rod seam before point kinetics.
4. Operational presentation reads only immutable M2.8 snapshot data and never reconstructs poison state.
5. Existing validated M7 v1 initial conditions remain xenon-disabled and continue to expose xenon as unavailable.
6. New M9.3 versioned initial conditions explicitly opt into poison state and configuration.
7. Scenario definitions may seed prior-history inventories but may not encode future xenon/reactivity/power trajectories.

## Consequences

- M2 remains the sole iodine/xenon physics owner.
- M5/M6 gain an architecture-correct observability seam without UI physics.
- Existing exact-version scenario/replay semantics are preserved.
- M9.3 scenarios can express history-dependent poisoning challenges from state rather than scripts.
- A future model-parameter/version migration must introduce a new initial-condition/configuration identity rather than silently changing old replay semantics.
