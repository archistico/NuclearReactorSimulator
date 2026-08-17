using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;

public sealed record SteamExportBoundarySnapshot(
    string BoundaryId,
    string SteamDrumId,
    string SourceNodeId,
    MassFlowRate MassFlowRate,
    FluidEnergyTransportMode EnergyTransportMode,
    SpecificEnergy ExportedSpecificInternalEnergy,
    SpecificEnergy ExportedSpecificFlowWork,
    SpecificEnergy ExportedSpecificEnthalpy,
    Power InternalEnergyExportRate,
    Power FlowWorkExportRate,
    Power EnergyExportRate);
