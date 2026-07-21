using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.ThermalPower;

/// <summary>
/// Immutable fission-power calibration plus a complete normalized heat-deposition partition.
/// </summary>
public sealed record FissionPowerDefinition
{
    private const double FractionSumTolerance = 1e-12d;

    public FissionPowerDefinition(
        string id,
        FissionPowerCalibration calibration,
        IEnumerable<FissionHeatDestinationDefinition> heatDestinations)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Fission-power definition id cannot be empty.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(calibration);
        ArgumentNullException.ThrowIfNull(heatDestinations);

        var canonical = heatDestinations
            .Select(static destination => destination ?? throw new ArgumentException(
                "Fission-heat destinations cannot contain null entries.",
                "heatDestinations"))
            .OrderBy(static destination => destination.TargetDomainId, StringComparer.Ordinal)
            .ToArray();

        if (canonical.Length == 0)
        {
            throw new ArgumentException("At least one fission-heat destination is required.", nameof(heatDestinations));
        }

        if (canonical.Select(static destination => destination.TargetDomainId)
            .Distinct(StringComparer.Ordinal)
            .Count() != canonical.Length)
        {
            throw new ArgumentException("Fission-heat target-domain ids must be unique.", nameof(heatDestinations));
        }

        var sum = CompensatedSum(canonical.Select(static destination => destination.Fraction.Fraction));
        if (Math.Abs(sum - 1d) > FractionSumTolerance)
        {
            throw new ArgumentException(
                $"Fission-heat destination fractions must sum to 1.0 within tolerance {FractionSumTolerance:G}; actual sum is {sum:R}.",
                nameof(heatDestinations));
        }

        Id = id;
        Calibration = calibration;
        HeatDestinations = new ReadOnlyCollection<FissionHeatDestinationDefinition>(canonical);
        HeatDestinationFractionSum = sum;
    }

    public string Id { get; }

    public FissionPowerCalibration Calibration { get; }

    public IReadOnlyList<FissionHeatDestinationDefinition> HeatDestinations { get; }

    /// <summary>
    /// Compensated sum retained so the solver can normalize tiny representational error deterministically.
    /// </summary>
    public double HeatDestinationFractionSum { get; }

    public FissionHeatDestinationDefinition GetDestination(string targetDomainId)
        => HeatDestinations.FirstOrDefault(destination => string.Equals(destination.TargetDomainId, targetDomainId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown fission-heat target domain '{targetDomainId}'.");

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
