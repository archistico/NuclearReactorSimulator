using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Recording;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Replay;

/// <summary>
/// Rehydrates canonical challenge evidence from an already-verified recording prefix, then forwards only future live
/// deterministic session evidence. This keeps archive/checkpoint restoration deterministic without an opaque challenge blob.
/// </summary>
internal sealed class ReplayedChallengeContinuationEvidenceSource : IChallengeEvidenceSource, IDisposable
{
    private readonly List<ScenarioOperatorActionRecord> _acceptedOperatorActions = new();
    private ScenarioSession? _liveSession;
    private bool _disposed;

    public ReplayedChallengeContinuationEvidenceSource(ControlRoomSnapshot initialSnapshot)
    {
        Current = initialSnapshot ?? throw new ArgumentNullException(nameof(initialSnapshot));
    }

    public ControlRoomSnapshot Current { get; private set; }

    public IReadOnlyList<ScenarioOperatorActionRecord> AcceptedOperatorActions => _acceptedOperatorActions;

    public event EventHandler<ControlRoomSnapshotChangedEventArgs>? DeterministicStepCompleted;
    public event EventHandler<ScenarioOperatorActionAcceptedEventArgs>? OperatorActionAccepted;

    public void ReplayPrefix(ScenarioRecording recording)
    {
        ArgumentNullException.ThrowIfNull(recording);
        ThrowIfDisposed();
        if (_liveSession is not null)
        {
            throw new InvalidOperationException("Replay prefix must be reconstructed before attaching live session evidence.");
        }
        if (recording.Frames.Count == 0 || recording.Frames[0].LogicalStep != Current.LogicalStep)
        {
            throw new InvalidOperationException("Replay-prefix origin does not match the continuation evidence origin.");
        }

        var actionsByStep = recording.OperatorActions
            .GroupBy(static item => item.LogicalStep)
            .ToDictionary(static group => group.Key, static group => group.OrderBy(static item => item.Sequence).ToArray());

        for (var index = 0; index < recording.Frames.Count; index++)
        {
            var frame = recording.Frames[index];
            if (index == 0)
            {
                if (!ReferenceEquals(frame.Snapshot, Current)
                    && ControlRoomSnapshotFingerprint.Compute(frame.Snapshot) != ControlRoomSnapshotFingerprint.Compute(Current))
                {
                    throw new InvalidOperationException("Replay-prefix initial snapshot does not match continuation evidence.");
                }
            }
            else
            {
                Advance(frame.Snapshot);
            }

            if (actionsByStep.TryGetValue(frame.LogicalStep, out var actions))
            {
                foreach (var action in actions)
                {
                    Accept(action);
                }
            }
        }
    }

    public void AttachLive(ScenarioSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        ThrowIfDisposed();
        if (_liveSession is not null)
        {
            throw new InvalidOperationException("Live session evidence is already attached.");
        }
        if (session.Coordinator.Current.LogicalStep != Current.LogicalStep
            || !string.Equals(
                ControlRoomSnapshotFingerprint.Compute(session.Coordinator.Current),
                ControlRoomSnapshotFingerprint.Compute(Current),
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Live session state does not match reconstructed challenge evidence.");
        }
        if (!session.OperatorActions.Actions.SequenceEqual(_acceptedOperatorActions))
        {
            throw new InvalidOperationException("Live session operator-action history does not match reconstructed challenge evidence.");
        }

        _liveSession = session;
        session.Coordinator.DeterministicStepCompleted += OnLiveDeterministicStepCompleted;
        session.OperatorActions.ActionAccepted += OnLiveOperatorActionAccepted;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        if (_liveSession is not null)
        {
            _liveSession.Coordinator.DeterministicStepCompleted -= OnLiveDeterministicStepCompleted;
            _liveSession.OperatorActions.ActionAccepted -= OnLiveOperatorActionAccepted;
            _liveSession = null;
        }
    }

    private void OnLiveDeterministicStepCompleted(object? sender, ControlRoomSnapshotChangedEventArgs e) => Advance(e.Snapshot);

    private void OnLiveOperatorActionAccepted(object? sender, ScenarioOperatorActionAcceptedEventArgs e) => Accept(e.Action);

    private void Advance(ControlRoomSnapshot snapshot)
    {
        var expected = checked(Current.LogicalStep + 1L);
        if (snapshot.LogicalStep != expected)
        {
            throw new InvalidOperationException($"Challenge continuation expected logical step {expected}, received {snapshot.LogicalStep}.");
        }
        Current = snapshot;
        DeterministicStepCompleted?.Invoke(this, new ControlRoomSnapshotChangedEventArgs(snapshot));
    }

    private void Accept(ScenarioOperatorActionRecord action)
    {
        var expectedSequence = checked((long)_acceptedOperatorActions.Count + 1L);
        if (action.Sequence != expectedSequence || action.LogicalStep != Current.LogicalStep)
        {
            throw new InvalidOperationException("Challenge continuation operator-action evidence is not contiguous with reconstructed history.");
        }
        _acceptedOperatorActions.Add(action);
        OperatorActionAccepted?.Invoke(this, new ScenarioOperatorActionAcceptedEventArgs(action));
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ReplayedChallengeContinuationEvidenceSource));
        }
    }
}
