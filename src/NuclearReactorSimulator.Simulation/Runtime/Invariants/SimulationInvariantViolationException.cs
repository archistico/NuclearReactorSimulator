namespace NuclearReactorSimulator.Simulation.Runtime.Invariants;

/// <summary>
/// Raised when a candidate state violates a registered runtime invariant.
/// </summary>
public sealed class SimulationInvariantViolationException : Exception
{
    public SimulationInvariantViolationException(
        string invariantName,
        string failureReason,
        long stepIndex)
        : base($"Simulation invariant '{invariantName}' failed at step {stepIndex}: {failureReason}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invariantName);
        ArgumentException.ThrowIfNullOrWhiteSpace(failureReason);

        InvariantName = invariantName;
        FailureReason = failureReason;
        StepIndex = stepIndex;
    }

    public string InvariantName { get; }

    public string FailureReason { get; }

    public long StepIndex { get; }
}
