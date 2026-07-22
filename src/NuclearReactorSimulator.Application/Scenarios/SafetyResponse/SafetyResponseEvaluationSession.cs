using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Training;

namespace NuclearReactorSimulator.Application.Scenarios.SafetyResponse;

/// <summary>
/// M8.7 debrief boundary combining deterministic acceptance/scoring with the existing accepted-operator-action journal.
/// It is observational Application state only and never participates in simulation stepping.
/// </summary>
public sealed class SafetyResponseEvaluationSession
{
    public SafetyResponseEvaluationSession(
        SafetyResponseExercise exercise,
        ScenarioSession session,
        TrainingGuidanceMode guidanceMode = TrainingGuidanceMode.ChecklistOnly)
    {
        Exercise = exercise ?? throw new ArgumentNullException(nameof(exercise));
        Session = session ?? throw new ArgumentNullException(nameof(session));
        if (!string.Equals(exercise.Scenario.ScenarioId, session.Scenario.ScenarioId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Loaded scenario must match the safety-response exercise.", nameof(session));
        }

        Tracker = new ScenarioTrainingTracker(
            session,
            exercise.TrainingPlan,
            new SafetyResponseCheckpointEvaluator(),
            guidanceMode);
    }

    public SafetyResponseExercise Exercise { get; }

    public ScenarioSession Session { get; }

    public ScenarioTrainingTracker Tracker { get; }

    public ScenarioTrainingAssessment Assessment => Tracker.Assessment;

    /// <summary>Accepted operator actions ordered only by deterministic logical sequence.</summary>
    public IReadOnlyList<ScenarioOperatorActionRecord> OperatorActionTimeline => Session.OperatorActions.Actions;
}
