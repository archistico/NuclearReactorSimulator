# ADR 0092 — Current-v2 sustained-generation seed closes solid-to-coolant heat transfer and primary circulation

## Status

Accepted from user-validated local evidence on 2026-07-24.

## Context

The first extended 300-second operating-envelope audit eventually tripped the turbine and generator. Initial attribution focused on condenser backpressure, but the repeated trip was a downstream symptom rather than the root cause.

The current-v2 sustained-generation seed deposited only the direct coolant share of reactor power into the coolant. Fuel and structure retained the remaining thermal power without conservative return links to the coolant. At the same time, current-v2 primary hydraulic resistances left circulation at roughly 0.07 kg/s while the steam-drum/main-steam path exported roughly 13 kg/s. The resulting long-horizon energy and hydraulic mismatch slowly depleted the drum's available internal energy and drove the plant away from its intended sustained point.

Historical v1 seeds and protection thresholds are compatibility contracts and must not be retuned to hide this defect.

## Decision

For the current-v2 sustained-generation profile:

- fuel and structure return heat conservatively to the coolant through explicit thermal links;
- primary-circuit hydraulic resistances are matched to the intended current-v2 circulation regime;
- initial steam-line pressures and control-valve bias are aligned with the resulting operating point;
- historical v1 seed behavior remains unchanged;
- protection thresholds remain unchanged.

The correction is a seed/integrated-balance closure, not a thermodynamic-resolver relaxation and not a protection-threshold change.

## Consequences

The user validated locally:

- exact 300-second sustained 5 MWe journey: passed;
- explicit 60-second synchronization journey: passed;
- build: 0 warnings, 0 errors;
- ordinary suite: 895 passed, 11 explicit tests skipped by the ordinary filter, 0 failures.

The earlier condenser-headroom candidate remains a separate model decision to be judged in the dedicated condenser phase-change phase; it is no longer recorded as the root-cause fix for the long-run failure.
