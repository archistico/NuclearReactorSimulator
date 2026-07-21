using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.Core;

/// <summary>
/// Immutable spatial snapshot that partitions one global fission-power value across configured core zones.
/// </summary>
public sealed class AggregatedCoreSnapshot
{
    public AggregatedCoreSnapshot(
        AggregatedCoreDefinition definition,
        Power totalFissionThermalPower,
        IEnumerable<CoreZoneSnapshot> zones)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(zones);

        var canonicalZones = zones
            .Select(zone => zone ?? throw new ArgumentException("Core-zone snapshots cannot contain null entries.", nameof(zones)))
            .OrderBy(static zone => zone.ZoneId, StringComparer.Ordinal)
            .ToArray();

        var expectedIds = definition.Zones.Select(static zone => zone.Id).ToArray();
        var actualIds = canonicalZones.Select(static zone => zone.ZoneId).ToArray();
        if (!expectedIds.SequenceEqual(actualIds, StringComparer.Ordinal))
        {
            throw new ArgumentException("Core snapshot must contain exactly one snapshot for every configured zone.", nameof(zones));
        }

        Definition = definition;
        TotalFissionThermalPower = totalFissionThermalPower;
        Zones = new ReadOnlyCollection<CoreZoneSnapshot>(canonicalZones);
    }

    public AggregatedCoreDefinition Definition { get; }

    public string CoreId => Definition.Id;

    public Power TotalFissionThermalPower { get; }

    public IReadOnlyList<CoreZoneSnapshot> Zones { get; }

    public CoreZoneSnapshot GetZone(string id)
        => Zones.FirstOrDefault(zone => string.Equals(zone.ZoneId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown core-zone snapshot '{id}'.");
}
