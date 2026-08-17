namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Immutable H.20 shadow activation contract derived from the user-validated H.19 evidence.
/// These are numerical authority/guard controls, not physical plant coefficients.
/// </summary>
public sealed record FourNodeBranchContinuityActivationOptions
{
    private static readonly IReadOnlyList<string> FrozenTargetNodeIds = Array.AsReadOnly(
        new[] { "steam", "stop-out", "header", "turbine-inlet" });

    public FourNodeBranchContinuityActivationOptions(
        bool activationArmEnabled,
        double predictedPressureChangeTriggerFraction,
        double predictedFlowChangeTriggerKilogramsPerSecond,
        double maximumRelativePressureResidual,
        double maximumAbsoluteFlowResidualKilogramsPerSecond,
        double maximumMassClosureKilogramsPerSecond,
        double maximumEnergyOwnershipResidualWatts)
    {
        ValidatePositiveFinite(predictedPressureChangeTriggerFraction, nameof(predictedPressureChangeTriggerFraction));
        ValidatePositiveFinite(predictedFlowChangeTriggerKilogramsPerSecond, nameof(predictedFlowChangeTriggerKilogramsPerSecond));
        ValidatePositiveFinite(maximumRelativePressureResidual, nameof(maximumRelativePressureResidual));
        ValidatePositiveFinite(maximumAbsoluteFlowResidualKilogramsPerSecond, nameof(maximumAbsoluteFlowResidualKilogramsPerSecond));
        ValidatePositiveFinite(maximumMassClosureKilogramsPerSecond, nameof(maximumMassClosureKilogramsPerSecond));
        ValidatePositiveFinite(maximumEnergyOwnershipResidualWatts, nameof(maximumEnergyOwnershipResidualWatts));

        ActivationArmEnabled = activationArmEnabled;
        PredictedPressureChangeTriggerFraction = predictedPressureChangeTriggerFraction;
        PredictedFlowChangeTriggerKilogramsPerSecond = predictedFlowChangeTriggerKilogramsPerSecond;
        MaximumRelativePressureResidual = maximumRelativePressureResidual;
        MaximumAbsoluteFlowResidualKilogramsPerSecond = maximumAbsoluteFlowResidualKilogramsPerSecond;
        MaximumMassClosureKilogramsPerSecond = maximumMassClosureKilogramsPerSecond;
        MaximumEnergyOwnershipResidualWatts = maximumEnergyOwnershipResidualWatts;
    }

    /// <summary>
    /// H.19-qualified values with the activation arm intentionally disabled. This is the H.20 default.
    /// </summary>
    public static FourNodeBranchContinuityActivationOptions H19QualifiedShadowOnly { get; } = new(
        activationArmEnabled: false,
        predictedPressureChangeTriggerFraction: 0.060d,
        predictedFlowChangeTriggerKilogramsPerSecond: 40d,
        maximumRelativePressureResidual: 1e-5d,
        maximumAbsoluteFlowResidualKilogramsPerSecond: 1e-2d,
        maximumMassClosureKilogramsPerSecond: 1e-8d,
        maximumEnergyOwnershipResidualWatts: 1e-3d);

    public bool ActivationArmEnabled { get; }

    public double PredictedPressureChangeTriggerFraction { get; }

    public double PredictedFlowChangeTriggerKilogramsPerSecond { get; }

    public double MaximumRelativePressureResidual { get; }

    public double MaximumAbsoluteFlowResidualKilogramsPerSecond { get; }

    public double MaximumMassClosureKilogramsPerSecond { get; }

    public double MaximumEnergyOwnershipResidualWatts { get; }

    public IReadOnlyList<string> TargetNodeIds => FrozenTargetNodeIds;

    public FourNodeBranchContinuityActivationOptions WithActivationArmEnabled(bool enabled)
        => new(
            enabled,
            PredictedPressureChangeTriggerFraction,
            PredictedFlowChangeTriggerKilogramsPerSecond,
            MaximumRelativePressureResidual,
            MaximumAbsoluteFlowResidualKilogramsPerSecond,
            MaximumMassClosureKilogramsPerSecond,
            MaximumEnergyOwnershipResidualWatts);

    private static void ValidatePositiveFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0d)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Activation-contract control must be finite and greater than zero.");
        }
    }
}
