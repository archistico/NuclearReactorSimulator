using System.Xml.Linq;
using NuclearReactorSimulator.App.ViewModels;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Application.Scenarios.Training;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.ViewModels;

public sealed class M10982Hotfix1Rev5ListRefreshStabilityTests
{
    [Fact]
    public void OperatorComputer_RuntimeAvailabilityRefreshPreservesDependencyListAndSelectionReferences()
    {
        var viewModel = new OperatorComputerViewModel(ProjectRuntimeSnapshot(0, ControlRoomRunState.Running));
        viewModel.SelectPage(OperatorComputerPageId.Commands);
        viewModel.SelectedCommand = viewModel.CommandEntries.Single(static command => command.Command.Kind == ControlRoomCommandKind.Pause);
        viewModel.SelectedCommandDependencyStep = viewModel.SelectedCommandDependencySteps.Last();

        var dependencySteps = viewModel.SelectedCommandDependencySteps;
        var selectedStep = viewModel.SelectedCommandDependencyStep;
        var listNotifications = 0;
        var selectionNotifications = 0;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(OperatorComputerViewModel.SelectedCommandDependencySteps))
            {
                listNotifications++;
            }
            if (args.PropertyName == nameof(OperatorComputerViewModel.SelectedCommandDependencyStep))
            {
                selectionNotifications++;
            }
        };

        viewModel.UpdateSnapshot(ProjectRuntimeSnapshot(1, ControlRoomRunState.Paused));

        Assert.Same(dependencySteps, viewModel.SelectedCommandDependencySteps);
        Assert.Same(selectedStep, viewModel.SelectedCommandDependencyStep);
        Assert.Equal(0, listNotifications);
        Assert.Equal(0, selectionNotifications);
    }

    [Fact]
    public void OperatorComputer_EquivalentCheckpointRefreshPreservesListAndSelectionReferences()
    {
        var checkpoints = new[]
        {
            new OperatorComputerSessionCheckpointSnapshot("checkpoint-1", 10, "fingerprint-1"),
            new OperatorComputerSessionCheckpointSnapshot("checkpoint-2", 20, "fingerprint-2"),
        };
        var viewModel = new OperatorComputerViewModel(WithSession(ProjectRuntimeSnapshot(20, ControlRoomRunState.Paused), checkpoints));
        viewModel.SelectedSessionCheckpoint = viewModel.SessionCheckpoints[1];

        var checkpointList = viewModel.SessionCheckpoints;
        var selectedCheckpoint = viewModel.SelectedSessionCheckpoint;
        var listNotifications = 0;
        var selectionNotifications = 0;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(OperatorComputerViewModel.SessionCheckpoints))
            {
                listNotifications++;
            }
            if (args.PropertyName == nameof(OperatorComputerViewModel.SelectedSessionCheckpoint))
            {
                selectionNotifications++;
            }
        };

        var equivalentNewInstances = checkpoints
            .Select(static checkpoint => new OperatorComputerSessionCheckpointSnapshot(
                checkpoint.CheckpointId,
                checkpoint.LogicalStep,
                checkpoint.SnapshotFingerprint))
            .ToArray();
        viewModel.UpdateSnapshot(WithSession(ProjectRuntimeSnapshot(21, ControlRoomRunState.Paused), equivalentNewInstances));

        Assert.Same(checkpointList, viewModel.SessionCheckpoints);
        Assert.Same(selectedCheckpoint, viewModel.SelectedSessionCheckpoint);
        Assert.Equal(0, listNotifications);
        Assert.Equal(0, selectionNotifications);
    }

    [Fact]
    public void MissionPerformance_ScalarRefreshDoesNotReplaceInteractiveTimelineOrUnchangedRows()
    {
        var initial = CreateMissionSnapshot(0);
        var viewModel = new MissionPerformanceViewModel(initial);
        var scoreDimensions = viewModel.ScoreDimensions;
        var recentEvents = viewModel.RecentEvents;
        var timeline = viewModel.Timeline;
        var scoreNotifications = 0;
        var eventNotifications = 0;
        var timelineNotifications = 0;
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MissionPerformanceViewModel.ScoreDimensions))
            {
                scoreNotifications++;
            }
            if (args.PropertyName == nameof(MissionPerformanceViewModel.RecentEvents))
            {
                eventNotifications++;
            }
            if (args.PropertyName == nameof(MissionPerformanceViewModel.Timeline))
            {
                timelineNotifications++;
            }
        };

        var scalarOnly = initial with
        {
            LogicalStep = initial.LogicalStep + 1,
            ElapsedLogicalSteps = initial.ElapsedLogicalSteps is { } elapsed ? elapsed + 1 : null,
            Score = initial.Score with { Dimensions = initial.Score.Dimensions.ToArray() },
            RecentEvents = initial.RecentEvents.ToArray(),
            LifecycleSpine = initial.LifecycleSpine.ToArray(),
            RecentOperationalEvidence = initial.RecentOperationalEvidence.ToArray(),
            Timeline = initial.Timeline.ToArray(),
        };

        Assert.True(viewModel.UpdateSnapshot(scalarOnly));
        Assert.Same(scoreDimensions, viewModel.ScoreDimensions);
        Assert.Same(recentEvents, viewModel.RecentEvents);
        Assert.Same(timeline, viewModel.Timeline);
        Assert.NotEmpty(timeline);
        Assert.Same(timeline[0], viewModel.Timeline[0]);
        Assert.Equal(0, scoreNotifications);
        Assert.Equal(0, eventNotifications);
        Assert.Equal(0, timelineNotifications);
    }

    [Fact]
    public void AllCollectionBackedControls_AreExplicitlyInventoriedForRefreshStability()
    {
        var appSource = Path.Combine(ResolveRepositoryRoot(), "src", "NuclearReactorSimulator.App");
        var documents = Directory.EnumerateFiles(appSource, "*.axaml", SearchOption.AllDirectories)
            .Select(XDocument.Load)
            .ToArray();
        var itemSources = documents
            .SelectMany(static document => document.Descendants())
            .Where(static element => element.Name.LocalName == "ListBox")
            .Select(static element => (string?)element.Attribute("ItemsSource") ?? string.Empty)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "{Binding CommandEntries}",
                "{Binding SelectedCommandDependencySteps}",
                "{Binding SessionCheckpoints}",
                "{Binding Workspaces}",
            },
            itemSources);

        var itemsControlSources = documents
            .SelectMany(static document => document.Descendants())
            .Where(static element => element.Name.LocalName == "ItemsControl")
            .Select(static element => (string?)element.Attribute("ItemsSource") ?? string.Empty)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                "{Binding AlarmEvents.Alarms}",
                "{Binding AlarmEvents.FirstOutGroups}",
                "{Binding Branches}",
                "{Binding Electrical.Generators}",
                "{Binding OperationalHistory.Events}",
                "{Binding OperationalHistory.TrendSeries}",
                "{Binding PrimaryCircuit.Loops}",
                "{Binding PrimaryCircuit.SteamDrums}",
                "{Binding PrimaryCircuit.Valves}",
                "{Binding Pumps}",
                "{Binding ReactorCore.Rods}",
                "{Binding ReactorCore.Zones}",
                "{Binding ScoreDimensions}",
                "{Binding Timeline}",
                "{Binding TurbineSecondary.AdmissionTrains}",
                "{Binding TurbineSecondary.Condensers}",
                "{Binding TurbineSecondary.FeedwaterTrains}",
                "{Binding TurbineSecondary.Rotors}",
                "{Binding TurbineSecondary.StageGroups}",
                "{Binding TurbineSecondary.SteamLines}",
            },
            itemsControlSources);

        var interactiveItemsControls = documents
            .SelectMany(static document => document.Descendants())
            .Where(static element => element.Name.LocalName == "ItemsControl")
            .Where(static element => element.Descendants().Any(descendant => descendant.Name.LocalName is "Button" or "ListBox" or "ComboBox" or "TextBox"))
            .Select(static element => (string?)element.Attribute("ItemsSource") ?? string.Empty)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "{Binding Timeline}" }, interactiveItemsControls);
        Assert.Equal(4, itemSources.Length);
        Assert.Equal(20, itemsControlSources.Length);
        Assert.Equal(24, itemSources.Length + itemsControlSources.Length);
        Assert.Equal(19, itemsControlSources.Length - interactiveItemsControls.Length);

        var programmaticSelectorLabels = documents
            .SelectMany(static document => document.Descendants())
            .Where(static element => element.Name.LocalName == "ControlRoomSelector")
            .Select(static element => (string?)element.Attribute("Label") ?? string.Empty)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                "ADMISSION TRAIN",
                "ALARM TARGET",
                "GENERATOR TARGET",
                "PUMP TARGET",
                "ROD TARGET",
            },
            programmaticSelectorLabels);

        var programmaticCollectionControlFiles = Directory.EnumerateFiles(appSource, "*.cs", SearchOption.AllDirectories)
            .Where(static path =>
            {
                var source = File.ReadAllText(path);
                return source.Contains("new ComboBox", StringComparison.Ordinal)
                    || source.Contains("new ListBox", StringComparison.Ordinal)
                    || source.Contains("new ItemsControl", StringComparison.Ordinal)
                    || source.Contains(".ItemsSource =", StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(appSource, path).Replace('\\', '/'))
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "Controls/ControlRoomSelector.cs" }, programmaticCollectionControlFiles);

        var selectorSourcePath = Path.Combine(appSource, "Controls", "ControlRoomSelector.cs");
        var selectorSource = File.ReadAllText(selectorSourcePath);
        Assert.Equal(1, CountOccurrences(selectorSource, "new ComboBox"));
        Assert.Contains("var optionsChanged = !HasEquivalentOptions(_options, options);", selectorSource, StringComparison.Ordinal);
        Assert.Contains("if (optionsChanged)", selectorSource, StringComparison.Ordinal);
        Assert.Contains("_selector.ItemsSource = _options;", selectorSource, StringComparison.Ordinal);
        Assert.DoesNotContain("_selector.ItemsSource = options;", selectorSource, StringComparison.Ordinal);
    }

    [Fact]
    public void ArtifactSummary_WritesRev5InteractiveListStabilityEvidence()
    {
        var directory = ResolveArtifactDirectory();
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "02-m10982-rev5-interactive-list-stability.summary.txt");
        File.WriteAllLines(path, new[]
        {
            "scope=M10.9.8.2 Hotfix 1 REV5 interactive-list refresh stability over REV4; production mission @2, Simulation physics, challenge/scoring/protection ownership, archive schema and fingerprint algorithm unchanged;",
            "xaml-collection-controls=24; selectable-listbox-count=4; read-only-itemscontrol-count=19; interactive-itemscontrol-count=1; programmatic-target-selector-instances=5; programmatic-combobox-implementation-count=1;",
            "workspaces-static-catalog=True; command-entries-semantic-refresh-stable=True; dependency-chain-cached=True; dependency-selection-stable=True; session-checkpoints-semantic-refresh-stable=True; session-checkpoint-selection-stable=True; target-selector-options-semantic-refresh-stable=True; mission-timeline-semantic-refresh-stable=True;",
            "f4-dependency-chain-hover-flicker-regression-covered=True; target-selector-items-source-reset-regression-covered=True; future-collection-control-inventory-fail-closed=True; production-list-command-authority-change=False; m10982-rev5-interactive-list-stability-passes=True;",
        });

        Assert.True(File.Exists(path));
    }

    private static MissionPerformanceSnapshot CreateMissionSnapshot(long logicalStep)
        => new(
            PackExactId: "list-stability-pack@1",
            ChallengeExactId: "list-stability-challenge@1",
            ScenarioId: "list-stability-scenario",
            ObjectiveId: "list-stability-objective",
            ObjectiveTitle: "List stability objective",
            ObjectiveDescription: "Test-only immutable presentation snapshot.",
            LifecycleState: ChallengeLifecycleState.Active,
            LogicalStep: logicalStep,
            ActivatedLogicalStep: 0,
            ElapsedLogicalSteps: logicalStep,
            TerminalLogicalStep: null,
            TargetWindowStartLogicalStep: 4000,
            TargetWindowEndLogicalStep: 8000,
            HardFailureDeadlineLogicalStep: null,
            Demand: new MissionPerformanceDemandSnapshot(
                ExternalDemandAvailable: true,
                ExternalDemandProfileExactId: "list-stability-demand@1",
                ExternalDemandMegawatts: 5,
                RequestedGeneratorLoadMegawatts: 5,
                ActualElectricalOutputMegawatts: 5,
                DemandOutputErrorMegawatts: 0,
                NextScheduledDemandChangeLogicalStep: 4000,
                NextScheduledDemandMegawatts: 10),
            Score: MissionPerformanceScoreSnapshot.Unavailable,
            RecentEvents: new[]
            {
                new MissionPerformanceEventSnapshot(
                    logicalStep,
                    MissionPerformanceEventKind.Objective,
                    "list-stability-event",
                    "Stable event row",
                    SourceSequence: 1),
            },
            LifecycleSpine: Array.Empty<MissionPerformanceTimelineEntrySnapshot>(),
            RecentOperationalEvidence: Array.Empty<MissionPerformanceTimelineEntrySnapshot>(),
            Timeline: new[]
            {
                new MissionPerformanceTimelineEntrySnapshot(
                    logicalStep,
                    MissionPerformanceTimelineEntryKind.OperatorAction,
                    "list-stability-action",
                    "Stable interactive timeline row",
                    SourceSequence: 1,
                    IsCritical: false,
                    DrillDownTarget: new MissionPerformanceDrillDownTarget(
                        ControlRoomWorkspaceId.OperatorComputer,
                        "OPEN COMMANDS",
                        OperatorComputerPageId.Commands)),
            },
            AssistanceMode: TrainingGuidanceMode.Guided,
            PlantControlAuthorityAvailable: false,
            RequestedControlAuthority: null,
            EffectiveControlAuthority: null,
            ControlAuthorityHealth: null,
            ControlAuthorityDegradationReason: null);


    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var start = 0;
        while ((start = source.IndexOf(value, start, StringComparison.Ordinal)) >= 0)
        {
            count++;
            start += value.Length;
        }
        return count;
    }

    private static string ResolveArtifactDirectory()
        => Path.Combine(ResolveRepositoryRoot(), "artifacts", "m1098-healthy-assistance-authority-matrix");

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
        {
            current = current.Parent;
        }
        return current?.FullName
            ?? throw new InvalidOperationException("Repository root could not be resolved for M10.9.8.2 REV5 list-stability artifacts.");
    }

    private static OperatorComputerSnapshot ProjectRuntimeSnapshot(long logicalStep, ControlRoomRunState runState)
        => OperatorComputerSnapshotProjector.Project(new ControlRoomSnapshot(
            logicalStep,
            runState,
            totalMeasuredSignalCount: 0,
            invalidMeasuredSignalCount: 0,
            annunciatedAlarmCount: 0,
            unacknowledgedAlarmCount: 0,
            reactorScramActive: false,
            turbineTripActive: false,
            generatorTripActive: false));

    private static OperatorComputerSnapshot WithSession(
        OperatorComputerSnapshot snapshot,
        IReadOnlyList<OperatorComputerSessionCheckpointSnapshot> checkpoints)
        => new(
            snapshot.RuntimeStatus,
            snapshot.Pages,
            snapshot.Information,
            snapshot.Guidance,
            snapshot.Diagnostics,
            snapshot.Alarms,
            snapshot.Log,
            snapshot.Commands,
            snapshot.Modes,
            new OperatorComputerSessionSnapshot(
                recorderActive: true,
                scenarioId: "list-stability-test",
                scenarioTitle: "List stability test",
                initialConditionText: "test@1",
                logicalStep: snapshot.RuntimeStatus.LogicalStep,
                recordedFrameCount: 0,
                checkpoints: checkpoints),
            snapshot.PlantMimic);
}
