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
