namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.7.1 — Operator Control-State & Synchronization Usability Hotfix",
        "Implementation candidate — M10.7.1 hotfix 2 on validated M10.7 baseline; preserves trip/protection-reset/synchronization usability, adds actual-state feedback for rods, main-circulation pumps and breaker position, and distinguishes persistent controls from momentary speed/load actions before M10.8");
}
