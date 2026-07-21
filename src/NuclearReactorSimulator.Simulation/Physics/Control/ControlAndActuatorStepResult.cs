namespace NuclearReactorSimulator.Simulation.Physics.Control;

public sealed record ControlAndActuatorStepResult(
    ControllerSystemStepResult ControllerStep,
    ActuatorSystemStepResult ActuatorStep,
    ControlAndActuatorState CandidateState,
    ControlAndActuatorSnapshot Snapshot);
