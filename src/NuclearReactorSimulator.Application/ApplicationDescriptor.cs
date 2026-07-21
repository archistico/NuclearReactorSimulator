namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M8.1 — Deterministic Fault-Injection Framework",
        "Baseline candidate — M7.7 validated / M7 complete; explicit versioned fault schedules, logical-step or committed-condition triggers, fail-closed applicator binding and replay-visible fault lifecycle state");
}
