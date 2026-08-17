namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Shadow-only H.20 authority decision. Production commit is intentionally impossible from this contract;
/// a future activation milestone must explicitly wire and revalidate any committed authority change.
/// </summary>
public sealed record FourNodeBranchContinuityActivationDecision(
    string SampleId,
    FourNodeBranchContinuityProposedAuthority ProposedAuthority,
    FourNodeBranchContinuityActivationReason Reason,
    bool RollbackRequired,
    bool TriggerObserved,
    bool ActivationArmEnabled)
{
    public bool ShadowCorrectedCandidateEligible
        => ProposedAuthority == FourNodeBranchContinuityProposedAuthority.CorrectedCandidate;

    public bool ProductionCommitAuthorized => false;
}
