using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;

public sealed record MainSteamReliefBoundaryStepResult(
    IReadOnlyList<MainSteamReliefBoundarySnapshot> Snapshots,
    PlantNetworkSourceTerms SourceTerms);
