using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Result of one isolated H.9 Jacobian-informed hydraulic correction.
/// This result is shadow evidence only; no production orchestrator consumes this type.
/// </summary>
public sealed record JacobianHydraulicCorrectorStepResult(
    PlantState CandidateState,
    SemiImplicitHydraulicEvaluation HydraulicEvaluation,
    IReadOnlyDictionary<string, FluidNodeBalance> AppliedHydraulicBalances,
    double AppliedHydraulicMassRateClosureResidualKilogramsPerSecond,
    double AppliedHydraulicEnergyOwnershipResidualWatts,
    int IterationCount,
    bool Converged,
    bool LineSearchExhausted,
    int JacobianBuildAttempts,
    int JacobianDirectionAcceptances,
    int JacobianRejectedCount,
    int ResidualFallbackAttempts,
    int ResidualFallbackAcceptances,
    int ProbeEvaluationCount,
    int MaximumJacobianDimension,
    double MaximumPivotConditionEstimate,
    double MaximumNormalizedNewtonStepInfinityNorm,
    double MaximumCoordinateResidualInfinityNorm,
    double MaximumRelativePressureFixedPointResidual,
    double MaximumAbsoluteFlowFixedPointResidualKilogramsPerSecond,
    double NormalizedMeritResidual,
    int HydraulicEvaluationCount,
    int BacktrackingTrialCount,
    double MinimumAcceptedRelaxationFactor,
    IReadOnlyList<JacobianHydraulicIteration> Iterations);
