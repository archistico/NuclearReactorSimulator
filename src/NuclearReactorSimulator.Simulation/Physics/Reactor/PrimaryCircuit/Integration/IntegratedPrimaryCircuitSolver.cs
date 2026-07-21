using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Integration;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Core;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Core.Channels;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Circulation;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.SteamDrums;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Integration;

/// <summary>
/// Deterministic top-level M3 primary-circuit orchestrator.
/// Every subsystem reads the same committed plant state. Their staged source terms are combined before exactly one
/// <see cref="PlantNetworkOrchestrator"/> integration of conserved inventories.
/// </summary>
public sealed class IntegratedPrimaryCircuitSolver
{
    private readonly IntegratedPrimaryCircuitDefinition _definition;
    private readonly AggregatedCorePowerSolver _coreSolver;
    private readonly FuelChannelGroupSolver _channelGroupSolver;
    private readonly MainCirculationSystemSolver _circulationSolver;
    private readonly SteamDrumSeparationSolver _steamDrumSolver;
    private readonly PrimaryCircuitBoundarySolver _boundarySolver;
    private readonly PlantNetworkOrchestrator _networkOrchestrator;

    public IntegratedPrimaryCircuitSolver(
        IntegratedPrimaryCircuitDefinition definition,
        IFluidThermodynamicModel thermodynamicModel)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ArgumentNullException.ThrowIfNull(thermodynamicModel);

        _coreSolver = new AggregatedCorePowerSolver(definition.CoreDefinition);
        _channelGroupSolver = new FuelChannelGroupSolver(definition.ChannelGroups);
        _circulationSolver = new MainCirculationSystemSolver(definition.MainCirculationSystem);
        _steamDrumSolver = new SteamDrumSeparationSolver(definition.SteamDrumSystem);
        _boundarySolver = new PrimaryCircuitBoundarySolver(definition.BoundarySystem);
        _networkOrchestrator = new PlantNetworkOrchestrator(thermodynamicModel);
    }

    public IntegratedPrimaryCircuitDefinition Definition => _definition;

    public IntegratedPrimaryCircuitStepResult Step(
        PlantState committedPlantState,
        IntegratedPrimaryCircuitInputs inputs,
        TimeSpan deltaTime)
        => Step(committedPlantState, inputs, deltaTime, PlantNetworkSourceTerms.Empty);

    /// <summary>
    /// Higher-phase composition seam for downstream plant systems that must contribute staged source terms
    /// before the same single plant-network integration boundary.
    /// </summary>
    public IntegratedPrimaryCircuitStepResult Step(
        PlantState committedPlantState,
        IntegratedPrimaryCircuitInputs inputs,
        TimeSpan deltaTime,
        PlantNetworkSourceTerms supplementalSourceTerms)
    {
        ArgumentNullException.ThrowIfNull(committedPlantState);
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(supplementalSourceTerms);

        if (!ReferenceEquals(committedPlantState.Definition, _definition.PlantDefinition))
        {
            throw new ArgumentException(
                "Committed plant state does not use the integrated primary circuit's canonical plant definition.",
                nameof(committedPlantState));
        }

        if (!ReferenceEquals(inputs.Definition, _definition))
        {
            throw new ArgumentException(
                "Integrated primary-circuit inputs do not use this solver's canonical definition.",
                nameof(inputs));
        }

        var core = _coreSolver.Solve(inputs.CoreState, inputs.TotalFissionThermalPower, committedPlantState);
        var channelGroups = _channelGroupSolver.Solve(core, inputs.TotalDecayHeatPower, committedPlantState);
        var circulation = _circulationSolver.Solve(committedPlantState);
        var steamDrums = _steamDrumSolver.Solve(committedPlantState, circulation);
        var boundaries = _boundarySolver.Solve(committedPlantState, inputs.BoundaryInputs);

        var combinedSourceTerms = PlantNetworkSourceTerms.Combine(
            channelGroups.SourceTerms,
            steamDrums.SourceTerms,
            boundaries.SourceTerms,
            supplementalSourceTerms);
        var networkStep = _networkOrchestrator.Step(committedPlantState, deltaTime, combinedSourceTerms);

        var snapshot = new IntegratedPrimaryCircuitSnapshot(
            _definition,
            core,
            channelGroups.Snapshot,
            circulation,
            steamDrums.Snapshot,
            boundaries.Snapshot,
            new PlantSnapshot(networkStep.CandidateState),
            networkStep.Audit);

        return new IntegratedPrimaryCircuitStepResult(networkStep, snapshot);
    }
}
