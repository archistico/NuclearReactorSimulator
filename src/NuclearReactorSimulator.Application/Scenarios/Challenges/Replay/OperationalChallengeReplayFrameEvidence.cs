using NuclearReactorSimulator.Application.Scenarios.Challenges.Demand;
using NuclearReactorSimulator.Application.Scenarios.Challenges;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Replay;

/// <summary>One deterministic reconstructed challenge frame derived from one canonical recorder frame.</summary>
public sealed record OperationalChallengeReplayFrameEvidence(
    long LogicalStep,
    string SnapshotFingerprint,
    ChallengeLifecycleState LifecycleState,
    long? ActivatedLogicalStep,
    long? TerminalLogicalStep,
    ExternalEnergyDemandEvidenceSnapshot ExternalDemand);
