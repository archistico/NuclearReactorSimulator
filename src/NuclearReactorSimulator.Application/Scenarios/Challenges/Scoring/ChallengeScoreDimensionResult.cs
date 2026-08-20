namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;

public sealed record ChallengeScoreDimensionResult(
    ChallengeScoreDimensionKind Kind,
    decimal MaximumPoints,
    decimal AwardedPoints,
    bool IsEvidenceAvailable,
    decimal? PerformanceFraction,
    string EvidenceSourceId,
    string EvidenceSummary,
    bool IsCriticalFailure);
