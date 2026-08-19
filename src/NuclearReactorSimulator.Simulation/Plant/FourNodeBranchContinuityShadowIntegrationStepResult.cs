namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// H.21/H.22 result for the integrated four-node correction evaluator. H.21 keeps the corrected candidate
/// observational; H.22 may pass it to a separate commit seam without changing the H.20 authority contract.
/// </summary>
public sealed record FourNodeBranchContinuityShadowIntegrationStepResult(
    HybridSemiImplicitHydraulicGateStepResult Predictor,
    FourNodeBranchContinuityActivationDecision AuthorityDecision,
    bool ShadowCorrectionEvaluated,
    bool UntargetedBranchDisagreementDetected,
    IReadOnlyList<string> UntargetedBranchDisagreementNodeIds,
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
    /// <summary>
    /// Full H.9 corrected result when a trigger was evaluated; null on untriggered intervals. H.21 observes this
    /// only. H.22 may consume it only after a separate fail-closed commit decision.
    /// </summary>
    public JacobianHydraulicCorrectorStepResult? CorrectedCandidate { get; init; }

    public bool CorrectedCandidateCommitted => false;
}
