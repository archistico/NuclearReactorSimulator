using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Control.ReactorPrimary;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Control;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Simulation.Physics.Reactor.Neutronics;
using NuclearReactorSimulator.Simulation.Physics.Reactor.ThermalPower;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.Control.ReactorPrimary;

/// <summary>
/// M5.3 plant-specific control layer. Controllers consume measured signals only. Existing M2 rod/point-kinetics solvers
/// remain the authoritative neutronic dynamics; MCP commands replace only canonical pump operational state before the M4.7 physics step.
/// </summary>
public sealed class ReactorPrimaryControlSolver
{
    private readonly ReactorPrimaryControlSystemDefinition _definition;
    private readonly ControlAndActuatorSolver _controlSolver;
    private readonly ControlRodSystemSolver _rodSolver;
    private readonly ControlRodReactivitySolver _rodReactivitySolver;
    private readonly PointKineticsSolver _pointKineticsSolver;
    private readonly FissionPowerSolver _fissionPowerSolver;

    public ReactorPrimaryControlSolver(ReactorPrimaryControlSystemDefinition definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _controlSolver = new ControlAndActuatorSolver(definition.ActuatorSystem);
        _rodSolver = new ControlRodSystemSolver(definition.ControlRods);
        _rodReactivitySolver = new ControlRodReactivitySolver(definition.ControlRods);
        _pointKineticsSolver = new PointKineticsSolver(definition.PointKineticsParameters);
        _fissionPowerSolver = new FissionPowerSolver(definition.FissionPowerDefinition);
    }

    public ReactorPrimaryControlStepResult Step(
        MeasuredSignalFrame measuredSignals,
        FullPlantState committedFullPlantState,
        ReactorPrimaryControlState committedControlState,
        ReactorPrimaryControlInputs inputs,
        TimeSpan deltaTime)
        => Step(
            measuredSignals,
            committedFullPlantState,
            committedControlState,
            inputs,
            deltaTime,
            scramCommand: false,
            inhibitRodWithdrawal: false);

    /// <summary>
    /// M5.5 protection-aware overload. SCRAM and withdrawal inhibit arbitrate only the rod command seam;
    /// committed rod state still determines current-step reactivity and validated M2 kinetics remains authoritative.
    /// </summary>
    public ReactorPrimaryControlStepResult Step(
        MeasuredSignalFrame measuredSignals,
        FullPlantState committedFullPlantState,
        ReactorPrimaryControlState committedControlState,
        ReactorPrimaryControlInputs inputs,
        TimeSpan deltaTime,
        bool scramCommand,
        bool inhibitRodWithdrawal)
    {
        ArgumentNullException.ThrowIfNull(measuredSignals);
        ArgumentNullException.ThrowIfNull(committedFullPlantState);
        ArgumentNullException.ThrowIfNull(committedControlState);
        ArgumentNullException.ThrowIfNull(inputs);

        if (!ReferenceEquals(committedFullPlantState.Definition, _definition.PlantDefinition))
        {
            throw new ArgumentException("Committed full-plant state does not use the M5.3 canonical plant definition.", nameof(committedFullPlantState));
        }
        if (!ReferenceEquals(committedControlState.Definition, _definition))
        {
            throw new ArgumentException("Committed reactor/primary control state does not use this solver's canonical definition.", nameof(committedControlState));
        }
        if (!ReferenceEquals(inputs.Definition, _definition))
        {
            throw new ArgumentException("Reactor/primary inputs do not use this solver's canonical definition.", nameof(inputs));
        }
        if (deltaTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "M5.3 timestep must be positive.");
        }

        // Physics reads the committed rod state. Commands generated this step advance rods for the next committed state.
        var committedRodReactivity = _rodReactivitySolver.Evaluate(committedControlState.ControlRods);
        var totalReactivityUsed = committedRodReactivity.Total + inputs.NonRodReactivity;
        var candidateKinetics = _pointKineticsSolver.Step(committedControlState.PointKinetics, totalReactivityUsed, deltaTime);
        var kineticsSnapshot = _pointKineticsSolver.CreateSnapshot(candidateKinetics, totalReactivityUsed);
        var fissionPower = _fissionPowerSolver.Solve(candidateKinetics.NeutronPopulation);

        var controlStep = _controlSolver.Step(
            measuredSignals,
            committedControlState.ControlAndActuator,
            inputs.Controllers,
            deltaTime);

        var commands = controlStep.Snapshot.ActuatorCommands;
        var rodCommands = ResolveRodCommands(commands.RodCommands.Select(static item => item.Command), scramCommand, inhibitRodWithdrawal);
        var candidateRods = _rodSolver.Step(committedControlState.ControlRods, rodCommands, deltaTime);
        var commandedPlantState = ApplyPumpCommands(committedFullPlantState.PlantState, commands.PumpCommands);
        var commandedFullPlant = new FullPlantState(
            _definition.PlantDefinition,
            commandedPlantState,
            committedFullPlantState.TurbineState,
            committedFullPlantState.ElectricalState);

        var candidateState = new ReactorPrimaryControlState(
            _definition,
            controlStep.CandidateState,
            candidateRods,
            candidateKinetics);
        var snapshot = new ReactorPrimaryControlSnapshot(
            _definition,
            controlStep.Snapshot,
            committedControlState.ControlRods,
            candidateRods,
            committedRodReactivity,
            _rodReactivitySolver.Evaluate(candidateRods),
            inputs.NonRodReactivity,
            totalReactivityUsed,
            kineticsSnapshot,
            fissionPower,
            BuildLoopDiagnostics(measuredSignals, inputs.Controllers, controlStep));

        return new ReactorPrimaryControlStepResult(controlStep, candidateState, commandedFullPlant, snapshot);
    }


    private IReadOnlyList<ControlRodCommand> ResolveRodCommands(
        IEnumerable<ControlRodCommand> normalCommands,
        bool scramCommand,
        bool inhibitRodWithdrawal)
    {
        if (scramCommand)
        {
            return _definition.ControlRods.Rods
                .Select(static rod => new ControlRodCommand(rod.Id, ControlRodCommandTargetKind.Rod, ControlRodMotion.Insert))
                .ToArray();
        }

        return normalCommands.Select(command => inhibitRodWithdrawal && command.Motion == ControlRodMotion.Withdraw
            ? new ControlRodCommand(command.TargetId, command.TargetKind, ControlRodMotion.Hold)
            : command).ToArray();
    }

    private IReadOnlyList<ReactorPrimaryLoopDiagnosticSnapshot> BuildLoopDiagnostics(
        MeasuredSignalFrame measuredSignals,
        ControllerInputs inputs,
        ControlAndActuatorStepResult step)
    {
        return _definition.Loops.Select(loop =>
        {
            var controller = _definition.ActuatorSystem.ControlSystem.GetController(loop.ControllerId);
            var actuator = _definition.ActuatorSystem.GetActuator(loop.ActuatorId);
            var input = inputs.GetController(loop.ControllerId);
            var measurement = measuredSignals.GetSignal(controller.MeasurementChannelId);
            var output = step.ControllerStep.Snapshot.Outputs.GetOutput(loop.ControllerId);
            return new ReactorPrimaryLoopDiagnosticSnapshot(
                loop.Id,
                loop.Kind,
                loop.ControllerId,
                loop.ActuatorId,
                actuator.TargetId,
                input.Setpoint,
                measurement.EngineeringValue,
                output.Output,
                measurement.EngineeringValue.HasValue && measurement.Validity == SignalValidity.Valid,
                output.IsSaturated);
        }).ToArray();
    }

    private static PlantState ApplyPumpCommands(PlantState committed, IEnumerable<PumpActuatorCommand> commands)
    {
        var byPump = commands.ToDictionary(static item => item.PumpId, StringComparer.Ordinal);
        var pumps = committed.Pumps.Select(state => byPump.TryGetValue(state.PumpId, out var command)
            ? new PumpState(state.PumpId, command.RequestedSpeed, command.RunCommand)
            : state).ToArray();

        return new PlantState(
            committed.Definition,
            committed.FluidNodes,
            committed.Valves,
            pumps,
            committed.ThermalBodies,
            committed.HeatSources);
    }
}
