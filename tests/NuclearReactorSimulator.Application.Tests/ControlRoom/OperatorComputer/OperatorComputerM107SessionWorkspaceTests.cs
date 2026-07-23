using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.OperatorComputer;

public sealed class OperatorComputerM107SessionWorkspaceTests
{
    [Fact]
    public void SessionWorkspace_WithoutRecorder_IsVisibleButCannotFabricateCheckpointOrArchive()
    {
        var factory = CreateFactory();
        var session = factory.Load(DesktopIntegratedOperationsProgram.Scenario);
        var workspace = new OperatorComputerSessionWorkspaceController(
            session,
            recorder: null,
            new ScenarioFullReplayRunner(factory),
            new TestArchiveSerializer());

        Assert.False(workspace.Current.RecorderActive);
        Assert.Empty(workspace.Current.Checkpoints);
        Assert.Throws<InvalidOperationException>(() => workspace.CreateCheckpoint());
        Assert.Throws<InvalidOperationException>(() => workspace.CaptureArchive());
    }

    [Fact]
    public void SessionWorkspace_RecordedPausedSession_CreatesCheckpointAndVerifiesReplay()
    {
        var factory = CreateFactory();
        var session = factory.Load(DesktopIntegratedOperationsProgram.Scenario);
        using var recorder = new ScenarioRecorder(session);
        var workspace = new OperatorComputerSessionWorkspaceController(
            session,
            recorder,
            new ScenarioFullReplayRunner(factory),
            new TestArchiveSerializer());

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        _ = session.Coordinator.AdvanceRunning(stepCount: 3, publicationStride: 3);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Pause));
        var checkpoint = workspace.CreateCheckpoint();
        var archive = workspace.CaptureArchive();
        var verification = workspace.VerifyCurrentReplay();

        Assert.True(workspace.Current.RecorderActive);
        Assert.Single(workspace.Current.Checkpoints);
        Assert.Equal(checkpoint.CheckpointId, workspace.Current.Checkpoints[0].CheckpointId);
        Assert.Equal(session.Coordinator.Current.LogicalStep, archive.FinalLogicalStep);
        Assert.Contains("VERIFIED", verification);
    }

    private static ScenarioSessionFactory CreateFactory()
        => new(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationInitialConditionFactory(),
        }));

    private sealed class TestArchiveSerializer : IScenarioSessionArchiveSerializer
    {
        public string Serialize(ScenarioSessionArchive archive) => archive.ArchiveId;
        public ScenarioSessionArchive Deserialize(string content) => throw new NotSupportedException();
    }
}
