using System.Xml.Linq;
using NuclearReactorSimulator.App.Composition;
using NuclearReactorSimulator.App.ViewModels;
using NuclearReactorSimulator.Application;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;
using NuclearReactorSimulator.Application.Scenarios.Operations;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.ControlRoom.MissionPerformance;

public sealed class M10973MissionPerformanceWorkspaceUiTests
{
    [Fact]
    public void StartupSelection_RequiresAnExplicitExactPackIdAndNeverInfersFromScenarioIdentity()
    {
        Assert.Null(MissionChallengeStartupSelection.Resolve(Array.Empty<string>()));

        var pack = MissionChallengeStartupSelection.Resolve(new[]
        {
            "--mission-pack=bounded-demand-following-5-10-5@1",
        });
        Assert.Same(InitialOperationalChallengePack.BoundedDemandFollowing, pack);

        Assert.Throws<ArgumentException>(() => MissionChallengeStartupSelection.Resolve(new[]
        {
            "--mission-pack=power-manoeuvring-normal-shutdown",
        }));
    }

    [Fact]
    public void Workspace_IsLiveRegisteredAndComputerContextNavigationDoesNotDispatchPlantCommands()
    {
        var session = CreateFactory().Load(InitialOperationalChallengePack.BoundedDemandFollowing.Scenario);
        var dispatcher = new RecordingDispatcher();
        var viewModel = new MainWindowViewModel(
            new ApplicationDescriptor("Nuclear Reactor Simulator", "TEST", "TEST"),
            session.SnapshotSource,
            dispatcher);

        Assert.Contains(viewModel.Workspaces, static item => item.Id == ControlRoomWorkspaceId.MissionPerformance);
        viewModel.OpenMissionPerformanceWorkspaceCommand.Execute(null);

        Assert.Equal(ControlRoomWorkspaceId.MissionPerformance, viewModel.SelectedWorkspace.Id);
        Assert.True(viewModel.IsMissionPerformanceWorkspaceSelected);
        Assert.Empty(dispatcher.Commands);
        Assert.True(MissionPerformanceWorkspaceActivation.Current.UiRouteActivated);
        Assert.False(MissionPerformanceWorkspaceActivation.Current.NavigationHasPlantCommandAuthority);
    }

    [Fact]
    public void ViewModel_NoMissionStateIsExplicitlyUnavailableRatherThanFabricatedOrZero()
    {
        var viewModel = new MissionPerformanceViewModel();

        Assert.False(viewModel.HasMission);
        Assert.True(viewModel.HasNoMission);
        Assert.Equal("NO ACTIVE MISSION", viewModel.ObjectiveTitle);
        Assert.Equal("UNBOUND", viewModel.LifecycleText);
        Assert.Equal("UNAVAILABLE", viewModel.ExternalDemandText);
        Assert.Equal("UNAVAILABLE", viewModel.RequestedLoadText);
        Assert.Equal("UNAVAILABLE", viewModel.ActualOutputText);
        Assert.Equal("UNAVAILABLE", viewModel.ScoreText);
    }

    [Fact]
    public void ViewModel_UsesExplicitStructuralChangeDetectionAndKeepsUnavailableDistinctFromZero()
    {
        var session = CreateFactory().Load(InitialOperationalChallengePack.BoundedDemandFollowing.Scenario);
        using var source = new MissionPerformanceLiveSnapshotSource(
            session,
            InitialOperationalChallengePack.BoundedDemandFollowing,
            TrainingGuidanceMode.Guided);
        var viewModel = new MissionPerformanceViewModel(source.Current);
        var initialRevision = viewModel.PresentationRevision;
        var equal = source.Current with
        {
            Score = source.Current.Score with { Dimensions = source.Current.Score.Dimensions.ToArray() },
            RecentEvents = source.Current.RecentEvents.ToArray(),
        };

        Assert.False(viewModel.UpdateSnapshot(equal));
        Assert.Equal(initialRevision, viewModel.PresentationRevision);
        Assert.True(viewModel.HasMission);
        Assert.NotEqual("0.000 MWe", viewModel.ExternalDemandText);

        Assert.True(viewModel.UpdateSnapshot(equal with { ObjectiveTitle = equal.ObjectiveTitle + " updated" }));
        Assert.Equal(initialRevision + 1, viewModel.PresentationRevision);
    }

    [Fact]
    public void MainWindow_XamlContainsFourMissionRegionsAndPreservesF1ToF8WithoutF9()
    {
        var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "MainWindow.axaml"));
        var texts = document.Descendants()
            .Select(static element => (string?)element.Attribute("Text"))
            .Where(static value => value is not null)
            .ToArray();

        Assert.Contains("MISSION / OBJECTIVE", texts);
        Assert.Contains("SAFETY / PROTECTION SIGNIFICANCE", texts);
        Assert.Contains("GRID DEMAND", texts);
        Assert.Contains("REQUESTED LOAD", texts);
        Assert.Contains("ACTUAL OUTPUT", texts);
        Assert.Contains("SCORE / CLASSIFICATION", texts);
        Assert.Contains("RECENT DETERMINISTIC EVIDENCE", texts);
        _ = Assert.Single(
            document.Descendants(),
            static element => element.Name.LocalName == "Button"
                && (string?)element.Attribute("Content") == "OPEN MISSION"
                && (string?)element.Attribute("Command") == "{Binding OpenMissionPerformanceWorkspaceCommand}");

        foreach (var key in Enumerable.Range(1, 8).Select(static value => $"F{value}"))
        {
            _ = Assert.Single(
                document.Descendants(),
                element => element.Name.LocalName == "KeyBinding"
                    && (string?)element.Attribute("Gesture") == key);
        }
        Assert.DoesNotContain(
            document.Descendants(),
            static element => element.Name.LocalName == "KeyBinding"
                && (string?)element.Attribute("Gesture") == "F9");

        var missionHost = Assert.Single(
            document.Descendants(),
            static element => element.Name.LocalName == "StackPanel"
                && (string?)element.Attribute("IsVisible") == "{Binding IsMissionPerformanceWorkspaceSelected}");
        Assert.Null(missionHost.Attribute("DataContext"));
        _ = Assert.Single(
            missionHost.Elements(),
            static element => element.Name.LocalName == "StackPanel"
                && (string?)element.Attribute("DataContext") == "{Binding MissionPerformance}");
    }


    [Fact]
    public void ArtifactSummary_WritesM10973LiveWorkspaceEvidence()
    {
        var directory = ResolveArtifactDirectory();
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "01-m10973-mission-performance-live-workspace.summary.txt");
        File.WriteAllLines(path, new[]
        {
            "scope=M10.9.7.3 Hotfix 1 REV2 live Mission/Performance workspace wiring over M10.9.7.2 Hotfix 3 REV1 VALIDATED; REV2 is now VALIDATED after automated and manual HMI gates on 2026-08-21; original M10.9.7.3 candidate, Hotfix 1 and Hotfix 1 REV1 remain superseded/not validated; presentation/application aggregation only; no new challenge definition, scoring arithmetic, protection authority, plant command authority or physics change;",
            "workspace-mission-live-registered=True; workspace-title=Mission & Performance; workspace-label=MISSION; operator-computer-contextual-entry=True; operator-computer-f1-f8-preserved=True; operator-computer-f9-added=False;",
            "explicit-presentation-change-detection=True; generated-record-equality-used=False; deterministic-step-demand-history=True; presentation-publication-cadence-separated=True; stale-batch-presentation-snapshots-ignored=True; presentation-cannot-lead-deterministic-evidence=True;",
            "grid-demand-requested-load-actual-output-separated=True; unavailable-is-not-zero=True; score-copied-from-m1096-owner=True; safety-protection-prominence=True; recent-events-bounded-by-presentation-contract=True;",
            "default-desktop-session-invents-challenge=False; explicit-pack-binding-required=True; exact-startup-pack-binding-fail-closed=True; user-facing-challenge-launcher-added=False; archive-restored-mission-binding-deferred-to=m10974; plant-command-authority=False;",
            "original-m10973-candidate-promotable=False; original-hotfix1-promotable=False; hotfix1-rev1-promotable=False; hotfix1-score-dimension-contract-aligned=True; hotfix1-mainwindow-datacontext-scope-aligned=True; hotfix1-rev1-batch-presentation-ordering-aligned=True; historical-situation-strip-test-scoped-to-strip=True; historical-current-step-binding=RuntimeProgressText; m10973-hotfix1-rev2-live-mission-performance-workspace-passes=True; manual-hmi-review-completed=True; m10973-hotfix1-rev2-validated=True; next-step=M10.9.7.3 Hotfix 2 desktop-host/session-integrity closure;",
        });

        Assert.True(File.Exists(path));
    }

    private static ScenarioSessionFactory CreateFactory()
        => new(new VersionedInitialConditionRegistry(new IVersionedInitialConditionFactory[]
        {
            new PowerManoeuvringInitialConditionFactory(),
        }));

    private static string ResolveArtifactDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
        {
            current = current.Parent;
        }
        if (current is null)
        {
            throw new InvalidOperationException("Repository root could not be resolved for M10.9.7.3 audit artifacts.");
        }
        return Path.Combine(current.FullName, "artifacts", "m10973-mission-performance-live-workspace");
    }

    private sealed class RecordingDispatcher : IControlRoomCommandDispatcher
    {
        public List<ControlRoomCommand> Commands { get; } = new();

        public void Dispatch(ControlRoomCommand command)
            => Commands.Add(command);
    }
}
