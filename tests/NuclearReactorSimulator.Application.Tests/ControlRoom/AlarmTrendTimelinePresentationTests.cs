using NuclearReactorSimulator.Application.ControlRoom;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom;

public sealed class AlarmTrendTimelinePresentationTests
{
    [Fact]
    public void TrendCatalog_IsStableUniqueAndWithinDesktopBudget()
    {
        var sources = ControlRoomTrendSourceCatalog.Default;

        Assert.NotEmpty(sources);
        Assert.Equal(sources.Count, sources.Select(static source => source.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.InRange(ControlRoomTrendSourceCatalog.DefaultEnabledSourceIds.Count, 1, ControlRoomPerformanceBudget.DesktopDefault.MaximumVisibleTrendSeries);
        Assert.All(ControlRoomTrendSourceCatalog.DefaultEnabledSourceIds, id => Assert.Contains(sources, source => source.Id == id));
    }

    [Fact]
    public void HistoryAccumulator_UsesLogicalStepsAndAlarmSequenceWithBoundedDeterministicHistory()
    {
        var history = new ControlRoomOperationalHistoryAccumulator(
            new[] { ControlRoomTrendSourceCatalog.UnacknowledgedAlarms },
            sampleCapacity: 2,
            eventCapacity: 2,
            maximumVisibleTrendSeries: 1);

        history.Observe(CreateSnapshot(1, 1, new ControlRoomAlarmEventPresentationSnapshot(10, 1, "a", "Alarm A", ControlRoomAlarmEventKind.Activated)));
        history.Observe(CreateSnapshot(2, 2, new ControlRoomAlarmEventPresentationSnapshot(11, 2, "a", "Alarm A", ControlRoomAlarmEventKind.Acknowledged)));
        history.Observe(CreateSnapshot(2, 4, new ControlRoomAlarmEventPresentationSnapshot(11, 2, "a", "Alarm A", ControlRoomAlarmEventKind.Acknowledged)));
        var result = history.Observe(CreateSnapshot(3, 3, new ControlRoomAlarmEventPresentationSnapshot(12, 3, "b", "Alarm B", ControlRoomAlarmEventKind.Activated)));

        var series = Assert.Single(result.TrendSeries);
        Assert.Equal(new long[] { 2L, 3L }, series.Points.Select(static point => point.LogicalStep).ToArray());
        Assert.Equal(new double?[] { 4d, 3d }, series.Points.Select(static point => point.Value).ToArray());
        Assert.Equal(new long[] { 12L, 11L }, result.Events.Select(static item => item.Sequence).ToArray());
        Assert.Equal(new long[] { 3L, 2L }, result.Events.Select(static item => item.LogicalStep).ToArray());
    }

    [Fact]
    public void AlarmCommands_AreTypedPresentationIntentsOnly()
    {
        var acknowledge = new ControlRoomCommand(
            ControlRoomCommandKind.AlarmAcknowledge,
            "steam-drum-pressure-high",
            ControlRoomCommandTargetKind.Alarm);
        var resetAll = new ControlRoomCommand(ControlRoomCommandKind.AlarmResetAll);

        Assert.Equal(ControlRoomCommandTargetKind.Alarm, acknowledge.TargetKind);
        Assert.Equal("steam-drum-pressure-high", acknowledge.TargetId);
        Assert.Equal(ControlRoomCommandKind.AlarmResetAll, resetAll.Kind);
        Assert.Null(resetAll.TargetId);
    }

    [Fact]
    public void M66PresentationTypes_DoNotExposeSimulationOrDomainPhysicsTypes()
    {
        var presentationTypes = new[]
        {
            typeof(AlarmEventsPanelSnapshot),
            typeof(ControlRoomAlarmPresentationSnapshot),
            typeof(ControlRoomFirstOutGroupPresentationSnapshot),
            typeof(ControlRoomAlarmEventPresentationSnapshot),
            typeof(ControlRoomTrendSourceDescriptor),
            typeof(ControlRoomTrendPointSnapshot),
            typeof(ControlRoomTrendSeriesSnapshot),
            typeof(ControlRoomOperationalHistorySnapshot),
        };

        foreach (var presentationType in presentationTypes)
        {
            Assert.All(
                presentationType.GetProperties(),
                static property =>
                {
                    var ns = property.PropertyType.Namespace;
                    Assert.False(ns?.StartsWith("NuclearReactorSimulator.Simulation", StringComparison.Ordinal) == true);
                    Assert.False(ns?.StartsWith("NuclearReactorSimulator.Domain.Physics", StringComparison.Ordinal) == true);
                });
        }
    }

    private static ControlRoomSnapshot CreateSnapshot(
        long logicalStep,
        int unacknowledgedCount,
        ControlRoomAlarmEventPresentationSnapshot alarmEvent)
        => new(
            logicalStep,
            ControlRoomRunState.Running,
            0,
            0,
            unacknowledgedCount,
            unacknowledgedCount,
            false,
            false,
            false,
            alarmEvents: new AlarmEventsPanelSnapshot(
                Array.Empty<ControlRoomAlarmPresentationSnapshot>(),
                Array.Empty<ControlRoomFirstOutGroupPresentationSnapshot>(),
                new[] { alarmEvent }));
}
