namespace NuclearReactorSimulator.Application;

/// <summary>
/// Describes the currently composed application baseline without coupling the UI to build-time constants.
/// </summary>
public sealed record ApplicationDescriptor(string ProductName, string Milestone, string Status)
{
    public static ApplicationDescriptor Current { get; } = new(
        "Nuclear Reactor Simulator",
        "M10.9.4.1-F.3 Hotfix 1 — Conservative Turbine Bypass to Condenser",
        "Hotfix 1 candidate on the validated F.2 header-relief baseline — adds one pressure-actuated current-v2 turbine-bypass path from the canonical main-steam header to the committed condenser steam space, resolves capacity against actual condenser backpressure and transfers mass/internal energy exactly once with zero external exchange, while leaving operator authority and Phase G flow-work/enthalpy migration deferred");
}
