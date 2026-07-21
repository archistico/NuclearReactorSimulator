using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Thermal;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Canonical externally supplied balance contributions for one plant-network step.
/// Terms may contain conservative internal transfers and/or declared signed exchanges across the modeled plant boundary.
/// </summary>
public sealed class PlantNetworkSourceTerms
{
    public PlantNetworkSourceTerms(
        IReadOnlyDictionary<string, FluidNodeBalance> fluidNodeBalances,
        IReadOnlyDictionary<string, ThermalEnergyBalance> thermalBodyBalances,
        Power externalPower)
        : this(fluidNodeBalances, thermalBodyBalances, MassFlowRate.Zero, externalPower)
    {
    }

    public PlantNetworkSourceTerms(
        IReadOnlyDictionary<string, FluidNodeBalance> fluidNodeBalances,
        IReadOnlyDictionary<string, ThermalEnergyBalance> thermalBodyBalances,
        MassFlowRate externalMassFlowRate,
        Power externalPower)
    {
        ArgumentNullException.ThrowIfNull(fluidNodeBalances);
        ArgumentNullException.ThrowIfNull(thermalBodyBalances);

        FluidNodeBalances = CanonicalCopy(fluidNodeBalances);
        ThermalBodyBalances = CanonicalCopy(thermalBodyBalances);
        ExternalMassFlowRate = externalMassFlowRate;
        ExternalPower = externalPower;
    }

    public static PlantNetworkSourceTerms Empty { get; } = new(
        new Dictionary<string, FluidNodeBalance>(StringComparer.Ordinal),
        new Dictionary<string, ThermalEnergyBalance>(StringComparer.Ordinal),
        MassFlowRate.Zero,
        Power.Zero);

    public IReadOnlyDictionary<string, FluidNodeBalance> FluidNodeBalances { get; }

    public IReadOnlyDictionary<string, ThermalEnergyBalance> ThermalBodyBalances { get; }

    /// <summary>
    /// Signed net mass flow across the modeled plant boundary. Positive values add plant inventory; negative values remove it.
    /// Conservative internal transfers declare zero.
    /// </summary>
    public MassFlowRate ExternalMassFlowRate { get; }

    /// <summary>
    /// Signed net power across the modeled plant boundary. Positive values add stored energy; negative values export it.
    /// Conservative internal transfers declare zero.
    /// </summary>
    public Power ExternalPower { get; }

    public static PlantNetworkSourceTerms Combine(params PlantNetworkSourceTerms[] sourceTerms)
        => Combine((IEnumerable<PlantNetworkSourceTerms>)sourceTerms);

    public static PlantNetworkSourceTerms Combine(IEnumerable<PlantNetworkSourceTerms> sourceTerms)
    {
        ArgumentNullException.ThrowIfNull(sourceTerms);

        var terms = sourceTerms
            .Select(term => term ?? throw new ArgumentException("Source-term collections cannot contain null entries.", nameof(sourceTerms)))
            .ToArray();
        if (terms.Length == 0)
        {
            return Empty;
        }

        var fluidBalances = terms
            .SelectMany(static term => term.FluidNodeBalances)
            .GroupBy(static entry => entry.Key, StringComparer.Ordinal)
            .OrderBy(static group => group.Key, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => SumFluidBalances(group.Select(static entry => entry.Value)),
                StringComparer.Ordinal);
        var thermalBalances = terms
            .SelectMany(static term => term.ThermalBodyBalances)
            .GroupBy(static entry => entry.Key, StringComparer.Ordinal)
            .OrderBy(static group => group.Key, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => SumThermalBalances(group.Select(static entry => entry.Value)),
                StringComparer.Ordinal);

        var externalMassFlowRate = MassFlowRate.FromKilogramsPerSecond(
            SumDeterministically(terms.Select(static term => term.ExternalMassFlowRate.KilogramsPerSecond)));
        var externalPower = Power.FromWatts(
            SumDeterministically(terms.Select(static term => term.ExternalPower.Watts)));

        return new PlantNetworkSourceTerms(fluidBalances, thermalBalances, externalMassFlowRate, externalPower);
    }

    private static FluidNodeBalance SumFluidBalances(IEnumerable<FluidNodeBalance> balances)
    {
        var materialized = balances.ToArray();
        return new FluidNodeBalance(
            MassFlowRate.FromKilogramsPerSecond(
                SumDeterministically(materialized.Select(static balance => balance.NetMassFlowRate.KilogramsPerSecond))),
            Power.FromWatts(
                SumDeterministically(materialized.Select(static balance => balance.NetEnergyRate.Watts))));
    }

    private static ThermalEnergyBalance SumThermalBalances(IEnumerable<ThermalEnergyBalance> balances)
        => new(Power.FromWatts(
            SumDeterministically(balances.Select(static balance => balance.NetHeatRate.Watts))));

    private static double SumDeterministically(IEnumerable<double> values)
    {
        var total = 0d;
        var compensation = 0d;
        foreach (var value in values.OrderBy(static value => value))
        {
            var adjusted = value - compensation;
            var next = total + adjusted;
            compensation = (next - total) - adjusted;
            total = next;
        }

        return total;
    }

    private static IReadOnlyDictionary<string, TValue> CanonicalCopy<TValue>(IReadOnlyDictionary<string, TValue> source)
    {
        var sorted = new SortedDictionary<string, TValue>(StringComparer.Ordinal);
        foreach (var entry in source)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
            {
                throw new ArgumentException("Plant-network source-term keys cannot be empty or whitespace.", nameof(source));
            }

            sorted.Add(entry.Key, entry.Value);
        }

        return new ReadOnlyDictionary<string, TValue>(sorted);
    }
}
