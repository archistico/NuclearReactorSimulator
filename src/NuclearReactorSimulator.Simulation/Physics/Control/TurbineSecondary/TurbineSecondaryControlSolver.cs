using NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Control;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Integration;

namespace NuclearReactorSimulator.Simulation.Physics.Control.TurbineSecondary;

/// <summary>
/// M5.4 plant-specific command adapter. It translates measured-signal controller outputs into existing canonical valve/pump
/// operational state before the one authoritative M4.7 physical step; it performs no hydraulic or thermodynamic integration.
/// </summary>
public sealed class TurbineSecondaryControlSolver
{
    private readonly TurbineSecondaryControlSystemDefinition _definition;
    private readonly ControlAndActuatorSolver _controlSolver;

    public TurbineSecondaryControlSolver(TurbineSecondaryControlSystemDefinition definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _controlSolver = new ControlAndActuatorSolver(definition.ActuatorSystem);
    }

    public TurbineSecondaryControlStepResult Step(
        MeasuredSignalFrame measuredSignals,
        FullPlantState committedFullPlantState,
        TurbineSecondaryControlState committedControlState,
        TurbineSecondaryControlInputs inputs,
        TimeSpan deltaTime)
    {
        ArgumentNullException.ThrowIfNull(measuredSignals);
        ArgumentNullException.ThrowIfNull(committedFullPlantState);
        ArgumentNullException.ThrowIfNull(committedControlState);
        ArgumentNullException.ThrowIfNull(inputs);

        if (!ReferenceEquals(committedFullPlantState.Definition, _definition.PlantDefinition))
        {
            throw new ArgumentException("Committed full-plant state does not use the M5.4 canonical plant definition.", nameof(committedFullPlantState));
        }
        if (!ReferenceEquals(committedControlState.Definition, _definition))
        {
            throw new ArgumentException("Committed turbine/secondary control state does not use this solver's canonical definition.", nameof(committedControlState));
        }
        if (!ReferenceEquals(inputs.Definition, _definition))
        {
            throw new ArgumentException("Turbine/secondary inputs do not use this solver's canonical definition.", nameof(inputs));
        }
        if (deltaTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "M5.4 timestep must be positive.");
        }

        var controlStep = _controlSolver.Step(
            measuredSignals,
            committedControlState.ControlAndActuator,
            inputs.Controllers,
            deltaTime);

        var commandedPlantState = ApplyCommands(committedFullPlantState.PlantState, controlStep.Snapshot.ActuatorCommands);
        var commandedFullPlantState = new FullPlantState(
            committedFullPlantState.Definition,
            commandedPlantState,
            committedFullPlantState.TurbineState,
            committedFullPlantState.ElectricalState);
        var candidateState = new TurbineSecondaryControlState(_definition, controlStep.CandidateState);
        var snapshot = new TurbineSecondaryControlSnapshot(
            _definition,
            controlStep.Snapshot,
            BuildLoopDiagnostics(measuredSignals, inputs, controlStep));

        return new TurbineSecondaryControlStepResult(controlStep, candidateState, commandedFullPlantState, snapshot);
    }

    private IReadOnlyList<TurbineSecondaryLoopDiagnosticSnapshot> BuildLoopDiagnostics(
        MeasuredSignalFrame measuredSignals,
        TurbineSecondaryControlInputs inputs,
        ControlAndActuatorStepResult controlStep)
        => _definition.Loops.Select(loop =>
        {
            var controller = _definition.ActuatorSystem.ControlSystem.GetController(loop.ControllerId);
            var actuator = _definition.ActuatorSystem.GetActuator(loop.ActuatorId);
            var measurement = measuredSignals.GetSignal(controller.MeasurementChannelId);
            var input = inputs.Controllers.GetController(loop.ControllerId);
            var output = controlStep.Snapshot.Controllers.Outputs.GetOutput(loop.ControllerId);
            return new TurbineSecondaryLoopDiagnosticSnapshot(
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

    private static PlantState ApplyCommands(PlantState committed, ActuatorCommandFrame commands)
    {
        var valvesById = commands.ValveCommands.ToDictionary(static item => item.ValveId, StringComparer.Ordinal);
        var pumpsById = commands.PumpCommands.ToDictionary(static item => item.PumpId, StringComparer.Ordinal);

        var valves = committed.Valves.Select(state => valvesById.TryGetValue(state.ValveId, out var command)
            ? new ValveState(state.ValveId, command.RequestedPosition, state.IsFailSafeActive)
            : state).ToArray();
        var pumps = committed.Pumps.Select(state => pumpsById.TryGetValue(state.PumpId, out var command)
            ? new PumpState(state.PumpId, command.RequestedSpeed, command.RunCommand)
            : state).ToArray();

        return new PlantState(
            committed.Definition,
            committed.FluidNodes,
            valves,
            pumps,
            committed.ThermalBodies,
            committed.HeatSources);
    }
}
