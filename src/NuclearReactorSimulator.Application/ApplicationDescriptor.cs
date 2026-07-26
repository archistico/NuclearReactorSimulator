namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.4.1-F.2 — Conservative Main-Steam Header Relief",
        "Candidate on the validated F.1 choked steam-flow baseline — adds one pressure-actuated current-v2 main-steam header relief boundary to atmosphere, limits the validated ideal-vapor capacity by committed vapor availability and integrates signed mass/internal-energy export exactly once, while leaving turbine bypass, receiver inventory, operator authority and enthalpy migration deferred");
}
