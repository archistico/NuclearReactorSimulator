using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;

public sealed record TurbineAdmissionBoundarySnapshot(
    string BoundaryId,
    string AdmissionTrainId,
    string SourceNodeId,
    MassFlowRate MassFlowRate,
    FluidEnergyTransportMode EnergyTransportMode,
    SpecificEnergy ExportedSpecificInternalEnergy,
    SpecificEnergy ExportedSpecificFlowWork,
    SpecificEnergy ExportedSpecificEnthalpy,
    Power InternalEnergyExportRate,
    Power FlowWorkExportRate,
    Power EnergyExportRate);
