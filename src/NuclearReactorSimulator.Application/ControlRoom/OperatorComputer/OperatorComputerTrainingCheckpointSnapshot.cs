namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public sealed record OperatorComputerTrainingCheckpointSnapshot(
    string CheckpointId,
    string Title,
    bool IsSatisfied,
    long? FirstSatisfiedLogicalStep,
    string Observation);
