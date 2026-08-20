using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;

/// <summary>Frozen M10.9.6.3 standard v1 scoring policies.</summary>
public static class StandardChallengeScoringPolicies
{
    public static ChallengeScoringPolicyDefinition GeneralOperationsV1 { get; } = new(
        "general-operations",
        1,
        new[]
        {
            new ChallengeScoreDimensionPolicy(ChallengeScoreDimensionKind.SafetyProtectionDiscipline, 45m),
            new ChallengeScoreDimensionPolicy(ChallengeScoreDimensionKind.ProcedureRequiredActions, 30m),
            new ChallengeScoreDimensionPolicy(ChallengeScoreDimensionKind.StabilityOperatingQuality, 20m),
            new ChallengeScoreDimensionPolicy(ChallengeScoreDimensionKind.LogicalTimeCompletionEfficiency, 5m),
        },
        NeutralGuidanceModifiers(),
        NeutralAuthorityModifiers());

    public static ChallengeScoringPolicyDefinition DemandFollowingV1 { get; } = new(
        "demand-following",
        1,
        new[]
        {
            new ChallengeScoreDimensionPolicy(ChallengeScoreDimensionKind.SafetyProtectionDiscipline, 40m),
            new ChallengeScoreDimensionPolicy(ChallengeScoreDimensionKind.ProcedureRequiredActions, 25m),
            new ChallengeScoreDimensionPolicy(ChallengeScoreDimensionKind.StabilityOperatingQuality, 15m),
            new ChallengeScoreDimensionPolicy(ChallengeScoreDimensionKind.DemandTracking, 15m),
            new ChallengeScoreDimensionPolicy(ChallengeScoreDimensionKind.LogicalTimeCompletionEfficiency, 5m),
        },
        NeutralGuidanceModifiers(),
        NeutralAuthorityModifiers());

    private static ChallengeGuidanceScoreModifier[] NeutralGuidanceModifiers()
        => Enum.GetValues<TrainingGuidanceMode>()
            .Select(static mode => new ChallengeGuidanceScoreModifier(mode, 1m))
            .ToArray();

    private static ChallengeAuthorityScoreModifier[] NeutralAuthorityModifiers()
        => Enum.GetValues<PlantControlAuthorityMode>()
            .Select(static mode => new ChallengeAuthorityScoreModifier(mode, 1m))
            .ToArray();
}
