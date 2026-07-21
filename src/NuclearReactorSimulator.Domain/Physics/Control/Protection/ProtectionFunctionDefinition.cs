namespace NuclearReactorSimulator.Domain.Physics.Control.Protection;

/// <summary>One deterministic latching protection function evaluated from one measured M5.1 channel.</summary>
public sealed record ProtectionFunctionDefinition
{
    public ProtectionFunctionDefinition(
        string id,
        string measurementChannelId,
        ProtectionComparison comparison,
        double tripThreshold,
        double resetThreshold,
        ProtectionAction actions,
        bool tripOnInvalidMeasurement = true)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(measurementChannelId))
        {
            throw new ArgumentException("Protection-function and measurement-channel ids must be non-empty.");
        }
        if (!Enum.IsDefined(typeof(ProtectionComparison), comparison))
        {
            throw new ArgumentOutOfRangeException(nameof(comparison), comparison, "Unknown protection comparison.");
        }
        if (!double.IsFinite(tripThreshold) || !double.IsFinite(resetThreshold))
        {
            throw new ArgumentOutOfRangeException(nameof(tripThreshold), "Protection thresholds must be finite.");
        }
        if (actions == ProtectionAction.None || (actions & ~(ProtectionAction.ReactorScram | ProtectionAction.TurbineTrip | ProtectionAction.GeneratorTrip)) != ProtectionAction.None)
        {
            throw new ArgumentOutOfRangeException(nameof(actions), actions, "A protection function must define at least one supported action.");
        }
        if (comparison == ProtectionComparison.High && resetThreshold > tripThreshold)
        {
            throw new ArgumentException("A high-trip reset threshold must be less than or equal to the trip threshold.", nameof(resetThreshold));
        }
        if (comparison == ProtectionComparison.Low && resetThreshold < tripThreshold)
        {
            throw new ArgumentException("A low-trip reset threshold must be greater than or equal to the trip threshold.", nameof(resetThreshold));
        }

        Id = id.Trim();
        MeasurementChannelId = measurementChannelId.Trim();
        Comparison = comparison;
        TripThreshold = tripThreshold;
        ResetThreshold = resetThreshold;
        Actions = actions;
        TripOnInvalidMeasurement = tripOnInvalidMeasurement;
    }

    public string Id { get; }
    public string MeasurementChannelId { get; }
    public ProtectionComparison Comparison { get; }
    public double TripThreshold { get; }
    public double ResetThreshold { get; }
    public ProtectionAction Actions { get; }
    public bool TripOnInvalidMeasurement { get; }
}
