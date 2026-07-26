using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;

public sealed record TurbineBypassStepResult(
    IReadOnlyList<TurbineBypassSnapshot> Snapshots,
    PlantNetworkSourceTerms SourceTerms);
