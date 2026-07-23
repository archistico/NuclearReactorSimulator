namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.4 — Subsystem Engineering Schematics",
        "Implementation candidate — M10.9.4 Hotfix 18 on validated M10.9.3 baseline; adds subsystem engineering schematics, long-gameplay acceptance tests, pressure-driven turbine expansion, current-v2 drum/main-steam closure and condenser UA×ΔT pressure feedback and current-v2 synchronous generator/grid phase-frequency stiffness without moving plant topology, protection or control authority into the UI");
}
