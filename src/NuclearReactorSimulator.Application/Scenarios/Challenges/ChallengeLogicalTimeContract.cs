namespace NuclearReactorSimulator.Application.Scenarios.Challenges;

/// <summary>
/// Logical-step-only timing metadata. It deliberately contains no wall-clock time. Target windows are observational;
/// only an explicit hard-deadline offset can produce lifecycle failure.
/// </summary>
public sealed record ChallengeLogicalTimeContract
{
    public ChallengeLogicalTimeContract(
        long readyAtLogicalStep = 0,
        long? targetWindowStartOffsetSteps = null,
        long? targetWindowEndOffsetSteps = null,
        long? hardFailureDeadlineOffsetSteps = null)
    {
        if (readyAtLogicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(readyAtLogicalStep));
        }
        ValidateOffset(targetWindowStartOffsetSteps, nameof(targetWindowStartOffsetSteps));
        ValidateOffset(targetWindowEndOffsetSteps, nameof(targetWindowEndOffsetSteps));
        ValidateOffset(hardFailureDeadlineOffsetSteps, nameof(hardFailureDeadlineOffsetSteps));
        if (targetWindowStartOffsetSteps.HasValue != targetWindowEndOffsetSteps.HasValue)
        {
            throw new ArgumentException("A target completion window requires both start and end offsets.");
        }
        if (targetWindowStartOffsetSteps > targetWindowEndOffsetSteps)
        {
            throw new ArgumentException("Target completion window start must not exceed its end.");
        }
        if (hardFailureDeadlineOffsetSteps.HasValue
            && targetWindowEndOffsetSteps.HasValue
            && hardFailureDeadlineOffsetSteps.Value < targetWindowEndOffsetSteps.Value)
        {
            throw new ArgumentException("Hard failure deadline must not precede the target completion window end.");
        }

        ReadyAtLogicalStep = readyAtLogicalStep;
        TargetWindowStartOffsetSteps = targetWindowStartOffsetSteps;
        TargetWindowEndOffsetSteps = targetWindowEndOffsetSteps;
        HardFailureDeadlineOffsetSteps = hardFailureDeadlineOffsetSteps;
    }

    public long ReadyAtLogicalStep { get; }
    public long? TargetWindowStartOffsetSteps { get; }
    public long? TargetWindowEndOffsetSteps { get; }
    public long? HardFailureDeadlineOffsetSteps { get; }

    private static void ValidateOffset(long? value, string parameterName)
    {
        if (value is < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
