# M8.7 — Safety-Response Scenario Pack

## Purpose

M8.7 is an Application/training composition milestone. It adds no new physical fault model, protection rule, controller or conserved state owner. Instead it composes validated or candidate M8 fault definitions with the M7.7 deterministic training/evaluation framework so abnormal-event response can be assessed without scripting a physical outcome.

The pack contains three capstone exercises:

1. **Protection Fail-Safe Response** — reuses the M8.3 unavailable protection-measurement diagnostic.
2. **Large Break-Class Safety Response** — reuses the bounded M8.5 educational pressure-driven break.
3. **Station Blackout-Class Safety Response** — reuses the explicit M8.6 external-supply/pump/control/turbine/generator fault composition.

M8.5 and M8.6 are still stacked, unvalidated candidates at the time M8.7 is created. Therefore M8.7 is also a stacked candidate and cannot be promoted independently.

## Deterministic acceptance criteria

`SafetyResponseCheckpointEvaluator` consumes only committed `ControlRoomSnapshot` presentation state. Supported checks cover:

- exact declared-fault lifecycle becoming `Active`;
- invalid measured-signal presence;
- canonical reactor SCRAM or any canonical trip state;
- canonical generator-breaker isolation;
- annunciated alarms and alarm acknowledgement state.

Checkpoints retain the M7.7 first-achievement semantics and prerequisites. Evaluation never mutates plant state, clears protection, changes fault lifecycle or substitutes true-state values for missing instrumentation.

Each capstone exercise has a 100-point `ScenarioTrainingPlan` split across:

- recognition of the initiating condition;
- verification of protection/isolation response;
- disciplined operator response.

Acceptance criteria observe what the validated owners actually publish. They never force a trip, breaker outcome, pressure trajectory, inventory trajectory or recovery state to make an exercise pass.

## Protection/control response verification

M8.7 verifies behavior through the same canonical seams used by earlier milestones:

- M8.3 instrumentation faults flow through M5.1 `MeasuredSignalFrame` and M5.5 protection;
- M8.5 break effects remain conservative M3 source terms;
- M8.6 electrical-loss/SBO-class consequences remain explicit M4.5/M8.2/M8.3/M8.4 effects;
- M5.5 remains the sole protection/interlock owner.

The safety-response layer does not inject protection latches or controller outputs.

## Operator-action timeline

`SafetyResponseEvaluationSession` exposes the existing M7.7 `ScenarioOperatorActionJournal` as an immutable logical timeline alongside the current training assessment.

Only scenario-gated commands actually accepted by the runtime command boundary appear in the timeline. Every entry retains:

- monotonic logical sequence;
- logical step;
- typed `ControlRoomCommand`.

Wall-clock timestamps and UI refresh cadence are deliberately absent. Automatic protection events remain visible through committed snapshots/fault/alarm state rather than being misrepresented as operator actions.

## Fidelity boundary

M8.7 inherits all fidelity limits of its source scenarios. In particular:

- the large-break exercise is educational and not licensing-grade LOCA analysis;
- the station-blackout-class exercise does not imply modeled AC/DC buses, diesels, batteries, ECCS electrical trains or quantitative decay-heat coastdown;
- no severe-accident or fuel-damage progression is introduced.

M8.7 is therefore a deterministic training/debrief composition layer, not a new accident-analysis code.
