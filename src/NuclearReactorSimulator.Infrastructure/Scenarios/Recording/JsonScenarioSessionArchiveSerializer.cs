using System.Text.Json;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using NuclearReactorSimulator.Infrastructure.Scenarios;

namespace NuclearReactorSimulator.Infrastructure.Scenarios.Recording;

/// <summary>JSON persistence adapter for the compact replay-backed M10.7 session archive schema v1.</summary>
public sealed class JsonScenarioSessionArchiveSerializer : IScenarioSessionArchiveSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly IScenarioDefinitionSerializer _scenarioSerializer;

    public JsonScenarioSessionArchiveSerializer(IScenarioDefinitionSerializer? scenarioSerializer = null)
    {
        _scenarioSerializer = scenarioSerializer ?? new JsonScenarioDefinitionSerializer();
    }

    public string Serialize(ScenarioSessionArchive archive)
    {
        ArgumentNullException.ThrowIfNull(archive);
        if (archive.SchemaVersion != ScenarioSessionArchive.CurrentSchemaVersion)
        {
            throw new NotSupportedException($"Session-archive schema version {archive.SchemaVersion} cannot be serialized by this adapter.");
        }

        var document = new ArchiveDocument
        {
            SchemaVersion = archive.SchemaVersion,
            ArchiveId = archive.ArchiveId,
            ScenarioJson = _scenarioSerializer.Serialize(archive.Scenario),
            Frames = archive.Frames.Select(static frame => new FrameDocument
            {
                LogicalStep = frame.LogicalStep,
                SnapshotFingerprint = frame.SnapshotFingerprint,
                FirstEventSequence = frame.FirstEventSequence,
                LastEventSequence = frame.LastEventSequence,
            }).ToArray(),
            OperatorActions = archive.OperatorActions.Select(static action => new OperatorActionDocument
            {
                Sequence = action.Sequence,
                LogicalStep = action.LogicalStep,
                Command = ToDocument(action.Command),
            }).ToArray(),
            AutomationIntents = archive.AutomationIntents.Select(static intent => new AutomationIntentDocument
            {
                Sequence = intent.Sequence,
                LogicalStep = intent.LogicalStep,
                Kind = intent.Kind,
                Authority = intent.Authority,
                Objective = intent.Objective is null ? null : new ObjectiveDocument
                {
                    Kind = intent.Objective.Kind,
                    CaptureCurrentOperatingPoint = intent.Objective.CaptureCurrentOperatingPoint,
                    ReactorPowerSetpointWatts = intent.Objective.ReactorPowerSetpointWatts,
                    TurbineSpeedSetpointRpm = intent.Objective.TurbineSpeedSetpointRpm,
                },
            }).ToArray(),
            Events = archive.Events.Select(static item => new EventDocument
            {
                Sequence = item.Sequence,
                LogicalStep = item.LogicalStep,
                Kind = item.Kind,
                SourceId = item.SourceId,
                Detail = item.Detail,
                OperatorCommand = item.OperatorCommand is null ? null : ToDocument(item.OperatorCommand),
            }).ToArray(),
            Checkpoints = archive.Checkpoints.Select(static checkpoint => new CheckpointDocument
            {
                CheckpointId = checkpoint.CheckpointId,
                SchemaVersion = checkpoint.SchemaVersion,
                ScenarioId = checkpoint.ScenarioId,
                InitialConditionId = checkpoint.InitialCondition.InitialConditionId,
                InitialConditionVersion = checkpoint.InitialCondition.Version,
                LogicalStep = checkpoint.LogicalStep,
                LastAppliedOperatorActionSequence = checkpoint.LastAppliedOperatorActionSequence,
                FingerprintAlgorithmId = checkpoint.FingerprintAlgorithmId,
                SnapshotFingerprint = checkpoint.SnapshotFingerprint,
            }).ToArray(),
        };

        return JsonSerializer.Serialize(document, Options);
    }

    public ScenarioSessionArchive Deserialize(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        var document = JsonSerializer.Deserialize<ArchiveDocument>(content, Options)
            ?? throw new InvalidDataException("Session archive document could not be deserialized.");
        if (document.SchemaVersion != ScenarioSessionArchive.CurrentSchemaVersion)
        {
            throw new NotSupportedException($"Session-archive schema version {document.SchemaVersion} is not supported. Current version is {ScenarioSessionArchive.CurrentSchemaVersion}.");
        }
        ValidateText(document.ArchiveId, "archiveId");
        ValidateText(document.ScenarioJson, "scenarioJson");
        var scenario = _scenarioSerializer.Deserialize(document.ScenarioJson!);

        var frames = (document.Frames ?? Array.Empty<FrameDocument>()).Select((frame, index) =>
        {
            if (frame is null)
            {
                throw new InvalidDataException($"Archive frame at index {index} cannot be null.");
            }
            ValidateText(frame.SnapshotFingerprint, $"frames[{index}].snapshotFingerprint");
            return new ScenarioSessionArchiveFrame(frame.LogicalStep, frame.SnapshotFingerprint!, frame.FirstEventSequence, frame.LastEventSequence);
        }).ToArray();

        var actions = (document.OperatorActions ?? Array.Empty<OperatorActionDocument>()).Select((action, index) =>
        {
            if (action?.Command is null)
            {
                throw new InvalidDataException($"Archive operator action at index {index} must contain a typed command.");
            }
            return new ScenarioOperatorActionRecord(action.Sequence, action.LogicalStep, FromDocument(action.Command));
        }).ToArray();

        var intents = (document.AutomationIntents ?? Array.Empty<AutomationIntentDocument>()).Select((intent, index) =>
        {
            if (intent is null)
            {
                throw new InvalidDataException($"Archive automation intent at index {index} cannot be null.");
            }
            return new ScenarioAutomationIntentRecord(
                intent.Sequence,
                intent.LogicalStep,
                intent.Kind,
                intent.Authority,
                intent.Objective is null ? null : FromDocument(intent.Objective));
        }).ToArray();

        var events = (document.Events ?? Array.Empty<EventDocument>()).Select((item, index) =>
        {
            if (item is null)
            {
                throw new InvalidDataException($"Archive event at index {index} cannot be null.");
            }
            ValidateText(item.SourceId, $"events[{index}].sourceId");
            ValidateText(item.Detail, $"events[{index}].detail");
            return new ScenarioRecordingEvent(
                item.Sequence,
                item.LogicalStep,
                item.Kind,
                item.SourceId!,
                item.Detail!,
                item.OperatorCommand is null ? null : FromDocument(item.OperatorCommand));
        }).ToArray();

        var checkpoints = (document.Checkpoints ?? Array.Empty<CheckpointDocument>()).Select((checkpoint, index) =>
        {
            if (checkpoint is null)
            {
                throw new InvalidDataException($"Archive checkpoint at index {index} cannot be null.");
            }
            ValidateText(checkpoint.CheckpointId, $"checkpoints[{index}].checkpointId");
            ValidateText(checkpoint.ScenarioId, $"checkpoints[{index}].scenarioId");
            ValidateText(checkpoint.InitialConditionId, $"checkpoints[{index}].initialConditionId");
            ValidateText(checkpoint.FingerprintAlgorithmId, $"checkpoints[{index}].fingerprintAlgorithmId");
            ValidateText(checkpoint.SnapshotFingerprint, $"checkpoints[{index}].snapshotFingerprint");
            return new ScenarioCheckpoint(
                checkpoint.CheckpointId!,
                checkpoint.SchemaVersion,
                checkpoint.ScenarioId!,
                new InitialConditionReference(checkpoint.InitialConditionId!, checkpoint.InitialConditionVersion),
                checkpoint.LogicalStep,
                checkpoint.LastAppliedOperatorActionSequence,
                checkpoint.FingerprintAlgorithmId!,
                checkpoint.SnapshotFingerprint!);
        }).ToArray();

        return new ScenarioSessionArchive(
            document.ArchiveId!,
            document.SchemaVersion,
            scenario,
            frames,
            actions,
            intents,
            events,
            checkpoints);
    }

    private static CommandDocument ToDocument(ControlRoomCommand command)
        => new() { Kind = command.Kind, TargetId = command.TargetId, TargetKind = command.TargetKind };

    private static ControlRoomCommand FromDocument(CommandDocument document)
        => new(document.Kind, document.TargetId, document.TargetKind);

    private static SupervisoryObjectiveRequest FromDocument(ObjectiveDocument document)
        => document.Kind switch
        {
            SupervisoryOperatingObjectiveKind.HoldOperatingPoint when document.CaptureCurrentOperatingPoint
                => SupervisoryObjectiveRequest.HoldCurrentOperatingPoint(),
            SupervisoryOperatingObjectiveKind.HoldReactorPower when document.ReactorPowerSetpointWatts.HasValue
                => SupervisoryObjectiveRequest.HoldReactorPower(document.ReactorPowerSetpointWatts.Value),
            SupervisoryOperatingObjectiveKind.HoldTurbineSpeed when document.TurbineSpeedSetpointRpm.HasValue
                => SupervisoryObjectiveRequest.HoldTurbineSpeed(document.TurbineSpeedSetpointRpm.Value),
            _ => throw new InvalidDataException("Session archive contains an unsupported or incomplete supervisory objective payload."),
        };

    private static void ValidateText(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"Session archive field '{fieldName}' is required.");
        }
    }

    private sealed class ArchiveDocument
    {
        public int SchemaVersion { get; set; }
        public string? ArchiveId { get; set; }
        public string? ScenarioJson { get; set; }
        public FrameDocument[]? Frames { get; set; }
        public OperatorActionDocument[]? OperatorActions { get; set; }
        public AutomationIntentDocument[]? AutomationIntents { get; set; }
        public EventDocument[]? Events { get; set; }
        public CheckpointDocument[]? Checkpoints { get; set; }
    }

    private sealed class FrameDocument
    {
        public long LogicalStep { get; set; }
        public string? SnapshotFingerprint { get; set; }
        public long FirstEventSequence { get; set; }
        public long LastEventSequence { get; set; }
    }

    private sealed class OperatorActionDocument
    {
        public long Sequence { get; set; }
        public long LogicalStep { get; set; }
        public CommandDocument? Command { get; set; }
    }

    private sealed class AutomationIntentDocument
    {
        public long Sequence { get; set; }
        public long LogicalStep { get; set; }
        public ScenarioAutomationIntentKind Kind { get; set; }
        public PlantControlAuthorityMode? Authority { get; set; }
        public ObjectiveDocument? Objective { get; set; }
    }

    private sealed class ObjectiveDocument
    {
        public SupervisoryOperatingObjectiveKind Kind { get; set; }
        public bool CaptureCurrentOperatingPoint { get; set; }
        public double? ReactorPowerSetpointWatts { get; set; }
        public double? TurbineSpeedSetpointRpm { get; set; }
    }

    private sealed class EventDocument
    {
        public long Sequence { get; set; }
        public long LogicalStep { get; set; }
        public ScenarioRecordingEventKind Kind { get; set; }
        public string? SourceId { get; set; }
        public string? Detail { get; set; }
        public CommandDocument? OperatorCommand { get; set; }
    }

    private sealed class CheckpointDocument
    {
        public string? CheckpointId { get; set; }
        public int SchemaVersion { get; set; }
        public string? ScenarioId { get; set; }
        public string? InitialConditionId { get; set; }
        public int InitialConditionVersion { get; set; }
        public long LogicalStep { get; set; }
        public long LastAppliedOperatorActionSequence { get; set; }
        public string? FingerprintAlgorithmId { get; set; }
        public string? SnapshotFingerprint { get; set; }
    }

    private sealed class CommandDocument
    {
        public ControlRoomCommandKind Kind { get; set; }
        public string? TargetId { get; set; }
        public ControlRoomCommandTargetKind? TargetKind { get; set; }
    }
}
