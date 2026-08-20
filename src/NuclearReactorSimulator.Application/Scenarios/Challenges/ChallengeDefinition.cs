namespace NuclearReactorSimulator.Application.Scenarios.Challenges;

/// <summary>
/// Immutable, versioned M10.9.6 challenge definition. It owns deterministic lifecycle metadata only; condition evaluation
/// is scenario/application evidence and never physical control authority.
/// </summary>
public sealed class ChallengeDefinition
{
    private readonly IReadOnlyList<ChallengeConditionDefinition> _requiredObservations;
    private readonly IReadOnlyList<ChallengeConditionDefinition> _completionConditions;
    private readonly IReadOnlyList<ChallengeConditionDefinition> _failureConditions;

    public ChallengeDefinition(
        string challengeId,
        int version,
        string scenarioId,
        string objectiveId,
        string title,
        string description,
        ChallengeConditionDefinition activationCondition,
        IEnumerable<ChallengeConditionDefinition> requiredObservations,
        IEnumerable<ChallengeConditionDefinition> completionConditions,
        IEnumerable<ChallengeConditionDefinition>? failureConditions,
        ChallengeLogicalTimeContract logicalTime,
        ChallengeAssistancePolicy assistancePolicy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(challengeId);
        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectiveId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ActivationCondition = activationCondition ?? throw new ArgumentNullException(nameof(activationCondition));
        LogicalTime = logicalTime ?? throw new ArgumentNullException(nameof(logicalTime));
        AssistancePolicy = assistancePolicy ?? throw new ArgumentNullException(nameof(assistancePolicy));
        ArgumentNullException.ThrowIfNull(requiredObservations);
        ArgumentNullException.ThrowIfNull(completionConditions);

        var required = requiredObservations.ToArray();
        var completion = completionConditions.ToArray();
        var failure = (failureConditions ?? Array.Empty<ChallengeConditionDefinition>()).ToArray();
        if (required.Any(static item => item is null)
            || completion.Any(static item => item is null)
            || failure.Any(static item => item is null))
        {
            throw new ArgumentException("Challenge condition collections cannot contain null entries.");
        }
        if (required.Length == 0)
        {
            throw new ArgumentException("A challenge requires at least one required observation.", nameof(requiredObservations));
        }
        if (completion.Length == 0)
        {
            throw new ArgumentException("A challenge requires at least one completion condition.", nameof(completionConditions));
        }

        var conditionIds = new[] { activationCondition }
            .Concat(required)
            .Concat(completion)
            .Concat(failure)
            .Select(static item => item.ConditionId)
            .ToArray();
        if (conditionIds.Distinct(StringComparer.Ordinal).Count() != conditionIds.Length)
        {
            throw new ArgumentException("Challenge condition IDs must be unique across activation, observation, completion and failure roles.");
        }

        ChallengeId = challengeId.Trim();
        Version = version;
        ScenarioId = scenarioId.Trim();
        ObjectiveId = objectiveId.Trim();
        Title = title.Trim();
        Description = description.Trim();
        _requiredObservations = Array.AsReadOnly(required);
        _completionConditions = Array.AsReadOnly(completion);
        _failureConditions = Array.AsReadOnly(failure);
    }

    public string ChallengeId { get; }
    public int Version { get; }
    public string ExactId => $"{ChallengeId}@{Version}";
    public string ScenarioId { get; }
    public string ObjectiveId { get; }
    public string Title { get; }
    public string Description { get; }
    public ChallengeConditionDefinition ActivationCondition { get; }
    public IReadOnlyList<ChallengeConditionDefinition> RequiredObservations => _requiredObservations;
    public IReadOnlyList<ChallengeConditionDefinition> CompletionConditions => _completionConditions;
    public IReadOnlyList<ChallengeConditionDefinition> FailureConditions => _failureConditions;
    public ChallengeLogicalTimeContract LogicalTime { get; }
    public ChallengeAssistancePolicy AssistancePolicy { get; }
}
