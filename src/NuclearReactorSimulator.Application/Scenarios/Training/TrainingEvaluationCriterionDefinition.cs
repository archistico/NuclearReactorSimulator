using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>Deterministic criterion over historical checkpoints and accepted operator-action history.</summary>
public sealed record TrainingEvaluationCriterionDefinition
{
    public TrainingEvaluationCriterionDefinition(
        string criterionId,
        string title,
        string description,
        TrainingEvaluationCriterionKind kind,
        string? checkpointId = null,
        IEnumerable<ControlRoomCommandKind>? operatorActions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(criterionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        var actions = (operatorActions ?? Array.Empty<ControlRoomCommandKind>()).ToArray();
        if (actions.Any(static action => !Enum.IsDefined(action)))
        {
            throw new ArgumentOutOfRangeException(nameof(operatorActions));
        }

        switch (kind)
        {
            case TrainingEvaluationCriterionKind.CheckpointSatisfied when string.IsNullOrWhiteSpace(checkpointId):
                throw new ArgumentException("Checkpoint criteria require a checkpoint ID.", nameof(checkpointId));
            case TrainingEvaluationCriterionKind.OperatorActionObserved when actions.Length != 1:
            case TrainingEvaluationCriterionKind.OperatorActionNotObserved when actions.Length != 1:
                throw new ArgumentException("Single-action criteria require exactly one operator action.", nameof(operatorActions));
            case TrainingEvaluationCriterionKind.OperatorActionSequenceObserved when actions.Length < 2:
                throw new ArgumentException("Action-sequence criteria require at least two ordered operator actions.", nameof(operatorActions));
        }

        CriterionId = criterionId;
        Title = title;
        Description = description;
        Kind = kind;
        CheckpointId = checkpointId;
        OperatorActions = Array.AsReadOnly(actions);
    }

    public string CriterionId { get; }
    public string Title { get; }
    public string Description { get; }
    public TrainingEvaluationCriterionKind Kind { get; }
    public string? CheckpointId { get; }
    public IReadOnlyList<ControlRoomCommandKind> OperatorActions { get; }
}
