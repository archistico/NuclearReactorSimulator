using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;

public sealed record FeedwaterBoundarySnapshot(
    string BoundaryId,
    string SteamDrumId,
    string TargetNodeId,
    MassFlowRate MassFlowRate,
    FluidEnergyTransportMode EnergyTransportMode,
    SpecificEnergy SpecificInternalEnergy,
    SpecificEnergy SpecificFlowWork,
    SpecificEnergy SpecificEnthalpy,
    Power InternalEnergyInputRate,
    Power FlowWorkInputRate,
    Power EnergyInputRate);
