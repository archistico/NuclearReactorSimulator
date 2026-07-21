using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.Core.Channels;

/// <summary>Immutable canonical snapshot for all equivalent fuel-channel groups.</summary>
public sealed class FuelChannelGroupSetSnapshot
{
    public FuelChannelGroupSetSnapshot(
        string definitionId,
        Power totalFissionThermalPower,
        Power totalDecayHeatPower,
        IEnumerable<FuelChannelGroupSnapshot> groups)
    {
        if (string.IsNullOrWhiteSpace(definitionId))
        {
            throw new ArgumentException("Fuel-channel group-set snapshot definition id cannot be empty.", nameof(definitionId));
        }

        ArgumentNullException.ThrowIfNull(groups);
        if (totalFissionThermalPower < Power.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(totalFissionThermalPower), totalFissionThermalPower, "Total fission thermal power cannot be negative.");
        }

        if (totalDecayHeatPower < Power.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(totalDecayHeatPower), totalDecayHeatPower, "Total decay-heat power cannot be negative.");
        }

        var canonical = groups
            .Select(group => group ?? throw new ArgumentException("Fuel-channel snapshot collection cannot contain null entries.", nameof(groups)))
            .OrderBy(static group => group.GroupId, StringComparer.Ordinal)
            .ToArray();

        DefinitionId = definitionId.Trim();
        TotalFissionThermalPower = totalFissionThermalPower;
        TotalDecayHeatPower = totalDecayHeatPower;
        TotalNuclearHeatPower = totalFissionThermalPower + totalDecayHeatPower;
        Groups = new ReadOnlyCollection<FuelChannelGroupSnapshot>(canonical);
    }

    public string DefinitionId { get; }

    public Power TotalFissionThermalPower { get; }

    public Power TotalDecayHeatPower { get; }

    public Power TotalNuclearHeatPower { get; }

    public IReadOnlyList<FuelChannelGroupSnapshot> Groups { get; }

    public FuelChannelGroupSnapshot GetGroup(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Fuel-channel group id cannot be empty or whitespace.", nameof(id));
        }

        return Groups.FirstOrDefault(group => string.Equals(group.GroupId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown fuel-channel group '{id}'.");
    }
}
