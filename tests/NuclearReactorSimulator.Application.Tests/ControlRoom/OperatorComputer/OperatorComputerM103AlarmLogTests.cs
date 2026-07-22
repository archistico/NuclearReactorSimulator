using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Analysis;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.OperatorComputer;

public sealed class OperatorComputerM103AlarmLogTests
{
    [Fact]
    public void AlarmProjector_PreservesCanonicalAnnunciatorStateAndBoundedEventOrdering()
    {
        var alarm = new ControlRoomAlarmPresentationSnapshot(
            "alarm-a",
            "Alarm A",
            ControlRoomAlarmSeverity.Warning,
            ControlRoomAlarmAnnunciatorState.ActiveUnacknowledged,
            "group-a",
            ConditionActive: true,
            IsLatched: true,
            IsAcknowledged: false,
            IsAnnunciated: true,
            IsFirstOut: true,
            ActivationSequence: 10,
            IsLatchedUntilReset: true);
        var currentEvent = new ControlRoomAlarmEventPresentationSnapshot(11, 7, "alarm-a", "Alarm A", ControlRoomAlarmEventKind.Activated);
        var snapshot = new ControlRoomSnapshot(
            logicalStep: 7,
            runState: ControlRoomRunState.Paused,
            totalMeasuredSignalCount: 0,
            invalidMeasuredSignalCount: 0,
            annunciatedAlarmCount: 1,
            unacknowledgedAlarmCount: 1,
            reactorScramActive: false,
            turbineTripActive: false,
            generatorTripActive: false,
            alarmEvents: new AlarmEventsPanelSnapshot(
                new[] { alarm },
                new[] { new ControlRoomFirstOutGroupPresentationSnapshot("group-a", "alarm-a", new[] { "alarm-a" }) },
                new[] { currentEvent }));
        var older = new ControlRoomAlarmEventPresentationSnapshot(9, 5, "alarm-b", "Alarm B", ControlRoomAlarmEventKind.Cleared);
        var history = new ControlRoomOperationalHistorySnapshot(Array.Empty<ControlRoomTrendSeriesSnapshot>(), new[] { older, currentEvent });

        var projected = OperatorComputerAlarmLogProjector.ProjectAlarms(snapshot, history);

        var row = Assert.Single(projected.Alarms);
        Assert.Equal("alarm-a", row.AlarmId);
        Assert.True(row.IsFirstOut);
        Assert.True(row.CanAcknowledge);
        Assert.False(row.CanReset);
        Assert.Equal(1, projected.AnnunciatedCount);
        Assert.Equal(1, projected.UnacknowledgedCount);
        Assert.Equal(1, projected.FirstOutCount);
        Assert.Equal(new long[] { 11, 9 }, projected.RecentEvents.Select(static item => item.Sequence));
    }

    [Fact]
    public void LogProjector_ReusesM66HistoryM91EventsAndM92ReportWithoutCreatingSecondStorage()
    {
        var trend = new ControlRoomTrendSeriesSnapshot(
            "power",
            "Reactor thermal power",
            "MW",
            "MEASURED",
            new[] { new ControlRoomTrendPointSnapshot(1, 10d), new ControlRoomTrendPointSnapshot(2, 12d) },
            10d,
            12d,
            12d,
            "▁█");
        var alarmEvent = new ControlRoomAlarmEventPresentationSnapshot(2, 2, "alarm-a", "Alarm A", ControlRoomAlarmEventKind.Activated);
        var history = new ControlRoomOperationalHistorySnapshot(new[] { trend }, new[] { alarmEvent });
        var sessionEvent = new ScenarioRecordingEvent(1, 2, ScenarioRecordingEventKind.Alarm, "alarm-a", "Activated:Alarm A");

        var session = new ScenarioSessionFactory(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new PowerManoeuvringInitialConditionFactory(),
        })).Load(PowerManoeuvringNormalShutdownProgram.Scenario);
        using var recorder = new ScenarioRecorder(session);
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadLower,
            "generator",
            ControlRoomCommandTargetKind.Generator));
        session.CommandDispatcher.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.SingleStep));
        var recording = recorder.Complete();
        var report = new ScenarioPostIncidentAnalyzer().Analyze(recording);

        var projected = OperatorComputerAlarmLogProjector.ProjectLog(history, new[] { sessionEvent }, report);

        Assert.Single(projected.LiveTrends);
        Assert.Equal("12 MW", projected.LiveTrends[0].Current);
        Assert.Single(projected.LiveEvents);
        Assert.True(projected.SessionEvidenceAvailable);
        Assert.Single(projected.SessionEvents);
        Assert.NotNull(projected.Incident);
        Assert.Contains("OPERATORACTION", projected.Incident!.AnchorText.Replace("_", string.Empty, StringComparison.Ordinal));
        Assert.NotEmpty(projected.Incident.MetricLines);
        Assert.NotEmpty(projected.Incident.Timeline);
    }

    [Fact]
    public void SnapshotProjector_WithHistory_ActivatesM103AlarmAndLogPagesButLeavesLaterPagesStaged()
    {
        var history = ControlRoomOperationalHistorySnapshot.Empty;
        var snapshot = OperatorComputerSnapshotProjector.Project(ControlRoomSnapshot.ShellOnly, operationalHistory: history);

        Assert.Equal(OperatorComputerPageContentState.Available, snapshot.Pages.Single(static page => page.Id == OperatorComputerPageId.Alarms).ContentState);
        Assert.Equal(OperatorComputerPageContentState.Available, snapshot.Pages.Single(static page => page.Id == OperatorComputerPageId.Log).ContentState);
        Assert.NotNull(snapshot.Alarms);
        Assert.NotNull(snapshot.Log);
        Assert.Equal(OperatorComputerPageContentState.Available, snapshot.Pages.Single(static page => page.Id == OperatorComputerPageId.Commands).ContentState);
        Assert.All(snapshot.Pages.Where(static page => page.Id is OperatorComputerPageId.Modes or OperatorComputerPageId.Session),
            static page => Assert.Equal(OperatorComputerPageContentState.ShellOnly, page.ContentState));
    }
}
