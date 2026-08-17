namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Deterministic Phase H.4 audit-only trigger thresholds for choosing between the explicit predictor
/// and the H.3 semi-implicit corrector. These are numerical gate controls, not physical coefficients.
/// </summary>
public sealed record HybridSemiImplicitHydraulicGateOptions
{
    public HybridSemiImplicitHydraulicGateOptions(
        double predictedSubcooledPressureChangeTriggerFraction,
        double predictedHydraulicFlowChangeTriggerKilogramsPerSecond,
        SemiImplicitHydraulicPrototypeOptions correctorOptions)
    {
        if (!double.IsFinite(predictedSubcooledPressureChangeTriggerFraction)
            || predictedSubcooledPressureChangeTriggerFraction < 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(predictedSubcooledPressureChangeTriggerFraction),
                predictedSubcooledPressureChangeTriggerFraction,
                "Predicted pressure-change trigger must be finite and non-negative.");
        }

        if (!double.IsFinite(predictedHydraulicFlowChangeTriggerKilogramsPerSecond)
            || predictedHydraulicFlowChangeTriggerKilogramsPerSecond < 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(predictedHydraulicFlowChangeTriggerKilogramsPerSecond),
                predictedHydraulicFlowChangeTriggerKilogramsPerSecond,
                "Predicted hydraulic-flow trigger must be finite and non-negative.");
        }

        ArgumentNullException.ThrowIfNull(correctorOptions);

        PredictedSubcooledPressureChangeTriggerFraction = predictedSubcooledPressureChangeTriggerFraction;
        PredictedHydraulicFlowChangeTriggerKilogramsPerSecond = predictedHydraulicFlowChangeTriggerKilogramsPerSecond;
        CorrectorOptions = correctorOptions;
    }

    public double PredictedSubcooledPressureChangeTriggerFraction { get; }

    public double PredictedHydraulicFlowChangeTriggerKilogramsPerSecond { get; }

    public SemiImplicitHydraulicPrototypeOptions CorrectorOptions { get; }

    public bool RequiresCorrection(
        double predictedSubcooledPressureChangeFraction,
        double predictedHydraulicFlowChangeKilogramsPerSecond)
        => predictedSubcooledPressureChangeFraction >= PredictedSubcooledPressureChangeTriggerFraction
            || predictedHydraulicFlowChangeKilogramsPerSecond >= PredictedHydraulicFlowChangeTriggerKilogramsPerSecond;
}
