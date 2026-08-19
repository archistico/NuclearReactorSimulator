namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Deterministic, fail-closed H.20 authority supervisor for the H.19-qualified four-node branch-continuity policy.
/// H.20 introduced it as shadow-only; H.21/H.22 consume the unchanged decision contract from the orchestrator.
/// </summary>
public sealed class FourNodeBranchContinuityShadowActivationSupervisor
{
    public FourNodeBranchContinuityActivationDecision Evaluate(
        FourNodeBranchContinuityActivationObservation observation,
        FourNodeBranchContinuityActivationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(observation);
        options ??= FourNodeBranchContinuityActivationOptions.H19QualifiedShadowOnly;

        if (!observation.TriggerObserved)
        {
            return Explicit(
                observation,
                options,
                FourNodeBranchContinuityActivationReason.NotTriggered,
                rollbackRequired: false);
        }

        if (!options.ActivationArmEnabled)
        {
            return Explicit(
                observation,
                options,
                FourNodeBranchContinuityActivationReason.ActivationArmDisabled,
                rollbackRequired: false);
        }

        var rollbackReason = GetRollbackReason(observation, options);
        if (rollbackReason is not null)
        {
            return Explicit(observation, options, rollbackReason.Value, rollbackRequired: true);
        }

        return new FourNodeBranchContinuityActivationDecision(
            observation.SampleId,
            FourNodeBranchContinuityProposedAuthority.CorrectedCandidate,
            FourNodeBranchContinuityActivationReason.QualifiedTriggeredCorrection,
            RollbackRequired: false,
            observation.TriggerObserved,
            options.ActivationArmEnabled);
    }

    private static FourNodeBranchContinuityActivationReason? GetRollbackReason(
        FourNodeBranchContinuityActivationObservation observation,
        FourNodeBranchContinuityActivationOptions options)
    {
        if (!observation.QualificationEvidenceAccepted)
        {
            return FourNodeBranchContinuityActivationReason.RollbackQualificationEvidenceUnavailable;
        }

        if (!observation.CorrectorConverged)
        {
            return FourNodeBranchContinuityActivationReason.RollbackCorrectorNonConvergence;
        }

        if (observation.LineSearchExhausted)
        {
            return FourNodeBranchContinuityActivationReason.RollbackLineSearchExhausted;
        }

        if (observation.RelativePressureResidual > options.MaximumRelativePressureResidual)
        {
            return FourNodeBranchContinuityActivationReason.RollbackPressureResidualExceeded;
        }

        if (observation.AbsoluteFlowResidualKilogramsPerSecond > options.MaximumAbsoluteFlowResidualKilogramsPerSecond)
        {
            return FourNodeBranchContinuityActivationReason.RollbackFlowResidualExceeded;
        }

        if (observation.MassClosureKilogramsPerSecond > options.MaximumMassClosureKilogramsPerSecond)
        {
            return FourNodeBranchContinuityActivationReason.RollbackMassClosureExceeded;
        }

        if (observation.EnergyOwnershipResidualWatts > options.MaximumEnergyOwnershipResidualWatts)
        {
            return FourNodeBranchContinuityActivationReason.RollbackEnergyOwnershipExceeded;
        }

        if (observation.UntargetedBranchDisagreementDetected)
        {
            return FourNodeBranchContinuityActivationReason.RollbackUntargetedBranchDisagreement;
        }

        return null;
    }

    private static FourNodeBranchContinuityActivationDecision Explicit(
        FourNodeBranchContinuityActivationObservation observation,
        FourNodeBranchContinuityActivationOptions options,
        FourNodeBranchContinuityActivationReason reason,
        bool rollbackRequired)
        => new(
            observation.SampleId,
            FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState,
            reason,
            rollbackRequired,
            observation.TriggerObserved,
            options.ActivationArmEnabled);
}
