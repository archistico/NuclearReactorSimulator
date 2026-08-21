# M10.9.7.3 Hotfix 1 REV2 manual HMI validation checklist

Run this review only after build, ordinary tests and `scripts\run-m10973-mission-performance-live-workspace-audit.cmd` are green.

M10.9.7.3 Hotfix 1 REV2 activates presentation only. Do not interpret this gate as challenge selection, score ownership, protection ownership or plant-command authority.

## A. Normal desktop startup — unbound mission state

Launch normally:

```bat
dotnet run --project src\NuclearReactorSimulator.App\NuclearReactorSimulator.App.csproj
```

Confirm:

- the left workspace rail contains exactly one `MISSION` entry titled `Mission & Performance`;
- opening MISSION shows `NO ACTIVE MISSION` / `UNBOUND`, not a fabricated challenge inferred from the desktop scenario;
- unavailable mission/demand/score values are shown as unavailable rather than zero;
- COMPUTER still exposes exactly F1–F8 and no F9;
- COMPUTER `OPEN MISSION` selects MISSION and does not operate the plant;
- returning among PLANT/REACTOR/PRIMARY/TURBINE/GRID/ALARMS/COMPUTER/MISSION remains practical.

## B. Explicit active mission startup

Close the app and launch one exact authored pack explicitly:

```bat
dotnet run --project src\NuclearReactorSimulator.App\NuclearReactorSimulator.App.csproj -- --mission-pack=bounded-demand-following-5-10-5@1
```

Confirm:

- startup opens a real M10.9.6 mission rather than inferring from scenario identity;
- objective title/description are the scenario objective metadata, not challenge-title aliases;
- lifecycle and logical-step information is readable;
- `GRID DEMAND`, `REQUESTED LOAD` and `ACTUAL OUTPUT` are three visibly separate values;
- `GRID DEMAND` is presented as a training/reference quantity, never as a generator command;
- score/classification and score dimensions are readable without relying on color alone;
- `SAFETY / PROTECTION SIGNIFICANCE` is visually at least as prominent as the score block;
- recent evidence is readable, bounded and ordered consistently by logical evidence;
- assistance and control-authority evidence are observational only.

## C. Live update and navigation behavior

Using the normal canonical controls for the loaded challenge:

- run a few deterministic steps and confirm MISSION updates without obvious 100 Hz UI churn;
- issue at least one applicable generator-load action and confirm requested/actual evidence can diverge without being merged;
- use F1 through F8 and confirm every key still opens the historical COMPUTER page associated with that key;
- from COMPUTER choose `OPEN MISSION` and confirm the plant state does not change as a result of navigation;
- verify no F9 behavior exists.

## D. Minimum-window/readability review

At the supported minimum window size (`1340 × 700`):

- objective/lifecycle remains discoverable;
- safety/protection significance remains discoverable;
- the three demand/request/output values remain distinguishable;
- secondary score/evidence detail may scroll, but the surface does not become ambiguous or overlap controls;
- text is not dependent on color alone for meaning.

## E. Scope confirmation

Confirm there is no UI control in MISSION that directly issues plant commands, changes scoring rules, changes challenge definitions, resets protection or changes physical state.

Archive-restored mission binding/timeline reconstruction is intentionally **not** an M10.9.7.3 acceptance criterion; it is owned by M10.9.7.4.

If automated gates and all applicable checks above are green, promote M10.9.7.3 Hotfix 1 REV2 to VALIDATED. Do **not** begin M10.9.7.4 yet: the accepted post-7.3 App review requires M10.9.7.3 Hotfix 2 — Desktop Host Failure & Session Save Integrity — to be built exclusively on that validated REV2 baseline and validated first.
