using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Instantaneous hydraulic evaluation over one immutable plant state. It contains no integration and no state mutation.
/// </summary>
public sealed class SemiImplicitHydraulicEvaluation
{
    internal SemiImplicitHydraulicEvaluation(
        SortedDictionary<string, FluidNodeBalance> fluidNodeBalances,
        SortedDictionary<string, MassFlowRate> pipeMassFlowRates,
        SortedDictionary<string, MassFlowRate> valveMassFlowRates,
        SortedDictionary<string, MassFlowRate> pumpMassFlowRates,
        Power pumpHydraulicPowerExchange,
        double massRateClosureResidualKilogramsPerSecond,
        double hydraulicEnergyOwnershipResidualWatts,
        HydraulicComponentEvaluationSnapshot? componentSnapshot = null)
    {
        FluidNodeBalances = new ReadOnlyDictionary<string, FluidNodeBalance>(fluidNodeBalances);
        PipeMassFlowRates = new ReadOnlyDictionary<string, MassFlowRate>(pipeMassFlowRates);
        ValveMassFlowRates = new ReadOnlyDictionary<string, MassFlowRate>(valveMassFlowRates);
        PumpMassFlowRates = new ReadOnlyDictionary<string, MassFlowRate>(pumpMassFlowRates);
        PumpHydraulicPowerExchange = pumpHydraulicPowerExchange;
        MassRateClosureResidualKilogramsPerSecond = massRateClosureResidualKilogramsPerSecond;
        HydraulicEnergyOwnershipResidualWatts = hydraulicEnergyOwnershipResidualWatts;
        ComponentSnapshot = componentSnapshot;
    }

    public IReadOnlyDictionary<string, FluidNodeBalance> FluidNodeBalances { get; }

    public IReadOnlyDictionary<string, MassFlowRate> PipeMassFlowRates { get; }

    public IReadOnlyDictionary<string, MassFlowRate> ValveMassFlowRates { get; }

    public IReadOnlyDictionary<string, MassFlowRate> PumpMassFlowRates { get; }

    public Power PumpHydraulicPowerExchange { get; }

    public double MassRateClosureResidualKilogramsPerSecond { get; }

    public double HydraulicEnergyOwnershipResidualWatts { get; }

    internal HydraulicComponentEvaluationSnapshot? ComponentSnapshot { get; }

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

}

internal sealed record HydraulicComponentEvaluationSnapshot(
    PlantDefinition Definition,
    IReadOnlyList<FluidNodeState> FluidNodeStates,
    IReadOnlyList<ValveState> ValveStates,
    IReadOnlyList<PumpState> PumpStates,
    PipeFlowResult[] PipeResults,
    ValveFlowResult[] ValveResults,
    PumpFlowResult[] PumpResults);
