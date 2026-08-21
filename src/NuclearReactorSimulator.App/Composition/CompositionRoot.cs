using NuclearReactorSimulator.App.ViewModels;
using NuclearReactorSimulator.Application;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;
using NuclearReactorSimulator.Application.Scenarios.Criticality;
using NuclearReactorSimulator.Application.Scenarios.Faults.SecondaryTransients;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Startup;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Application.Scenarios.Xenon;
using NuclearReactorSimulator.Infrastructure.Scenarios.Recording;

namespace NuclearReactorSimulator.App.Composition;

/// <summary>The only place responsible for constructing the application object graph.</summary>
internal static class CompositionRoot
{
    public static ApplicationRoot Create(bool enableSessionRecording = false)
    {
        var descriptor = ApplicationDescriptor.Current;
        var sessionFactory = CreateSessionFactory();
        var session = sessionFactory.Load(DesktopIntegratedOperationsProductionProgram.Scenario);
        var recorder = enableSessionRecording ? new ScenarioRecorder(session) : null;
        var trainingTracker = CreateDesktopTrainingTracker(session);
        var archiveSerializer = new JsonScenarioSessionArchiveSerializer();
        var replayRunner = new ScenarioFullReplayRunner(sessionFactory);
        var sessionWorkspace = new OperatorComputerSessionWorkspaceController(
            session,
            recorder,
            replayRunner,
            archiveSerializer);
        var mainWindowViewModel = CreateMainWindowViewModel(
            descriptor,
            session,
            trainingTracker,
            recorder,
            sessionWorkspace,
            missionPerformanceSource: null);

        return new ApplicationRoot(mainWindowViewModel, session.Coordinator);
    }

    /// <summary>
    /// Explicit M10.9.7.3 challenge composition path. Selecting a pack is an external session decision; the MISSION UI
    /// never invents or infers one from an ambiguous scenario identity.
    /// </summary>
    public static ApplicationRoot CreateMissionChallenge(
        OperationalChallengePackDefinition missionPack,
        bool enableSessionRecording = false)
    {
        ArgumentNullException.ThrowIfNull(missionPack);
        var descriptor = ApplicationDescriptor.Current;
        var sessionFactory = CreateSessionFactory();
        var session = sessionFactory.Load(missionPack.Scenario);
        var recorder = enableSessionRecording ? new ScenarioRecorder(session) : null;
        var missionPerformanceSource = new MissionPerformanceLiveSnapshotSource(
            session,
            missionPack,
            TrainingGuidanceMode.Guided,
            recorder);
        var archiveSerializer = new JsonScenarioSessionArchiveSerializer();
        var replayRunner = new ScenarioFullReplayRunner(sessionFactory);
        var sessionWorkspace = new OperatorComputerSessionWorkspaceController(
            session,
            recorder,
            replayRunner,
            archiveSerializer);
        var mainWindowViewModel = new MainWindowViewModel(
            descriptor,
            session.SnapshotSource,
            session.CommandDispatcher,
            scenarioRecorder: recorder,
            plantControlAuthorityDispatcher: session.PlantControlAuthority,
            sessionWorkspace: sessionWorkspace,
            missionPerformanceSource: missionPerformanceSource);
        mainWindowViewModel.SelectedWorkspace = mainWindowViewModel.Workspaces.Single(
            static workspace => workspace.Id == ControlRoomWorkspaceId.MissionPerformance);

        return new ApplicationRoot(mainWindowViewModel, session.Coordinator);
    }

    public static ApplicationRoot CreateFromSessionArchive(string content, string? checkpointId = null)
    {
        var descriptor = ApplicationDescriptor.Current;
        var sessionFactory = CreateSessionFactory();
        var archiveSerializer = new JsonScenarioSessionArchiveSerializer();
        var archive = archiveSerializer.Deserialize(content);
        var replayRunner = new ScenarioFullReplayRunner(sessionFactory);
        ScenarioTrainingTracker? trainingTracker = null;

        void AttachTraining(ScenarioSession replaySession)
        {
            if (DesktopIntegratedOperationsProductionProgram.IsDesktopTrainingScenario(replaySession.Scenario.ScenarioId))
            {
                trainingTracker = CreateDesktopTrainingTracker(replaySession);
            }
        }

        var replay = checkpointId is null
            ? replayRunner.ReplayAndVerify(archive, AttachTraining)
            : replayRunner.SeekAndVerify(archive, checkpointId, AttachTraining);
        var recorder = new ScenarioRecorder(replay.Session, replay.ReplayedRecording);
        var sessionWorkspace = new OperatorComputerSessionWorkspaceController(
            replay.Session,
            recorder,
            replayRunner,
            archiveSerializer);
        var mainWindowViewModel = CreateMainWindowViewModel(
            descriptor,
            replay.Session,
            trainingTracker,
            recorder,
            sessionWorkspace,
            missionPerformanceSource: null);

        return new ApplicationRoot(mainWindowViewModel, replay.Session.Coordinator);
    }

    private static ScenarioSessionFactory CreateSessionFactory()
        => new(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new ColdShutdownInitialConditionFactory(),
            new FirstCriticalityInitialConditionFactory(),
            new HeatUpTurbineStartupInitialConditionFactory(),
            new GridSynchronizationInitialConditionFactory(),
            new PowerManoeuvringInitialConditionFactory(),
            new DesktopIntegratedOperationsInitialConditionFactory(),
            new DesktopSustainedGenerationInitialConditionFactory(),
            new DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory(),
            new DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory(),
            new GridSynchronizationSustainedInitialConditionFactory(),
            new GridSynchronizationCorrectedInitialConditionFactory(),
            new SecondaryTransientInitialConditionFactory(),
            new XenonRestartInitialConditionFactory(),
            new LowPowerXenonInitialConditionFactory(),
        }));

    private static ScenarioTrainingTracker CreateDesktopTrainingTracker(ScenarioSession session)
        => new(
            session,
            DesktopIntegratedOperationsProductionProgram.ResolveTrainingPlan(session.Scenario.ScenarioId),
            DesktopIntegratedOperationsProductionProgram.CreateCheckpointEvaluator(),
            TrainingGuidanceMode.Guided);

    private static MainWindowViewModel CreateMainWindowViewModel(
        ApplicationDescriptor descriptor,
        ScenarioSession session,
        ScenarioTrainingTracker? trainingTracker,
        ScenarioRecorder? recorder,
        OperatorComputerSessionWorkspaceController sessionWorkspace,
        IMissionPerformanceSnapshotSource? missionPerformanceSource)
    {
        var isDesktopTraining = DesktopIntegratedOperationsProductionProgram.IsDesktopTrainingScenario(
            session.Scenario.ScenarioId);
        return new MainWindowViewModel(
            descriptor,
            session.SnapshotSource,
            session.CommandDispatcher,
            powerManoeuvringGuidance: isDesktopTraining ? DesktopIntegratedOperationsProductionProgram.ProcedureGuidance : null,
            trainingTracker: trainingTracker,
            scenarioRecorder: recorder,
            plantControlAuthorityDispatcher: session.PlantControlAuthority,
            sessionWorkspace: sessionWorkspace,
            missionPerformanceSource: missionPerformanceSource);
    }
}

internal sealed record ApplicationRoot(MainWindowViewModel MainWindowViewModel, ControlRoomRuntimeCoordinator RuntimeCoordinator);
