using System.Globalization;
using NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Demand;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;
using NuclearReactorSimulator.Application.Scenarios.Recording;

namespace NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;

/// <summary>
/// M10.9.7.4 bounded deterministic timeline projection. Lifecycle-spine retention is independent from dense operational
/// evidence, so protection/scoring traffic cannot erase the mission-defining activation/terminal narrative.
/// </summary>
public static class MissionPerformanceTimelineProjector
{
    public const int MaximumLifecycleSpineEntries = 32;
    public const int MaximumRecentOperationalEvidenceEntries = 100;

    public static MissionPerformanceTimelineProjection Project(
        ChallengeLifecycleSnapshot lifecycle,
        ChallengeScoreEvaluationResult? score,
        IEnumerable<ScenarioRecordingEvent>? recordingEvents,
        IReadOnlyList<ExternalEnergyDemandEvidenceSnapshot>? demandTimeline = null)
    {
        ArgumentNullException.ThrowIfNull(lifecycle);

        var spine = RetainLifecycleSpine(lifecycle);
        var operational = BuildOperationalEvidence(lifecycle, score, recordingEvents, demandTimeline);
        var merged = spine.Concat(operational)
            .DistinctBy(static item => TimelineIdentity(item))
            .OrderBy(static item => item, TimelineComparer.Instance)
            .ToArray();

        return new MissionPerformanceTimelineProjection(
            Array.AsReadOnly(spine.ToArray()),
            Array.AsReadOnly(operational.ToArray()),
            Array.AsReadOnly(merged));
    }

    private static IReadOnlyList<MissionPerformanceTimelineEntrySnapshot> RetainLifecycleSpine(
        ChallengeLifecycleSnapshot lifecycle)
    {
        var eligibleTransitions = lifecycle.Transitions
            .Where(transition => transition.LogicalStep <= lifecycle.LogicalStep)
            .ToArray();
        var all = eligibleTransitions
            .Select(static transition => ProjectLifecycleTransition(transition))
            .OrderBy(static item => item, TimelineComparer.Instance)
            .ToArray();

        if (all.Length <= MaximumLifecycleSpineEntries)
        {
            return all;
        }

        // Preserve the first activation boundary and latest terminal boundary before filling the remaining capacity with
        // the newest lifecycle evidence. This keeps the mission narrative intact even after many explicit resets.
        var retained = new Dictionary<
            (long LogicalStep, MissionPerformanceTimelineEntryKind Kind, long? SourceSequence, string SourceId, string Summary),
            MissionPerformanceTimelineEntrySnapshot>();
        var firstActivationTransition = eligibleTransitions.FirstOrDefault(static transition =>
            transition.To == ChallengeLifecycleState.Active);
        if (firstActivationTransition is not null)
        {
            var firstActivation = ProjectLifecycleTransition(firstActivationTransition);
            retained[TimelineIdentity(firstActivation)] = firstActivation;
        }

        var latestTerminalTransition = eligibleTransitions.LastOrDefault(static transition =>
            transition.To is ChallengeLifecycleState.Completed
                or ChallengeLifecycleState.Failed
                or ChallengeLifecycleState.Cancelled);
        if (latestTerminalTransition is not null)
        {
            var latestTerminal = ProjectLifecycleTransition(latestTerminalTransition);
            retained[TimelineIdentity(latestTerminal)] = latestTerminal;
        }

        for (var index = all.Length - 1; index >= 0 && retained.Count < MaximumLifecycleSpineEntries; index--)
        {
            retained.TryAdd(TimelineIdentity(all[index]), all[index]);
        }

        return retained.Values.OrderBy(static item => item, TimelineComparer.Instance).ToArray();
    }


    private static MissionPerformanceTimelineEntrySnapshot ProjectLifecycleTransition(ChallengeLifecycleTransition transition)
        => new(
            transition.LogicalStep,
            MissionPerformanceTimelineEntryKind.Objective,
            "challenge-lifecycle",
            $"{transition.From} -> {transition.To}: {transition.Reason}",
            transition.Sequence,
            transition.To == ChallengeLifecycleState.Failed);

    private static IReadOnlyList<MissionPerformanceTimelineEntrySnapshot> BuildOperationalEvidence(
        ChallengeLifecycleSnapshot lifecycle,
        ChallengeScoreEvaluationResult? score,
        IEnumerable<ScenarioRecordingEvent>? recordingEvents,
        IReadOnlyList<ExternalEnergyDemandEvidenceSnapshot>? demandTimeline)
    {
        var items = new List<MissionPerformanceTimelineEntrySnapshot>();

        if (recordingEvents is not null)
        {
            foreach (var item in recordingEvents)
            {
                if (item.LogicalStep > lifecycle.LogicalStep)
                {
                    continue;
                }

                var projected = item.Kind switch
                {
                    ScenarioRecordingEventKind.OperatorAction => new MissionPerformanceTimelineEntrySnapshot(
                        item.LogicalStep,
                        MissionPerformanceTimelineEntryKind.OperatorAction,
                        item.SourceId,
                        item.Detail,
                        item.Sequence,
                        false,
                        new MissionPerformanceDrillDownTarget(
                            ControlRoomWorkspaceId.OperatorComputer,
                            "COMMAND CONTEXT",
                            OperatorComputerPageId.Commands)),
                    ScenarioRecordingEventKind.Alarm => new MissionPerformanceTimelineEntrySnapshot(
                        item.LogicalStep,
                        MissionPerformanceTimelineEntryKind.Alarm,
                        item.SourceId,
                        item.Detail,
                        item.Sequence,
                        false,
                        new MissionPerformanceDrillDownTarget(ControlRoomWorkspaceId.AlarmsEvents, "ALARMS / EVENTS")),
                    ScenarioRecordingEventKind.ProtectionTransition => new MissionPerformanceTimelineEntrySnapshot(
                        item.LogicalStep,
                        MissionPerformanceTimelineEntryKind.Protection,
                        item.SourceId,
                        item.Detail,
                        item.Sequence,
                        string.Equals(item.Detail, "Active", StringComparison.Ordinal),
                        new MissionPerformanceDrillDownTarget(ControlRoomWorkspaceId.AlarmsEvents, "PROTECTION / EVENTS")),
                    ScenarioRecordingEventKind.FaultTransition => new MissionPerformanceTimelineEntrySnapshot(
                        item.LogicalStep,
                        MissionPerformanceTimelineEntryKind.Fault,
                        item.SourceId,
                        item.Detail,
                        item.Sequence,
                        string.Equals(item.Detail, "Active", StringComparison.Ordinal),
                        new MissionPerformanceDrillDownTarget(
                            ControlRoomWorkspaceId.OperatorComputer,
                            "DIAGNOSTICS",
                            OperatorComputerPageId.Diagnostics)),
                    _ => null,
                };

                if (projected is not null)
                {
                    items.Add(projected);
                }
            }
        }

        AddDemandChanges(items, lifecycle.LogicalStep, demandTimeline);

        if (score is not null)
        {
            items.Add(new MissionPerformanceTimelineEntrySnapshot(
                lifecycle.LogicalStep,
                MissionPerformanceTimelineEntryKind.Scoring,
                score.ScoringPolicyExactId,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Score={score.FinalPercentage:0.##}% grade={score.Grade}; evidence-complete={score.IsEvidenceComplete}; dominance={score.DominanceOutcome}."),
                null,
                score.DominanceOutcome != ChallengeScoreDominanceOutcome.None));
        }

        return items
            .DistinctBy(static item => TimelineIdentity(item))
            .OrderBy(static item => item, TimelineComparer.Instance)
            .TakeLast(MaximumRecentOperationalEvidenceEntries)
            .ToArray();
    }

    private static void AddDemandChanges(
        List<MissionPerformanceTimelineEntrySnapshot> items,
        long currentLogicalStep,
        IReadOnlyList<ExternalEnergyDemandEvidenceSnapshot>? demandTimeline)
    {
        if (demandTimeline is null || demandTimeline.Count == 0)
        {
            return;
        }

        double? previousDemand = null;
        string? previousProfile = null;
        var havePrevious = false;
        foreach (var sample in demandTimeline)
        {
            if (sample.LogicalStep > currentLogicalStep || !sample.IsAvailable || !sample.ExternalDemandMegawatts.HasValue)
            {
                continue;
            }

            var changed = !havePrevious
                || previousDemand != sample.ExternalDemandMegawatts
                || !string.Equals(previousProfile, sample.ProfileExactId, StringComparison.Ordinal);
            if (changed)
            {
                items.Add(new MissionPerformanceTimelineEntrySnapshot(
                    sample.LogicalStep,
                    MissionPerformanceTimelineEntryKind.Demand,
                    sample.ProfileExactId ?? "external-demand",
                    string.Create(CultureInfo.InvariantCulture, $"External grid demand -> {sample.ExternalDemandMegawatts.Value:0.###} MWe."),
                    null,
                    false,
                    new MissionPerformanceDrillDownTarget(ControlRoomWorkspaceId.Electrical, "ELECTRICAL")));
            }

            previousDemand = sample.ExternalDemandMegawatts;
            previousProfile = sample.ProfileExactId;
            havePrevious = true;
        }
    }

    private static (
        long LogicalStep,
        MissionPerformanceTimelineEntryKind Kind,
        long? SourceSequence,
        string SourceId,
        string Summary) TimelineIdentity(MissionPerformanceTimelineEntrySnapshot item)
        => (item.LogicalStep, item.Kind, item.SourceSequence, item.SourceId, item.Summary);

    private sealed class TimelineComparer : IComparer<MissionPerformanceTimelineEntrySnapshot>
    {
        public static TimelineComparer Instance { get; } = new();

        public int Compare(MissionPerformanceTimelineEntrySnapshot? left, MissionPerformanceTimelineEntrySnapshot? right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }
            if (left is null)
            {
                return -1;
            }
            if (right is null)
            {
                return 1;
            }

            var comparison = left.LogicalStep.CompareTo(right.LogicalStep);
            if (comparison != 0)
            {
                return comparison;
            }
            comparison = (left.SourceSequence ?? long.MaxValue).CompareTo(right.SourceSequence ?? long.MaxValue);
            if (comparison != 0)
            {
                return comparison;
            }
            comparison = left.Kind.CompareTo(right.Kind);
            if (comparison != 0)
            {
                return comparison;
            }
            comparison = StringComparer.Ordinal.Compare(left.SourceId, right.SourceId);
            return comparison != 0 ? comparison : StringComparer.Ordinal.Compare(left.Summary, right.Summary);
        }
    }
}
