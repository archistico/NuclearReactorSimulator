using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Feedwater;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Turbine;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Feedwater;

/// <summary>
/// Deterministic M4.4 committed-state condensate/feedwater solver.
/// Canonical plant pumps provide the internal mass transport through the existing PlantNetworkOrchestrator;
/// this layer validates ownership, projects pump diagnostics and contributes only explicit feedwater-conditioning heat.
/// </summary>
public sealed class CondensateFeedwaterSystemSolver
{
    private readonly CondensateFeedwaterSystemDefinition _definition;
    private readonly CondenserSystemSolver _condenserSolver;
    private readonly PumpFlowSolver _pumpFlowSolver = new();

    public CondensateFeedwaterSystemSolver(
        CondensateFeedwaterSystemDefinition definition,
        IFluidThermodynamicModel thermodynamicModel)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ArgumentNullException.ThrowIfNull(thermodynamicModel);
        _condenserSolver = new CondenserSystemSolver(definition.CondenserSystem, thermodynamicModel);
    }

    public CondensateFeedwaterSystemDefinition Definition => _definition;

    public CondensateFeedwaterSystemStepResult Step(
        PlantState committedPlantState,
        TurbineExpansionState committedTurbineState,
        CondensateFeedwaterSystemInputs inputs,
        TimeSpan deltaTime)
        => Step(committedPlantState, committedTurbineState, inputs, deltaTime, PlantNetworkSourceTerms.Empty);

    /// <summary>
    /// Higher M4 composition seam. Later secondary/electrical phases may stage additional thermofluid source terms
    /// before the same single plant-network integration while M4.4 retains canonical pump ownership.
    /// </summary>
    public CondensateFeedwaterSystemStepResult Step(
        PlantState committedPlantState,
        TurbineExpansionState committedTurbineState,
        CondensateFeedwaterSystemInputs inputs,
        TimeSpan deltaTime,
        PlantNetworkSourceTerms supplementalSourceTerms)
    {
        ArgumentNullException.ThrowIfNull(committedPlantState);
        ArgumentNullException.ThrowIfNull(committedTurbineState);
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(supplementalSourceTerms);

        if (deltaTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "Condensate/feedwater-system step time must be greater than zero.");
        }

        if (!ReferenceEquals(committedPlantState.Definition, _definition.PlantDefinition))
        {
            throw new ArgumentException("Committed plant state does not use the M4.4 system's canonical plant definition.", nameof(committedPlantState));
        }

        if (!ReferenceEquals(committedTurbineState.Definition, _definition.CondenserSystem.TurbineExpansionSystem))
        {
            throw new ArgumentException("Committed turbine state does not use the M4.4 system's canonical turbine definition.", nameof(committedTurbineState));
        }

        if (!ReferenceEquals(inputs.Definition, _definition))
        {
            throw new ArgumentException("Condensate/feedwater inputs do not use this solver's canonical definition.", nameof(inputs));
        }

        var working = _definition.Trains
            .Select(train => SolveCommittedTrain(committedPlantState, train, inputs.GetTrainInput(train.Id)))
            .OrderBy(static item => item.Definition.Id, StringComparer.Ordinal)
            .ToArray();
        var conditioningTerms = PlantNetworkSourceTerms.Combine(
            BuildConditioningSourceTerms(working),
            supplementalSourceTerms);
        var condenserStep = _condenserSolver.Step(
            committedPlantState,
            committedTurbineState,
            inputs.CondenserInputs,
            deltaTime,
            conditioningTerms);
        var trainSnapshots = working
            .Select(item => BuildSnapshot(item, condenserStep.CandidatePlantState))
            .ToArray();
        var snapshot = new CondensateFeedwaterSystemSnapshot(
            _definition,
            condenserStep.Snapshot,
            trainSnapshots);

        return new CondensateFeedwaterSystemStepResult(condenserStep, snapshot);
    }

    private TrainWorking SolveCommittedTrain(
        PlantState committedPlantState,
        CondensateFeedwaterTrainDefinition definition,
        CondensateFeedwaterTrainInput input)
    {
        var condenser = _definition.CondenserSystem.GetCondenser(definition.CondenserId);
        var feedwaterBoundary = _definition.CondenserSystem
            .TurbineExpansionSystem
            .MainSteamNetwork
            .PrimaryCircuit
            .BoundarySystem
            .GetFeedwaterBoundary(definition.FeedwaterBoundaryId);
        var condensatePump = _definition.PlantDefinition.GetPump(definition.CondensatePumpId);
        var feedwaterPump = _definition.PlantDefinition.GetPump(definition.FeedwaterPumpId);
        var hotwell = committedPlantState.GetFluidNode(condenser.HotwellNodeId);
        var inventory = committedPlantState.GetFluidNode(definition.FeedwaterInventoryNodeId);
        var target = committedPlantState.GetFluidNode(feedwaterBoundary.TargetNodeId);
        var condensatePumpState = committedPlantState.GetPump(condensatePump.Id);
        var feedwaterPumpState = committedPlantState.GetPump(feedwaterPump.Id);
        var condensateResult = _pumpFlowSolver.Solve(condensatePump, condensatePumpState, hotwell, inventory);
        var feedwaterResult = _pumpFlowSolver.Solve(feedwaterPump, feedwaterPumpState, inventory, target);

        return new TrainWorking(
            definition,
            input,
            condenser.HotwellNodeId,
            feedwaterBoundary.TargetNodeId,
            hotwell,
            inventory,
            ToSnapshot(condensatePump, condensatePumpState, condensateResult),
            ToSnapshot(feedwaterPump, feedwaterPumpState, feedwaterResult));
    }

    private static PlantNetworkSourceTerms BuildConditioningSourceTerms(IEnumerable<TrainWorking> working)
    {
        var fluidBalances = new Dictionary<string, FluidNodeBalance>(StringComparer.Ordinal);
        var externalPower = Power.Zero;

        foreach (var item in working.OrderBy(static item => item.Definition.Id, StringComparer.Ordinal))
        {
            var power = item.Input.ThermalConditioningPower;
            if (power != Power.Zero)
            {
                var balance = new FluidNodeBalance(MassFlowRate.Zero, power);
                fluidBalances[item.Definition.FeedwaterInventoryNodeId] = fluidBalances.TryGetValue(item.Definition.FeedwaterInventoryNodeId, out var existing)
                    ? existing + balance
                    : balance;
                externalPower += power;
            }
        }

        return new PlantNetworkSourceTerms(
            fluidBalances,
            new Dictionary<string, ThermalEnergyBalance>(StringComparer.Ordinal),
            MassFlowRate.Zero,
            externalPower);
    }

    private static CondensateFeedwaterTrainSnapshot BuildSnapshot(TrainWorking working, PlantState candidatePlantState)
    {
        var finalHotwell = candidatePlantState.GetFluidNode(working.HotwellNodeId);
        var finalInventory = candidatePlantState.GetFluidNode(working.Definition.FeedwaterInventoryNodeId);

        return new CondensateFeedwaterTrainSnapshot(
            working.Definition.Id,
            working.Definition.CondenserId,
            working.Definition.FeedwaterBoundaryId,
            working.HotwellNodeId,
            working.Definition.FeedwaterInventoryNodeId,
            working.FeedwaterTargetNodeId,
            working.CondensatePump,
            working.FeedwaterPump,
            working.Input.ThermalConditioningPower,
            working.InitialHotwell.Mass,
            finalHotwell.Mass,
            working.InitialInventory.Mass,
            finalInventory.Mass,
            working.InitialInventory.Temperature,
            finalInventory.Temperature,
            working.InitialInventory.SpecificInternalEnergy,
            finalInventory.SpecificInternalEnergy,
            finalInventory.Phase);
    }

    private static FeedwaterPumpSnapshot ToSnapshot(
        PumpDefinition definition,
        PumpState state,
        PumpFlowResult result)
        => new(
            definition.Id,
            definition.Pipe.FromNodeId,
            definition.Pipe.ToNodeId,
            state.IsRunning,
            result.EffectiveSpeed,
            result.MassFlowRate,
            result.ActivePressureBoost,
            result.InternalPressureLoss,
            result.HydraulicPowerExchange,
            result.ShaftPowerDemand);

    private sealed record TrainWorking(
        CondensateFeedwaterTrainDefinition Definition,
        CondensateFeedwaterTrainInput Input,
        string HotwellNodeId,
        string FeedwaterTargetNodeId,
        FluidNodeState InitialHotwell,
        FluidNodeState InitialInventory,
        FeedwaterPumpSnapshot CondensatePump,
        FeedwaterPumpSnapshot FeedwaterPump);
}
