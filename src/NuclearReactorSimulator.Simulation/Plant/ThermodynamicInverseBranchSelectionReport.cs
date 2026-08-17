using NuclearReactorSimulator.Simulation.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Plant;

/// <summary>
/// M10.9.4.1-H.12 shadow-only evidence explaining how the simplified conserved-inventory
/// thermodynamic inverse map selects between overlapping phase roots near H.11 boundaries.
/// </summary>
public sealed record ThermodynamicInverseBranchSelectionReport(
    IReadOnlyList<ThermodynamicInverseBranchNodeDiagnosis> Nodes)
{
    public int NodeCount => Nodes.Count;

    public int OverlappingRootNodeCount => Nodes.Count(static item => item.AllProbesHaveOverlappingPhaseRoots);

    public int CoarseDetectionToggleNodeCount => Nodes.Count(static item => item.CoarseSaturatedDetectionToggles);

    public int LateBoundarySaturatedShadowNodeCount => Nodes.Count(static item => item.LateBoundarySaturatedShadowedCount > 0);

    public int PreviousStateTieBreakNodeCount => Nodes.Count(static item => item.PreviousStateTieBreakObserved);
}

public sealed record ThermodynamicInverseBranchNodeDiagnosis(
    string NodeId,
    string NominalPhase,
    bool AllProbesHaveOverlappingPhaseRoots,
    bool CoarseSaturatedDetectionToggles,
    int LateBoundarySaturatedShadowedCount,
    bool PreviousStateTieBreakObserved,
    string MechanismClassification,
    string RecommendedShadowPolicy,
    IReadOnlyList<ThermodynamicInverseBranchProbeDiagnosis> Probes);

public sealed record ThermodynamicInverseBranchProbeDiagnosis(
    string NodeId,
    string Probe,
    double MassKilograms,
    double InternalEnergyJoules,
    string H11ResolvedPhase,
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
    bool PreviousStateSelectionSensitive,
    IReadOnlyList<WaterSteamInverseBranchCandidate> Candidates);
