namespace NuclearReactorSimulator.Application.Scenarios.Training;

public sealed record TrainingCheckpointProgress(
    TrainingCheckpointDefinition Definition,
    bool IsSatisfied,
    long? FirstSatisfiedLogicalStep,
    string Observation);
