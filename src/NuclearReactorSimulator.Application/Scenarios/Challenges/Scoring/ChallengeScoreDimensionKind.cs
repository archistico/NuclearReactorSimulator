namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;

/// <summary>Authoritative M10.9.6.3 challenge evaluation dimensions.</summary>
public enum ChallengeScoreDimensionKind
{
    SafetyProtectionDiscipline = 0,
    ProcedureRequiredActions = 1,
    StabilityOperatingQuality = 2,
    DemandTracking = 3,
    LogicalTimeCompletionEfficiency = 4,
}
