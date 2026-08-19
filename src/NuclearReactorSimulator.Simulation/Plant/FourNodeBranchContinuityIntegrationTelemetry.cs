namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Immutable H.21/H.22 per-step four-node integration telemetry. H.21 remains observation-only; H.22 adds
/// a separately opt-in corrected-candidate commit seam while preserving the unchanged H.20 authority decision.
/// </summary>
public sealed record FourNodeBranchContinuityIntegrationTelemetry(
    bool TriggerObserved,
    bool ShadowCorrectionEvaluated,
    FourNodeBranchContinuityProposedAuthority ProposedAuthority,
    FourNodeBranchContinuityActivationReason Reason,
    bool RollbackRequired,
    bool ShadowCorrectedCandidateEligible,
    bool CorrectedCandidateCommitted,
    bool UntargetedBranchDisagreementDetected,
    int BranchOverrideCount,
    int PreviousPhaseHoldCount,
    int HysteresisReleaseCount,
    int ShadowIterationCount,
    bool ShadowConverged,
    bool ShadowLineSearchExhausted,
    double ShadowMaximumRelativePressureResidual,
    double ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond,
    double ShadowMassClosureKilogramsPerSecond,
    double ShadowEnergyOwnershipResidualWatts)
{
    public bool CorrectedCommitArmEnabled { get; init; }

    public bool CorrectedCommitAuthorized { get; init; }

    public FourNodeBranchContinuityCorrectedCommitReason CorrectedCommitReason { get; init; }
        = FourNodeBranchContinuityCorrectedCommitReason.CommitArmDisabled;
}
