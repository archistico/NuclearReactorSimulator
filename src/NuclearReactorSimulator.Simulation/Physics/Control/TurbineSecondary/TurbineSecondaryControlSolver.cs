using NuclearReactorSimulator.Domain.Physics.Control;
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
        => Step(
            measuredSignals,
            committedFullPlantState,
            committedControlState,
            inputs,
            plantInputs: null,
            deltaTime: deltaTime);

    public TurbineSecondaryControlStepResult Step(
        MeasuredSignalFrame measuredSignals,
        FullPlantState committedFullPlantState,
        TurbineSecondaryControlState committedControlState,
        TurbineSecondaryControlInputs inputs,
        IntegratedSecondaryCycleInputs? plantInputs,
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
        if (plantInputs is not null && !ReferenceEquals(plantInputs.Definition, _definition.PlantDefinition))
        {
            throw new ArgumentException("Plant inputs do not use this solver's canonical integrated secondary-cycle definition.", nameof(plantInputs));
        }
        if (deltaTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "M5.4 timestep must be positive.");
        }

        var effectiveControllerInputs = ResolveGovernorControllerInputs(
            committedFullPlantState,
            inputs.Controllers,
            plantInputs);
        var controlStep = _controlSolver.Step(
            measuredSignals,
            committedControlState.ControlAndActuator,
            effectiveControllerInputs,
            deltaTime);

        var commandedPlantState = ApplyCommands(
            committedFullPlantState.PlantState,
            controlStep.Snapshot.ActuatorCommands,
            deltaTime);
        var commandedFullPlantState = new FullPlantState(
            committedFullPlantState.Definition,
            commandedPlantState,
            committedFullPlantState.TurbineState,
            committedFullPlantState.ElectricalState);
        var candidateState = new TurbineSecondaryControlState(_definition, controlStep.CandidateState);
        var snapshot = new TurbineSecondaryControlSnapshot(
            _definition,
            controlStep.Snapshot,
            BuildLoopDiagnostics(measuredSignals, effectiveControllerInputs, controlStep));

        return new TurbineSecondaryControlStepResult(controlStep, candidateState, commandedFullPlantState, snapshot);
    }

    private IReadOnlyList<TurbineSecondaryLoopDiagnosticSnapshot> BuildLoopDiagnostics(
        MeasuredSignalFrame measuredSignals,
        ControllerInputs effectiveControllerInputs,
        ControlAndActuatorStepResult controlStep)
        => _definition.Loops.Select(loop =>
        {
            var controller = _definition.ActuatorSystem.ControlSystem.GetController(loop.ControllerId);
            var actuator = _definition.ActuatorSystem.GetActuator(loop.ActuatorId);
            var measurement = measuredSignals.GetSignal(controller.MeasurementChannelId);
            var input = effectiveControllerInputs.GetController(loop.ControllerId);
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

    private ControllerInputs ResolveGovernorControllerInputs(
        FullPlantState committedFullPlantState,
        ControllerInputs requestedControllerInputs,
        IntegratedSecondaryCycleInputs? plantInputs)
    {
        var governor = _definition.GovernorDroop;
        if (governor is null || plantInputs is null)
        {
            return requestedControllerInputs;
        }

        var generatorState = committedFullPlantState.ElectricalState.GetGenerator(governor.GeneratorId);
        var requestedSpeedController = requestedControllerInputs.GetController(governor.SpeedControllerId);
        if (!generatorState.BreakerClosed || requestedSpeedController.Mode != ControllerMode.Automatic)
        {
            return requestedControllerInputs;
        }

        var generator = _definition.PlantDefinition.GeneratorGridSystem.GetGenerator(governor.GeneratorId);
        var generatorInput = plantInputs.GeneratorGridInputs.GetGeneratorInput(governor.GeneratorId);
        var requestedLoadFraction = generatorInput.RequestedElectricalPower.Watts / generator.MaximumElectricalPower.Watts;
        requestedLoadFraction = Math.Clamp(requestedLoadFraction, 0d, 1d);

        var synchronousMechanicalRpm =
            _definition.PlantDefinition.GeneratorGridSystem.Grid.NominalFrequency.Hertz
            * 60d
            / generator.PolePairs;
        var effectiveDroopSetpointRpm = synchronousMechanicalRpm
            + (governor.FullLoadSpeedReferenceRise.RevolutionsPerMinute * requestedLoadFraction);

        var effectiveInputs = requestedControllerInputs.Controllers
            .Select(input => string.Equals(input.ControllerId, governor.SpeedControllerId, StringComparison.Ordinal)
                ? new ControllerInput(input.ControllerId, input.Mode, effectiveDroopSetpointRpm, input.ManualOutput)
                : input)
            .ToArray();
        return new ControllerInputs(requestedControllerInputs.Definition, effectiveInputs);
    }

    private PlantState ApplyCommands(PlantState committed, ActuatorCommandFrame commands, TimeSpan deltaTime)
    {
        var valvesById = commands.ValveCommands.ToDictionary(static item => item.ValveId, StringComparer.Ordinal);
        var pumpsById = commands.PumpCommands.ToDictionary(static item => item.PumpId, StringComparer.Ordinal);
        var valveActuatorsByTarget = _definition.ActuatorSystem.Actuators
            .Where(static actuator => actuator.TargetKind == ActuatorTargetKind.Valve)
            .ToDictionary(static actuator => actuator.TargetId, StringComparer.Ordinal);
        var pumpActuatorsByTarget = _definition.ActuatorSystem.Actuators
            .Where(static actuator => actuator.TargetKind == ActuatorTargetKind.Pump)
            .ToDictionary(static actuator => actuator.TargetId, StringComparer.Ordinal);

        var valves = committed.Valves.Select(state =>
        {
            if (!valvesById.TryGetValue(state.ValveId, out var command))
            {
                return state;
            }

            var actuator = valveActuatorsByTarget[state.ValveId];
            var requestedFraction = command.RequestedPosition.Fraction;
            var effectiveFraction = MoveTowards(
                state.Position.Fraction,
                requestedFraction,
                actuator.TravelRate,
                deltaTime);
            return new ValveState(
                state.ValveId,
                ValvePosition.FromFraction(effectiveFraction),
                state.IsFailSafeActive);
        }).ToArray();

        var pumps = committed.Pumps.Select(state =>
        {
            if (!pumpsById.TryGetValue(state.PumpId, out var command))
            {
                return state;
            }

            var actuator = pumpActuatorsByTarget[state.PumpId];
            if (!actuator.TravelRate.HasValue)
            {
                return new PumpState(state.PumpId, command.RequestedSpeed, command.RunCommand);
            }

            var effectiveFraction = MoveTowards(
                state.Speed.Fraction,
                command.RequestedSpeed.Fraction,
                actuator.TravelRate,
                deltaTime);
            var effectiveSpeed = PumpSpeed.FromFraction(effectiveFraction);

            // A zero-speed request ramps down deterministically instead of turning IsRunning off before the
            // physical speed reaches zero. A positive request starts the pump immediately at the first finite
            // ramped speed. This preserves one canonical speed state rather than inventing a second coast-down model.
            var isRunning = command.RunCommand || (state.IsRunning && !effectiveSpeed.IsStopped);
            return new PumpState(state.PumpId, effectiveSpeed, isRunning);
        }).ToArray();

        return new PlantState(
            committed.Definition,
            committed.FluidNodes,
            valves,
            pumps,
            committed.ThermalBodies,
            committed.HeatSources);
    }

    private static double MoveTowards(
        double committedFraction,
        double requestedFraction,
        ActuatorTravelRate? travelRate,
        TimeSpan deltaTime)
    {
        if (!travelRate.HasValue)
        {
            return requestedFraction;
        }

        var maximumDelta = travelRate.Value.FractionPerSecond * deltaTime.TotalSeconds;
        var delta = requestedFraction - committedFraction;
        if (Math.Abs(delta) <= maximumDelta)
        {
            return requestedFraction;
        }

        return committedFraction + (Math.Sign(delta) * maximumDelta);
    }
}
