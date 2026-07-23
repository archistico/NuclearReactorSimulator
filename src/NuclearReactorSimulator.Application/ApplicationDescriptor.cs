namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.4.1-A — Extended Operating-Envelope Audit",
        "Audit candidate on validated M10.9.4 baseline — adds separately runnable 300-second steady, load-step, load-rejection, condenser-cooling degradation, pump non-return, conservation, replay/checkpoint and performance evidence without changing production physics, seed values, control laws, protection thresholds or canonical state ownership");
}
