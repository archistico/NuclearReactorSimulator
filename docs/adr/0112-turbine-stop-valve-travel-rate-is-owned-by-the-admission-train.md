# ADR 0112 — Turbine stop-valve travel rate is owned by the admission train

## Status

Proposed / M10.9.4.1-D.4.1 candidate.

## Context

D.4 introduced persistent typed STOP OPEN/CLOSE requests and finite normal travel while protection retained later authority to force the effective stop valve closed. The initial implementation obtained the STOP travel rate from the normal control-valve actuator because both current-v2 rates happened to be equal.

That dependency assigned a physical property of the stop/isolation valve to a different valve and controller path. It also made future independent travel calibration impossible without changing unrelated control-valve configuration.

## Decision

`TurbineAdmissionTrainDefinition` owns an optional `StopValveTravelRate`.

- Current versioned sustained-generation and synchronization profiles declare the STOP rate explicitly.
- Runtime STOP OPEN/CLOSE requests copy the rate from the selected admission train.
- The control-valve actuator is no longer consulted for STOP travel.
- `null` preserves the historical instantaneous movement contract for legacy definitions, independently of other secondary-valve travel configuration.
- The optional operational-seed factory parameter is appended at the end of the public signature so positional source compatibility is preserved.
- Protection remains a later arbitration layer and may force effective closure without rewriting the persistent operator target.

## Consequences

- STOP and ADMISSION/CONTROL travel may be calibrated independently.
- Existing current-v2 behavior remains unchanged because the explicit STOP rate equals the previously borrowed rate.
- Historical definitions remain source compatible and preserve their prior instantaneous behavior.
- Deterministic replay/checkpoint behavior includes the same requested and effective positions because the versioned initial-condition definition reconstructs the same travel contract.
- Any future STOP travel-rate change must be versioned with the owning initial-condition/profile definition and validated through replay plus trip/reset regressions.
