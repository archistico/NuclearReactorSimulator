using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Analysis;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Analysis;

public sealed class ScenarioPostIncidentAnalyzerTests
{
    [Fact]
    public void Analyze_DefaultAnchorPrefersFirstFaultEvidenceWithoutClaimingCausality()
    {
        var report = new ScenarioPostIncidentAnalyzer().Analyze(
            CreateRecording(),
            new PostIncidentAnalysisOptions(preIncidentSteps: 1, postIncidentSteps: 2));

        Assert.Equal(2, report.AnchorEventSequence);
        Assert.Equal(2, report.AnchorLogicalStep);
        Assert.Equal(PostIncidentAnalysisAnchorKind.FaultTransition, report.AnchorKind);
        Assert.Equal("break-a", report.AnchorSourceId);
        Assert.Equal(1, report.WindowStartLogicalStep);
        Assert.Equal(4, report.WindowEndLogicalStep);
        Assert.Equal("cp-before", report.PrecedingCheckpointId);
        Assert.Equal(new long[] { -1, 0, 1, 2 }, report.Trends.Select(static sample => sample.RelativeLogicalStep));

        var earlierAlarm = Assert.Single(report.Timeline, static item => item.Sequence == 1);
        Assert.Equal(PostIncidentTemporalRelation.BeforeAnchor, earlierAlarm.Relation);
        Assert.Equal(-1, earlierAlarm.RelativeLogicalStep);

        var anchor = Assert.Single(report.Timeline, static item => item.Sequence == 2);
        Assert.Equal(PostIncidentTemporalRelation.Anchor, anchor.Relation);

        Assert.Equal<long?>(1L, report.Metrics.FirstAlarmLatencySteps);
        Assert.Equal<long?>(1L, report.Metrics.FirstProtectionActivationLatencySteps);
        Assert.Equal<long?>(2L, report.Metrics.FirstOperatorActionLatencySteps);
        Assert.Null(report.Metrics.FirstFaultClearLatencySteps);
        Assert.Equal(3, report.Metrics.PeakInvalidMeasuredSignalCount);
        Assert.Equal(4, report.Metrics.PeakAnnunciatedAlarmCount);
        Assert.Equal(3, report.Metrics.PeakUnacknowledgedAlarmCount);
        var operatorEntry = Assert.Single(report.Timeline, static item => item.Kind == ScenarioRecordingEventKind.OperatorAction);
        var operatorCommand = Assert.IsType<ControlRoomCommand>(operatorEntry.OperatorCommand);
        Assert.Equal(ControlRoomCommandKind.AlarmAcknowledge, operatorCommand.Kind);
    }

    [Fact]
    public void Analyze_ExplicitAnchorUsesExactRecorderEventSequence()
    {
        var report = new ScenarioPostIncidentAnalyzer().Analyze(
            CreateRecording(),
            new PostIncidentAnalysisOptions(preIncidentSteps: 0, postIncidentSteps: 2, anchorEventSequence: 4));

        Assert.Equal(4, report.AnchorEventSequence);
        Assert.Equal(3, report.AnchorLogicalStep);
        Assert.Equal(PostIncidentAnalysisAnchorKind.ProtectionTransition, report.AnchorKind);
        Assert.Equal<long?>(0L, report.Metrics.FirstProtectionActivationLatencySteps);
        Assert.Equal<long?>(1L, report.Metrics.FirstOperatorActionLatencySteps);
        Assert.Equal<long?>(2L, report.Metrics.FirstFaultClearLatencySteps);
    }

    [Fact]
    public void Analyze_ThrowsWhenRecordingContainsNoObservedEvents()
    {
        var frames = new[] { Frame(0, Snapshot(0, 0, 0, 0, false)) };
        var recording = new ScenarioRecording(
            "quiet",
            new InitialConditionReference("ic", 1),
            frames,
            Array.Empty<ScenarioOperatorActionRecord>(),
            Array.Empty<ScenarioRecordingEvent>());

        Assert.Throws<InvalidOperationException>(() => new ScenarioPostIncidentAnalyzer().Analyze(recording));
    }


    [Fact]
    public void Analyze_ConsumesM91RecordingWithoutMutatingRuntime()
    {
        var factory = new ScenarioSessionFactory(
            new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
            {
                new PowerManoeuvringInitialConditionFactory(),
            }));
        var session = factory.Load(PowerManoeuvringNormalShutdownProgram.Scenario);
        using var recorder = new ScenarioRecorder(session);

        session.CommandDispatcher.Dispatch(
            new ControlRoomCommand(
                ControlRoomCommandKind.GeneratorLoadLower,
                "generator",
                ControlRoomCommandTargetKind.Generator));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        var recording = recorder.Complete();
        var beforeAnalysisStep = session.Coordinator.Current.LogicalStep;

        var report = new ScenarioPostIncidentAnalyzer().Analyze(recording);

        Assert.Equal(PostIncidentAnalysisAnchorKind.OperatorAction, report.AnchorKind);
        Assert.Equal(0, report.AnchorLogicalStep);
        Assert.Equal(beforeAnalysisStep, session.Coordinator.Current.LogicalStep);
        Assert.Equal<long?>(0L, report.Metrics.FirstOperatorActionLatencySteps);
        Assert.Equal(recording.Frames.Count, report.Trends.Count);
        Assert.Contains(report.Trends, static sample => sample.TotalPrimaryMass.HasValue);
    }

    [Fact]
    public void Analyze_UnknownExplicitAnchorFailsClosed()
    {
        Assert.Throws<ArgumentException>(() => new ScenarioPostIncidentAnalyzer().Analyze(
            CreateRecording(),
            new PostIncidentAnalysisOptions(anchorEventSequence: 999)));
    }

    private static ScenarioRecording CreateRecording()
    {
        var frames = new[]
        {
            Frame(0, Snapshot(0, 0, 0, 0, false)),
            Frame(1, Snapshot(1, 0, 1, 1, false)),
            Frame(2, Snapshot(2, 1, 2, 2, false)),
            Frame(3, Snapshot(3, 3, 4, 3, true)),
            Frame(4, Snapshot(4, 2, 3, 2, true)),
            Frame(5, Snapshot(5, 1, 2, 1, true)),
        };
        var events = new[]
        {
            new ScenarioRecordingEvent(1, 1, ScenarioRecordingEventKind.Alarm, "alarm-early", "Activated:Early"),
            new ScenarioRecordingEvent(2, 2, ScenarioRecordingEventKind.FaultTransition, "break-a", "Active"),
            new ScenarioRecordingEvent(3, 3, ScenarioRecordingEventKind.Alarm, "alarm-main", "Activated:Main"),
            new ScenarioRecordingEvent(4, 3, ScenarioRecordingEventKind.ProtectionTransition, "reactor-scram", "Active"),
            new ScenarioRecordingEvent(
                5,
                4,
                ScenarioRecordingEventKind.OperatorAction,
                "alarm-main",
                ControlRoomCommandKind.AlarmAcknowledge.ToString(),
                new ControlRoomCommand(ControlRoomCommandKind.AlarmAcknowledge, "alarm-main", ControlRoomCommandTargetKind.Alarm)),
            new ScenarioRecordingEvent(6, 5, ScenarioRecordingEventKind.FaultTransition, "break-a", "Cleared"),
        };
        var actions = new[]
        {
            new ScenarioOperatorActionRecord(
                1,
                4,
                new ControlRoomCommand(ControlRoomCommandKind.AlarmAcknowledge, "alarm-main", ControlRoomCommandTargetKind.Alarm)),
        };
        var checkpoint = new ScenarioCheckpoint(
            "cp-before",
            ScenarioCheckpoint.CurrentSchemaVersion,
            "incident",
            new InitialConditionReference("ic", 1),
            1,
            0,
            ControlRoomSnapshotFingerprint.AlgorithmId,
            "fp1");

        return new ScenarioRecording(
            "incident",
            new InitialConditionReference("ic", 1),
            frames,
            actions,
            events,
            new[] { checkpoint });
    }

    private static ScenarioRecordingFrame Frame(long step, ControlRoomSnapshot snapshot)
        => new(step, snapshot, $"fp{step}", 0, 0);

    private static ControlRoomSnapshot Snapshot(
        long step,
        int invalidSignals,
        int annunciated,
        int unacknowledged,
        bool scram)
        => new(
            step,
            ControlRoomRunState.Paused,
            totalMeasuredSignalCount: 10,
            invalidMeasuredSignalCount: invalidSignals,
            annunciatedAlarmCount: annunciated,
            unacknowledgedAlarmCount: unacknowledged,
            reactorScramActive: scram,
            turbineTripActive: false,
            generatorTripActive: false);
}
