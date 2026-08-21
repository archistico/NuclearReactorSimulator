using System.Globalization;
using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Demand;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Packs;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;
using NuclearReactorSimulator.Application.Scenarios.Recording;
using NuclearReactorSimulator.Application.Scenarios.Training;

namespace NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;

/// <summary>
/// M10.9.7.1 pure presentation aggregation. It copies canonical M10.9.6 output and current control-room evidence;
/// it contains no score formula, challenge transition logic, command dispatcher, runtime engine or wall-clock dependency.
/// </summary>
public static class MissionPerformanceSnapshotProjector
{
    private const int MaximumRecentEvents = 100;

    public static MissionPerformanceSnapshot Project(
        OperationalChallengePackDefinition pack,
        ChallengeLifecycleSnapshot lifecycle,
        ControlRoomSnapshot controlRoom,
        ExternalEnergyDemandEvidenceSnapshot externalDemand,
        ChallengeScoreEvaluationResult? score,
        TrainingGuidanceMode assistanceMode,
        PlantControlAuthorityPresentationSnapshot? controlAuthority = null,
        IEnumerable<ScenarioRecordingEvent>? recordingEvents = null)
    {
        ArgumentNullException.ThrowIfNull(pack);
        ArgumentNullException.ThrowIfNull(lifecycle);
        ArgumentNullException.ThrowIfNull(controlRoom);
        ArgumentNullException.ThrowIfNull(externalDemand);
        if (!Enum.IsDefined(assistanceMode))
        {
            throw new ArgumentOutOfRangeException(nameof(assistanceMode));
        }
        if (!string.Equals(pack.Challenge.ExactId, lifecycle.ChallengeExactId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Challenge pack and lifecycle exact identities must match.", nameof(lifecycle));
        }
        if (externalDemand.LogicalStep != controlRoom.LogicalStep)
        {
            throw new ArgumentException("Mission/performance projection requires demand and control-room evidence from the same logical step.");
        }
        if (score is not null
            && (!string.Equals(score.ChallengeExactId, pack.Challenge.ExactId, StringComparison.Ordinal)
                || !string.Equals(score.ScoringPolicyExactId, pack.ScoringPolicy.ExactId, StringComparison.Ordinal)))
        {
            throw new ArgumentException("Score identity does not match the challenge pack.", nameof(score));
        }

        var alignedLifecycle = ChallengeLifecycleLogicalStepAlignment.Align(lifecycle, controlRoom.LogicalStep);
        var authority = controlAuthority ?? PlantControlAuthorityPresentationSnapshot.Unavailable;
        var requested = ControlRoomElectricalEvidence.RequestedGeneratorLoadMegawatts(controlRoom);
        var actual = controlRoom.Electrical.GrossElectricalOutput.NumericValue;
        var demand = new MissionPerformanceDemandSnapshot(
            externalDemand.IsAvailable,
            externalDemand.ProfileExactId,
            externalDemand.ExternalDemandMegawatts,
            externalDemand.RequestedGeneratorLoadMegawatts ?? requested,
            externalDemand.ActualElectricalOutputMegawatts ?? actual,
            externalDemand.DemandOutputErrorMegawatts,
            externalDemand.NextScheduledChangeLogicalStep,
            externalDemand.NextScheduledDemandMegawatts);
        var objective = pack.Scenario.Objectives.Single(item =>
            string.Equals(item.ObjectiveId, pack.Challenge.ObjectiveId, StringComparison.Ordinal));
        long? elapsed = alignedLifecycle.ActivatedLogicalStep.HasValue
            ? Math.Max(0L, alignedLifecycle.LogicalStep - alignedLifecycle.ActivatedLogicalStep.Value)
            : null;

        return new MissionPerformanceSnapshot(
            pack.ExactId,
            pack.Challenge.ExactId,
            pack.Scenario.ScenarioId,
            objective.ObjectiveId,
            objective.Title,
            objective.Description,
            alignedLifecycle.State,
            alignedLifecycle.LogicalStep,
            alignedLifecycle.ActivatedLogicalStep,
            elapsed,
            alignedLifecycle.TerminalLogicalStep,
            alignedLifecycle.TargetWindowStartLogicalStep,
            alignedLifecycle.TargetWindowEndLogicalStep,
            alignedLifecycle.HardFailureDeadlineLogicalStep,
            demand,
            ProjectScore(score),
            ProjectEvents(alignedLifecycle, score, recordingEvents),
            assistanceMode,
            authority.IsAvailable,
            authority.IsAvailable ? authority.RequestedAuthority : null,
            authority.IsAvailable ? authority.EffectiveAuthority : null,
            authority.IsAvailable ? authority.Health : null,
            authority.IsAvailable ? authority.DegradationReason : null);
    }

    private static MissionPerformanceScoreSnapshot ProjectScore(ChallengeScoreEvaluationResult? score)
    {
        if (score is null)
        {
            return MissionPerformanceScoreSnapshot.Unavailable;
        }

        var dimensions = score.Dimensions
            .Select(static item => new MissionPerformanceScoreDimensionSnapshot(
                item.Kind,
                item.MaximumPoints,
                item.AwardedPoints,
                item.IsEvidenceAvailable,
                item.PerformanceFraction,
                item.EvidenceSourceId,
                item.EvidenceSummary,
                item.IsCriticalFailure))
            .ToArray();
        return new MissionPerformanceScoreSnapshot(
            true,
            score.ScoringPolicyExactId,
            score.FinalScore,
            score.FinalPercentage,
            score.IsEvidenceComplete,
            score.IsPassing,
            score.DominanceOutcome,
            score.Grade,
            Array.AsReadOnly(dimensions));
    }

    private static IReadOnlyList<MissionPerformanceEventSnapshot> ProjectEvents(
        ChallengeLifecycleSnapshot lifecycle,
        ChallengeScoreEvaluationResult? score,
        IEnumerable<ScenarioRecordingEvent>? recordingEvents)
    {
        var events = new List<MissionPerformanceEventSnapshot>(MaximumRecentEvents);
        foreach (var transition in lifecycle.Transitions)
        {
            AddRecentEvent(events, new MissionPerformanceEventSnapshot(
                transition.LogicalStep,
                MissionPerformanceEventKind.Objective,
                "challenge-lifecycle",
                $"{transition.From} -> {transition.To}: {transition.Reason}",
                transition.Sequence));
        }

        if (recordingEvents is not null)
        {
            foreach (var item in recordingEvents)
            {
                if (item.Kind != ScenarioRecordingEventKind.ProtectionTransition
                    || item.LogicalStep > lifecycle.LogicalStep)
                {
                    continue;
                }

                AddRecentEvent(events, new MissionPerformanceEventSnapshot(
                    item.LogicalStep,
                    MissionPerformanceEventKind.Protection,
                    item.SourceId,
                    item.Detail,
                    item.Sequence,
                    string.Equals(item.Detail, "Active", StringComparison.Ordinal)));
            }
        }

        if (score is not null)
        {
            AddRecentEvent(events, new MissionPerformanceEventSnapshot(
                lifecycle.LogicalStep,
                MissionPerformanceEventKind.Scoring,
                score.ScoringPolicyExactId,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Score={score.FinalPercentage:0.##}% grade={score.Grade}; evidence-complete={score.IsEvidenceComplete}; dominance={score.DominanceOutcome}."),
                null,
                score.DominanceOutcome != ChallengeScoreDominanceOutcome.None));
        }

        return Array.AsReadOnly(events.ToArray());
    }

    private static void AddRecentEvent(
        List<MissionPerformanceEventSnapshot> events,
        MissionPerformanceEventSnapshot candidate)
    {
        var insertionIndex = events.Count;
        while (insertionIndex > 0 && CompareEvents(events[insertionIndex - 1], candidate) > 0)
        {
            insertionIndex--;
        }
        events.Insert(insertionIndex, candidate);
        if (events.Count > MaximumRecentEvents)
        {
            events.RemoveAt(0);
        }
    }

    private static int CompareEvents(MissionPerformanceEventSnapshot left, MissionPerformanceEventSnapshot right)
    {
        var comparison = left.LogicalStep.CompareTo(right.LogicalStep);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = left.Kind.CompareTo(right.Kind);
        if (comparison != 0)
        {
            return comparison;
        }
        comparison = (left.SourceSequence ?? long.MaxValue).CompareTo(right.SourceSequence ?? long.MaxValue);
        return comparison != 0
            ? comparison
            : StringComparer.Ordinal.Compare(left.SourceId, right.SourceId);
    }
}
