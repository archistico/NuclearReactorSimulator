using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.DecayHeat;

/// <summary>
/// Immutable canonical collection of latent decay-energy inventories.
/// </summary>
public sealed record DecayHeatState
{
    public DecayHeatState(IEnumerable<DecayHeatGroupState> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);

        var canonical = groups
            .Select(static group => group ?? throw new ArgumentException(
                "Decay-heat state groups cannot contain null entries.",
                "groups"))
            .OrderBy(static group => group.GroupId, StringComparer.Ordinal)
            .ToArray();

        if (canonical.Length == 0)
        {
            throw new ArgumentException("Decay-heat state requires at least one group.", nameof(groups));
        }

        if (canonical.Select(static group => group.GroupId).Distinct(StringComparer.Ordinal).Count() != canonical.Length)
        {
            throw new ArgumentException("Decay-heat state group ids must be unique.", nameof(groups));
        }

        Groups = new ReadOnlyCollection<DecayHeatGroupState>(canonical);
        TotalStoredDecayEnergy = Energy.FromJoules(CompensatedSum(canonical.Select(static group => group.StoredDecayEnergy.Joules)));
    }

    public IReadOnlyList<DecayHeatGroupState> Groups { get; }

    public Energy TotalStoredDecayEnergy { get; }

    public DecayHeatGroupState GetGroup(string id)
        => Groups.FirstOrDefault(group => string.Equals(group.GroupId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown decay-heat state group '{id}'.");

    public static DecayHeatState CreateEmpty(DecayHeatDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new DecayHeatState(definition.Groups.Select(static group => new DecayHeatGroupState(group.Id, Energy.Zero)));
    }

    /// <summary>
    /// Creates the long-running steady-state inventory for a constant fission-power level.
    /// For each group E_eq = fraction * P_fission / lambda.
    /// </summary>
    public static DecayHeatState CreateEquilibrium(
        DecayHeatDefinition definition,
        Power fissionThermalPower)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (fissionThermalPower < Power.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fissionThermalPower),
                fissionThermalPower,
                "Fission thermal power cannot be negative.");
        }

        var groups = new DecayHeatGroupState[definition.Groups.Count];
        for (var index = 0; index < groups.Length; index++)
        {
            var definitionGroup = definition.Groups[index];
            var sourceWatts = fissionThermalPower.Watts * definitionGroup.GenerationFraction.Fraction;
            var joules = sourceWatts / definitionGroup.DecayConstant.PerSecond;

            if (!double.IsFinite(joules) || joules < 0d)
            {
                throw new InvalidOperationException(
                    $"Decay-heat equilibrium inventory for group '{definitionGroup.Id}' is outside the finite supported range.");
            }

            groups[index] = new DecayHeatGroupState(definitionGroup.Id, Energy.FromJoules(joules));
        }

        return new DecayHeatState(groups);
    }

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
