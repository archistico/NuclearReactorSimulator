# ADR 0113 — Electrical protection thresholds are derived from signed current-v2 trajectories

## Status

Accepted for M10.9.4.1-E.3.1.

## Context

E.2 validates a 10 MWe current-v2 generator with bidirectional infinite-bus coupling. The model can now expose generation, motoring, breaker-open coastdown, frequency slip and generator/grid phase separation. However, the project does not yet have evidence-based pickup levels or delays for reverse power, supervised underfrequency or loss of synchronism.

Adding conventional real-plant relay values directly would be inappropriate because the simulator uses a reduced-order educational machine/grid model rather than stator, field, reactance and network transient equations.

## Decision

Before any E.3 protection is implemented:

1. run deterministic current-v2 normal and abnormal signed electrical trajectories;
2. persist machine-readable CSV evidence;
3. distinguish breaker-open non-eligible underfrequency from breaker-closed abnormal behavior;
4. measure reverse power after prime-mover loss with the electrical request lowered to zero and the breaker closed;
5. measure the reduced-order coupling response to controlled phase offsets;
6. select pickup, reset, delay and supervision only after reviewing those envelopes.

E.3.1 is therefore audit-only. It adds no trip function and changes no existing threshold.

## Consequences

- E.3.2 can be reviewed against reproducible evidence.
- Disconnected rotor coastdown cannot accidentally define a generator underfrequency trip condition.
- Reverse-power protection can include intentional timing clear of normal signed transients.
- Loss-of-synchronism protection remains limited to observables genuinely supported by the reduced-order model.
- Generated files live under ignored `artifacts/` and do not become authoritative source by themselves; the reviewed conclusions must be copied into project documentation.
