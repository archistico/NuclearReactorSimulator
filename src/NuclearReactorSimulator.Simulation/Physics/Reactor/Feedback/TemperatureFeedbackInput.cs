using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Feedback;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.Feedback;

/// <summary>
/// One committed temperature reading evaluated against a configured feedback definition.
/// </summary>
public sealed record TemperatureFeedbackInput
{
    public TemperatureFeedbackInput(
        TemperatureReactivityFeedbackDefinition definition,
        Temperature temperature)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Definition = definition;
        Temperature = temperature;
    }

    public TemperatureReactivityFeedbackDefinition Definition { get; }

    public Temperature Temperature { get; }
}
