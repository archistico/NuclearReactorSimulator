using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Plant;

public sealed class FourNodeBranchContinuityShadowActivationSupervisorTests
{
    [Fact]
    public void H19QualifiedShadowOnly_FreezesValidatedTargetAndNumericalGuardsWithArmDisabled()
    {
        var options = FourNodeBranchContinuityActivationOptions.H19QualifiedShadowOnly;

        Assert.False(options.ActivationArmEnabled);
        Assert.Equal(0.060d, options.PredictedPressureChangeTriggerFraction);
        Assert.Equal(40d, options.PredictedFlowChangeTriggerKilogramsPerSecond);
        Assert.Equal(1e-5d, options.MaximumRelativePressureResidual);
        Assert.Equal(1e-2d, options.MaximumAbsoluteFlowResidualKilogramsPerSecond);
        Assert.Equal(1e-8d, options.MaximumMassClosureKilogramsPerSecond);
        Assert.Equal(1e-3d, options.MaximumEnergyOwnershipResidualWatts);
        Assert.Equal(new[] { "steam", "stop-out", "header", "turbine-inlet" }, options.TargetNodeIds);
    }

    [Fact]
    public void DefaultShadowSupervisor_IsFailClosedAndCannotAuthorizeProductionCommit()
    {
        var supervisor = new FourNodeBranchContinuityShadowActivationSupervisor();

        var decision = supervisor.Evaluate(QualifiedObservation());

        Assert.Equal(FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState, decision.ProposedAuthority);
        Assert.Equal(FourNodeBranchContinuityActivationReason.ActivationArmDisabled, decision.Reason);
        Assert.False(decision.RollbackRequired);
        Assert.False(decision.ShadowCorrectedCandidateEligible);
        Assert.False(decision.ProductionCommitAuthorized);
    }

    [Fact]
    public void ArmedShadowSupervisor_ProposesCorrectedCandidateOnlyForQualifiedTriggeredObservation()
    {
        var supervisor = new FourNodeBranchContinuityShadowActivationSupervisor();
        var options = FourNodeBranchContinuityActivationOptions.H19QualifiedShadowOnly.WithActivationArmEnabled(true);

        var decision = supervisor.Evaluate(QualifiedObservation(), options);

        Assert.Equal(FourNodeBranchContinuityProposedAuthority.CorrectedCandidate, decision.ProposedAuthority);
        Assert.Equal(FourNodeBranchContinuityActivationReason.QualifiedTriggeredCorrection, decision.Reason);
        Assert.False(decision.RollbackRequired);
        Assert.True(decision.ShadowCorrectedCandidateEligible);
        Assert.False(decision.ProductionCommitAuthorized);
    }

    [Fact]
    public void ArmedShadowSupervisor_UntriggeredObservationStaysExplicitWithoutRollback()
    {
        var supervisor = new FourNodeBranchContinuityShadowActivationSupervisor();
        var options = FourNodeBranchContinuityActivationOptions.H19QualifiedShadowOnly.WithActivationArmEnabled(true);
        var observation = Observation(triggerObserved: false);

        var decision = supervisor.Evaluate(observation, options);

        Assert.Equal(FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState, decision.ProposedAuthority);
        Assert.Equal(FourNodeBranchContinuityActivationReason.NotTriggered, decision.Reason);
        Assert.False(decision.RollbackRequired);
        Assert.False(decision.ProductionCommitAuthorized);
    }

    [Theory]
    [MemberData(nameof(RollbackChallenges))]
    public void ArmedShadowSupervisor_AnyFailedGuardRollsBackImmediatelyToExplicit(
        FourNodeBranchContinuityActivationObservation observation,
        FourNodeBranchContinuityActivationReason expectedReason)
    {
        var supervisor = new FourNodeBranchContinuityShadowActivationSupervisor();
        var options = FourNodeBranchContinuityActivationOptions.H19QualifiedShadowOnly.WithActivationArmEnabled(true);

        var decision = supervisor.Evaluate(observation, options);

        Assert.Equal(FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState, decision.ProposedAuthority);
        Assert.Equal(expectedReason, decision.Reason);
        Assert.True(decision.RollbackRequired);
        Assert.False(decision.ShadowCorrectedCandidateEligible);
        Assert.False(decision.ProductionCommitAuthorized);
    }

    public static TheoryData<FourNodeBranchContinuityActivationObservation, FourNodeBranchContinuityActivationReason> RollbackChallenges
        => new()
        {
            {
                Observation(qualificationEvidenceAccepted: false),
                FourNodeBranchContinuityActivationReason.RollbackQualificationEvidenceUnavailable
            },
            {
                Observation(correctorConverged: false),
                FourNodeBranchContinuityActivationReason.RollbackCorrectorNonConvergence
            },
            {
                Observation(lineSearchExhausted: true),
                FourNodeBranchContinuityActivationReason.RollbackLineSearchExhausted
            },
            {
                Observation(relativePressureResidual: 1.0001e-5d),
                FourNodeBranchContinuityActivationReason.RollbackPressureResidualExceeded
            },
            {
                Observation(absoluteFlowResidualKilogramsPerSecond: 1.0001e-2d),
                FourNodeBranchContinuityActivationReason.RollbackFlowResidualExceeded
            },
            {
                Observation(massClosureKilogramsPerSecond: 1.0001e-8d),
                FourNodeBranchContinuityActivationReason.RollbackMassClosureExceeded
            },
            {
                Observation(energyOwnershipResidualWatts: 1.0001e-3d),
                FourNodeBranchContinuityActivationReason.RollbackEnergyOwnershipExceeded
            },
            {
                Observation(untargetedBranchDisagreementDetected: true),
                FourNodeBranchContinuityActivationReason.RollbackUntargetedBranchDisagreement
            },
        };

    private static FourNodeBranchContinuityActivationObservation QualifiedObservation()
        => Observation();

    private static FourNodeBranchContinuityActivationObservation Observation(
        bool triggerObserved = true,
        bool qualificationEvidenceAccepted = true,
        bool correctorConverged = true,
        bool lineSearchExhausted = false,
        double relativePressureResidual = 1e-7d,
        double absoluteFlowResidualKilogramsPerSecond = 1e-4d,
        double massClosureKilogramsPerSecond = 0d,
        double energyOwnershipResidualWatts = 2.39e-7d,
        bool untargetedBranchDisagreementDetected = false)
        => new(
            "synthetic-qualified",
            triggerObserved,
            qualificationEvidenceAccepted,
            correctorConverged,
            lineSearchExhausted,
            relativePressureResidual,
            absoluteFlowResidualKilogramsPerSecond,
            massClosureKilogramsPerSecond,
            energyOwnershipResidualWatts,
            untargetedBranchDisagreementDetected);
}
