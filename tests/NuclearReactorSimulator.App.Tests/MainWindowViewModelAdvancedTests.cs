using NuclearReactorSimulator.App.ViewModels;
using NuclearReactorSimulator.Application;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.ViewModels;

public sealed class MainWindowViewModelAdvancedTests
{
    [Fact]
    public void WorkspaceSelection_ExposesExactlyTheSelectedWorkspace()
    {
        var viewModel = CreateViewModel(ControlRoomSnapshot.ShellOnly, new RecordingDispatcher());

        foreach (var workspace in viewModel.Workspaces)
        {
            viewModel.SelectedWorkspace = workspace;

            Assert.Equal(workspace.Title, viewModel.SelectedWorkspaceTitle);
            Assert.Equal(workspace.Description, viewModel.SelectedWorkspaceDescription);
            Assert.Equal(workspace.Id == ControlRoomWorkspaceId.Overview, viewModel.IsOverviewWorkspaceSelected);
            Assert.Equal(workspace.Id == ControlRoomWorkspaceId.Reactor, viewModel.IsReactorWorkspaceSelected);
            Assert.Equal(workspace.Id == ControlRoomWorkspaceId.PrimaryCircuit, viewModel.IsPrimaryWorkspaceSelected);
            Assert.Equal(workspace.Id == ControlRoomWorkspaceId.TurbineSecondary, viewModel.IsTurbineWorkspaceSelected);
            Assert.Equal(workspace.Id == ControlRoomWorkspaceId.Electrical, viewModel.IsElectricalWorkspaceSelected);
            Assert.Equal(workspace.Id == ControlRoomWorkspaceId.AlarmsEvents, viewModel.IsAlarmsWorkspaceSelected);
            Assert.Equal(workspace.Id == ControlRoomWorkspaceId.OperatorComputer, viewModel.IsOperatorComputerWorkspaceSelected);
        }
    }

    [Fact]
    public void OperatorComputerKeyboardCommands_OpenComputerWorkspaceAndSelectNamedPageWithoutDispatchingPlantCommands()
    {
        var dispatcher = new RecordingDispatcher();
        var viewModel = CreateViewModel(ControlRoomSnapshot.ShellOnly, dispatcher);

        viewModel.OpenOperatorComputerSessionPageCommand.Execute(null);

        Assert.Equal(ControlRoomWorkspaceId.OperatorComputer, viewModel.SelectedWorkspace.Id);
        Assert.Equal(OperatorComputerPageId.Session, viewModel.OperatorComputer.SelectedPage.Id);
        Assert.Empty(dispatcher.Commands);
    }

    [Fact]
    public void SnapshotPublication_RefreshesHeadlineStateAndRaisesCorePresentationNotifications()
    {
        var source = new InMemoryControlRoomSnapshotSource(CreateSnapshot(logicalStep: 1, totalSignals: 10));
        var viewModel = CreateViewModel(source, new RecordingDispatcher());
        var notifications = new HashSet<string>(StringComparer.Ordinal);
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is not null)
            {
                notifications.Add(args.PropertyName);
            }
        };

        source.Publish(CreateSnapshot(
            logicalStep: 12,
            runState: ControlRoomRunState.Running,
            totalSignals: 10,
            invalidSignals: 2,
            reactorScramActive: true,
            annunciatedAlarmCount: 3,
            unacknowledgedAlarmCount: 2));

        Assert.Equal(12, viewModel.LogicalStep);
        Assert.Equal("RUNNING", viewModel.RuntimeState);
        Assert.Equal("8/10 measured signals valid", viewModel.SignalHealthText);
        Assert.True(viewModel.ProtectionSummary.Contains("REACTOR SCRAM", StringComparison.Ordinal));
        Assert.Equal(ControlRoomVisualState.Warning, viewModel.UnacknowledgedAlarmVisualState);
        Assert.Contains(nameof(viewModel.LogicalStep), notifications);
        Assert.Contains(nameof(viewModel.RuntimeState), notifications);
        Assert.Contains(nameof(viewModel.ReactorCore), notifications);
        Assert.Contains(nameof(viewModel.OperationalHistory), notifications);
        Assert.Contains(nameof(viewModel.XenonAvailabilityText), notifications);
    }

    [Fact]
    public void DetachRuntimeSubscriptions_DetachesViewModelFromTheDiscardedRuntimeSnapshotSource()
    {
        var source = new InMemoryControlRoomSnapshotSource(CreateSnapshot(logicalStep: 1));
        var viewModel = CreateViewModel(source, new RecordingDispatcher());

        viewModel.DetachRuntimeSubscriptions();
        source.Publish(CreateSnapshot(logicalStep: 99, runState: ControlRoomRunState.Running));

        Assert.Equal(1, viewModel.LogicalStep);
        Assert.False(viewModel.IsRuntimeRunning);
    }

    [Fact]
    public void NoCanonicalTargets_FailsClosedWithoutDispatchingTargetedCommands()
    {
        var dispatcher = new RecordingDispatcher();
        var viewModel = CreateViewModel(CreateSnapshot(), dispatcher);

        viewModel.RodWithdrawCommand.Execute(null);
        Assert.True(viewModel.CommandStatus.Contains("no canonical rod/group target", StringComparison.OrdinalIgnoreCase));
        viewModel.PumpStartCommand.Execute(null);
        Assert.True(viewModel.CommandStatus.Contains("no commandable canonical", StringComparison.OrdinalIgnoreCase));
        viewModel.GeneratorBreakerCloseCommand.Execute(null);
        Assert.True(viewModel.CommandStatus.Contains("no canonical generator/breaker", StringComparison.OrdinalIgnoreCase));
        viewModel.AlarmAcknowledgeCommand.Execute(null);
        Assert.True(viewModel.CommandStatus.Contains("no canonical alarm target", StringComparison.OrdinalIgnoreCase));

        Assert.Empty(dispatcher.Commands);
    }

    [Fact]
    public void CanonicalTargetCommands_PreserveTargetIdsAndKindsAcrossUiBoundary()
    {
        var dispatcher = new RecordingDispatcher();
        var viewModel = CreateViewModel(CreateSnapshot(
            rodTargetCount: 1,
            pumpCount: 1,
            generatorCount: 1,
            breakerClosed: true,
            alarmMode: AlarmMode.ActiveUnacknowledged), dispatcher);

        viewModel.RodInsertCommand.Execute(null);
        viewModel.PumpStopCommand.Execute(null);
        viewModel.TurbineSpeedRaiseCommand.Execute(null);
        viewModel.GeneratorLoadLowerCommand.Execute(null);
        viewModel.GeneratorBreakerOpenCommand.Execute(null);
        viewModel.AlarmAcknowledgeCommand.Execute(null);

        Assert.Collection(
            dispatcher.Commands,
            command => AssertCommand(command, ControlRoomCommandKind.ControlRodInsert, "rod-target-1", ControlRoomCommandTargetKind.ControlRodGroup),
            command => AssertCommand(command, ControlRoomCommandKind.MainCirculationPumpStop, "pump-1", ControlRoomCommandTargetKind.Pump),
            command => AssertCommand(command, ControlRoomCommandKind.TurbineSpeedRaise, "rotor-1", ControlRoomCommandTargetKind.TurbineRotor),
            command => AssertCommand(command, ControlRoomCommandKind.GeneratorLoadLower, "generator-1", ControlRoomCommandTargetKind.Generator),
            command => AssertCommand(command, ControlRoomCommandKind.GeneratorBreakerOpen, "breaker-1", ControlRoomCommandTargetKind.Breaker),
            command => AssertCommand(command, ControlRoomCommandKind.AlarmAcknowledge, "alarm-1", ControlRoomCommandTargetKind.Alarm));
    }

    [Fact]
    public void HostAndProtectionCommands_RemainUntargetedTypedIntents()
    {
        var dispatcher = new RecordingDispatcher();
        var viewModel = CreateViewModel(CreateSnapshot(), dispatcher);

        viewModel.RunCommand.Execute(null);
        viewModel.PauseCommand.Execute(null);
        viewModel.SingleStepCommand.Execute(null);
        viewModel.ReactorScramCommand.Execute(null);
        viewModel.ProtectionResetCommand.Execute(null);
        viewModel.TurbineTripCommand.Execute(null);
        viewModel.GeneratorTripCommand.Execute(null);
        viewModel.AlarmAcknowledgeAllCommand.Execute(null);
        viewModel.AlarmResetAllCommand.Execute(null);

        var expected = new[]
        {
            ControlRoomCommandKind.Run,
            ControlRoomCommandKind.Pause,
            ControlRoomCommandKind.SingleStep,
            ControlRoomCommandKind.ReactorScram,
            ControlRoomCommandKind.ProtectionReset,
            ControlRoomCommandKind.TurbineTrip,
            ControlRoomCommandKind.GeneratorTrip,
            ControlRoomCommandKind.AlarmAcknowledgeAll,
            ControlRoomCommandKind.AlarmResetAll,
        };
        Assert.Equal(expected, dispatcher.Commands.Select(static command => command.Kind));
        Assert.All(dispatcher.Commands, static command =>
        {
            Assert.Null(command.TargetId);
            Assert.Null(command.TargetKind);
        });
    }

    [Fact]
    public void DispatcherRejection_IsSurfacedAsBlockedStatusWithoutUiStateMutation()
    {
        var viewModel = CreateViewModel(CreateSnapshot(), new RejectingDispatcher("not permitted by scenario"));

        viewModel.ReactorScramCommand.Execute(null);

        Assert.True(viewModel.CommandStatus.Contains("Command blocked by the loaded scenario", StringComparison.Ordinal));
        Assert.True(viewModel.CommandStatus.Contains("not permitted by scenario", StringComparison.Ordinal));
    }

    [Fact]
    public void SelectionIndices_ClampWhenRodPumpGeneratorAndAlarmCollectionsShrink()
    {
        var source = new InMemoryControlRoomSnapshotSource(CreateSnapshot(
            rodTargetCount: 2,
            pumpCount: 2,
            generatorCount: 2,
            alarmCount: 2,
            alarmMode: AlarmMode.ActiveUnacknowledged));
        var viewModel = CreateViewModel(source, new RecordingDispatcher());
        viewModel.SelectedRodIndex = 1;
        viewModel.SelectedPumpIndex = 1;
        viewModel.SelectedGeneratorIndex = 1;
        viewModel.SelectedAlarmIndex = 1;

        source.Publish(CreateSnapshot(
            rodTargetCount: 1,
            pumpCount: 1,
            generatorCount: 1,
            alarmCount: 1,
            alarmMode: AlarmMode.ActiveUnacknowledged));

        Assert.Equal(0, viewModel.SelectedRodIndex);
        Assert.Equal("rod-target-1", viewModel.SelectedRodId);
        Assert.Equal(0, viewModel.SelectedPumpIndex);
        Assert.Equal("pump-1", viewModel.SelectedPumpId);
        Assert.Equal(0, viewModel.SelectedGeneratorIndex);
        Assert.Equal("generator-1", viewModel.SelectedGeneratorId);
        Assert.Equal(0, viewModel.SelectedAlarmIndex);
        Assert.Equal("alarm-1", viewModel.SelectedAlarmId);
    }

    [Theory]
    [InlineData(AlarmMode.ActiveUnacknowledged, ControlRoomVisualState.Warning, ControlRoomVisualState.Unavailable)]
    [InlineData(AlarmMode.ActiveAcknowledged, ControlRoomVisualState.Unavailable, ControlRoomVisualState.Unavailable)]
    [InlineData(AlarmMode.ReturnedAcknowledgedResettable, ControlRoomVisualState.Unavailable, ControlRoomVisualState.Normal)]
    public void AlarmCommandStates_FollowCanonicalAnnunciatorSemantics(
        AlarmMode mode,
        ControlRoomVisualState expectedAcknowledge,
        ControlRoomVisualState expectedReset)
    {
        var viewModel = CreateViewModel(CreateSnapshot(alarmMode: mode), new RecordingDispatcher());

        Assert.Equal(expectedAcknowledge, viewModel.AlarmAcknowledgeCommandState);
        Assert.Equal(expectedReset, viewModel.AlarmResetCommandState);
    }

    [Fact]
    public void TripAndInterlockStates_DisableOnlyTheAffectedNormalControls()
    {
        var viewModel = CreateViewModel(CreateSnapshot(
            rodTargetCount: 1,
            generatorCount: 1,
            breakerClosed: true,
            rodWithdrawalInhibited: true,
            turbineTripActive: true,
            generatorTripActive: true), new RecordingDispatcher());

        Assert.Equal(ControlRoomVisualState.Normal, viewModel.RodCommandState);
        Assert.Equal(ControlRoomVisualState.Unavailable, viewModel.RodWithdrawCommandState);
        Assert.Equal(ControlRoomVisualState.Trip, viewModel.TurbineTripCommandState);
        Assert.Equal(ControlRoomVisualState.Trip, viewModel.GeneratorTripCommandState);
        Assert.Equal(ControlRoomVisualState.Unavailable, viewModel.TurbineSpeedCommandState);
        Assert.Equal(ControlRoomVisualState.Unavailable, viewModel.GeneratorLoadCommandState);
        Assert.Equal(ControlRoomVisualState.Unavailable, viewModel.BreakerCloseCommandState);
    }

    [Fact]
    public void RealValidatedSeeds_ExposeExpectedUiCommandSurfaces()
    {
        var cold = new ColdShutdownInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);
        var sync = new GridSynchronizationInitialConditionFactory()
            .CreateRuntimeEngine()
            .CreatePresentationSnapshot(ControlRoomRunState.Paused);

        var coldViewModel = CreateViewModel(cold, new RecordingDispatcher());
        var syncViewModel = CreateViewModel(sync, new RecordingDispatcher());

        Assert.NotEqual(ControlRoomVisualState.Unavailable, coldViewModel.PumpCommandState);
        Assert.NotEqual(ControlRoomVisualState.Unavailable, coldViewModel.RodCommandState);
        Assert.Equal(ControlRoomVisualState.Normal, syncViewModel.GeneratorSelectionState);
        Assert.Equal(ControlRoomVisualState.Normal, syncViewModel.BreakerCloseCommandState);
        Assert.Equal(ControlRoomVisualState.Unavailable, syncViewModel.GeneratorLoadCommandState);
    }

    private static MainWindowViewModel CreateViewModel(ControlRoomSnapshot snapshot, IControlRoomCommandDispatcher dispatcher)
        => CreateViewModel(new InMemoryControlRoomSnapshotSource(snapshot), dispatcher);

    private static MainWindowViewModel CreateViewModel(IControlRoomSnapshotSource source, IControlRoomCommandDispatcher dispatcher)
        => new(new ApplicationDescriptor("Nuclear Reactor Simulator", "TEST", "TEST"), source, dispatcher);

    private static ControlRoomSnapshot CreateSnapshot(
        long logicalStep = 1,
        ControlRoomRunState runState = ControlRoomRunState.Paused,
        int totalSignals = 0,
        int invalidSignals = 0,
        int annunciatedAlarmCount = 0,
        int unacknowledgedAlarmCount = 0,
        bool reactorScramActive = false,
        bool turbineTripActive = false,
        bool generatorTripActive = false,
        bool rodWithdrawalInhibited = false,
        int rodTargetCount = 0,
        int pumpCount = 0,
        int generatorCount = 0,
        bool breakerClosed = false,
        int alarmCount = 0,
        AlarmMode alarmMode = AlarmMode.None)
    {
        var normal = new ControlRoomValueSnapshot("0", string.Empty, 0d, ControlRoomVisualState.Normal);
        var rodTargets = Enumerable.Range(1, rodTargetCount)
            .Select(index => new ReactorRodTargetPresentationSnapshot($"rod-target-{index}", ControlRoomCommandTargetKind.ControlRodGroup))
            .ToArray();
        var reactor = new ReactorCorePanelSnapshot(
            normal,
            normal,
            normal,
            normal,
            normal,
            normal,
            ControlRoomValueSnapshot.Unavailable("pcm"),
            Array.Empty<ReactorCoreZonePresentationSnapshot>(),
            Array.Empty<ReactorRodPresentationSnapshot>(),
            rodTargets,
            reactorScramActive,
            rodWithdrawalInhibited);

        var pumps = Enumerable.Range(1, pumpCount)
            .Select(index => new PrimaryCircuitPumpPresentationSnapshot(
                $"pump-{index}",
                "loop-1",
                false,
                normal,
                normal,
                normal,
                true))
            .ToArray();
        var loops = pumpCount == 0
            ? Array.Empty<PrimaryCircuitLoopPresentationSnapshot>()
            : new[]
            {
                new PrimaryCircuitLoopPresentationSnapshot(
                    "loop-1",
                    normal,
                    normal,
                    normal,
                    normal,
                    "FORWARD",
                    pumps,
                    Array.Empty<PrimaryCircuitBranchPresentationSnapshot>()),
            };
        var primary = new PrimaryCircuitPanelSnapshot(
            loops,
            Array.Empty<PrimaryCircuitSteamDrumPresentationSnapshot>(),
            Array.Empty<PrimaryCircuitValvePresentationSnapshot>(),
            normal,
            normal,
            normal);

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
                true,
                breakerClosed,
                false,
                false))
            .ToArray();
        var electrical = new ElectricalPanelSnapshot(ElectricalGridPresentationSnapshot.Unavailable, generators, normal, generatorTripActive);
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

        var requestedAlarmCount = alarmMode == AlarmMode.None ? alarmCount : Math.Max(1, alarmCount);
        var alarms = Enumerable.Range(1, requestedAlarmCount)
            .Select(index => CreateAlarm($"alarm-{index}", alarmMode == AlarmMode.None ? AlarmMode.ActiveUnacknowledged : alarmMode))
            .ToArray();
        var alarmPanel = new AlarmEventsPanelSnapshot(
            alarms,
            Array.Empty<ControlRoomFirstOutGroupPresentationSnapshot>(),
            Array.Empty<ControlRoomAlarmEventPresentationSnapshot>());
        var effectiveAnnunciated = annunciatedAlarmCount == 0 && alarms.Length > 0 ? alarmPanel.AnnunciatedCount : annunciatedAlarmCount;
        var effectiveUnacknowledged = unacknowledgedAlarmCount == 0 && alarms.Length > 0 ? alarmPanel.UnacknowledgedCount : unacknowledgedAlarmCount;

        return new ControlRoomSnapshot(
            logicalStep,
            runState,
            totalSignals,
            invalidSignals,
            effectiveAnnunciated,
            effectiveUnacknowledged,
            reactorScramActive,
            turbineTripActive,
            generatorTripActive,
            reactor,
            primary,
            turbine,
            electrical,
            alarmPanel);
    }

    private static ControlRoomAlarmPresentationSnapshot CreateAlarm(string alarmId, AlarmMode mode)
        => mode switch
        {
            AlarmMode.ActiveUnacknowledged => new ControlRoomAlarmPresentationSnapshot(
                alarmId, "Test alarm", ControlRoomAlarmSeverity.Warning, ControlRoomAlarmAnnunciatorState.ActiveUnacknowledged,
                null, true, true, false, true, false, 1, true),
            AlarmMode.ActiveAcknowledged => new ControlRoomAlarmPresentationSnapshot(
                alarmId, "Test alarm", ControlRoomAlarmSeverity.Warning, ControlRoomAlarmAnnunciatorState.ActiveAcknowledged,
                null, true, true, true, true, false, 1, true),
            AlarmMode.ReturnedAcknowledgedResettable => new ControlRoomAlarmPresentationSnapshot(
                alarmId, "Test alarm", ControlRoomAlarmSeverity.Warning, ControlRoomAlarmAnnunciatorState.ReturnedAcknowledged,
                null, false, true, true, true, false, 1, true),
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };

    private static void AssertCommand(
        ControlRoomCommand command,
        ControlRoomCommandKind kind,
        string targetId,
        ControlRoomCommandTargetKind targetKind)
    {
        Assert.Equal(kind, command.Kind);
        Assert.Equal(targetId, command.TargetId);
        Assert.Equal(targetKind, command.TargetKind);
    }

    public enum AlarmMode
    {
        None = 0,
        ActiveUnacknowledged = 1,
        ActiveAcknowledged = 2,
        ReturnedAcknowledgedResettable = 3,
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

    private sealed class RejectingDispatcher(string message) : IControlRoomCommandDispatcher
    {
        public void Dispatch(ControlRoomCommand command)
            => throw new InvalidOperationException(message);
    }
}
