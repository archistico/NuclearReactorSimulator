namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Deterministic numerical controls for the Phase H.8 shadow-only Anderson accelerated hydraulic corrector.
/// These controls affect only nonlinear iteration and safeguarding; they do not alter physical coefficients,
/// production routing, logical timestep or the frozen H.4 trigger thresholds.
/// </summary>
public sealed record AndersonHydraulicCorrectorOptions
{
    public AndersonHydraulicCorrectorOptions(
        int maximumIterations,
        int memoryDepth,
        double regularization,
        double maximumCoefficientL1Norm,
        double initialRelaxationFactor,
        double backtrackingFactor,
        double minimumRelaxationFactor,
        double relativePressureTolerance,
        double absoluteFlowToleranceKilogramsPerSecond)
    {
        if (maximumIterations < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumIterations), maximumIterations, "At least one Anderson-corrector iteration is required.");
        }

        if (memoryDepth < 1 || memoryDepth > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(memoryDepth), memoryDepth, "Anderson memory depth must be in [1, 8].");
        }

        if (!double.IsFinite(regularization) || regularization <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(regularization), regularization, "Anderson least-squares regularization must be finite and greater than zero.");
        }

        if (!double.IsFinite(maximumCoefficientL1Norm) || maximumCoefficientL1Norm < 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCoefficientL1Norm), maximumCoefficientL1Norm, "Maximum Anderson coefficient L1 norm must be finite and at least one.");
        }

        if (!double.IsFinite(initialRelaxationFactor) || initialRelaxationFactor <= 0d || initialRelaxationFactor > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(initialRelaxationFactor), initialRelaxationFactor, "Initial relaxation factor must be finite and in (0, 1].");
        }

        if (!double.IsFinite(backtrackingFactor) || backtrackingFactor <= 0d || backtrackingFactor >= 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(backtrackingFactor), backtrackingFactor, "Backtracking factor must be finite and in (0, 1).");
        }

        if (!double.IsFinite(minimumRelaxationFactor)
            || minimumRelaxationFactor <= 0d
            || minimumRelaxationFactor > initialRelaxationFactor)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumRelaxationFactor), minimumRelaxationFactor, "Minimum relaxation factor must be finite, positive and no greater than the initial relaxation factor.");
        }

        if (!double.IsFinite(relativePressureTolerance) || relativePressureTolerance <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(relativePressureTolerance), relativePressureTolerance, "Relative pressure tolerance must be finite and greater than zero.");
        }

        if (!double.IsFinite(absoluteFlowToleranceKilogramsPerSecond) || absoluteFlowToleranceKilogramsPerSecond <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(absoluteFlowToleranceKilogramsPerSecond), absoluteFlowToleranceKilogramsPerSecond, "Absolute flow tolerance must be finite and greater than zero.");
        }

        MaximumIterations = maximumIterations;
        MemoryDepth = memoryDepth;
        Regularization = regularization;
        MaximumCoefficientL1Norm = maximumCoefficientL1Norm;
        InitialRelaxationFactor = initialRelaxationFactor;
        BacktrackingFactor = backtrackingFactor;
        MinimumRelaxationFactor = minimumRelaxationFactor;
        RelativePressureTolerance = relativePressureTolerance;
        AbsoluteFlowToleranceKilogramsPerSecond = absoluteFlowToleranceKilogramsPerSecond;
    }

    public static AndersonHydraulicCorrectorOptions H8AuditDefault { get; } = new(
        maximumIterations: 96,
        memoryDepth: 3,
        regularization: 1e-8d,
        maximumCoefficientL1Norm: 16d,
        initialRelaxationFactor: 1d,
        backtrackingFactor: 0.5d,
        minimumRelaxationFactor: 1d / 1024d,
        relativePressureTolerance: 1e-5d,
        absoluteFlowToleranceKilogramsPerSecond: 1e-2d);

    public int MaximumIterations { get; }

    public int MemoryDepth { get; }

    public double Regularization { get; }

    public double MaximumCoefficientL1Norm { get; }

    public double InitialRelaxationFactor { get; }

    public double BacktrackingFactor { get; }

    public double MinimumRelaxationFactor { get; }

    public double RelativePressureTolerance { get; }

    public double AbsoluteFlowToleranceKilogramsPerSecond { get; }
}
