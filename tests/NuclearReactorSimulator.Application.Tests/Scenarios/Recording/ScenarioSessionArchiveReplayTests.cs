using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Recording;

public sealed class ScenarioSessionArchiveReplayTests
{
    [Fact]
    public void CompactArchive_ReplaysAndRestoresCheckpointThroughCanonicalM91Authority()
    {
        var factory = CreateFactory();
        var session = factory.Load(DesktopIntegratedOperationsProgram.Scenario);
        using var recorder = new ScenarioRecorder(session);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        _ = session.Coordinator.AdvanceRunning(stepCount: 3, publicationStride: 3);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Pause));
        var checkpoint = recorder.CreateCheckpoint("cp-test");
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        _ = session.Coordinator.AdvanceRunning(stepCount: 2, publicationStride: 2);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Pause));

        var recording = recorder.Capture();
        var archive = ScenarioSessionArchive.FromRecording("archive-test", session.Scenario, recording);
        var runner = new ScenarioFullReplayRunner(factory);

        var replay = runner.ReplayAndVerify(archive);
        var seek = runner.SeekAndVerify(archive, checkpoint.CheckpointId);

        Assert.Equal(recording.FinalLogicalStep, replay.Session.Coordinator.Current.LogicalStep);
        Assert.Equal(recording.Frames.Count, replay.VerifiedFrameCount);
        Assert.Equal(checkpoint.LogicalStep, seek.Session.Coordinator.Current.LogicalStep);
        Assert.Equal(checkpoint.SnapshotFingerprint, ControlRoomSnapshotFingerprint.Compute(seek.Session.Coordinator.Current));
    }


    [Fact]
    public void CheckpointPrefix_PreservesAppliedOperatorActionEventsAcceptedBetweenFrames()
    {
        var factory = CreateFactory();
        var session = factory.Load(PowerManoeuvringNormalShutdownProgram.Scenario);
        using var recorder = new ScenarioRecorder(session);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadLower,
            "generator",
            ControlRoomCommandTargetKind.Generator));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        var checkpoint = recorder.CreateCheckpoint("cp-after-first-action");

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.ControlRodHold,
            "regulating",
            ControlRoomCommandTargetKind.ControlRodGroup));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));

        var archive = ScenarioSessionArchive.FromRecording("action-prefix-test", session.Scenario, recorder.Capture());
        var prefix = archive.ThroughLogicalStep(checkpoint.LogicalStep);
        var restored = new ScenarioFullReplayRunner(factory).SeekAndVerify(archive, checkpoint.CheckpointId);

        Assert.Single(prefix.OperatorActions);
        Assert.Single(prefix.Events, static item => item.Kind == ScenarioRecordingEventKind.OperatorAction);
        Assert.Equal(Enumerable.Range(1, prefix.Events.Count).Select(static value => (long)value), prefix.Events.Select(static item => item.Sequence));
        Assert.Equal(checkpoint.LogicalStep, restored.Session.Coordinator.Current.LogicalStep);
        Assert.Equal(checkpoint.SnapshotFingerprint, ControlRoomSnapshotFingerprint.Compute(restored.Session.Coordinator.Current));
    }

    [Fact]
    public void Recorder_CanResumeFromVerifiedReplayPrefixWithoutLosingDeterministicEvidence()
    {
        var factory = CreateFactory();
        var session = factory.Load(DesktopIntegratedOperationsProgram.Scenario);
        ScenarioRecording prefix;
        using (var recorder = new ScenarioRecorder(session))
        {
            session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
            _ = session.Coordinator.AdvanceRunning(stepCount: 2, publicationStride: 2);
            session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Pause));
            prefix = recorder.Capture();
        }

        var archive = ScenarioSessionArchive.FromRecording("resume-test", session.Scenario, prefix);
        var replay = new ScenarioFullReplayRunner(factory).ReplayAndVerify(archive);
        using var resumed = new ScenarioRecorder(replay.Session, replay.ReplayedRecording);

        replay.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        _ = replay.Session.Coordinator.AdvanceRunning(stepCount: 1, publicationStride: 1);
        replay.Session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Pause));
        var extended = resumed.Capture();

        Assert.Equal(prefix.FinalLogicalStep + 1, extended.FinalLogicalStep);
        Assert.Equal(prefix.Frames.Count + 1, extended.Frames.Count);
        Assert.Equal(prefix.Frames[0].SnapshotFingerprint, extended.Frames[0].SnapshotFingerprint);
    }

    private static ScenarioSessionFactory CreateFactory()
        => new(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationInitialConditionFactory(),
            new PowerManoeuvringInitialConditionFactory(),
        }));
}
