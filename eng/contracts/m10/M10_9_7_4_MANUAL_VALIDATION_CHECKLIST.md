# M10.9.7.4 manual validation checklist

Run this checklist only after build, the complete ordinary test suite and `scripts\run-m10974-mission-performance-timeline-audit.cmd` are green.

## 1. Unbound baseline

Start normally.

- `MISSION` remains available as a main workspace.
- It shows `NO ACTIVE MISSION / UNBOUND`.
- COMPUTER remains F1–F8 and no F9 exists.
- No timeline row or drill-down invents a mission for the normal desktop session.

## 2. Explicit bound mission and recorded restart

Start with:

```bat
dotnet run --project src\NuclearReactorSimulator.App\NuclearReactorSimulator.App.csproj -- --mission-pack=bounded-demand-following-5-10-5@1
```

Open COMPUTER / SESSION and choose `START RECORDED SESSION`.

- The new recorded session remains bound to the same exact mission pack.
- Returning to `MISSION` still shows the bounded-demand-following objective rather than `NO ACTIVE MISSION`.
- No pack is selected through scenario inference.

## 3. Deterministic timeline and retention presentation

Advance the mission and issue at least one generator-load command.

Verify:

- `DETERMINISTIC TIMELINE / DRILL-DOWN` is visible;
- lifecycle/objective rows, demand changes and operator evidence use logical `STEP n` labels;
- `GRID DEMAND`, `REQUESTED LOAD` and `ACTUAL OUTPUT` remain distinct;
- timeline retention text reports lifecycle-spine and recent-evidence counts;
- safety/protection information remains visually more prominent than score presentation;
- no obvious duplicate row appears merely because the presentation refreshes.

## 4. Presentation-only drill-down

Use at least two available timeline drill-down buttons, including an operator-action row if present.

- operator action opens COMPUTER / COMMANDS;
- demand evidence opens ELECTRICAL when available;
- protection/alarm evidence opens ALARMS / EVENTS when available;
- navigating does not change logical step, plant values or command state by itself;
- evidence-only rows remain readable without a button.

Return to `MISSION` and confirm timeline state remains intact.

## 5. Archive full replay with exact mission binding

From the recorded bound mission, save a local session archive. Advance the live session further, then load the saved archive while the current runtime is still bound to the same mission.

Verify:

- LOAD reports replay/fingerprint verification success;
- `MISSION` remains bound to the same exact pack;
- restored logical step and timeline match the saved prefix rather than the later live state;
- continuing RUN advances lifecycle/demand/timeline from that restored point;
- no duplicate activation/operator-action rows appear after continuation.

## 6. Checkpoint restore

Create a checkpoint in the recorded mission, advance further, then restore the selected checkpoint.

Verify:

- logical step returns to the checkpoint;
- no timeline row from a later logical step remains visible;
- mission lifecycle/timeline match the checkpoint prefix;
- subsequent RUN continues normally from the restored prefix.

## 7. Minimum-window readability

At 1340×700 verify that objective, safety/protection, demand/request/output, score and timeline remain usable without overlapping controls. Timeline detail may wrap/scroll naturally but must not obscure the safety/status surface.

## Acceptance

M10.9.7.4 may be promoted only if all items above are green and the automatic artifact reports `m10974-mission-performance-timeline-passes=True`.
