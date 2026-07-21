namespace NuclearReactorSimulator.Simulation.Physics.Control.Integration;

/// <summary>Per-phase measured-signal tracking acceptance target used only by the M5.7 verification gate.</summary>
public sealed record AutomaticOperationTrackingTarget
{
    public AutomaticOperationTrackingTarget(string channelId, double targetEngineeringValue, double maximumAbsoluteFinalError)
    {
        if (string.IsNullOrWhiteSpace(channelId))
        {
            throw new ArgumentException("Tracking channel id cannot be empty or whitespace.", nameof(channelId));
        }
        if (!double.IsFinite(targetEngineeringValue))
        {
            throw new ArgumentOutOfRangeException(nameof(targetEngineeringValue), targetEngineeringValue, "Tracking target must be finite.");
        }
        if (!double.IsFinite(maximumAbsoluteFinalError) || maximumAbsoluteFinalError < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAbsoluteFinalError), maximumAbsoluteFinalError, "Tracking tolerance must be finite and non-negative.");
        }

        ChannelId = channelId.Trim();
        TargetEngineeringValue = targetEngineeringValue;
        MaximumAbsoluteFinalError = maximumAbsoluteFinalError;
    }

    public string ChannelId { get; }
    public double TargetEngineeringValue { get; }
    public double MaximumAbsoluteFinalError { get; }
}
