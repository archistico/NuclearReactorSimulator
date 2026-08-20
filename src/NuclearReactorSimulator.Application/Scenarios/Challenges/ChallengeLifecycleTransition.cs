namespace NuclearReactorSimulator.Application.Scenarios.Challenges;

/// <summary>One deterministic challenge lifecycle transition ordered by sequence and logical step only.</summary>
public sealed record ChallengeLifecycleTransition(
    long Sequence,
    ChallengeLifecycleState From,
    ChallengeLifecycleState To,
    long LogicalStep,
    string Reason);
