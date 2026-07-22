using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Infrastructure.Scenarios.Recording;
using Xunit;

namespace NuclearReactorSimulator.Infrastructure.Tests.Scenarios.Recording;

public sealed class JsonScenarioSessionArchiveSerializerTests
{
    [Fact]
    public void RoundTrip_PreservesReplayEvidenceAndExactScenarioIdentity()
    {
        var factory = new ScenarioSessionFactory(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopIntegratedOperationsInitialConditionFactory(),
        }));
        var session = factory.Load(DesktopIntegratedOperationsProgram.Scenario);
        using var recorder = new ScenarioRecorder(session);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        _ = session.Coordinator.AdvanceRunning(stepCount: 2, publicationStride: 2);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Pause));
        _ = recorder.CreateCheckpoint("cp-json");
        var archive = ScenarioSessionArchive.FromRecording("archive-json", session.Scenario, recorder.Capture());
        var serializer = new JsonScenarioSessionArchiveSerializer();

        var restored = serializer.Deserialize(serializer.Serialize(archive));
        var replay = new ScenarioFullReplayRunner(factory).ReplayAndVerify(restored);

        Assert.Equal(archive.ArchiveId, restored.ArchiveId);
        Assert.Equal(archive.Scenario.ScenarioId, restored.Scenario.ScenarioId);
        Assert.Equal(archive.Scenario.InitialCondition, restored.Scenario.InitialCondition);
        Assert.Equal(archive.Frames.Select(static frame => frame.SnapshotFingerprint), restored.Frames.Select(static frame => frame.SnapshotFingerprint));
        Assert.Single(restored.Checkpoints);
        Assert.Equal(archive.FinalLogicalStep, replay.Session.Coordinator.Current.LogicalStep);
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
}
