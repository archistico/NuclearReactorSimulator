# ADR-0178 — Place Mission/Performance as a dedicated main-HMI workspace

## Status

Accepted — M10.9.7.2 REV1 validated; subsequent hotfixes preserve the dedicated MISSION workspace/no-F9/navigation-only decision.

- Status: Accepted
- Date: 2026-08-21

## Context

M10.9.7.1 Hotfix 3 validated the immutable read-only Mission/Performance presentation contract, including the Hotfix 2 pre-workstation robustness hardening. The existing Operator Computer is already a validated fixed F1–F8 workstation: F1 GUIDANCE, F2 INFO, F3 ALARMS, F4 COMMANDS, F5 MODES, F6 DIAGNOSTICS, F7 LOG and F8 SESSION. Adding an implicit F9 would revise an established navigation contract and widen the M10.8/M10.9 regression surface for a presentation feature that does not need to own computer-page semantics.

The Mission/Performance view is also conceptually broader than a utility page. It aggregates mission objective, external demand, requested load, actual output, progress, score and deterministic history across the whole plant.

## Decision

Choose M10.9.7.2 REV1 placement option **A**, rebuilt on M10.9.7.1 Hotfix 3 VALIDATED.

Mission/Performance will be a **dedicated peer workspace in the main control-room HMI shell**. The Operator Computer remains a fixed F1–F8 workstation. COMPUTER may expose an explicit contextual navigation action that selects the Mission/Performance workspace, but that navigation action is presentation-only and may not dispatch or imply any plant command.

M10.9.7.2 freezes this topology but does **not** activate the new workspace in the live catalog, MainWindow, ViewModel or XAML. UI activation is deliberately deferred to M10.9.7.3 so there is no partially implemented or blank workspace between milestones.

The canonical Application decision is `MissionPerformanceNavigationDecision.Current`:

- placement: `DedicatedMainHmiWorkspace`;
- COMPUTER entry: `ContextualNavigationAction`;
- workspace title: `Mission & Performance`;
- workspace short label: `MISSION`;
- F1–F8 contract changed: false;
- added Operator Computer function key: none;
- plant command authority: false;
- live UI route active in 7.2: false;
- activation milestone: M10.9.7.3.

## Rejected alternative

Option B would add or otherwise revise Operator Computer fixed-page navigation. That would require an intentional M10.8 contract revision, broader keyboard/navigation regression work and a stronger migration justification. No such need exists for the mission/performance presentation requirement.

## Consequences

M10.9.7.3 can implement one coherent main-HMI workstation without overloading the Operator Computer page model. Existing F1–F8 keyboard behavior remains stable, while COMPUTER can still provide discoverable contextual entry. Because 7.2 does not register the workspace live, the decision can be validated independently from visual/layout work.
