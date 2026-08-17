using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Instantaneous hydraulic evaluation over one immutable plant state. It contains no integration and no state mutation.
/// </summary>
public sealed class SemiImplicitHydraulicEvaluation
{
    internal SemiImplicitHydraulicEvaluation(
        IReadOnlyDictionary<string, FluidNodeBalance> fluidNodeBalances,
        IReadOnlyDictionary<string, MassFlowRate> pipeMassFlowRates,
        IReadOnlyDictionary<string, MassFlowRate> valveMassFlowRates,
        IReadOnlyDictionary<string, MassFlowRate> pumpMassFlowRates,
        Power pumpHydraulicPowerExchange,
        double massRateClosureResidualKilogramsPerSecond,
        double hydraulicEnergyOwnershipResidualWatts)
    {
        FluidNodeBalances = CanonicalCopy(fluidNodeBalances);
        PipeMassFlowRates = CanonicalCopy(pipeMassFlowRates);
        ValveMassFlowRates = CanonicalCopy(valveMassFlowRates);
        PumpMassFlowRates = CanonicalCopy(pumpMassFlowRates);
        PumpHydraulicPowerExchange = pumpHydraulicPowerExchange;
        MassRateClosureResidualKilogramsPerSecond = massRateClosureResidualKilogramsPerSecond;
        HydraulicEnergyOwnershipResidualWatts = hydraulicEnergyOwnershipResidualWatts;
    }

    public IReadOnlyDictionary<string, FluidNodeBalance> FluidNodeBalances { get; }

    public IReadOnlyDictionary<string, MassFlowRate> PipeMassFlowRates { get; }

    public IReadOnlyDictionary<string, MassFlowRate> ValveMassFlowRates { get; }

    public IReadOnlyDictionary<string, MassFlowRate> PumpMassFlowRates { get; }

    public Power PumpHydraulicPowerExchange { get; }

    public double MassRateClosureResidualKilogramsPerSecond { get; }

    public double HydraulicEnergyOwnershipResidualWatts { get; }

    public MassFlowRate GetPipeMassFlowRate(string id) => GetFlow(PipeMassFlowRates, id, "pipe");

    public MassFlowRate GetValveMassFlowRate(string id) => GetFlow(ValveMassFlowRates, id, "valve");

    public MassFlowRate GetPumpMassFlowRate(string id) => GetFlow(PumpMassFlowRates, id, "pump");

    private static MassFlowRate GetFlow(IReadOnlyDictionary<string, MassFlowRate> source, string id, string label)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException($"A {label} id cannot be empty or whitespace.", nameof(id));
        }

        return source.TryGetValue(id, out var value)
            ? value
            : throw new KeyNotFoundException($"Unknown hydraulic {label} '{id}'.");
    }

    private static IReadOnlyDictionary<string, TValue> CanonicalCopy<TValue>(IReadOnlyDictionary<string, TValue> source)
    {
        var sorted = new SortedDictionary<string, TValue>(StringComparer.Ordinal);
        foreach (var entry in source)
        {
            sorted.Add(entry.Key, entry.Value);
        }

        return new ReadOnlyDictionary<string, TValue>(sorted);
    }
}
