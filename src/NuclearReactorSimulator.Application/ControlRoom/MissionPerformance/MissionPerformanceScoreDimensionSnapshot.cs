using NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;

namespace NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;

public sealed record MissionPerformanceScoreDimensionSnapshot(
    ChallengeScoreDimensionKind Kind,
    decimal MaximumPoints,
    decimal AwardedPoints,
    bool IsEvidenceAvailable,
    decimal? PerformanceFraction,
    string EvidenceSourceId,
    string EvidenceSummary,
    bool IsCriticalFailure);
