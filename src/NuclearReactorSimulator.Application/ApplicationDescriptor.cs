namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.4.1-F.1 — Choked Steam-Flow Capacity Law & Audit",
        "Candidate on the validated E.3.2 Hotfix 3 electrical-protection baseline — adds a typed ideal-vapor one-way compressible steam-flow capacity seam with continuous subcritical-to-choked behavior and deterministic CSV/summary evidence, while leaving relief/bypass topology, plant inventories, valve authority and runtime source-term integration unchanged");
}
