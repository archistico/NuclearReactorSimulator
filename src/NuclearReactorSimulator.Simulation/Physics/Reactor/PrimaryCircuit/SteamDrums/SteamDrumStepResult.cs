using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.SteamDrums;

public sealed record SteamDrumStepResult(
    SteamDrumSystemSnapshot Snapshot,
    PlantNetworkSourceTerms SourceTerms);
