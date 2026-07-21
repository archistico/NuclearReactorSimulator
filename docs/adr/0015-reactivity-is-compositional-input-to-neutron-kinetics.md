# ADR 0015 — Reactivity is a compositional input to neutron kinetics

## Status

Accepted for M2.1 baseline candidate.

## Context

Reactor behavior depends on several independent reactivity mechanisms: control devices, temperature feedbacks, coolant voiding, xenon poisoning and future effects. A common simulation shortcut is to mix these mechanisms directly into power calculations or hide them inside one monolithic coefficient.

That would make diagnostics difficult, couple future models unnecessarily and bypass the neutron-kinetics layer planned for M2.3.

## Decision

1. Reactivity is represented by a strongly typed signed dimensionless `Reactivity` quantity with canonical `delta-k/k` storage.
2. Every source is represented as an explicit named `ReactivityContribution` with a diagnostic category.
3. `ReactivityModel` only validates, canonicalizes and sums contributions.
4. Contribution order is canonicalized before compensated summation so caller enumeration order is not a hidden simulation input.
5. M2.1 does not calculate rod worth, temperature coefficients, void coefficients or xenon dynamics; later milestones own those mappings.
6. M2.1 does not transform total reactivity directly into neutron flux, reactor period, thermal power or gameplay outcomes.
7. Dollars/cents are deferred until the effective delayed-neutron fraction exists in the neutron-kinetics model.

## Consequences

- Every reactivity mechanism remains independently observable and testable.
- Later feedback models can evolve without changing the aggregation boundary.
- Neutron kinetics receives one explicit total reactivity input plus a diagnostic breakdown.
- The simulator avoids a non-physical `reactivity -> MW` shortcut.
