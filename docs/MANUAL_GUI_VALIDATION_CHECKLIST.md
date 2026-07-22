# Manual GUI Validation Checklist

Use this short checklist after `dotnet build --no-restore` and `dotnet test --no-build` are fully green.

> M9.6 note: this checklist originated the pre-M10 GUI validation requirement. The authoritative final phase-gate checklist is now `M9_FINAL_MANUAL_VALIDATION_CHECKLIST.md`, which is limited to capabilities exposed by the current desktop composition.

## 1. Startup and navigation

- Launch the desktop app and confirm there are no startup exceptions, blank window or broken styles.
- Open each workspace: Overview, Reactor, Primary, Turbine, Electrical, Alarms.
- Confirm selected-workspace title/content changes correctly and no previous workspace content visually overlaps the new one.

## 2. Run/pause/single-step

- From PAUSED, press **Single step** several times: logical step must increase exactly once per click and the UI must remain responsive.
- Press **Run**, let the plant advance briefly, then **Pause**: values must update while running and stop advancing after pause.
- Confirm no obvious flicker, frozen controls or stale selected-target text.

## 3. Canonical controls and target selection

- Select available rod/group, pump and generator targets; change selection and verify displayed target IDs follow the selector.
- Exercise one safe command in each available area (for example pump start/stop in an appropriate scenario, rod hold/motion where allowed, turbine speed/load/breaker control in the correct scenario).
- Confirm blocked operations report a clear status and do not silently change plant state.

## 4. Protection and alarms

- In a suitable test/scenario, verify SCRAM/trip/interlock indication is visually obvious and affected normal commands become unavailable.
- Trigger/use an alarm-capable scenario: verify active/unacknowledged state, ACK, returned state and RESET behavior.
- Confirm alarm ACK/RESET does not clear a physical protection latch.

## 5. Data quality and provenance

- Confirm invalid/unavailable values display as unavailable rather than fabricated numbers.
- Spot-check that values labeled **MEASURED** and **MODEL** remain visually distinguishable by their labels.
- For an M9.3 xenon-enabled seed, verify xenon is shown; for an older xenon-disabled seed, verify the UI explicitly reports it unavailable.

## 6. Resize/readability smoke test

- Resize the window through small, normal and maximized sizes.
- Confirm text is readable, controls are not clipped in normal/maximized use, scrolling works where needed, and critical trip/alarm states remain visible.

Record any failure with: scenario/initial condition, logical step, workspace, action performed and screenshot if visual.
