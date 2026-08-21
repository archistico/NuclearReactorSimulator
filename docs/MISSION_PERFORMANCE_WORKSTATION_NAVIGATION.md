# Mission & Performance workstation navigation

M10.9.7.2 REV1 is VALIDATED and freezes the navigation topology for the Mission/Performance presentation. It was rebuilt on the authoritative M10.9.7.1 Hotfix 3 VALIDATED baseline. It does not implement the workstation UI. The earlier pre-Hotfix-3 7.2 package is superseded/not validated.

## Chosen topology

Mission/Performance is a dedicated workspace in the main control-room HMI shell, peer to PLANT, REACTOR, PRIMARY, TURBINE, GRID, ALARMS and COMPUTER. Its planned short shell label is `MISSION` and its planned title is `Mission & Performance`.

The existing Operator Computer remains a fixed eight-page workstation:

- F1 GUIDANCE;
- F2 INFO;
- F3 ALARMS;
- F4 COMMANDS;
- F5 MODES;
- F6 DIAGNOSTICS;
- F7 LOG;
- F8 SESSION.

No F9 is introduced. COMPUTER may expose a contextual action that navigates to the dedicated Mission/Performance workspace. That action selects presentation state only; it cannot dispatch a plant command or alter simulation, protection, challenge, score or control authority.

## Deliberate 7.2 boundary

M10.9.7.2 REV1 records the decision in `MissionPerformanceNavigationDecision.Current` but leaves the live workspace catalog and XAML untouched. `UiRouteActivated` therefore remains false and activation is assigned to M10.9.7.3.

This boundary avoids a transient blank or partially wired main-HMI workspace and lets the placement decision be validated independently from layout, ViewModel and manual HMI acceptance.

## Pre-live constraints

Before the route is wired live, the pre-live hardening sequence must address or explicitly qualify the review follow-ups already retained in the milestone plan: per-step `ObservationFingerprint()` allocation and hot `PlantDefinition` / `PlantState` id lookup allocation/scanning at 10 ms cadence; UI change detection must not rely on generated record equality over `IReadOnlyList<>`; score-dominance authoring must fail earlier before future pack expansion; the current `FinalScore == FinalPercentage` equivalence remains tied to the explicit v1 100-point scoring invariant.

## M10.9.7.3 implementation obligations

When the route is activated, 7.3 must:

1. add the dedicated `MISSION` workspace to the existing shell without changing F1–F8;
2. render the validated M10.9.7.1 immutable read model rather than recomputing challenge/scoring semantics;
3. add an explicit contextual COMPUTER navigation action without command-dispatch side effects;
4. keep external demand, requested generator load and actual electrical output visibly distinct;
5. preserve safety/protection visual priority over performance decoration;
6. remain usable at the minimum supported window size and by keyboard.

ADR-0178 records the architectural choice and rejected F9 alternative.
