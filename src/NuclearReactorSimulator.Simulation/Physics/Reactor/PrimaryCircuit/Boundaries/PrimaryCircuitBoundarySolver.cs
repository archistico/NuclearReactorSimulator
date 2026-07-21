using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Boundaries;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;

/// <summary>
/// Deterministic committed-state solver for the temporary M3 feedwater and steam-export external boundaries.
/// It emits signed external mass/energy source terms and never integrates plant state directly.
/// </summary>
public sealed class PrimaryCircuitBoundarySolver
{
    private readonly PrimaryCircuitBoundarySystemDefinition _definition;

    public PrimaryCircuitBoundarySolver(PrimaryCircuitBoundarySystemDefinition definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public PrimaryCircuitBoundarySystemDefinition Definition => _definition;

    public PrimaryCircuitBoundaryStepResult Solve(
        PlantState committedPlantState,
        PrimaryCircuitBoundaryInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(committedPlantState);
        ArgumentNullException.ThrowIfNull(inputs);

        var canonicalPlant = _definition.SteamDrumSystem.MainCirculationSystem.ChannelGroups.CoreDefinition.PlantDefinition;
        if (!ReferenceEquals(committedPlantState.Definition, canonicalPlant))
        {
            throw new ArgumentException(
                "Committed plant state does not use the primary-circuit boundary system's canonical plant definition.",
                nameof(committedPlantState));
        }

        if (!ReferenceEquals(inputs.Definition, _definition))
        {
            throw new ArgumentException(
                "Boundary inputs do not use the solver's canonical primary-circuit boundary definition.",
                nameof(inputs));
        }

        var fluidBalances = new Dictionary<string, FluidNodeBalance>(StringComparer.Ordinal);
        var feedwaterSnapshots = new List<FeedwaterBoundarySnapshot>(_definition.FeedwaterBoundaries.Count);
        var steamExportSnapshots = new List<SteamExportBoundarySnapshot>(_definition.SteamExportBoundaries.Count);
        var externalMassFlowRate = MassFlowRate.Zero;
        var externalPower = Power.Zero;

        foreach (var boundary in _definition.FeedwaterBoundaries)
        {
            var input = inputs.GetFeedwaterInput(boundary.Id);
            var energyInputRate = input.SpecificInternalEnergy * input.MassFlowRate;
            AddBalance(
                fluidBalances,
                boundary.TargetNodeId,
                new FluidNodeBalance(input.MassFlowRate, energyInputRate));
            externalMassFlowRate += input.MassFlowRate;
            externalPower += energyInputRate;

            feedwaterSnapshots.Add(new FeedwaterBoundarySnapshot(
                boundary.Id,
                boundary.SteamDrumId,
                boundary.TargetNodeId,
                input.MassFlowRate,
                input.SpecificInternalEnergy,
                energyInputRate));
        }

        foreach (var boundary in _definition.SteamExportBoundaries)
        {
            var input = inputs.GetSteamExportInput(boundary.Id);
            var sourceState = committedPlantState.GetFluidNode(boundary.SourceNodeId);
            var energyExportRate = sourceState.SpecificInternalEnergy * input.MassFlowRate;
            AddBalance(
                fluidBalances,
                boundary.SourceNodeId,
                new FluidNodeBalance(-input.MassFlowRate, -energyExportRate));
            externalMassFlowRate -= input.MassFlowRate;
            externalPower -= energyExportRate;

            steamExportSnapshots.Add(new SteamExportBoundarySnapshot(
                boundary.Id,
                boundary.SteamDrumId,
                boundary.SourceNodeId,
                input.MassFlowRate,
                sourceState.SpecificInternalEnergy,
                energyExportRate));
        }

        var sourceTerms = new PlantNetworkSourceTerms(
            fluidBalances,
            new Dictionary<string, ThermalEnergyBalance>(StringComparer.Ordinal),
            externalMassFlowRate,
            externalPower);

        var snapshot = new PrimaryCircuitBoundarySystemSnapshot(
            _definition,
            feedwaterSnapshots,
            steamExportSnapshots,
            externalMassFlowRate,
            externalPower);

        return new PrimaryCircuitBoundaryStepResult(snapshot, sourceTerms);
    }

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
