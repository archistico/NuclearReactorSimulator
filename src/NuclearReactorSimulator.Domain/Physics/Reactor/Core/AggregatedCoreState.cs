using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.Core;

/// <summary>
/// Immutable current spatial power-share state for an aggregated core.
/// </summary>
public sealed class AggregatedCoreState
{
    private const double FractionSumTolerance = 1e-12d;

    public AggregatedCoreState(
        AggregatedCoreDefinition definition,
        IEnumerable<CoreZoneState> zones)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(zones);

        var canonicalZones = zones
            .Select(zone => zone ?? throw new ArgumentException("Core-zone state collection cannot contain null entries.", nameof(zones)))
            .OrderBy(static zone => zone.ZoneId, StringComparer.Ordinal)
            .ToArray();

        if (canonicalZones.Select(static zone => zone.ZoneId).Distinct(StringComparer.Ordinal).Count() != canonicalZones.Length)
        {
            throw new ArgumentException("Core-zone state ids must be unique.", nameof(zones));
        }

        var expectedIds = definition.Zones.Select(static zone => zone.Id).ToArray();
        var actualIds = canonicalZones.Select(static zone => zone.ZoneId).ToArray();
        if (!expectedIds.SequenceEqual(actualIds, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"Aggregated-core state must contain exactly one state for every zone. Expected [{string.Join(", ", expectedIds)}], actual [{string.Join(", ", actualIds)}].",
                nameof(zones));
        }

        var fractionSum = CompensatedSum(canonicalZones.Select(static zone => zone.PowerFraction.Fraction));
        if (Math.Abs(fractionSum - 1d) > FractionSumTolerance)
        {
            throw new ArgumentException(
                $"Current core-zone power fractions must sum to 1.0 within tolerance {FractionSumTolerance:G}; actual sum is {fractionSum:R}.",
                nameof(zones));
        }

        Definition = definition;
        Zones = new ReadOnlyCollection<CoreZoneState>(canonicalZones);
        PowerFractionSum = fractionSum;
    }

    public AggregatedCoreDefinition Definition { get; }

    public IReadOnlyList<CoreZoneState> Zones { get; }

    public double PowerFractionSum { get; }

    public CoreZoneState GetZone(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Core-zone id cannot be empty or whitespace.", nameof(id));
        }

        return Zones.FirstOrDefault(zone => string.Equals(zone.ZoneId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown core-zone state '{id}'.");
    }

    public static AggregatedCoreState CreateNominal(AggregatedCoreDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        return new AggregatedCoreState(
            definition,
            definition.Zones.Select(zone => new CoreZoneState(zone.Id, zone.NominalPowerFraction)));
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
