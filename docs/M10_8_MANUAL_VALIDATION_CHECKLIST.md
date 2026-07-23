# M10.8 Manual Validation Checklist — Integrated Operator Computer UI

**VALIDATION RESULT: PASSED / M10.8 VALIDATED.** The user confirmed local compilation and the complete automated suite passed; M10.8 is now the baseline beneath M10.9.1.

Use this checklist after a clean build and complete automated suite.

## A. Global terminal navigation

- [ ] From Overview, press F1: Computer opens on GUIDANCE.
- [ ] Repeat F2–F8: each key opens the correct page.
- [ ] Exactly one page-selection indicator is visible at a time.
- [ ] Mouse clicking each page button selects the same page as the corresponding F-key.
- [ ] Merely changing pages does not advance the logical step or change plant state.

## B. Fixed workstation shell

- [ ] Header/menu remain visible while scrolling a long page.
- [ ] Runtime, logical step, alarms, signal health and protection summary remain visible while scrolling.
- [ ] Footer status/keyboard-help remains visible while scrolling.
- [ ] Active-page title/state are readable and not clipped.

## C. Keyboard-only operation

Without using the mouse after opening the Computer workspace:

- [ ] F1–F8 selects every page.
- [ ] Tab / Shift+Tab reaches all interactive controls on COMMANDS, MODES and SESSION.
- [ ] COMMANDS list supports Up/Down selection.
- [ ] Enter dispatches only the selected available typed command.
- [ ] Blocked/unavailable commands remain fail-closed.
- [ ] SESSION buttons can be focused/activated by keyboard; native file dialogs remain operable by keyboard.

## D. COMMANDS layout

- [ ] Catalog rows remain readable at the minimum supported window size.
- [ ] No horizontal overlap/clipping requires a hidden scroll hack.
- [ ] Selected command detail and availability/block reason remain readable.
- [ ] Executing a command still uses canonical runtime validation.

## E. SESSION layout

- [ ] START RECORDED SESSION remains available.
- [ ] CREATE CHECKPOINT, VERIFY REPLAY, SAVE ARCHIVE, LOAD ARCHIVE and RESTORE SELECTED remain reachable.
- [ ] Checkpoint list is readable without forcing an excessively wide center workspace.
- [ ] Save/load/restore semantics remain replay-backed and verified.

## F. M10.7.1 regression checks

- [ ] SCRAM / TURBINE TRIP / GENERATOR TRIP show persistent ACTIVE state only when latched.
- [ ] RESET PROTECTION shows AVAILABLE or a real canonical blocking reason.
- [ ] Breaker closed shows PARALLELED rather than stale SYNC WARNING.
- [ ] Breaker open shows Δf / Δphase / ΔV synchronization details.
- [ ] INSERT/HOLD/WITHDRAW show committed rod motion state.
- [ ] START/RUN/STOP show committed pump state.
- [ ] CLOSE/OPEN BREAKER show committed breaker position.
- [ ] SPEED± and LOAD± remain momentary and show explicit accepted/blocked last-action feedback rather than staying latched.

## G. Acceptance

M10.8 may be marked VALIDATED only when:

1. clean restore/build succeeds with warnings-as-errors;
2. complete automated suite passes;
3. this manual checklist passes without material overlap/clipping/keyboard-navigation regressions.

After validation, update authoritative docs to `M10.8 VALIDATED` and continue in a new chat with M10.9.
