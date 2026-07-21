namespace NuclearReactorSimulator.Simulation.Runtime.Invariants;

/// <summary>
/// Immutable result of one invariant evaluation.
/// </summary>
public readonly record struct SimulationInvariantResult
{
    private SimulationInvariantResult(bool isSatisfied, string? failureReason)
    {
        IsSatisfied = isSatisfied;
        FailureReason = failureReason;
    }

    public bool IsSatisfied { get; }

    public string? FailureReason { get; }

    public static SimulationInvariantResult Satisfied() => new(true, null);

    public static SimulationInvariantResult Violated(string failureReason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureReason);
        return new SimulationInvariantResult(false, failureReason);
    }
}
