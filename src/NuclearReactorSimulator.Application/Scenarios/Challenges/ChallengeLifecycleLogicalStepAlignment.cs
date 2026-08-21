namespace NuclearReactorSimulator.Application.Scenarios.Challenges;

/// <summary>
/// Shared logical-step alignment for read-only challenge projections. A non-terminal lifecycle must already match the
/// requested evidence step. A terminal lifecycle may be viewed at a later logical step while preserving its frozen
/// terminal step, transitions and observations.
/// </summary>
internal static class ChallengeLifecycleLogicalStepAlignment
{
    public static ChallengeLifecycleSnapshot Align(ChallengeLifecycleSnapshot lifecycle, long logicalStep)
    {
        ArgumentNullException.ThrowIfNull(lifecycle);
        if (logicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalStep));
        }
        if (lifecycle.LogicalStep == logicalStep)
        {
            return lifecycle;
        }
        if (!lifecycle.IsTerminal
            || !lifecycle.TerminalLogicalStep.HasValue
            || lifecycle.TerminalLogicalStep.Value != lifecycle.LogicalStep
            || logicalStep < lifecycle.LogicalStep)
        {
            throw new InvalidOperationException(
                $"Challenge lifecycle logical step {lifecycle.LogicalStep} cannot be aligned to evidence step {logicalStep} unless the lifecycle is terminal at its frozen terminal step.");
        }

        return new ChallengeLifecycleSnapshot(
            lifecycle.ChallengeExactId,
            lifecycle.State,
            logicalStep,
            lifecycle.ActivatedLogicalStep,
            lifecycle.TerminalLogicalStep,
            lifecycle.TargetWindowStartLogicalStep,
            lifecycle.TargetWindowEndLogicalStep,
            lifecycle.HardFailureDeadlineLogicalStep,
            lifecycle.Observations,
            lifecycle.Transitions);
    }
}
