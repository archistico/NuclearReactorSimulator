using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// M7.7 observational training tracker. It samples every deterministic runtime step and the accepted operator-action journal,
/// retaining only first-achievement checkpoints. Guidance mode changes presentation assistance only and never evaluation.
/// </summary>
public sealed class ScenarioTrainingTracker : ITrainingAssistanceDispatcher
{
    private readonly object _gate = new();
    private readonly ScenarioSession _session;
    private readonly ScenarioTrainingPlan _plan;
    private readonly ITrainingCheckpointEvaluator _checkpointEvaluator;
    private readonly Dictionary<string, TrainingCheckpointProgress> _checkpointProgress;
    private TrainingGuidanceMode _guidanceMode;
    private ScenarioTrainingAssessment _assessment;

    public ScenarioTrainingTracker(
        ScenarioSession session,
        ScenarioTrainingPlan plan,
        ITrainingCheckpointEvaluator checkpointEvaluator,
        TrainingGuidanceMode guidanceMode = TrainingGuidanceMode.Guided)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        _checkpointEvaluator = checkpointEvaluator ?? throw new ArgumentNullException(nameof(checkpointEvaluator));
        if (!Enum.IsDefined(guidanceMode))
        {
            throw new ArgumentOutOfRangeException(nameof(guidanceMode));
        }
        if (!string.Equals(session.Scenario.ScenarioId, plan.ScenarioId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Training plan scenario ID must match the loaded session scenario.", nameof(plan));
        }

        _guidanceMode = guidanceMode;
        _checkpointProgress = plan.Checkpoints.ToDictionary(
            static checkpoint => checkpoint.CheckpointId,
            static checkpoint => new TrainingCheckpointProgress(checkpoint, false, null, "Not yet observed."),
            StringComparer.Ordinal);

        ObserveSnapshot(session.Coordinator.Current);
        _assessment = BuildAssessment();
        session.Coordinator.DeterministicStepCompleted += OnDeterministicStepCompleted;
        session.OperatorActions.ActionAccepted += OnActionAccepted;
    }

    public event EventHandler<ScenarioTrainingAssessmentChangedEventArgs>? AssessmentChanged;

    public event EventHandler? GuidanceModeChanged;

    public TrainingGuidanceMode GuidanceMode
    {
        get
        {
            lock (_gate)
            {
                return _guidanceMode;
            }
        }
        set => SetGuidanceMode(value);
    }

    public void SetGuidanceMode(TrainingGuidanceMode mode)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        var changed = false;
        lock (_gate)
        {
            if (_guidanceMode != mode)
            {
                _guidanceMode = mode;
                changed = true;
            }
        }

        if (changed)
        {
            GuidanceModeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ScenarioTrainingAssessment Assessment
    {
        get
        {
            lock (_gate)
            {
                return _assessment;
            }
        }
    }

    private void OnDeterministicStepCompleted(object? sender, ControlRoomSnapshotChangedEventArgs e)
    {
        ObserveAndPublish(e.Snapshot);
    }

    private void OnActionAccepted(object? sender, ScenarioOperatorActionAcceptedEventArgs e)
    {
        ScenarioTrainingAssessment assessment;
        lock (_gate)
        {
            assessment = _assessment = BuildAssessment();
        }
        AssessmentChanged?.Invoke(this, new ScenarioTrainingAssessmentChangedEventArgs(assessment));
    }

    private void ObserveAndPublish(ControlRoomSnapshot snapshot)
    {
        ScenarioTrainingAssessment assessment;
        lock (_gate)
        {
            ObserveSnapshot(snapshot);
            assessment = _assessment = BuildAssessment();
        }
        AssessmentChanged?.Invoke(this, new ScenarioTrainingAssessmentChangedEventArgs(assessment));
    }

    private void ObserveSnapshot(ControlRoomSnapshot snapshot)
    {
        foreach (var checkpoint in _plan.Checkpoints)
        {
            var current = _checkpointProgress[checkpoint.CheckpointId];
            if (current.IsSatisfied)
            {
                continue;
            }

            var pendingPrerequisite = checkpoint.RequiredPriorCheckpointIds
                .FirstOrDefault(id => !_checkpointProgress[id].IsSatisfied);
            if (pendingPrerequisite is not null)
            {
                _checkpointProgress[checkpoint.CheckpointId] = new TrainingCheckpointProgress(
                    checkpoint,
                    false,
                    null,
                    $"Waiting for prerequisite checkpoint '{pendingPrerequisite}'.");
                continue;
            }

            var observation = _checkpointEvaluator.Evaluate(snapshot, checkpoint);
            _checkpointProgress[checkpoint.CheckpointId] = observation.IsSatisfied
                ? new TrainingCheckpointProgress(checkpoint, true, snapshot.LogicalStep, observation.Observation)
                : new TrainingCheckpointProgress(checkpoint, false, null, observation.Observation);
        }
    }

    private ScenarioTrainingAssessment BuildAssessment()
    {
        var actions = _session.OperatorActions.Actions;
        var criteria = _plan.Criteria.ToDictionary(
            static criterion => criterion.CriterionId,
            criterion => EvaluateCriterion(criterion, actions),
            StringComparer.Ordinal);
        var scenarioObjectives = _session.Scenario.Objectives.ToDictionary(static objective => objective.ObjectiveId, StringComparer.Ordinal);
        var objectiveAssessments = new List<TrainingObjectiveAssessment>(_plan.Objectives.Count);

        foreach (var objectiveDefinition in _plan.Objectives)
        {
            if (!scenarioObjectives.TryGetValue(objectiveDefinition.ObjectiveId, out var scenarioObjective))
            {
                throw new InvalidOperationException($"Training objective '{objectiveDefinition.ObjectiveId}' is not declared by scenario '{_session.Scenario.ScenarioId}'.");
            }

            var objectiveCriteria = objectiveDefinition.CriterionIds.Select(id => criteria[id]).ToArray();
            var satisfiedCount = objectiveCriteria.Count(static criterion => criterion.IsSatisfied);
            var score = objectiveDefinition.MaximumScore * satisfiedCount / objectiveCriteria.Length;
            objectiveAssessments.Add(new TrainingObjectiveAssessment(
                scenarioObjective,
                score,
                objectiveDefinition.MaximumScore,
                satisfiedCount == objectiveCriteria.Length,
                Array.AsReadOnly(objectiveCriteria)));
        }

        var penaltyAssessments = _plan.Penalties.Select(penalty =>
        {
            var trigger = actions.FirstOrDefault(action => action.Command.Kind == penalty.TriggerAction);
            return new TrainingPenaltyAssessment(penalty, trigger is not null, trigger?.LogicalStep);
        }).ToArray();
        var objectiveScore = objectiveAssessments.Sum(static objective => objective.Score);
        var penaltyPoints = penaltyAssessments.Where(static penalty => penalty.IsTriggered).Sum(static penalty => penalty.Definition.Points);
        var totalScore = Math.Max(0, objectiveScore - penaltyPoints);

        return new ScenarioTrainingAssessment(
            _session.Scenario.ScenarioId,
            objectiveScore,
            penaltyPoints,
            totalScore,
            _plan.MaximumScore,
            Array.AsReadOnly(objectiveAssessments.ToArray()),
            Array.AsReadOnly(penaltyAssessments),
            Array.AsReadOnly(_checkpointProgress.Values.OrderBy(static progress => progress.Definition.CheckpointId, StringComparer.Ordinal).ToArray()));
    }

    private TrainingCriterionAssessment EvaluateCriterion(
        TrainingEvaluationCriterionDefinition criterion,
        IReadOnlyList<ScenarioOperatorActionRecord> actions)
    {
        return criterion.Kind switch
        {
            TrainingEvaluationCriterionKind.CheckpointSatisfied => EvaluateCheckpointCriterion(criterion),
            TrainingEvaluationCriterionKind.OperatorActionObserved => EvaluateActionObservedCriterion(criterion, actions, expected: true),
            TrainingEvaluationCriterionKind.OperatorActionNotObserved => EvaluateActionObservedCriterion(criterion, actions, expected: false),
            TrainingEvaluationCriterionKind.OperatorActionSequenceObserved => EvaluateActionSequenceCriterion(criterion, actions),
            _ => throw new InvalidOperationException($"Unsupported training criterion kind '{criterion.Kind}'."),
        };
    }

    private TrainingCriterionAssessment EvaluateCheckpointCriterion(TrainingEvaluationCriterionDefinition criterion)
    {
        var progress = _checkpointProgress[criterion.CheckpointId!];
        var observation = progress.IsSatisfied
            ? $"Satisfied at logical step {progress.FirstSatisfiedLogicalStep}: {progress.Observation}"
            : progress.Observation;
        return new TrainingCriterionAssessment(criterion, progress.IsSatisfied, observation);
    }

    private static TrainingCriterionAssessment EvaluateActionObservedCriterion(
        TrainingEvaluationCriterionDefinition criterion,
        IReadOnlyList<ScenarioOperatorActionRecord> actions,
        bool expected)
    {
        var kind = criterion.OperatorActions[0];
        var matching = actions.FirstOrDefault(action => action.Command.Kind == kind);
        var satisfied = expected ? matching is not null : matching is null;
        var observation = matching is null
            ? $"Operator action {kind} has not been accepted."
            : $"Operator action {kind} first accepted at logical step {matching.LogicalStep}, sequence {matching.Sequence}.";
        return new TrainingCriterionAssessment(criterion, satisfied, observation);
    }

    private static TrainingCriterionAssessment EvaluateActionSequenceCriterion(
        TrainingEvaluationCriterionDefinition criterion,
        IReadOnlyList<ScenarioOperatorActionRecord> actions)
    {
        var searchFrom = 0;
        var matched = new List<ScenarioOperatorActionRecord>();
        foreach (var expected in criterion.OperatorActions)
        {
            ScenarioOperatorActionRecord? found = null;
            for (var index = searchFrom; index < actions.Count; index++)
            {
                if (actions[index].Command.Kind == expected)
                {
                    found = actions[index];
                    searchFrom = index + 1;
                    break;
                }
            }
            if (found is null)
            {
                return new TrainingCriterionAssessment(
                    criterion,
                    false,
                    $"Ordered action sequence incomplete; next required action is {expected}.");
            }
            matched.Add(found);
        }

        return new TrainingCriterionAssessment(
            criterion,
            true,
            $"Ordered action sequence observed from logical step {matched[0].LogicalStep} to {matched[^1].LogicalStep}.");
    }
}
