namespace NuclearReactorSimulator.Domain.Physics.Control.Protection;

/// <summary>Non-latching command inhibit evaluated from measured state before physical command arbitration.</summary>
public sealed record ProtectionInterlockDefinition
{
    public ProtectionInterlockDefinition(
        string id,
        string measurementChannelId,
        ProtectionComparison comparison,
        double threshold,
        ProtectionInterlockAction actions,
        bool blockOnInvalidMeasurement = true)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(measurementChannelId))
        {
            throw new ArgumentException("Interlock and measurement-channel ids must be non-empty.");
        }
        if (!Enum.IsDefined(typeof(ProtectionComparison), comparison))
        {
            throw new ArgumentOutOfRangeException(nameof(comparison), comparison, "Unknown interlock comparison.");
        }
        if (!double.IsFinite(threshold))
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), threshold, "Interlock threshold must be finite.");
        }
        if (actions == ProtectionInterlockAction.None
            || (actions & ~(ProtectionInterlockAction.BlockRodWithdrawal | ProtectionInterlockAction.BlockTurbineAdmissionOpening | ProtectionInterlockAction.BlockGeneratorBreakerClose)) != ProtectionInterlockAction.None)
        {
            throw new ArgumentOutOfRangeException(nameof(actions), actions, "An interlock must define at least one supported inhibit action.");
        }

        Id = id.Trim();
        MeasurementChannelId = measurementChannelId.Trim();
        Comparison = comparison;
        Threshold = threshold;
        Actions = actions;
        BlockOnInvalidMeasurement = blockOnInvalidMeasurement;
    }

    public string Id { get; }
    public string MeasurementChannelId { get; }
    public ProtectionComparison Comparison { get; }
    public double Threshold { get; }
    public ProtectionInterlockAction Actions { get; }
    public bool BlockOnInvalidMeasurement { get; }
}
