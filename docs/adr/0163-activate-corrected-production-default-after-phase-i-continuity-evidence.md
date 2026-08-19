# ADR 0163 — Activate corrected production default after Phase-I continuity evidence

## Status

Accepted / H.30 Requalification 1 user-validated on 2026-08-20.

## Context

Original H.30 selected `OPT-IN ONLY` because exact v3 corrected-commit was technically qualified but H.28 measured it as `bounded-but-costly`. Phase-I later established that exact v2 is not operationally equivalent: 338/338 generation-drop steps coincided one-for-one with targeted stop/control/admission reverse flow, while exact v3 produced zero such events in the matched comparison and remained healthy for 300 s / 30,000 steps.

## Decision

Activate exact v3 `FourNodeBranchContinuityCorrectedCommitOptIn` as the authoritative desktop production default. Preserve exact v2 `ExplicitCommittedState` as fail-closed rollback/reference and keep exact-version persistence identities immutable.

The higher H.28 cost is accepted; it must not be hidden by changing the 10 ms fixed step, weakening numerical tolerances or retuning physics.

## Consequences

- I.3 and current long-running production baselines use the production selector and therefore exact v3.
- exact v2 remains loadable for rollback/reference and historical replay compatibility;
- H.30 original `OPT-IN ONLY` remains historical evidence, not current policy;
- future performance work may optimize v3 only under a separately qualified numerical contract.
