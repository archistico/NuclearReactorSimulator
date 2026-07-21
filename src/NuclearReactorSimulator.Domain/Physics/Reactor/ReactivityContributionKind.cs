namespace NuclearReactorSimulator.Domain.Physics.Reactor;

/// <summary>
/// Diagnostic source category for a reactivity contribution.
/// Categories describe origin only; they do not define kinetics or feedback equations.
/// </summary>
public enum ReactivityContributionKind
{
    ControlRods = 0,
    FuelTemperature = 1,
    CoolantTemperature = 2,
    Void = 3,
    Xenon = 4,
    Other = 5,
}
