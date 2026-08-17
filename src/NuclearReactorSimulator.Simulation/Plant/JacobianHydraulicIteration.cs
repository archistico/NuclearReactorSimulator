namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Accepted-iterate evidence emitted by the H.9 Jacobian-informed hydraulic corrector.
/// Iteration one is the explicit predictor. Later iterations identify Newton or residual-fallback directions.
/// </summary>
public sealed record JacobianHydraulicIteration(
    int IterationIndex,
    string DirectionKind,
    double AcceptedRelaxationFactor,
    int BacktrackingTrials,
    int JacobianDimension,
    int ProbeEvaluations,
    double PivotConditionEstimate,
    double NormalizedNewtonStepInfinityNorm,
    double CoordinateResidualInfinityNorm,
    double MaximumRelativePressureFixedPointResidual,
    double MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond,
    double NormalizedMeritResidual);
