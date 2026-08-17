namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Numerical controls for the Phase H.10 shadow-only hydraulic-map smoothness diagnosis.
/// These values define observational probe amplitudes only; they do not alter production physics,
/// nonlinear-corrector tolerances, the fixed timestep, or trigger thresholds.
/// </summary>
public sealed record HydraulicMapSmoothnessProbeOptions
{
    public HydraulicMapSmoothnessProbeOptions(
        double relativePressureProbe,
        double relativeInventoryProbe,
        double fineProbeFactor,
        double derivativeScaleGrowthThreshold,
        double oneSidedSlopeAsymmetryThreshold)
    {
        if (!double.IsFinite(relativePressureProbe) || relativePressureProbe <= 0d || relativePressureProbe > 0.01d)
        {
            throw new ArgumentOutOfRangeException(nameof(relativePressureProbe), relativePressureProbe, "Relative pressure probe must be finite and in (0, 0.01].");
        }

        if (!double.IsFinite(relativeInventoryProbe) || relativeInventoryProbe <= 0d || relativeInventoryProbe > 0.01d)
        {
            throw new ArgumentOutOfRangeException(nameof(relativeInventoryProbe), relativeInventoryProbe, "Relative inventory probe must be finite and in (0, 0.01].");
        }

        if (!double.IsFinite(fineProbeFactor) || fineProbeFactor <= 0d || fineProbeFactor >= 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(fineProbeFactor), fineProbeFactor, "Fine-probe factor must be finite and in (0, 1).");
        }

        if (!double.IsFinite(derivativeScaleGrowthThreshold) || derivativeScaleGrowthThreshold <= 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(derivativeScaleGrowthThreshold), derivativeScaleGrowthThreshold, "Derivative scale-growth threshold must be finite and greater than one.");
        }

        if (!double.IsFinite(oneSidedSlopeAsymmetryThreshold) || oneSidedSlopeAsymmetryThreshold <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(oneSidedSlopeAsymmetryThreshold), oneSidedSlopeAsymmetryThreshold, "One-sided slope-asymmetry threshold must be finite and positive.");
        }

        RelativePressureProbe = relativePressureProbe;
        RelativeInventoryProbe = relativeInventoryProbe;
        FineProbeFactor = fineProbeFactor;
        DerivativeScaleGrowthThreshold = derivativeScaleGrowthThreshold;
        OneSidedSlopeAsymmetryThreshold = oneSidedSlopeAsymmetryThreshold;
    }

    public static HydraulicMapSmoothnessProbeOptions H10AuditDefault { get; } = new(
        relativePressureProbe: 1e-6d,
        relativeInventoryProbe: 1e-6d,
        fineProbeFactor: 0.25d,
        derivativeScaleGrowthThreshold: 1.5d,
        oneSidedSlopeAsymmetryThreshold: 0.25d);

    public double RelativePressureProbe { get; }

    public double RelativeInventoryProbe { get; }

    public double FineProbeFactor { get; }

    public double DerivativeScaleGrowthThreshold { get; }

    public double OneSidedSlopeAsymmetryThreshold { get; }
}
