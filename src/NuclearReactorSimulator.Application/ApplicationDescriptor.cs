namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.7.1 — Operator Control-State & Synchronization Usability Hotfix",
        "Implementation candidate — M10.7.1 on validated M10.7 baseline; separates latched trip indication from command availability, exposes canonical protection-reset readiness, makes synchronization presentation breaker-aware, and adds canonical next-action/startup-to-power operator guidance before M10.8");
}
