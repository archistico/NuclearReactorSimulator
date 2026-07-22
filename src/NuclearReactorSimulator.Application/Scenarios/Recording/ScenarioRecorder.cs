using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Faults;

namespace NuclearReactorSimulator.Application.Scenarios.Recording;

/// <summary>
/// Session-scoped deterministic recorder. It observes every fixed step plus accepted operator actions and preserves separate
/// M10.5/M10.6 semantic automation intents from the session journal; it never mutates or
/// delays the runtime. Presentation publication stride therefore cannot change the recorded frame sequence.
/// </summary>
public sealed class ScenarioRecorder : IDisposable
{
    private readonly ScenarioSession _session;
    private readonly List<ScenarioRecordingFrame> _frames = new();
    private readonly List<ScenarioRecordingEvent> _events = new();
    private readonly List<ScenarioCheckpoint> _checkpoints = new();
    private ControlRoomSnapshot _previousSnapshot;
    private long _nextEventSequence = 1;
    private bool _completed;
    private bool _disposed;

    public ScenarioRecorder(ScenarioSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        if (session.OperatorActions.Actions.Count != 0 || session.AutomationIntents.Intents.Count != 0)
        {
            throw new InvalidOperationException("Recorder attachment requires a fresh scenario session with no previously accepted operator actions or automation intents.");
        }

        _previousSnapshot = session.Coordinator.Current;
        AddFrame(_previousSnapshot, 0, 0);
        Subscribe();
    }

    /// <summary>Resumes M9.1 recording after an exact replay-backed M10.7 archive restoration.</summary>
    public ScenarioRecorder(ScenarioSession session, ScenarioRecording verifiedPrefix)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        ArgumentNullException.ThrowIfNull(verifiedPrefix);
        if (!string.Equals(session.Scenario.ScenarioId, verifiedPrefix.ScenarioId, StringComparison.Ordinal)
            || session.InitialCondition.Reference != verifiedPrefix.InitialCondition
            || session.Coordinator.Current.LogicalStep != verifiedPrefix.FinalLogicalStep)
        {
            throw new InvalidOperationException("Recorder resume prefix identity/final step does not match the restored scenario session.");
        }
        var actualFingerprint = ControlRoomSnapshotFingerprint.Compute(session.Coordinator.Current);
        if (!string.Equals(actualFingerprint, verifiedPrefix.Frames[^1].SnapshotFingerprint, StringComparison.Ordinal))
        {
            throw new ScenarioReplayDivergenceException(verifiedPrefix.FinalLogicalStep, verifiedPrefix.Frames[^1].SnapshotFingerprint, actualFingerprint);
        }
        if (!session.OperatorActions.Actions.SequenceEqual(verifiedPrefix.OperatorActions)
            || !session.AutomationIntents.Intents.SequenceEqual(verifiedPrefix.AutomationIntents))
        {
            throw new InvalidOperationException("Restored scenario journals do not match the verified recording prefix.");
        }

        _frames.AddRange(verifiedPrefix.Frames);
        _events.AddRange(verifiedPrefix.Events);
        _checkpoints.AddRange(verifiedPrefix.Checkpoints);
        _nextEventSequence = _events.Count == 0 ? 1L : checked(_events[^1].Sequence + 1L);
        _previousSnapshot = session.Coordinator.Current;
        Subscribe();
    }

    public IReadOnlyList<ScenarioRecordingFrame> Frames => Array.AsReadOnly(_frames.ToArray());
    public int FrameCount => _frames.Count;
    public ScenarioRecordingFrame LatestFrame => _frames[^1];
    public IReadOnlyList<ScenarioRecordingEvent> Events => Array.AsReadOnly(_events.ToArray());
    public IReadOnlyList<ScenarioCheckpoint> Checkpoints => Array.AsReadOnly(_checkpoints.ToArray());

    public ScenarioCheckpoint CreateCheckpoint(string checkpointId)
    {
        ThrowIfUnavailable();
        ArgumentException.ThrowIfNullOrWhiteSpace(checkpointId);
        if (_checkpoints.Any(item => string.Equals(item.CheckpointId, checkpointId, StringComparison.Ordinal)))
        {
            throw new ArgumentException($"Checkpoint id '{checkpointId}' already exists in this recording.", nameof(checkpointId));
        }

        var frame = _frames[^1];
        var lastAppliedActionSequence = _session.OperatorActions.Actions
            .Where(action => checked(action.LogicalStep + 1) <= frame.LogicalStep)
            .Select(static action => action.Sequence)
            .DefaultIfEmpty(0L)
            .Max();
        var checkpoint = new ScenarioCheckpoint(
            checkpointId,
            ScenarioCheckpoint.CurrentSchemaVersion,
            _session.Scenario.ScenarioId,
            _session.InitialCondition.Reference,
            frame.LogicalStep,
            lastAppliedActionSequence,
            ControlRoomSnapshotFingerprint.AlgorithmId,
            frame.SnapshotFingerprint);
        _checkpoints.Add(checkpoint);
        return checkpoint;
    }

    public ScenarioRecording Capture()
    {
        ThrowIfUnavailable();
        EnsureNoPendingAcceptedIntent();
        return BuildRecording();
    }

    public ScenarioRecording Complete()
    {
        ThrowIfUnavailable();
        EnsureNoPendingAcceptedIntent();
        _completed = true;
        Unsubscribe();
        return BuildRecording();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Unsubscribe();
            _disposed = true;
        }
    }


    private ScenarioRecording BuildRecording()
        => new(
            _session.Scenario.ScenarioId,
            _session.InitialCondition.Reference,
            _frames,
            _session.OperatorActions.Actions,
            _events,
            _checkpoints,
            _session.AutomationIntents.Intents);

    private void EnsureNoPendingAcceptedIntent()
    {
        var finalStep = _frames[^1].LogicalStep;
        if (_session.OperatorActions.Actions.Any(action => checked(action.LogicalStep + 1) > finalStep)
            || _session.AutomationIntents.Intents.Any(intent => checked(intent.LogicalStep + 1) > finalStep))
        {
            throw new InvalidOperationException("Cannot capture a recording while an accepted operator action or automation intent has not yet reached its application step.");
        }
    }

    private void Subscribe()
    {
        _session.Coordinator.DeterministicStepCompleted += OnDeterministicStepCompleted;
        _session.OperatorActions.ActionAccepted += OnOperatorActionAccepted;
    }

    private void OnOperatorActionAccepted(object? sender, ScenarioOperatorActionAcceptedEventArgs args)
    {
        AddEvent(
            args.Action.LogicalStep,
            ScenarioRecordingEventKind.OperatorAction,
            args.Action.Command.TargetId ?? args.Action.Command.Kind.ToString(),
            args.Action.Command.Kind.ToString(),
            args.Action.Command);
    }

    private void OnDeterministicStepCompleted(object? sender, ControlRoomSnapshotChangedEventArgs args)
    {
        var snapshot = args.Snapshot;
        var firstEvent = _nextEventSequence;

        foreach (var alarmEvent in snapshot.AlarmEvents.Events.OrderBy(static item => item.Sequence))
        {
            AddEvent(
                snapshot.LogicalStep,
                ScenarioRecordingEventKind.Alarm,
                alarmEvent.AlarmId,
                $"{alarmEvent.Kind}:{alarmEvent.AlarmTitle}");
        }

        var previousFaults = _previousSnapshot.Faults.Faults.ToDictionary(static fault => fault.FaultId, StringComparer.Ordinal);
        foreach (var fault in snapshot.Faults.Faults
                     .Where(fault => !previousFaults.TryGetValue(fault.FaultId, out var previous)
                         || previous.LastTransitionSequence != fault.LastTransitionSequence)
                     .OrderBy(static fault => fault.LastTransitionSequence)
                     .ThenBy(static fault => fault.FaultId, StringComparer.Ordinal))
        {
            AddEvent(
                snapshot.LogicalStep,
                ScenarioRecordingEventKind.FaultTransition,
                fault.FaultId,
                fault.Lifecycle.ToString());
        }

        AddProtectionTransition(
            snapshot.LogicalStep,
            "reactor-scram",
            _previousSnapshot.ReactorScramActive,
            snapshot.ReactorScramActive);
        AddProtectionTransition(
            snapshot.LogicalStep,
            "turbine-trip",
            _previousSnapshot.TurbineTripActive,
            snapshot.TurbineTripActive);
        AddProtectionTransition(
            snapshot.LogicalStep,
            "generator-trip",
            _previousSnapshot.GeneratorTripActive,
            snapshot.GeneratorTripActive);

        var lastEvent = _nextEventSequence - 1;
        AddFrame(snapshot, firstEvent <= lastEvent ? firstEvent : 0, firstEvent <= lastEvent ? lastEvent : 0);
        _previousSnapshot = snapshot;
    }

    private void AddProtectionTransition(long logicalStep, string sourceId, bool previous, bool current)
    {
        if (previous != current)
        {
            AddEvent(
                logicalStep,
                ScenarioRecordingEventKind.ProtectionTransition,
                sourceId,
                current ? "Active" : "Cleared");
        }
    }

    private void AddEvent(
        long logicalStep,
        ScenarioRecordingEventKind kind,
        string sourceId,
        string detail,
        ControlRoomCommand? command = null)
        => _events.Add(new ScenarioRecordingEvent(
            _nextEventSequence++,
            logicalStep,
            kind,
            sourceId,
            detail,
            command));

    private void AddFrame(ControlRoomSnapshot snapshot, long firstEventSequence, long lastEventSequence)
        => _frames.Add(new ScenarioRecordingFrame(
            snapshot.LogicalStep,
            snapshot,
            ControlRoomSnapshotFingerprint.Compute(snapshot),
            firstEventSequence,
            lastEventSequence));

    private void ThrowIfUnavailable()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ScenarioRecorder));
        }
        if (_completed)
        {
            throw new InvalidOperationException("The scenario recorder has already been completed.");
        }
    }

    private void Unsubscribe()
    {
        _session.Coordinator.DeterministicStepCompleted -= OnDeterministicStepCompleted;
        _session.OperatorActions.ActionAccepted -= OnOperatorActionAccepted;
    }
}
