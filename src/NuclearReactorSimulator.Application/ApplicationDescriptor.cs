namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application candidate without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.7.3 Hotfix 1 REV2 — Live Mission / Performance Historical Shell Contract Alignment",
        "M10.9.4.1 / Phase I, M10.9.5, M10.9.6, M10.9.7.1 Hotfix 3, M10.9.7.2 REV1, M10.9.7.2 Hotfix 1 REV1, M10.9.7.2 Hotfix 2 REV1 and M10.9.7.2 Hotfix 3 REV1 are validated; M10.9.6 remains closed. M10.9.7.3 Hotfix 1 REV2 is rebuilt exclusively over the validated Hotfix 3 REV1 runtime plus its Docs3 planning alignment. The original M10.9.7.3 candidate failed compilation in one score-dimension test assertion and one Avalonia DataContext binding; the first Hotfix 1 then built but ordinary tests exposed one stale batch-presentation ordering defect in the live Mission/Performance source and one historical situation-strip test that incorrectly scanned the entire MainWindow; Hotfix 1 REV1 fixed both, after which Application.Tests passed but the scoped historical shell test revealed its old direct LogicalStepText expectation was itself obsolete because the validated top runtime block exposes current step through RuntimeProgressText. It activates the dedicated MISSION / Mission & Performance peer workspace chosen in M10.9.7.2, preserves the global COMPUTER F1-F8 contract with no F9, and adds only selection-only contextual navigation from COMPUTER. A read-only live MissionPerformance source consumes deterministic challenge lifecycle, external-demand, score and control-authority evidence; demand history observes every deterministic step while UI publication follows presentation cadence. Explicit structural presentation comparison replaces generated record equality for update suppression. GRID DEMAND, REQUESTED LOAD and ACTUAL OUTPUT remain separate; unavailable values remain unavailable rather than zero. The normal desktop startup does not infer or invent a challenge pack; explicit pack binding is required for a live mission session. Archive-restored mission binding/timeline equivalence remains M10.9.7.4 scope. No new challenge definition, scoring arithmetic, protection authority, plant command authority or physics change is included."
    );
}
