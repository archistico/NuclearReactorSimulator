namespace NuclearReactorSimulator.Simulation.Physics.Control.Alarms;

public sealed record AlarmSystemStepResult(AlarmSystemState CandidateState, AlarmSystemSnapshot Snapshot);
