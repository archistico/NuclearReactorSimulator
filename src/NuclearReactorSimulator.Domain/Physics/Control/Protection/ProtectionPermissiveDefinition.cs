namespace NuclearReactorSimulator.Domain.Physics.Control.Protection;

/// <summary>Measured condition that must be satisfied before a latched M5.5 protection reset is accepted.</summary>
public sealed record ProtectionPermissiveDefinition
{
    public ProtectionPermissiveDefinition(
        string id,
        string measurementChannelId,
        ProtectionComparison comparison,
        double threshold)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(measurementChannelId))
        {
            throw new ArgumentException("Permissive and measurement-channel ids must be non-empty.");
        }
        if (!Enum.IsDefined(typeof(ProtectionComparison), comparison))
        {
            throw new ArgumentOutOfRangeException(nameof(comparison), comparison, "Unknown permissive comparison.");
        }
        if (!double.IsFinite(threshold))
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), threshold, "Permissive threshold must be finite.");
        }

        Id = id.Trim();
        MeasurementChannelId = measurementChannelId.Trim();
        Comparison = comparison;
        Threshold = threshold;
    }

    public string Id { get; }
    public string MeasurementChannelId { get; }
    public ProtectionComparison Comparison { get; }
    public double Threshold { get; }
}
