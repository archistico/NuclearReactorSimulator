namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;

public sealed record ChallengeScoreEvaluationResult(
    string ChallengeExactId,
    string ScoringPolicyExactId,
    decimal RawScore,
    decimal GuidanceMultiplier,
    decimal AuthorityMultiplier,
    decimal ScoreBeforeDominanceCap,
    decimal FinalScore,
    decimal FinalPercentage,
    decimal? AppliedDominanceCapPercentage,
    bool IsEvidenceComplete,
    bool IsPassing,
    ChallengeScoreDominanceOutcome DominanceOutcome,
    ChallengeScoreGrade Grade,
    IReadOnlyList<ChallengeScoreDimensionResult> Dimensions);
