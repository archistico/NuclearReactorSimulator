# ADR 0006 — Transactional step commit and terminal runtime fault semantics

## Status

Accepted for M0.3 baseline candidate.

## Context

Physical simulation will eventually contain tightly coupled numerical models. A failed kernel calculation or violated physical invariant must never leave the runtime appearing to have completed a step that was only partially accepted.

Commands are consumed at step boundaries, so a failed step must also avoid silently losing the commands that caused or accompanied the failure.

## Decision

Each fixed simulation step follows a transactional runtime boundary:

1. create the next immutable step context;
2. drain commands assigned to that boundary;
3. calculate a candidate next state;
4. evaluate all registered invariants against the candidate;
5. create the immutable candidate snapshot;
6. commit the logical clock;
7. publish the candidate state and snapshot together.

If calculation or invariant validation fails before commit:

- the logical clock remains at the last committed step;
- the previously published state and cached immutable snapshot remain current;
- drained commands are restored to the front of the queue in original order;
- the runtime enters `Faulted`;
- stable fault metadata is exposed in snapshots;
- a `SimulationRuntimeFaultException` is raised to the caller.

A faulted runtime is terminal in M0.3. It may be inspected through snapshots but cannot resume, step, accept commands or change speed. Recovery from checkpoints belongs to later persistence/checkpoint milestones.

## State ownership requirement

The runtime can atomically choose whether to replace its state reference, but it cannot undo arbitrary in-place mutations performed by a kernel. Concrete plant state models must therefore follow immutable or copy-on-write semantics across `ISimulationKernel.Step`.

This requirement becomes mandatory for physical state introduced from M1 onward.

## Consequences

- numerical failures cannot silently advance simulation time;
- commands associated with a failed step remain available for diagnostics;
- invariant violations have the same fail-closed semantics as solver faults;
- post-fault inspection is deterministic;
- physical model implementations must not mutate the previously committed state in place.
