# ADR 0121 — Accident progression is causal, persistent and separate from alarm priority

## Status

Accepted on 2026-08-16 as future architecture direction. Implementation remains deferred until after the active H.4 numerical gate, any evidence-authorized integration/optimization, and Phase I hardening.

## Context

The simulator already supports deterministic faults, trips, leaks/LOCA-class scenarios, blackout-class composition, alarms, protection, replay and post-incident analysis. It does not yet model a general persistent equipment-integrity layer, severe core-damage progression, fires or explosion mechanisms.

The gameplay goal is to allow aggressive off-design operation and meaningful accidents without replacing plant physics with scripted outcomes or adding interlocks merely to keep the solver alive.

## Decision

1. Accident consequences must arise from explicit modeled mechanisms and canonical plant state.
2. Functional equipment state and physical integrity are separate concepts.
3. Future damage should generally accumulate from severity × exposure duration rather than from a single arbitrary threshold when the component physics supports such treatment.
4. Physical damage persists across alarm acknowledgement and ordinary protection reset.
5. Incident severity is a separate axis from alarm/annunciator priority.
6. Fires, ruptures, explosions or severe core damage may only be introduced when their initiating physical mechanism and required state variables are represented.
7. Integrated post-trip decay heat and Phase-H extreme-state numerical evidence are prerequisites for credible severe core-damage progression.
8. Damage and accident transitions must be deterministic, checkpoint/replay compatible and visible in post-incident evidence.

## Rejected alternatives

- Scripted `if threshold then explosion` gameplay.
- Treating every critical alarm as a severe physical incident.
- Clearing physical damage with alarm/protection reset.
- Adding operational interlocks solely because a solver path cannot tolerate the state.

## Consequences

Future work needs an explicit integrity/damage model, severity evidence, extreme-envelope audits and persistent incident state. This increases implementation effort but preserves the simulator's core advantages: causal explainability, determinism, replayability and testability.
