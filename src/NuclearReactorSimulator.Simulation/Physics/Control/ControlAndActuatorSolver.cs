using NuclearReactorSimulator.Domain.Physics.Control;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;

namespace NuclearReactorSimulator.Simulation.Physics.Control;

/// <summary>M5.2 composition boundary: measured signals -> deterministic controllers -> typed actuator commands.</summary>
public sealed class ControlAndActuatorSolver
{
    private readonly ActuatorSystemDefinition _definition;
    private readonly ControllerSystemSolver _controllers;
    private readonly ActuatorSystemSolver _actuators;

    public ControlAndActuatorSolver(ActuatorSystemDefinition definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _controllers = new ControllerSystemSolver(definition.ControlSystem);
        _actuators = new ActuatorSystemSolver(definition);
    }

    public ControlAndActuatorStepResult Step(
        MeasuredSignalFrame measuredSignals,
        ControlAndActuatorState committedState,
        ControllerInputs inputs,
        TimeSpan deltaTime)
    {
        ArgumentNullException.ThrowIfNull(committedState);
        if (!ReferenceEquals(committedState.Definition, _definition))
        {
            throw new ArgumentException("Committed control/actuator state does not use this solver's canonical definition.", nameof(committedState));
        }

        var controllerStep = _controllers.Step(measuredSignals, committedState.Controllers, inputs, deltaTime);
        var actuatorStep = _actuators.Step(controllerStep.Snapshot.Outputs, committedState.Actuators);
        var candidateState = new ControlAndActuatorState(
            _definition,
            controllerStep.CandidateState,
            actuatorStep.CandidateState);
        var snapshot = new ControlAndActuatorSnapshot(
            _definition,
            controllerStep.Snapshot,
            actuatorStep.Commands);
        return new ControlAndActuatorStepResult(controllerStep, actuatorStep, candidateState, snapshot);
    }
}
