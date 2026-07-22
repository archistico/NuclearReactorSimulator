using System.Collections.ObjectModel;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Simulation.Runtime.Replay;

namespace NuclearReactorSimulator.Application.Scenarios.Recording;

/// <summary>Immutable complete M9.1 deterministic scenario recording.</summary>
public sealed class ScenarioRecording
{
    public ScenarioRecording(
        string scenarioId,
        InitialConditionReference initialCondition,
        IEnumerable<ScenarioRecordingFrame> frames,
        IEnumerable<ScenarioOperatorActionRecord> operatorActions,
        IEnumerable<ScenarioRecordingEvent> events,
        IEnumerable<ScenarioCheckpoint>? checkpoints = null,
        IEnumerable<ScenarioAutomationIntentRecord>? automationIntents = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        InitialCondition = initialCondition ?? throw new ArgumentNullException(nameof(initialCondition));

        var frameArray = (frames ?? throw new ArgumentNullException(nameof(frames))).ToArray();
        if (frameArray.Length == 0)
        {
            throw new ArgumentException("A scenario recording must contain at least its initial frame.", nameof(frames));
        }
        for (var index = 1; index < frameArray.Length; index++)
        {
            if (frameArray[index].LogicalStep != checked(frameArray[index - 1].LogicalStep + 1))
            {
                throw new ArgumentException("Scenario recording frames must cover contiguous logical steps.", nameof(frames));
            }
        }

        var actionArray = (operatorActions ?? throw new ArgumentNullException(nameof(operatorActions)))
            .OrderBy(static action => action.Sequence)
            .ToArray();
        for (var index = 0; index < actionArray.Length; index++)
        {
            if (actionArray[index].Sequence != index + 1L)
            {
                throw new ArgumentException("Recorded operator-action sequences must be contiguous and start at one.", nameof(operatorActions));
            }
            if (checked(actionArray[index].LogicalStep + 1) > frameArray[^1].LogicalStep)
            {
                throw new ArgumentException("A completed recording cannot contain an operator action that has not reached its application step.", nameof(operatorActions));
            }
        }


        var automationIntentArray = (automationIntents ?? Array.Empty<ScenarioAutomationIntentRecord>())
            .OrderBy(static intent => intent.Sequence)
            .ToArray();
        for (var index = 0; index < automationIntentArray.Length; index++)
        {
            if (automationIntentArray[index].Sequence != index + 1L)
            {
                throw new ArgumentException("Recorded automation-intent sequences must be contiguous and start at one.", nameof(automationIntents));
            }
            if (checked(automationIntentArray[index].LogicalStep + 1) > frameArray[^1].LogicalStep)
            {
                throw new ArgumentException("A completed recording cannot contain an automation intent that has not reached its application step.", nameof(automationIntents));
            }
        }

        var eventArray = (events ?? throw new ArgumentNullException(nameof(events)))
            .OrderBy(static item => item.Sequence)
            .ToArray();
        for (var index = 0; index < eventArray.Length; index++)
        {
            if (eventArray[index].Sequence != index + 1L)
            {
                throw new ArgumentException("Recorder event sequences must be contiguous and start at one.", nameof(events));
            }
        }

        var checkpointArray = (checkpoints ?? Array.Empty<ScenarioCheckpoint>())
            .OrderBy(static checkpoint => checkpoint.LogicalStep)
            .ThenBy(static checkpoint => checkpoint.CheckpointId, StringComparer.Ordinal)
            .ToArray();
        foreach (var checkpoint in checkpointArray)
        {
            if (!string.Equals(checkpoint.ScenarioId, scenarioId, StringComparison.Ordinal)
                || checkpoint.InitialCondition != initialCondition)
            {
                throw new ArgumentException("Checkpoint identity must match the recording identity.", nameof(checkpoints));
            }
            var frame = frameArray.SingleOrDefault(item => item.LogicalStep == checkpoint.LogicalStep)
                ?? throw new ArgumentException($"Checkpoint '{checkpoint.CheckpointId}' does not reference a recorded frame.", nameof(checkpoints));
            if (!string.Equals(frame.SnapshotFingerprint, checkpoint.SnapshotFingerprint, StringComparison.Ordinal))
            {
                throw new ArgumentException($"Checkpoint '{checkpoint.CheckpointId}' fingerprint does not match its recorded frame.", nameof(checkpoints));
            }
        }

        ScenarioId = scenarioId.Trim();
        Frames = new ReadOnlyCollection<ScenarioRecordingFrame>(frameArray);
        OperatorActions = new ReadOnlyCollection<ScenarioOperatorActionRecord>(actionArray);
        AutomationIntents = new ReadOnlyCollection<ScenarioAutomationIntentRecord>(automationIntentArray);
        Events = new ReadOnlyCollection<ScenarioRecordingEvent>(eventArray);
        Checkpoints = new ReadOnlyCollection<ScenarioCheckpoint>(checkpointArray);
    }

    public string ScenarioId { get; }
    public InitialConditionReference InitialCondition { get; }
    public IReadOnlyList<ScenarioRecordingFrame> Frames { get; }
    public IReadOnlyList<ScenarioOperatorActionRecord> OperatorActions { get; }
    public IReadOnlyList<ScenarioAutomationIntentRecord> AutomationIntents { get; }
    public IReadOnlyList<ScenarioRecordingEvent> Events { get; }
    public IReadOnlyList<ScenarioCheckpoint> Checkpoints { get; }
    public long InitialLogicalStep => Frames[0].LogicalStep;
    public long FinalLogicalStep => Frames[^1].LogicalStep;

    public SimulationCommandTrace<ControlRoomCommand> CreateOperatorCommandTrace(long? throughLogicalStep = null)
    {
        var finalStep = throughLogicalStep ?? FinalLogicalStep;
        if (finalStep < InitialLogicalStep || finalStep > FinalLogicalStep)
        {
            throw new ArgumentOutOfRangeException(nameof(throughLogicalStep));
        }

        return new SimulationCommandTrace<ControlRoomCommand>(OperatorActions
            .Where(action => checked(action.LogicalStep + 1) <= finalStep)
            .OrderBy(static action => action.Sequence)
            .Select(static action => new SimulationCommandTraceEntry<ControlRoomCommand>(
                checked(action.LogicalStep + 1),
                action.Command)));
    }
}
