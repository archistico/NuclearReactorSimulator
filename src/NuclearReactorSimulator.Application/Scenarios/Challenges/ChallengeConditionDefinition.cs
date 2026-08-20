namespace NuclearReactorSimulator.Application.Scenarios.Challenges;

/// <summary>
/// Authored reference to one deterministic challenge condition. The definition describes intent; a scenario-owned
/// evaluator resolves it only from immutable snapshots and accepted operator-action history.
/// </summary>
public sealed record ChallengeConditionDefinition
{
    public ChallengeConditionDefinition(string conditionId, string title, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conditionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ConditionId = conditionId.Trim();
        Title = title.Trim();
        Description = description.Trim();
    }

    public string ConditionId { get; }
    public string Title { get; }
    public string Description { get; }
}
