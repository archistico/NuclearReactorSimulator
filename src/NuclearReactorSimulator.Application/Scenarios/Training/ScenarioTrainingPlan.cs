namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>Immutable M7.7 training/evaluation overlay for one scenario. It contains no physical inputs.</summary>
public sealed class ScenarioTrainingPlan
{
    private readonly IReadOnlyList<TrainingCheckpointDefinition> _checkpoints;
    private readonly IReadOnlyList<TrainingEvaluationCriterionDefinition> _criteria;
    private readonly IReadOnlyList<TrainingObjectiveEvaluationDefinition> _objectives;
    private readonly IReadOnlyList<TrainingPenaltyDefinition> _penalties;

    public ScenarioTrainingPlan(
        string scenarioId,
        IEnumerable<TrainingCheckpointDefinition> checkpoints,
        IEnumerable<TrainingEvaluationCriterionDefinition> criteria,
        IEnumerable<TrainingObjectiveEvaluationDefinition> objectives,
        IEnumerable<TrainingPenaltyDefinition>? penalties = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        ArgumentNullException.ThrowIfNull(checkpoints);
        ArgumentNullException.ThrowIfNull(criteria);
        ArgumentNullException.ThrowIfNull(objectives);

        var checkpointArray = checkpoints.ToArray();
        var criterionArray = criteria.ToArray();
        var objectiveArray = objectives.ToArray();
        var penaltyArray = (penalties ?? Array.Empty<TrainingPenaltyDefinition>()).ToArray();
        ValidateUnique(checkpointArray.Select(static item => item.CheckpointId), "checkpoint");
        ValidateUnique(criterionArray.Select(static item => item.CriterionId), "criterion");
        ValidateUnique(objectiveArray.Select(static item => item.ObjectiveId), "objective");
        ValidateUnique(penaltyArray.Select(static item => item.PenaltyId), "penalty");

        var checkpointIds = checkpointArray.Select(static item => item.CheckpointId).ToHashSet(StringComparer.Ordinal);
        if (checkpointArray.SelectMany(static item => item.RequiredPriorCheckpointIds).Any(id => !checkpointIds.Contains(id)))
        {
            throw new ArgumentException("Training checkpoint prerequisites must reference checkpoints declared by the training plan.", nameof(checkpoints));
        }
        if (criterionArray.Any(item => item.Kind == TrainingEvaluationCriterionKind.CheckpointSatisfied && !checkpointIds.Contains(item.CheckpointId!)))
        {
            throw new ArgumentException("Checkpoint criteria must reference checkpoints declared by the training plan.", nameof(criteria));
        }

        var criterionIds = criterionArray.Select(static item => item.CriterionId).ToHashSet(StringComparer.Ordinal);
        if (objectiveArray.SelectMany(static item => item.CriterionIds).Any(id => !criterionIds.Contains(id)))
        {
            throw new ArgumentException("Training objectives may reference only criteria declared by the training plan.", nameof(objectives));
        }

        ScenarioId = scenarioId;
        _checkpoints = Array.AsReadOnly(checkpointArray);
        _criteria = Array.AsReadOnly(criterionArray);
        _objectives = Array.AsReadOnly(objectiveArray);
        _penalties = Array.AsReadOnly(penaltyArray);
    }

    public string ScenarioId { get; }
    public IReadOnlyList<TrainingCheckpointDefinition> Checkpoints => _checkpoints;
    public IReadOnlyList<TrainingEvaluationCriterionDefinition> Criteria => _criteria;
    public IReadOnlyList<TrainingObjectiveEvaluationDefinition> Objectives => _objectives;
    public IReadOnlyList<TrainingPenaltyDefinition> Penalties => _penalties;
    public int MaximumScore => _objectives.Sum(static objective => objective.MaximumScore);

    private static void ValidateUnique(IEnumerable<string> values, string label)
    {
        var array = values.ToArray();
        if (array.Any(string.IsNullOrWhiteSpace) || array.Distinct(StringComparer.Ordinal).Count() != array.Length)
        {
            throw new ArgumentException($"Training {label} IDs must be non-empty and unique.");
        }
    }
}
