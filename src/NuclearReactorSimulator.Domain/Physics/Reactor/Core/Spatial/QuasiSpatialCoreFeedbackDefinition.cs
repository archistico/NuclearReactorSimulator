using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Reactor;
using NuclearReactorSimulator.Domain.Physics.Reactor.Feedback;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.Core.Spatial;

/// <summary>
/// Optional M9.4 quasi-spatial refinement over an existing M3.3 aggregated core. Global point kinetics remains
/// authoritative; this definition only specifies local feedback weighting and deterministic power-shape redistribution.
/// </summary>
public sealed class QuasiSpatialCoreFeedbackDefinition
{
    private const double CouplingSumTolerance = 1e-12d;

    public QuasiSpatialCoreFeedbackDefinition(
        string id,
        AggregatedCoreDefinition coreDefinition,
        TemperatureReactivityFeedbackDefinition fuelTemperatureFeedback,
        TemperatureReactivityFeedbackDefinition coolantTemperatureFeedback,
        VoidReactivityFeedbackDefinition voidFeedback,
        CorePowerShapeSensitivity powerShapeSensitivity,
        TimeSpan powerShapeRelaxationTime,
        IEnumerable<CoreZoneCouplingDefinition>? couplings = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Quasi-spatial core-feedback id cannot be empty or whitespace.", nameof(id));
        }

        CoreDefinition = coreDefinition ?? throw new ArgumentNullException(nameof(coreDefinition));
        FuelTemperatureFeedback = fuelTemperatureFeedback ?? throw new ArgumentNullException(nameof(fuelTemperatureFeedback));
        CoolantTemperatureFeedback = coolantTemperatureFeedback ?? throw new ArgumentNullException(nameof(coolantTemperatureFeedback));
        VoidFeedback = voidFeedback ?? throw new ArgumentNullException(nameof(voidFeedback));

        if (fuelTemperatureFeedback.Kind != ReactivityContributionKind.FuelTemperature)
        {
            throw new ArgumentException("Fuel-temperature feedback must use the FuelTemperature contribution kind.", nameof(fuelTemperatureFeedback));
        }

        if (coolantTemperatureFeedback.Kind != ReactivityContributionKind.CoolantTemperature)
        {
            throw new ArgumentException("Coolant-temperature feedback must use the CoolantTemperature contribution kind.", nameof(coolantTemperatureFeedback));
        }

        if (powerShapeRelaxationTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(powerShapeRelaxationTime), powerShapeRelaxationTime, "Power-shape relaxation time must be positive.");
        }

        var canonicalCouplings = (couplings ?? Array.Empty<CoreZoneCouplingDefinition>())
            .Select(item => item ?? throw new ArgumentException("Core-zone coupling collection cannot contain null entries.", nameof(couplings)))
            .OrderBy(static item => CanonicalFirst(item), StringComparer.Ordinal)
            .ThenBy(static item => CanonicalSecond(item), StringComparer.Ordinal)
            .ToArray();

        var pairs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var coupling in canonicalCouplings)
        {
            _ = coreDefinition.GetZone(coupling.FirstZoneId);
            _ = coreDefinition.GetZone(coupling.SecondZoneId);

            var pairKey = $"{CanonicalFirst(coupling)}\u001f{CanonicalSecond(coupling)}";
            if (!pairs.Add(pairKey))
            {
                throw new ArgumentException(
                    $"Duplicate quasi-spatial coupling between '{CanonicalFirst(coupling)}' and '{CanonicalSecond(coupling)}'.",
                    nameof(couplings));
            }
        }

        foreach (var zone in coreDefinition.Zones)
        {
            var sum = canonicalCouplings
                .Where(item => item.Connects(zone.Id))
                .Sum(static item => item.CouplingFraction.Fraction);
            if (sum > 1d + CouplingSumTolerance)
            {
                throw new ArgumentException(
                    $"Quasi-spatial coupling fractions incident on zone '{zone.Id}' must sum to at most 1.0; actual sum is {sum:R}.",
                    nameof(couplings));
            }
        }

        Id = id.Trim();
        PowerShapeSensitivity = powerShapeSensitivity;
        PowerShapeRelaxationTime = powerShapeRelaxationTime;
        Couplings = new ReadOnlyCollection<CoreZoneCouplingDefinition>(canonicalCouplings);
    }

    public string Id { get; }

    public AggregatedCoreDefinition CoreDefinition { get; }

    public TemperatureReactivityFeedbackDefinition FuelTemperatureFeedback { get; }

    public TemperatureReactivityFeedbackDefinition CoolantTemperatureFeedback { get; }

    public VoidReactivityFeedbackDefinition VoidFeedback { get; }

    public CorePowerShapeSensitivity PowerShapeSensitivity { get; }

    public TimeSpan PowerShapeRelaxationTime { get; }

    public IReadOnlyList<CoreZoneCouplingDefinition> Couplings { get; }

    private static string CanonicalFirst(CoreZoneCouplingDefinition coupling)
        => StringComparer.Ordinal.Compare(coupling.FirstZoneId, coupling.SecondZoneId) <= 0
            ? coupling.FirstZoneId
            : coupling.SecondZoneId;

    private static string CanonicalSecond(CoreZoneCouplingDefinition coupling)
        => StringComparer.Ordinal.Compare(coupling.FirstZoneId, coupling.SecondZoneId) <= 0
            ? coupling.SecondZoneId
            : coupling.FirstZoneId;
}
