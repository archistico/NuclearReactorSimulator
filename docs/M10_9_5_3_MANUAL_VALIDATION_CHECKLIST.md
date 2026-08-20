# M10.9.5.3 — Manual HMI validation checklist

Use the validated M10.9.5.2 baseline plus the M10.9.5.3 candidate. Do not change physics/configuration merely to make the presentation look better.

- [ ] Open **F4 COMMANDS** with the desktop at the minimum supported window size and confirm the page remains usable through normal scrolling.
- [ ] Select commands using keyboard **TAB + UP/DOWN** and confirm selection alone never dispatches a command.
- [ ] For at least one reactor command, verify **DIRECT EFFECT**, **EXPECTED INFLUENCE** and **WHAT TO MONITOR** are visibly distinct and understandable.
- [ ] Repeat for one primary/pump command, one turbine command and one generator/breaker command.
- [ ] Select a blocked/unavailable command and confirm the blocker remains visible while the consequence/dependency evidence is still inspectable.
- [ ] Select different dependency-chain steps and confirm only the presentation/schematic focus changes; plant state and command status do not change.
- [ ] Select a dependency step backed by a canonical mimic element and confirm that exact element is highlighted.
- [ ] Select a dependency step backed by a canonical mimic connection and confirm the text explicitly identifies the connection plus its proxy highlight.
- [ ] Select a command-target or published-state step with no graphical reference and confirm the mimic highlight clears instead of moving to an unrelated element.
- [ ] Confirm the compact whole-plant mimic uses the same canonical equipment/path vocabulary as the main plant overview.
- [ ] Confirm unavailable/non-mimic dependency evidence is labelled rather than fabricated as a schematic target.
- [ ] Press **ENTER** or **EXECUTE [ENTER]** only on an intentionally selected available command and confirm the existing M10.4 dispatch feedback remains intact.
- [ ] Confirm there is still no free-form text command input.

Promotion to M10.9.5.3 VALIDATED requires build, complete ordinary tests, focused audit and this checklist to be green.
