namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// H.22 fail-closed corrected-candidate commit-seam reason. This is deliberately separate from the unchanged
/// H.20 shadow authority reason so activation eligibility and committed-state ownership remain independently auditable.
/// </summary>
public enum FourNodeBranchContinuityCorrectedCommitReason
{
    CommitArmDisabled = 0,
    NotTriggered = 1,
    H20ActivationArmDisabled = 2,
    H20RollbackRequired = 3,
    H20AuthorityDenied = 4,
    ShadowCorrectionNotEvaluated = 5,
    CorrectedCandidateUnavailable = 6,
    QualifiedH20Authority = 10,
}
