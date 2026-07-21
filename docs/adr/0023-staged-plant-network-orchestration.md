# ADR 0023 — Staged plant network orchestration uses a common committed state

## Status

Accepted for M3 implementation planning.

## Context

Through M2.8 the project validates individual physical primitives and small deterministic couplings. M3 introduces a plant topology where many hydraulic and thermal components can affect the same conserved inventories during one fixed timestep.

A naive sequential implementation would allow component order to change the result:

```text
solve A → update node
solve B → read updated node
```

Reversing A/B would then produce different physics even with identical initial state and commands.

That would violate the project's determinism and replay guarantees.

## Decision

Plant-level network orchestration must use staged committed-state solving.

For each fixed step and conserved network domain:

1. all components read the same committed pre-integration physical state (plus explicitly defined step-effective actuator/command state);
2. component solvers return transfers/balances without mutating shared inventories;
3. all balances targeting each inventory are accumulated deterministically;
4. each conserved inventory is integrated exactly once;
5. derived/thermodynamic closure is resolved from the candidate conserved state;
6. invariants are evaluated before transactional commit;
7. only the committed candidate becomes visible to later fixed steps and snapshots.

Component registration/enumeration order must not be a physical input. Canonical ordering may be used for deterministic arithmetic/diagnostics, but a permutation of equivalent topology definitions must not change the modeled result beyond explicitly documented floating-point tolerance.

## Consequences

### Positive

- prevents hidden component-order dependence;
- preserves transactional runtime semantics;
- makes plant-level conservation audits possible;
- allows solvers to remain stateless/pure where practical;
- supports deterministic replay and parallelization opportunities later;
- makes faults/invariants easier to localize.

### Trade-offs

- algebraic network feedback within the same timestep is not implicitly converged;
- some future high-fidelity models may require an explicit iterative coupled solver;
- such iteration, if introduced, must be a named deterministic solver with fixed convergence semantics, not incidental mutation order.

## Non-decision

This ADR does not freeze the complete neutronic/thermal-hydraulic substage ordering for all future fidelity levels. It defines the invariant rule for shared conserved network state: **gather transfers from a common committed state, accumulate, integrate once, validate, commit**.
