namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>Scored mapping from scenario objective metadata to deterministic criteria.</summary>
public sealed record TrainingObjectiveEvaluationDefinition
{
    private readonly IReadOnlyList<string> _criterionIds;

    public TrainingObjectiveEvaluationDefinition(string objectiveId, int maximumScore, IEnumerable<string> criterionIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectiveId);
        if (maximumScore <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumScore));
        }
        ArgumentNullException.ThrowIfNull(criterionIds);
        var ids = criterionIds.ToArray();
        if (ids.Length == 0 || ids.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Training objectives require at least one valid criterion ID.", nameof(criterionIds));
        }
        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
        {
            throw new ArgumentException("Criterion IDs within one objective must be unique.", nameof(criterionIds));
        }

        ObjectiveId = objectiveId;
        MaximumScore = maximumScore;
        _criterionIds = Array.AsReadOnly(ids);
    }

    public string ObjectiveId { get; }
    public int MaximumScore { get; }
    public IReadOnlyList<string> CriterionIds => _criterionIds;
}
