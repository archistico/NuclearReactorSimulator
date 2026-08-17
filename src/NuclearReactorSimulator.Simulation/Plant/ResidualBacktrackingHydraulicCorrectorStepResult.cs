using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Result of one isolated H.7 residual-based nonlinear hydraulic correction.
/// The result is shadow evidence only: no production orchestrator consumes this type.
/// </summary>
public sealed record ResidualBacktrackingHydraulicCorrectorStepResult(
    PlantState CandidateState,
    SemiImplicitHydraulicEvaluation HydraulicEvaluation,
    IReadOnlyDictionary<string, FluidNodeBalance> AppliedHydraulicBalances,
    double AppliedHydraulicMassRateClosureResidualKilogramsPerSecond,
    double AppliedHydraulicEnergyOwnershipResidualWatts,
    int IterationCount,
    bool Converged,
    bool LineSearchExhausted,
    double MaximumRelativePressureFixedPointResidual,
    double MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond,
    double NormalizedMeritResidual,
    int HydraulicEvaluationCount,
    int BacktrackingTrialCount,
    double MinimumAcceptedRelaxationFactor,
    IReadOnlyList<ResidualBacktrackingHydraulicIteration> Iterations);
