namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Deterministic numerical controls for the Phase H.9 shadow-only Jacobian-informed hydraulic corrector.
/// These controls affect only nonlinear root finding and safeguarding; they do not alter physical coefficients,
/// production routing, logical timestep or the frozen H.4 trigger thresholds.
/// </summary>
public sealed record JacobianHydraulicCorrectorOptions
{
    public JacobianHydraulicCorrectorOptions(
        int maximumIterations,
        double finiteDifferenceRelativeStep,
        double jacobianDiagonalRegularization,
        double maximumPivotConditionEstimate,
        double maximumNormalizedNewtonStep,
        double initialRelaxationFactor,
        double backtrackingFactor,
        double minimumRelaxationFactor,
        double relativePressureTolerance,
        double absoluteFlowToleranceKilogramsPerSecond)
    {
        if (maximumIterations < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumIterations), maximumIterations, "At least one Jacobian-corrector iteration is required.");
        }

        if (!double.IsFinite(finiteDifferenceRelativeStep) || finiteDifferenceRelativeStep <= 0d || finiteDifferenceRelativeStep > 0.1d)
        {
            throw new ArgumentOutOfRangeException(nameof(finiteDifferenceRelativeStep), finiteDifferenceRelativeStep, "Finite-difference relative step must be finite and in (0, 0.1].");
        }

        if (!double.IsFinite(jacobianDiagonalRegularization) || jacobianDiagonalRegularization < 0d || jacobianDiagonalRegularization > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(jacobianDiagonalRegularization), jacobianDiagonalRegularization, "Jacobian diagonal regularization must be finite and in [0, 1].");
        }

        if (!double.IsFinite(maximumPivotConditionEstimate) || maximumPivotConditionEstimate < 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumPivotConditionEstimate), maximumPivotConditionEstimate, "Maximum pivot condition estimate must be finite and at least one.");
        }

        if (!double.IsFinite(maximumNormalizedNewtonStep) || maximumNormalizedNewtonStep <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumNormalizedNewtonStep), maximumNormalizedNewtonStep, "Maximum normalized Newton step must be finite and greater than zero.");
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
        FiniteDifferenceRelativeStep = finiteDifferenceRelativeStep;
        JacobianDiagonalRegularization = jacobianDiagonalRegularization;
        MaximumPivotConditionEstimate = maximumPivotConditionEstimate;
        MaximumNormalizedNewtonStep = maximumNormalizedNewtonStep;
        InitialRelaxationFactor = initialRelaxationFactor;
        BacktrackingFactor = backtrackingFactor;
        MinimumRelaxationFactor = minimumRelaxationFactor;
        RelativePressureTolerance = relativePressureTolerance;
        AbsoluteFlowToleranceKilogramsPerSecond = absoluteFlowToleranceKilogramsPerSecond;
    }

    public static JacobianHydraulicCorrectorOptions H9AuditDefault { get; } = new(
        maximumIterations: 24,
        finiteDifferenceRelativeStep: 1e-4d,
        jacobianDiagonalRegularization: 1e-8d,
        maximumPivotConditionEstimate: 1e12d,
        maximumNormalizedNewtonStep: 8d,
        initialRelaxationFactor: 1d,
        backtrackingFactor: 0.5d,
        minimumRelaxationFactor: 1d / 1024d,
        relativePressureTolerance: 1e-5d,
        absoluteFlowToleranceKilogramsPerSecond: 1e-2d);

    public int MaximumIterations { get; }

    public double FiniteDifferenceRelativeStep { get; }

    public double JacobianDiagonalRegularization { get; }

    public double MaximumPivotConditionEstimate { get; }

    public double MaximumNormalizedNewtonStep { get; }

    public double InitialRelaxationFactor { get; }

    public double BacktrackingFactor { get; }

    public double MinimumRelaxationFactor { get; }

    public double RelativePressureTolerance { get; }

    public double AbsoluteFlowToleranceKilogramsPerSecond { get; }
}
