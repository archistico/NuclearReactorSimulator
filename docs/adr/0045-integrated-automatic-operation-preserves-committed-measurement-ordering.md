# ADR 0045 — Integrated automatic operation preserves committed-measurement ordering

## Status

Accepted; M5.7 locally validated and M5 gate complete.

## Context

M5.1–M5.6 establish separate owners for instrumentation, normal control, physical actuators, protection and annunciator memory. A full automatic-operation baseline must compose them over many deterministic steps without allowing controllers/protection to read candidate true state, without integrating physical state twice and without turning verification scenarios into hidden physics.

## Decision

M5.7 introduces a thin integrated automatic-operation envelope and solver with this ordering:

1. the committed `MeasuredSignalFrame` drives M5.3/M5.4 control and M5.5 protection;
2. protection arbitration acts through existing canonical seams;
3. the existing protected M4.7 path performs one physical step;
4. M5.6 alarm memory observes the same committed measured frame and resulting protection snapshot;
5. M5.1 instrumentation observes the candidate immutable full-plant snapshot and produces the measured frame committed for the next step.

Verification uses explicit finite phases with immutable input bundles. Criteria are observational only and cannot alter state.

## Consequences

- No algebraic loop between candidate plant truth and current-step controller/protection decisions.
- Instrument lag/fault behavior remains physically meaningful across repeated automatic steps.
- One physical integration remains authoritative.
- Setpoint changes, disturbances and protection-matrix cases can be verified headlessly without introducing a general scenario scheduler.
- M6 can consume stable measured/control/protection/alarm snapshot boundaries without implementing simulation logic in the UI.
