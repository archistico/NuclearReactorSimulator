using NuclearReactorSimulator.Domain.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Shadow-only diagnostic contract for inspecting the deterministic branch-selection behavior of the
/// simplified conserved-inventory water/steam inverse map. It does not alter the production resolver.
/// </summary>
public interface IWaterSteamInverseBranchDiagnosticProvider
{
    WaterSteamInverseBranchSelectionDiagnostic DiagnoseInverseBranchSelection(
        FluidNodeDefinition definition,
        FluidNodeInventory inventory,
        FluidThermodynamicState previousState);
}

public sealed record WaterSteamInverseBranchCandidate(
    string Branch,
    int AttemptOrder,
    bool RootFound,
    string Phase,
    double PressurePascals,
    double TemperatureKelvins,
    double? VaporQuality);

public sealed record WaterSteamInverseBranchSelectionDiagnostic(
    string NodeId,
    double SpecificVolumeCubicMetresPerKilogram,
    double SpecificInternalEnergyJoulesPerKilogram,
    string ProductionSelectedBranch,
    string ProductionSelectedPhase,
    bool SaturatedRootAvailable,
    bool SuperheatedRootAvailable,
    bool MultiplePhaseRootsAvailable,
    bool CoarseSaturatedRootFound,
    bool BoundaryAwareSaturatedRootFound,
    bool CoarseSuperheatedRootFound,
    bool BoundaryAwareSuperheatedRootFound,
    bool LateBoundarySaturatedShadowedByEarlierSuperheated,
    IReadOnlyList<WaterSteamInverseBranchCandidate> Candidates);
