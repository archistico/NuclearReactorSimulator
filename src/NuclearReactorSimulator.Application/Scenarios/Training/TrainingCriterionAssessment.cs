namespace NuclearReactorSimulator.Application.Scenarios.Training;

public sealed record TrainingCriterionAssessment(
    TrainingEvaluationCriterionDefinition Definition,
    bool IsSatisfied,
    string Observation);
