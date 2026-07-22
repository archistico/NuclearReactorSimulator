using NuclearReactorSimulator.App.ViewModels;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.ViewModels;

public sealed class OperatorComputerM104CommandConsoleTests
{
    [Fact]
    public void ExecuteSelectedCommand_DispatchesExactTypedIntentThroughCanonicalBoundary()
    {
        var dispatcher = new RecordingDispatcher();
        var viewModel = CreatePausedViewModel(dispatcher);
        viewModel.SelectPage(OperatorComputerPageId.Commands);
        viewModel.SelectedCommand = viewModel.CommandEntries.Single(static command => command.Command.Kind == ControlRoomCommandKind.Run);

        viewModel.ExecuteSelectedCommandCommand.Execute(null);

        var dispatched = Assert.Single(dispatcher.Commands);
        Assert.Equal(ControlRoomCommandKind.Run, dispatched.Kind);
        Assert.Null(dispatched.TargetId);
        Assert.Contains("DISPATCHED", viewModel.CommandConsoleStatus);
    }

    [Fact]
    public void BlockedPresentationCommand_DoesNotDispatch()
    {
        var dispatcher = new RecordingDispatcher();
        var viewModel = CreatePausedViewModel(dispatcher);
        viewModel.SelectPage(OperatorComputerPageId.Commands);
        viewModel.SelectedCommand = viewModel.CommandEntries.Single(static command => command.Command.Kind == ControlRoomCommandKind.Pause);

        viewModel.ExecuteSelectedCommandCommand.Execute(null);

        Assert.Empty(dispatcher.Commands);
        Assert.Contains("NOT DISPATCHED", viewModel.CommandConsoleStatus);
        Assert.Contains("already paused", viewModel.CommandConsoleStatus);
    }

    [Fact]
    public void RuntimeRejection_RemainsAuthoritativeAndIsShownWithoutDirectStateMutation()
    {
        var dispatcher = new RecordingDispatcher("scenario command denied");
        var viewModel = CreatePausedViewModel(dispatcher);
        viewModel.SelectPage(OperatorComputerPageId.Commands);
        viewModel.SelectedCommand = viewModel.CommandEntries.Single(static command => command.Command.Kind == ControlRoomCommandKind.Run);

        viewModel.ExecuteSelectedCommandCommand.Execute(null);

        Assert.Single(dispatcher.Commands);
        Assert.Contains("BLOCKED BY RUNTIME/SCENARIO", viewModel.CommandConsoleStatus);
        Assert.Contains("scenario command denied", viewModel.CommandConsoleStatus);
    }

    [Fact]
    public void SnapshotRefresh_PreservesStableCommandSelectionByEntryId()
    {
        var dispatcher = new RecordingDispatcher();
        var viewModel = CreatePausedViewModel(dispatcher);
        viewModel.SelectPage(OperatorComputerPageId.Commands);
        viewModel.SelectedCommand = viewModel.CommandEntries.Single(static command => command.Command.Kind == ControlRoomCommandKind.SingleStep);
        Assert.NotNull(viewModel.SelectedCommand);
        var selectedId = viewModel.SelectedCommand!.EntryId;

        viewModel.UpdateSnapshot(OperatorComputerSnapshotProjector.Project(new ControlRoomSnapshot(
            logicalStep: 1,
            runState: ControlRoomRunState.Paused,
            totalMeasuredSignalCount: 0,
            invalidMeasuredSignalCount: 0,
            annunciatedAlarmCount: 0,
            unacknowledgedAlarmCount: 0,
            reactorScramActive: false,
            turbineTripActive: false,
            generatorTripActive: false)));

        Assert.Equal(selectedId, viewModel.SelectedCommand?.EntryId);
        Assert.True(viewModel.IsCommandsPageSelected);
    }

    private static OperatorComputerViewModel CreatePausedViewModel(IControlRoomCommandDispatcher dispatcher)
    {
        var snapshot = new ControlRoomSnapshot(
            logicalStep: 0,
            runState: ControlRoomRunState.Paused,
            totalMeasuredSignalCount: 0,
            invalidMeasuredSignalCount: 0,
            annunciatedAlarmCount: 0,
            unacknowledgedAlarmCount: 0,
            reactorScramActive: false,
            turbineTripActive: false,
            generatorTripActive: false);
        return new OperatorComputerViewModel(OperatorComputerSnapshotProjector.Project(snapshot), dispatcher);
    }

    private sealed class RecordingDispatcher : IControlRoomCommandDispatcher
    {
        private readonly string? _failure;

        public RecordingDispatcher(string? failure = null)
        {
            _failure = failure;
        }

        public List<ControlRoomCommand> Commands { get; } = new();

        public void Dispatch(ControlRoomCommand command)
        {
            Commands.Add(command);
            if (_failure is not null)
            {
                throw new InvalidOperationException(_failure);
            }
        }
    }
}
