namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Accepted-iterate evidence emitted by the H.8 safeguarded Anderson corrector.
/// Iteration one is the explicit predictor. Later iterations record whether the accepted direction
/// came from Anderson acceleration or the deterministic residual fallback.
/// </summary>
public sealed record AndersonHydraulicIteration(
    int IterationIndex,
    string DirectionKind,
    int HistorySampleCount,
    double AcceptedRelaxationFactor,
    int BacktrackingTrials,
    double AndersonCoefficientL1Norm,
    double MaximumRelativePressureFixedPointResidual,
    double MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond,
    double NormalizedMeritResidual);
