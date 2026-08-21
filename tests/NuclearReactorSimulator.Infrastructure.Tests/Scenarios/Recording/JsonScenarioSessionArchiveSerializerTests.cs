using System.Text.Json;
using System.Text.Json.Nodes;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;
using NuclearReactorSimulator.Infrastructure.Scenarios.Recording;
using Xunit;

namespace NuclearReactorSimulator.Infrastructure.Tests.Scenarios.Recording;

public sealed class JsonScenarioSessionArchiveSerializerTests
{
    [Fact]
    public void RoundTrip_PreservesReplayEvidenceAndExactScenarioIdentity()
    {
        var archive = CreateArchive(out var factory);
        var serializer = new JsonScenarioSessionArchiveSerializer();

        var json = serializer.Serialize(archive);
        Assert.DoesNotContain("\"numericValue\"", json, StringComparison.Ordinal);
        var restored = serializer.Deserialize(json);
        var replay = new ScenarioFullReplayRunner(factory).ReplayAndVerify(restored);

        Assert.Equal(archive.ArchiveId, restored.ArchiveId);
        Assert.Equal(archive.Scenario.ScenarioId, restored.Scenario.ScenarioId);
        Assert.Equal(archive.Scenario.InitialCondition, restored.Scenario.InitialCondition);
        Assert.Equal(archive.Frames.Select(static frame => frame.SnapshotFingerprint), restored.Frames.Select(static frame => frame.SnapshotFingerprint));
        Assert.Single(restored.Checkpoints);
        Assert.Equal(archive.FinalLogicalStep, replay.Session.Coordinator.Current.LogicalStep);
    }


    [Fact]
    public void ManualTurbineControlValveDemand_RoundTripsNumericPayloadInActionsAndEventsAndFullReplay()
    {
        var archive = CreateManualDemandArchive(out var factory);
        var serializer = new JsonScenarioSessionArchiveSerializer();

        var json = serializer.Serialize(archive);
        Assert.Contains("\"numericValue\": 37.5", json, StringComparison.Ordinal);
        var restored = serializer.Deserialize(json);
        var replay = new ScenarioFullReplayRunner(factory).ReplayAndVerify(restored);

        var action = Assert.Single(restored.OperatorActions, static item =>
            item.Command.Kind == ControlRoomCommandKind.TurbineControlValveManualDemandSet);
        Assert.True(action.Command.TargetKind.HasValue);
        Assert.Equal(ControlRoomCommandTargetKind.Valve, action.Command.TargetKind.GetValueOrDefault());
        Assert.True(action.Command.NumericValue.HasValue);
        Assert.Equal(37.5d, action.Command.NumericValue.GetValueOrDefault());

        var recorderEvent = Assert.Single(restored.Events, static item =>
            item.Kind == ScenarioRecordingEventKind.OperatorAction
            && item.OperatorCommand?.Kind == ControlRoomCommandKind.TurbineControlValveManualDemandSet);
        var recordedNumericValue = recorderEvent.OperatorCommand!.NumericValue;
        Assert.True(recordedNumericValue.HasValue);
        Assert.Equal(37.5d, recordedNumericValue.GetValueOrDefault());
        Assert.Equal(restored.FinalLogicalStep, replay.Session.Coordinator.Current.LogicalStep);
        Assert.Equal(
            restored.Frames[^1].SnapshotFingerprint,
            ControlRoomSnapshotFingerprint.Compute(replay.Session.Coordinator.Current));
    }

    [Fact]
    public void ManualDemandWithoutNumericValue_FailsAtArchiveBoundaryBeforeReplay()
    {
        var archive = CreateManualDemandArchive(out _);
        var serializer = new JsonScenarioSessionArchiveSerializer();
        var document = JsonNode.Parse(serializer.Serialize(archive))!.AsObject();
        var command = FindOperatorActionCommand(document, ControlRoomCommandKind.TurbineControlValveManualDemandSet);
        command.Remove("numericValue");

        var exception = Assert.Throws<InvalidDataException>(() => serializer.Deserialize(document.ToJsonString()));

        Assert.Contains("without numericValue", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UndefinedCommandKind_FailsAtArchiveBoundary()
    {
        var archive = CreateManualDemandArchive(out _);
        var serializer = new JsonScenarioSessionArchiveSerializer();
        var document = JsonNode.Parse(serializer.Serialize(archive))!.AsObject();
        var command = FindOperatorActionCommand(document, ControlRoomCommandKind.TurbineControlValveManualDemandSet);
        command["kind"] = 9999;

        var exception = Assert.Throws<InvalidDataException>(() => serializer.Deserialize(document.ToJsonString()));

        Assert.Contains(".kind", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UndefinedCommandTargetKind_FailsAtArchiveBoundary()
    {
        var archive = CreateManualDemandArchive(out _);
        var serializer = new JsonScenarioSessionArchiveSerializer();
        var document = JsonNode.Parse(serializer.Serialize(archive))!.AsObject();
        var command = FindOperatorActionCommand(document, ControlRoomCommandKind.TurbineControlValveManualDemandSet);
        command["targetKind"] = 9999;

        var exception = Assert.Throws<InvalidDataException>(() => serializer.Deserialize(document.ToJsonString()));

        Assert.Contains(".targetKind", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UndefinedRecordingEventKind_FailsAtArchiveBoundary()
    {
        var archive = CreateManualDemandArchive(out _);
        var serializer = new JsonScenarioSessionArchiveSerializer();
        var document = JsonNode.Parse(serializer.Serialize(archive))!.AsObject();
        document["events"]!.AsArray()[0]!.AsObject()["kind"] = 9999;

        var exception = Assert.Throws<InvalidDataException>(() => serializer.Deserialize(document.ToJsonString()));

        Assert.Contains("unsupported kind", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SchemaV1_NumericEnumOrdinalsAreFrozenAndCommandsRemainNumericOnDisk()
    {
        Assert.Equal(Enumerable.Range(0, 27), Enum.GetValues<ControlRoomCommandKind>().Select(static value => (int)value));
        Assert.Equal(Enumerable.Range(0, 8), Enum.GetValues<ControlRoomCommandTargetKind>().Select(static value => (int)value));
        Assert.Equal(Enumerable.Range(0, 4), Enum.GetValues<ScenarioRecordingEventKind>().Select(static value => (int)value));
        Assert.Equal(Enumerable.Range(0, 2), Enum.GetValues<ScenarioAutomationIntentKind>().Select(static value => (int)value));
        Assert.Equal(Enumerable.Range(0, 3), Enum.GetValues<PlantControlAuthorityMode>().Select(static value => (int)value));
        Assert.Equal(Enumerable.Range(0, 3), Enum.GetValues<SupervisoryOperatingObjectiveKind>().Select(static value => (int)value));

        var serializer = new JsonScenarioSessionArchiveSerializer();
        var document = JsonNode.Parse(serializer.Serialize(CreateManualDemandArchive(out _)))!.AsObject();
        var command = FindOperatorActionCommand(document, ControlRoomCommandKind.TurbineControlValveManualDemandSet);

        Assert.Equal(26, command["kind"]!.GetValue<int>());
        Assert.Equal(7, command["targetKind"]!.GetValue<int>());
        Assert.Equal(37.5d, command["numericValue"]!.GetValue<double>());
        var recorderEvent = FindRecorderEvent(document, ControlRoomCommandKind.TurbineControlValveManualDemandSet);
        Assert.Equal(0, recorderEvent["kind"]!.GetValue<int>());
    }

    [Fact]
    public void FutureSchemaVersion_FailsClosed()
    {
        var serializer = new JsonScenarioSessionArchiveSerializer();
        const string content = """
        { "schemaVersion": 999, "archiveId": "future", "scenarioJson": "{}", "frames": [] }
        """;

        Assert.Throws<NotSupportedException>(() => serializer.Deserialize(content));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankContent_IsNormalizedToInvalidDataException(string content)
    {
        var serializer = new JsonScenarioSessionArchiveSerializer();

        var exception = Assert.Throws<InvalidDataException>(() => serializer.Deserialize(content));

        Assert.IsAssignableFrom<ArgumentException>(exception.InnerException);
    }

    [Fact]
    public void TruncatedJson_IsNormalizedToInvalidDataException()
    {
        var serializer = new JsonScenarioSessionArchiveSerializer();
        const string content = """
        { "schemaVersion": 1, "archiveId": "truncated",
        """;

        var exception = Assert.Throws<InvalidDataException>(() => serializer.Deserialize(content));

        Assert.IsType<JsonException>(exception.InnerException);
    }

    [Fact]
    public void StructurallyInvalidArchiveRecord_IsNormalizedToInvalidDataException()
    {
        var archive = CreateArchive(out _);
        var serializer = new JsonScenarioSessionArchiveSerializer();
        var document = JsonNode.Parse(serializer.Serialize(archive))!.AsObject();
        var frames = document["frames"]!.AsArray();
        var firstFrame = frames[0]!.AsObject();
        firstFrame["logicalStep"] = -1;

        var exception = Assert.Throws<InvalidDataException>(() => serializer.Deserialize(document.ToJsonString()));

        Assert.IsType<ArgumentOutOfRangeException>(exception.InnerException);
    }


    private static ScenarioSessionArchive CreateManualDemandArchive(out ScenarioSessionFactory factory)
    {
        factory = new ScenarioSessionFactory(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationInitialConditionFactory(),
        }));
        var session = factory.Load(DesktopIntegratedOperationsProgram.Scenario);
        var train = session.Coordinator.Current.TurbineSecondary.AdmissionTrains.Single();
        using var recorder = new ScenarioRecorder(session);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.TurbineControlValveManualMode,
            train.ControlValveId,
            ControlRoomCommandTargetKind.Valve));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.TurbineControlValveManualDemandSet,
            train.ControlValveId,
            ControlRoomCommandTargetKind.Valve,
            37.5d));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        var recording = recorder.Complete();
        return ScenarioSessionArchive.FromRecording("archive-manual-demand", session.Scenario, recording);
    }

    private static JsonObject FindOperatorActionCommand(JsonObject document, ControlRoomCommandKind kind)
        => document["operatorActions"]!.AsArray()
            .Select(static item => item!.AsObject()["command"]!.AsObject())
            .Single(command => command["kind"]!.GetValue<int>() == (int)kind);


    private static JsonObject FindRecorderEvent(JsonObject document, ControlRoomCommandKind commandKind)
        => document["events"]!.AsArray()
            .Select(static item => item!.AsObject())
            .Single(item => item["operatorCommand"] is JsonObject command
                && command["kind"]!.GetValue<int>() == (int)commandKind);

    private static ScenarioSessionArchive CreateArchive(out ScenarioSessionFactory factory)
    {
        factory = new ScenarioSessionFactory(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationInitialConditionFactory(),
        }));
        var session = factory.Load(DesktopIntegratedOperationsProgram.Scenario);
        using var recorder = new ScenarioRecorder(session);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        _ = session.Coordinator.AdvanceRunning(stepCount: 2, publicationStride: 2);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Pause));
        _ = recorder.CreateCheckpoint("cp-json");
        return ScenarioSessionArchive.FromRecording("archive-json", session.Scenario, recorder.Capture());
    }
}
