using NuclearReactorSimulator.Domain.Plant;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// Immutable per-step numerical diagnostics for the plant-network hydraulic coupling.
/// These values describe the numerical path used to obtain the candidate state; they are not plant physics.
/// </summary>
public sealed record PlantNetworkHydraulicNumericalSnapshot(
    HydraulicNumericalCouplingMode Mode,
    bool UsedSemiImplicitCorrection,
    int IterationCount,
    bool Converged,
    double PredictorMaximumFractionalSubcooledPressureChange,
    double PredictorMaximumAbsoluteHydraulicFlowChangeKilogramsPerSecond,
    double MaximumRelativePressureResidual,
    double MaximumAbsoluteFlowResidualKilogramsPerSecond)
{
    public static PlantNetworkHydraulicNumericalSnapshot Explicit { get; } = new(
        HydraulicNumericalCouplingMode.ExplicitCommittedState,
        false,
        1,
        true,
        0d,
        0d,
        0d,
        0d);
}
