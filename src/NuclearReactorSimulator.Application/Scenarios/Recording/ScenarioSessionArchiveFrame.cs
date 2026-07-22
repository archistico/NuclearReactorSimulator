namespace NuclearReactorSimulator.Application.Scenarios.Recording;

/// <summary>Compact deterministic frame evidence persisted by an M10.7 session archive.</summary>
public sealed record ScenarioSessionArchiveFrame
{
    public ScenarioSessionArchiveFrame(
        long logicalStep,
        string snapshotFingerprint,
        long firstEventSequence,
        long lastEventSequence)
    {
        if (logicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalStep));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotFingerprint);
        if (firstEventSequence < 0 || lastEventSequence < 0 || lastEventSequence < firstEventSequence)
        {
            throw new ArgumentOutOfRangeException(nameof(firstEventSequence));
        }

        LogicalStep = logicalStep;
        SnapshotFingerprint = snapshotFingerprint.Trim();
        FirstEventSequence = firstEventSequence;
        LastEventSequence = lastEventSequence;
    }

    public long LogicalStep { get; }
    public string SnapshotFingerprint { get; }
    public long FirstEventSequence { get; }
    public long LastEventSequence { get; }
}
