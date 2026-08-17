namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Deterministic numerical controls for the Phase H.7 residual-based shadow hydraulic corrector.
/// These controls affect only the nonlinear iteration; they do not change any physical coefficient,
/// production routing, logical timestep or H.4 trigger threshold.
/// </summary>
public sealed record ResidualBacktrackingHydraulicCorrectorOptions
{
    public ResidualBacktrackingHydraulicCorrectorOptions(
        int maximumIterations,
        double initialRelaxationFactor,
        double backtrackingFactor,
        double minimumRelaxationFactor,
        double relativePressureTolerance,
        double absoluteFlowToleranceKilogramsPerSecond)
    {
        if (maximumIterations < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumIterations), maximumIterations, "At least one residual-corrector iteration is required.");
        }

        if (!double.IsFinite(initialRelaxationFactor) || initialRelaxationFactor <= 0d || initialRelaxationFactor > 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialRelaxationFactor),
                initialRelaxationFactor,
                "Initial relaxation factor must be finite and in (0, 1].");
        }

        if (!double.IsFinite(backtrackingFactor) || backtrackingFactor <= 0d || backtrackingFactor >= 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(backtrackingFactor),
                backtrackingFactor,
                "Backtracking factor must be finite and in (0, 1).");
        }

        if (!double.IsFinite(minimumRelaxationFactor)
            || minimumRelaxationFactor <= 0d
            || minimumRelaxationFactor > initialRelaxationFactor)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumRelaxationFactor),
                minimumRelaxationFactor,
                "Minimum relaxation factor must be finite, positive and no greater than the initial relaxation factor.");
        }

        if (!double.IsFinite(relativePressureTolerance) || relativePressureTolerance <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(relativePressureTolerance),
                relativePressureTolerance,
                "Relative pressure tolerance must be finite and greater than zero.");
        }

        if (!double.IsFinite(absoluteFlowToleranceKilogramsPerSecond) || absoluteFlowToleranceKilogramsPerSecond <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(absoluteFlowToleranceKilogramsPerSecond),
                absoluteFlowToleranceKilogramsPerSecond,
                "Absolute flow tolerance must be finite and greater than zero.");
        }

        MaximumIterations = maximumIterations;
        InitialRelaxationFactor = initialRelaxationFactor;
        BacktrackingFactor = backtrackingFactor;
        MinimumRelaxationFactor = minimumRelaxationFactor;
        RelativePressureTolerance = relativePressureTolerance;
        AbsoluteFlowToleranceKilogramsPerSecond = absoluteFlowToleranceKilogramsPerSecond;
    }

    public static ResidualBacktrackingHydraulicCorrectorOptions H7AuditDefault { get; } = new(
        maximumIterations: 96,
        initialRelaxationFactor: 1d,
        backtrackingFactor: 0.5d,
        minimumRelaxationFactor: 1d / 1024d,
        relativePressureTolerance: 1e-5d,
        absoluteFlowToleranceKilogramsPerSecond: 1e-2d);

    public int MaximumIterations { get; }

    public double InitialRelaxationFactor { get; }

    public double BacktrackingFactor { get; }

    public double MinimumRelaxationFactor { get; }

    public double RelativePressureTolerance { get; }

    public double AbsoluteFlowToleranceKilogramsPerSecond { get; }
}
