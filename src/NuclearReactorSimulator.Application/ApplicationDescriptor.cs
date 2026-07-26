namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.4.1-E.3.2 Hotfix 2 — Canonical Grid Nominal-Frequency Seed",
        "Candidate on the validated E.3.1 Hotfix 1 trajectory baseline — restores complete logical-step-zero measured instrumentation using the canonical grid nominal frequency and adds breaker-supervised delayed reverse-power, underfrequency and loss-of-synchronism generator trips derived from recorded current-v2 evidence, preserves immediate unsupervised legacy protection by default, and publishes the new trip markers without moving protection ownership out of canonical M5.5");
}
