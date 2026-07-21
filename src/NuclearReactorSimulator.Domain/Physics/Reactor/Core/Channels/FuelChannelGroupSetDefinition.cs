using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Reactor.Core;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.Core.Channels;

/// <summary>
/// Immutable canonical collection of equivalent fuel-channel groups mapped onto an aggregated core definition.
/// </summary>
public sealed class FuelChannelGroupSetDefinition
{
    private const double FractionSumTolerance = 1e-12d;

    public FuelChannelGroupSetDefinition(
        string id,
        AggregatedCoreDefinition coreDefinition,
        IEnumerable<FuelChannelGroupDefinition> groups)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Fuel-channel group-set id cannot be empty or whitespace.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(coreDefinition);
        ArgumentNullException.ThrowIfNull(groups);

        var canonicalGroups = groups
            .Select(group => group ?? throw new ArgumentException("Fuel-channel group collection cannot contain null entries.", nameof(groups)))
            .OrderBy(static group => group.Id, StringComparer.Ordinal)
            .ToArray();

        if (canonicalGroups.Length == 0)
        {
            throw new ArgumentException("A fuel-channel group set must contain at least one group.", nameof(groups));
        }

        if (canonicalGroups.Select(static group => group.Id).Distinct(StringComparer.Ordinal).Count() != canonicalGroups.Length)
        {
            throw new ArgumentException("Fuel-channel group ids must be unique.", nameof(groups));
        }

        if (canonicalGroups.Select(static group => group.HydraulicPipeId).Distinct(StringComparer.Ordinal).Count() != canonicalGroups.Length)
        {
            throw new ArgumentException("Each fuel-channel group must own a distinct passive hydraulic pipe.", nameof(groups));
        }

        var plant = coreDefinition.PlantDefinition;
        foreach (var group in canonicalGroups)
        {
            var zone = coreDefinition.GetZone(group.ZoneId);
            var pipe = plant.GetPipe(group.HydraulicPipeId);

            if (!string.Equals(pipe.FromNodeId, group.InletCoolantNodeId, StringComparison.Ordinal)
                || !string.Equals(pipe.ToNodeId, group.OutletCoolantNodeId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Fuel-channel group '{group.Id}' hydraulic pipe '{group.HydraulicPipeId}' must run from inlet '{group.InletCoolantNodeId}' to outlet '{group.OutletCoolantNodeId}'.",
                    nameof(groups));
            }

            _ = plant.GetFluidNode(group.InletCoolantNodeId);
            _ = plant.GetFluidNode(group.OutletCoolantNodeId);
            _ = plant.GetThermalBody(group.FuelThermalBodyId);
            _ = plant.GetThermalBody(group.StructureThermalBodyId);

            if (!string.Equals(group.FuelThermalBodyId, zone.FuelThermalBodyId, StringComparison.Ordinal)
                || !string.Equals(group.StructureThermalBodyId, zone.StructureThermalBodyId, StringComparison.Ordinal)
                || !string.Equals(group.OutletCoolantNodeId, zone.CoolantFluidNodeId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Fuel-channel group '{group.Id}' must use the canonical fuel, structure, and outlet-coolant domains of parent zone '{zone.Id}'.",
                    nameof(groups));
            }
        }

        foreach (var zone in coreDefinition.Zones)
        {
            var zoneGroups = canonicalGroups.Where(group => string.Equals(group.ZoneId, zone.Id, StringComparison.Ordinal)).ToArray();
            if (zoneGroups.Length == 0)
            {
                throw new ArgumentException($"Core zone '{zone.Id}' must contain at least one fuel-channel group.", nameof(groups));
            }

            var sum = CompensatedSum(zoneGroups.Select(static group => group.ZonePowerFraction.Fraction));
            if (Math.Abs(sum - 1d) > FractionSumTolerance)
            {
                throw new ArgumentException(
                    $"Fuel-channel group power fractions for zone '{zone.Id}' must sum to 1.0 within tolerance {FractionSumTolerance:G}; actual sum is {sum:R}.",
                    nameof(groups));
            }
        }

        Id = id.Trim();
        CoreDefinition = coreDefinition;
        Groups = new ReadOnlyCollection<FuelChannelGroupDefinition>(canonicalGroups);
        RepresentedChannelCount = canonicalGroups.Sum(static group => group.RepresentedChannelCount);
    }

    public string Id { get; }

    public AggregatedCoreDefinition CoreDefinition { get; }

    public IReadOnlyList<FuelChannelGroupDefinition> Groups { get; }

    public int RepresentedChannelCount { get; }

    public FuelChannelGroupDefinition GetGroup(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Fuel-channel group id cannot be empty or whitespace.", nameof(id));
        }

        return Groups.FirstOrDefault(group => string.Equals(group.Id, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown fuel-channel group '{id}'.");
    }

    private static double CompensatedSum(IEnumerable<double> values)
    {
        var sum = 0d;
        var compensation = 0d;

        foreach (var value in values)
        {
            var adjusted = value - compensation;
            var next = sum + adjusted;
            compensation = (next - sum) - adjusted;
            sum = next;
        }

        return sum;
    }
}
