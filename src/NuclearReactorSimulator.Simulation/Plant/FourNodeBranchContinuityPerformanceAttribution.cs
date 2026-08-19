namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// H.28.1-A diagnostic-only attribution for the four-node corrected-commit orchestration path.
/// Timings and allocation counts are observational and never affect trigger, authority, commit or physics.
/// </summary>
internal sealed record FourNodeBranchContinuityPerformanceAttribution(
    long OrchestratorElapsedTicks,
    long OrchestratorAllocatedBytes,
    long HistoricalExplicitPreparationElapsedTicks,
    long HistoricalExplicitPreparationAllocatedBytes,
    long SidecarElapsedTicks,
    long SidecarAllocatedBytes,
    long PredictorElapsedTicks,
    long PredictorAllocatedBytes,
    long CorrectorElapsedTicks,
    long CorrectorAllocatedBytes,
    long UntargetedDisagreementScanElapsedTicks,
    long UntargetedDisagreementScanAllocatedBytes,
    long AuthorityEvaluationElapsedTicks,
    long AuthorityEvaluationAllocatedBytes,
    long CommitAndAccountingElapsedTicks,
    long CommitAndAccountingAllocatedBytes,
    int HydraulicEvaluationCount,
    int ProbeEvaluationCount,
    int MaximumJacobianDimension,
    int JacobianBuildAttempts,
    int JacobianDirectionAcceptances,
    int JacobianRejectedCount,
    int ResidualFallbackAttempts,
    int ResidualFallbackAcceptances,
    int BacktrackingTrialCount)
{
    public JacobianHydraulicCorrectorPerformanceAttribution? H9 { get; init; }

    /// <summary>
    /// H.28.1-B diagnostic-only count of predictor fluid nodes reused from the historical explicit
    /// candidate because the historical applied total balance exactly matched the canonical H.4 balance.
    /// </summary>
    public int HistoricalPredictorFluidNodeReuseCount { get; init; }

    /// <summary>
    /// H.28.1-B diagnostic-only total number of predictor fluid nodes considered for exact reuse.
    /// </summary>
    public int HistoricalPredictorFluidNodeCount { get; init; }
}
