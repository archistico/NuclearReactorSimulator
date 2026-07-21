using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.Feedback;

/// <summary>
/// Immutable diagnostic projection of one evaluated temperature-reactivity feedback.
/// </summary>
public sealed record TemperatureFeedbackSnapshot(
    string Id,
    ReactivityContributionKind Kind,
    Temperature ReferenceTemperature,
    Temperature MeasuredTemperature,
    TemperatureDifference TemperatureDifference,
    TemperatureReactivityCoefficient Coefficient,
    Reactivity Reactivity)
{
    public ReactivityContribution ToContribution() => new(Id, Kind, Reactivity);
}
