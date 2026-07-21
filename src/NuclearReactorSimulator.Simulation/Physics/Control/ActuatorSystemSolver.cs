using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;

namespace NuclearReactorSimulator.Simulation.Physics.Control;

/// <summary>Pure command translator from controller outputs to typed valve, pump and rod seams.</summary>
public sealed class ActuatorSystemSolver
{
    private readonly ActuatorSystemDefinition _definition;

    public ActuatorSystemSolver(ActuatorSystemDefinition definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public ActuatorSystemStepResult Step(ControllerOutputFrame controllerOutputs, ActuatorSystemState committedState)
    {
        ArgumentNullException.ThrowIfNull(controllerOutputs);
        ArgumentNullException.ThrowIfNull(committedState);
        if (!ReferenceEquals(controllerOutputs.Definition, _definition.ControlSystem))
        {
            throw new ArgumentException("Controller outputs do not use the actuator system's canonical control definition.", nameof(controllerOutputs));
        }
        if (!ReferenceEquals(committedState.Definition, _definition))
        {
            throw new ArgumentException("Actuator state does not use this solver's canonical definition.", nameof(committedState));
        }

        var valveCommands = new List<ValveActuatorCommand>();
        var pumpCommands = new List<PumpActuatorCommand>();
        var rodCommands = new List<RodActuatorCommand>();
        var candidateStates = new List<ActuatorCommandState>(_definition.Actuators.Count);

        foreach (var actuator in _definition.Actuators)
        {
            var controllerOutput = controllerOutputs.GetOutput(actuator.ControllerId).Output;
            var normalized = actuator.InputRange.Normalize(controllerOutput);
            candidateStates.Add(new ActuatorCommandState(actuator.Id, controllerOutput));

            switch (actuator.TargetKind)
            {
                case ActuatorTargetKind.Valve:
                    valveCommands.Add(new ValveActuatorCommand(actuator.Id, actuator.TargetId, ValvePosition.FromFraction(normalized)));
                    break;
                case ActuatorTargetKind.Pump:
                    var speed = PumpSpeed.FromFraction(normalized);
                    pumpCommands.Add(new PumpActuatorCommand(actuator.Id, actuator.TargetId, speed, !speed.IsStopped));
                    break;
                case ActuatorTargetKind.ControlRod:
                    var motion = ResolveRodMotion(actuator, normalized);
                    rodCommands.Add(new RodActuatorCommand(
                        actuator.Id,
                        new ControlRodCommand(
                            actuator.TargetId,
                            actuator.RodTargetKind ?? throw new InvalidOperationException($"Rod actuator '{actuator.Id}' has no target kind."),
                            motion)));
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported actuator target kind '{actuator.TargetKind}'.");
            }
        }

        return new ActuatorSystemStepResult(
            new ActuatorSystemState(_definition, candidateStates),
            new ActuatorCommandFrame(_definition, valveCommands, pumpCommands, rodCommands));
    }

    private static ControlRodMotion ResolveRodMotion(ActuatorDefinition actuator, double normalized)
    {
        var lower = 0.5d - actuator.RodNeutralDeadbandFraction;
        var upper = 0.5d + actuator.RodNeutralDeadbandFraction;
        if (normalized >= lower && normalized <= upper)
        {
            return ControlRodMotion.Hold;
        }

        var positive = normalized > upper;
        var withdraw = positive == actuator.PositiveRodOutputWithdraws;
        return withdraw ? ControlRodMotion.Withdraw : ControlRodMotion.Insert;
    }
}
