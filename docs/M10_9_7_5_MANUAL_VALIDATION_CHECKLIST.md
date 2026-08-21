# M10.9.7.5 Hotfix 1 manual Mission / Performance closure checklist

Run this checklist only after:

```bat
dotnet build
dotnet test
scripts\run-m1097-mission-performance-closure-audit.cmd
```

are green. M10.9.7.4 Hotfix 1 manual timeline/drill-down/archive validation is already a validated prerequisite; this checklist is the final cumulative HMI closure review.

## 1. Unbound and active-at-a-glance comprehension

Start once without a mission and once with:

```bat
dotnet run --project src\NuclearReactorSimulator.App\NuclearReactorSimulator.App.csproj -- --mission-pack=bounded-demand-following-5-10-5@1
```

Confirm:

- unbound startup says `NO ACTIVE MISSION / UNBOUND` and does not fabricate score, objective or timeline state;
- the bound mission exposes objective/lifecycle, GRID DEMAND, REQUESTED LOAD, ACTUAL OUTPUT, score and safety/protection state without requiring logs;
- demand/request/actual cannot be visually mistaken for the same quantity;
- F1–F8 remain intact and there is no F9.

## 2. Keyboard-only practicality

Using the keyboard only, move among the main workspaces, enter MISSION, traverse the primary mission regions and activate at least one available drill-down.

Accept only if:

- focus remains visible;
- essential mission state can be reached without mouse-only controls;
- drill-down does not trap focus or require a plant command to return to useful evidence;
- navigation alone does not change plant state or logical step.

## 3. Safety hierarchy versus score

During an active mission, inspect the workstation while safety/protection information is present.

Confirm:

- protection/safety state is visually more prominent than score/game-like feedback;
- a degraded/trip/protection condition cannot be overlooked because of score emphasis;
- score decomposition remains supporting evidence, not an operational authority cue.

## 4. Drill-down usefulness

Exercise at least:

- one operator-action drill-down to COMPUTER / COMMANDS;
- one demand/electrical drill-down when available;
- one protection/alarm drill-down when available.

Each destination must expose evidence relevant to the selected row. Returning to MISSION must preserve the mission/timeline context.

## 5. Minimum supported window

At **1340×700**, verify that objective/lifecycle, safety/protection, demand/request/output, score and deterministic timeline remain usable. Wrapping/scrolling is acceptable; overlapping controls, hidden critical safety state or unusable keyboard navigation are not.

## 6. Final closure statement

Before promotion confirm that the automated artifact reported:

`m10975-mission-performance-closure-automated-passes=True`

and that every item above is green.

Report acceptance as:

```text
M10.9.7.5 Hotfix 1 manual closure validation OK
```

After that statement, M10.9.7 is VALIDATED/CLOSED and M10.9.8 may begin.
