namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.4.1-D.4.1 — Turbine Valve Replay, Reset & Travel Ownership Hardening",
        "Candidate on the fully validated D.4 baseline — gives each turbine STOP valve an explicit optional travel-rate contract, preserves legacy instantaneous definitions, verifies valve commands through full replay and in-flight checkpoint restoration, and proves a preserved STOP OPEN request resumes finite travel after an accepted turbine-trip reset without hidden repair");
}
