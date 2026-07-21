namespace NuclearReactorSimulator.Application.Scenarios.Training;

public sealed record TrainingPenaltyAssessment(
    TrainingPenaltyDefinition Definition,
    bool IsTriggered,
    long? FirstTriggeredLogicalStep);
