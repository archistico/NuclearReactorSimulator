namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M8.4 — Turbine / Generator / Feedwater / Condenser Transients",
        "Baseline candidate — M8.3 validated; M8.4 adds deterministic secondary-system transient packs through canonical M4/M5 and M8 fault seams");
}
