namespace NuclearReactorSimulator.Simulation.Physics.Instrumentation;

/// <summary>Committed deterministic dynamic state of one instrument channel.</summary>
public sealed record InstrumentationChannelState
{
    public InstrumentationChannelState(
        string channelId,
        bool isInitialized,
        double filteredEngineeringValue,
        double lastOutputEngineeringValue)
    {
        if (string.IsNullOrWhiteSpace(channelId))
        {
            throw new ArgumentException("Instrumentation channel-state id cannot be empty or whitespace.", nameof(channelId));
        }

        if (!double.IsFinite(filteredEngineeringValue))
        {
            throw new ArgumentOutOfRangeException(nameof(filteredEngineeringValue), filteredEngineeringValue, "Filtered value must be finite.");
        }

        if (!double.IsFinite(lastOutputEngineeringValue))
        {
            throw new ArgumentOutOfRangeException(nameof(lastOutputEngineeringValue), lastOutputEngineeringValue, "Last output value must be finite.");
        }

        ChannelId = channelId.Trim();
        IsInitialized = isInitialized;
        FilteredEngineeringValue = filteredEngineeringValue;
        LastOutputEngineeringValue = lastOutputEngineeringValue;
    }

    public string ChannelId { get; }

    public bool IsInitialized { get; }

    public double FilteredEngineeringValue { get; }

    public double LastOutputEngineeringValue { get; }
}
