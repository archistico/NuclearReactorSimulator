namespace NuclearReactorSimulator.Application.Scenarios.Recording;

/// <summary>
/// Versioned replay-backed seek anchor. It is intentionally not an authoritative physical-state dump: restoration always
/// replays the exact scenario/initial-condition/action prefix and verifies the expected snapshot fingerprint fail-closed.
/// </summary>
public sealed record ScenarioCheckpoint
{
    public const int CurrentSchemaVersion = 1;

    public ScenarioCheckpoint(
        string checkpointId,
        int schemaVersion,
        string scenarioId,
        InitialConditionReference initialCondition,
        long logicalStep,
        long lastAppliedOperatorActionSequence,
        string fingerprintAlgorithmId,
        string snapshotFingerprint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(checkpointId);
        if (schemaVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        InitialCondition = initialCondition ?? throw new ArgumentNullException(nameof(initialCondition));
        if (logicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalStep));
        }
        if (lastAppliedOperatorActionSequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lastAppliedOperatorActionSequence));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(fingerprintAlgorithmId);
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotFingerprint);

        CheckpointId = checkpointId.Trim();
        SchemaVersion = schemaVersion;
        ScenarioId = scenarioId.Trim();
        LogicalStep = logicalStep;
        LastAppliedOperatorActionSequence = lastAppliedOperatorActionSequence;
        FingerprintAlgorithmId = fingerprintAlgorithmId.Trim();
        SnapshotFingerprint = snapshotFingerprint.Trim();
    }

    public string CheckpointId { get; }
    public int SchemaVersion { get; }
    public string ScenarioId { get; }
    public InitialConditionReference InitialCondition { get; }
    public long LogicalStep { get; }
    public long LastAppliedOperatorActionSequence { get; }
    public string FingerprintAlgorithmId { get; }
    public string SnapshotFingerprint { get; }
}
