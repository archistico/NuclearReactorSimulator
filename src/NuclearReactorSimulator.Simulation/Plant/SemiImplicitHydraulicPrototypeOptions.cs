namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Deterministic bounded iteration controls for the Phase H.3 audit-only semi-implicit hydraulic prototype.
/// These values are numerical-method controls only; they do not change any physical component coefficient.
/// </summary>
public sealed record SemiImplicitHydraulicPrototypeOptions
{
    public SemiImplicitHydraulicPrototypeOptions(
        int maximumIterations,
        double relaxationFactor,
        double relativePressureTolerance,
        double absoluteFlowToleranceKilogramsPerSecond)
    {
        if (maximumIterations < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumIterations), maximumIterations, "At least two prototype iterations are required.");
        }

        if (!double.IsFinite(relaxationFactor) || relaxationFactor <= 0d || relaxationFactor > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(relaxationFactor), relaxationFactor, "Relaxation factor must be finite and in (0, 1].");
        }

        if (!double.IsFinite(relativePressureTolerance) || relativePressureTolerance <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(relativePressureTolerance), relativePressureTolerance, "Relative pressure tolerance must be finite and greater than zero.");
        }

        if (!double.IsFinite(absoluteFlowToleranceKilogramsPerSecond) || absoluteFlowToleranceKilogramsPerSecond <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(absoluteFlowToleranceKilogramsPerSecond),
                absoluteFlowToleranceKilogramsPerSecond,
                "Absolute flow tolerance must be finite and greater than zero.");
        }

        MaximumIterations = maximumIterations;
        RelaxationFactor = relaxationFactor;
        RelativePressureTolerance = relativePressureTolerance;
        AbsoluteFlowToleranceKilogramsPerSecond = absoluteFlowToleranceKilogramsPerSecond;
    }

    public static SemiImplicitHydraulicPrototypeOptions H3AuditDefault { get; } = new(
        maximumIterations: 96,
        relaxationFactor: 0.10d,
        relativePressureTolerance: 1e-5d,
        absoluteFlowToleranceKilogramsPerSecond: 1e-2d);

    public int MaximumIterations { get; }

    public double RelaxationFactor { get; }

    public double RelativePressureTolerance { get; }

    public double AbsoluteFlowToleranceKilogramsPerSecond { get; }
}
