namespace NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;

/// <summary>
/// Explicit structural comparison for M10.9.7.3 publication suppression. Generated record equality is intentionally not
/// used because MissionPerformanceSnapshot contains IReadOnlyList members whose generated equality is reference based.
/// </summary>
public static class MissionPerformancePresentationComparer
{
    public static bool AreEquivalent(MissionPerformanceSnapshot? left, MissionPerformanceSnapshot? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }
        if (left is null || right is null)
        {
            return false;
        }

        return string.Equals(left.PackExactId, right.PackExactId, StringComparison.Ordinal)
            && string.Equals(left.ChallengeExactId, right.ChallengeExactId, StringComparison.Ordinal)
            && string.Equals(left.ScenarioId, right.ScenarioId, StringComparison.Ordinal)
            && string.Equals(left.ObjectiveId, right.ObjectiveId, StringComparison.Ordinal)
            && string.Equals(left.ObjectiveTitle, right.ObjectiveTitle, StringComparison.Ordinal)
            && string.Equals(left.ObjectiveDescription, right.ObjectiveDescription, StringComparison.Ordinal)
            && left.LifecycleState == right.LifecycleState
            && left.LogicalStep == right.LogicalStep
            && left.ActivatedLogicalStep == right.ActivatedLogicalStep
            && left.ElapsedLogicalSteps == right.ElapsedLogicalSteps
            && left.TerminalLogicalStep == right.TerminalLogicalStep
            && left.TargetWindowStartLogicalStep == right.TargetWindowStartLogicalStep
            && left.TargetWindowEndLogicalStep == right.TargetWindowEndLogicalStep
            && left.HardFailureDeadlineLogicalStep == right.HardFailureDeadlineLogicalStep
            && DemandEquivalent(left.Demand, right.Demand)
            && ScoreEquivalent(left.Score, right.Score)
            && left.RecentEvents.SequenceEqual(right.RecentEvents)
            && left.AssistanceMode == right.AssistanceMode
            && left.PlantControlAuthorityAvailable == right.PlantControlAuthorityAvailable
            && left.RequestedControlAuthority == right.RequestedControlAuthority
            && left.EffectiveControlAuthority == right.EffectiveControlAuthority
            && left.ControlAuthorityHealth == right.ControlAuthorityHealth
            && string.Equals(left.ControlAuthorityDegradationReason, right.ControlAuthorityDegradationReason, StringComparison.Ordinal);
    }

    private static bool DemandEquivalent(MissionPerformanceDemandSnapshot left, MissionPerformanceDemandSnapshot right)
        => left.ExternalDemandAvailable == right.ExternalDemandAvailable
            && string.Equals(left.ExternalDemandProfileExactId, right.ExternalDemandProfileExactId, StringComparison.Ordinal)
            && left.ExternalDemandMegawatts == right.ExternalDemandMegawatts
            && left.RequestedGeneratorLoadMegawatts == right.RequestedGeneratorLoadMegawatts
            && left.ActualElectricalOutputMegawatts == right.ActualElectricalOutputMegawatts
            && left.DemandOutputErrorMegawatts == right.DemandOutputErrorMegawatts
            && left.NextScheduledDemandChangeLogicalStep == right.NextScheduledDemandChangeLogicalStep
            && left.NextScheduledDemandMegawatts == right.NextScheduledDemandMegawatts;

    private static bool ScoreEquivalent(MissionPerformanceScoreSnapshot left, MissionPerformanceScoreSnapshot right)
        => left.IsAvailable == right.IsAvailable
            && string.Equals(left.ScoringPolicyExactId, right.ScoringPolicyExactId, StringComparison.Ordinal)
            && left.FinalScore == right.FinalScore
            && left.FinalPercentage == right.FinalPercentage
            && left.IsEvidenceComplete == right.IsEvidenceComplete
            && left.IsPassing == right.IsPassing
            && left.DominanceOutcome == right.DominanceOutcome
            && left.Grade == right.Grade
            && left.Dimensions.SequenceEqual(right.Dimensions);
}
