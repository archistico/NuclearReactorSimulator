# M10.9.8.2 Hotfix 1 REV5 — Interactive List Refresh Stability Audit

**Status:** CANDIDATE / NOT VALIDATED.

## Purpose

REV5 closes the remaining F4 `DEPENDENCY CHAIN — SELECT A STEP` hover flicker reported during RUN and audits every collection-backed UI surface for the same refresh-identity failure class, including selectors created programmatically rather than declared with `ItemsSource` in XAML.

This is a presentation-stability repair only. It does not change Simulation physics or coefficients, challenge/scoring/protection ownership, plant-command authority, archive schema, fingerprint algorithm, exact mission identities or the bounded-demand @2 runtime composition.

## UI collection-control census

The App currently has **29 collection-backed control instances** relevant to this audit:

- **24 XAML controls with `ItemsSource`**: 4 selectable `ListBox` + 20 `ItemsControl`;
- **5 `ControlRoomSelector` instances**, each backed by the single programmatic `ComboBox` implementation in `ControlRoomSelector.cs`.

The four selectable `ListBox` surfaces are:

| Surface | ItemsSource | Stability contract |
| --- | --- | --- |
| Plant navigation | `Workspaces` | Static catalog; no snapshot replacement |
| F4 contextual command catalog | `CommandEntries` | REV4 semantic presentation equivalence preserves collection/selection when visible identity/availability is unchanged |
| F4 dependency chain | `SelectedCommandDependencySteps` | REV5 caches consequence/dependency projection per selected typed command and does not notify/replace the list on unrelated runtime snapshots, including availability-only refreshes |
| F8 session checkpoints | `SessionCheckpoints` | REV5 preserves collection and selected checkpoint when the incoming immutable checkpoint sequence is value-equivalent |

The five programmatic selector instances are `PUMP TARGET`, `ADMISSION TRAIN`, `GENERATOR TARGET`, `ROD TARGET` and `ALARM TARGET`. Previously `ControlRoomSelector.UpdateVisuals()` reassigned `ComboBox.ItemsSource` every time **any** selector visual property changed, including availability/state refreshes. REV5 caches the parsed options and changes `ItemsSource` only when the option sequence actually changes; selected index is likewise written only when needed.

Of the 20 `ItemsControl` controls, **19 are read-only presentation lists** with no selectable/focusable child controls. Eighteen are plant/alarm/history telemetry surfaces whose immutable values intentionally change with snapshots; replacing those data projections remains appropriate and does not carry selection/focus state. The nineteenth is MISSION `ScoreDimensions`, which REV5 also preserves when the canonical score-dimension sequence is unchanged.

The one interactive `ItemsControl` is the `MISSION` deterministic timeline, `ItemsSource="{Binding Timeline}"`, because rows may contain a drill-down `Button`. REV5 preserves the timeline collection reference and suppresses `Timeline` property notification when only scalar mission state changes and the canonical timeline sequence is unchanged. The same semantic suppression is applied to unchanged score-dimension and recent-event row projections.

## Root cause of the dependency-chain flicker

`SelectedCommandDependencySteps` previously returned `CurrentDependencyChain.Steps`, while `CurrentDependencyChain` called `OperatorComputerCommandDependencyChainCatalog.Project(...)` on every getter invocation. `UpdateSnapshot(...)` then raised `SelectedCommandDependencySteps` and `SelectedCommandDependencyStep` on every runtime snapshot through the broad command-context notification method.

The dependency-chain content is authored from the selected typed command and is not a per-step plant-state list. Reprojecting and renotifying it every snapshot caused Avalonia to receive a new `ItemsSource` identity repeatedly, recreating list containers and disturbing pointer hover/selection.

REV5 caches `CurrentConsequence` and `CurrentDependencyChain` until the selected typed command changes. Dynamic schematic/mimic state continues to refresh independently.

## Regression contracts

`M10982Hotfix1Rev5ListRefreshStabilityTests` verifies:

1. a RUN→PAUSED availability refresh for the same selected typed command preserves dependency-list and selected-step object identity with zero dependency-list/selection notifications;
2. equivalent checkpoint sequences preserve F8 list and selected checkpoint object identity with zero list/selection notifications;
3. scalar-only MISSION changes with newly allocated but value-equivalent canonical row lists preserve the existing score/event/timeline projections and interactive timeline row identity;
4. all 4 XAML `ListBox` bindings and all 20 XAML `ItemsControl` bindings are explicitly inventoried;
5. the only interactive XAML `ItemsControl` is the MISSION timeline;
6. all five `ControlRoomSelector` instances are inventoried and the shared programmatic `ComboBox` implementation has an options-equivalence guard before `ItemsSource` reassignment.

Any newly introduced collection-backed UI surface changes the frozen inventory and forces an explicit refresh-policy review.

## Manual smoke

During RUN:

- hover several F4 command rows;
- hover/select rows near the bottom of `DEPENDENCY CHAIN — SELECT A STEP` and leave the pointer stationary for multiple logical steps;
- if F8 has checkpoints, hover/select a checkpoint while runtime/session presentation refreshes;
- open each available target selector (`PUMP TARGET`, `ADMISSION TRAIN`, `GENERATOR TARGET`, `ROD TARGET`, `ALARM TARGET`) and verify ordinary state refresh does not repeatedly collapse/rebuild the option list or move selection;
- in MISSION, hover a timeline drill-down button while logical steps advance and no new timeline evidence is emitted.

No continuously repeating hover disappearance/reappearance, dropdown reconstruction or selection reset is acceptable.
