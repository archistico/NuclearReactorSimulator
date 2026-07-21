using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Plant;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.Core;

/// <summary>
/// Immutable configurable spatial decomposition of a core into aggregated zones.
/// Point kinetics remains global; this definition only establishes deterministic spatial ownership.
/// </summary>
public sealed class AggregatedCoreDefinition
{
    private const double FractionSumTolerance = 1e-12d;

    public AggregatedCoreDefinition(
        string id,
        PlantDefinition plantDefinition,
        IEnumerable<CoreZoneDefinition> zones)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Aggregated-core id cannot be empty or whitespace.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(plantDefinition);
        ArgumentNullException.ThrowIfNull(zones);

        var canonicalZones = zones
            .Select(zone => zone ?? throw new ArgumentException("Core-zone collection cannot contain null entries.", nameof(zones)))
            .OrderBy(static zone => zone.Id, StringComparer.Ordinal)
            .ToArray();

        if (canonicalZones.Length == 0)
        {
            throw new ArgumentException("An aggregated core must contain at least one zone.", nameof(zones));
        }

        if (canonicalZones.Select(static zone => zone.Id).Distinct(StringComparer.Ordinal).Count() != canonicalZones.Length)
        {
            throw new ArgumentException("Core-zone ids must be unique.", nameof(zones));
        }

        if (canonicalZones.Select(static zone => zone.Coordinate).Distinct().Count() != canonicalZones.Length)
        {
            throw new ArgumentException("Core-zone logical coordinates must be unique.", nameof(zones));
        }

        foreach (var zone in canonicalZones)
        {
            _ = plantDefinition.GetThermalBody(zone.FuelThermalBodyId);
            _ = plantDefinition.GetThermalBody(zone.StructureThermalBodyId);
            _ = plantDefinition.GetFluidNode(zone.CoolantFluidNodeId);
        }

        var fractionSum = CompensatedSum(canonicalZones.Select(static zone => zone.NominalPowerFraction.Fraction));
        if (Math.Abs(fractionSum - 1d) > FractionSumTolerance)
        {
            throw new ArgumentException(
                $"Nominal core-zone power fractions must sum to 1.0 within tolerance {FractionSumTolerance:G}; actual sum is {fractionSum:R}.",
                nameof(zones));
        }

        Id = id.Trim();
        PlantDefinition = plantDefinition;
        Zones = new ReadOnlyCollection<CoreZoneDefinition>(canonicalZones);
        NominalPowerFractionSum = fractionSum;
    }

    public string Id { get; }

    public PlantDefinition PlantDefinition { get; }

    public IReadOnlyList<CoreZoneDefinition> Zones { get; }

    public double NominalPowerFractionSum { get; }

    public CoreZoneDefinition GetZone(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Core-zone id cannot be empty or whitespace.", nameof(id));
        }

        return Zones.FirstOrDefault(zone => string.Equals(zone.Id, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown core zone '{id}'.");
    }

    public static AggregatedCoreDefinition CreateSingleZone(
        string id,
        PlantDefinition plantDefinition,
        string zoneId,
        string fuelThermalBodyId,
        string structureThermalBodyId,
        string coolantFluidNodeId)
        => new(
            id,
            plantDefinition,
            new[]
            {
                new CoreZoneDefinition(
                    zoneId,
                    new CoreZoneCoordinate(0, 0),
                    CoreZonePowerFraction.Full,
                    fuelThermalBodyId,
                    structureThermalBodyId,
                    coolantFluidNodeId),
            });

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
