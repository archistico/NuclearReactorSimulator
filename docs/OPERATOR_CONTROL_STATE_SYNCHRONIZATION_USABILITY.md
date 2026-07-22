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
## Unified persistent-control feedback

Normal operator controls now distinguish **actual committed state**, **command availability**, and **momentary button press feedback**. A filled button means the published canonical state is actually active; it never means merely that the operator clicked it.

- Reactor rod motion: one of `INSERT`, `HOLD` or `WITHDRAW` remains filled while that motion is the effective committed mode for the selected rod/group. A group whose members differ reports `MIXED`.
- Main-circulation pumps: `START / RUN` is filled while the selected pump is running; `STOP` is filled while it is stopped. The already-satisfied side is disabled.
- Generator breaker: `CLOSE BREAKER` is filled while closed; `OPEN BREAKER` is filled while open. The already-satisfied side is disabled, while close remains blocked/warning when the synchronization permissive is not satisfied.

Because commands are applied at deterministic fixed-step boundaries, `LAST CONTROL ACTION · ACCEPTED` may appear before the persistent fill changes. The fill changes only after a later committed snapshot confirms the new state.

## Momentary speed/load controls

`SPEED LOWER`, `SPEED RAISE`, `LOAD LOWER` and `LOAD RAISE` are not persistent modes. Each accepted command changes a setpoint once. These buttons therefore use a short press-feedback pulse and the last-action status, but intentionally never remain filled or latched. This prevents a visual state from falsely implying that speed/load is continuously being raised or lowered.

