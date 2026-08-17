using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Result of one deterministic semi-implicit hydraulic correction. H.3 used the type in isolation;
/// H.5 may apply the selected candidate through the canonical orchestrator after the H.4 gate triggers.
/// </summary>
public sealed record SemiImplicitHydraulicPrototypeStepResult(
    PlantState CandidateState,
    SemiImplicitHydraulicEvaluation HydraulicEvaluation,
    IReadOnlyDictionary<string, FluidNodeBalance> AppliedHydraulicBalances,
    int IterationCount,
    bool Converged,
    double MaximumRelativePressureResidual,
    double MaximumAbsoluteFlowResidualKilogramsPerSecond);
