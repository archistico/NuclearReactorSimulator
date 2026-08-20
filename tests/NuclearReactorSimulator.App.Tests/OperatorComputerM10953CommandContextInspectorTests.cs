using NuclearReactorSimulator.App.ViewModels;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.ViewModels;

public sealed class OperatorComputerM10953CommandContextInspectorTests
{
    [Fact]
    public void SelectedCommand_ProjectsAuthoredConsequenceDependencyAndCanonicalMimicFocusWithoutDispatch()
    {
        var dispatcher = new RecordingDispatcher();
        var baseSnapshot = OperatorComputerSnapshotProjector.Project(new ControlRoomSnapshot(
            logicalStep: 0,
            runState: ControlRoomRunState.Running,
            totalMeasuredSignalCount: 0,
            invalidMeasuredSignalCount: 0,
            annunciatedAlarmCount: 0,
            unacknowledgedAlarmCount: 0,
            reactorScramActive: false,
            turbineTripActive: false,
            generatorTripActive: false));
        var command = new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            "generator-1",
            ControlRoomCommandTargetKind.Generator);
        var commandSnapshot = new OperatorComputerCommandSnapshot(
            "electrical-generator-load-raise",
            OperatorComputerCommandGroup.Electrical,
            "GENERATOR LOAD RAISE",
            command,
            OperatorComputerCommandAvailability.Available,
            "REQUEST 5 MWe");
        var snapshot = new OperatorComputerSnapshot(
            baseSnapshot.RuntimeStatus,
            baseSnapshot.Pages,
            baseSnapshot.Information,
            baseSnapshot.Guidance,
            baseSnapshot.Diagnostics,
            baseSnapshot.Alarms,
            baseSnapshot.Log,
            new OperatorComputerCommandConsoleSnapshot(new[] { commandSnapshot }),
            baseSnapshot.Modes,
            baseSnapshot.Session,
            baseSnapshot.PlantMimic);
        var viewModel = new OperatorComputerViewModel(snapshot, dispatcher);

        viewModel.SelectPage(OperatorComputerPageId.Commands);
        viewModel.SelectedCommand = commandSnapshot;

        Assert.True(viewModel.CurrentConsequence.HasAuthoredMap);
        Assert.True(viewModel.CurrentDependencyChain.HasAuthoredChain);
        Assert.Contains("DIRECT EFFECT", viewModel.SelectedCommandContextSummaryText, StringComparison.Ordinal);
        Assert.Contains("EXPECTED INFLUENCE", viewModel.SelectedCommandContextSummaryText, StringComparison.Ordinal);
        Assert.Contains("WHAT TO MONITOR", viewModel.SelectedCommandContextSummaryText, StringComparison.Ordinal);
        Assert.NotEmpty(viewModel.SelectedCommandDependencySteps);
        Assert.NotNull(viewModel.CommandContextPlantMimic);
        Assert.NotNull(viewModel.SelectedCommandSchematicElementId);
        Assert.Empty(dispatcher.Commands);
    }

    [Fact]
    public void DependencyStepSelection_ChangesPresentationFocusOnly()
    {
        var dispatcher = new RecordingDispatcher();
        var snapshot = OperatorComputerSnapshotProjector.Project(new ControlRoomSnapshot(
            logicalStep: 0,
            runState: ControlRoomRunState.Paused,
            totalMeasuredSignalCount: 0,
            invalidMeasuredSignalCount: 0,
            annunciatedAlarmCount: 0,
            unacknowledgedAlarmCount: 0,
            reactorScramActive: false,
            turbineTripActive: false,
            generatorTripActive: false));
        var viewModel = new OperatorComputerViewModel(snapshot, dispatcher);
        viewModel.SelectPage(OperatorComputerPageId.Commands);
        var command = viewModel.CommandEntries.First();
        viewModel.SelectedCommand = command;
        var last = viewModel.SelectedCommandDependencySteps.LastOrDefault();

        viewModel.SelectedCommandDependencyStep = last;

        Assert.Equal(last, viewModel.SelectedCommandDependencyStep);
        Assert.Empty(dispatcher.Commands);
        Assert.True(viewModel.IsCommandsPageSelected);
    }


    [Fact]
    public void NonGraphicalDependencyStep_ClearsSchematicHighlightWithoutDispatch()
    {
        var dispatcher = new RecordingDispatcher();
        var baseSnapshot = OperatorComputerSnapshotProjector.Project(new ControlRoomSnapshot(
            logicalStep: 0,
            runState: ControlRoomRunState.Running,
            totalMeasuredSignalCount: 0,
            invalidMeasuredSignalCount: 0,
            annunciatedAlarmCount: 0,
            unacknowledgedAlarmCount: 0,
            reactorScramActive: false,
            turbineTripActive: false,
            generatorTripActive: false));
        var command = new ControlRoomCommand(
            ControlRoomCommandKind.GeneratorLoadRaise,
            "generator-1",
            ControlRoomCommandTargetKind.Generator);
        var commandSnapshot = new OperatorComputerCommandSnapshot(
            "electrical-generator-load-raise",
            OperatorComputerCommandGroup.Electrical,
            "GENERATOR LOAD RAISE",
            command,
            OperatorComputerCommandAvailability.Available,
            "REQUEST 5 MWe");
        var snapshot = new OperatorComputerSnapshot(
            baseSnapshot.RuntimeStatus,
            baseSnapshot.Pages,
            baseSnapshot.Information,
            baseSnapshot.Guidance,
            baseSnapshot.Diagnostics,
            baseSnapshot.Alarms,
            baseSnapshot.Log,
            new OperatorComputerCommandConsoleSnapshot(new[] { commandSnapshot }),
            baseSnapshot.Modes,
            baseSnapshot.Session,
            baseSnapshot.PlantMimic);
        var viewModel = new OperatorComputerViewModel(snapshot, dispatcher);
        viewModel.SelectPage(OperatorComputerPageId.Commands);
        viewModel.SelectedCommand = commandSnapshot;
        var nonGraphical = viewModel.SelectedCommandDependencySteps.First(step =>
            step.Reference is null
            || (step.Reference.Kind != OperatorComputerCommandConsequenceReferenceKind.PlantMimicElement
                && step.Reference.Kind != OperatorComputerCommandConsequenceReferenceKind.PlantMimicConnection));

        viewModel.SelectedCommandDependencyStep = nonGraphical;

        Assert.Null(viewModel.SelectedCommandSchematicElementId);
        Assert.Contains("has no canonical whole-plant mimic element/connection", viewModel.SelectedCommandSchematicFocusText, StringComparison.Ordinal);
        Assert.Empty(dispatcher.Commands);
    }

    private sealed class RecordingDispatcher : IControlRoomCommandDispatcher
    {
        public List<ControlRoomCommand> Commands { get; } = new();

        public void Dispatch(ControlRoomCommand command) => Commands.Add(command);
    }
}
