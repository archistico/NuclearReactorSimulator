using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;

/// <summary>
/// Pure M10.9.6.3 challenge score arithmetic. It consumes authored observational evidence and owns no dispatcher,
/// simulation state, controller, protection or wall-clock seam.
/// </summary>
public static class ChallengeScoreCalculator
{
    public static ChallengeScoreEvaluationResult Evaluate(
        ChallengeDefinition challenge,
        ChallengeScoringPolicyDefinition policy,
        TrainingGuidanceMode guidanceMode,
        PlantControlAuthorityMode authorityMode,
        IEnumerable<ChallengeScoreDimensionEvidence> evidence)
    {
        ArgumentNullException.ThrowIfNull(challenge);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(evidence);
        if (!challenge.AssistancePolicy.Allows(guidanceMode))
        {
            throw new InvalidOperationException($"Guidance mode '{guidanceMode}' is not allowed by challenge '{challenge.ExactId}'.");
        }
        if (!string.Equals(challenge.AssistancePolicy.ScoringPolicyId, policy.ExactId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Challenge scoring policy identity '{challenge.AssistancePolicy.ScoringPolicyId}' does not match resolved policy '{policy.ExactId}'.");
        }
        if (!Enum.IsDefined(authorityMode))
        {
            throw new ArgumentOutOfRangeException(nameof(authorityMode));
        }

        var evidenceArray = evidence.ToArray();
        if (evidenceArray.Any(static item => item is null))
        {
            throw new ArgumentException("Score evidence cannot contain null entries.", nameof(evidence));
        }
        if (evidenceArray.Select(static item => item.Kind).Distinct().Count() != evidenceArray.Length)
        {
            throw new ArgumentException("Score evidence dimensions must be unique.", nameof(evidence));
        }

        var evidenceByKind = evidenceArray.ToDictionary(static item => item.Kind);
        var policyKinds = policy.Dimensions.Select(static item => item.Kind).ToHashSet();
        if (evidenceByKind.Keys.Any(kind => !policyKinds.Contains(kind))
            || policy.Dimensions.Any(dimension => !evidenceByKind.ContainsKey(dimension.Kind)))
        {
            throw new ArgumentException("Score evidence must contain exactly one item for every policy dimension and no extras.", nameof(evidence));
        }

        var dimensionResults = policy.Dimensions
            .Select(dimension => EvaluateDimension(dimension, evidenceByKind[dimension.Kind]))
            .ToArray();
        var rawScore = dimensionResults.Sum(static item => item.AwardedPoints);
        var guidanceMultiplier = policy.GuidanceMultiplier(guidanceMode);
        var authorityMultiplier = policy.AuthorityMultiplier(authorityMode);
        var beforeCap = rawScore * guidanceMultiplier * authorityMultiplier;

        var safetyCritical = dimensionResults.Any(static item => item.Kind == ChallengeScoreDimensionKind.SafetyProtectionDiscipline && item.IsCriticalFailure);
        var procedureCritical = dimensionResults.Any(static item => item.Kind == ChallengeScoreDimensionKind.ProcedureRequiredActions && item.IsCriticalFailure);
        var dominance = safetyCritical
            ? ChallengeScoreDominanceOutcome.CriticalSafetyFailure
            : procedureCritical
                ? ChallengeScoreDominanceOutcome.CriticalProcedureFailure
                : ChallengeScoreDominanceOutcome.None;
        decimal? capPercentage = dominance switch
        {
            ChallengeScoreDominanceOutcome.CriticalSafetyFailure => policy.CriticalSafetyCapPercentage,
            ChallengeScoreDominanceOutcome.CriticalProcedureFailure => policy.CriticalProcedureCapPercentage,
            _ => null,
        };
        var finalScore = capPercentage.HasValue
            ? Math.Min(beforeCap, capPercentage.Value)
            : beforeCap;
        var complete = dimensionResults.All(static item => item.IsEvidenceAvailable);
        var passing = complete
            && dominance == ChallengeScoreDominanceOutcome.None
            && finalScore >= policy.PassPercentage;
        var grade = Grade(policy, complete, dominance, finalScore);

        return new ChallengeScoreEvaluationResult(
            challenge.ExactId,
            policy.ExactId,
            rawScore,
            guidanceMultiplier,
            authorityMultiplier,
            beforeCap,
            finalScore,
            finalScore,
            capPercentage,
            complete,
            passing,
            dominance,
            grade,
            Array.AsReadOnly(dimensionResults));
    }

    private static ChallengeScoreDimensionResult EvaluateDimension(
        ChallengeScoreDimensionPolicy policy,
        ChallengeScoreDimensionEvidence evidence)
    {
        var points = evidence.IsAvailable
            ? policy.MaximumPoints * evidence.PerformanceFraction!.Value
            : 0m;
        return new ChallengeScoreDimensionResult(
            policy.Kind,
            policy.MaximumPoints,
            points,
            evidence.IsAvailable,
            evidence.PerformanceFraction,
            evidence.EvidenceSourceId,
            evidence.EvidenceSummary,
            evidence.IsCriticalFailure);
    }

    private static ChallengeScoreGrade Grade(
        ChallengeScoringPolicyDefinition policy,
        bool complete,
        ChallengeScoreDominanceOutcome dominance,
        decimal finalPercentage)
    {
        if (!complete)
        {
            return ChallengeScoreGrade.IncompleteEvidence;
        }
        if (dominance == ChallengeScoreDominanceOutcome.CriticalSafetyFailure)
        {
            return ChallengeScoreGrade.Unsafe;
        }
        if (dominance == ChallengeScoreDominanceOutcome.CriticalProcedureFailure)
        {
            return ChallengeScoreGrade.ProcedureFailure;
        }
        if (finalPercentage < policy.PassPercentage)
        {
            return ChallengeScoreGrade.NeedsImprovement;
        }
        if (finalPercentage < policy.ProficientPercentage)
        {
            return ChallengeScoreGrade.Satisfactory;
        }
        if (finalPercentage < policy.ExcellentPercentage)
        {
            return ChallengeScoreGrade.Proficient;
        }
        return ChallengeScoreGrade.Excellent;
    }
}
