namespace NuclearReactorSimulator.Domain.Physics.Control.Protection;

/// <summary>
/// Optional measured enable condition for one protection function. The protection pickup is blocked whenever the
/// supervision condition is not satisfied. Invalid supervision measurements fail inactive so disconnected or
/// otherwise ineligible equipment cannot create a spurious trip.
/// </summary>
public sealed record ProtectionFunctionSupervisionDefinition
{
    public ProtectionFunctionSupervisionDefinition(
        string measurementChannelId,
        ProtectionComparison comparison,
        double threshold)
    {
        if (string.IsNullOrWhiteSpace(measurementChannelId))
        {
            throw new ArgumentException("Protection-function supervision channel id cannot be empty or whitespace.", nameof(measurementChannelId));
        }
        if (!Enum.IsDefined(typeof(ProtectionComparison), comparison))
        {
            throw new ArgumentOutOfRangeException(nameof(comparison), comparison, "Unknown protection supervision comparison.");
        }
        if (!double.IsFinite(threshold))
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), threshold, "Protection supervision threshold must be finite.");
        }

        MeasurementChannelId = measurementChannelId.Trim();
        Comparison = comparison;
        Threshold = threshold;
    }

    public string MeasurementChannelId { get; }
    public ProtectionComparison Comparison { get; }
    public double Threshold { get; }
}
