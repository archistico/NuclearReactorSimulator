# ADR 0016 — Control-rod mechanics precede neutron response

## Status

Accepted for M2.2 baseline candidate.

## Context

Control rods have at least three distinct concerns: actuator/mechanical motion, integral neutronic worth, and the dynamic neutron response to the resulting total reactivity. Combining these concerns would make command handling, replay, testing and later plant-specific refinement difficult.

## Decision

Model the concerns as an explicit pipeline:

```text
command -> mechanical position -> rod-worth contribution -> total reactivity -> future neutron kinetics
```

Use normalized withdrawal position (`0 = fully inserted`, `1 = fully withdrawn`) as the canonical position convention. Persist `Insert`, `Withdraw` or `Hold` in rod operational state. Apply group commands as deterministic fan-out over individual rods, with same-step commands processed in FIFO order.

Map position to reactivity through a replaceable worth solver. M2.2 supplies linear and smooth-step integral curves only. Do not let control rods directly modify neutron population, reactor power or thermal state.

## Consequences

- Rod motion can be tested independently of reactor kinetics.
- Replays preserve deterministic command ordering and endpoint behavior.
- Grouping does not duplicate physical rod state.
- Later RBMK-specific worth curves can replace the educational approximation without changing actuator semantics.
- M2.3 remains the sole owner of dynamic neutron response to total reactivity.
