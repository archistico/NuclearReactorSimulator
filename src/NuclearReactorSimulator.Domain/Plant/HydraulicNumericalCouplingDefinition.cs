namespace NuclearReactorSimulator.Domain.Plant;

/// <summary>
/// Immutable numerical pressure/flow coupling definition. Legacy definitions default to the historical
/// explicit committed-state solve. Hybrid settings are deterministic and depend only on simulated state.
/// </summary>
public sealed class HydraulicNumericalCouplingDefinition
{
    private HydraulicNumericalCouplingDefinition(
        HydraulicNumericalCouplingMode mode,
        double predictedSubcooledPressureChangeTriggerFraction,
        double predictedHydraulicFlowChangeTriggerKilogramsPerSecond,
        int maximumCorrectorIterations,
        double correctorRelaxationFactor,
        double correctorRelativePressureTolerance,
        double correctorAbsoluteFlowToleranceKilogramsPerSecond)
    {
        Mode = mode;
        PredictedSubcooledPressureChangeTriggerFraction = predictedSubcooledPressureChangeTriggerFraction;
        PredictedHydraulicFlowChangeTriggerKilogramsPerSecond = predictedHydraulicFlowChangeTriggerKilogramsPerSecond;
        MaximumCorrectorIterations = maximumCorrectorIterations;
        CorrectorRelaxationFactor = correctorRelaxationFactor;
        CorrectorRelativePressureTolerance = correctorRelativePressureTolerance;
        CorrectorAbsoluteFlowToleranceKilogramsPerSecond = correctorAbsoluteFlowToleranceKilogramsPerSecond;
    }

    public static HydraulicNumericalCouplingDefinition ExplicitCommittedState { get; } = new(
        HydraulicNumericalCouplingMode.ExplicitCommittedState,
        0d,
        0d,
        1,
        1d,
        0d,
        0d);

    /// <summary>
    /// H.21 opt-in shadow-integrated definition. The production candidate remains the explicit predictor;
    /// the H.19-qualified four-node H.9 correction is evaluated only as an orchestrator sidecar and can never be committed.
    /// The numerical controls are intentionally frozen to the user-validated H.19/H.20 contract.
    /// </summary>
    public static HydraulicNumericalCouplingDefinition H19QualifiedFourNodeBranchContinuityShadowIntegrated { get; } = new(
        HydraulicNumericalCouplingMode.FourNodeBranchContinuityShadowIntegrated,
        0.060d,
        40d,
        24,
        1d,
        1e-5d,
        1e-2d);

    /// <summary>
    /// H.22 separately opt-in corrected-candidate commit definition. It freezes the same H.19/H.20 numerical
    /// controls as H.21; the only new authority is the H.22 commit seam after the unchanged H.20 decision.
    /// Standard current-v2 factories never select this definition.
    /// </summary>
    public static HydraulicNumericalCouplingDefinition H22FourNodeBranchContinuityCorrectedCommitOptIn { get; } = new(
        HydraulicNumericalCouplingMode.FourNodeBranchContinuityCorrectedCommitOptIn,
        0.060d,
        40d,
        24,
        1d,
        1e-5d,
        1e-2d);

    public HydraulicNumericalCouplingMode Mode { get; }

    public double PredictedSubcooledPressureChangeTriggerFraction { get; }

    public double PredictedHydraulicFlowChangeTriggerKilogramsPerSecond { get; }

    public int MaximumCorrectorIterations { get; }

    public double CorrectorRelaxationFactor { get; }

    public double CorrectorRelativePressureTolerance { get; }

    public double CorrectorAbsoluteFlowToleranceKilogramsPerSecond { get; }

    public static HydraulicNumericalCouplingDefinition CreateDeterministicHybridSemiImplicit(
        double predictedSubcooledPressureChangeTriggerFraction,
        double predictedHydraulicFlowChangeTriggerKilogramsPerSecond,
        int maximumCorrectorIterations,
        double correctorRelaxationFactor,
        double correctorRelativePressureTolerance,
        double correctorAbsoluteFlowToleranceKilogramsPerSecond)
    {
        if (!double.IsFinite(predictedSubcooledPressureChangeTriggerFraction)
            || predictedSubcooledPressureChangeTriggerFraction <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(predictedSubcooledPressureChangeTriggerFraction),
                predictedSubcooledPressureChangeTriggerFraction,
                "Hybrid pressure-change trigger must be finite and greater than zero.");
        }

        if (!double.IsFinite(predictedHydraulicFlowChangeTriggerKilogramsPerSecond)
            || predictedHydraulicFlowChangeTriggerKilogramsPerSecond <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(predictedHydraulicFlowChangeTriggerKilogramsPerSecond),
                predictedHydraulicFlowChangeTriggerKilogramsPerSecond,
                "Hybrid hydraulic-flow trigger must be finite and greater than zero.");
        }

        if (maximumCorrectorIterations < 2 || maximumCorrectorIterations > 512)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumCorrectorIterations),
                maximumCorrectorIterations,
                "Hybrid corrector iteration budget must be between 2 and 512 iterations.");
        }

        if (!double.IsFinite(correctorRelaxationFactor)
            || correctorRelaxationFactor <= 0d
            || correctorRelaxationFactor > 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(correctorRelaxationFactor),
                correctorRelaxationFactor,
                "Hybrid corrector relaxation factor must be finite, greater than zero and no greater than one.");
        }

        if (!double.IsFinite(correctorRelativePressureTolerance)
            || correctorRelativePressureTolerance <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(correctorRelativePressureTolerance),
                correctorRelativePressureTolerance,
                "Hybrid relative-pressure tolerance must be finite and greater than zero.");
        }

        if (!double.IsFinite(correctorAbsoluteFlowToleranceKilogramsPerSecond)
            || correctorAbsoluteFlowToleranceKilogramsPerSecond <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(correctorAbsoluteFlowToleranceKilogramsPerSecond),
                correctorAbsoluteFlowToleranceKilogramsPerSecond,
                "Hybrid absolute-flow tolerance must be finite and greater than zero.");
        }

        return new HydraulicNumericalCouplingDefinition(
            HydraulicNumericalCouplingMode.DeterministicHybridSemiImplicit,
            predictedSubcooledPressureChangeTriggerFraction,
            predictedHydraulicFlowChangeTriggerKilogramsPerSecond,
            maximumCorrectorIterations,
            correctorRelaxationFactor,
            correctorRelativePressureTolerance,
            correctorAbsoluteFlowToleranceKilogramsPerSecond);
    }
}
