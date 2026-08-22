# M10.9.8.2 Hotfix 1 REV5 — Manual smoke checklist

This is a narrow promotion gate for the live mission/F4/list-stability defects reported during M10.9.8.2 validation. It does not replace the broader M10.9.8.5 end-to-end HMI acceptance.

- [ ] Build, complete ordinary suite and `scripts\run-m10982-healthy-assistance-authority-matrix-audit.cmd` are green.
- [ ] Start `--mission-pack=bounded-demand-following-5-10-5@2`; confirm MISSION shows the exact @2 pack and the session runs past STEP 1000 without the historical `control-out` water/steam envelope failure around STEP 610–615.
- [ ] Open COMPUTER → F4 COMMANDS while RUN is active. Keep the mouse over several command-catalog rows for at least 10 seconds; there is no continuous hover/selection flicker caused by snapshot refresh.
- [ ] In `DEPENDENCY CHAIN — SELECT A STEP`, select a row near the bottom and keep the mouse stationary over several rows for at least 10 seconds while RUN advances; the list and selected step remain visually stable unless the selected command itself changes.
- [ ] If F8 SESSION contains checkpoints, select a non-first checkpoint and leave it selected while normal snapshot/session refresh occurs; selection does not jump or flicker unless the checkpoint collection actually changes.
- [ ] Exercise each available target selector (`PUMP TARGET`, `ADMISSION TRAIN`, `GENERATOR TARGET`, `ROD TARGET`, `ALARM TARGET`) while RUN/state refresh continues; opening/hovering the dropdown does not cause repeated option-list rebuild, collapse or selection jump when the options themselves have not changed.
- [ ] In MISSION, when a timeline row exposes a drill-down button, hover the button while logical steps advance without new timeline evidence; the button/container does not continuously disappear/reappear.
- [ ] Select an AVAILABLE F4 command and press ENTER. The application remains open and COMMANDS reports `DISPATCHED` or an explicit `BLOCKED BY RUNTIME/SCENARIO` result.
- [ ] Repeat ENTER on a command that is currently blocked/unavailable; the app remains open and no plant state is mutated by the presentation layer.
- [ ] Mouse click on `EXECUTE [ENTER]` still uses the same canonical command boundary.
- [ ] F1–F8 navigation remains intact; no F9 is introduced.

Promotion acceptance text:

`M10.9.8.2 Hotfix 1 REV5 manual mission/F4/list-stability smoke validation OK`
