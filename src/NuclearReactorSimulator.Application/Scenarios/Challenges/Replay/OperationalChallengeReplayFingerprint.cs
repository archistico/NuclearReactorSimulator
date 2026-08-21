using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;
using NuclearReactorSimulator.Application.Scenarios.Challenges;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Replay;

public static class OperationalChallengeReplayFingerprint
{
    public const string AlgorithmId = "m10965-challenge-replay-sha256-v1";

    public static string Compute(
        string packExactId,
        ChallengeLifecycleSnapshot lifecycle,
        IReadOnlyList<OperationalChallengeReplayFrameEvidence> frames,
        ChallengeScoreEvaluationResult score)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packExactId);
        ArgumentNullException.ThrowIfNull(lifecycle);
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(score);

        var builder = new StringBuilder();
        builder.Append(AlgorithmId).Append('|').Append(packExactId).Append('|')
            .Append(lifecycle.ChallengeExactId).Append('|').Append(lifecycle.State).Append('|')
            .Append(lifecycle.LogicalStep.ToString(CultureInfo.InvariantCulture)).Append('|')
            .Append(lifecycle.ActivatedLogicalStep?.ToString(CultureInfo.InvariantCulture) ?? "-").Append('|')
            .Append(lifecycle.TerminalLogicalStep?.ToString(CultureInfo.InvariantCulture) ?? "-").AppendLine();

        foreach (var transition in lifecycle.Transitions)
        {
            builder.Append("T|").Append(transition.Sequence.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(transition.From).Append('|').Append(transition.To).Append('|')
                .Append(transition.LogicalStep.ToString(CultureInfo.InvariantCulture)).AppendLine();
        }
        foreach (var observation in lifecycle.Observations)
        {
            builder.Append("O|").Append(observation.ConditionId).Append('|').Append(observation.IsSatisfied).Append('|')
                .Append(observation.LogicalStep.ToString(CultureInfo.InvariantCulture)).AppendLine();
        }
        foreach (var frame in frames)
        {
            builder.Append("F|").Append(frame.LogicalStep.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(frame.SnapshotFingerprint).Append('|').Append(frame.LifecycleState).Append('|')
                .Append(frame.ActivatedLogicalStep?.ToString(CultureInfo.InvariantCulture) ?? "-").Append('|')
                .Append(frame.TerminalLogicalStep?.ToString(CultureInfo.InvariantCulture) ?? "-").Append('|')
                .Append(frame.ExternalDemand.IsAvailable).Append('|')
                .Append(frame.ExternalDemand.ExternalDemandMegawatts?.ToString("R", CultureInfo.InvariantCulture) ?? "-").Append('|')
                .Append(frame.ExternalDemand.RequestedGeneratorLoadMegawatts?.ToString("R", CultureInfo.InvariantCulture) ?? "-").Append('|')
                .Append(frame.ExternalDemand.ActualElectricalOutputMegawatts?.ToString("R", CultureInfo.InvariantCulture) ?? "-")
                .AppendLine();
        }
        builder.Append("S|").Append(score.ScoringPolicyExactId).Append('|')
            .Append(score.FinalScore.ToString(CultureInfo.InvariantCulture)).Append('|')
            .Append(score.IsEvidenceComplete).Append('|').Append(score.IsPassing).Append('|')
            .Append(score.DominanceOutcome).Append('|').Append(score.Grade).AppendLine();
        foreach (var dimension in score.Dimensions.OrderBy(static item => item.Kind))
        {
            builder.Append("D|").Append(dimension.Kind).Append('|')
                .Append(dimension.IsEvidenceAvailable).Append('|')
                .Append(dimension.PerformanceFraction?.ToString(CultureInfo.InvariantCulture) ?? "-").Append('|')
                .Append(dimension.AwardedPoints.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(dimension.IsCriticalFailure).Append('|').Append(dimension.EvidenceSourceId).AppendLine();
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }
}
