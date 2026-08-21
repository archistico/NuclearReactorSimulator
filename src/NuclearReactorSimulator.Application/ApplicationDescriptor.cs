namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application candidate without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.7.4 Hotfix 1 — Deterministic Mission Timeline, Drill-Down & Replay Equivalence",
        "M10.9.7.4 Hotfix 1 is stacked on the original M10.9.7.4 candidate after that candidate compiled but failed 3 ordinary tests due to test-contract misalignment only: the superseded M10.9.7.3 timeline heading, a raw-XAML F9 substring false positive, and an invalid non-empty PrimaryCircuit.Valves precondition for the retained H29 topology. Production XAML/runtime semantics and the frozen fingerprint remain unchanged. M10.9.7.3 Hotfix 2 REV2 is VALIDATED after build, complete ordinary tests, the focused desktop-host/session-integrity gate and its manual checklist on 2026-08-21. M10.9.7.4 is stacked exclusively on that validated baseline plus Docs4. It freezes sha256-control-room-snapshot-v1 with a populated exact-version golden fingerprint fixture; separates a protected bounded lifecycle spine from bounded recent operational evidence so dense protection/scoring traffic cannot erase activation or terminal mission boundaries; projects a deterministic logical-step timeline over lifecycle, demand-change, operator-action, alarm/protection/fault and current scoring evidence; and adds presentation-only drill-down targets to existing ELECTRICAL, ALARMS/EVENTS and COMPUTER pages without plant-command authority. The M10.9.7.3 combined RecentEvents at-a-glance contract remains available and unchanged. Replay/checkpoint restoration reconstructs challenge lifecycle and demand history from the canonical verified recording prefix, then continues from future live deterministic evidence without an opaque challenge-state checkpoint blob. Archive restoration preserves MISSION only when an explicit exact operational challenge pack binding is supplied and matches scenario/initial-condition identity; an unbound archive load remains mission-unbound and no pack is inferred from ScenarioId. Archive schema v1, scoring arithmetic, challenge definitions, protection ownership, Simulation physics, F1-F8 and no-F9 remain unchanged. The focused gate is scripts\\run-m10974-mission-performance-timeline-audit.cmd; final promotion requires the M10.9.7.4 manual timeline/drill-down checklist."
    );
}
