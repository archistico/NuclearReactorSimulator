namespace NuclearReactorSimulator.Application.Scenarios.Challenges;

/// <summary>Deterministic logical lifecycle for one M10.9.6 operational challenge.</summary>
public enum ChallengeLifecycleState
{
    NotStarted = 0,
    Ready = 1,
    Active = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5,
}
