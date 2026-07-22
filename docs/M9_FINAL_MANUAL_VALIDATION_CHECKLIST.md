# M9 Final Manual GUI Validation Checklist

## Final status

**COMPLETE / VALIDATED.** The user confirmed the M9.7 hotfix-5 package compiled and all 760 automated tests passed. The remaining workspace-layout overlap/clipping issue was corrected manually in the supplied `MainWindow.axaml`; the user confirmed the corrected layout works correctly. That file is integrated as the authoritative final M9.7 layout baseline.

Complete this short checklist on the desktop application before promoting M9.7 / the M9 phase gate.

## 1. Startup and workspace navigation

- Application opens without exception.
- Visit `OVERVIEW`, `REACTOR`, `PRIMARY`, `TURBINE`, `ELECTRICAL`, `ALARMS / EVENTS`.
- No blank workspace, clipped critical controls or overlapping panels.

## 2. Runtime controls

- `SINGLE STEP` advances exactly one logical step and passes logical step 5 without a `control-out` thermodynamic failure.
- `RUN` changes the runtime state to running; the indicator beside RUN shows `RUNNING` and the displayed `STEP` value continues to increase continuously for at least 60 simulated seconds / beyond logical step 3111.
- `PAUSE` stops advancement cleanly; the displayed logical step remains stable while paused.
- RUN → PAUSE → RUN resumes advancement from the same committed state; RUN → PAUSE → SINGLE STEP advances exactly one additional step.
- After advancing the simulation, click `Reset session`: the application must return to logical step 0 in `PAUSED` state with the original initial-condition values/history and no residual command/alarm/training history from the discarded run.
- No thermodynamic `Command blocked ... Fluid node ... outside the supported ... envelope` message appears during the default desktop run through at least logical step 6000.

## 3. Target selection and command feedback

- Change at least one available rod/pump/generator/alarm target selector.
- Hover `INSERT`, `HOLD`, `WITHDRAW` and another operational push button: the pointer cursor must appear over the whole rectangle, and clicking near the padded edge must activate the same button as clicking its text.
- Execute one command that is valid in the loaded default training scenario.
- The UI must act on the selected target, not another item.
- A blocked command must produce a clear blocked/unavailable indication rather than silently changing state.

## 4. Protection and alarms

- Verify SCRAM/trip/interlock indications remain visually distinct from ordinary alarms.
- ACK/RESET actions must not clear physical protection state implicitly.
- Unavailable ACK/RESET actions must remain unavailable rather than fabricating success.

## 5. Data quality and unavailable values

- Check several values labelled `MEASURED` and `MODEL`.
- Missing/unavailable data must display an unavailable marker/text, never a fabricated zero.
- Signal-health/alarm counts must remain internally coherent while stepping.

## 6. Resize and usability

- Test normal window, maximized window and a smaller practical size.
- In every workspace (`OVERVIEW`, `REACTOR`, `PRIMARY`, `TURBINE`, `ELECTRICAL`, `ALARMS / EVENTS`), verify the leftmost and rightmost cards are reachable horizontally when needed. Scroll to the final card: its heading and all body text must be fully reachable above the footer/status bar, never clipped behind it.
- Verify important command controls, selectors, trip/alarm indications and scrollable content remain reachable and readable.

## Validation record

```text
Date:
Build / commit or package:
1. Startup/navigation: PASS / FAIL
2. Runtime controls:   PASS / FAIL
3. Targets/commands:   PASS / FAIL
4. Protection/alarms:  PASS / FAIL
5. Data quality:       PASS / FAIL
6. Resize/usability:   PASS / FAIL
Blocking defects:
Notes:
```
