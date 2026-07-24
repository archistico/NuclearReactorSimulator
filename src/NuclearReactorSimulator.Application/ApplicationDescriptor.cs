namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.4.1-B.1 — Steam-Drum Liquid Inventory Closure",
        "Candidate on the locally green A.3 checkpoint — keeps the corrected current-v2 operating seed, clarifies manual-only game penalties and SPEED/LOAD reference steps in the HMI, and inventory-limits current-v2 steam-drum liquid recirculation without changing legacy/v1 behavior or protection thresholds");
}
