namespace NuclearReactorSimulator.Domain.Physics.Reactor.Core;

/// <summary>
/// Immutable dynamic state for one aggregated core zone.
/// M3.3 stores only the current normalized power share; local physical inventories remain owned by PlantState.
/// </summary>
public sealed record CoreZoneState
{
    public CoreZoneState(string zoneId, CoreZonePowerFraction powerFraction)
    {
        if (string.IsNullOrWhiteSpace(zoneId))
        {
            throw new ArgumentException("Core-zone state id cannot be empty or whitespace.", nameof(zoneId));
        }

        ZoneId = zoneId.Trim();
        PowerFraction = powerFraction;
    }

    public string ZoneId { get; }

    public CoreZonePowerFraction PowerFraction { get; }
}
