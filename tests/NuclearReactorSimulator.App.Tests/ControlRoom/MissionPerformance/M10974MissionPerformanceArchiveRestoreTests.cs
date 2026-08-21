using NuclearReactorSimulator.App.Composition;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;
using Xunit;

namespace NuclearReactorSimulator.App.Tests.ControlRoom.MissionPerformance;

public sealed class M10974MissionPerformanceArchiveRestoreTests
{
    [Fact]
    public void ExplicitExactPackBinding_RestoresMissionTimelineAcrossFullArchiveReplay()
    {
        var pack = InitialOperationalChallengePack.BoundedDemandFollowing;
        var root = CompositionRoot.CreateMissionChallenge(pack, enableSessionRecording: true);
        var viewModel = root.MainWindowViewModel;

        viewModel.RunCommand.Execute(null);
        _ = root.RuntimeCoordinator.AdvanceRunning(stepCount: 4, publicationStride: 1);
        viewModel.PauseCommand.Execute(null);

        var expected = viewModel.MissionPerformance.Timeline
            .Select(static row => $"{row.StepText}|{row.KindText}|{row.SourceText}|{row.DetailText}|{row.DrillDownText}")
            .ToArray();
        var archive = viewModel.OperatorComputer.ExportSessionArchive();
        viewModel.DetachRuntimeSubscriptions();

        var restored = CompositionRoot.CreateFromSessionArchive(archive, missionPack: pack);
        try
        {
            Assert.True(restored.MainWindowViewModel.MissionPerformance.HasMission);
            Assert.Equal(pack.ExactId, restored.MainWindowViewModel.MissionPerformancePackExactId);
            Assert.Equal(
                expected,
                restored.MainWindowViewModel.MissionPerformance.Timeline
                    .Select(static row => $"{row.StepText}|{row.KindText}|{row.SourceText}|{row.DetailText}|{row.DrillDownText}")
                    .ToArray());

            restored.MainWindowViewModel.RunCommand.Execute(null);
            _ = restored.RuntimeCoordinator.AdvanceRunning(stepCount: 1, publicationStride: 1);
            restored.MainWindowViewModel.PauseCommand.Execute(null);
            Assert.Equal(
                string.Create(System.Globalization.CultureInfo.InvariantCulture, $"STEP {restored.RuntimeCoordinator.Current.LogicalStep}"),
                restored.MainWindowViewModel.MissionPerformance.LogicalStepText);

            var continuedRows = restored.MainWindowViewModel.MissionPerformance.Timeline
                .Select(static row => $"{row.StepText}|{row.KindText}|{row.SourceText}|{row.DetailText}|{row.DrillDownText}")
                .ToArray();
            Assert.Equal(continuedRows.Length, continuedRows.Distinct(StringComparer.Ordinal).Count());
        }
        finally
        {
            restored.MainWindowViewModel.DetachRuntimeSubscriptions();
        }
    }

    [Fact]
    public void ExplicitExactPackBinding_RestoresCheckpointPrefixAndRejectsMismatchedPack()
    {
        var pack = InitialOperationalChallengePack.BoundedDemandFollowing;
        var root = CompositionRoot.CreateMissionChallenge(pack, enableSessionRecording: true);
        var viewModel = root.MainWindowViewModel;

        viewModel.RunCommand.Execute(null);
        _ = root.RuntimeCoordinator.AdvanceRunning(stepCount: 2, publicationStride: 1);
        viewModel.PauseCommand.Execute(null);
        viewModel.OperatorComputer.CreateSessionCheckpointCommand.Execute(null);
        var checkpointId = Assert.IsType<string>(viewModel.OperatorComputer.SelectedSessionCheckpointId);
        var expectedCheckpointTimeline = viewModel.MissionPerformance.Timeline
            .Select(static row => $"{row.StepText}|{row.KindText}|{row.SourceText}|{row.DetailText}|{row.DrillDownText}")
            .ToArray();

        viewModel.RunCommand.Execute(null);
        _ = root.RuntimeCoordinator.AdvanceRunning(stepCount: 2, publicationStride: 1);
        viewModel.PauseCommand.Execute(null);
        var archive = viewModel.OperatorComputer.ExportSessionArchive();
        viewModel.DetachRuntimeSubscriptions();

        var restored = CompositionRoot.CreateFromSessionArchive(archive, checkpointId, pack);
        try
        {
            Assert.Equal(pack.ExactId, restored.MainWindowViewModel.MissionPerformancePackExactId);
            Assert.Equal(2, restored.RuntimeCoordinator.Current.LogicalStep);
            Assert.Equal(
                expectedCheckpointTimeline,
                restored.MainWindowViewModel.MissionPerformance.Timeline
                    .Select(static row => $"{row.StepText}|{row.KindText}|{row.SourceText}|{row.DetailText}|{row.DrillDownText}")
                    .ToArray());
            Assert.All(
                restored.MainWindowViewModel.MissionPerformance.Timeline,
                row => Assert.True(ParseStep(row.StepText) <= restored.RuntimeCoordinator.Current.LogicalStep));
        }
        finally
        {
            restored.MainWindowViewModel.DetachRuntimeSubscriptions();
        }

        Assert.Throws<InvalidDataException>(() => CompositionRoot.CreateFromSessionArchive(
            archive,
            missionPack: InitialOperationalChallengePack.PreStartupPreparation));
    }

    [Fact]
    public void ArchiveWithoutExplicitPackBinding_RemainsMissionUnboundRatherThanInferringFromScenarioId()
    {
        var pack = InitialOperationalChallengePack.BoundedDemandFollowing;
        var root = CompositionRoot.CreateMissionChallenge(pack, enableSessionRecording: true);
        root.MainWindowViewModel.PauseCommand.Execute(null);
        var archive = root.MainWindowViewModel.OperatorComputer.ExportSessionArchive();
        root.MainWindowViewModel.DetachRuntimeSubscriptions();

        var restored = CompositionRoot.CreateFromSessionArchive(archive);
        try
        {
            Assert.False(restored.MainWindowViewModel.MissionPerformance.HasMission);
            Assert.Null(restored.MainWindowViewModel.MissionPerformancePackExactId);
        }
        finally
        {
            restored.MainWindowViewModel.DetachRuntimeSubscriptions();
        }
    }


    [Fact]
    public void StartRecordedSessionBoundary_PreservesCurrentExplicitPackBinding()
    {
        var path = ResolveRepoFile("src", "NuclearReactorSimulator.App", "Controls", "ControlRoomComputerControl.axaml.cs");
        var source = File.ReadAllText(path);

        Assert.Contains("MissionPerformancePackExactId", source, StringComparison.Ordinal);
        Assert.Contains("MissionChallengeStartupSelection.ResolveExactId", source, StringComparison.Ordinal);
        Assert.Contains("CompositionRoot.CreateMissionChallenge(missionPack, enableSessionRecording: true)", source, StringComparison.Ordinal);
        Assert.Contains("CompositionRoot.Create(enableSessionRecording: true)", source, StringComparison.Ordinal);
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

    private static long ParseStep(string text)
        => long.Parse(text.AsSpan("STEP ".Length), System.Globalization.CultureInfo.InvariantCulture);
}
