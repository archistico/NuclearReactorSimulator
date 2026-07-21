using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;

public sealed record SteamExportBoundarySnapshot(
    string BoundaryId,
    string SteamDrumId,
    string SourceNodeId,
    MassFlowRate MassFlowRate,
    SpecificEnergy ExportedSpecificInternalEnergy,
    Power EnergyExportRate);
