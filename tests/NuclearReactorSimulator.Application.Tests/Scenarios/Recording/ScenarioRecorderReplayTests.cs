using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Recording;

public sealed class ScenarioRecorderReplayTests
{
    [Fact]
    public void Recorder_CapturesEveryDeterministicStep_IndependentOfPublicationStride()
    {
        var session = CreateFactory().Load(PowerManoeuvringNormalShutdownProgram.Scenario);
        using var recorder = new ScenarioRecorder(session);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        var batch = session.Coordinator.AdvanceRunning(stepCount: 4, publicationStride: 4);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Pause));
        var recording = recorder.Complete();

        Assert.Equal(4, batch.ExecutedStepCount);
        Assert.Equal(1, batch.PublishedSnapshotCount);
        Assert.Equal(new long[] { 0, 1, 2, 3, 4 }, recording.Frames.Select(static frame => frame.LogicalStep));
        Assert.All(recording.Frames, static frame => Assert.False(string.IsNullOrWhiteSpace(frame.SnapshotFingerprint)));
    }

    [Fact]
    public void SnapshotFingerprint_NormalizesHostRunPauseState()
    {
        var session = CreateFactory().Load(PowerManoeuvringNormalShutdownProgram.Scenario);
        var pausedFingerprint = ControlRoomSnapshotFingerprint.Compute(session.Coordinator.Current);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Run));
        var runningFingerprint = ControlRoomSnapshotFingerprint.Compute(session.Coordinator.Current);

        Assert.Equal(pausedFingerprint, runningFingerprint);
    }

    [Fact]
    public void Recorder_MapsAcceptedOperatorActionToNextApplicationStep()
    {
        var session = CreateFactory().Load(PowerManoeuvringNormalShutdownProgram.Scenario);
        using var recorder = new ScenarioRecorder(session);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.GeneratorLoadRaise, "generator", ControlRoomCommandTargetKind.Generator));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        var recording = recorder.Complete();
        var trace = recording.CreateOperatorCommandTrace();

        var action = Assert.Single(recording.OperatorActions);
        Assert.Equal(0, action.LogicalStep);
        var entry = Assert.Single(trace.Entries);
        Assert.Equal(1, entry.StepIndex);
        Assert.Equal(ControlRoomCommandKind.GeneratorLoadRaise, entry.Command.Kind);
        Assert.Contains(recording.Events, static item => item.Kind == ScenarioRecordingEventKind.OperatorAction);
    }

    [Fact]
    public void FullReplay_ReconstructsAndVerifiesEveryRecordedFrameAndEvent()
    {
        var factory = CreateFactory();
        var session = factory.Load(PowerManoeuvringNormalShutdownProgram.Scenario);
        using var recorder = new ScenarioRecorder(session);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.GeneratorLoadRaise, "generator", ControlRoomCommandTargetKind.Generator));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.ControlRodHold, "regulating", ControlRoomCommandTargetKind.ControlRodGroup));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        var recording = recorder.Complete();

        var result = new ScenarioFullReplayRunner(factory).ReplayAndVerify(
            PowerManoeuvringNormalShutdownProgram.Scenario,
            recording);

        Assert.Equal(recording.Frames.Count, result.VerifiedFrameCount);
        Assert.Equal(recording.Events.Count, result.VerifiedEventCount);
        Assert.Equal(recording.FinalLogicalStep, result.Session.Coordinator.Current.LogicalStep);
        Assert.Equal(
            recording.Frames[^1].SnapshotFingerprint,
            ControlRoomSnapshotFingerprint.Compute(result.Session.Coordinator.Current));
    }

    [Fact]
    public void SeekAndVerify_ReplaysExactActionPrefixToVersionedCheckpoint()
    {
        var factory = CreateFactory();
        var session = factory.Load(PowerManoeuvringNormalShutdownProgram.Scenario);
        using var recorder = new ScenarioRecorder(session);

        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.GeneratorLoadLower, "generator", ControlRoomCommandTargetKind.Generator));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        var checkpoint = recorder.CreateCheckpoint("after-two-steps");
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        var recording = recorder.Complete();

        var sought = new ScenarioFullReplayRunner(factory).SeekAndVerify(
            PowerManoeuvringNormalShutdownProgram.Scenario,
            recording,
            checkpoint);

        Assert.Equal(ScenarioCheckpoint.CurrentSchemaVersion, checkpoint.SchemaVersion);
        Assert.Equal(2, sought.Coordinator.Current.LogicalStep);
        Assert.Equal(checkpoint.SnapshotFingerprint, ControlRoomSnapshotFingerprint.Compute(sought.Coordinator.Current));
    }

    [Fact]
    public void FullReplay_FailsClosedOnTamperedFrameFingerprint()
    {
        var factory = CreateFactory();
        var session = factory.Load(PowerManoeuvringNormalShutdownProgram.Scenario);
        using var recorder = new ScenarioRecorder(session);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        var original = recorder.Complete();

        var frames = original.Frames.ToArray();
        var tampered = frames[1];
        frames[1] = new ScenarioRecordingFrame(
            tampered.LogicalStep,
            tampered.Snapshot,
            new string('0', 64),
            tampered.FirstEventSequence,
            tampered.LastEventSequence);
        var recording = new ScenarioRecording(
            original.ScenarioId,
            original.InitialCondition,
            frames,
            original.OperatorActions,
            original.Events);

        var exception = Assert.Throws<ScenarioReplayDivergenceException>(() =>
            new ScenarioFullReplayRunner(factory).ReplayAndVerify(
                PowerManoeuvringNormalShutdownProgram.Scenario,
                recording));

        Assert.Equal(1, exception.LogicalStep);
    }

    [Fact]
    public void Recorder_RejectsCompletionWithAcceptedActionNotYetApplied()
    {
        var session = CreateFactory().Load(PowerManoeuvringNormalShutdownProgram.Scenario);
        using var recorder = new ScenarioRecorder(session);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.GeneratorLoadLower, "generator", ControlRoomCommandTargetKind.Generator));

        Assert.Throws<InvalidOperationException>(() => recorder.Complete());
    }

    [Fact]
    public void FingerprintV1_IgnoresM1071PresentationOnlyResetAndSynchronizationDiagnostics()
    {
        var normal = new ControlRoomValueSnapshot("0", string.Empty, 0d, ControlRoomVisualState.Normal);
        var leftGenerator = new GeneratorPresentationSnapshot(
            "generator", "rotor", "breaker", normal, normal, normal, normal, normal, normal, normal,
            false, false, false, false,
            0.05d, 0.2d, 2d, 10d, 1d, 10d);
        var rightGenerator = leftGenerator with
        {
            CloseCheckFrequencyDifferenceHz = 99d,
            CloseCheckPhaseDifferenceDegrees = 99d,
            CloseCheckVoltageDifferenceKilovolts = 99d,
        };

        Assert.Equal(ControlRoomVisualState.Warning, leftGenerator.SynchronizationState);
        Assert.Equal("OUTSIDE SYNCHRONIZATION WINDOW", leftGenerator.SynchronizationText);
        Assert.Equal(ControlRoomVisualState.Warning, leftGenerator.DisplaySynchronizationState);
        Assert.Contains("SYNC NOT READY", leftGenerator.DisplaySynchronizationText);

        var left = new ControlRoomSnapshot(
            7, ControlRoomRunState.Paused, 0, 0, 0, 0, false, true, false,
            electrical: new ElectricalPanelSnapshot(
                ElectricalGridPresentationSnapshot.Unavailable,
                new[] { leftGenerator },
                normal,
                false),
            protectionReset: new ProtectionResetPresentationSnapshot(true, false, true, false, new[] { "blocked-a" }));
        var right = new ControlRoomSnapshot(
            7, ControlRoomRunState.Paused, 0, 0, 0, 0, false, true, false,
            electrical: new ElectricalPanelSnapshot(
                ElectricalGridPresentationSnapshot.Unavailable,
                new[] { rightGenerator },
                normal,
                false),
            protectionReset: new ProtectionResetPresentationSnapshot(true, true, true, true));

        Assert.Equal(ControlRoomSnapshotFingerprint.Compute(left), ControlRoomSnapshotFingerprint.Compute(right));
    }

    [Fact]
    public void FingerprintV1_IgnoresM10712PresentationOnlyRodTargetEffectiveMotion()
    {
        var normal = new ControlRoomValueSnapshot("0", string.Empty, 0d, ControlRoomVisualState.Normal);
        var leftTarget = new ReactorRodTargetPresentationSnapshot("all-rods", ControlRoomCommandTargetKind.ControlRodGroup)
        {
            EffectiveMotion = "HOLD",
        };
        var rightTarget = leftTarget with
        {
            EffectiveMotion = "WITHDRAW",
        };

        ReactorCorePanelSnapshot Reactor(ReactorRodTargetPresentationSnapshot target) => new(
            normal,
            normal,
            normal,
            normal,
            normal,
            normal,
            ControlRoomValueSnapshot.Unavailable("pcm"),
            Array.Empty<ReactorCoreZonePresentationSnapshot>(),
            Array.Empty<ReactorRodPresentationSnapshot>(),
            new[] { target },
            false,
            false);

        var left = new ControlRoomSnapshot(8, ControlRoomRunState.Paused, 0, 0, 0, 0, false, false, false, reactorCore: Reactor(leftTarget));
        var right = new ControlRoomSnapshot(8, ControlRoomRunState.Paused, 0, 0, 0, 0, false, false, false, reactorCore: Reactor(rightTarget));

        Assert.Equal(ControlRoomSnapshotFingerprint.Compute(left), ControlRoomSnapshotFingerprint.Compute(right));
    }

    private static ScenarioSessionFactory CreateFactory()
        => new(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new PowerManoeuvringInitialConditionFactory(),
        }));
}
