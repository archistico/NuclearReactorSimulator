namespace NuclearReactorSimulator.Application.Scenarios.Recording;

public sealed class ScenarioReplayDivergenceException : InvalidOperationException
{
    public ScenarioReplayDivergenceException(long logicalStep, string expectedFingerprint, string actualFingerprint)
        : base($"Scenario replay diverged at logical step {logicalStep}: expected '{expectedFingerprint}', actual '{actualFingerprint}'.")
    {
        LogicalStep = logicalStep;
        ExpectedFingerprint = expectedFingerprint;
        ActualFingerprint = actualFingerprint;
    }

    public long LogicalStep { get; }
    public string ExpectedFingerprint { get; }
    public string ActualFingerprint { get; }
}
