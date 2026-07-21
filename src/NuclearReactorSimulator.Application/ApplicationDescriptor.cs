namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M7.6 — Power Manoeuvring & Normal Shutdown",
        "Baseline candidate — M7.5 validated; exact stable-low-load parallel v1 initial condition, bounded load manoeuvring, explicit temperature/void feedback observation, xenon boundary preservation and controlled normal-shutdown guidance");
}
