using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Result of one deterministic hybrid predictor/corrector decision. H.4 used it for audit-only evidence;
/// H.5 may apply the selected balances through the canonical production orchestrator for opt-in current-v2 definitions.
/// </summary>
public sealed record HybridSemiImplicitHydraulicGateStepResult(
    PlantState CandidateState,
    SemiImplicitHydraulicEvaluation HydraulicEvaluation,
    IReadOnlyDictionary<string, FluidNodeBalance> AppliedHydraulicBalances,
    bool UsedSemiImplicitCorrection,
    int IterationCount,
    bool Converged,
    double PredictorMaximumFractionalSubcooledPressureChange,
    double PredictorMaximumAbsoluteHydraulicFlowChangeKilogramsPerSecond,
    double MaximumRelativePressureResidual,
    double MaximumAbsoluteFlowResidualKilogramsPerSecond);
