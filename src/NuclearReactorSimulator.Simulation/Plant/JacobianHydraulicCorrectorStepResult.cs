using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Result of one H.9 Jacobian-informed hydraulic correction.
/// H.9-H.21 consume it as shadow evidence; H.22 may consume it only through the separately opt-in,
/// fail-closed corrected-candidate commit seam.
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
    IReadOnlyList<JacobianHydraulicIteration> Iterations)
{
    /// <summary>
    /// Pump hydraulic power associated with the actually applied H.9 iterate, used by H.22 when the corrected
    /// hydraulic balances become the opt-in committed candidate.
    /// </summary>
    public Power AppliedPumpHydraulicPowerExchange { get; init; } = Power.Zero;
}
