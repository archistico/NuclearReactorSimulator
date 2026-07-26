using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;

/// <summary>
/// F.2 pressure-actuated main-steam header relief owner. It evaluates the validated F.1 capacity seam from committed
/// source state, limits ideal-vapor capacity by available vapor mass fraction and stages one explicit external mass/
/// energy export before the canonical plant-network integration boundary.
/// </summary>
public sealed class MainSteamReliefBoundarySolver
{
    private readonly MainSteamNetworkDefinition _definition;
    private readonly CompressibleSteamFlowSolver _flowSolver = new();

    public MainSteamReliefBoundarySolver(MainSteamNetworkDefinition definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public MainSteamReliefBoundaryStepResult Solve(PlantState committedState)
    {
        ArgumentNullException.ThrowIfNull(committedState);
        if (!ReferenceEquals(committedState.Definition, _definition.PlantDefinition))
        {
            throw new ArgumentException(
                "Committed plant state does not use the main-steam network's canonical plant definition.",
                nameof(committedState));
        }

        var balances = new Dictionary<string, FluidNodeBalance>(StringComparer.Ordinal);
        var snapshots = new List<MainSteamReliefBoundarySnapshot>(_definition.ReliefBoundaries.Count);
        var externalMassFlowRate = MassFlowRate.Zero;
        var externalPower = Power.Zero;

        foreach (var boundary in _definition.ReliefBoundaries)
        {
            var source = committedState.GetFluidNode(boundary.SourceHeaderNodeId);
            var liftFraction = boundary.CalculateLiftFraction(source.Pressure);
            var vaporAvailabilityFraction = ResolveVaporAvailabilityFraction(source);
            var effectiveAreaFraction = liftFraction * vaporAvailabilityFraction;
            var flow = _flowSolver.Solve(
                boundary.FlowDefinition,
                source.Pressure,
                source.Temperature,
                boundary.ReceiverPressure,
                effectiveAreaFraction);
            var energyExportRate = source.SpecificInternalEnergy * flow.MassFlowRate;
            var balance = new FluidNodeBalance(-flow.MassFlowRate, -energyExportRate);
            balances[boundary.SourceHeaderNodeId] = balances.TryGetValue(boundary.SourceHeaderNodeId, out var existing)
                ? existing + balance
                : balance;
            externalMassFlowRate -= flow.MassFlowRate;
            externalPower -= energyExportRate;

            snapshots.Add(new MainSteamReliefBoundarySnapshot(
                boundary.Id,
                boundary.SourceHeaderNodeId,
                boundary.ReceiverBoundaryId,
                source.Pressure,
                source.Temperature,
                source.Phase,
                source.VaporQuality,
                boundary.ReceiverPressure,
                liftFraction,
                vaporAvailabilityFraction,
                flow.EffectiveThroatArea,
                flow.IsChoked,
                flow.MassFlowRate,
                source.SpecificInternalEnergy,
                energyExportRate));
        }

        return new MainSteamReliefBoundaryStepResult(
            snapshots,
            new PlantNetworkSourceTerms(
                balances,
                new Dictionary<string, ThermalEnergyBalance>(StringComparer.Ordinal),
                externalMassFlowRate,
                externalPower));
    }

    private static double ResolveVaporAvailabilityFraction(FluidNodeState source)
        => source.Phase switch
        {
            FluidPhase.SuperheatedVapor => 1d,
            FluidPhase.SaturatedMixture => source.VaporQuality?.Fraction ?? 0d,
            _ => 0d,
        };
}
