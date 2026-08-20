namespace NuclearReactorSimulator.Application.Scenarios.Challenges;

/// <summary>One deterministic observation of an authored challenge condition at a logical simulation step.</summary>
public sealed record ChallengeConditionObservation
{
    public ChallengeConditionObservation(string conditionId, bool isSatisfied, long logicalStep, string evidence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conditionId);
        if (logicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalStep));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(evidence);
        ConditionId = conditionId.Trim();
        IsSatisfied = isSatisfied;
        LogicalStep = logicalStep;
        Evidence = evidence.Trim();
    }

    public string ConditionId { get; }
    public bool IsSatisfied { get; }
    public long LogicalStep { get; }
    public string Evidence { get; }
}
