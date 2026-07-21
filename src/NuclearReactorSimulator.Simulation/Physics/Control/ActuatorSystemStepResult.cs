namespace NuclearReactorSimulator.Simulation.Physics.Control;

public sealed record ActuatorSystemStepResult(ActuatorSystemState CandidateState, ActuatorCommandFrame Commands);
