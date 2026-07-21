using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.DecayHeat;

/// <summary>
/// Immutable equivalent-group decay-heat model definition plus a complete heat-deposition partition.
/// </summary>
public sealed record DecayHeatDefinition
{
    private const double FractionSumTolerance = 1e-12d;

    public DecayHeatDefinition(
        string id,
        IEnumerable<DecayHeatGroupDefinition> groups,
        IEnumerable<DecayHeatDestinationDefinition> heatDestinations)
    {
        Id = string.IsNullOrWhiteSpace(id)
            ? throw new ArgumentException("Decay-heat definition id is required.", nameof(id))
            : id.Trim();

        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(heatDestinations);

        var canonicalGroups = groups
            .Select(static group => group ?? throw new ArgumentException(
                "Decay-heat groups cannot contain null entries.",
                "groups"))
            .OrderBy(static group => group.Id, StringComparer.Ordinal)
            .ToArray();

        if (canonicalGroups.Length == 0)
        {
            throw new ArgumentException("At least one decay-heat group is required.", nameof(groups));
        }

        if (canonicalGroups.Select(static group => group.Id).Distinct(StringComparer.Ordinal).Count() != canonicalGroups.Length)
        {
            throw new ArgumentException("Decay-heat group ids must be unique.", nameof(groups));
        }

        var generationFractionSum = CompensatedSum(canonicalGroups.Select(static group => group.GenerationFraction.Fraction));
        if (generationFractionSum <= 0d || generationFractionSum > 1d + FractionSumTolerance)
        {
            throw new ArgumentOutOfRangeException(
                nameof(groups),
                generationFractionSum,
                "Total decay-heat generation fraction must be greater than zero and no greater than one.");
        }

        var canonicalDestinations = heatDestinations
            .Select(static destination => destination ?? throw new ArgumentException(
                "Decay-heat destinations cannot contain null entries.",
                "heatDestinations"))
            .OrderBy(static destination => destination.TargetDomainId, StringComparer.Ordinal)
            .ToArray();

        if (canonicalDestinations.Length == 0)
        {
            throw new ArgumentException("At least one decay-heat destination is required.", nameof(heatDestinations));
        }

        if (canonicalDestinations.Select(static destination => destination.TargetDomainId)
            .Distinct(StringComparer.Ordinal)
            .Count() != canonicalDestinations.Length)
        {
            throw new ArgumentException("Decay-heat target-domain ids must be unique.", nameof(heatDestinations));
        }

        var destinationFractionSum = CompensatedSum(canonicalDestinations.Select(static destination => destination.Fraction.Fraction));
        if (Math.Abs(destinationFractionSum - 1d) > FractionSumTolerance)
        {
            throw new ArgumentException(
                $"Decay-heat destination fractions must sum to 1.0 within tolerance {FractionSumTolerance:G}; actual sum is {destinationFractionSum:R}.",
                nameof(heatDestinations));
        }

        Groups = new ReadOnlyCollection<DecayHeatGroupDefinition>(canonicalGroups);
        HeatDestinations = new ReadOnlyCollection<DecayHeatDestinationDefinition>(canonicalDestinations);
        TotalGenerationFraction = DecayHeatGenerationFraction.FromFraction(Math.Min(1d, generationFractionSum));
        HeatDestinationFractionSum = destinationFractionSum;
    }

    public string Id { get; }

    public IReadOnlyList<DecayHeatGroupDefinition> Groups { get; }

    public IReadOnlyList<DecayHeatDestinationDefinition> HeatDestinations { get; }

    public DecayHeatGenerationFraction TotalGenerationFraction { get; }

    /// <summary>
    /// Compensated sum retained so emitted heat can be partitioned deterministically.
    /// </summary>
    public double HeatDestinationFractionSum { get; }

    public DecayHeatGroupDefinition GetGroup(string id)
        => Groups.FirstOrDefault(group => string.Equals(group.Id, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown decay-heat group '{id}'.");

    public DecayHeatDestinationDefinition GetDestination(string targetDomainId)
        => HeatDestinations.FirstOrDefault(destination => string.Equals(destination.TargetDomainId, targetDomainId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown decay-heat target domain '{targetDomainId}'.");

    private static double CompensatedSum(IEnumerable<double> values)
    {
        var sum = 0d;
        var compensation = 0d;

        foreach (var value in values)
        {
            var corrected = value - compensation;
            var next = sum + corrected;
            compensation = (next - sum) - corrected;
            sum = next;
        }

        return sum;
    }
}
