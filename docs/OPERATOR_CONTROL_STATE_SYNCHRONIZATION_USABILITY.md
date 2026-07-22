# Operator control-state and synchronization usability

## Latched protection commands

`SCRAM`, `TURBINE TRIP` and `GENERATOR TRIP` are momentary operator intents that latch canonical M5.5 protection state. They are not ON/OFF toggles.

When clear, the corresponding pushbutton has a transparent background and remains available. When latched, the button uses a filled trip-colored background, an `— ACTIVE` label and is no longer commandable. The latched state remains visible until canonical reset succeeds.

## Protection reset

`RESET PROTECTION` is the same canonical `ProtectionReset` command wherever it is presented. It is exposed in the reactor, turbine and electrical protection areas for discoverability; there is still only one protection-reset owner.

The UI and F4 COMMANDS project the same M5.5 function reset-safety and reset-permissive status:

- `RESET AVAILABLE` when all currently published reset conditions are satisfied;
- `RESET NOT READY` / `RESET BLOCKED` with canonical blocker identifiers when a function remains active/unsafe or a permissive is unsatisfied;
- `PROTECTION CLEAR` when there is no latched reactor/turbine/generator trip.

## Synchronization

With the generator breaker open, the UI shows `SYNC READY` or `SYNC NOT READY` and the canonical M4.5 close-check differences for frequency, phase and voltage against their configured limits.

With the breaker closed, the synchronization lamp changes to `PARALLELED` / normal. The pre-close synchronization window is no longer shown as an operator warning because the synchronization decision has already been completed.

## Operator action plan

The Overview now exposes:

- current grid/protection/output condition;
- the next action from the loaded canonical procedure/training progression;
- a non-automatic cold-shutdown-to-first-output command map composed from the validated M7.2–M7.5 guidance plans.

Hidden and ChecklistOnly training-assistance modes continue to suppress step-by-step recommendations.
