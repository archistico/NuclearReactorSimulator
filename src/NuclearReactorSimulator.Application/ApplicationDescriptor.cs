namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M8.2 — Hydraulic Component Faults",
        "Baseline candidate — M8.1 validated; deterministic pump/valve/path/leak fault effects reuse canonical hydraulic seams and the single plant-network integration boundary");
}
