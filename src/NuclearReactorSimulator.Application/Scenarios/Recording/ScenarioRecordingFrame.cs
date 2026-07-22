using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Recording;

/// <summary>
/// One immutable deterministic recorder frame. The presentation snapshot is retained for analysis; the fingerprint is the
/// versioned replay-verification contract and deliberately normalizes host run/pause state.
/// </summary>
public sealed record ScenarioRecordingFrame
{
    public ScenarioRecordingFrame(
        long logicalStep,
        ControlRoomSnapshot snapshot,
        string snapshotFingerprint,
        long firstEventSequence,
        long lastEventSequence)
    {
        if (logicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalStep));
        }
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        if (snapshot.LogicalStep != logicalStep)
        {
            throw new ArgumentException("Recorder frame logical step must match its snapshot.", nameof(snapshot));
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
    public ControlRoomSnapshot Snapshot { get; }
    public string SnapshotFingerprint { get; }
    public long FirstEventSequence { get; }
    public long LastEventSequence { get; }
}
