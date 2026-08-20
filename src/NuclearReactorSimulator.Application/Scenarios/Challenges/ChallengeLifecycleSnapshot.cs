namespace NuclearReactorSimulator.Application.Scenarios.Challenges;

/// <summary>Immutable read model of one challenge lifecycle at the latest observed logical step.</summary>
public sealed record ChallengeLifecycleSnapshot
{
    public ChallengeLifecycleSnapshot(
        string challengeExactId,
        ChallengeLifecycleState state,
        long logicalStep,
        long? activatedLogicalStep,
        long? terminalLogicalStep,
        long? targetWindowStartLogicalStep,
        long? targetWindowEndLogicalStep,
        long? hardFailureDeadlineLogicalStep,
        IReadOnlyList<ChallengeConditionObservation> observations,
        IReadOnlyList<ChallengeLifecycleTransition> transitions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(challengeExactId);
        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }
        if (logicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalStep));
        }
        ChallengeExactId = challengeExactId;
        State = state;
        LogicalStep = logicalStep;
        ActivatedLogicalStep = activatedLogicalStep;
        TerminalLogicalStep = terminalLogicalStep;
        TargetWindowStartLogicalStep = targetWindowStartLogicalStep;
        TargetWindowEndLogicalStep = targetWindowEndLogicalStep;
        HardFailureDeadlineLogicalStep = hardFailureDeadlineLogicalStep;
        Observations = observations ?? throw new ArgumentNullException(nameof(observations));
        Transitions = transitions ?? throw new ArgumentNullException(nameof(transitions));
    }

    public string ChallengeExactId { get; }
    public ChallengeLifecycleState State { get; }
    public long LogicalStep { get; }
    public long? ActivatedLogicalStep { get; }
    public long? TerminalLogicalStep { get; }
    public long? TargetWindowStartLogicalStep { get; }
    public long? TargetWindowEndLogicalStep { get; }
    public long? HardFailureDeadlineLogicalStep { get; }
    public IReadOnlyList<ChallengeConditionObservation> Observations { get; }
    public IReadOnlyList<ChallengeLifecycleTransition> Transitions { get; }

    public bool IsTerminal => State is ChallengeLifecycleState.Completed or ChallengeLifecycleState.Failed or ChallengeLifecycleState.Cancelled;
}
