using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Simulation.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;

/// <summary>
/// F.3 committed-state turbine-bypass owner. It evaluates the validated F.1 compressible-flow capacity against the
/// actual condenser steam-space backpressure and stages one internal header-to-condenser mass/internal-energy transfer
/// before the inherited single plant-network commit boundary.
/// </summary>
public sealed class TurbineBypassSolver
{
    private readonly CondenserSystemDefinition _definition;
    private readonly CompressibleSteamFlowSolver _flowSolver = new();

    public TurbineBypassSolver(CondenserSystemDefinition definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public TurbineBypassStepResult Solve(PlantState committedState)
    {
        ArgumentNullException.ThrowIfNull(committedState);
        if (!ReferenceEquals(committedState.Definition, _definition.PlantDefinition))
        {
            throw new ArgumentException(
                "Committed plant state does not use the condenser system's canonical plant definition.",
                nameof(committedState));
        }

        var balances = new Dictionary<string, FluidNodeBalance>(StringComparer.Ordinal);
        var snapshots = new List<TurbineBypassSnapshot>(_definition.TurbineBypasses.Count);

        foreach (var bypass in _definition.TurbineBypasses)
        {
            var condenser = _definition.GetCondenser(bypass.CondenserId);
            var source = committedState.GetFluidNode(bypass.SourceHeaderNodeId);
            var destination = committedState.GetFluidNode(condenser.SteamSpaceNodeId);
            var openFraction = bypass.CalculateOpenFraction(source.Pressure);
            var vaporAvailabilityFraction = ResolveVaporAvailabilityFraction(source);
            var effectiveAreaFraction = openFraction * vaporAvailabilityFraction;
            var flow = _flowSolver.Solve(
                bypass.FlowDefinition,
                source.Pressure,
                source.Temperature,
                destination.Pressure,
                effectiveAreaFraction);
            var energyTransferRate = source.SpecificInternalEnergy * flow.MassFlowRate;

            AddBalance(
                balances,
                source.Id,
                new FluidNodeBalance(-flow.MassFlowRate, -energyTransferRate));
            AddBalance(
                balances,
                destination.Id,
                new FluidNodeBalance(flow.MassFlowRate, energyTransferRate));

            snapshots.Add(new TurbineBypassSnapshot(
                bypass.Id,
                source.Id,
                condenser.Id,
                destination.Id,
                source.Pressure,
                source.Temperature,
                source.Phase,
                source.VaporQuality,
                destination.Pressure,
                openFraction,
                vaporAvailabilityFraction,
                flow.EffectiveThroatArea,
                flow.IsChoked,
                flow.MassFlowRate,
                source.SpecificInternalEnergy,
                energyTransferRate));
        }

        return new TurbineBypassStepResult(
            snapshots,
            new PlantNetworkSourceTerms(
                balances,
                new Dictionary<string, ThermalEnergyBalance>(StringComparer.Ordinal),
                MassFlowRate.Zero,
                Power.Zero));
    }

    private static double ResolveVaporAvailabilityFraction(FluidNodeState source)
        => source.Phase switch
        {
            FluidPhase.SuperheatedVapor => 1d,
            FluidPhase.SaturatedMixture => source.VaporQuality?.Fraction ?? 0d,
            _ => 0d,
        };

    private static void AddBalance(
        IDictionary<string, FluidNodeBalance> balances,
        string nodeId,
        FluidNodeBalance balance)
    {
        balances[nodeId] = balances.TryGetValue(nodeId, out var existing)
            ? existing + balance
            : balance;
    }
}
