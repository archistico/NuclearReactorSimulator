using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;

internal sealed record TurbineAdmissionBoundaryStepResult(
    IReadOnlyList<TurbineAdmissionBoundarySnapshot> Snapshots,
    PlantNetworkSourceTerms SourceTerms);
