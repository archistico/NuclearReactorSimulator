namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// H.22 deterministic, fail-closed commit seam. It consumes the unchanged H.20 authority decision and never
/// broadens its eligibility: only a qualified, triggered, non-rollback corrected candidate can be committed.
/// </summary>
public sealed class FourNodeBranchContinuityCorrectedCommitSeam
{
    public FourNodeBranchContinuityCorrectedCommitDecision Evaluate(
        FourNodeBranchContinuityActivationDecision authorityDecision,
        bool shadowCorrectionEvaluated,
        bool correctedCandidateAvailable,
        bool commitArmEnabled)
    {
        ArgumentNullException.ThrowIfNull(authorityDecision);

        if (!commitArmEnabled)
        {
            return Explicit(commitArmEnabled, correctedCandidateAvailable, FourNodeBranchContinuityCorrectedCommitReason.CommitArmDisabled);
        }

        if (!authorityDecision.TriggerObserved)
        {
            return Explicit(commitArmEnabled, correctedCandidateAvailable, FourNodeBranchContinuityCorrectedCommitReason.NotTriggered);
        }

        if (!authorityDecision.ActivationArmEnabled)
        {
            return Explicit(commitArmEnabled, correctedCandidateAvailable, FourNodeBranchContinuityCorrectedCommitReason.H20ActivationArmDisabled);
        }

        if (authorityDecision.RollbackRequired)
        {
            return Explicit(commitArmEnabled, correctedCandidateAvailable, FourNodeBranchContinuityCorrectedCommitReason.H20RollbackRequired);
        }

        if (!authorityDecision.ShadowCorrectedCandidateEligible
            || authorityDecision.Reason != FourNodeBranchContinuityActivationReason.QualifiedTriggeredCorrection)
        {
            return Explicit(commitArmEnabled, correctedCandidateAvailable, FourNodeBranchContinuityCorrectedCommitReason.H20AuthorityDenied);
        }

        if (!shadowCorrectionEvaluated)
        {
            return Explicit(commitArmEnabled, correctedCandidateAvailable, FourNodeBranchContinuityCorrectedCommitReason.ShadowCorrectionNotEvaluated);
        }

        if (!correctedCandidateAvailable)
        {
            return Explicit(commitArmEnabled, correctedCandidateAvailable, FourNodeBranchContinuityCorrectedCommitReason.CorrectedCandidateUnavailable);
        }

        return new FourNodeBranchContinuityCorrectedCommitDecision(
            commitArmEnabled,
            correctedCandidateAvailable,
            CommitAuthorized: true,
            FourNodeBranchContinuityCorrectedCommitReason.QualifiedH20Authority);
    }

    private static FourNodeBranchContinuityCorrectedCommitDecision Explicit(
        bool commitArmEnabled,
        bool correctedCandidateAvailable,
        FourNodeBranchContinuityCorrectedCommitReason reason)
        => new(commitArmEnabled, correctedCandidateAvailable, CommitAuthorized: false, reason);
}
