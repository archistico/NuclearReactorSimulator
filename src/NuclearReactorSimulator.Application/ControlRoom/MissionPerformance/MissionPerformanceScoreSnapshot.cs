using NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;

namespace NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;

/// <summary>Pure presentation copy of M10.9.6 scoring output; this type owns no scoring arithmetic.</summary>
public sealed record MissionPerformanceScoreSnapshot(
    bool IsAvailable,
    string? ScoringPolicyExactId,
    decimal? FinalScore,
    decimal? FinalPercentage,
    bool? IsEvidenceComplete,
    bool? IsPassing,
    ChallengeScoreDominanceOutcome? DominanceOutcome,
    ChallengeScoreGrade? Grade,
    IReadOnlyList<MissionPerformanceScoreDimensionSnapshot> Dimensions)
{
    public static MissionPerformanceScoreSnapshot Unavailable { get; } = new(
        false,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        Array.Empty<MissionPerformanceScoreDimensionSnapshot>());
}
