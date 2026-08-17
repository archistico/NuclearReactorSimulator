namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Deterministic accepted-iterate evidence emitted by the H.7 residual/backtracking corrector.
/// Iteration 1 is the explicit-predictor starting iterate and therefore has relaxation factor zero.
/// Later entries are recorded only after a line-search trial strictly reduces the normalized fixed-point merit residual.
/// </summary>
public sealed record ResidualBacktrackingHydraulicIteration(
    int IterationIndex,
    double AcceptedRelaxationFactor,
    int BacktrackingTrials,
    double MaximumRelativePressureFixedPointResidual,
    double MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond,
    double NormalizedMeritResidual);
