using System.Xml.Linq;
using NuclearReactorSimulator.App.Composition;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.ControlRoom.MissionPerformance;

public sealed class M10974MissionPerformanceDrillDownUiTests
{
    [Fact]
    public void OperatorActionTimelineDrillDown_SelectsComputerCommandsWithoutChangingPlantState()
    {
        var root = CompositionRoot.CreateMissionChallenge(
            InitialOperationalChallengePack.BoundedDemandFollowing,
            enableSessionRecording: true);
        try
        {
            var viewModel = root.MainWindowViewModel;
            var before = ControlRoomSnapshotFingerprint.Compute(root.RuntimeCoordinator.Current);

            viewModel.GeneratorLoadRaiseCommand.Execute(null);
            var row = Assert.Single(
                viewModel.MissionPerformance.Timeline,
                static item => item.KindText == "OPERATORACTION");
            Assert.True(row.HasDrillDown);
            Assert.Equal("COMMAND CONTEXT", row.DrillDownText);

            var afterOperatorAction = ControlRoomSnapshotFingerprint.Compute(root.RuntimeCoordinator.Current);
            row.DrillDownCommand.Execute(null);
            var afterNavigation = ControlRoomSnapshotFingerprint.Compute(root.RuntimeCoordinator.Current);

            Assert.Equal(before, afterOperatorAction);
            Assert.Equal(afterOperatorAction, afterNavigation);
            Assert.Equal(ControlRoomWorkspaceId.OperatorComputer, viewModel.SelectedWorkspace.Id);
            Assert.Equal(OperatorComputerPageId.Commands, viewModel.OperatorComputer.SelectedPage.Id);
        }
        finally
        {
            root.MainWindowViewModel.DetachRuntimeSubscriptions();
        }
    }

    [Fact]
    public void MainWindow_ExposesDeterministicTimelineRetentionAndPresentationOnlyDrillDown()
    {
        var path = ResolveRepoFile("src", "NuclearReactorSimulator.App", "Views", "MainWindow.axaml");
        var document = XDocument.Load(path);
        var text = File.ReadAllText(path);

        Assert.Contains("DETERMINISTIC TIMELINE / DRILL-DOWN", text, StringComparison.Ordinal);
        Assert.Contains("Lifecycle spine is protected", text, StringComparison.Ordinal);
        Assert.Contains("{Binding TimelineRetentionText}", text, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding Timeline}\"", text, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding DrillDownCommand}\"", text, StringComparison.Ordinal);
        Assert.Contains("does not dispatch a plant command", text, StringComparison.Ordinal);
        Assert.NotNull(document.Root);
        Assert.DoesNotContain(
            document.Descendants(),
            static element => element.Name.LocalName == "KeyBinding"
                && (string?)element.Attribute("Gesture") == "F9");
    }

    private static string ResolveRepoFile(params string[] segments)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(new[] { current.FullName }.Concat(segments).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Repository root could not be resolved from the test output directory.");
    }
}
