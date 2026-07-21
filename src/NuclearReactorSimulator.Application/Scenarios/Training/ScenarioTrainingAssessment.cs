namespace NuclearReactorSimulator.Application.Scenarios.Training;

public sealed record ScenarioTrainingAssessment(
    string ScenarioId,
    int ObjectiveScore,
    int PenaltyPoints,
    int TotalScore,
    int MaximumScore,
    IReadOnlyList<TrainingObjectiveAssessment> Objectives,
    IReadOnlyList<TrainingPenaltyAssessment> Penalties,
    IReadOnlyList<TrainingCheckpointProgress> Checkpoints)
{
    public bool AllObjectivesAchieved => Objectives.All(static objective => objective.IsAchieved);
}
