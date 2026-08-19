using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Non-authoritative H.29 deployment diagnostics accumulated from the existing H.20/H.22 per-step telemetry.
/// These counters are numerical diagnostics only and are intentionally not part of the operator-facing control-room snapshot.
/// </summary>
public sealed record FourNodeProductionActivationTelemetrySnapshot(
    long ObservedSteps,
    long FourNodeTelemetrySteps,
    long TriggeredSteps,
    long CandidateEligibleSteps,
    long CommitAuthorizedSteps,
    long CorrectedCommittedSteps,
    long ExplicitFallbackSteps,
    long RollbackSteps,
    long FallbackCommitViolations,
    long UnsafeCommitViolations,
    long UntargetedBranchDisagreementSteps,
    IReadOnlyDictionary<FourNodeBranchContinuityActivationReason, long> RollbackReasonCounts,
    IReadOnlyDictionary<FourNodeBranchContinuityCorrectedCommitReason, long> CommitReasonCounts)
{
    public static FourNodeProductionActivationTelemetrySnapshot Empty { get; } = new(
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        new ReadOnlyDictionary<FourNodeBranchContinuityActivationReason, long>(
            new Dictionary<FourNodeBranchContinuityActivationReason, long>()),
        new ReadOnlyDictionary<FourNodeBranchContinuityCorrectedCommitReason, long>(
            new Dictionary<FourNodeBranchContinuityCorrectedCommitReason, long>()));
}
