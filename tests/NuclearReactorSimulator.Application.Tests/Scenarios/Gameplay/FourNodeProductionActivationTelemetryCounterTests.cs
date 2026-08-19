using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Simulation.Plant;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

public sealed class FourNodeProductionActivationTelemetryCounterTests
{
    private static readonly FourNodeBranchContinuityActivationReason[] RollbackReasons =
    {
        FourNodeBranchContinuityActivationReason.RollbackQualificationEvidenceUnavailable,
        FourNodeBranchContinuityActivationReason.RollbackCorrectorNonConvergence,
        FourNodeBranchContinuityActivationReason.RollbackLineSearchExhausted,
        FourNodeBranchContinuityActivationReason.RollbackPressureResidualExceeded,
        FourNodeBranchContinuityActivationReason.RollbackFlowResidualExceeded,
        FourNodeBranchContinuityActivationReason.RollbackMassClosureExceeded,
        FourNodeBranchContinuityActivationReason.RollbackEnergyOwnershipExceeded,
        FourNodeBranchContinuityActivationReason.RollbackUntargetedBranchDisagreement,
    };

    [Fact]
    public void Counter_AccountsQualifiedCommitAndEveryH20RollbackReasonWithoutGrantingAuthority()
    {
        var counter = new FourNodeProductionActivationTelemetryCounter();
        counter.Observe(PlantNetworkHydraulicNumericalSnapshot.Explicit);
        counter.Observe(CreateQualifiedCommit());
        foreach (var reason in RollbackReasons)
        {
            counter.Observe(CreateRollback(reason));
        }

        var snapshot = counter.Snapshot();
        Assert.Equal(10, snapshot.ObservedSteps);
        Assert.Equal(9, snapshot.FourNodeTelemetrySteps);
        Assert.Equal(9, snapshot.TriggeredSteps);
        Assert.Equal(1, snapshot.CandidateEligibleSteps);
        Assert.Equal(1, snapshot.CommitAuthorizedSteps);
        Assert.Equal(1, snapshot.CorrectedCommittedSteps);
        Assert.Equal(8, snapshot.ExplicitFallbackSteps);
        Assert.Equal(8, snapshot.RollbackSteps);
        Assert.Equal(0, snapshot.FallbackCommitViolations);
        Assert.Equal(0, snapshot.UnsafeCommitViolations);
        Assert.Equal(1, snapshot.UntargetedBranchDisagreementSteps);
        Assert.Equal(8, snapshot.RollbackReasonCounts.Count);
        Assert.All(RollbackReasons, reason => Assert.Equal(1, snapshot.RollbackReasonCounts[reason]));
        Assert.Equal(1, snapshot.CommitReasonCounts[FourNodeBranchContinuityCorrectedCommitReason.QualifiedH20Authority]);
        Assert.Equal(8, snapshot.CommitReasonCounts[FourNodeBranchContinuityCorrectedCommitReason.H20RollbackRequired]);
    }

    [Fact]
    public void OperatorSnapshot_DoesNotExposeInternalFourNodeProductionDiagnostics()
    {
        var properties = typeof(ControlRoomSnapshot).GetProperties();
        Assert.DoesNotContain(properties, static property =>
            property.Name.Contains("HydraulicNumerical", StringComparison.Ordinal)
            || property.Name.Contains("FourNode", StringComparison.Ordinal)
            || property.PropertyType.FullName?.Contains("FourNodeBranchContinuity", StringComparison.Ordinal) == true
            || property.PropertyType.FullName?.Contains("ProductionActivationTelemetry", StringComparison.Ordinal) == true);
    }

    private static PlantNetworkHydraulicNumericalSnapshot CreateQualifiedCommit()
        => CreateSnapshot(new FourNodeBranchContinuityIntegrationTelemetry(
            TriggerObserved: true,
            ShadowCorrectionEvaluated: true,
            ProposedAuthority: FourNodeBranchContinuityProposedAuthority.CorrectedCandidate,
            Reason: FourNodeBranchContinuityActivationReason.QualifiedTriggeredCorrection,
            RollbackRequired: false,
            ShadowCorrectedCandidateEligible: true,
            CorrectedCandidateCommitted: true,
            UntargetedBranchDisagreementDetected: false,
            BranchOverrideCount: 1,
            PreviousPhaseHoldCount: 1,
            HysteresisReleaseCount: 0,
            ShadowIterationCount: 2,
            ShadowConverged: true,
            ShadowLineSearchExhausted: false,
            ShadowMaximumRelativePressureResidual: 1e-6d,
            ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond: 1e-3d,
            ShadowMassClosureKilogramsPerSecond: 0d,
            ShadowEnergyOwnershipResidualWatts: 0d)
        {
            CorrectedCommitArmEnabled = true,
            CorrectedCommitAuthorized = true,
            CorrectedCommitReason = FourNodeBranchContinuityCorrectedCommitReason.QualifiedH20Authority,
        });

    private static PlantNetworkHydraulicNumericalSnapshot CreateRollback(FourNodeBranchContinuityActivationReason reason)
        => CreateSnapshot(new FourNodeBranchContinuityIntegrationTelemetry(
            TriggerObserved: true,
            ShadowCorrectionEvaluated: true,
            ProposedAuthority: FourNodeBranchContinuityProposedAuthority.ExplicitCommittedState,
            Reason: reason,
            RollbackRequired: true,
            ShadowCorrectedCandidateEligible: false,
            CorrectedCandidateCommitted: false,
            UntargetedBranchDisagreementDetected: reason == FourNodeBranchContinuityActivationReason.RollbackUntargetedBranchDisagreement,
            BranchOverrideCount: 0,
            PreviousPhaseHoldCount: 0,
            HysteresisReleaseCount: 0,
            ShadowIterationCount: 2,
            ShadowConverged: reason != FourNodeBranchContinuityActivationReason.RollbackCorrectorNonConvergence,
            ShadowLineSearchExhausted: reason == FourNodeBranchContinuityActivationReason.RollbackLineSearchExhausted,
            ShadowMaximumRelativePressureResidual: reason == FourNodeBranchContinuityActivationReason.RollbackPressureResidualExceeded ? 1e-3d : 0d,
            ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond: reason == FourNodeBranchContinuityActivationReason.RollbackFlowResidualExceeded ? 1d : 0d,
            ShadowMassClosureKilogramsPerSecond: reason == FourNodeBranchContinuityActivationReason.RollbackMassClosureExceeded ? 1e-3d : 0d,
            ShadowEnergyOwnershipResidualWatts: reason == FourNodeBranchContinuityActivationReason.RollbackEnergyOwnershipExceeded ? 1d : 0d)
        {
            CorrectedCommitArmEnabled = true,
            CorrectedCommitAuthorized = false,
            CorrectedCommitReason = FourNodeBranchContinuityCorrectedCommitReason.H20RollbackRequired,
        });

    private static PlantNetworkHydraulicNumericalSnapshot CreateSnapshot(FourNodeBranchContinuityIntegrationTelemetry telemetry)
        => new(
            NuclearReactorSimulator.Domain.Plant.HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn,
            UsedSemiImplicitCorrection: telemetry.CorrectedCandidateCommitted,
            IterationCount: telemetry.ShadowIterationCount,
            Converged: telemetry.ShadowConverged,
            PredictorMaximumFractionalSubcooledPressureChange: 0.1d,
            PredictorMaximumAbsoluteHydraulicFlowChangeKilogramsPerSecond: 50d,
            MaximumRelativePressureResidual: telemetry.ShadowMaximumRelativePressureResidual,
            MaximumAbsoluteFlowResidualKilogramsPerSecond: telemetry.ShadowMaximumAbsoluteFlowResidualKilogramsPerSecond)
        {
            FourNodeBranchContinuity = telemetry,
        };
}
