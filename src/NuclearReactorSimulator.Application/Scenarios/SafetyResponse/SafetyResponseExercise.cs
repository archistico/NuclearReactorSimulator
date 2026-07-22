using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Training;

namespace NuclearReactorSimulator.Application.Scenarios.SafetyResponse;

/// <summary>One M8.7 safety-response exercise: deterministic scenario plus observational acceptance/scoring metadata.</summary>
public sealed record SafetyResponseExercise
{
    public SafetyResponseExercise(ScenarioDefinition scenario, ScenarioTrainingPlan trainingPlan)
    {
        Scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
        TrainingPlan = trainingPlan ?? throw new ArgumentNullException(nameof(trainingPlan));
        if (!string.Equals(scenario.ScenarioId, trainingPlan.ScenarioId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Safety-response scenario and training-plan IDs must match.", nameof(trainingPlan));
        }

        var scenarioObjectiveIds = scenario.Objectives.Select(static objective => objective.ObjectiveId).OrderBy(static id => id, StringComparer.Ordinal);
        var trainingObjectiveIds = trainingPlan.Objectives.Select(static objective => objective.ObjectiveId).OrderBy(static id => id, StringComparer.Ordinal);
        if (!scenarioObjectiveIds.SequenceEqual(trainingObjectiveIds, StringComparer.Ordinal))
        {
            throw new ArgumentException("Safety-response training objectives must exactly match scenario objectives.", nameof(trainingPlan));
        }
    }

    public ScenarioDefinition Scenario { get; }

    public ScenarioTrainingPlan TrainingPlan { get; }

    public SafetyResponseEvaluationSession Attach(
        ScenarioSession session,
        TrainingGuidanceMode guidanceMode = TrainingGuidanceMode.ChecklistOnly)
        => new(this, session, guidanceMode);
}
