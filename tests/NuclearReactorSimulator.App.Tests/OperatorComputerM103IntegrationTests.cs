using NuclearReactorSimulator.App.ViewModels;
using NuclearReactorSimulator.Application;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.ViewModels;

public sealed class OperatorComputerM103IntegrationTests
{
    [Fact]
    public void DesktopTerminal_ExposesReadOnlyAlarmAndLiveLogPagesWithoutRequiringFullRecorder()
    {
        var session = CreateSession();
        var tracker = CreateTracker(session);
        var viewModel = new MainWindowViewModel(
            ApplicationDescriptor.Current,
            session.SnapshotSource,
            session.CommandDispatcher,
            powerManoeuvringGuidance: DesktopIntegratedOperationsProgram.ProcedureGuidance,
            trainingTracker: tracker);

        viewModel.OperatorComputer.SelectPage(OperatorComputerPageId.Alarms);
        Assert.Equal(OperatorComputerPageContentState.Available, viewModel.OperatorComputer.SelectedPage.ContentState);
        Assert.Contains("CURRENT ANNUNCIATOR", viewModel.OperatorComputer.SelectedPageContentText);
        Assert.Contains("READ-ONLY WORKSTATION", viewModel.OperatorComputer.SelectedPageContentText);

        viewModel.OperatorComputer.SelectPage(OperatorComputerPageId.Log);
        Assert.Equal(OperatorComputerPageContentState.Available, viewModel.OperatorComputer.SelectedPage.ContentState);
        Assert.Contains("LIVE // M6.6", viewModel.OperatorComputer.SelectedPageContentText);
        Assert.Contains("SESSION // M9.1", viewModel.OperatorComputer.SelectedPageContentText);
        Assert.Contains("not attached", viewModel.OperatorComputer.SelectedPageContentText.ToLowerInvariant());
        Assert.Contains("INCIDENT // M9.2", viewModel.OperatorComputer.SelectedPageContentText);
    }

    [Fact]
    public void OptionalM91RecorderEvidence_AppearsInSessionLogAfterAcceptedOperatorAction()
    {
        var session = CreateSession();
        using var recorder = new ScenarioRecorder(session);
        var tracker = CreateTracker(session);
        var viewModel = new MainWindowViewModel(
            ApplicationDescriptor.Current,
            session.SnapshotSource,
            session.CommandDispatcher,
            powerManoeuvringGuidance: DesktopIntegratedOperationsProgram.ProcedureGuidance,
            trainingTracker: tracker,
            scenarioRecorder: recorder);

        session.CommandDispatcher.Dispatch(new NuclearReactorSimulator.Application.ControlRoom.ControlRoomCommand(
            NuclearReactorSimulator.Application.ControlRoom.ControlRoomCommandKind.GeneratorLoadLower,
            "generator",
            NuclearReactorSimulator.Application.ControlRoom.ControlRoomCommandTargetKind.Generator));
        session.CommandDispatcher.Dispatch(new NuclearReactorSimulator.Application.ControlRoom.ControlRoomCommand(
            NuclearReactorSimulator.Application.ControlRoom.ControlRoomCommandKind.SingleStep));

        viewModel.OperatorComputer.SelectPage(OperatorComputerPageId.Log);
        Assert.Contains("SESSION // M9.1", viewModel.OperatorComputer.SelectedPageContentText);
        Assert.Contains("OPERATORACTION", viewModel.OperatorComputer.SelectedPageContentText.Replace("_", string.Empty, StringComparison.Ordinal));
        Assert.DoesNotContain("not attached", viewModel.OperatorComputer.SelectedPageContentText.ToLowerInvariant());
    }


    [Fact]
    public void PublishedAlarmEvent_IsVisibleInTerminalLogOnTheSameSnapshotPublication()
    {
        var source = new TestSnapshotSource(ControlRoomSnapshot.ShellOnly);
        var viewModel = new MainWindowViewModel(ApplicationDescriptor.Current, source, new NoOpDispatcher());
        var alarm = new ControlRoomAlarmPresentationSnapshot(
            "alarm-a",
            "Alarm A",
            ControlRoomAlarmSeverity.Warning,
            ControlRoomAlarmAnnunciatorState.ActiveUnacknowledged,
            null,
            ConditionActive: true,
            IsLatched: true,
            IsAcknowledged: false,
            IsAnnunciated: true,
            IsFirstOut: false,
            ActivationSequence: 1,
            IsLatchedUntilReset: false);
        var alarmEvent = new ControlRoomAlarmEventPresentationSnapshot(1, 1, "alarm-a", "Alarm A", ControlRoomAlarmEventKind.Activated);
        source.Publish(new ControlRoomSnapshot(
            1,
            ControlRoomRunState.Paused,
            0,
            0,
            1,
            1,
            false,
            false,
            false,
            alarmEvents: new AlarmEventsPanelSnapshot(
                new[] { alarm },
                Array.Empty<ControlRoomFirstOutGroupPresentationSnapshot>(),
                new[] { alarmEvent })));

        viewModel.OperatorComputer.SelectPage(OperatorComputerPageId.Log);
        Assert.Contains("STEP 00000001", viewModel.OperatorComputer.SelectedPageContentText);
        Assert.Contains("alarm-a", viewModel.OperatorComputer.SelectedPageContentText);
    }

    private static ScenarioSession CreateSession()
        => new ScenarioSessionFactory(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new DesktopSustainedGenerationInitialConditionFactory(),
        })).Load(DesktopIntegratedOperationsProgram.Scenario);

    private static ScenarioTrainingTracker CreateTracker(ScenarioSession session)
        => new(
            session,
            DesktopIntegratedOperationsProgram.TrainingPlan,
            DesktopIntegratedOperationsProgram.CreateCheckpointEvaluator(),
            TrainingGuidanceMode.Guided);
    private sealed class TestSnapshotSource : IControlRoomSnapshotSource
    {
        public TestSnapshotSource(ControlRoomSnapshot current)
        {
            Current = current;
        }

        public ControlRoomSnapshot Current { get; private set; }

        public event EventHandler<ControlRoomSnapshotChangedEventArgs>? SnapshotChanged;

        public void Publish(ControlRoomSnapshot snapshot)
        {
            Current = snapshot;
            SnapshotChanged?.Invoke(this, new ControlRoomSnapshotChangedEventArgs(snapshot));
        }
    }

    private sealed class NoOpDispatcher : IControlRoomCommandDispatcher
    {
        public void Dispatch(ControlRoomCommand command)
        {
        }
    }

}
