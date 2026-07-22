using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Recording;

/// <summary>
/// M9.1 full deterministic replay verifier. It reconstructs from the exact versioned initial condition, accepted operator-action
/// trace and any M10.5/M10.6 semantic automation-intent trace, verifies every recorded logical-step fingerprint, then verifies
/// the richer event stream fail-closed.
/// </summary>
public sealed class ScenarioFullReplayRunner
{
    private readonly ScenarioSessionFactory _sessionFactory;

    public ScenarioFullReplayRunner(ScenarioSessionFactory sessionFactory)
    {
        _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
    }

    public ScenarioFullReplayResult ReplayAndVerify(ScenarioDefinition scenario, ScenarioRecording recording)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(recording);
        ValidateIdentity(scenario, recording);

        var session = _sessionFactory.Load(scenario);
        if (session.InitialCondition.Reference != recording.InitialCondition)
        {
            throw new InvalidOperationException("Loaded initial-condition identity does not match the recording.");
        }
        if (session.Coordinator.Current.LogicalStep != recording.InitialLogicalStep)
        {
            throw new InvalidOperationException("Loaded initial logical step does not match the recording origin.");
        }

        using var recorder = new ScenarioRecorder(session);
        VerifyFrame(recording.Frames[0], recorder.LatestFrame);

        var actions = recording.OperatorActions.OrderBy(static action => action.Sequence).ToArray();
        var automationIntents = recording.AutomationIntents.OrderBy(static intent => intent.Sequence).ToArray();
        var actionIndex = 0;
        var automationIntentIndex = 0;
        for (var frameIndex = 1; frameIndex < recording.Frames.Count; frameIndex++)
        {
            var expected = recording.Frames[frameIndex];
            var nextStep = checked(session.Coordinator.Current.LogicalStep + 1);
            if (expected.LogicalStep != nextStep)
            {
                throw new InvalidOperationException("Recording frame sequence is not contiguous with replay state.");
            }

            while (automationIntentIndex < automationIntents.Length && checked(automationIntents[automationIntentIndex].LogicalStep + 1) == nextStep)
            {
                ApplyAutomationIntent(session, automationIntents[automationIntentIndex]);
                automationIntentIndex++;
            }
            while (actionIndex < actions.Length && checked(actions[actionIndex].LogicalStep + 1) == nextStep)
            {
                session.CommandDispatcher.Dispatch(actions[actionIndex].Command);
                actionIndex++;
            }

            session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
            VerifyFrame(expected, recorder.LatestFrame);
        }

        if (actionIndex != actions.Length || automationIntentIndex != automationIntents.Length)
        {
            throw new InvalidOperationException("Recording contains operator actions or automation intents beyond its final replay step.");
        }

        var replayed = recorder.Complete();
        VerifyAutomationIntents(recording.AutomationIntents, replayed.AutomationIntents);
        VerifyEvents(recording.Events, replayed.Events);
        return new ScenarioFullReplayResult(session, replayed, recording.Frames.Count, recording.Events.Count);
    }


    /// <summary>
    /// Reconstructs a compact M10.7 replay-backed archive through the same exact-version M9.1 replay authority and regenerates
    /// the full immutable recording only after every archived fingerprint/event/checkpoint has been verified fail-closed.
    /// </summary>
    public ScenarioFullReplayResult ReplayAndVerify(ScenarioSessionArchive archive)
        => ReplayAndVerify(archive, sessionInitialized: null);

    public ScenarioFullReplayResult ReplayAndVerify(ScenarioSessionArchive archive, Action<ScenarioSession>? sessionInitialized)
    {
        ArgumentNullException.ThrowIfNull(archive);
        var scenario = archive.Scenario;
        var session = _sessionFactory.Load(scenario);
        sessionInitialized?.Invoke(session);
        if (session.InitialCondition.Reference != scenario.InitialCondition
            || session.Coordinator.Current.LogicalStep != archive.InitialLogicalStep)
        {
            throw new InvalidOperationException("Loaded archive origin does not match the exact scenario/initial-condition identity.");
        }

        using var recorder = new ScenarioRecorder(session);
        VerifyArchiveFrame(archive.Frames[0], recorder.LatestFrame);
        var actions = archive.OperatorActions.OrderBy(static item => item.Sequence).ToArray();
        var automationIntents = archive.AutomationIntents.OrderBy(static item => item.Sequence).ToArray();
        var checkpointsByStep = archive.Checkpoints.GroupBy(static item => item.LogicalStep)
            .ToDictionary(static group => group.Key, static group => group.OrderBy(item => item.CheckpointId, StringComparer.Ordinal).ToArray());
        CreateAndVerifyArchivedCheckpoints(recorder, checkpointsByStep, archive.InitialLogicalStep);

        var actionIndex = 0;
        var automationIntentIndex = 0;
        for (var frameIndex = 1; frameIndex < archive.Frames.Count; frameIndex++)
        {
            var expected = archive.Frames[frameIndex];
            var nextStep = checked(session.Coordinator.Current.LogicalStep + 1);
            if (expected.LogicalStep != nextStep)
            {
                throw new InvalidOperationException("Archived frame evidence is not contiguous with replay state.");
            }

            while (automationIntentIndex < automationIntents.Length && checked(automationIntents[automationIntentIndex].LogicalStep + 1) == nextStep)
            {
                ApplyAutomationIntent(session, automationIntents[automationIntentIndex]);
                automationIntentIndex++;
            }
            while (actionIndex < actions.Length && checked(actions[actionIndex].LogicalStep + 1) == nextStep)
            {
                session.CommandDispatcher.Dispatch(actions[actionIndex].Command);
                actionIndex++;
            }

            session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
            VerifyArchiveFrame(expected, recorder.LatestFrame);
            CreateAndVerifyArchivedCheckpoints(recorder, checkpointsByStep, expected.LogicalStep);
        }

        if (actionIndex != actions.Length || automationIntentIndex != automationIntents.Length)
        {
            throw new InvalidOperationException("Archive contains replay inputs beyond its final deterministic frame.");
        }

        var replayed = recorder.Complete();
        VerifyAutomationIntents(archive.AutomationIntents, replayed.AutomationIntents);
        VerifyEvents(archive.Events, replayed.Events);
        if (!archive.Checkpoints.SequenceEqual(replayed.Checkpoints))
        {
            throw new InvalidOperationException("Scenario archive checkpoint stream diverged during replay reconstruction.");
        }
        return new ScenarioFullReplayResult(session, replayed, archive.Frames.Count, archive.Events.Count);
    }

    /// <summary>Restores and verifies the exact prefix ending at one archived replay-backed checkpoint.</summary>
    public ScenarioFullReplayResult SeekAndVerify(ScenarioSessionArchive archive, string checkpointId)
        => SeekAndVerify(archive, checkpointId, sessionInitialized: null);

    public ScenarioFullReplayResult SeekAndVerify(
        ScenarioSessionArchive archive,
        string checkpointId,
        Action<ScenarioSession>? sessionInitialized)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentException.ThrowIfNullOrWhiteSpace(checkpointId);
        var checkpoint = archive.Checkpoints.SingleOrDefault(item => string.Equals(item.CheckpointId, checkpointId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Checkpoint '{checkpointId}' is not present in session archive '{archive.ArchiveId}'.");
        var prefix = archive.ThroughLogicalStep(checkpoint.LogicalStep);
        var result = ReplayAndVerify(prefix, sessionInitialized);
        var restoredFingerprint = ControlRoomSnapshotFingerprint.Compute(result.Session.Coordinator.Current);
        if (!string.Equals(restoredFingerprint, checkpoint.SnapshotFingerprint, StringComparison.Ordinal))
        {
            throw new ScenarioReplayDivergenceException(checkpoint.LogicalStep, checkpoint.SnapshotFingerprint, restoredFingerprint);
        }
        return result;
    }

    public ScenarioSession SeekAndVerify(
        ScenarioDefinition scenario,
        ScenarioRecording recording,
        ScenarioCheckpoint checkpoint)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(recording);
        ArgumentNullException.ThrowIfNull(checkpoint);
        ValidateIdentity(scenario, recording);
        ValidateCheckpoint(recording, checkpoint);

        var session = _sessionFactory.Load(scenario);
        if (session.InitialCondition.Reference != recording.InitialCondition
            || session.Coordinator.Current.LogicalStep != recording.InitialLogicalStep)
        {
            throw new InvalidOperationException("Loaded replay origin does not match the recording identity/logical step.");
        }

        var actions = recording.OperatorActions
            .Where(action => action.Sequence <= checkpoint.LastAppliedOperatorActionSequence)
            .OrderBy(static action => action.Sequence)
            .ToArray();
        var automationIntents = recording.AutomationIntents
            .Where(intent => checked(intent.LogicalStep + 1) <= checkpoint.LogicalStep)
            .OrderBy(static intent => intent.Sequence)
            .ToArray();
        var actionIndex = 0;
        var automationIntentIndex = 0;
        while (session.Coordinator.Current.LogicalStep < checkpoint.LogicalStep)
        {
            var nextStep = checked(session.Coordinator.Current.LogicalStep + 1);
            while (automationIntentIndex < automationIntents.Length && checked(automationIntents[automationIntentIndex].LogicalStep + 1) == nextStep)
            {
                ApplyAutomationIntent(session, automationIntents[automationIntentIndex]);
                automationIntentIndex++;
            }
            while (actionIndex < actions.Length && checked(actions[actionIndex].LogicalStep + 1) == nextStep)
            {
                session.CommandDispatcher.Dispatch(actions[actionIndex].Command);
                actionIndex++;
            }
            session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        }

        if (actionIndex != actions.Length || automationIntentIndex != automationIntents.Length)
        {
            throw new InvalidOperationException("Checkpoint command/automation-intent prefix is inconsistent with its logical step.");
        }

        var actual = ControlRoomSnapshotFingerprint.Compute(session.Coordinator.Current);
        if (!string.Equals(actual, checkpoint.SnapshotFingerprint, StringComparison.Ordinal))
        {
            throw new ScenarioReplayDivergenceException(checkpoint.LogicalStep, checkpoint.SnapshotFingerprint, actual);
        }

        return session;
    }


    private static void VerifyArchiveFrame(ScenarioSessionArchiveFrame expected, ScenarioRecordingFrame actual)
    {
        if (expected.LogicalStep != actual.LogicalStep)
        {
            throw new InvalidOperationException("Replay frame logical step does not match archived frame evidence.");
        }
        if (!string.Equals(expected.SnapshotFingerprint, actual.SnapshotFingerprint, StringComparison.Ordinal))
        {
            throw new ScenarioReplayDivergenceException(expected.LogicalStep, expected.SnapshotFingerprint, actual.SnapshotFingerprint);
        }
        if (expected.FirstEventSequence != actual.FirstEventSequence || expected.LastEventSequence != actual.LastEventSequence)
        {
            throw new InvalidOperationException($"Replay frame event-sequence range diverged at logical step {expected.LogicalStep}.");
        }
    }

    private static void CreateAndVerifyArchivedCheckpoints(
        ScenarioRecorder recorder,
        IReadOnlyDictionary<long, ScenarioCheckpoint[]> checkpointsByStep,
        long logicalStep)
    {
        if (!checkpointsByStep.TryGetValue(logicalStep, out var checkpoints))
        {
            return;
        }
        foreach (var expected in checkpoints)
        {
            var actual = recorder.CreateCheckpoint(expected.CheckpointId);
            if (actual != expected)
            {
                throw new InvalidOperationException($"Replay-backed checkpoint '{expected.CheckpointId}' diverged during archive reconstruction.");
            }
        }
    }

    private static void ApplyAutomationIntent(ScenarioSession session, ScenarioAutomationIntentRecord intent)
    {
        switch (intent.Kind)
        {
            case ScenarioAutomationIntentKind.PlantControlAuthority:
                session.PlantControlAuthority.RequestAuthority(
                    intent.Authority ?? throw new InvalidOperationException("Recorded authority intent is missing its authority payload."));
                break;
            case ScenarioAutomationIntentKind.SupervisoryObjective:
                session.PlantControlAuthority.RequestSupervisoryObjective(
                    intent.Objective ?? throw new InvalidOperationException("Recorded supervisory-objective intent is missing its objective payload."));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(intent), intent.Kind, "Unsupported recorded automation intent kind.");
        }
    }

    private static void VerifyAutomationIntents(
        IReadOnlyList<ScenarioAutomationIntentRecord> expected,
        IReadOnlyList<ScenarioAutomationIntentRecord> actual)
    {
        if (expected.Count != actual.Count)
        {
            throw new InvalidOperationException($"Scenario replay automation-intent count diverged: expected {expected.Count}, actual {actual.Count}.");
        }
        for (var index = 0; index < expected.Count; index++)
        {
            if (expected[index] != actual[index])
            {
                throw new InvalidOperationException($"Scenario replay automation-intent stream diverged at sequence {index + 1}.");
            }
        }
    }

    private static void ValidateIdentity(ScenarioDefinition scenario, ScenarioRecording recording)
    {
        if (!string.Equals(scenario.ScenarioId, recording.ScenarioId, StringComparison.Ordinal)
            || scenario.InitialCondition != recording.InitialCondition)
        {
            throw new InvalidOperationException("Scenario identity/version does not match the recording.");
        }
    }

    private static void ValidateCheckpoint(ScenarioRecording recording, ScenarioCheckpoint checkpoint)
    {
        if (checkpoint.SchemaVersion != ScenarioCheckpoint.CurrentSchemaVersion)
        {
            throw new NotSupportedException($"Checkpoint schema version {checkpoint.SchemaVersion} is not supported.");
        }
        if (!string.Equals(checkpoint.FingerprintAlgorithmId, ControlRoomSnapshotFingerprint.AlgorithmId, StringComparison.Ordinal))
        {
            throw new NotSupportedException($"Checkpoint fingerprint algorithm '{checkpoint.FingerprintAlgorithmId}' is not supported.");
        }
        if (!string.Equals(checkpoint.ScenarioId, recording.ScenarioId, StringComparison.Ordinal)
            || checkpoint.InitialCondition != recording.InitialCondition)
        {
            throw new InvalidOperationException("Checkpoint identity does not match the recording.");
        }
        var recordedFrame = recording.Frames.SingleOrDefault(frame => frame.LogicalStep == checkpoint.LogicalStep)
            ?? throw new InvalidOperationException("Checkpoint logical step is not present in the recording.");
        if (!string.Equals(recordedFrame.SnapshotFingerprint, checkpoint.SnapshotFingerprint, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Checkpoint fingerprint does not match its recording frame.");
        }
        var expectedLastSequence = recording.OperatorActions
            .Where(action => checked(action.LogicalStep + 1) <= checkpoint.LogicalStep)
            .Select(static action => action.Sequence)
            .DefaultIfEmpty(0L)
            .Max();
        if (checkpoint.LastAppliedOperatorActionSequence != expectedLastSequence)
        {
            throw new InvalidOperationException("Checkpoint operator-action prefix does not match the recording.");
        }
    }

    private static void VerifyFrame(ScenarioRecordingFrame expected, ScenarioRecordingFrame actual)
    {
        if (expected.LogicalStep != actual.LogicalStep)
        {
            throw new InvalidOperationException("Replay frame logical step does not match the recording.");
        }
        if (!string.Equals(expected.SnapshotFingerprint, actual.SnapshotFingerprint, StringComparison.Ordinal))
        {
            throw new ScenarioReplayDivergenceException(
                expected.LogicalStep,
                expected.SnapshotFingerprint,
                actual.SnapshotFingerprint);
        }
    }

    private static void VerifyEvents(
        IReadOnlyList<ScenarioRecordingEvent> expected,
        IReadOnlyList<ScenarioRecordingEvent> actual)
    {
        if (expected.Count != actual.Count)
        {
            throw new InvalidOperationException($"Scenario replay event count diverged: expected {expected.Count}, actual {actual.Count}.");
        }
        for (var index = 0; index < expected.Count; index++)
        {
            if (expected[index] != actual[index])
            {
                throw new InvalidOperationException($"Scenario replay event stream diverged at recorder event sequence {index + 1}.");
            }
        }
    }
}
