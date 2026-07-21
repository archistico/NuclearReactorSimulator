namespace NuclearReactorSimulator.Domain.Physics.Thermal;

/// <summary>
/// Immutable enabled/disabled state for an external thermal source.
/// </summary>
public sealed record HeatSourceState
{
    public HeatSourceState(string heatSourceId, bool isEnabled = true)
    {
        if (string.IsNullOrWhiteSpace(heatSourceId))
        {
            throw new ArgumentException("Heat-source id cannot be empty.", nameof(heatSourceId));
        }

        HeatSourceId = heatSourceId;
        IsEnabled = isEnabled;
    }

    public string HeatSourceId { get; }

    public bool IsEnabled { get; }
}
