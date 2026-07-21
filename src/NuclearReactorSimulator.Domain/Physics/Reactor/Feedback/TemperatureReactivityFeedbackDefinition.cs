using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.Feedback;

/// <summary>
/// Immutable linear temperature-feedback definition around an explicit reference temperature.
/// </summary>
public sealed record TemperatureReactivityFeedbackDefinition
{
    public TemperatureReactivityFeedbackDefinition(
        string id,
        ReactivityContributionKind kind,
        Temperature referenceTemperature,
        TemperatureReactivityCoefficient coefficient)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Temperature-feedback id cannot be empty.", nameof(id));
        }

        if (kind is not ReactivityContributionKind.FuelTemperature
            and not ReactivityContributionKind.CoolantTemperature)
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Temperature feedback must use FuelTemperature or CoolantTemperature reactivity kind.");
        }

        Id = id;
        Kind = kind;
        ReferenceTemperature = referenceTemperature;
        Coefficient = coefficient;
    }

    public string Id { get; }

    public ReactivityContributionKind Kind { get; }

    public Temperature ReferenceTemperature { get; }

    public TemperatureReactivityCoefficient Coefficient { get; }
}
