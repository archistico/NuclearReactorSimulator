using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Plant;

public sealed class FourNodeBranchContinuityCorrectedCommitSeamTests
{
    private readonly FourNodeBranchContinuityCorrectedCommitSeam _sut = new();

    [Fact]
    public void QualifiedH20Decision_CommitsOnlyWhenH22ArmIsEnabledAndCandidateExists()
    {
        var authority = Qualified();

        var disabled = _sut.Evaluate(authority, shadowCorrectionEvaluated: true, correctedCandidateAvailable: true, commitArmEnabled: false);
        var missing = _sut.Evaluate(authority, shadowCorrectionEvaluated: true, correctedCandidateAvailable: false, commitArmEnabled: true);
        var enabled = _sut.Evaluate(authority, shadowCorrectionEvaluated: true, correctedCandidateAvailable: true, commitArmEnabled: true);

        Assert.False(disabled.CommitAuthorized);
        Assert.Equal(FourNodeBranchContinuityCorrectedCommitReason.CommitArmDisabled, disabled.Reason);
        Assert.False(missing.CommitAuthorized);
        Assert.Equal(FourNodeBranchContinuityCorrectedCommitReason.CorrectedCandidateUnavailable, missing.Reason);
        Assert.True(enabled.CommitAuthorized);
        Assert.Equal(FourNodeBranchContinuityCorrectedCommitReason.QualifiedH20Authority, enabled.Reason);
    }

    [Fact]
    public void H20Rollback_ForcesImmediateExplicitFallback()
    {
        var authority = new FourNodeBranchContinuityActivationDecision(
            "rollback",
            FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState,
            FourNodeBranchContinuityActivationReason.RollbackFlowResidualExceeded,
            RollbackRequired: true,
            TriggerObserved: true,
            ActivationArmEnabled: true);

        var decision = _sut.Evaluate(authority, shadowCorrectionEvaluated: true, correctedCandidateAvailable: true, commitArmEnabled: true);

        Assert.False(decision.CommitAuthorized);
        Assert.Equal(FourNodeBranchContinuityCorrectedCommitReason.H20RollbackRequired, decision.Reason);
    }

    [Fact]
    public void DisabledH20ArmOrDeniedH20Authority_FailsClosedWithTypedReason()
    {
        var disabledH20 = new FourNodeBranchContinuityActivationDecision(
            "disabled-h20",
            FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState,
            FourNodeBranchContinuityActivationReason.ActivationArmDisabled,
            RollbackRequired: false,
            TriggerObserved: true,
            ActivationArmEnabled: false);
        var deniedH20 = new FourNodeBranchContinuityActivationDecision(
            "denied-h20",
            FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState,
            FourNodeBranchContinuityActivationReason.QualifiedTriggeredCorrection,
            RollbackRequired: false,
            TriggerObserved: true,
            ActivationArmEnabled: true);

        var first = _sut.Evaluate(disabledH20, shadowCorrectionEvaluated: true, correctedCandidateAvailable: true, commitArmEnabled: true);
        var second = _sut.Evaluate(deniedH20, shadowCorrectionEvaluated: true, correctedCandidateAvailable: true, commitArmEnabled: true);

        Assert.False(first.CommitAuthorized);
        Assert.Equal(FourNodeBranchContinuityCorrectedCommitReason.H20ActivationArmDisabled, first.Reason);
        Assert.False(second.CommitAuthorized);
        Assert.Equal(FourNodeBranchContinuityCorrectedCommitReason.H20AuthorityDenied, second.Reason);
    }

    [Fact]
    public void UntriggeredOrNonEvaluatedCandidate_FailsClosed()
    {
        var untriggered = new FourNodeBranchContinuityActivationDecision(
            "untriggered",
            FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState,
            FourNodeBranchContinuityActivationReason.NotTriggered,
            RollbackRequired: false,
            TriggerObserved: false,
            ActivationArmEnabled: true);
        var qualified = Qualified();

        var first = _sut.Evaluate(untriggered, shadowCorrectionEvaluated: false, correctedCandidateAvailable: false, commitArmEnabled: true);
        var second = _sut.Evaluate(qualified, shadowCorrectionEvaluated: false, correctedCandidateAvailable: true, commitArmEnabled: true);

        Assert.False(first.CommitAuthorized);
        Assert.Equal(FourNodeBranchContinuityCorrectedCommitReason.NotTriggered, first.Reason);
        Assert.False(second.CommitAuthorized);
        Assert.Equal(FourNodeBranchContinuityCorrectedCommitReason.ShadowCorrectionNotEvaluated, second.Reason);
    }

    private static FourNodeBranchContinuityActivationDecision Qualified()
        => new(
            "qualified",
            FourNodeBranchContinuityProposedAuthority.CorrectedCandidate,
            FourNodeBranchContinuityActivationReason.QualifiedTriggeredCorrection,
            RollbackRequired: false,
            TriggerObserved: true,
            ActivationArmEnabled: true);
}
