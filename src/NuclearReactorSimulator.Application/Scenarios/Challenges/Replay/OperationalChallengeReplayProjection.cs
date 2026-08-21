using NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;
using NuclearReactorSimulator.Application.Scenarios.Challenges;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Replay;

/// <summary>
/// Immutable M10.9.6.5 replay/checkpoint projection. Everything in this type is derived from an existing deterministic
/// recording plus exact challenge/scoring identities; none of it is authoritative plant state.
/// </summary>
public sealed record OperationalChallengeReplayProjection(
    string PackExactId,
    string ScenarioId,
    long InitialLogicalStep,
    long FinalLogicalStep,
    ChallengeLifecycleSnapshot Lifecycle,
    IReadOnlyList<OperationalChallengeReplayFrameEvidence> Frames,
    ChallengeScoreEvaluationResult Score,
    string DeterministicFingerprint);
