using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.OperatorComputer;

public sealed class OperatorComputerContractsTests
{
    [Fact]
    public void FixedPageCatalog_ProvidesExactlyEightStableNamedPages()
    {
        var pages = OperatorComputerPageCatalog.Default;

        Assert.Equal(8, pages.Count);
        Assert.Equal(
            new[]
            {
                OperatorComputerPageId.Guidance,
                OperatorComputerPageId.Info,
                OperatorComputerPageId.Alarms,
                OperatorComputerPageId.Commands,
                OperatorComputerPageId.Modes,
                OperatorComputerPageId.Diagnostics,
                OperatorComputerPageId.Log,
                OperatorComputerPageId.Session,
            },
            pages.Select(static page => page.Id));
        Assert.Equal(
            new[] { "GUIDANCE", "INFO", "ALARMS", "COMMANDS", "MODES", "DIAGNOSTICS", "LOG", "SESSION" },
            pages.Select(static page => page.MenuLabel));
        Assert.Equal(pages.Count, pages.Select(static page => page.Id).Distinct().Count());
    }

    [Fact]
    public void SnapshotProjector_ProjectsRuntimeStatusInformationAlarmsAndStagesUnavailableHistoryOrFuturePages()
    {
        var source = new ControlRoomSnapshot(
            logicalStep: 42,
            runState: ControlRoomRunState.Running,
            totalMeasuredSignalCount: 12,
            invalidMeasuredSignalCount: 2,
            annunciatedAlarmCount: 3,
            unacknowledgedAlarmCount: 1,
            reactorScramActive: true,
            turbineTripActive: false,
            generatorTripActive: false);

        var snapshot = OperatorComputerSnapshotProjector.Project(source);

        Assert.Equal(42, snapshot.RuntimeStatus.LogicalStep);
        Assert.Equal(ControlRoomRunState.Running, snapshot.RuntimeStatus.RunState);
        Assert.Equal(2, snapshot.RuntimeStatus.InvalidMeasuredSignalCount);
        Assert.Equal(3, snapshot.RuntimeStatus.AnnunciatedAlarmCount);
        Assert.Equal(1, snapshot.RuntimeStatus.UnacknowledgedAlarmCount);
        Assert.True(snapshot.RuntimeStatus.AnyTripActive);
        Assert.NotNull(snapshot.Information);
        Assert.Equal(OperatorComputerPageContentState.Available, snapshot.Pages.Single(static page => page.Id == OperatorComputerPageId.Info).ContentState);
        Assert.Equal(OperatorComputerPageContentState.Unavailable, snapshot.Pages.Single(static page => page.Id == OperatorComputerPageId.Guidance).ContentState);
        Assert.Equal(OperatorComputerPageContentState.Unavailable, snapshot.Pages.Single(static page => page.Id == OperatorComputerPageId.Diagnostics).ContentState);
        Assert.Equal(OperatorComputerPageContentState.Available, snapshot.Pages.Single(static page => page.Id == OperatorComputerPageId.Alarms).ContentState);
        Assert.Equal(OperatorComputerPageContentState.Unavailable, snapshot.Pages.Single(static page => page.Id == OperatorComputerPageId.Log).ContentState);
        Assert.NotNull(snapshot.Alarms);
        Assert.Equal(OperatorComputerPageContentState.Available, snapshot.Pages.Single(static page => page.Id == OperatorComputerPageId.Commands).ContentState);
        Assert.NotNull(snapshot.Commands);
        Assert.All(snapshot.Pages.Where(static page => page.Id is OperatorComputerPageId.Modes or OperatorComputerPageId.Session),
            static page => Assert.Equal(OperatorComputerPageContentState.ShellOnly, page.ContentState));
    }

    [Fact]
    public void Snapshot_FreezesPageCollectionAndRejectsIncompleteOrDuplicatePageSets()
    {
        var runtime = new OperatorComputerRuntimeStatusSnapshot(0, ControlRoomRunState.Paused, 0, 0, 0, false);
        var projected = OperatorComputerSnapshotProjector.Project(ControlRoomSnapshot.ShellOnly);
        var mutableView = Assert.IsAssignableFrom<IList<OperatorComputerPageSnapshot>>(projected.Pages);

        Assert.Throws<NotSupportedException>(() => mutableView.RemoveAt(0));
        Assert.Throws<ArgumentException>(() => new OperatorComputerSnapshot(runtime, projected.Pages.Take(7)));
        Assert.Throws<ArgumentException>(() => new OperatorComputerSnapshot(runtime, projected.Pages.Take(7).Append(projected.Pages[0])));
        Assert.Throws<ArgumentException>(() => new OperatorComputerSnapshot(runtime, projected.Pages.Reverse()));
    }
}
