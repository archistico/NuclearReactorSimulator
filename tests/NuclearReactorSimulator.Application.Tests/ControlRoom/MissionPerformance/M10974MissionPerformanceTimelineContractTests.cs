using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance;

public sealed class M10974MissionPerformanceTimelineContractTests
{
    [Fact]
    public void DenseOperationalEvidence_CannotEvictActivationOrTerminalLifecycleSpine()
    {
        var transitions = new List<ChallengeLifecycleTransition>
        {
            new(1, ChallengeLifecycleState.NotStarted, ChallengeLifecycleState.Ready, 1, "ready"),
            new(2, ChallengeLifecycleState.Ready, ChallengeLifecycleState.Active, 2, "activate"),
        };
        for (var index = 0; index < 40; index++)
        {
            var sequence = transitions.Count + 1L;
            transitions.Add(new ChallengeLifecycleTransition(
                sequence,
                index % 2 == 0 ? ChallengeLifecycleState.Active : ChallengeLifecycleState.NotStarted,
                index % 2 == 0 ? ChallengeLifecycleState.NotStarted : ChallengeLifecycleState.Active,
                3 + index,
                $"reset-cycle-{index:D2}"));
        }
        transitions.Add(new ChallengeLifecycleTransition(
            transitions.Count + 1L,
            ChallengeLifecycleState.Active,
            ChallengeLifecycleState.Failed,
            50,
            "terminal-failure"));

        var lifecycle = new ChallengeLifecycleSnapshot(
            "challenge@1",
            ChallengeLifecycleState.Failed,
            50,
            2,
            50,
            null,
            null,
            null,
            Array.Empty<ChallengeConditionObservation>(),
            transitions);
        var events = Enumerable.Range(1, 140)
            .Select(index => new ScenarioRecordingEvent(
                index,
                50,
                ScenarioRecordingEventKind.ProtectionTransition,
                $"protection-{index:D3}",
                "Active"))
            .ToArray();

        var projection = MissionPerformanceTimelineProjector.Project(lifecycle, null, events);

        Assert.Equal(MissionPerformanceTimelineProjector.MaximumLifecycleSpineEntries, projection.LifecycleSpine.Count);
        Assert.Equal(MissionPerformanceTimelineProjector.MaximumRecentOperationalEvidenceEntries, projection.RecentOperationalEvidence.Count);
        Assert.Contains(projection.LifecycleSpine, item => item.Summary.Contains("-> Active", StringComparison.Ordinal));
        Assert.Contains(projection.LifecycleSpine, item => item.Summary.Contains("-> Failed", StringComparison.Ordinal));
        Assert.Contains(projection.Timeline, item => item.Summary.Contains("-> Active", StringComparison.Ordinal));
        Assert.Contains(projection.Timeline, item => item.Summary.Contains("-> Failed", StringComparison.Ordinal));
        Assert.Equal(
            projection.Timeline.OrderBy(static item => item.LogicalStep)
                .ThenBy(static item => item.SourceSequence ?? long.MaxValue)
                .ThenBy(static item => item.Kind)
                .ThenBy(static item => item.SourceId, StringComparer.Ordinal)
                .ThenBy(static item => item.Summary, StringComparer.Ordinal)
                .ToArray(),
            projection.Timeline.ToArray());
    }

    [Fact]
    public void OperationalRows_CarryPresentationOnlyDrillDownTargets()
    {
        var lifecycle = new ChallengeLifecycleSnapshot(
            "challenge@1",
            ChallengeLifecycleState.Active,
            7,
            1,
            null,
            null,
            null,
            null,
            Array.Empty<ChallengeConditionObservation>(),
            Array.Empty<ChallengeLifecycleTransition>());
        var action = new ScenarioRecordingEvent(
            1,
            7,
            ScenarioRecordingEventKind.OperatorAction,
            "generator-load-raise",
            "Accepted GeneratorLoadRaise",
            new ControlRoomCommand(
                ControlRoomCommandKind.GeneratorLoadRaise,
                "generator",
                ControlRoomCommandTargetKind.Generator));
        var protection = new ScenarioRecordingEvent(
            2,
            7,
            ScenarioRecordingEventKind.ProtectionTransition,
            "generator-trip",
            "Active");

        var projection = MissionPerformanceTimelineProjector.Project(lifecycle, null, new[] { action, protection });
        var operatorRow = Assert.Single(projection.RecentOperationalEvidence, item => item.Kind == MissionPerformanceTimelineEntryKind.OperatorAction);
        var protectionRow = Assert.Single(projection.RecentOperationalEvidence, item => item.Kind == MissionPerformanceTimelineEntryKind.Protection);

        Assert.NotNull(operatorRow.DrillDownTarget);
        Assert.Equal(ControlRoomWorkspaceId.OperatorComputer, operatorRow.DrillDownTarget.WorkspaceId);
        Assert.Equal(OperatorComputerPageId.Commands, operatorRow.DrillDownTarget.OperatorComputerPageId);
        Assert.NotNull(protectionRow.DrillDownTarget);
        Assert.Equal(ControlRoomWorkspaceId.AlarmsEvents, protectionRow.DrillDownTarget.WorkspaceId);
        Assert.Null(protectionRow.DrillDownTarget.OperatorComputerPageId);

        var publicTypes = new[]
        {
            typeof(MissionPerformanceTimelineEntrySnapshot),
            typeof(MissionPerformanceDrillDownTarget),
            typeof(MissionPerformanceTimelineProjection),
        };
        Assert.All(publicTypes, type => Assert.DoesNotContain(
            type.GetProperties(),
            property => property.PropertyType.Name.Contains("CommandDispatcher", StringComparison.Ordinal)));
    }
}
