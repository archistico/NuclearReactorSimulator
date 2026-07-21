namespace NuclearReactorSimulator.Application.Scenarios.Training;

public sealed record TrainingObjectiveAssessment(
    ScenarioObjectiveDefinition Objective,
    int Score,
    int MaximumScore,
    bool IsAchieved,
    IReadOnlyList<TrainingCriterionAssessment> Criteria);
