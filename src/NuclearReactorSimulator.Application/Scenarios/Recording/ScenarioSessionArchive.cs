using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.Scenarios.Recording;

/// <summary>
/// Versioned M10.7 replay-backed session archive. It persists scenario identity plus deterministic replay evidence; it is
/// deliberately not an opaque physical-state dump. Restoration is owned by <see cref="ScenarioFullReplayRunner"/>.
/// </summary>
public sealed class ScenarioSessionArchive
{
    public const int CurrentSchemaVersion = 1;

    public ScenarioSessionArchive(
        string archiveId,
        int schemaVersion,
        ScenarioDefinition scenario,
        IEnumerable<ScenarioSessionArchiveFrame> frames,
        IEnumerable<ScenarioOperatorActionRecord> operatorActions,
        IEnumerable<ScenarioAutomationIntentRecord> automationIntents,
        IEnumerable<ScenarioRecordingEvent> events,
        IEnumerable<ScenarioCheckpoint> checkpoints)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archiveId);
        if (schemaVersion != CurrentSchemaVersion)
        {
            throw new NotSupportedException($"Session-archive schema version {schemaVersion} is not supported. Current version is {CurrentSchemaVersion}.");
        }
        Scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));

        var frameArray = (frames ?? throw new ArgumentNullException(nameof(frames))).ToArray();
        if (frameArray.Length == 0)
        {
            throw new ArgumentException("A session archive must contain at least its initial deterministic frame evidence.", nameof(frames));
        }
        for (var index = 1; index < frameArray.Length; index++)
        {
            if (frameArray[index].LogicalStep != checked(frameArray[index - 1].LogicalStep + 1))
            {
                throw new ArgumentException("Session-archive frame evidence must cover contiguous logical steps.", nameof(frames));
            }
        }

        var actionArray = (operatorActions ?? throw new ArgumentNullException(nameof(operatorActions))).OrderBy(static item => item.Sequence).ToArray();
        ValidateContiguousSequence(actionArray.Select(static item => item.Sequence), nameof(operatorActions));
        var automationArray = (automationIntents ?? throw new ArgumentNullException(nameof(automationIntents))).OrderBy(static item => item.Sequence).ToArray();
        ValidateContiguousSequence(automationArray.Select(static item => item.Sequence), nameof(automationIntents));
        var eventArray = (events ?? throw new ArgumentNullException(nameof(events))).OrderBy(static item => item.Sequence).ToArray();
        ValidateContiguousSequence(eventArray.Select(static item => item.Sequence), nameof(events));
        var checkpointArray = (checkpoints ?? throw new ArgumentNullException(nameof(checkpoints)))
            .OrderBy(static item => item.LogicalStep)
            .ThenBy(static item => item.CheckpointId, StringComparer.Ordinal)
            .ToArray();

        foreach (var checkpoint in checkpointArray)
        {
            if (!string.Equals(checkpoint.ScenarioId, scenario.ScenarioId, StringComparison.Ordinal)
                || checkpoint.InitialCondition != scenario.InitialCondition)
            {
                throw new ArgumentException("Session-archive checkpoint identity must match the embedded scenario identity.", nameof(checkpoints));
            }
            var frame = frameArray.SingleOrDefault(item => item.LogicalStep == checkpoint.LogicalStep)
                ?? throw new ArgumentException($"Checkpoint '{checkpoint.CheckpointId}' does not reference archived frame evidence.", nameof(checkpoints));
            if (!string.Equals(frame.SnapshotFingerprint, checkpoint.SnapshotFingerprint, StringComparison.Ordinal))
            {
                throw new ArgumentException($"Checkpoint '{checkpoint.CheckpointId}' fingerprint does not match archived frame evidence.", nameof(checkpoints));
            }
        }

        ArchiveId = archiveId.Trim();
        SchemaVersion = schemaVersion;
        Frames = new ReadOnlyCollection<ScenarioSessionArchiveFrame>(frameArray);
        OperatorActions = new ReadOnlyCollection<ScenarioOperatorActionRecord>(actionArray);
        AutomationIntents = new ReadOnlyCollection<ScenarioAutomationIntentRecord>(automationArray);
        Events = new ReadOnlyCollection<ScenarioRecordingEvent>(eventArray);
        Checkpoints = new ReadOnlyCollection<ScenarioCheckpoint>(checkpointArray);
    }

    public string ArchiveId { get; }
    public int SchemaVersion { get; }
    public ScenarioDefinition Scenario { get; }
    public IReadOnlyList<ScenarioSessionArchiveFrame> Frames { get; }
    public IReadOnlyList<ScenarioOperatorActionRecord> OperatorActions { get; }
    public IReadOnlyList<ScenarioAutomationIntentRecord> AutomationIntents { get; }
    public IReadOnlyList<ScenarioRecordingEvent> Events { get; }
    public IReadOnlyList<ScenarioCheckpoint> Checkpoints { get; }
    public long InitialLogicalStep => Frames[0].LogicalStep;
    public long FinalLogicalStep => Frames[^1].LogicalStep;

    public static ScenarioSessionArchive FromRecording(string archiveId, ScenarioDefinition scenario, ScenarioRecording recording)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(recording);
        if (!string.Equals(scenario.ScenarioId, recording.ScenarioId, StringComparison.Ordinal)
            || scenario.InitialCondition != recording.InitialCondition)
        {
            throw new InvalidOperationException("Scenario identity/version does not match the recording used to create the session archive.");
        }

        return new ScenarioSessionArchive(
            archiveId,
            CurrentSchemaVersion,
            scenario,
            recording.Frames.Select(static frame => new ScenarioSessionArchiveFrame(
                frame.LogicalStep,
                frame.SnapshotFingerprint,
                frame.FirstEventSequence,
                frame.LastEventSequence)),
            recording.OperatorActions,
            recording.AutomationIntents,
            recording.Events,
            recording.Checkpoints);
    }

    public ScenarioSessionArchive ThroughLogicalStep(long logicalStep)
    {
        if (logicalStep < InitialLogicalStep || logicalStep > FinalLogicalStep)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalStep));
        }

        var frames = Frames.Where(frame => frame.LogicalStep <= logicalStep).ToArray();
        var actions = OperatorActions.Where(action => checked(action.LogicalStep + 1) <= logicalStep).ToArray();
        var automationIntents = AutomationIntents.Where(intent => checked(intent.LogicalStep + 1) <= logicalStep).ToArray();

        // Operator-action recorder events are emitted when an action is accepted between committed frames, so they are not
        // necessarily covered by a frame's First/LastEventSequence range. Rebuild the exact event prefix from semantic
        // ownership: include one operator-action event per applied action plus all step-generated evidence through the cutoff.
        var operatorActionEventSequences = Events
            .Where(static item => item.Kind == ScenarioRecordingEventKind.OperatorAction)
            .Take(actions.Length)
            .Select(static item => item.Sequence)
            .ToHashSet();
        if (operatorActionEventSequences.Count != actions.Length)
        {
            throw new InvalidOperationException("Session archive operator-action event stream is inconsistent with its applied action prefix.");
        }
        var events = Events
            .Where(item => item.Kind == ScenarioRecordingEventKind.OperatorAction
                ? operatorActionEventSequences.Contains(item.Sequence)
                : item.LogicalStep <= logicalStep)
            .ToArray();

        return new ScenarioSessionArchive(
            ArchiveId,
            SchemaVersion,
            Scenario,
            frames,
            actions,
            automationIntents,
            events,
            Checkpoints.Where(checkpoint => checkpoint.LogicalStep <= logicalStep));
    }

    private static void ValidateContiguousSequence(IEnumerable<long> sequence, string parameterName)
    {
        var values = sequence.ToArray();
        for (var index = 0; index < values.Length; index++)
        {
            if (values[index] != index + 1L)
            {
                throw new ArgumentException("Session-archive deterministic sequences must be contiguous and start at one.", parameterName);
            }
        }
    }
}
