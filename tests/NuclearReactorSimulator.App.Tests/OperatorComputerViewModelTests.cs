using NuclearReactorSimulator.App.ViewModels;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.ViewModels;

public sealed class OperatorComputerViewModelTests
{
    [Fact]
    public void DefaultSelection_IsGuidanceAndFixedCommandsSelectEveryNamedPage()
    {
        var viewModel = CreateViewModel(ControlRoomSnapshot.ShellOnly);

        Assert.Equal(OperatorComputerPageId.Guidance, viewModel.SelectedPage.Id);

        var commands = new (System.Windows.Input.ICommand Command, OperatorComputerPageId Expected)[]
        {
            (viewModel.SelectGuidancePageCommand, OperatorComputerPageId.Guidance),
            (viewModel.SelectInfoPageCommand, OperatorComputerPageId.Info),
            (viewModel.SelectAlarmsPageCommand, OperatorComputerPageId.Alarms),
            (viewModel.SelectCommandsPageCommand, OperatorComputerPageId.Commands),
            (viewModel.SelectModesPageCommand, OperatorComputerPageId.Modes),
            (viewModel.SelectDiagnosticsPageCommand, OperatorComputerPageId.Diagnostics),
            (viewModel.SelectLogPageCommand, OperatorComputerPageId.Log),
            (viewModel.SelectSessionPageCommand, OperatorComputerPageId.Session),
        };

        foreach (var entry in commands)
        {
            entry.Command.Execute(null);
            Assert.Equal(entry.Expected, viewModel.SelectedPage.Id);
        }
    }

    [Fact]
    public void PageSelection_ExposesExactlyOneIntegratedNavigationState()
    {
        var viewModel = CreateViewModel(ControlRoomSnapshot.ShellOnly);
        var pageIds = new[]
        {
            OperatorComputerPageId.Guidance,
            OperatorComputerPageId.Info,
            OperatorComputerPageId.Alarms,
            OperatorComputerPageId.Commands,
            OperatorComputerPageId.Modes,
            OperatorComputerPageId.Diagnostics,
            OperatorComputerPageId.Log,
            OperatorComputerPageId.Session,
        };

        foreach (var pageId in pageIds)
        {
            viewModel.SelectPage(pageId);
            var selectedStates = new[]
            {
                viewModel.IsGuidancePageSelected,
                viewModel.IsInfoPageSelected,
                viewModel.IsAlarmsPageSelected,
                viewModel.IsCommandsPageSelected,
                viewModel.IsModesPageSelected,
                viewModel.IsDiagnosticsPageSelected,
                viewModel.IsLogPageSelected,
                viewModel.IsSessionPageSelected,
            };

            Assert.Single(selectedStates, static selected => selected);
        }
    }

    [Fact]
    public void SnapshotUpdate_PreservesPresentationOnlyPageSelectionAndRefreshesStatusLine()
    {
        var viewModel = CreateViewModel(ControlRoomSnapshot.ShellOnly);
        viewModel.SelectPage(OperatorComputerPageId.Log);

        viewModel.UpdateSnapshot(OperatorComputerSnapshotProjector.Project(new ControlRoomSnapshot(
            logicalStep: 123,
            runState: ControlRoomRunState.Running,
            totalMeasuredSignalCount: 10,
            invalidMeasuredSignalCount: 2,
            annunciatedAlarmCount: 4,
            unacknowledgedAlarmCount: 3,
            reactorScramActive: false,
            turbineTripActive: true,
            generatorTripActive: false)));

        Assert.Equal(OperatorComputerPageId.Log, viewModel.SelectedPage.Id);
        Assert.Equal("RUNNING", viewModel.RuntimeStateText);
        Assert.Equal("00000123", viewModel.LogicalStepText);
        Assert.Contains("PAGE LOG", viewModel.StatusLineText);
        Assert.Contains("ALARMS 4/3 UNACK", viewModel.StatusLineText);
        Assert.Contains("SIGNALS 2 INVALID", viewModel.StatusLineText);
        Assert.Contains("PROTECTION ACTIVE", viewModel.StatusLineText);
    }

    [Fact]
    public void TerminalPages_DistinguishAvailableUnavailableAndFutureContentExplicitly()
    {
        var viewModel = CreateViewModel(ControlRoomSnapshot.ShellOnly);

        Assert.Equal(OperatorComputerPageContentState.Unavailable, viewModel.Pages.Single(static page => page.Id == OperatorComputerPageId.Guidance).ContentState);
        Assert.Equal(OperatorComputerPageContentState.Available, viewModel.Pages.Single(static page => page.Id == OperatorComputerPageId.Info).ContentState);
        Assert.Equal(OperatorComputerPageContentState.Unavailable, viewModel.Pages.Single(static page => page.Id == OperatorComputerPageId.Diagnostics).ContentState);
        Assert.Equal(OperatorComputerPageContentState.Available, viewModel.Pages.Single(static page => page.Id == OperatorComputerPageId.Alarms).ContentState);
        Assert.Equal(OperatorComputerPageContentState.Available, viewModel.Pages.Single(static page => page.Id == OperatorComputerPageId.Commands).ContentState);
        Assert.Equal(OperatorComputerPageContentState.Unavailable, viewModel.Pages.Single(static page => page.Id == OperatorComputerPageId.Log).ContentState);

        viewModel.SelectPage(OperatorComputerPageId.Info);
        Assert.Contains("[REACTOR]", viewModel.SelectedPageContentText);
        Assert.Contains("[UNAVAILABLE]", viewModel.SelectedPageContentText);

        viewModel.SelectPage(OperatorComputerPageId.Alarms);
        Assert.Contains("CURRENT ANNUNCIATOR", viewModel.SelectedPageContentText);
        Assert.Contains("READ-ONLY WORKSTATION", viewModel.SelectedPageContentText);
    }

    private static OperatorComputerViewModel CreateViewModel(ControlRoomSnapshot snapshot)
        => new(OperatorComputerSnapshotProjector.Project(snapshot));
}
