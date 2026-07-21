using System.Xml.Linq;
using NuclearReactorSimulator.App.ViewModels;
using NuclearReactorSimulator.Application;
using NuclearReactorSimulator.Application.ControlRoom;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.ViewModels;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void GeneratorSelectionState_RemainsNormal_WhenGeneratorTripIsActive()
    {
        var viewModel = CreateViewModel(CreateSnapshot(generatorTripActive: true));

        Assert.Equal(ControlRoomVisualState.Normal, viewModel.GeneratorSelectionState);
        Assert.Equal(ControlRoomVisualState.Trip, viewModel.GeneratorTripCommandState);
    }

    [Fact]
    public void GeneratorSelectionState_IsUnavailable_WhenNoGeneratorExists()
    {
        var viewModel = CreateViewModel(CreateSnapshot(generatorCount: 0));

        Assert.Equal(ControlRoomVisualState.Unavailable, viewModel.GeneratorSelectionState);
    }

    [Fact]
    public void GeneratorTargetSelector_BindsToNeutralSelectionState()
    {
        var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "MainWindow.axaml"));
        var selector = Assert.Single(
            document.Descendants(),
            static element => element.Name.LocalName == "ControlRoomSelector"
                && (string?)element.Attribute("Label") == "GENERATOR TARGET");

        Assert.Equal("{Binding GeneratorSelectionState}", (string?)selector.Attribute("State"));
    }

    [Fact]
    public void TurbineSpeedCommandState_IsUnavailable_WhenTurbineTripIsActive()
    {
        var viewModel = CreateViewModel(CreateSnapshot(turbineTripActive: true));

        Assert.Equal(ControlRoomVisualState.Unavailable, viewModel.TurbineSpeedCommandState);
        Assert.Equal(ControlRoomVisualState.Unavailable, viewModel.GeneratorLoadCommandState);
    }

    [Fact]
    public void BreakerCloseCommandState_IsUnavailable_OutsideSynchronizationWindow()
    {
        var viewModel = CreateViewModel(CreateSnapshot(synchronizationConditionsSatisfied: false, breakerClosed: false));

        Assert.Equal(ControlRoomVisualState.Unavailable, viewModel.BreakerCloseCommandState);
    }

    [Fact]
    public void SelectedGeneratorIndex_Clamps_WhenPublishedGeneratorListShrinks()
    {
        var source = new InMemoryControlRoomSnapshotSource(CreateSnapshot(generatorCount: 2));
        var viewModel = CreateViewModel(source, new RecordingDispatcher());
        viewModel.SelectedGeneratorIndex = 1;

        source.Publish(CreateSnapshot(generatorCount: 1));

        Assert.Equal(0, viewModel.SelectedGeneratorIndex);
        Assert.Equal("generator-1", viewModel.SelectedGeneratorId);
    }

    [Fact]
    public void GeneratorLoadRaise_DispatchesTypedCommandForSelectedGenerator()
    {
        var dispatcher = new RecordingDispatcher();
        var viewModel = CreateViewModel(
            new InMemoryControlRoomSnapshotSource(CreateSnapshot(generatorCount: 2, breakerClosed: true)),
            dispatcher);
        viewModel.SelectedGeneratorIndex = 1;

        viewModel.GeneratorLoadRaiseCommand.Execute(null);

        var command = Assert.Single(dispatcher.Commands);
        Assert.Equal(ControlRoomCommandKind.GeneratorLoadRaise, command.Kind);
        Assert.Equal("generator-2", command.TargetId);
        Assert.Equal(ControlRoomCommandTargetKind.Generator, command.TargetKind);
    }

    private static MainWindowViewModel CreateViewModel(ControlRoomSnapshot snapshot)
        => CreateViewModel(new InMemoryControlRoomSnapshotSource(snapshot), new RecordingDispatcher());

    private static MainWindowViewModel CreateViewModel(
        IControlRoomSnapshotSource source,
        IControlRoomCommandDispatcher dispatcher)
        => new(
            new ApplicationDescriptor("Nuclear Reactor Simulator", "TEST", "TEST"),
            source,
            dispatcher);

    private static ControlRoomSnapshot CreateSnapshot(
        bool generatorTripActive = false,
        bool turbineTripActive = false,
        bool synchronizationConditionsSatisfied = true,
        bool breakerClosed = true,
        int generatorCount = 1)
    {
        var normal = new ControlRoomValueSnapshot("0", string.Empty, 0d, ControlRoomVisualState.Normal);
        var generators = Enumerable.Range(1, generatorCount)
            .Select(index => new GeneratorPresentationSnapshot(
                $"generator-{index}",
                $"rotor-{index}",
                $"breaker-{index}",
                normal,
                normal,
                normal,
                normal,
                normal,
                normal,
                normal,
                synchronizationConditionsSatisfied,
                breakerClosed,
                false,
                false))
            .ToArray();
        var electrical = new ElectricalPanelSnapshot(
            ElectricalGridPresentationSnapshot.Unavailable,
            generators,
            normal,
            generatorTripActive);
        var turbine = new TurbineSecondaryPanelSnapshot(
            Array.Empty<MainSteamLinePresentationSnapshot>(),
            Array.Empty<TurbineAdmissionTrainPresentationSnapshot>(),
            Array.Empty<TurbineRotorPresentationSnapshot>(),
            Array.Empty<TurbineStageGroupPresentationSnapshot>(),
            Array.Empty<CondenserPresentationSnapshot>(),
            Array.Empty<FeedwaterTrainPresentationSnapshot>(),
            normal,
            normal,
            normal,
            turbineTripActive);

        return new ControlRoomSnapshot(
            logicalStep: 1,
            runState: ControlRoomRunState.Paused,
            totalMeasuredSignalCount: 0,
            invalidMeasuredSignalCount: 0,
            annunciatedAlarmCount: 0,
            unacknowledgedAlarmCount: 0,
            reactorScramActive: false,
            turbineTripActive: turbineTripActive,
            generatorTripActive: generatorTripActive,
            turbineSecondary: turbine,
            electrical: electrical);
    }

    private sealed class RecordingDispatcher : IControlRoomCommandDispatcher
    {
        public List<ControlRoomCommand> Commands { get; } = new();

        public void Dispatch(ControlRoomCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            Commands.Add(command);
        }
    }
}
