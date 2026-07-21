namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M7.3 — First Criticality & Low-Power Operation",
        "Baseline candidate — M7.2 validated; exact pre-criticality/source-range v1 initial condition, controlled rod-operation permissions, observational criticality/period/low-power guidance and preserved steam/grid isolation");
}
