namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Deterministic reason emitted by the H.20 shadow activation supervisor.
/// Rollback reasons are deliberately explicit so a future production integration cannot hide fallback.
/// </summary>
public enum FourNodeBranchContinuityActivationReason
{
    NotTriggered = 0,
    ActivationArmDisabled = 1,
    QualifiedTriggeredCorrection = 2,
    RollbackQualificationEvidenceUnavailable = 10,
    RollbackCorrectorNonConvergence = 11,
    RollbackLineSearchExhausted = 12,
    RollbackPressureResidualExceeded = 13,
    RollbackFlowResidualExceeded = 14,
    RollbackMassClosureExceeded = 15,
    RollbackEnergyOwnershipExceeded = 16,
    RollbackUntargetedBranchDisagreement = 17,
}
