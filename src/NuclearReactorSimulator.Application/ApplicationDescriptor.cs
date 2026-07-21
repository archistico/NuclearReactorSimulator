namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M8.3 — Instrumentation & Control Faults",
        "Baseline candidate — M8.2 hotfix 2 validated; M8.3 adds deterministic sensor, controller-output and actuator-command faults through canonical M5 seams");
}
