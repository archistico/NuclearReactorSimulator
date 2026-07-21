namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M8.2 — Hydraulic Component Faults",
        "Baseline candidate hotfix 1 — M8.1 validated; M8.2 hydraulic fault semantics unchanged; control-room generator selection/trip gating hardened with headless App regression coverage");
}
