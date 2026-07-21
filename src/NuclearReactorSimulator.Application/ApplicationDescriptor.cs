namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M7.7 — Training Objectives, Procedure Guidance & Evaluation",
        "Baseline candidate — M7.6 validated; deterministic historical checkpoints, accepted-action journal, optional guidance modes and observational 100-point training evaluation with no physics ownership");
}
