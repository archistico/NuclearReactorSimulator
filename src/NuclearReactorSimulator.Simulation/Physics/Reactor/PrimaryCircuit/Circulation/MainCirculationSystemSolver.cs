using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Circulation;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Circulation;

/// <summary>
/// Produces committed-state diagnostics for the semantic main-circulation system.
/// It never integrates or mutates plant inventories; physical evolution remains owned by PlantNetworkOrchestrator.
/// </summary>
public sealed class MainCirculationSystemSolver
{
    private readonly MainCirculationSystemDefinition _definition;
    private readonly PumpFlowSolver _pumpFlowSolver = new();
    private readonly PipeFlowSolver _pipeFlowSolver = new();
    private readonly WaterSteamVoidFractionSolver _voidFractionSolver = new(new SimplifiedWaterSteamThermodynamicModel());

    public MainCirculationSystemSolver(MainCirculationSystemDefinition definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public MainCirculationSystemDefinition Definition => _definition;

    public MainCirculationSystemSnapshot Solve(PlantState committedPlantState)
    {
        ArgumentNullException.ThrowIfNull(committedPlantState);

        var canonicalPlant = _definition.ChannelGroups.CoreDefinition.PlantDefinition;
        if (!ReferenceEquals(committedPlantState.Definition, canonicalPlant))
        {
            throw new ArgumentException(
                "Committed plant state does not use the main-circulation system's canonical plant definition.",
                nameof(committedPlantState));
        }

        var loopSnapshots = new List<MainCirculationLoopSnapshot>(_definition.Loops.Count);
        foreach (var loop in _definition.Loops)
        {
            var suctionHeader = committedPlantState.GetFluidNode(loop.SuctionHeaderNodeId);
            var pressureHeader = committedPlantState.GetFluidNode(loop.PressureHeaderNodeId);
            var pumpSnapshots = loop.PumpIds.Select(pumpId => SolvePump(pumpId, committedPlantState)).ToArray();
            var branchSnapshots = loop.Branches.Select(branch => SolveBranch(branch, loop, committedPlantState)).ToArray();

            loopSnapshots.Add(new MainCirculationLoopSnapshot(
                loop.Id,
                suctionHeader.Pressure,
                pressureHeader.Pressure,
                pumpSnapshots,
                branchSnapshots));
        }

        return new MainCirculationSystemSnapshot(_definition, loopSnapshots);
    }

    private MainCirculationPumpSnapshot SolvePump(string pumpId, PlantState state)
    {
        var plant = state.Definition;
        var definition = plant.GetPump(pumpId);
        var pumpState = state.GetPump(pumpId);
        var fromNode = state.GetFluidNode(definition.Pipe.FromNodeId);
        var toNode = state.GetFluidNode(definition.Pipe.ToNodeId);
        var flow = _pumpFlowSolver.Solve(definition, pumpState, fromNode, toNode);

        return new MainCirculationPumpSnapshot(
            pumpId,
            pumpState.IsRunning,
            flow.EffectiveSpeed,
            flow.ActivePressureBoost,
            flow.MassFlowRate,
            flow.VolumetricFlowRate,
            flow.HydraulicPowerExchange,
            flow.ShaftPowerDemand);
    }

    private MainCirculationBranchSnapshot SolveBranch(
        MainCirculationBranchDefinition branch,
        MainCirculationLoopDefinition loop,
        PlantState state)
    {
        var plant = state.Definition;
        var group = _definition.ChannelGroups.GetGroup(branch.FuelChannelGroupId);
        var channelPipe = plant.GetPipe(group.HydraulicPipeId);
        var returnPipe = plant.GetPipe(branch.ReturnPipeId);
        var pressureHeader = state.GetFluidNode(loop.PressureHeaderNodeId);
        var outlet = state.GetFluidNode(group.OutletCoolantNodeId);
        var returnCollector = state.GetFluidNode(loop.ReturnCollectorNodeId);
        var channelFlow = _pipeFlowSolver.Solve(channelPipe, pressureHeader, outlet);
        var returnFlow = _pipeFlowSolver.Solve(returnPipe, outlet, returnCollector);

        return new MainCirculationBranchSnapshot(
            group.Id,
            group.RepresentedChannelCount,
            group.HydraulicPipeId,
            branch.ReturnPipeId,
            channelFlow.MassFlowRate,
            returnFlow.MassFlowRate,
            channelFlow.MassFlowRate / group.RepresentedChannelCount,
            channelFlow.MassFlowRate - returnFlow.MassFlowRate,
            channelFlow.PressureDifference,
            returnFlow.PressureDifference,
            outlet.Phase,
            outlet.VaporQuality,
            ResolveVoidFraction(outlet));
    }

    private VoidFraction? ResolveVoidFraction(FluidNodeState state)
        => state.Phase == FluidPhase.Unspecified
            ? null
            : _voidFractionSolver.Resolve(state.Thermodynamics);
}
