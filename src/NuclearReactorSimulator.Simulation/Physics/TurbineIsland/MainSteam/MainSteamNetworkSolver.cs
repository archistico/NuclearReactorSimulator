using NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Simulation.Physics.Thermal;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;

/// <summary>
/// Top-level M4.1 steam-supply solver. Main-steam pipes and valves are canonical plant-network components,
/// evaluated from the same committed state and integrated exactly once together with M3 source terms and the turbine-admission boundary.
/// </summary>
public sealed class MainSteamNetworkSolver
{
    private readonly MainSteamNetworkDefinition _definition;
    private readonly IntegratedPrimaryCircuitSolver _primaryCircuitSolver;
    private readonly TurbineAdmissionBoundarySolver _boundarySolver;
    private readonly SteamDrumSeparationSolver _steamDrumSolver;
    private readonly PipeFlowSolver _pipeFlowSolver = new();
    private readonly ValveFlowSolver _valveFlowSolver = new();

    public MainSteamNetworkSolver(
        MainSteamNetworkDefinition definition,
        IFluidThermodynamicModel thermodynamicModel)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ArgumentNullException.ThrowIfNull(thermodynamicModel);

        _primaryCircuitSolver = new IntegratedPrimaryCircuitSolver(definition.PrimaryCircuit, thermodynamicModel);
        _boundarySolver = new TurbineAdmissionBoundarySolver(definition);
        _steamDrumSolver = new SteamDrumSeparationSolver(definition.PrimaryCircuit.SteamDrumSystem);
    }

    public MainSteamNetworkDefinition Definition => _definition;

    public MainSteamNetworkStepResult Step(
        PlantState committedState,
        MainSteamNetworkInputs inputs,
        TimeSpan deltaTime)
        => Step(committedState, inputs, deltaTime, PlantNetworkSourceTerms.Empty);

    /// <summary>
    /// Higher M4 composition seam. Downstream turbine/condenser phases contribute staged source terms
    /// before the same single M3/M4 plant-network integration boundary.
    /// </summary>
    public MainSteamNetworkStepResult Step(
        PlantState committedState,
        MainSteamNetworkInputs inputs,
        TimeSpan deltaTime,
        PlantNetworkSourceTerms supplementalSourceTerms)
    {
        ArgumentNullException.ThrowIfNull(committedState);
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(supplementalSourceTerms);

        if (!ReferenceEquals(committedState.Definition, _definition.PlantDefinition))
        {
            throw new ArgumentException(
                "Committed plant state does not use the main-steam network's canonical plant definition.",
                nameof(committedState));
        }

        if (!ReferenceEquals(inputs.Definition, _definition))
        {
            throw new ArgumentException("Main-steam inputs do not use this solver's canonical definition.", nameof(inputs));
        }

        var lineSnapshots = _definition.SteamLines.Select(line => SolveLine(line, committedState)).ToArray();
        var trainSnapshots = _definition.AdmissionTrains.Select(train => SolveTrain(train, committedState)).ToArray();
        var turbineBoundaries = _boundarySolver.Solve(committedState, inputs);
        var steamSupplyTerms = BuildDemandBalancedSteamSupplyTerms(committedState, lineSnapshots);
        var combinedDownstreamSourceTerms = PlantNetworkSourceTerms.Combine(
            turbineBoundaries.SourceTerms,
            steamSupplyTerms,
            supplementalSourceTerms);
        var primaryStep = _primaryCircuitSolver.Step(
            committedState,
            inputs.PrimaryCircuitInputs,
            deltaTime,
            combinedDownstreamSourceTerms);

        var snapshot = new MainSteamNetworkSnapshot(
            _definition,
            primaryStep.Snapshot,
            lineSnapshots,
            trainSnapshots,
            turbineBoundaries.Snapshots);

        return new MainSteamNetworkStepResult(primaryStep, snapshot);
    }

    private PlantNetworkSourceTerms BuildDemandBalancedSteamSupplyTerms(
        PlantState committedState,
        IReadOnlyList<MainSteamLineSnapshot> lineSnapshots)
    {
        var separation = _steamDrumSolver.Solve(committedState);
        var balances = new Dictionary<string, FluidNodeBalance>(StringComparer.Ordinal);

        foreach (var line in lineSnapshots.OrderBy(static item => item.LineId, StringComparer.Ordinal))
        {
            var export = _definition.PrimaryCircuit.BoundarySystem.GetSteamExportBoundary(line.SteamExportBoundaryId);
            var drumDefinition = _definition.PrimaryCircuit.SteamDrumSystem.GetDrum(export.SteamDrumId);
            if (drumDefinition.LiquidRecirculationMode != SteamDrumLiquidRecirculationMode.CirculationDemandBalanced)
            {
                continue;
            }

            var demandedFlow = Math.Max(0d, line.MassFlowRate.KilogramsPerSecond);
            var returnSeparatedFlow = separation.Snapshot
                .GetDrum(drumDefinition.Id)
                .SeparatedSteamMassFlowRate
                .KilogramsPerSecond;
            var supplementalFlow = MassFlowRate.FromKilogramsPerSecond(
                Math.Max(0d, demandedFlow - returnSeparatedFlow));
            if (supplementalFlow == MassFlowRate.Zero)
            {
                continue;
            }

            // The canonical main-steam pipe removes the steam-outlet node's committed specific energy.
            // Replenishing with the same carried energy makes the internal drum -> outlet transfer exactly
            // mass/energy conservative without clamping either node or inventing an external heat source.
            var carriedEnergyRate = committedState
                .GetFluidNode(drumDefinition.SteamOutletNodeId)
                .SpecificInternalEnergy * supplementalFlow;
            AddBalance(
                balances,
                drumDefinition.InventoryNodeId,
                new FluidNodeBalance(-supplementalFlow, -carriedEnergyRate));
            AddBalance(
                balances,
                drumDefinition.SteamOutletNodeId,
                new FluidNodeBalance(supplementalFlow, carriedEnergyRate));
        }

        return balances.Count == 0
            ? PlantNetworkSourceTerms.Empty
            : new PlantNetworkSourceTerms(
                balances,
                new Dictionary<string, ThermalEnergyBalance>(StringComparer.Ordinal),
                Power.Zero);
    }

    private MainSteamLineSnapshot SolveLine(MainSteamLineDefinition line, PlantState state)
    {
        var export = _definition.PrimaryCircuit.BoundarySystem.GetSteamExportBoundary(line.SteamExportBoundaryId);
        var pipe = _definition.PlantDefinition.GetPipe(line.PipeId);
        var flow = _pipeFlowSolver.Solve(
            pipe,
            state.GetFluidNode(pipe.FromNodeId),
            state.GetFluidNode(pipe.ToNodeId));

        return new MainSteamLineSnapshot(
            line.Id,
            export.Id,
            pipe.Id,
            pipe.FromNodeId,
            pipe.ToNodeId,
            flow.PressureDifference,
            flow.MassFlowRate,
            flow.InternalEnergyFlowRate);
    }

    private TurbineAdmissionTrainSnapshot SolveTrain(TurbineAdmissionTrainDefinition train, PlantState state)
    {
        var stop = SolveValve(train.StopValveId, state);
        var control = SolveValve(train.ControlValveId, state);
        var admission = SolveValve(train.AdmissionValveId, state);
        var turbineInlet = state.GetFluidNode(train.TurbineInletNodeId);

        return new TurbineAdmissionTrainSnapshot(
            train.Id,
            train.HeaderNodeId,
            train.TurbineInletNodeId,
            stop,
            control,
            admission,
            stop.MassFlowRate - control.MassFlowRate,
            control.MassFlowRate - admission.MassFlowRate,
            turbineInlet.Pressure,
            turbineInlet.Temperature,
            turbineInlet.Phase,
            turbineInlet.VaporQuality);
    }

    private MainSteamValveSnapshot SolveValve(string valveId, PlantState state)
    {
        var definition = _definition.PlantDefinition.GetValve(valveId);
        var flow = _valveFlowSolver.Solve(
            definition,
            state.GetValve(valveId),
            state.GetFluidNode(definition.Pipe.FromNodeId),
            state.GetFluidNode(definition.Pipe.ToNodeId));

        return new MainSteamValveSnapshot(
            definition.Id,
            flow.EffectivePosition,
            flow.FlowCoefficient,
            flow.PressureDifference,
            flow.MassFlowRate,
            flow.InternalEnergyFlowRate);
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
