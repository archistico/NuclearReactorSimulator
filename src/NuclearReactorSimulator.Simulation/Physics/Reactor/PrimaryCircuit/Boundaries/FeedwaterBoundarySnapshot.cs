using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;

public sealed record FeedwaterBoundarySnapshot(
    string BoundaryId,
    string SteamDrumId,
    string TargetNodeId,
    MassFlowRate MassFlowRate,
    SpecificEnergy SpecificInternalEnergy,
    Power EnergyInputRate);
