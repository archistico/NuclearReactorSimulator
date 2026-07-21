using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;

public sealed record TurbineAdmissionBoundarySnapshot(
    string BoundaryId,
    string AdmissionTrainId,
    string SourceNodeId,
    MassFlowRate MassFlowRate,
    SpecificEnergy ExportedSpecificInternalEnergy,
    Power EnergyExportRate);
