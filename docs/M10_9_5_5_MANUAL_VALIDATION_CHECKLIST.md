# M10.9.5.5 — Manual HMI Closure Checklist

Use this checklist only after `dotnet build`, the complete ordinary suite and `scripts\run-m1095-command-consequence-closure-audit.cmd` are green.

The purpose is to validate the operator-facing consequence model as one coherent workflow. Do **not** use this gate to retune plant physics, protection or command authority.

## Setup

- [ ] Launch the current authoritative exact-v4 desktop production scenario.
- [ ] Open the Operator Computer and select **F4 COMMANDS**.
- [ ] Confirm F1–F8 navigation and the rest of the control-room HMI still render normally.
- [ ] Repeat the essential checks at the minimum supported window size.

## Representative command inspection — no dispatch required

Inspect at least one command from each representative family below. It is acceptable for the command to be BLOCKED/UNAVAILABLE; blocked commands must remain inspectable.

- [ ] runtime command (`RUN`, `PAUSE` or `SINGLE STEP`);
- [ ] reactor/protection command (`SCRAM`, rod command or protection reset);
- [ ] primary-system command (main-circulation pump start/stop);
- [ ] turbine command (trip or speed raise/lower);
- [ ] generator/electrical command (load raise/lower or breaker command);
- [ ] alarm command (acknowledge/reset when an applicable alarm is present).

For the inspected commands confirm:

- [ ] `DIRECT EFFECT` is clearly separated from `EXPECTED INFLUENCE`;
- [ ] expected influence is qualitative and is not presented as a numerical prediction;
- [ ] current blockers/permissives read as current-state evidence, not as a second authority owner;
- [ ] `WHAT TO MONITOR` points to useful existing measurements/states;
- [ ] the dependency chain is understandable without reading source code;
- [ ] selecting commands or dependency steps never dispatches a command.

## Canonical schematic focus

- [ ] a `PlantMimicElement` step highlights the exact canonical element;
- [ ] a `PlantMimicConnection` step visibly explains that the source node is being used as the graphical proxy;
- [ ] a non-graphical step clears the highlight instead of fabricating a target;
- [ ] the embedded mimic is not clickable and remains presentation-only.

## Observed response

Use a safe command that can be accepted in the current scenario (for example a runtime RUN/PAUSE transition) to exercise post-dispatch evidence.

- [ ] dispatch occurs only through explicit `ENTER` / `EXECUTE [ENTER]`;
- [ ] accepted feedback is visible;
- [ ] `OBSERVED RESPONSE — POST-DISPATCH EVIDENCE` is visually separate from expected influence;
- [ ] baseline/latest values or states reflect what was actually observed after dispatch;
- [ ] numeric changes show actual delta/direction only when the monitor is numeric;
- [ ] the UI states that post-dispatch co-variation is **not proof of causality**;
- [ ] no generic `SUCCESS/FAILURE` is invented from a monitor delta.

Also inspect or intentionally attempt one currently rejected/blocked action without bypassing canonical authority:

- [ ] rejection/block feedback is visible;
- [ ] rejected commands do not show fictional plant-effect delta rows.

## Keyboard and usability closure

- [ ] TAB / SHIFT+TAB reaches the COMMANDS catalog, dependency list and execute boundary predictably;
- [ ] UP/DOWN selection remains practical;
- [ ] keyboard navigation alone does not dispatch;
- [ ] the Context Inspector, mimic and Observed Response remain readable at the minimum supported window;
- [ ] the amount of information is useful rather than visually overwhelming for normal operator use.

## Promotion

M10.9.5 may be promoted to **VALIDATED** only when every applicable checkbox above is green and the automated closure artifact contains:

```text
m1095-automated-closure-passes=True
m1095-closure-ready=True
```

After explicit promotion, M10.9.6.1 — challenge lifecycle and logical-time contract — is next.
