# M10.9.7.3 Hotfix 2 REV2 manual validation checklist

Use this checklist only after `dotnet build`, `dotnet test` and `scripts\run-m10973-desktop-host-session-integrity-audit.cmd` are green.

## 1. Existing MISSION baseline remains intact

- Start the desktop application normally.
- Confirm the validated `MISSION` workspace is still present and normal startup remains `NO ACTIVE MISSION / UNBOUND`.
- Confirm COMPUTER still exposes F1–F8 only and no F9.
- Confirm `OPEN MISSION` remains navigation-only.

## 2. Recorded session and cancel-before-export behavior

- Open COMPUTER → SESSION and choose **START RECORDED SESSION**.
- Let the plant advance for several seconds and pause it.
- Choose **SAVE ARCHIVE**, then cancel the file picker.
- Confirm the status reports cancellation and the current session remains usable.
- Confirm there is no second save dialog, crash or unexpected runtime reset.

## 3. Non-destructive overwrite on the desktop filesystem

- Save the recorded session to a new local file such as `hotfix2-session.nrs-session.json`.
- Resume/advance the session, pause again, then save to the **same** file.
- Confirm the second save succeeds.
- Load that same archive through **LOAD ARCHIVE**.
- Confirm replay verification succeeds and the restored logical state is coherent with the saved session.
- Keep a copy of the resulting archive as manual evidence if desired; it is not part of the source ZIP.

## 4. Reset/start failure boundary smoke check

- Use **Reset session** and **START RECORDED SESSION** in normal valid conditions.
- Confirm neither operation crashes the application and both leave a coherent operator-visible state.
- Expected injected numerical/write/replace failure cases are covered by the automated focused gate; no manual fault injection is required.

## 5. Engineering-number decimal consistency

On an Italian/European host culture, inspect at least one linear/circular gauge with a fractional scale bound and the COMPUTER modes/controller setpoint view.

- Fractional engineering numbers must use the same technical invariant decimal point convention as the canonical HMI value text.
- Do not accept one instrument showing a value with `.` and its min/max scale with `,`.

## Promotion rule

Promote **M10.9.7.3 Hotfix 2 REV2** only if all automated gates and all applicable manual checks above are green. After promotion, M10.9.7.4 may begin from Hotfix 2 REV2 VALIDATED.
