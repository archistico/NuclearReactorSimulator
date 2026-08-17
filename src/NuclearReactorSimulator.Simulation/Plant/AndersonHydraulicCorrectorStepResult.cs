using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Result of one isolated H.8 safeguarded Anderson hydraulic correction.
/// This result is shadow evidence only; no production orchestrator consumes this type.
/// </summary>
public sealed record AndersonHydraulicCorrectorStepResult(
    PlantState CandidateState,
    SemiImplicitHydraulicEvaluation HydraulicEvaluation,
    IReadOnlyDictionary<string, FluidNodeBalance> AppliedHydraulicBalances,
    double AppliedHydraulicMassRateClosureResidualKilogramsPerSecond,
    double AppliedHydraulicEnergyOwnershipResidualWatts,
    int IterationCount,
    bool Converged,
    bool LineSearchExhausted,
    int AndersonDirectionAttempts,
    int AndersonDirectionAcceptances,
    int ResidualFallbackAttempts,
    int ResidualFallbackAcceptances,
    int LeastSquaresRejectedCount,
    double MaximumAndersonCoefficientL1Norm,
    double MaximumRelativePressureFixedPointResidual,
    double MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond,
    double NormalizedMeritResidual,
    int HydraulicEvaluationCount,
    int BacktrackingTrialCount,
    double MinimumAcceptedRelaxationFactor,
    IReadOnlyList<AndersonHydraulicIteration> Iterations);
